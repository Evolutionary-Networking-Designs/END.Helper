using System.Data;

namespace END.Database.Oracle.Wrapper;

public interface IOraQuery
{
    public abstract DataTable GetDataTable(string inQuery, List<QueryParam>? inParams);
    public abstract int ExecuteQuery(string inQuery, List<QueryParam>? inParams);
}