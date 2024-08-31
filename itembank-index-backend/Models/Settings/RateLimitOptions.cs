namespace itembank_index_backend.Models.Settings;

public class RateLimitOptions
{
    public const string RateLimitSection = "RateLimitOptions";
    public int PermitLimit { get; set; }
    public int QueueLimit { get; set; }
}