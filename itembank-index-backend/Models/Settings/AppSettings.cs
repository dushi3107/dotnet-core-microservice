namespace itembank_index_backend.Models.Settings;

public class AppSettings
{
    public string ElasticsearchUrl { get; set; }
    public string ElasticsearchConditionIndex { get; set; }
    public string ElasticsearchRecordIndex { get; set; }
    public string ElasticsearchApiKeyId { get; set; }
    public string ElasticsearchApiKey { get; set; }
    public string ElasticsearchReservedWordSeasrchEnable { get; set; }
    public string MsSqlConnectionString { get; set; }
    public string S3Url { get; set; }
    public string ItembankApiUrl { get; set; }
    public string Environment { get; set; }
}

public static class AppSettingDefinition
{
    public static string Enabled = "Enabled";
}