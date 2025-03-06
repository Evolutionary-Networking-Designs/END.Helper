using System.Data;

namespace END.Database.Interface;

public interface IQuery
{
    public string GetScalar(string inQuery, List<QueryParam>? inParams);
    public DataTable GetDataTable(string inQuery, List<QueryParam>? inParams);
}