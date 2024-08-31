using System.Text.Json;
using itembank_index_backend.Models.Definition;
using itembank_index_backend.Models.Settings;
using itembank_index_backend.Models.Requests;
using itembank_index_backend.Models.Responses;
using itembank_index_backend.Models.Resources.Elastic;
using ResourceS3 = itembank_index_backend.Models.Resources.S3;
using itembank_index_backend.Models.Resources.Sql;
using RepositoryElastic = itembank_index_backend.Repositories.Elasticsearch;
using RepositorySql = itembank_index_backend.Repositories.Sql;
using Microsoft.Extensions.Options;

namespace itembank_index_backend.Services;

public class ItemService
{
    private readonly AppSettings _appSettings;
    private readonly IHttpClientFactory _clientFactory;
    private readonly RepositoryElastic.ItemRepository _itemRepositoryElastic;
    private readonly RepositorySql.ItemRepository _itemRepositorySql;

    public ItemService(IOptions<AppSettings> appSettings, IHttpClientFactory clientFactory,
        RepositoryElastic.ItemRepository itemRepositoryElastic, RepositorySql.ItemRepository itemRepositorySql)
    {
        _appSettings = appSettings.Value;
        _clientFactory = clientFactory;
        _itemRepositoryElastic = itemRepositoryElastic;
        _itemRepositorySql = itemRepositorySql;
    }

    public bool InvalidConditionContent(ItemConditionReq itemConditionReq, bool needTransform = true)
    {
        // no need to check the conditions
        return false;
    }

    /**
     * pure item id sesarch
     */
    public async Task<List<string>> ConditionSearchIdsAsync(ItemConditionReq itemConditionReq)
    {
        var elasticResult = await _itemRepositoryElastic.SearchItemIdAsync(itemConditionReq, true);
        if (elasticResult.TotalElements == 0)
        {
            return new List<string>();
        }

        return elasticResult.Ids;
    }

    /**
     * store this condition information to elastic then return the document id
     */
    public async Task<string> SetConditionIndexAsync(ItemConditionReq itemConditionReq)
    {
        return await _itemRepositoryElastic.SaveSearchRecordAsync(itemConditionReq);
    }

    public async Task<ItemConditionReq> GetConditionIndexAsync(string recordId)
    {
        return await _itemRepositoryElastic.RetrieveSearchRecordAsync(recordId);
    }

    /**
     * condition information filtered by elastic, then retrieve completed document from itembankapi,
     * there are two search approaches, the special sorting will be adopted if the "sortField" is "inputId"
     */
    public async Task<ItemResourceResp> ConditionSearchAsync(ItemConditionReq itemConditionReq)
    {
        if (_appSettings.ElasticsearchReservedWordSeasrchEnable == AppSettingDefinition.Enabled)
        {
            itemConditionReq.MappingReservedWord();
        }

        bool sortByInputIdList = itemConditionReq.SortField == "inputId" && itemConditionReq.Ids != null &&
                                 itemConditionReq.Ids.Count > 0;

        var itemResult = sortByInputIdList
            ? await SearchSortByInputIds(itemConditionReq)
            : await SearchNormal(itemConditionReq);

        return itemResult;
    }

    /**
     * normal search, sort by the "itemConditionReq.sortField" field value that passed to elastic
     */
    public async Task<ItemResourceResp> SearchNormal(ItemConditionReq itemConditionReq)
    {
        var elasticResult = await _itemRepositoryElastic.SearchItemIdAsync(itemConditionReq);
        if (elasticResult == null || elasticResult.TotalElements == 0)
        {
            return null;
        }

        var itemRawList = await GetItembankRawResourceAsync(elasticResult.Ids);
        var itemResult = await ConvertRawToResponseContent(itemRawList);
        Dictionary<string, Content> itemResultContentMap = new Dictionary<string, Content>();
        foreach (var item in itemResult.content!)
        {
            itemResultContentMap.Add(item.id, item);
        }

        // sorting according to the elastic result
        List<Content> sortedResult = new List<Content>();
        foreach (var id in elasticResult.Ids)
        {
            if (!itemResultContentMap.ContainsKey(id)) continue;
            sortedResult.Add(itemResultContentMap[id]);
        }

        itemResult.content = sortedResult;

        double d = itemConditionReq.PageSize <= 0 ? 0 : (double)elasticResult.TotalElements / itemConditionReq.PageSize;
        int totalPages = itemConditionReq.PageSize <= 0 ? 0 : (int)Math.Ceiling(d);

        itemResult.totalPages = totalPages;
        itemResult.totalElements = (int)elasticResult.TotalElements;
        itemResult.hasContent = itemResult.content is { Count: > 0 };
        itemResult.isLastPage = itemConditionReq.PageNumber == totalPages;
        itemResult.number = itemConditionReq.PageNumber;
        itemResult.size = itemConditionReq.PageSize;
        itemResult.numberOfElements = itemResult.content!.Count;
        itemResult.isFirstPage = itemConditionReq.PageNumber == 1;
        itemResult.hasPreviousPage = !itemResult.isFirstPage;
        itemResult.hasNextPage = !itemResult.isLastPage;

        return itemResult;
    }

    /**
     * the result sort by the "inputId" field, should be extra sorted according to the "itemConditionReq.Ids" list, and the field pass to elastic doesn't matter.<br/>
     * *** note: the itemConditionReq.ids must be valued, should check before calling this method
     */
    public async Task<ItemResourceResp> SearchSortByInputIds(ItemConditionReq itemConditionReq)
    {
        int originSize = itemConditionReq.PageSize;
        int originNumber = itemConditionReq.PageNumber;
        itemConditionReq.PageSize = itemConditionReq.Ids.Count;
        itemConditionReq.PageNumber = 1;

        var elasticResult = await _itemRepositoryElastic.SearchItemIdAsync(itemConditionReq);
        if (elasticResult == null || elasticResult.TotalElements == 0)
        {
            return null;
        }

        itemConditionReq.PageSize = originSize;
        itemConditionReq.PageNumber = originNumber;

        Dictionary<string, bool> resultIdMap = new Dictionary<string, bool>();
        foreach (var id in elasticResult.Ids)
        {
            resultIdMap[id] = true;
        }

        // sorting according to the request list order
        List<string> resultSortedIds = new List<string>();
        foreach (var id in itemConditionReq.Ids)
        {
            if (resultIdMap.ContainsKey(id))
            {
                resultSortedIds.Add(id);
            }
        }

        var skipCount = originSize * (originNumber - 1);
        var remainingCount = resultSortedIds.Count - skipCount;
        var resultPageItemIds = resultSortedIds.Skip(skipCount)
            .Take(originSize > remainingCount ? remainingCount : originSize).ToList();
        var itemRawList = await GetItembankRawResourceAsync(elasticResult.Ids);
        var itemResult = await ConvertRawToResponseContent(itemRawList);

        Dictionary<string, Content> contentMap = new Dictionary<string, Content>();
        foreach (var content in itemResult.content)
        {
            contentMap[content.id] = content;
        }

        List<Content> contentList = new List<Content>();
        foreach (var id in resultPageItemIds)
        {
            if (contentMap.ContainsKey(id))
            {
                contentList.Add(contentMap[id]);
            }
        }

        itemResult.content = contentList;

        double d = itemConditionReq.PageSize <= 0 ? 0 : (double)elasticResult.TotalElements / itemConditionReq.PageSize;
        int totalPages = itemConditionReq.PageSize <= 0 ? 0 : (int)Math.Ceiling(d);

        itemResult.totalPages = totalPages;
        itemResult.totalElements = (int)elasticResult.TotalElements;
        itemResult.hasContent = itemResult.content is { Count: > 0 };
        itemResult.isLastPage = itemConditionReq.PageNumber == totalPages;
        itemResult.number = itemConditionReq.PageNumber;
        itemResult.size = itemConditionReq.PageSize;
        itemResult.numberOfElements = itemResult.content!.Count;
        itemResult.isFirstPage = itemConditionReq.PageNumber == 1;
        itemResult.hasPreviousPage = !itemResult.isFirstPage;
        itemResult.hasNextPage = !itemResult.isLastPage;

        return itemResult;
    }

    public async Task<bool> WriteOrUpdateAsync(ItemResourceElastic item)
    {
        if (_appSettings.ElasticsearchReservedWordSeasrchEnable == AppSettingDefinition.Enabled)
        {
            item.MappingReservedWord();
        }

        return await _itemRepositoryElastic.UpsertItemAsync(item);
    }

    public async Task<bool> WriteOrUpdateMultiAsync(List<ItemResourceElastic> items)
    {
        if (_appSettings.ElasticsearchReservedWordSeasrchEnable == AppSettingDefinition.Enabled)
        {
            foreach (var item in items)
            {
                item.MappingReservedWord();
            }
        }

        return await _itemRepositoryElastic.BulkUpsertItemsAsync(items);
    }

    /**
     * gets raw data from sql
     */
    public async Task<List<ItemResourceSql>> GetItembankRawResourceAsync(List<string> ids)
    {
        return await _itemRepositorySql.GetItemsAsync(ids);
    }

    /**
     * converts the raw data to response content
     */
    public async Task<ItemResourceResp> ConvertRawToResponseContent(List<ItemResourceSql> rows)
    {
        ItemResourceResp itemResult = new ItemResourceResp() { content = new List<Content>() };
        Dictionary<string, int> indexMap = new Dictionary<string, int>();
        Dictionary<string, List<int>> bodyOfKnowledgeMap = new Dictionary<string, List<int>>();
        foreach (var data in rows)
        {
            if (indexMap.ContainsKey(data.id))
            {
                var item = itemResult.content[indexMap[data.id]];

                if (data.applicableYear != null && !item.applicableYears.Contains(data.applicableYear.Value))
                {
                    item.applicableYears.Add(data.applicableYear.Value);
                }

                if (data.bodyOfKnowledgeId != null)
                {
                    var bodyOfKnowledgeId = data.bodyOfKnowledgeId.Value;
                    if (!bodyOfKnowledgeMap.ContainsKey(data.id) || !bodyOfKnowledgeMap[data.id].Contains(bodyOfKnowledgeId))
                    {
                        item.bodyOfKnowledges.Add(new BodyOfKnowledge()
                        {
                            code = data.bodyOfKnowledgeCode,
                            name = data.bodyOfKnowledgeName,
                            subjectId = data.bodyOfKnowledgeSubjectId,
                            initiationYear = data.bodyOfKnowledgeInitiationYear,
                            finalYear = data.bodyOfKnowledgeFinalYear
                        });
                        bodyOfKnowledgeMap[data.id].Add(bodyOfKnowledgeId);
                    }
                }

                if (data.QuestionIndex != null &&
                    item.content.questions.Count >= data.QuestionIndex)
                {
                    item.content.questions[data.QuestionIndex.Value].answeringMethod =
                        data.AnsweringMethod;
                }

                continue;
            }

            indexMap[data.id] = itemResult.content.Count;

            var content = new Content()
            {
                id = data.id,
                fidelity = data.fidelity,
                difficulty = data.difficulty,
                subjectIds = data.subjectIds.Split(",").ToList(),
                solution = data.solution,
                onlineReadiness = data.onlineReadiness,
                isOnlineReady = data.onlineReadiness == ItemDefinition.OnlineReadinessReady,
                createdOn = data.createdOn,
                updatedOn = data.updatedOn,
            };

            if (data.bodyOfKnowledgeId != null)
            {
                content.bodyOfKnowledges.Add(new BodyOfKnowledge()
                {
                    code = data.bodyOfKnowledgeCode,
                    name = data.bodyOfKnowledgeName,
                    subjectId = data.bodyOfKnowledgeSubjectId,
                    initiationYear = data.bodyOfKnowledgeInitiationYear,
                    finalYear = data.bodyOfKnowledgeFinalYear
                });
                bodyOfKnowledgeMap[data.id] = new List<int>() { data.bodyOfKnowledgeId.Value };
            }

            if (data.applicableYear != null)
            {
                content.applicableYears.Add(data.applicableYear.Value);
            }

            if (data.subjectIds != null)
            {
                content.subjectIds = data.subjectIds.Split(",").ToList();
            }

            if (data.content != null)
            {
                content.content = JsonSerializer.Deserialize<ItemContent>(data.content);
                content.isSet = content.content.questionCount != null && content.content.questionCount > 1;
                if (content.content.questions != null && content.content.questions.Count > 0)
                {
                    content.content.questions[0].answeringMethod = data.AnsweringMethod;
                }
            }

            if (data.metadata != null)
            {
                content.metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(data.metadata);
            }

            if (data.resourceLinks != null)
            {
                content.resourceLinks = JsonSerializer.Deserialize<List<ResourceLink>>(data.resourceLinks);
            }

            itemResult.content.Add(content);
        }

        return itemResult;
    }

    /**
     * deprecated, gets item detail from itembank api
     */
    public async Task<ItemResourceResp> GetItembankAPIAsync(List<string> ids)
    {
        string itemIds = string.Join("&itemIds=", ids);
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_appSettings.ItembankApiUrl}/api/v1/item/search?pageSize={ids.Count}&itemIds={itemIds}");
        var client = _clientFactory.CreateClient();
        var response = await client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                // debug
                // string str = response.Content.ReadAsStringAsync().Result;
                return await response.Content.ReadFromJsonAsync<ItemResourceResp>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        return null;
    }

    public async Task<ResourceS3.ItemResourceS3> GetItemS3Async(string id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_appSettings.S3Url}/v1/items/{id}/item.json");
        var client = _clientFactory.CreateClient();
        var response = await client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        return null;
    }

    public async Task<IReadOnlyCollection<ResourceS3.ItemResourceS3>> GetItemsS3Async(
        IReadOnlyCollection<string> itemIds)
    {
        var itemResources = new List<ResourceS3.ItemResourceS3>();

        var queueItemIds = new Queue<string>(itemIds);

        while (queueItemIds.Any())
        {
            var currentItemIds = new List<string>();

            var tasks = new List<Task<ResourceS3.ItemResourceS3>>();

            while (queueItemIds.Any() && currentItemIds.Count < 20)
            {
                var itemId = queueItemIds.Dequeue();

                tasks.Add(GetItemS3Async(itemId));

                currentItemIds.Add(itemId);
            }

            foreach (var task in tasks)
            {
                itemResources.Add(await task);
            }
        }

        return itemResources;
    }
}