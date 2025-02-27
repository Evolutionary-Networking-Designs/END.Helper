using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Diagnostics;
using System.Web;

// ReSharper disable once CheckNamespace
namespace END.Helper;

public static class Config
{
    [DebuggerStepThrough]
    public static string GetAppPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string appPath;

        if (File.Exists(Path.Combine(baseDir, "web.config")))
        {
            var appDomain = HttpRuntime.AppDomainAppVirtualPath;
            if (HttpContext.Current == null) return string.Empty;
            appPath = HttpContext.Current.Server.MapPath(appDomain);
        }
        else
        {
            appPath = baseDir;
        }

        return appPath;
    }

    /// <summary>
    /// [DebuggerStepThrough]
    /// </summary>
    /// <returns></returns>
    public static AppSettings LoadConfig()
    {
        var appPath = GetAppPath();
        var settingsFile = Path.Combine(appPath, "appsettings.json");
        
        var config = new ConfigurationBuilder()
            .AddJsonFile(settingsFile, true, true)
            .Build();

        var appSettings = config.Get<AppSettings>() ?? new AppSettings();
        Dictionary<string,string> connStr = new();
        Dictionary<string, string> dataSource = new();
        var env = new EnvironmentSettings();

        var dirty = ValidateConfig(ref appSettings, ref connStr, ref dataSource);

        if (dirty)
        {
            appSettings.ConnectionStrings = connStr;
            appSettings.DataSource = dataSource;
            SaveConfig(appSettings);
        }
        appSettings.CipherKey = env.CipherKey;

        return appSettings;
    }

    public static AppSettings DecodeConfig()
    {
        var appPath = GetAppPath();
        var settingsFile = Path.Combine(appPath, "appsettings.json");
        
        var config = new ConfigurationBuilder()
            .AddJsonFile(settingsFile, true, true)
            .Build();

        var appSettings = config.Get<AppSettings>() ?? new AppSettings();
        Dictionary<string, string> connStr = new();
        Dictionary<string, string> dataSource = new();
        var env = new EnvironmentSettings();

        var dirty = ValidateConfig(ref appSettings, ref connStr, ref dataSource, true);

        if (dirty)
        {
            appSettings.ConnectionStrings = connStr;
            appSettings.DataSource = dataSource;
            SaveConfig(appSettings);
        }
        appSettings.CipherKey = env.CipherKey;

        return appSettings;
    }

    private static bool ValidateConfig(
        ref AppSettings settings, 
        ref Dictionary<string,string> connStr, 
        ref Dictionary<string,string> dataSource,
        bool decode = false
    )
    {
        if (settings.ConnectionStrings.Count == 0)
            return false;

        return decode ? DecodeConfig(ref settings, ref connStr, ref dataSource) : EncodeConfig(ref settings, ref connStr, ref dataSource);
    }

    private static bool EncodeConfig(
        ref AppSettings settings,
        ref Dictionary<string, string> connStr,
        ref Dictionary<string, string> dataSource)
    {
        var dirty = false;
        var cu = new CryptoUtil();

        // Ensure that connection strings are encrypted.
        foreach (var conn in settings.ConnectionStrings)
        {
            if (conn.Value.Contains("CipherText:")) continue;
            var encrypt = cu.EncryptValue(conn.Value);

            connStr.Add(conn.Key, encrypt);
            dirty = true;
        }

        foreach (var conn in settings.DataSource)
        {
            if (conn.Value.Contains("CipherText:")) continue;
            var encrypt = cu.EncryptValue(conn.Value);

            dataSource.Add(conn.Key, encrypt);
            dirty = true;
        }

        return dirty;
    }

    private static bool DecodeConfig(
        ref AppSettings settings, 
        ref Dictionary<string, string> connStr, 
        ref Dictionary<string, string> dataSource)
    {
        var dirty = false;
        var cu = new CryptoUtil();

        connStr.Clear();
        dataSource.Clear();

        // Ensure that connection strings are encrypted.
        foreach (var conn in settings.ConnectionStrings)
        {
            if (!conn.Value.Contains("CipherText:")) continue;
            var decrypt = cu.DecryptValue(conn.Value);

            connStr.Add(conn.Key, decrypt);
            dirty = true;
        }

        foreach (var conn in settings.DataSource)
        {
            if (!conn.Value.Contains("CipherText:")) continue;
            var decrypt = cu.DecryptValue(conn.Value);

            dataSource.Add(conn.Key, decrypt);
            dirty = true;
        }

        return dirty;

    }

    private static void SaveConfig(AppSettings? appSettings)
    {
        var appPath = Config.GetAppPath();
        var settingsFile = Path.Combine(appPath, "appsettings.json");
        
        if (File.Exists(settingsFile))
            File.Delete(settingsFile);

        var config = appSettings ?? new AppSettings();
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var jsonString = JsonSerializer.Serialize(config, options);
        File.WriteAllText(settingsFile, jsonString, System.Text.Encoding.UTF8);
    }

}