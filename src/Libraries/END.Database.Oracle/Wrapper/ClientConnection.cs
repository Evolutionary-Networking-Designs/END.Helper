using System.Data;
using System.Data.Common;
using System.Diagnostics;
using END.Embedded;
using Oracle.ManagedDataAccess.Client;

// ReSharper disable ConvertToAutoProperty
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable StringIndexOfIsCultureSpecific.1

namespace END.Database.Oracle.Wrapper
{
    public class ClientConnection : DbConnection, IDisposable
    {
        private readonly string _appName;
        private readonly OracleConnection _oraConn;
        private readonly bool _setRoles;
        private readonly string _userId;
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private bool _valid;
        private string _clientId;

        #region "Properties"

        public OracleConnection Connection => _oraConn;
        public bool SetRoles => _setRoles;
        public string AppName => _appName;
        public string UserId => _userId;

        public string ClientId
        { 
            get { return _clientId; }
            set { _clientId = value; }
        }
        public override ConnectionState State => _oraConn.State;

        public override string Database => _oraConn.Database;

        public override string DataSource => _oraConn.DataSource;

        public override string ServerVersion => _oraConn.ServerVersion;

        public override string ConnectionString
        {
            get => _oraConn.ConnectionString;
            set => _oraConn.ConnectionString = Convert.ToString(value);
        }

        #endregion

        public ClientConnection(string connStr)
        {
            if (string.IsNullOrEmpty(connStr))
            {
                _valid = false;
                _ = new QueryException("Connection String can not be empty!");
                return;
            }

            _valid = true;
            _appName = Config.Helper.GetAppSetting(Config.AppSetting.AppName);
            _setRoles = Helper.CheckSetRolesOnSelect();
            _oraConn = new OracleConnection(connStr);
            var builder = new OracleConnectionStringBuilder(connStr);
            _userId = builder.UserID;
        }

        public ClientConnection(string connStr, bool setRole, string clientId = "", string appName = "")
        {
            if (string.IsNullOrEmpty(connStr))
            {
                _valid = false;
                _ = new QueryException("Connection String can not be empty!");
                return;
            }

            _valid = true;

            if (!string.IsNullOrEmpty(appName))
                _appName = appName;
            else
                _appName = Config.Helper.GetAppSetting(Config.AppSetting.AppName);

            var builder = new OracleConnectionStringBuilder(connStr);
            _userId = builder.UserID;
            _oraConn = new OracleConnection(connStr);
            _setRoles = setRole;
            _clientId = clientId;
        }
        
        public override void Open()
        {
            if (!_valid)
                return;

            if (_oraConn.State != ConnectionState.Open)
                _oraConn.Open();

            if (!_setRoles) return;
            
            SetApplication();

            if (string.IsNullOrEmpty(_clientId)) return;
                
            _oraConn.ClientId = _clientId;
            SetOrRemoveRoles(true);
        }

        public override void Close()
        {
            if (!_valid)
                return;

            if (!string.IsNullOrEmpty(_clientId))
                SetOrRemoveRoles(false);

            if (_oraConn.State != ConnectionState.Closed)
                _oraConn.Close();
        }

        #region "Role Management"

        [DebuggerStepThrough]
        private void SetApplication()
        {
            OracleCommand? oCmd = null;
            var iResult = -1;
            var sql = Prop.GetProperty(@"connect\appinfo_set_module.sql");

            try
            {
                oCmd = new OracleCommand(Helper.PrepQuery(sql), _oraConn);
                oCmd.CommandType = CommandType.StoredProcedure;
                oCmd.Prepare();
                oCmd.Parameters.Add("p01", _appName);
                oCmd.Parameters.Add("p02", "Initialize");
                iResult = oCmd.ExecuteNonQuery();
            }
            catch (OracleException ex)
            {
                _ = new QueryException(_userId, _clientId, ex, oCmd);
            }
            finally
            {
                oCmd?.Dispose();
            }
        }

        [DebuggerStepThrough]
        private int SetOrRemoveRoles(bool enable)
        {

            OracleCommand? oCmd = null;
            var iResult = -1;
            var sql = "";

            sql = Prop.GetProperty(enable ? @"user\app_set_roles.sql" : @"user\app_unset_roles.sql");

            try
            {
                oCmd = new OracleCommand(Helper.PrepQuery(sql), _oraConn);
                oCmd.CommandType = CommandType.StoredProcedure;
                oCmd.Prepare();

                iResult = oCmd.ExecuteNonQuery();
            }
            catch (OracleException ex)
            {
                _ = new QueryException(_userId, _clientId, ex, oCmd);
            }
            finally
            {
                oCmd?.Dispose();
            }

            return iResult;
        }

        #endregion

        void IDisposable.Dispose()
        {
            if (!_valid)
                return;

            _oraConn.Dispose();
        }

        #region "Oracle Wrapper"

        public override void ChangeDatabase(string databaseName)
        {
            _oraConn.ChangeDatabase(databaseName);
        }

        protected OracleTransaction BeginOraTransaction(IsolationLevel isolationLevel)
        {
            return _oraConn.BeginTransaction(isolationLevel);
        }

        protected OracleCommand CreateOraCommand()
        {
            return _oraConn.CreateCommand();
        }

        #endregion

        #region "Generic Functions"

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return _oraConn.BeginTransaction(isolationLevel);
        }

        protected override DbCommand CreateDbCommand()
        {
            return _oraConn.CreateCommand();
        }

        #endregion
    }
}
