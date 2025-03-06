using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Web;
using System.Web.SessionState;

namespace END.Database
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
