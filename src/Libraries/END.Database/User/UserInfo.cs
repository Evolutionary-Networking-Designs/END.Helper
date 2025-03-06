using System.Data;
using END.Database.Interface;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace END.Database.User
{
    public class UserInfo : IUserInfo
    {
        #region "Helper Functions"
        
        public static string GetTableNameByEnum(UserTables tableEnum)
        {
            var tblName = tableEnum switch
            {
                UserTables.CERT_INFO => Const.CERT_INFO,
                UserTables.USER_INFO => Const.USER_INFO,
                UserTables.USER_DETAIL => Const.USER_DETAIL,
                UserTables.USER_ROLES => Const.USER_ROLES,
                UserTables.USER_COND => Const.USER_COND,
                _ => Const.NO_DATA
            };
            return tblName;
        }

        public static DataTable GetDataTable(
            Dictionary<string, DataTable> dtDict, 
            UserTables tableEnum = UserTables.USER_INFO
        )
        {
            var tblName = GetTableNameByEnum(tableEnum);

            var valid = dtDict.TryGetValue(tblName, out DataTable? dt);
            if (valid && (dt != null)) return dt;
            
            dt = new DataTable();
            dt.TableName = Const.NO_DATA;
            return dt;
        }
        
        #endregion

        #region "IUserInfo Members"
        
        public string GetEncryptKey()
        {
            throw new NotImplementedException();
        }

        public string GetCommonCred()
        {
            throw new NotImplementedException();
        }

        public DataTable GetUniqueCertList(bool showExpired = false)
        {
            throw new NotImplementedException();
        }

        public DataTable GetUserCertList(bool showExpired = false)
        {
            throw new NotImplementedException();
        }

        public DataTable GetUserList()
        {
            throw new NotImplementedException();
        }

        public DataTable GetUsersByApp(string appName)
        {
            throw new NotImplementedException();
        }

        public DataTable GetRoleListByApp(string appName)
        {
            throw new NotImplementedException();
        }

        public DataTable GetCondByUser(string appName, string userId)
        {
            throw new NotImplementedException();
        }

        #endregion
        
        public Dictionary<string, DataTable> GetUserData(string appName)
        {
            var dtUsers = GetUsersByApp(appName);
            var dtCerts = GetUserCertList();
            var dtUserDetail = GetUserList();
            var dtRoles = GetRoleListByApp(appName);

            var ret = new Dictionary<string, DataTable>();

            var dtCertUser = QueryUtil.MergeTables(
                dtCerts, dtUsers, "USER_SEQ", JoinType.Left);
            dtCertUser.TableName = Const.USER_INFO;

            var dtCertDetail = QueryUtil.MergeTables(
                dtCerts, dtUserDetail, "USER_SEQ", JoinType.Right);
            dtCertDetail.TableName = Const.USER_DETAIL;

            var dtUserRoles = QueryUtil.MergeTables(
                dtCerts, dtRoles, "USER_SEQ", JoinType.Left);
            dtUserRoles.TableName = Const.USER_ROLES;

            ret.Add(Const.USER_INFO, dtCertUser);
            ret.Add(Const.USER_DETAIL, dtCertDetail);
            ret.Add(Const.USER_ROLES, dtUserRoles);
            
            return ret;
        }

        /// <summary>
        /// Returned a filtered set of tables.
        /// </summary>
        /// <param name="appName">Application Name</param>
        /// <param name="rowFilter">Filter to Apply</param>
        /// <param name="showExpired">Show Expired Certs?</param>
        /// <returns></returns>
        private Dictionary<string, DataTable> GetFilteredTables( 
            string appName,
            string rowFilter,
            bool showExpired = true
        )
        {
            var ds = GetUserData(appName);

            var dsResult = new Dictionary<string, DataTable>();
            var dtCerts = GetUniqueCertList(false);
            var dvUser = ds[Const.USER_INFO].DefaultView;
            var dvDetail = ds[Const.USER_DETAIL].DefaultView;
            var dvRoles = ds[Const.USER_ROLES].DefaultView;

            dvUser.RowFilter = rowFilter;
            var dtUser = dvUser.ToTable();

            dvDetail.RowFilter = rowFilter;
            var dtDetail = dvDetail.ToTable();

            if (dvUser.Count == 0)
                return dsResult;

            dvRoles.RowFilter = rowFilter;
            var dtRoles = dvRoles.ToTable();

            dtUser.TableName = Const.USER_INFO;
            dtDetail.TableName = Const.USER_DETAIL;
            dtRoles.TableName = Const.USER_ROLES;

            dsResult.Add(dtCerts.TableName, dtCerts);
            dsResult.Add(dtUser.TableName, dtUser);
            dsResult.Add(dtDetail.TableName, dtDetail);
            dsResult.Add(dtRoles.TableName, dtRoles);

            return dsResult;
        }

        public Dictionary<string, DataTable> GetDevUserData(
            string appName
        )
        {
            var rowFilter = "USER_ID LIKE 'XX_%'";
            return GetFilteredTables(appName, rowFilter, false);
        }

        public Dictionary<string, DataTable> GetUserInfo(string appName, string userId)
        {
            var rowFilter = $"USER_ID = '{userId}'";
            var dtDict = GetFilteredTables(appName, rowFilter, false);
            var dtCond = GetCondByUser(appName, userId);

            dtDict.Add(dtCond.TableName, dtCond);

            return dtDict;
        }

        public Dictionary<string, DataTable> GetUserInfoByCert(string appName, string certName)
        {
            var rowFilter = $"CERT_SUBJECTCN = '{certName}'";
            return GetFilteredTables(appName, rowFilter, false);
        }

    }
}
