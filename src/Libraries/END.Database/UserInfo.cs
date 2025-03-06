using System.Data;

namespace END.Database;

public static class UserInfo
{
    public static string GetCommonCred()
    {
        var ui = new User.UserInfo();
        return ui.GetCommonCred();
    }

    public static Dictionary<string, DataTable> GetUserInfo(string appName, string userId)
    {
        var ui = new User.UserInfo();
        return ui.GetUserInfo(appName, userId);
    }

    public static DataTable GetDataTable(Dictionary<string, DataTable> dtDict, UserTables tableEnum)
    {
        return User.UserInfo.GetDataTable(dtDict, tableEnum);
    }
}