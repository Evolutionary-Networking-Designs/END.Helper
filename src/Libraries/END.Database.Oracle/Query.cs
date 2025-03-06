using System.Data;
using END.Database.Interface;

namespace END.Database.Oracle;

public class Query : IQuery
{
    public string GetScalar(string inQuery, List<QueryParam>? inParams)
    {
        throw new NotImplementedException();
    }

    public DataTable GetDataTable(string inQuery, List<QueryParam>? inParams)
    {
        throw new NotImplementedException();
    }
}