using System.Diagnostics;
using System.Web;

namespace END.Config;

public class Helper
{
    public static AppSettings AppSettings()
    {
        var config = Config.LoadConfig();
        return config;
    }
    
    [DebuggerStepThrough]
    public static string? GetAppPath()
    {
        var appDomain = HttpRuntime.AppDomainAppVirtualPath;
        if (HttpContext.Current == null) return null;
        var appPath = HttpContext.Current.Server.MapPath(appDomain);
        return appPath;
    }

    [DebuggerStepThrough]
    private static AppSetting GetAppEnumByString(string key)
    {
        return key switch
        {
            "AppName" => AppSetting.AppName,
            "ServiceName" => AppSetting.ServiceName,
            "Env" => AppSetting.Environment,
            "ConnStr" => AppSetting.ConnectionString,
            "DataSource" => AppSetting.DataSource,
            _ => AppSetting.Settings
        };
    }
    
    /// <summary>
    /// .NET Version independent function for returning AppSetting.
    /// </summary>
    /// <param name="key">Name of setting to retrieve.</param>
    /// <returns>App Setting</returns>
    [DebuggerStepThrough]
    public static string GetAppSetting(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;

        var setting = GetAppSetting(GetAppEnumByString(key));

        if (string.IsNullOrEmpty(setting))
            setting = GetAppSettingByKey(key);

        return setting;
    }
    
    /// <summary>
    /// Retrieve AppSetting based upon value selected.
    /// </summary>
    /// <param name="setting">The Setting to Retrieve.</param>
    /// <returns>Application Setting selected by Enum</returns>
    [DebuggerStepThrough]
    public static string GetAppSetting(AppSetting setting)
    {
        var config = Config.LoadConfig();

        return setting switch
        {
            AppSetting.Environment => config.Environment,
            AppSetting.ServiceName => config.ServiceName,
            AppSetting.AppName => config.AppName,
            AppSetting.DataSource => GetDataSource(),
            AppSetting.ConnectionString => GetConfigConnStr(),
            _ => string.Empty
        };
    }
    
    [DebuggerStepThrough]
    public static string GetAppSettingByKey(string key)
    {
        var config = Config.LoadConfig();
        if (string.IsNullOrEmpty(key)) return string.Empty;
        return !config.Settings.TryGetValue(key, out var byKey) ? string.Empty : byKey;
    }
    
    [DebuggerStepThrough]
    public static string GetDataSource( 
        string env = "", 
        string service = ""
    )
    {
        var config = Config.LoadConfig();
        var envName = (string.IsNullOrEmpty(env)) ? config.Environment : env;
        var serviceName = (string.IsNullOrEmpty(service)) ? config.ServiceName.ToUpper() : service.ToUpper();
        var listDataSource = config.DataSource;

        var keyName = envName + "-" + serviceName;

        return (listDataSource.TryGetValue(keyName, out var source)) ? source : string.Empty;
    }

    [DebuggerStepThrough]
    public static string GetConfigConnStr(
        string env = "", 
        string service = "", 
        string app = ""
    )
    {
        var config = Config.LoadConfig();
        var envName = (string.IsNullOrEmpty(env)) ? config.Environment : env;
        var serviceName = (string.IsNullOrEmpty(service)) ? config.ServiceName.ToUpper() : service.ToUpper();
        var appName = (string.IsNullOrEmpty(app)) ? config.AppName.ToUpper() : app.ToUpper();

        var listConnStr = config.ConnectionStrings;

        var defaultName = envName + "-" + serviceName;
        var keyName = envName + "-" + serviceName + "-" + appName;

        if (listConnStr.TryGetValue(keyName, out var str))
            return str;

        return listConnStr.ContainsKey(defaultName) ? listConnStr[keyName] : string.Empty;
    }
}