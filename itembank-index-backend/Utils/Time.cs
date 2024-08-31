namespace itembank_index_backend.Utils;

public class Time
{
    public static string GetNowDateTimeString()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    }
}