using Antlr4.Runtime;
// ReSharper disable ConvertToAutoProperty
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace END.QueryParser;

public static class QueryParser
{
    private static Query.QueryParser? _queryParser;
    private static string? _query;

    public static string Query
    {
        get => string.IsNullOrEmpty(_query) ? string.Empty : _query;
        set => _query = value;
    }

    public static string Name
    {
        get
        {
            if (string.IsNullOrEmpty(_query)) return string.Empty;
            if (_queryParser == null)
                LoadImplementation(_query);
            return _queryParser.ParserName;
        }
    }
    
    public static Parser? Parser
    {
        get
        {
            if (string.IsNullOrEmpty(_query)) return null;
            if (_queryParser == null)
                LoadImplementation(_query);
            return _queryParser.Parser;
        }
    }
    
    public static object? Visitor
    {
        get
        {
            if (string.IsNullOrEmpty(_query)) return null;
            if (_queryParser == null)
                LoadImplementation(_query);
            return _queryParser.Visitor;
        }
    }
    
    private static void LoadImplementation(string inQuery)
    {
        _queryParser = new Query.QueryParser(inQuery);
    }
    
    public static ParserRuleContext GetTree(string inQuery)
    {
        if (_queryParser != null)
            LoadImplementation(inQuery);
        
        var parser = _queryParser;
        return parser.GetTree();
        
    }

}