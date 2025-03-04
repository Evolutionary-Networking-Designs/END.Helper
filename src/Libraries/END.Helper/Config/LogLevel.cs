using Newtonsoft.Json;

namespace END.Helper;

public class LogLevel
{
    [JsonProperty("Microsoft.AspNetCore")]
    public string MicrosoftAspNetCore { get; set; }

    [JsonProperty("Microsoft.Hosting.Lifetime")]
    public string MicrosoftHostingLifetime { get; set; }
    public string Application { get; set; }
        
    public LogLevel()
    {
        MicrosoftAspNetCore = "Warning";
        MicrosoftHostingLifetime = "Information";
        Application = "Warning";
    }

}