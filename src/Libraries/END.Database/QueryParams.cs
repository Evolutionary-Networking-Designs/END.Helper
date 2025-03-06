using System.Diagnostics;

namespace END.Database
{
    public class QueryParam
    {
        public QueryParam()
        {
            Name = string.Empty;
        }

        [DebuggerStepThrough]
        public QueryParam(string name, object? value = null)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public object? Value { get; set; }

    }
}
