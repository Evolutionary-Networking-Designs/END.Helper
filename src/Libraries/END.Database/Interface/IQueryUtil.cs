using System.Data.Common;

namespace END.Database.Interface;

public interface IQueryUtil
{
    public List<DbParameter> GetParameters(List<QueryParam>? inParams);
    public List<DbParameter> ParseParameters(string inQuery, List<QueryParam>? inParams);

}