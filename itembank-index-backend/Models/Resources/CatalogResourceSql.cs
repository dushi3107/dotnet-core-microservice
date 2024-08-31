namespace itembank_index_backend.Models.Resources.Sql;

public class CatalogResourceSql
{
}

public class CatalogDetailResourceSql : CatalogResourceSql
{
    public string? ItemId { get; set; }
    public string? Id { get; set; }
    public string? GroupCode { get; set; }
    public string? Name { get; set; }
    public string? Year { get; set; }
}