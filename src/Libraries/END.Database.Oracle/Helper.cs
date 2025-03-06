using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using static System.DateTime;

// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ReplaceSubstringWithRangeIndexer

namespace END.Database.Oracle
{
    public static class Helper
    {
        #region "Query Prep"

        /// <summary>
        /// Should Role Package Be Executed
        /// </summary>
        /// <returns></returns>
        [DebuggerStepThrough]
        internal static bool CheckSetRolesOnSelect()
        {
            var setRoles = Config.Helper.GetAppSetting("SetRolesOnQuery");
            setRoles = setRoles.Trim().ToUpper();
            const string trueValues = " YES Y TRUE T 1 ";
            return trueValues.IndexOf(" " + setRoles + " ", StringComparison.Ordinal) >= 0;
        }

        [DebuggerStepThrough]
        internal static string StripCommentedLines(string inQuery)
        {
            if (string.IsNullOrEmpty(inQuery)) return string.Empty;
            if (inQuery.IndexOf("\n", StringComparison.Ordinal) < 0) return inQuery;
            char[] sDelim = ['\n'];
            var sQuery = inQuery.Split(sDelim);
            var sResult = "";
            foreach (var t in sQuery)
            {
                if (t.Trim().StartsWith("--")) continue;
                var iComment = t.IndexOf("--", StringComparison.Ordinal);
                var line = (iComment > 0) ? t.Substring(0, iComment -1) : t;
                sResult += line;
            }
            return sResult;
        }

        [DebuggerStepThrough]
        internal static string PrepQuery(string sql)
        {
            if ((sql.IndexOf(";", StringComparison.Ordinal) > 0))
                sql = sql.Replace(";","");

            var ret = sql.Replace("\r", "");
            ret = StripCommentedLines(ret);

            if (!ret.StartsWith("exec ")) return ret;
            
            ret = sql.Substring(5).Trim();
            var iIndex = ret.IndexOf('(');
            ret = iIndex > 0 ? ret.Substring(0, iIndex).Trim() : ret.Trim();

            return ret;
        }

        #endregion
       
        /// <summary>
        /// Returns ClearText Credential.
        /// </summary>
        /// <param name="cred">SecureString Credential</param>
        /// <returns></returns>
        public static string GetCredential(SecureString? cred)
        {
            if (cred == null)
                return string.Empty;

            var result = Marshal.PtrToStringUni(Marshal.SecureStringToGlobalAllocUnicode(cred));
            return result ?? string.Empty;
        }

        /// <summary>
        /// Return SecureString Credential.
        /// </summary>
        /// <param name="cred">ClearText Credential</param>
        /// <returns></returns>
        public static SecureString SetCredential(string cred)
        {
            SecureString ss = new SecureString();

            foreach (char c in cred)
            {
                ss.AppendChar(c);
            }

            return ss;
        }
        
        /// <summary>
        /// Convert string date from Oracle to 03-NOV-2020 format
        /// </summary>
        /// <param name="inOracleDate"></param>
        /// <returns></returns>
        public static string MilDateFromOracleDate(string inOracleDate)
        {
            return TryParse(inOracleDate, out DateTime result) ? result.ToString("dd-MMM-yyyy").ToUpper() : "";
        }
        
        /// <summary>
        /// Convert 03-NOV-2020 format to 11/03/2020
        /// </summary>
        /// <param name="inMilDate"></param>
        /// <returns></returns>
        public static string OracleDateFromMilDate(string inMilDate)
        {
            return TryParse(inMilDate, out var result) ? result.ToString("MM/dd/yyyy") : "";
        }

        /// <summary>
        /// Takes a string date, attempts to convert to a DateTime type.
        /// </summary>
        /// <param name="inDate">A potential date string</param>
        /// <returns>Empty String or Oracle formatted string.</returns>
        public static string GetOracleDate(string inDate)
        {
            return TryParse(inDate, out var aDate) ? GetOracleDate(aDate) : "";
        }
        
        /// <summary>
        /// Takes a DateTime type and converts it to a String for Oracle.
        /// </summary>
        /// <param name="inDate">DateTime to convert</param>
        /// <returns>Empty string or String version of date.</returns>
        public static string GetOracleDate(DateTime inDate)
        {
            return inDate.ToString("dd-MMM-yyyy HH:mm:ss");
        }
    }
}
