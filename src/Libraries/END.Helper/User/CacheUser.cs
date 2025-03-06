using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using END.Embedded;
using Microsoft.Extensions.Configuration;
using DB = END.Database;
using Cfg = END.Config;
// ReSharper disable ReplaceSubstringWithRangeIndexer
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantEmptySwitchSection
// ReSharper disable CanSimplifyDictionaryLookupWithTryGetValue
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

// ReSharper disable ConvertToAutoProperty
#pragma warning disable CS8618, CS9264

namespace END.Helper.User
{
    [DataContract]
    public class CacheUser : IUser
    {
        #region "Vars / Const"

        private const string UserSeqField = "USER_SEQ";
        private const int MaxAllowedPortalTime = 5;
        private readonly string _userId;
        private readonly SecureString? _cred;
        private readonly int _userSeq;

        private readonly bool _isValid = true;
        private readonly Dictionary<string, string> _userDetail = new();
        private readonly List<Dictionary<string, string>> _roleInfo = new();
        private readonly List<Dictionary<string, string>> _condInfo = new();
        private readonly Hashtable _roles = new();

        #endregion

        #region "Properties"

        [JsonIgnore]
        public bool IsValid => _isValid;

        [DataMember]
        public string UserId => _userId;

        [JsonIgnore]
        public SecureString? Credential => _cred;

        [DataMember]
        public string UserKey => _userSeq.ToString();

        [JsonIgnore]
        public int UserSeq => _userSeq;

        [DataMember]
        public Dictionary<string, string> UserInfo => _userDetail;

        [DataMember] public Hashtable UserRoles => _roles;

        #endregion

        #region "Constructors"

        public CacheUser(string auth)
        {
            string userId;
            string cred;

            if (auth.IndexOf("CN=", StringComparison.Ordinal) > 0)
            {
                userId = GetUserFromCert(auth.Substring(3));
                cred = DB.UserInfo.GetCommonCred();
            }
            else
            {
                _isValid = false;
                return;
            }
            var config = Cfg.Config.LoadConfig();
            var appName = config.AppName;
            var userDict = DB.UserInfo.GetUserInfo(appName, userId);

            _userId = userId;
            _cred = ConvertToSecureString(cred);

            var dt = DB.UserInfo.GetDataTable(userDict, DB.UserTables.USER_DETAIL);
            var listDict = ToListDict(dt);
            _userDetail = listDict[0];
            dt = DB.UserInfo.GetDataTable(userDict, DB.UserTables.USER_ROLES);
            _roleInfo = ToListDict(dt);
            dt = DB.UserInfo.GetDataTable(userDict, DB.UserTables.USER_COND);
            _condInfo = ToListDict(dt);

            if (!int.TryParse(_userDetail[UserSeqField], out _userSeq))
                _userSeq = -1;

            _roles = GetUserRoles();
        }

        public CacheUser( IConfigurationRoot? config)
        {
            if (config == null) return;
            foreach(var entry in config.GetChildren())
            {
                switch (entry.Key)
                {
                    case "UserInfo":
                        foreach(var userDict in entry.GetChildren())
                        {
                            _userDetail.Add(userDict.Key, ToStr(userDict.Value));
                        }
                        break;
                    case "UserId":
                        _userId = ToStr(entry.Value);
                        break;
                    case "UserKey":
                        int.TryParse(ToStr(entry.Value), out _userSeq);
                        break;
                    case "UserRoles":
                        foreach(var sRole in entry.GetChildren())
                        {
                            var listValue = new List<string>();
                            var dictCond = new Dictionary<string, List<string>?>();
                            foreach (var sCond in sRole.GetChildren())
                            {
                                foreach(var sValue in sCond.GetChildren())
                                {
                                    listValue.Add(ToStr(sValue.Value));
                                }
                                if (listValue.Count > 0)
                                {
                                    dictCond.Add(sCond.Key, listValue);
                                }
                                else
                                {
                                    dictCond.Add(sCond.Key, null);
                                }
                            }

                            _roleInfo.Add(GetDict("ROLE", sRole.Key));
                            if (dictCond.Count == 0) continue;
                            {
                                _condInfo = GetListDictCond(sRole.Key, dictCond);
                            }

                            _roles = GetUserRoles();
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public CacheUser(string userId, string cred, Dictionary<string, DataTable> userDict)
        {
            _userId = userId;
            _cred = ConvertToSecureString(cred);

            var dt = DB.UserInfo.GetDataTable(userDict, DB.UserTables.USER_DETAIL);
            var listDict = ToListDict(dt);
            _userDetail = listDict[0];
            dt = DB.UserInfo.GetDataTable(userDict, DB.UserTables.USER_ROLES);
            _roleInfo = ToListDict(dt);
            dt = DB.UserInfo.GetDataTable(userDict, DB.UserTables.USER_COND);
            _condInfo = ToListDict(dt);

            if (!int.TryParse(_userDetail[UserSeqField], out _userSeq))
                _userSeq = -1;

            _roles = GetUserRoles();
        }

        #endregion

        #region "IUser Functions"

        public Hashtable GetUserConditions()
        {
            throw new NotImplementedException();
        }

        public Hashtable GetUserConditionsForRole(string inRole)
        {
            throw new NotImplementedException();
        }

        public Hashtable GetUserValues(string inCondition)
        {
            throw new NotImplementedException();
        }

        public bool HasRole(string inRole)
        {
            throw new NotImplementedException();
        }

        public bool HasRoleInContext(string inRole, Hashtable inContext)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region "Helper Functions"

        private string GetServerHhmm()
        {
            var query = Prop.GetProperty(@"user\get_server_hhmm.sql");
            return DB.Query.GetScalar(query);
        }

        private bool CheckTimeWindow(string ts)
        {
            var sCurrentTime = GetServerHhmm();
            var dtCurrentTime = DateTime.Parse(DateTime.Now.ToShortDateString() + " " + sCurrentTime.Insert(2, ":"));
            var dtNewTime = DateTime.Parse(DateTime.Now.ToShortDateString() + " " + ts.Insert(2, ":"));
            var elapsedSpan = (dtNewTime - dtCurrentTime);
            var iElapsedMinutes = (elapsedSpan.Hours * 60) + elapsedSpan.Minutes;
            var iElapsed = (iElapsedMinutes > 0 ? iElapsedMinutes : (-1 * iElapsedMinutes));

            return (iElapsed <= MaxAllowedPortalTime);
        }

        private string GetUserFromCert(string cert)
        {
            var userId = string.Empty;
            return userId;
        }

        private Hashtable GetUserRoles()
        {
            if (_roleInfo.Count == 0)
                return new Hashtable();
            if (_condInfo.Count == 0)
                return new Hashtable();

            var htRoles = new Hashtable();
            var htCond = GetUserCond();

            var listRoles = new List<string>();
            var listCond = new List<string>();
            var dictRoles = new Dictionary<string, List<string>>();

            var dt = ToDataTable(_condInfo);

            foreach(var entry in _roleInfo)
            {
                var role = entry["ROLE"];
                if (!listRoles.Contains(role))
                    listRoles.Add(role);
            }

            foreach(var role in listRoles)
            {
                var dv = dt.DefaultView;
                dv.RowFilter = $"ROLE = '{role}'";
                if (dv.Count == 0) continue;
                listCond.Clear();
                for (var i = 0; i < dv.Count; i++)
                {
                    var rw = dv[i];
                    var cond = (string?)rw["COND_TITLE"];
                    if (cond == null) continue;
                    if (!listCond.Contains(cond))
                        listCond.Add(Convert.ToString(cond));
                }
                dictRoles.Add(role, listCond);
            }

            foreach(var role in listRoles)
            {
                if (!dictRoles.ContainsKey(role))
                {
                    htRoles.Add(role, null);
                    continue;
                }
                var condList = dictRoles[role];
                foreach(var cond in condList)
                {
                    if (htCond == null) continue;
                    if (htCond.ContainsKey(cond))
                    {
                        var value = htCond[cond];
                        if (value == null) htRoles.Add(role, cond);
                        var htTemp = new Hashtable();
                        htTemp.Add(cond, value);
                        htRoles.Add(role, htTemp);
                    }
                    else
                    {
                        htRoles.Add(role, cond);
                    }
                }
            }

            return htRoles;
        }

        private Hashtable GetUserCond()
        {
            const string condTitle = "COND_TITLE";
            const string condValue = "COND_VALUE";

            var ret = new Hashtable();
            var listCond = new List<string>();
            var listValue = new List<string>();

            foreach(var entry in _condInfo)
            {
                var cond = entry[condTitle];
                if (!listCond.Contains(cond))
                    listCond.Add(cond);
            }

            foreach (var cond in listCond)
            {
                listValue.Clear();
                foreach(var entry in _condInfo)
                {
                    if (cond != entry[condTitle])
                        continue;
                    var value = entry[condValue];
                    if (!listValue.Contains(value))
                        listValue.Add(value);
                }
                ret.Add(cond, listValue.ToArray());
            }

            return ret;
        }

        public List<Dictionary<string, string>> GetListDictCond
        (
        string role,
        Dictionary<string, List<string>?> dictCond
        )
        {
            var listDict = new List<Dictionary<string, string>>();

            foreach (var cond in dictCond)
            {
                if (cond.Value == null)
                {
                    var dict = new Dictionary<string, string>();
                    dict.Add("ROLE", role);
                    dict.Add("COND_TITLE", cond.Key);
                    dict.Add("COND_VALUE", string.Empty);
                    listDict.Add(dict);
                    continue;
                }

                foreach (var item in cond.Value)
                {
                    var dict = new Dictionary<string, string>();
                    dict.Add("ROLE", role);
                    dict.Add("COND_TITLE", cond.Key);
                    dict.Add("COND_VALUE", item);
                    listDict.Add(dict);
                }
            }

            return listDict;
        }

        #endregion

        #region "Util Functions"

        private static SecureString ConvertToSecureString(string cred)
        {
            if (cred == null)
                throw new ArgumentNullException("cred");

            var secureCred = new SecureString();

            foreach (char c in cred)
                secureCred.AppendChar(c);

            secureCred.MakeReadOnly();
            return secureCred;
        }

        private static string? SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        private static MemoryStream GenerateStreamFromString(string? value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

        [DebuggerStepThrough]
        private static string StrFromObj(object? val)
        {
            if (val == null) return string.Empty;
            string? ret = Convert.ToString(val);
            return string.IsNullOrEmpty(ret) ? string.Empty : ret;
        }

        [DebuggerStepThrough]
        private static string ToStr(string? val)
        {
            return string.IsNullOrEmpty(val) ? string.Empty : Convert.ToString(val);
        }

        [DebuggerStepThrough]
        private static DataTable ToDataTable(List<Dictionary<string,string>> inData)
        {
            DataTable dt = new DataTable();

            if (inData.Count > 0)
            {
                var dict = inData[0];
                foreach (var key in dict.Keys)
                {
                    dt.Columns.Add(key);
                }
            }

            dt.BeginLoadData();
            foreach(var entry in inData)
            {
                var dr = dt.NewRow();
                foreach (var key in entry.Keys)
                {
                    dr[key] = entry[key];
                }
                dt.Rows.Add(dr);
            }
            dt.EndLoadData();

            return dt;
        }

        [DebuggerStepThrough]
        private static List<Dictionary<string,string>> ToListDict(DataTable dt)
        {
            var listDict = new List<Dictionary<string,string>>();

            foreach(DataRow dr in dt.Rows)
            {
                var dict = new Dictionary<string,string>();
                foreach(DataColumn dc in dt.Columns)
                {
                    dict.Add(dc.ColumnName, StrFromObj(dr[dc]));
                }
                listDict.Add(dict);
            }

            return listDict;
        }

        private static Dictionary<string, string> GetDict(string key, string? value)
        {
            var ret = new Dictionary<string, string>();
            ret.Add(key, ToStr(value));
            return ret;
        }

        #endregion

        #region "Json Functions"

        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var jsonString = JsonSerializer.Serialize(this, options);
            return jsonString;
        }

        public static CacheUser GetCacheUser(string jsonData)
        {
            using var jsonStream = GenerateStreamFromString(jsonData);
            var config = new ConfigurationBuilder()
                .AddJsonStream(jsonStream)
                .Build();

            CacheUser userSettings = new(config);
            return userSettings;
        }

        #endregion
    }
}
