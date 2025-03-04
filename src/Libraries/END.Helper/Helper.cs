using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Web;

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
}