using System.Data;

namespace END.Database.Interface;

#if NET8_0_OR_GREATER

public interface IUserInfo
{
    public string GetEncryptKey();
    public string GetCommonCred();
    
    protected DataTable GetUniqueCertList(bool showExpired);
    protected DataTable GetUserCertList(bool showExpired);
    protected DataTable GetUserList();
    protected DataTable GetUsersByApp(string appName);
    protected DataTable GetRoleListByApp(string appName);

    public DataTable GetCondByUser(string appName, string userId);

    protected Dictionary<string, DataTable> GetUserData(string appName);

}

#else

public interface IUserInfo
{
    public string GetEncryptKey();
    public string GetCommonCred();

    public DataTable GetCondByUser(string appName, string userId);
}

#endif
