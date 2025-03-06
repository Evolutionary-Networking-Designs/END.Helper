using System.Data;

namespace END.Database;

public static class Query
{
    public static void Init(string appName, bool setRoles = false)
    {
        
    }

    public static void Reset()
    {
        
    }
    public static string GetScalar(string inQuery, List<QueryParam>? inParams = null)
    {
        return string.Empty;
    }

    public static DataTable GetDataTable(string inQuery, List<QueryParam>? inParams = null)
    {
        var dt = new DataTable();
        return dt;
    }
}