using System.Text.Json;

// ReSharper disable once CheckNamespace
namespace END.Helper;

public class EnvironmentSettings
{
    public string CipherKey { get; set; } = string.Empty;

    public EnvironmentSettings()
    {
        if (string.IsNullOrEmpty(this.CipherKey))
        {
            var appPath = Config.GetAppPath();
            var envFile = Path.Combine(appPath, "bin", Const.EnvFile);
            
            if (File.Exists(envFile)) return;

            CipherKey = Aes256Cipher.GenerateNewKey();

            if (Helper.HasWriteAccessToFolder(appPath))
                WriteConfigFile(envFile);
        }
    }

    private void WriteConfigFile(string envFile)
    {
        
        var options = new JsonSerializerOptions();
        options.WriteIndented = true;
        var jsonString = JsonSerializer.Serialize(this, options);
        File.WriteAllText(envFile, jsonString, System.Text.Encoding.UTF8);
    }
}