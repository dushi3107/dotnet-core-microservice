using System.Linq.Expressions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.QueryDsl;
using itembank_index_backend.Models.Definition;
using itembank_index_backend.Models.Settings;
using itembank_index_backend.Models.Requests;
using itembank_index_backend.Models.Resources.Elastic;
using itembank_index_backend.Models.Responses;
using Microsoft.Extensions.Options;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace itembank_index_backend.Repositories.Elasticsearch;

public class ItemRepository : Repository<ItemResourceElastic>
{
    private string _recordIndex;

    public ItemRepository(IOptions<AppSettings> appSettings, ILogger<Repository<ItemResourceElastic>> logger) : base(
        appSettings, logger)
    {
        _recordIndex = appSettings.Value.ElasticsearchRecordIndex;
    }

    private Action<QueryDescriptor<ItemResourceElastic>> BuildQueryDescriptor(ItemConditionReq req)
    {
        // 入學學程
        Action<QueryDescriptor<ItemResourceElastic>> tempCondition;
        List<Action<QueryDescriptor<ItemResourceElastic>>> baseConditions = [];
        List<Action<QueryDescriptor<ItemResourceElastic>>> exclusionConditions = [];

        // ***篩選條件
        // 科目, 入學學程, 適用學年度,
        // 產品(不分年度、版本), 目錄(區分年度、版本), 出處, 來源, 版本,
        // 知識向度, 必出知識向度, 課名, 必出課名,
        // 認知向度, 題型, 作答方式, 解析,
        // 解題影片, 題組, 答案, 比對結果,
        // 版權, 素養題, 編輯備註, 入庫檔名,
        // 答案格式使用方程式, 議題, 檔案id(import id), 文件id(document id),
        // 五欄資料夾id(document repository id), 題目id
        Func<ItemConditionReq, Action<QueryDescriptor<ItemResourceElastic>>>[] conditionFuncs =
        [
            SubjectConditions, BodyOfKnowledgeCodeConditions, ItemYearsConditions,
            ProductCodesCondition, CatalogCondition, PublishSourcesCondition, SourcesCondition, VersionIdsCondition,
            KnowledgeIdsCondition, DiscreteKnowledgeIdsCondition, LessonIdsCondition, DiscreteLessonIdsCondition,
            RecognitionIdsCondition, UserTypesCondition, AnsweringMethodsCondition, HasSolutionCondition,
            HasVideoUrlsCondition, IsSetCondition, AnswersCondition, OnlineReadinessCondition,
            CopyrightCondition, IsLiteracyCondition, EditorRemarksCondition, FilenamesCondition,
            HasLatexCondition, TopicsCondition, ImportRecordIdsCondition, DocumentIdsCondition,
            DocumentRepositoryIdsCondition, IdsCondition
        ];

        // 線上測驗上下架狀態
        if (req.ProductStatus != ItemDefinition.OffShelfProductStatus)
        {
            conditionFuncs = conditionFuncs.Append(ProductStatusCondition).ToArray();
        }

        // priori to consider the more rigorous condition
        // 關鍵字(交集/聯集)
        if (req.MustSearchTexts != null && req.MustSearchTexts.Any())
        {
            conditionFuncs = conditionFuncs.Append(MustSearchTextsCondition).ToArray();
        }
        else if (req.SearchTexts != null && req.SearchTexts.Any())
        {
            conditionFuncs = conditionFuncs.Append(SearchTextsCondition).ToArray();
        }

        foreach (var func in conditionFuncs)
        {
            tempCondition = func(req);
            if (tempCondition != null) baseConditions.Add(tempCondition);
        }

        // ***剔除條件
        Func<ItemConditionReq, Action<QueryDescriptor<ItemResourceElastic>>>[] excludeConditionFuncs =
        [
            NeLabelNamesConditions, NeItemYearsConditions, NePublishSourcesCondition, NeCatalogCondition,
            NeSourcesCondition, NeVersionIdsCondition, NeKnowledgeIdsCondition,
            NeDiscreteKnowledgeIdsCondition, NeLessonIdsCondition, NeDiscreteLessonIdsCondition,
            NeRecognitionIdsCondition, NeUserTypesCondition, NeAnsweringMethodsCondition, NeEditorRemarksCondition,
            NeFilenamesCondition, NeSearchTextsCondition, NeMustSearchTextsCondition, NeImportRecordIdsCondition,
            NeDocumentIdsCondition, NeDocumentRepositoryIdsCondition, NeIdsCondition
        ];

        // 線上測驗上下架狀態
        if (req.ProductStatus == ItemDefinition.OffShelfProductStatus)
        {
            req.ProductStatus = ItemDefinition.OnShelfProductStatus;
            excludeConditionFuncs = excludeConditionFuncs.Append(ProductStatusCondition).ToArray();
        }

        foreach (var func in excludeConditionFuncs)
        {
            tempCondition = func(req);
            if (tempCondition != null) exclusionConditions.Add(tempCondition);
        }

        return q => q
            .Bool(b => b
                .Must(baseConditions.ToArray())
                .MustNot(exclusionConditions.ToArray())
            );
    }

    private SortOptionsDescriptor<ItemResourceElastic> BuildSortDescriptor(ItemConditionReq req)
    {
        if (string.IsNullOrEmpty(req.SortField))
        {
            return null;
        }

        SortOrder order = req.Ascending ? SortOrder.Asc : SortOrder.Desc;
        if (req.SortField == "inputId") // do sorting in service level
        {
            return null;
        }

        return new SortOptionsDescriptor<ItemResourceElastic>().Field(req.SortField, new FieldSort { Order = order });
    }

    public async Task<ItemIdsElastic> SearchItemIdAsync(ItemConditionReq req, bool isPureIdSearch = false)
    {
        try
        {
            SearchResponse<ItemResourceElastic> response;
            if (isPureIdSearch)
            {
                response = await _elasticsearchClient.SearchAsync<ItemResourceElastic>(s => s
                    .Sort(BuildSortDescriptor(req))
                    .Index(_index)
                    .Query(BuildQueryDescriptor(req))
                    .Size(Int32.MaxValue)
                    .From(0)
                    .SourceIncludes("Id")
                    .TrackTotalHits(new TrackHits(true))
                );
            }
            else
            {
                response = await _elasticsearchClient.SearchAsync<ItemResourceElastic>(s => s
                    // .Profile()
                    .Sort(BuildSortDescriptor(req))
                    .Index(_index)
                    .Query(BuildQueryDescriptor(req))
                    .Size(req.PageSize)
                    .From((req.PageNumber - 1) * req.PageSize)
                    .TrackTotalHits(new TrackHits(true))
                );
            }

            if (response == null)
            {
                throw new Exception("response is null");
            }

            if (!response.IsValidResponse)
            {
                throw new Exception(response.ToString());
            }

            return new ItemIdsElastic()
                { TotalElements = response.Total, Ids = response.Hits.ToArray().Select(i => i.Id).ToList() };
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return null;
        }
    }

    public async Task<string> SaveSearchRecordAsync(ItemConditionReq req)
    {
        try
        {
            // not compatible with old version
            var response = await _elasticsearchClient.IndexAsync(
                new ItemRecordsElastic() { Data = JsonSerializer.Serialize(req) }, c => c
                    .Index(_recordIndex));

            if (response == null)
            {
                throw new Exception("response is null");
            }

            if (!response.IsValidResponse)
            {
                throw new Exception(response.ToString());
            }

            return response.Id;
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return null;
        }
    }

    public async Task<ItemConditionReq> RetrieveSearchRecordAsync(string recordId)
    {
        try
        {
            // Force a refresh to avoid elasticsearch indexing delay
            var refreshResponse = await _elasticsearchClient.Indices.RefreshAsync(_recordIndex);
            if (!refreshResponse.IsValidResponse)
            {
                throw new Exception(refreshResponse.ToString());
            }

            // not compatible with old version
            var response = await _elasticsearchClient.SearchAsync<ItemRecordsElastic>(c => c
                .Index(_recordIndex)
                .Query(q => q
                    .Match(m => m
                        .Field("_id")
                        .Query(recordId)
                    )
                ));

            if (response == null)
            {
                throw new Exception("response is null");
            }

            if (!response.IsValidResponse)
            {
                throw new Exception(response.ToString());
            }

            return JsonSerializer.Deserialize<ItemConditionReq>(response.Hits.First().Source.Data);
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return null;
        }
    }

    /**
     * just from elastic
     */
    public async Task<ItemResourcesElasticResp> SearchItemAsync(ItemConditionReq req)
    {
        if (string.IsNullOrEmpty(req.BodyOfKnowledgeCode))
        {
            return new ItemResourcesElasticResp() { };
        }

        try
        {
            SearchResponse<ItemResourceElastic> response =
                await _elasticsearchClient.SearchAsync<ItemResourceElastic>(s => s
                    .Sort(BuildSortDescriptor(req))
                    .Index(_index)
                    .Query(BuildQueryDescriptor(req))
                    // .Analyzer("ik_max_word")
                    .Size(req.PageSize)
                    .From((req.PageNumber - 1) * req.PageSize)
                    .TrackTotalHits(new TrackHits(true))
                );

            if (response == null)
            {
                throw new Exception("response is null");
            }

            if (!response.IsValidResponse)
            {
                throw new Exception(response.ToString());
            }

            return new ItemResourcesElasticResp()
            {
                TotalElements = response.Total, Items = response.Documents.ToList(), Size = req.PageSize,
                Number = req.PageNumber, TotalPages = (int)Math.Ceiling((double)response.Total / req.PageSize)
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return null;
        }
    }

    public async Task<bool> UpsertItemAsync(ItemResourceElastic item)
    {
        if (item == null) return false;

        try
        {
            var response = await _elasticsearchClient.IndexAsync(item, _index, item.Id);

            if (response == null)
            {
                throw new Exception("response is null");
            }

            if (!response.IsValidResponse)
            {
                throw new Exception(response.ToString());
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return false;
        }
    }

    public async Task<bool> BulkUpsertItemsAsync(List<ItemResourceElastic> items)
    {
        if (items == null || items.Count == 0) return false;

        try
        {
            // bulk insert to new server
            var operations = new List<IBulkOperation>();
            foreach (var item in items)
            {
                operations.Add(new BulkIndexOperation<ItemResourceElastic>(item));
            }

            var response = await _elasticsearchClient.BulkAsync(new BulkRequest(_index) { Operations = operations });

            if (response == null)
            {
                throw new Exception("response is null");
            }

            if (!response.IsValidResponse)
            {
                throw new Exception(response.ToString());
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return false;
        }
    }

    /**
     * 科目
     */
    private Action<QueryDescriptor<ItemResourceElastic>> SubjectConditions(ItemConditionReq req)
    {
        if (string.IsNullOrEmpty(req.SubjectId))
        {
            return null;
        }

        return m => m
            .MatchPhrase(ma => ma
                .Field(f => f.SubjectIds)
                .Query(req.SubjectId)
            );
    }

    /**
     * 入學學程
     */
    private Action<QueryDescriptor<ItemResourceElastic>> BodyOfKnowledgeCodeConditions(ItemConditionReq req)
    {
        if (string.IsNullOrEmpty(req.BodyOfKnowledgeCode))
        {
            return null;
        }

        if (req.BodyOfKnowledgeCode == ItemDefinition.NullBodyOfKnowledgeCode)
        {
            return BuildFieldExistsAndNonEmptyCondition(false, f => f.BodyOfKnowledgeCodes);
        }

        return m => m
            .MatchPhrase(ma => ma
                .Field(f => f.BodyOfKnowledgeCodes)
                .Query(req.BodyOfKnowledgeCode)
            );
    }

    /**
     * 適用學年度
     */
    private Action<QueryDescriptor<ItemResourceElastic>> ItemYearsConditions(ItemConditionReq req)
    {
        if (req.ItemYears == null || !req.ItemYears.Any())
        {
            return null;
        }

        return m => m
            .Nested(n => n
                .Path(p => p.ItemYears)
                .Query(nq => nq
                    .Terms(t => t
                        .Field(f => f.ItemYears.FirstOrDefault().Year)
                        .Terms(new TermsQueryField(req.ItemYears.Select(val => FieldValue.String(val)).ToArray()))))
            );
    }

    /**
     * 標籤名稱-剔除 被標記為刪除的項目
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeLabelNamesConditions(ItemConditionReq req)
    {
        return m => m
            .Terms(ma => ma
                .Field(f => f.LabelNames)
                .Terms(new TermsQueryField(req.NeLabelNames.Select(val => FieldValue.String(val)).ToArray()))
            );
    }

    /**
     * 適用學年度-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeItemYearsConditions(ItemConditionReq req)
    {
        if (req.NeItemYears == null || !req.NeItemYears.Any())
        {
            return null;
        }

        return m => m
            .Nested(n => n
                .Path(p => p.ItemYears)
                .Query(nq => nq
                    .Terms(t => t
                        .Field(f => f.ItemYears.FirstOrDefault().Year)
                        .Terms(new TermsQueryField(req.NeItemYears.Select(val => FieldValue.String(val)).ToArray()))))
            );
    }

    /**
     * 產品(不分年度、版本)
     */
    private Action<QueryDescriptor<ItemResourceElastic>> ProductCodesCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.ProductCodes, f => f.ProductCodes);
    }

    /**
     * 目錄(區分年度、版本)
     */
    private Action<QueryDescriptor<ItemResourceElastic>> CatalogCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.CatalogIds, f => f.CatalogIds);
    }

    /**
     * 目錄(區分年度、版本)-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeCatalogCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeCatalogIds, f => f.CatalogIds);
    }

    /**
     * 出處
     */
    private Action<QueryDescriptor<ItemResourceElastic>> PublishSourcesCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.PublishSources, f => f.PublishSources);
    }

    /**
     * 出處-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NePublishSourcesCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NePublishSources, f => f.PublishSources);
    }

    /**
     * 來源
     */
    private Action<QueryDescriptor<ItemResourceElastic>> SourcesCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.Sources, f => f.Sources);
    }

    /**
     * 來源-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeSourcesCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeSources, f => f.Sources);
    }

    /**
     * 版本
     */
    private Action<QueryDescriptor<ItemResourceElastic>> VersionIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.VersionIds, f => f.VersionIds);
    }

    /**
     * 版本-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeVersionIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeVersionIds, f => f.VersionIds);
    }

    /**
     * 知識向度
     */
    private Action<QueryDescriptor<ItemResourceElastic>> KnowledgeIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.KnowledgeIds, f => f.RegularKnowledgeIds);
    }

    /**
     * 知識向度-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeKnowledgeIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeKnowledgeIds, f => f.RegularKnowledgeIds);
    }

    /**
     * 必出知識向度
     */
    private Action<QueryDescriptor<ItemResourceElastic>> DiscreteKnowledgeIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.DiscreteKnowledgeIds, f => f.DiscreteKnowledgeIds);
    }

    /**
     * 必出知識向度-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeDiscreteKnowledgeIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeDiscreteKnowledgeIds, f => f.DiscreteKnowledgeIds);
    }

    /**
     * 課名
     */
    private Action<QueryDescriptor<ItemResourceElastic>> LessonIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.LessonIds, f => f.RegularLessonIds);
    }

    /**
     * 課名-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeLessonIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeLessonIds, f => f.RegularLessonIds);
    }

    /**
     * 必出課名
     */
    private Action<QueryDescriptor<ItemResourceElastic>> DiscreteLessonIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.DiscreteLessonIds, f => f.DiscreteLessonIds);
    }

    /**
     * 必出課名-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeDiscreteLessonIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeDiscreteLessonIds, f => f.DiscreteLessonIds);
    }

    /**
     * 認知向度
     */
    private Action<QueryDescriptor<ItemResourceElastic>> RecognitionIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.RecognitionIds, f => f.RecognitionIds);
    }

    /**
     * 認知向度-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeRecognitionIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeRecognitionIds, f => f.RecognitionIds);
    }

    /**
     * 題型
     */
    private Action<QueryDescriptor<ItemResourceElastic>> UserTypesCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.UserTypes, f => f.UserTypes);
    }

    /**
     * 題型-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeUserTypesCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeUserTypes, f => f.UserTypes);
    }

    /**
     * 作答方式
     */
    private Action<QueryDescriptor<ItemResourceElastic>> AnsweringMethodsCondition(ItemConditionReq req)
    {
        if (req.AnsweringMethods == null || !req.AnsweringMethods.Any())
        {
            return null;
        }

        return m => m
            .Nested(n => n
                .Path(p => p.Questions)
                .Query(nq => nq
                    .Terms(t => t
                        .Field(f => f.Questions.FirstOrDefault().AnsweringMethod)
                        .Terms(new TermsQueryField(
                            req.AnsweringMethods.Select(val => FieldValue.String(val)).ToArray()))))
            );
    }

    /**
     * 作答方式-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeAnsweringMethodsCondition(ItemConditionReq req)
    {
        if (req.NeAnsweringMethods == null || !req.NeAnsweringMethods.Any())
        {
            return null;
        }

        return m => m
            .Nested(n => n
                .Path(p => p.Questions)
                .Query(nq => nq
                    .Terms(t => t
                        .Field(f => f.Questions.FirstOrDefault().AnsweringMethod)
                        .Terms(new TermsQueryField(req.NeAnsweringMethods.Select(val => FieldValue.String(val))
                            .ToArray()))))
            );
    }

    /**
     * 解析
     */
    private Action<QueryDescriptor<ItemResourceElastic>> HasSolutionCondition(ItemConditionReq req)
    {
        if (req.HasSolution == null)
        {
            return null;
        }

        return BuildFieldExistsAndNonEmptyCondition(req.HasSolution, f => f.Solution);
    }

    /**
     * 解題影片
     */
    private Action<QueryDescriptor<ItemResourceElastic>> HasVideoUrlsCondition(ItemConditionReq req)
    {
        if (req.HasVideoUrls == null)
        {
            return null;
        }

        return BuildTermBoolCondition(req.HasVideoUrls, f => f.HasVideoUrls);
    }

    /**
     * 題組
     */
    private Action<QueryDescriptor<ItemResourceElastic>> IsSetCondition(ItemConditionReq req)
    {
        if (req.IsSet == null)
        {
            return null;
        }

        return BuildTermBoolCondition(req.IsSet, f => f.IsSet);
    }

    /**
     * 答案
     */
    private Action<QueryDescriptor<ItemResourceElastic>> AnswersCondition(ItemConditionReq req)
    {
        if (req.Answers == null || !req.Answers.Any())
        {
            return null;
        }

        return m => m
            .Nested(n => n
                .Path(p => p.Questions)
                .Query(nq => nq
                    .Terms(t => t
                        .Field(f => f.Questions.FirstOrDefault().Answers)
                        .Terms(new TermsQueryField(req.Answers.Select(val => FieldValue.String(val)).ToArray()))))
            );
    }

    /**
     * 比對結果
     */
    private Action<QueryDescriptor<ItemResourceElastic>> OnlineReadinessCondition(ItemConditionReq req)
    {
        if (string.IsNullOrEmpty(req.OnlineReadiness))
        {
            return null;
        }

        return BuildMatchPhraseCondition(req.OnlineReadiness, f => f.OnlineReadiness);
    }

    /**
     * 線上測驗上下架狀態
     */
    private Action<QueryDescriptor<ItemResourceElastic>> ProductStatusCondition(ItemConditionReq req)
    {
        if (req.ProductStatus == null || !req.ProductStatus.Any())
        {
            return null;
        }

        return m => m
            .Nested(n => n
                .Path(p => p.ProductStatuses)
                .Query(nq => nq
                    .Terms(t => t
                        .Field(f => f.ProductStatuses.FirstOrDefault().Status)
                        .Terms(new TermsQueryField(new[] { FieldValue.String(req.ProductStatus) }))))
            );
    }

    /**
     * 版權
     */
    private Action<QueryDescriptor<ItemResourceElastic>> CopyrightCondition(ItemConditionReq req)
    {
        if (req.Copyright == null)
        {
            return null;
        }

        return BuildTermsCondition(req.Copyright, f => f.Copyright);
    }

    /**
     * 素養題
     */
    private Action<QueryDescriptor<ItemResourceElastic>> IsLiteracyCondition(ItemConditionReq req)
    {
        if (req.IsLiteracy == null)
        {
            return null;
        }

        return BuildTermBoolCondition(req.IsLiteracy, f => f.IsLiteracy);
    }

    /**
     * 編輯備註
     */
    private Action<QueryDescriptor<ItemResourceElastic>> EditorRemarksCondition(ItemConditionReq req)
    {
        if (req.EditorRemarks == null || !req.EditorRemarks.Any())
        {
            return null;
        }

        Action<QueryDescriptor<ItemResourceElastic>>[] conditions =
            new Action<QueryDescriptor<ItemResourceElastic>>[req.EditorRemarks.Count];
        var i = 0;
        foreach (var text in req.EditorRemarks)
        {
            conditions[i] = m => m
                .MatchPhrase(ma => ma
                    .Field(f => f.EditorRemark)
                    .Query(text)
                );
            i++;
        }

        // use nested should(OR) condition
        return m => m
            .Bool(b => b
                .Should(conditions)
                .MinimumShouldMatch(1));
    }

    /**
     * 編輯備註-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeEditorRemarksCondition(ItemConditionReq req)
    {
        if (req.NeEditorRemarks == null || !req.NeEditorRemarks.Any())
        {
            return null;
        }

        Action<QueryDescriptor<ItemResourceElastic>>[] conditions =
            new Action<QueryDescriptor<ItemResourceElastic>>[req.NeEditorRemarks.Count];
        var i = 0;
        foreach (var text in req.NeEditorRemarks)
        {
            conditions[i] = m => m
                .MatchPhrase(ma => ma
                    .Field(f => f.EditorRemark)
                    .Query(text)
                );
            i++;
        }

        // use nested should(OR) condition
        return m => m
            .Bool(b => b
                .Should(conditions)
                .MinimumShouldMatch(1));
    }

    /**
     * 入庫檔名
     */
    private Action<QueryDescriptor<ItemResourceElastic>> FilenamesCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.FileNames, f => f.FileNames);
    }

    /**
     * 入庫檔名-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeFilenamesCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeFileNames, f => f.FileNames);
    }

    /**
     * 關鍵字(聯集)通用搜尋條件欄位: 題組題幹/解析/題幹/選項/答案
     */
    private Action<QueryDescriptor<ItemResourceElastic>>[] BuildSearchTextsBaseConditions(IEnumerable<string> texts)
    {
        Expression<Func<ItemResourceElastic, object>> pathSelector = f => f.Questions;

        return BuildMatchPhraseConditions(texts, f => f.Preamble)
            .Concat(BuildMatchPhraseConditions(texts, f => f.Solution))
            .Concat(BuildNestedMatchPhraseConditions(texts, pathSelector,
                f => f.Questions.FirstOrDefault().Stem))
            .Concat(BuildNestedMatchPhraseConditions(texts, pathSelector,
                f => f.Questions.FirstOrDefault().Options))
            .Concat(BuildNestedMatchPhraseConditions(texts, pathSelector,
                f => f.Questions.FirstOrDefault().AnswerKeywords))
            .Append(BuildNestedTermsCondition(texts, pathSelector,
                f => f.Questions.FirstOrDefault().Answers))
            .Append(BuildNestedTermsCondition(texts, pathSelector,
                f => f.Questions.FirstOrDefault().ProposeAnswers)).ToArray();
    }

    /**
     * 關鍵字(交集)通用搜尋條件欄位: 題組題幹/解析/題幹/選項/答案
     */
    private Action<QueryDescriptor<ItemResourceElastic>>[] BuildMustSearchTextsBaseConditions(IEnumerable<string> texts)
    {
        Expression<Func<ItemResourceElastic, object>> pathSelector = f => f.Questions;
        Expression<Func<ItemResourceElastic, object>> preambleSelector = f => f.Preamble;
        Expression<Func<ItemResourceElastic, object>> solutionSelector = f => f.Solution;
        Expression<Func<ItemResourceElastic, object>> stemSelector = f => f.Questions.FirstOrDefault().Stem;
        Expression<Func<ItemResourceElastic, object>> optionsSelector = f => f.Questions.FirstOrDefault().Options;
        Expression<Func<ItemResourceElastic, object>> answerKeywordsSelector = f => f.Questions.FirstOrDefault().AnswerKeywords;
        Expression<Func<ItemResourceElastic, object>> answersSelector = f => f.Questions.FirstOrDefault().Answers;
        Expression<Func<ItemResourceElastic, object>> proposeAnswersSelector = f => f.Questions.FirstOrDefault().ProposeAnswers;

        Action<QueryDescriptor<ItemResourceElastic>>[] conditions =
            new Action<QueryDescriptor<ItemResourceElastic>>[texts.Count()];
        int i = 0;
        foreach (var text in texts)
        {
            // at least one field must matches each one of the given conditions
            var preambleCondition = BuildMatchPhraseCondition(text, preambleSelector);
            var solutionCondition = BuildMatchPhraseCondition(text, solutionSelector);
            var stemCondition = BuildNestedMatchPhraseCondition(text, pathSelector, stemSelector);
            var optionsCondition = BuildNestedMatchPhraseCondition(text, pathSelector, optionsSelector);
            var answerKeywordsCondition = BuildNestedMatchPhraseCondition(text, pathSelector, answerKeywordsSelector);
            var answersCondition = BuildNestedTermsCondition(text, pathSelector, answersSelector);
            var proposeAnswersCondition = BuildNestedTermsCondition(text, pathSelector, proposeAnswersSelector);

            conditions[i] =
                m => m.Bool(b => b
                    .Should([preambleCondition, solutionCondition, stemCondition, optionsCondition, answerKeywordsCondition, answersCondition, proposeAnswersCondition])
                    .MinimumShouldMatch(1));
            i++;
        }

        return conditions;
    }

    /**
     * 關鍵字(聯集)
     */
    private Action<QueryDescriptor<ItemResourceElastic>> SearchTextsCondition(ItemConditionReq req)
    {
        if (req.SearchTexts == null || !req.SearchTexts.Any())
        {
            return null;
        }

        Action<QueryDescriptor<ItemResourceElastic>>[] conditions = BuildSearchTextsBaseConditions(req.SearchTexts);

        // use nested should(OR) condition
        return m => m
            .Bool(b => b
                .Should(conditions)
                .MinimumShouldMatch(1));
    }

    /**
     * 關鍵字(聯集)-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeSearchTextsCondition(ItemConditionReq req)
    {
        if (req.NeSearchTexts == null || !req.NeSearchTexts.Any())
        {
            return null;
        }

        Action<QueryDescriptor<ItemResourceElastic>>[] conditions = BuildSearchTextsBaseConditions(req.NeSearchTexts);

        // use nested should(OR) condition
        return m => m
            .Bool(b => b
                .Should(conditions)
                .MinimumShouldMatch(1));
    }

    /**
     * 關鍵字(交集)
     */
    private Action<QueryDescriptor<ItemResourceElastic>> MustSearchTextsCondition(ItemConditionReq req)
    {
        if (req.MustSearchTexts == null || !req.MustSearchTexts.Any())
        {
            return null;
        }

        Action<QueryDescriptor<ItemResourceElastic>>[] conditions =
            BuildMustSearchTextsBaseConditions(req.MustSearchTexts);

        // use nested must(AND) condition
        return m => m
            .Bool(b => b
                .Must(conditions));
    }

    /**
     * 關鍵字(交集)-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeMustSearchTextsCondition(ItemConditionReq req)
    {
        if (req.NeMustSearchTexts == null || !req.NeMustSearchTexts.Any())
        {
            return null;
        }

        Action<QueryDescriptor<ItemResourceElastic>>[] conditions =
            BuildMustSearchTextsBaseConditions(req.NeMustSearchTexts);

        // use nested must(AND) condition
        return m => m
            .Bool(b => b
                .Must(conditions));
    }

    /**
     * 答案格式使用方程式
     */
    private Action<QueryDescriptor<ItemResourceElastic>> HasLatexCondition(ItemConditionReq req)
    {
        if (req.HasLatex == null)
        {
            return null;
        }

        return m => m
            .Nested(n => n
                .Path(p => p.Questions)
                .Query(nq => nq
                    .Term(t => t
                        .Field(f => f.Questions.FirstOrDefault().LatexAnswers)
                        .Value(req.HasLatex.Value)))
            );
    }

    /**
     * 議題
     */
    private Action<QueryDescriptor<ItemResourceElastic>> TopicsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.Topics, f => f.Topic);
    }

    /**
     * 檔案id(import id)
     */
    private Action<QueryDescriptor<ItemResourceElastic>> ImportRecordIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.ImportRecordIds, f => f.ImportRecordIds);
    }

    /**
     * 檔案id(import id)-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeImportRecordIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeImportRecordIds, f => f.ImportRecordIds);
    }

    /**
     * 文件id(document id)
     */
    private Action<QueryDescriptor<ItemResourceElastic>> DocumentIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.DocumentIds, f => f.DocumentIds);
    }

    /**
     * 文件id(document id)-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeDocumentIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeDocumentIds, f => f.DocumentIds);
    }

    /**
     * 五欄資料夾id(document repository id)
     */
    private Action<QueryDescriptor<ItemResourceElastic>> DocumentRepositoryIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.DocumentRepositoryIds, f => f.DocumentRepositoryIds);
    }

    /**
     * 五欄資料夾id(document repository id)-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeDocumentRepositoryIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeDocumentRepositoryIds, f => f.DocumentRepositoryIds);
    }

    /**
     * 題目id
     */
    private Action<QueryDescriptor<ItemResourceElastic>> IdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.Ids, f => f.Id);
    }

    /**
     * 題目id-剔除
     */
    private Action<QueryDescriptor<ItemResourceElastic>> NeIdsCondition(ItemConditionReq req)
    {
        return BuildTermsCondition(req.NeIds, f => f.Id);
    }
}