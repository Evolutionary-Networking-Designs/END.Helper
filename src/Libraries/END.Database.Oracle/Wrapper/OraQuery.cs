using System.Data;
using END.Config;
using END.Config.Crypto;
using END.Database;
using Oracle.ManagedDataAccess.Client;

// ReSharper disable StringIndexOfIsCultureSpecific.1
// ReSharper disable ConvertToAutoProperty
// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable ReplaceSubstringWithRangeIndexer

namespace END.Database.Oracle.Wrapper
{
    public class OraQuery : IOraQuery, IDisposable
    {
        private readonly AppSettings _config;
        private readonly string _appName;
        private readonly string _connStr;
        private readonly string _dataSource;
        private string _clientId;
        private string _userId;
        private bool _setRoles;
        private ClientConnection? _connection = null;
        private CryptoUtil _crypto = new();
        private bool _valid;
        private readonly bool _showError;

        public string ClientId
        {
            get { return _clientId; }
            set { _clientId = value; }
        }

        public string UserId
        {
            get { return _userId; }
        }

        public string ConnectionString
        {
            get { return _connStr; }
        }

        public bool SetRole
        {
            get { return _setRoles; }
            set 
            {
                if (value == true)
                    _setRoles = Helper.CheckSetRolesOnSelect();
                else
                    _setRoles = false;
            }
        }

        public ClientConnection Connection => GetDbConnection();

        private string GetConfigConnStr()
        {
            var appName = _appName.ToUpper();
            var listConnStr = _config.ConnectionStrings;

            var defaultName = string.Concat(_config.Environment, "-", _config.ServiceName);
            var keyName = string.Concat(defaultName, "-", appName);

            if (listConnStr.ContainsKey(keyName))
                return listConnStr[keyName];
            else
                if (listConnStr.ContainsKey(defaultName))
                return listConnStr[keyName];

            return string.Empty;
        }

        private string GetConnString()
        {
            var connStr = GetConfigConnStr();
            connStr = _crypto.DecryptValue(connStr);

            var builder = new OracleConnectionStringBuilder(connStr)
            {
                DataSource = _dataSource
            };

            _userId = builder.UserID;

            return builder.ConnectionString;
        }

        private bool ShowError(string inValue)
        {
            inValue = inValue.ToUpper();
            return " YES Y TRUE T 1 ".IndexOf(" " + inValue + " ") >= 0;
        }

        private DataTable GetUserRoles()
        {
            string query = "SELECT * FROM SESSION_ROLES";
            return GetDataTable(query, null);
        }

        public OraQuery(string appName = "", bool? setRoles = null, string clientId = "")
        {
            _valid = true;
            _config = Config.Helper.AppSettings();
            _appName = string.IsNullOrEmpty(appName) ? Config.Helper.GetAppSetting(AppSetting.AppName) : appName;

            _showError = ShowError(_config.ShowError);

            _setRoles = setRoles == null ? Helper.CheckSetRolesOnSelect() : Convert.ToBoolean(setRoles);

            _dataSource = Config.Helper.GetAppSetting(AppSetting.DataSource);
            _dataSource = _crypto.DecryptValue(_dataSource);
            _connStr = GetConnString();

            _clientId = clientId;

            var dtRoles = GetUserRoles();

            if (string.IsNullOrEmpty(_connStr))
            {
                if (_showError)
                    throw new QueryException("Connection String can not be null!", LogLevel.Error);
                else
                    _ = new QueryException("Connection String can not be null!", LogLevel.Error);
                _valid = false;
            }

        }

        private ClientConnection GetDbConnection()
        {
            if (_connection == null)
                _connection = new ClientConnection(_connStr, _setRoles);
            return _connection;
        }

        #region "Data Routines"

        private DataTable ProcessDataReader(ref OracleDataReader odr)
        {
            var dt = new DataTable();

            if (odr != null)
            {
                int colCount = odr.FieldCount;
                for(int i = 0; i< colCount; i++)
                {
                    dt.Columns.Add(new DataColumn(odr.GetName(i)));
                }

                while (odr.Read())
                {
                    object?[] arrVal;
                    arrVal = new object?[colCount];
                    for (int i = 0; i < colCount; i++)
                    {
                        object? oVal = odr.GetValue(i);
#pragma warning disable 
                        arrVal[i] = oVal;
#pragma warning restore
                    }
                    dt.Rows.Add(arrVal);
                }
            }
            return dt;
        }

        public DataTable RunSelectTable(string inQuery, List<QueryParam>? inParams)
        {
            var dt = new DataTable();

            if (!_valid)
            {
                dt.TableName = "NO_DATA";
                return dt;
            }

            using var clientConn = new ClientConnection(_connStr, _setRoles, _clientId);
            clientConn.Open();
            var oraConn = clientConn.Connection;
            var isProc = inQuery.StartsWith("exec");
            var query = Helper.PrepQuery(inQuery);
            var oraParams = QueryUtil.ParseParameters(inQuery, inParams);
            OracleDataReader odr;

            using (var oraCmd = new OracleCommand(query, oraConn))
            {
                try
                {
                    oraCmd.CommandType = isProc ? CommandType.StoredProcedure : CommandType.Text;

                    foreach (var param in oraParams)
                    {
                        if (!isProc)
                        {
                            oraCmd.BindByName = true;
                            param.ParameterName = ":" + param.ParameterName;
                        }
                        oraCmd.Parameters.Add(param);
                    }

                    odr = oraCmd.ExecuteReader();
                    dt = ProcessDataReader(ref odr);
                }
                catch (OracleException ex)
                {
                    if (_showError)
                        throw new QueryException(_userId, _clientId, ex, oraCmd);
                    else
                        _ = new QueryException(_userId, _clientId, ex, oraCmd);
                }
            }

            clientConn.Close();
            return dt;
        }

        public DataTable GetDataTable(string inQuery, List<QueryParam>? inParams)
        {
            var dt = new DataTable();

            if (!_valid)
            {
                dt.TableName = "NO_DATA";
                return dt;
            }

            using var clientConn = new ClientConnection(_connStr, _setRoles);

            clientConn.ClientId = _clientId;
            clientConn.Open();
            var oraConn = clientConn.Connection;
            var isProc = inQuery.StartsWith("exec");
            var query = Helper.PrepQuery(inQuery);
            var oraParams = QueryUtil.ParseParameters(inQuery, inParams);

            using (var oraCmd = new OracleCommand(query, oraConn))
            {
                try
                {
                    oraCmd.CommandType = isProc ? CommandType.StoredProcedure : CommandType.Text;

                    foreach (var param in oraParams)
                    {
                        if (!isProc)
                        {
                            oraCmd.BindByName = true;
                            param.ParameterName = ":" + param.ParameterName;
                        }
                        oraCmd.Parameters.Add(param);
                    }

                    if (isProc)
                        oraCmd.ExecuteNonQuery();
                    else
                    {
                        var oraAdapter = new OracleDataAdapter(oraCmd);
                        oraAdapter.Fill(dt);
                    }
                }
                catch (OracleException ex)
                {
                    if (_showError)
                        throw new QueryException(_userId, _clientId, ex, oraCmd);
                    else
                        _ = new QueryException(_userId, _clientId, ex, oraCmd);
                }
            }

            clientConn.Close();
            return dt;
        }

        public DataTable GetDataTable(OracleCommand oraCommand)
        {
            var dt = new DataTable();

            if (!_valid)
            {
                dt.TableName = "NO_DATA";
                return dt;
            }

            using var clientConn = new ClientConnection(_connStr, _setRoles);
            oraCommand.Connection = clientConn.Connection;

            clientConn.ClientId = _clientId;
            clientConn.Open();

            using (var oraCmd = oraCommand)
            {
                try
                {
                    var oraAdapter = new OracleDataAdapter(oraCmd);
                    oraAdapter.Fill(dt);
                }
                catch (OracleException ex)
                {
                    if (_showError)
                        throw new QueryException(_userId, _clientId, ex, oraCmd);
                    else
                        _ = new QueryException(_userId, _clientId, ex, oraCmd);
                }
            }

            clientConn.Close();
            return dt;
        }

        public int ExecuteQuery(string inQuery, List<QueryParam>? inParams)
        {
            var ret = -1;

            if (!_valid) return ret;

            using var clientConn = GetDbConnection();

            clientConn.ClientId = _clientId;
            clientConn.Open();

            var oraConn = clientConn.Connection;
            var isProc = inQuery.StartsWith("exec");
            var query = Helper.PrepQuery(inQuery);
            var oraParams = QueryUtil.ParseParameters(inQuery, inParams);

            using (var oraCmd = new OracleCommand(query, oraConn))
            {
                try
                {
                    oraCmd.CommandType = isProc ? CommandType.StoredProcedure : CommandType.Text;

                    foreach (var param in oraParams)
                    {
                        if (!isProc)
                        {
                            oraCmd.BindByName = true;
                            param.ParameterName = ":" + param.ParameterName;
                        }
                        oraCmd.Parameters.Add(param);
                    }

                    ret = oraCmd.ExecuteNonQuery();
                }
                catch (OracleException ex)
                {
                    if (_showError)
                        throw new QueryException(_userId, _clientId, ex, oraCmd);
                    else
                        _ = new QueryException(_userId, _clientId, ex, oraCmd);
                }
            }

            clientConn.Close();
            return ret;
        }

        public int ExecuteQuery(OracleCommand oraCommand)
        {
            var ret = -1;

            if (!_valid) return ret;

            using var clientConn = GetDbConnection();
            oraCommand.Connection = clientConn.Connection;

            clientConn.ClientId = _clientId;
            clientConn.Open();

            using (var oraCmd = oraCommand)
            {
                try
                {
                    ret = oraCmd.ExecuteNonQuery();
                }
                catch (OracleException ex)
                {
                    if (_showError)
                        throw new QueryException(_userId, _clientId, ex, oraCmd);
                    else
                        _ = new QueryException(_userId, _clientId, ex, oraCmd);
                }
            }

            clientConn.Close();
            return ret;
        }

        public void RunScript(string inQuery, List<QueryParam>? inParams)
        {
            if (!_valid) return;

            char[] sDelim = { ';' };
            string[] sSQL = inQuery.Split(sDelim);

            using var clientConn = GetDbConnection();
            clientConn.ClientId = _clientId;
            clientConn.Open();

            OracleConnection oConn = clientConn.Connection;
            OracleTransaction? oTran = null;

            try
            {
                for (int i = 0; i < sSQL.Length; i++)
                {
                    sSQL[i] = sSQL[i].Replace(";", "");
                    string sTest = sSQL[i].Trim().ToUpper();
                    while (sTest.StartsWith("--"))
                    {
                        int iIndex = sTest.IndexOf("\n");
                        sTest = sTest.Substring(iIndex + 2).Trim();
                    }
                    if (sTest.Length < 3)
                    {
                        continue;
                    }

                    if (sTest == "BEGIN")
                    {
                        if (oTran != null)
                        {
                            oTran.Commit();
                            oTran.Dispose();
                        }
                        oTran = oConn.BeginTransaction();
                        continue;
                    }
                    if (sTest == "END" || sTest == "COMMIT")
                    {
                        if (oTran != null)
                        {
                            oTran.Commit();
                            oTran.Dispose();
                            oTran = null;
                        }
                        continue;
                    }
                    if (sTest == "ROLLBACK")
                    {
                        if (oTran != null)
                        {
                            oTran.Rollback();
                            oTran.Dispose();
                            oTran = null;
                        }
                        continue;
                    }
                    RunUpdate(ref oConn, sSQL[i], inParams);
                }
            }
            catch (OracleException ex)
            {
                if (_showError)
                    throw new QueryException(_userId, _clientId, ex);
                else
                    _ = new QueryException(_userId, _clientId, ex);
            }
        }

        private int RunUpdate(ref OracleConnection conn, string inQuery, List<QueryParam>? inParams)
        {
            var ret = -1;

            var oraConn = conn;
            var isProc = inQuery.StartsWith("exec");
            var query = Helper.PrepQuery(inQuery);
            var oraParams = QueryUtil.ParseParameters(inQuery, inParams);

            using (var oraCmd = new OracleCommand(query, oraConn))
            {
                try
                {
                    oraCmd.CommandType = isProc ? CommandType.StoredProcedure : CommandType.Text;

                    foreach (var param in oraParams)
                    {
                        if (!isProc)
                        {
                            oraCmd.BindByName = true;
                            param.ParameterName = ":" + param.ParameterName;
                        }
                        oraCmd.Parameters.Add(param);
                    }

                    ret = oraCmd.ExecuteNonQuery();
                }
                catch (OracleException ex)
                {
                    if (_showError)
                        throw new QueryException(_userId, _clientId, ex, oraCmd);
                    else
                        _ = new QueryException(_userId, _clientId, ex, oraCmd);
                }
            }

            return ret;
        }

        #endregion

        public void Dispose()
        {
            
        }

    }
}
