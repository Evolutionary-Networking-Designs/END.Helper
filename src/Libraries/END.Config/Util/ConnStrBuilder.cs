using System.Data.Common;

// ReSharper disable ConvertToAutoProperty

namespace END.Config.Util;

public class ConnStrBuilder : DbConnectionStringBuilder
{
    private string _dataSource = string.Empty;

    public new string ConnectionString
    {
        get => base.ConnectionString;
        set => UpdateDataSource(value);
    }
    
    public string DataSource
    {
        get => _dataSource;
        set => UpdateConnectionString(value);
    }

    private void UpdateDataSource(string connStr)
    {
        base.ConnectionString = connStr;
    }

    private void UpdateConnectionString(string dataSource)
    {
        _dataSource = dataSource;
    }
}