namespace Active.Toolbox.Core.Plugins.Shelly;

public class ShellyPluginConfig
{
    public ShellyPluginConfig(string url, string authKey)
    {
        Url = url;
        AuthKey = authKey;
    }

    public string Url { get; set; } = string.Empty;
    public string AuthKey { get; set; } = string.Empty;
}