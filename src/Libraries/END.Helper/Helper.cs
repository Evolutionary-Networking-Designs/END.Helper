using System.Diagnostics;
using System.Web;
using END.Config;
using Cfg = END.Config;

// ReSharper disable ConvertIfStatementToReturnStatement

#if WINDOWS7_0_OR_GREATER
using System.Security.AccessControl;
using System.Security.Principal;
#endif

namespace END.Helper;

public static class Helper
{
    
#region "Folder Access"

    /// <summary>
    /// Check if App can Access Folder
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns>returns true if write access is allowed.</returns>
    public static bool HasWriteAccessToFolder(string folderPath)
    {
        
#if WINDOWS7_0_OR_GREATER
        return HasWriteAccessToFolderWindows(folderPath);
#else
        return true;
#endif
        
    }


#if WINDOWS7_0_OR_GREATER
    private static bool HasWriteAccessToFolderWindows(string folderPath)
    {
        var hasAccess = false;

        DirectoryInfo di = new DirectoryInfo(folderPath);
        DirectorySecurity acl;

        try
        {
            acl = di.GetAccessControl(AccessControlSections.Access);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        AuthorizationRuleCollection rules = acl.GetAccessRules(true, true, typeof(NTAccount));

        var user = WindowsIdentity.GetCurrent().User;
        if (user == null) return false;
        var userName = user.Translate(typeof(NTAccount));
        if (userName == null) return false;
        IdentityReference accountName = userName;

        //Go through the rules returned from the DirectorySecurity
        foreach (AuthorizationRule rule in rules)
        {
            //If we find one that matches the identity we are looking for
            if (rule.IdentityReference.Value == accountName.Value)
            {
                var filesystemAccessRule = (FileSystemAccessRule)rule;

                //Cast to a FileSystemAccessRule to check for access rights
                if ((filesystemAccessRule.FileSystemRights & FileSystemRights.WriteData) > 0 && filesystemAccessRule.AccessControlType != AccessControlType.Deny)
                {
                    return true;
                }
                else
                {
                    hasAccess = false;
                    break;
                }
            }
        }

        return hasAccess;
    }
#endif

    #endregion

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
        var config = Cfg.Config.LoadConfig();

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
        var config = Cfg.Config.LoadConfig();
        if (string.IsNullOrEmpty(key)) return string.Empty;
        return !config.Settings.TryGetValue(key, out var byKey) ? string.Empty : byKey;
    }
    
    [DebuggerStepThrough]
    public static string GetDataSource( 
        string env = "", 
        string service = ""
    )
    {
        var config = Cfg.Config.LoadConfig();
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
        var config = Cfg.Config.LoadConfig();
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

    [DebuggerStepThrough]
    public static string? GetRequest(string param)
    {
        var request = HttpContext.Current?.Request;
        if (request == null) return null;
        if (request.QueryString[param] != null) return request.QueryString[param];
        if (request.Form[param] != null) return request.Form[param];
        if (request.Cookies[param] != null)
        {
            var cookie = request.Cookies[param];
            return Convert.ToString(cookie?.Value);
        }
        if (request.ServerVariables[param] != null) return request.ServerVariables[param];
        return null;
    }

    [DebuggerStepThrough]
    public static bool IsLocalUri(string url)
    {
        var uri = new Uri(url);
        var ctx = HttpContext.Current;
        if (ctx == null) return false;
        if (uri.Host.Equals(ctx.Request.Url.Host) && uri.Port.Equals(ctx.Request.Url.Port))
            return true;
        return false;
    }

    /// <summary>
    /// Generate repeated string
    /// </summary>
    /// <param name="inString">String to repeat</param>
    /// <param name="inCount">Number of times to repeat</param>
    /// <returns></returns>
    public static string RepeatString(string inString, int inCount)
    {
        var result = "";
        for (var count = 0; count < inCount; count++)
        {
            result += inString;
        }
        return result;
    }
}