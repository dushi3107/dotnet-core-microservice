using System.Linq.Expressions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace itembank_index_backend.Repositories.Elasticsearch;

public interface IRepository<T>
{
    Task<int> PingAsync();
    Task<Result> CreateAsync(T item);
    Task<T> ReadAsync(string id);
    Task<Result> UpdateAsync(T item);
    Task<Result> DeleteAsync(string id);
    Task<T> SearchAsync(T item);

    Action<QueryDescriptor<T>> BuildTermsCondition(IEnumerable<string> terms,
        Expression<Func<T, object>> fieldSelector);

    Action<QueryDescriptor<T>> BuildNestedTermsCondition(IEnumerable<string> terms,
        Expression<Func<T, object>> nestedPathSelector,
        Expression<Func<T, object>> fieldSelector);

    Action<QueryDescriptor<T>> BuildTermsCondition(string term,
        Expression<Func<T, object>> fieldSelector);
    
    Action<QueryDescriptor<T>> BuildNestedTermsCondition(string term,
        Expression<Func<T, object>> nestedPathSelector,
        Expression<Func<T, object>> fieldSelector);

    Action<QueryDescriptor<T>> BuildMatchPhraseCondition(string item,
        Expression<Func<T, object>> fieldSelector);
    
    Action<QueryDescriptor<T>> BuildNestedMatchPhraseCondition(string item,
        Expression<Func<T, object>> nestedPathSelector,
        Expression<Func<T, object>> fieldSelector);

    Action<QueryDescriptor<T>>[] BuildMatchPhraseConditions(IEnumerable<string> items,
        Expression<Func<T, object>> fieldSelector);

    Action<QueryDescriptor<T>>[] BuildNestedMatchPhraseConditions(IEnumerable<string> items,
        Expression<Func<T, object>> nestedPathSelector,
        Expression<Func<T, object>> fieldSelector);

    Action<QueryDescriptor<T>> BuildMatchCondition(string item,
        Expression<Func<T, object>> fieldSelector);

    Action<QueryDescriptor<T>> BuildTermBoolCondition(bool? item,
        Expression<Func<T, object>> fieldSelector);

    Action<QueryDescriptor<T>> BuildFieldExistsAndNonEmptyCondition(bool? exists,
        Expression<Func<T, object>> fieldSelector);
}