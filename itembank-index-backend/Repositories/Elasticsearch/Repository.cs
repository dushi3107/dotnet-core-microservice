using System.Linq.Expressions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using itembank_index_backend.Models.Settings;
using itembank_index_backend.Models.Resources.Elastic;
using Microsoft.Extensions.Options;

namespace itembank_index_backend.Repositories.Elasticsearch;

public class Repository<T> : IRepository<T>
{
    protected readonly ElasticsearchClient _elasticsearchClient;
    protected readonly string _index;
    protected readonly AppSettings _appSettings;
    protected readonly string _apiKeyId;
    protected readonly string _apiKey;
    protected readonly ILogger<Repository<T>> _logger;

    // default constructor
    public Repository(IOptions<AppSettings> appSettings, ILogger<Repository<T>> logger)
    {
        _appSettings = appSettings.Value;
        _logger = logger;

        _apiKeyId = Environment.GetEnvironmentVariable(EnvironmentVariables.ELASTICSEARCH_API_KEY_ID) ??
                    _appSettings.ElasticsearchApiKeyId;
        _apiKey = Environment.GetEnvironmentVariable(EnvironmentVariables.ELASTICSEARCH_API_KEY) ??
                  _appSettings.ElasticsearchApiKey;

        _elasticsearchClient =
            new ElasticsearchClient(
                new ElasticsearchClientSettings(new Uri(_appSettings.ElasticsearchUrl)).Authentication(
                    new ApiKey(_apiKey)));
        _index = _appSettings.ElasticsearchConditionIndex;
    }

    public async Task<int> PingAsync()
    {
        var response = await _elasticsearchClient.PingAsync();
        if (!response.IsValidResponse)
        {
            _logger.LogError(response.ToString());
        }

        return response.ApiCallDetails.HttpStatusCode.Value;
    }

    public async Task<Result> CreateAsync(T item)
    {
        try
        {
            var response = await _elasticsearchClient.IndexAsync<T>(item, index: _index);

            if (response == null)
            {
                throw new Exception("response is null");
            }

            if (!response.IsValidResponse)
            {
                throw new Exception(response.ToString());
            }

            return response.Result;
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return Result.NoOp;
        }
    }

    public async Task<T> ReadAsync(string id)
    {
        try
        {
            var response = await _elasticsearchClient.GetAsync<T>(_index, id);

            if (response == null || response.Source == null)
            {
                throw new Exception("response or source is null");
            }

            if (!response.IsValidResponse)
            {
                throw new Exception(response.ToString());
            }

            return response.Source;
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return default(T);
        }
    }

    public async Task<Result> UpdateAsync(T item)
    {
        try
        {
            var response = await _elasticsearchClient.UpdateAsync<T, T>(_index, u => u.Doc(item));

            if (response == null)
            {
                throw new Exception("response is null");
            }

            if (!response.IsValidResponse)
            {
                throw new Exception(response.ToString());
            }

            return response.Result;
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return Result.NoOp;
        }
    }

    public async Task<Result> DeleteAsync(string id)
    {
        try
        {
            var response = await _elasticsearchClient.DeleteAsync<T>(_index, id);

            if (response == null)
            {
                throw new Exception("response is null");
            }

            if (!response.IsValidResponse)
            {
                throw new Exception(response.ToString());
            }

            return response.Result;
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return Result.NoOp;
        }
    }

    public async Task<T> SearchAsync(T item)
    {
        try
        {
            var response = await _elasticsearchClient.SearchAsync<T>(s => s
                .Index(_index)
                .Query(q => q
                    .Match(m => m
                        .Field(
                            "<FIELD>"
                        )
                    )
                )
            );

            if (response == null)
            {
                throw new Exception("response is null");
            }

            if (!response.IsValidResponse)
            {
                throw new Exception(response.ToString());
            }

            return response.Documents.FirstOrDefault();
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return default(T);
        }
    }

    /**
     * BuildTermsCondition builds terms query condition, used to compare a source array to a target array or a source array to a target field
     */
    public Action<QueryDescriptor<T>> BuildTermsCondition(IEnumerable<string> terms,
        Expression<Func<T, object>> fieldSelector)
    {
        if (terms == null || !terms.Any())
        {
            return null;
        }

        return m => m
            .Terms(ma => ma
                .Field(fieldSelector)
                .Terms(new TermsQueryField(terms.Select(val => FieldValue.String(val)).ToArray()))
            );
    }

    public Action<QueryDescriptor<T>> BuildNestedTermsCondition(IEnumerable<string> terms,
        Expression<Func<T, object>> nestedPathSelector,
        Expression<Func<T, object>> fieldSelector)
    {
        if (terms == null || !terms.Any())
        {
            return null;
        }

        return m => m
            .Nested(n => n
                .Path(nestedPathSelector)
                .Query(nq => nq
                    .Terms(ma => ma
                        .Field(fieldSelector)
                        .Terms(new TermsQueryField(terms.Select(val => FieldValue.String(val)).ToArray()))
                    )
                )
            );
    }

    public Action<QueryDescriptor<T>> BuildTermsCondition(string term,
        Expression<Func<T, object>> fieldSelector)
    {
        if (string.IsNullOrEmpty(term))
        {
            return null;
        }

        return m => m
            .Terms(ma => ma
                .Field(fieldSelector)
                .Terms(new TermsQueryField(new[] { FieldValue.String(term) }))
            );
    }

    public Action<QueryDescriptor<T>> BuildNestedTermsCondition(string term,
        Expression<Func<T, object>> nestedPathSelector,
        Expression<Func<T, object>> fieldSelector)
    {
        if (string.IsNullOrEmpty(term))
        {
            return null;
        }

        return m => m
            .Nested(n => n
                .Path(nestedPathSelector)
                .Query(nq => nq
                    .Terms(ma => ma
                        .Field(fieldSelector)
                        .Terms(new TermsQueryField(new[] { FieldValue.String(term) }))
                    )
                )
            );
    }

    /**
     * BuildMatchPhraseCondition builds match_phrase(full word comparison) query condition, used to compare term to term
     */
    public Action<QueryDescriptor<T>> BuildMatchPhraseCondition(string item,
        Expression<Func<T, object>> fieldSelector)
    {
        if (string.IsNullOrEmpty(item))
        {
            return null;
        }

        return m => m
            .MatchPhrase(ma => ma
                .Field(fieldSelector)
                .Query(item)
            );
    }
    
    public Action<QueryDescriptor<T>> BuildNestedMatchPhraseCondition(string item,
        Expression<Func<T, object>> nestedPathSelector,
        Expression<Func<T, object>> fieldSelector)
    {
        if (string.IsNullOrEmpty(item))
        {
            return null;
        }

        return m => m
            .Nested(n => n
                .Path(nestedPathSelector)
                .Query(nq => nq
                    .MatchPhrase(ma => ma
                        .Field(fieldSelector)
                        .Query(item)))
            );;
    }

    public Action<QueryDescriptor<T>>[] BuildMatchPhraseConditions(IEnumerable<string> items,
        Expression<Func<T, object>> fieldSelector)
    {
        if (items == null || !items.Any())
        {
            return [];
        }

        Action<QueryDescriptor<T>>[] conditions =
            new Action<QueryDescriptor<T>>[items.Count()];
        var i = 0;
        foreach (var item in items)
        {
            conditions[i] = m => m
                .MatchPhrase(ma => ma
                    .Field(fieldSelector)
                    .Query(item)
                );
            i++;
        }

        return conditions;
    }

    public Action<QueryDescriptor<T>>[] BuildNestedMatchPhraseConditions(IEnumerable<string> items,
        Expression<Func<T, object>> nestedPathSelector,
        Expression<Func<T, object>> fieldSelector)
    {
        if (items == null || !items.Any())
        {
            return [];
        }

        Action<QueryDescriptor<T>>[] conditions =
            new Action<QueryDescriptor<T>>[items.Count()];
        var i = 0;
        foreach (var item in items)
        {
            conditions[i] = m => m
                .Nested(n => n
                    .Path(nestedPathSelector)
                    .Query(nq => nq
                        .MatchPhrase(ma => ma
                            .Field(fieldSelector)
                            .Query(item)))
                );
            i++;

            // Somehow Regexp can not work
            // conditions[i] = m => m
            //         .Nested(n => n
            //             .Path(pathSelector)
            //             .Query(q => q
            //                 .Regexp(r => r
            //                     .Field(fieldSelector)
            //                     .Value($".*{item}.*")))
            //         );
        }

        return conditions;
    }

    /**
     * BuildMatchCondition builds match query condition, used to compare term to term
     */
    public Action<QueryDescriptor<T>> BuildMatchCondition(string item,
        Expression<Func<T, object>> fieldSelector)
    {
        if (string.IsNullOrEmpty(item))
        {
            return null;
        }

        return m => m
            .Match(ma => ma
                .Field(fieldSelector)
                .Query(item)
            );
    }

    public Action<QueryDescriptor<T>> BuildTermBoolCondition(bool? item,
        Expression<Func<T, object>> fieldSelector)
    {
        if (item == null)
        {
            return null;
        }

        return m => m
            .Term(t => t
                .Field(fieldSelector)
                .Value(item.Value)
            );
    }

    public Action<QueryDescriptor<T>> BuildFieldExistsAndNonEmptyCondition(bool? exists,
        Expression<Func<T, object>> fieldSelector)
    {
        if (exists == null)
        {
            return null;
        }

        Action<QueryDescriptor<T>> conditionExists = m => m
            .Exists(e => e
                .Field(fieldSelector)
            );

        Action<QueryDescriptor<T>> conditionNonEmptyString = m => m
            .Wildcard(w => w.Field(fieldSelector).Value("*"));

        if (exists.Value)
        {
            return m => m.Bool(b => b
                .Must(conditionExists)
                .Must(conditionNonEmptyString)
            );
        }

        return m => m.Bool(b => b
            .Should(s => s
                .Bool(bb => bb.MustNot(conditionExists))
                .Bool(bb => bb.MustNot(conditionNonEmptyString)))
            .MinimumShouldMatch(1)
        );
    }
}