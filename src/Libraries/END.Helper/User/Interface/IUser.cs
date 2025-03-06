using System.Collections;
using System.Security;

namespace END.Helper.User
{
    internal interface IUser
    {
        string UserId { get; }
        SecureString? Credential { get; }
        int UserSeq { get; }
        string UserKey { get; }
        Dictionary<string, string> UserInfo { get; }
        Hashtable? UserRoles { get; }
        bool HasRole(string inRole);
        bool HasRoleInContext(string inRole, Hashtable inContext);
        Hashtable? GetUserConditions();
        Hashtable? GetUserConditionsForRole(string inRole);
        Hashtable? GetUserValues(string inCondition);
    }
}