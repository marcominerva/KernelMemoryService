namespace KernelMemoryService.Services;

public class AppSettings
{
    public int MessageLimit { get; set; }

    public TimeSpan MessageExpiration { get; set; }
}
