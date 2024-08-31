namespace itembank_index_backend.Models.Definition;

public static class ItemDefinition
{
    public const string NullBodyOfKnowledgeCode = "無學程";
    public const string OnShelfProductStatus = "on_shelf";
    public const string OffShelfProductStatus = "off_shelf";
    public const string OnlineReadinessReady = "ready";

    public static readonly Dictionary<string, string> CopyrightMap = new()
    {
        { "無版權", "0" },
        { "有版權限制", "1" },
        { "版權是翰教科", "2" },
        { "待談版權", "3" }
    };
}