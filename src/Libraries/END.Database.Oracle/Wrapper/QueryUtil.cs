using System.Data;
using System.Diagnostics;
using System.Web;
using System.Web.SessionState;
using Oracle.ManagedDataAccess.Client;

namespace END.Database.Oracle.Wrapper
{
    internal static class QueryUtil
    {

        public static List<QueryParam>? GetListParam(string?[]? inParams)
        {
            List<string?>? lParams = null;

            if (inParams != null)
                lParams = inParams.ToList();
            return GetParams(lParams);
        }

        public static List<QueryParam>? GetListParam(object?[]? inParams)
        {
            List<object?>? lParams = null;

            if (inParams != null)
                lParams = inParams.ToList();
            return GetParams(lParams);
        }

        public static List<QueryParam>? GetParams(List<string?>? inParams)
        {
            if (inParams == null) return null;

            var ret = new List<QueryParam>();
            string paramName;

            for (int i = 0; i < inParams.Count; i++)
            {
                if (i + 1 < 10) 
                    paramName = "p0" + (i + 1);
                else
                    paramName = "p" + (i + 1);

                string? value = inParams[i];
                var parm = new QueryParam(paramName, value);
                ret.Add(parm);
            }
            return ret;
        }

        [DebuggerStepThrough]
        public static List<QueryParam>? GetParams(List<object?>? inParams)
        {
            if (inParams == null) return null;

            var ret = new List<QueryParam>();
            string paramName;

            for (int i = 0; i < inParams.Count; i++)
            {
                if (i + 1 < 10)
                    paramName = "p0" + (i + 1);
                else
                    paramName = "p" + (i + 1);

                object? value = inParams[i];
                var parm = new QueryParam(paramName, value);
                ret.Add(parm);
            }
            return ret;
        }

        [DebuggerStepThrough]
        public static List<OracleParameter> GetParameters(List<QueryParam>? inParams)
        {
            var ret = new List<OracleParameter>();

            if (inParams == null)
                return ret;

            foreach (var param in inParams)
            {
                var oraParam = new OracleParameter();
                oraParam.ParameterName = param.Name;
                oraParam.Value = param.Value ?? DBNull.Value;
                ret.Add(oraParam);
            }

            return ret;
        }

        public static List<OracleParameter> ParseParameters(string inQuery, List<QueryParam>? inParams)
        {
            var oraParams = new List<OracleParameter>();

            if (inParams == null) return oraParams;

            inQuery = END.Database.Oracle.Helper.StripCommentedLines(inQuery);
            bool bIsCall = inQuery.StartsWith("exec ");
            if (!bIsCall)
            {
                foreach (var param in inParams)
                {
                    var oraParam = new OracleParameter();
                    oraParam.ParameterName = param.Name;
                    oraParam.Value = param.Value ?? DBNull.Value;
                    oraParams.Add(oraParam);
                }
            }
            else
            {
                string sArgList = inQuery.Substring(inQuery.IndexOf("(") + 1).Trim();
                sArgList = sArgList.Substring(0, sArgList.LastIndexOf(")")).Trim();
                char[] sDelims = { ',' };
                string[] sArgs = sArgList.Split(sDelims, StringSplitOptions.RemoveEmptyEntries);

                string sOutputParamName = "";
                OracleParameter? oOutputParam = null;
                int iIndex = 0;

                for (int i = 0; i < sArgs.Length; i++)
                {
                    string sParamName = sArgs[i].Trim();

                    OracleDbType eType = OracleDbType.Varchar2;

                    string sToken = "";

                    if (sParamName.IndexOf("(out as int)") > 0) { eType = OracleDbType.Int32; sToken = "(out as int)"; }
                    else if (sParamName.IndexOf("(out as varchar)") > 0) { eType = OracleDbType.Varchar2; sToken = "(out as varchar)"; }
                    else if (sParamName.IndexOf("(out as varchar2)") > 0) { eType = OracleDbType.Varchar2; sToken = "(out as varchar2)"; }
                    else if (sParamName.IndexOf("(out as cursor)") > 0) { eType = OracleDbType.RefCursor; sToken = "(out as cursor)"; }
                    else if (sParamName.IndexOf("(out)") > 0 ) { eType = OracleDbType.RefCursor; sToken = "(out)"; }

                    if ((sParamName.IndexOf("(out as ") > 0) || (sParamName.IndexOf("(out)") == 0) || (sParamName.IndexOf("(out)") > 0))
                    {
                        sOutputParamName = sParamName.Replace(sToken, "");
                        sOutputParamName = sOutputParamName.Trim();
                        oOutputParam = new OracleParameter(sOutputParamName, eType);
                        oOutputParam.Direction = ParameterDirection.Output;
                        if (sParamName.IndexOf("(out as varchar") > 0)
                        {
                            oOutputParam.Size = 4000;
                        }
                        oraParams.Add(oOutputParam);
                        continue;
                    }

                    if ((sParamName.IndexOf("(in as array)") > 0))
                    {
                        string[]? sEmpty = null;
                        // ----------------------------------------
                        string sInputParamName = sParamName.Replace("(in as array)", "").Trim();
                        string[] sDelimiter = { "," };

                        if (inParams[i].Value == null) continue;
                        string? value = Convert.ToString(inParams[i].Value);
                        if (string.IsNullOrEmpty(value)) continue;

                        string[] sArrayValues = value.Split(sDelimiter, StringSplitOptions.RemoveEmptyEntries);
                        for (int j = 0; j < sArrayValues.Length; j++)
                        {
                            sArrayValues[j] = sArrayValues[j].Replace("[comma]", ",");
                        }

                        // ----------------------------------------

                        OracleParameter oParam = new OracleParameter();
                        oParam.ParameterName = sInputParamName;
                        oParam.OracleDbType = OracleDbType.Varchar2;
                        oParam.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
                        if (sArrayValues.Length > 0) oParam.Value = sArrayValues;
                        else oParam.Value = sEmpty;
                        oParam.Size = sArrayValues.Length;
                        oraParams.Add(oParam);

                        // ----------------------------------------
                        continue;
                    }
                    else
                    {

                        OracleParameter oParam = new OracleParameter(sParamName, OracleDbType.Varchar2);

                        oParam.Direction = ParameterDirection.Input;
                        if (sParamName.ToUpper() == "NULL")
                        {
                            oParam.Value = DBNull.Value;
                            oraParams.Add(oParam);
                            continue;
                        }
                        if (sParamName.StartsWith("'"))
                        {
                            sParamName = sParamName.Substring(1);
                            sParamName = sParamName.Substring(0, sParamName.Length - 1);
                            oParam.Value = sParamName;
                            oraParams.Add(oParam);
                            continue;
                        }

                        if (inParams.Count > 0)
                        {
                            if (inParams[iIndex] == null)
                            {
                                oParam.Value = DBNull.Value;
                            }
                            else
                            {
                                oParam.Value = inParams[iIndex].Value;
                            }
                        }
                        else
                        {
                            oParam.Value = DBNull.Value;
                        }

                        oraParams.Add(oParam);
                    }
                    iIndex++;
                }
            }
            if (inQuery.IndexOf(":p_return_value") >= 0)
            {
                OracleParameter oReturnParam = new OracleParameter("p_return_value", OracleDbType.Varchar2);
                oReturnParam.Direction = ParameterDirection.ReturnValue;
                oReturnParam.Size = 4000;
                oraParams.Add(oReturnParam);
            }

            return oraParams;
        }

        [DebuggerStepThrough]
        public static List<OracleParameter> ParseParameters(string inQuery, bool inRunSelect, List<OracleParameter> inParams)
        {
            bool bIsCall = inQuery.StartsWith("exec ");

            if (bIsCall)
            {
                var sArgList = inQuery.Substring(inQuery.IndexOf("(") + 1).Trim();
                sArgList = sArgList.Substring(0, sArgList.LastIndexOf(")")).Trim();

                char[] sDelims = { ',' };
                string[] sArgs = sArgList.Split(sDelims);

                var sOutputParamName = "";
                OracleParameter? oOutputParam = null;

                for (var i = 0; i < sArgs.Length; i++)
                {
                    var sParamName = sArgs[i].Trim();

                    var eType = OracleDbType.Varchar2;

                    var sToken = "";

                    if (sParamName.IndexOf("(out as int)") > 0) { eType = OracleDbType.Int32; sToken = "(out as int)"; }
                    else if (sParamName.IndexOf("(out as varchar)") > 0) { eType = OracleDbType.Varchar2; sToken = "(out as varchar)"; }
                    else if (sParamName.IndexOf("(out as varchar2)") > 0) { eType = OracleDbType.Varchar2; sToken = "(out as varchar2)"; }
                    else if (sParamName.IndexOf("(out as cursor)") > 0) { eType = OracleDbType.RefCursor; sToken = "(out as cursor)"; }
                    else if (sParamName.IndexOf("(out)") > 0 && inRunSelect) { eType = OracleDbType.RefCursor; sToken = "(out)"; }
                    else if (sParamName.IndexOf("(out)") > 0 && !inRunSelect) { eType = OracleDbType.Varchar2; sToken = "(out)"; }

                    if ((sParamName.IndexOf("(out as ") > 0) || (sParamName.IndexOf("(out)") == 0))
                    {
                        sOutputParamName = sParamName.Replace(sToken, "");
                        sOutputParamName = sOutputParamName.Trim();
                        oOutputParam = new OracleParameter(sOutputParamName, eType);
                        oOutputParam.Direction = ParameterDirection.Output;
                        if (sParamName.IndexOf("(out as varchar") > 0)
                        {
                            oOutputParam.Size = 4000;
                        }
                        inParams.Add(oOutputParam);
                        continue;
                    }
                }
            }

            if (inQuery.IndexOf(":p_return_value") >= 0)
            {
                var oReturnParam = new OracleParameter("p_return_value", OracleDbType.Varchar2);
                oReturnParam.Direction = ParameterDirection.ReturnValue;
                oReturnParam.Size = 4000;
                inParams.Add(oReturnParam);
            }

            return inParams;
        }

        /// <summary>
        /// Merge DataTables into a combined DataTable.
        /// </summary>
        /// <param name="dtLeft"></param>
        /// <param name="dtRight"></param>
        /// <param name="commonColumn"></param>
        /// <param name="join"></param>
        /// <returns></returns>
        public static DataTable MergeTables(
            DataTable dtLeft,
            DataTable dtRight,
            string commonColumn,
            JoinType join = JoinType.Left
        )
        {
            DataTable dtFirst;
            DataTable dtSecond;

            if (dtRight.Columns.Count == 0)
                return dtLeft;
            if (dtLeft.Columns.Count == 0)
                return dtRight;

            switch (join)
            {
                case JoinType.Right:
                    dtFirst = dtRight;
                    dtSecond = dtLeft;
                    break;
                case JoinType.Left:
                    dtFirst = dtLeft;
                    dtSecond = dtRight;
                    break;
                default:
                    dtFirst = dtLeft;
                    dtSecond = dtRight;
                    break;
            }

            DataTable dtResults = dtFirst.Clone();
            int count = 0;
            for (int i = 0; i < dtSecond.Columns.Count; i++)
            {
                if (!dtFirst.Columns.Contains(dtSecond.Columns[i].ColumnName))
                {
                    dtResults.Columns.Add(dtSecond.Columns[i].ColumnName, dtSecond.Columns[i].DataType);
                    count++;
                }
            }

            DataColumn[] columns = new DataColumn[count];
            int j = 0;
            for (int i = 0; i < dtSecond.Columns.Count; i++)
            {
                if (!dtFirst.Columns.Contains(dtSecond.Columns[i].ColumnName))
                {
                    columns[j++] = new DataColumn(dtSecond.Columns[i].ColumnName, dtSecond.Columns[i].DataType);
                }
            }

            var dvSecond = dtSecond.DefaultView;

            dtResults.BeginLoadData();

            foreach (DataRow drFirst in dtFirst.Rows)
            {
                dvSecond.RowFilter = $"{commonColumn} = {drFirst[commonColumn]}";
                var tmpTable = dvSecond.ToTable();

                foreach (DataRow tmpRow in tmpTable.Rows)
                {
                    var data = new List<object>();
                    foreach (DataColumn col in dtFirst.Columns)
                    {
                        data.Add(drFirst[col.ColumnName]);
                    }

                    foreach (DataColumn col in columns)
                    {
                        data.Add(tmpRow[col.ColumnName]);
                    }
                    dtResults.Rows.Add(data.ToArray());
                }
            }

            dtResults.EndLoadData();
            return dtResults;
        }

        public static object[,]? GetArray(DataTable dt)
        {
            string[,]? ret;
            int rowIdx = 0;
            int colIdx;
            string? tmp = "";

            if (dt.Rows.Count > 0)
            {
                ret = new string[dt.Rows.Count, dt.Columns.Count];

                foreach (DataRow row in dt.Rows)
                {
                    colIdx = 0;
                    foreach (DataColumn col in dt.Columns)
                    {
                        object val = dt.Rows[rowIdx][col];
                        tmp = val.ToString();
                        if (string.IsNullOrEmpty(tmp))
                        {
                            ret[rowIdx, colIdx] = "";
                        }
                        else
                        {
                            ret[rowIdx, colIdx] = tmp;
                        }
                        colIdx++;
                    }
                    rowIdx++;
                }
            }
            else
            {
                ret = null;
            }
            return ret;
        }

        public static string[,]? GetStringArray(DataTable dt)
        {
            string[,]? ret;
            int rowIdx = 0;
            int colIdx;
            string? tmp = "";

            if (dt.Rows.Count > 0)
            {
                ret = new string[dt.Rows.Count, dt.Columns.Count];

                foreach (DataRow row in dt.Rows)
                {
                    colIdx = 0;
                    foreach (DataColumn col in dt.Columns)
                    {
                        object val = dt.Rows[rowIdx][col];
                        tmp = val.ToString();
                        if (string.IsNullOrEmpty(tmp))
                        {
                            ret[rowIdx, colIdx] = "";
                        }
                        else
                        {
                            ret[rowIdx, colIdx] = tmp;
                        }
                        colIdx++;
                    }
                    rowIdx++;
                }
            }
            else
            {
                ret = null;
            }
            return ret;
        }

        [DebuggerStepThrough]
        public static HttpSessionState? GetSession()
        {
            var context = HttpContext.Current;
            if (context == null) return null;
            return context.Session;
        }

    }
}
