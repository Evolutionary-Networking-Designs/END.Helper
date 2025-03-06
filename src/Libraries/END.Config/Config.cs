using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Diagnostics;
using System.Web;
using END.Config.Crypto;

#if WINDOWS7_0_OR_GREATER
using Conf = System.Configuration;
using System.Collections.Specialized;
using END.Config.Util;
#endif

// ReSharper disable ReplaceSubstringWithRangeIndexer
// ReSharper disable once CheckNamespace

namespace END.Config;

public static class Config
{
    [DebuggerStepThrough]
    public static string GetAppPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string appPath;

        if (File.Exists(Path.Combine(baseDir, Const.WebConfigFile)))
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

    private static void LoadWebConfigSettings(ref AppSettings settings)
    {
        var appPath = GetAppPath();
        var settingsFile = Path.Combine(appPath, Const.WebConfigFile);

        if (!File.Exists(settingsFile)) return;
        
        #if WINDOWS7_0_OR_GREATER
        
        if (!System.Configuration.ConfigurationManager.AppSettings.HasKeys())
            return;

        NameValueCollection webSettings = Conf.ConfigurationManager.AppSettings;
        Conf.ConnectionStringSettingsCollection conSettings = Conf.ConfigurationManager.ConnectionStrings;

        foreach (string key in webSettings)
        {
            string? value = webSettings.Get(key);
            if (value == null) continue;
            if (!settings.Settings.ContainsKey(key))
                settings.Settings.Add(key, value);
        }

        if (settings.Settings.ContainsKey("AppName") && string.IsNullOrEmpty(settings.AppName))
            settings.AppName = settings.Settings["AppName"];

        var builder = new ConnStrBuilder();

        foreach (Conf.ConnectionStringSettings conn in conSettings)
        {
            var provider = conn.ProviderName;
            var connStr = conn.ConnectionString;

            try
            {
                builder.ConnectionString = connStr;
            } catch
            {
                continue;
            }

            builder.ConnectionString = connStr;
            var dataSourceName = settings.Environment + "-" + settings.ServiceName;

            if (!settings.DataSource.ContainsKey(dataSourceName))
            {
                var dataSource = builder.DataSource;
                var host = dataSource.Substring(dataSource.IndexOf("HOST", StringComparison.Ordinal));
                host = host.Substring(0, host.IndexOf(')')).Split('=')[1];
                var port = dataSource.Substring(dataSource.IndexOf("PORT", StringComparison.Ordinal));
                port = port.Substring(0, port.IndexOf(')')).Split('=')[1];
                dataSource = string.Concat(host, ":", port, "/", settings.ServiceName);

                settings.DataSource.Add(dataSourceName, dataSource);
            }

            builder.DataSource = dataSourceName;
            connStr = builder.ConnectionString;
            var key = dataSourceName + "-" + conn.Name.ToUpper();

            settings.ConnectionStrings.TryAdd(key, connStr);
        }
        
        #else

        LoadXmlConfig(ref settings);

        #endif
    }

    private static void LoadXmlConfig(ref AppSettings settings)
    {
    }

    /// <summary>
    /// [DebuggerStepThrough]
    /// </summary>
    /// <returns></returns>
    public static AppSettings LoadConfig(bool decode = false)
    {
        var appPath = GetAppPath();
        var settingsFile = Path.Combine(appPath, "bin", Const.AppSettingsFile);
        
        var config = new ConfigurationBuilder()
            .AddJsonFile(settingsFile, true, true)
            .Build();

        var appSettings = config.Get<AppSettings>() ?? new AppSettings();
        Dictionary<string,string> connStr = new();
        Dictionary<string, string> dataSource = new();
        var env = new EnvironmentSettings();
        
        LoadWebConfigSettings(ref appSettings);
        
        var dirty = ValidateConfig(ref appSettings, ref connStr, ref dataSource, decode);

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
        return LoadConfig(true);
    }

    private static bool ValidateConfig(
        ref AppSettings settings, 
        ref Dictionary<string,string> connStr, 
        ref Dictionary<string,string> dataSource,
        bool decode = false
    )
    {
        bool dirty;
        
        if (settings.ConnectionStrings.Count == 0)
            return false;

        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (decode)
            dirty = DecodeConfig(ref settings, ref connStr, ref dataSource); 
        else 
            dirty = EncodeConfig(ref settings, ref connStr, ref dataSource);

        return dirty;
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
        var settingsFile = Path.Combine(appPath, "bin", Const.AppSettingsFile);
        
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