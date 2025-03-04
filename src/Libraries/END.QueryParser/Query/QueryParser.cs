using System.Diagnostics;
using Antlr4.Runtime;
using Antlr4.Query.mariadb;
using Antlr4.Query.plsql;
using Antlr4.Query.postresql;
using Antlr4.Query.sqlite;
using Antlr4.Query.tsql;
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace END.QueryParser.Query;

public class QueryParser : IQueryParser
{
    private readonly Parser _parser;
    private readonly ITokenSource _lexer;
    private readonly ITokenStream _tokens;
    private readonly ParserEnum _parserType;
    private readonly dynamic _visitor;

    public string ParserName => GetParserName();
    public Parser Parser => _parser;
    public dynamic Visitor => _visitor;
    
    [DebuggerStepThrough]
    private string GetParserName()
    {
        return _parserType switch
        {
            ParserEnum.SqLite => "sqlite",
            ParserEnum.MariaDb => "mariadb",
            ParserEnum.PostgreSQL => "postgresql",
            ParserEnum.Oracle => "oracle",
            ParserEnum.SqlServer => "sqlserver",
            _ => "sqlite"
        };
    }
    
    private static ITokenSource GetLexer(string inQuery, ParserEnum parserType)
    {
        var inputStream = new AntlrInputStream(inQuery);

        return parserType switch
        {
            ParserEnum.SqLite => new SQLiteLexer(inputStream),
            ParserEnum.MariaDb => new MariaDBLexer(inputStream),
            ParserEnum.PostgreSQL => new PostgreSQLLexer(inputStream),
            ParserEnum.Oracle => new PlSqlLexer(inputStream),
            ParserEnum.SqlServer => new TSqlLexer(inputStream),
            _ => new SQLiteLexer(inputStream)
        };
    }

    private static Parser GetParser(ITokenStream inToken, ParserEnum parserType)
    {
        return parserType switch
        {
            ParserEnum.SqLite => new SQLiteParser(inToken),
            ParserEnum.MariaDb => new MariaDBParser(inToken),
            ParserEnum.PostgreSQL => new PostgreSQLParser(inToken),
            ParserEnum.Oracle => new PlSqlParser(inToken),
            ParserEnum.SqlServer => new TSqlParser(inToken),
            _ => new SQLiteParser(inToken)
        };
    }

    private dynamic GetVisitor()
    {
        return _parserType switch
        {
            ParserEnum.SqLite => new SQLiteVisitor(),
            ParserEnum.MariaDb => new MariaDBVisitor(),
            ParserEnum.PostgreSQL => new PostgreSQLVisitor(),
            ParserEnum.Oracle => new PlSqlVisitor(),
            ParserEnum.SqlServer => new TSqlVisitor(),
            _ => new SQLiteVisitor()
        };
    }
    
    public QueryParser(string inQuery, ParserEnum parserType = ParserEnum.SqLite)
    {
        _parserType = parserType;
        _lexer = GetLexer(inQuery, parserType);
        _tokens = new CommonTokenStream(_lexer);
        _parser = GetParser(_tokens, parserType);
        _visitor = GetVisitor();
    }

    public ParserRuleContext GetTree()
    {
        _parser.BuildParseTree = true;

        return _parserType switch
        {
            ParserEnum.SqLite => ((SQLiteParser)_parser).sql_stmt_list(),
            ParserEnum.MariaDb => ((MariaDBParser)_parser).root(),
            ParserEnum.PostgreSQL => ((PostgreSQLParser)_parser).root(),
            ParserEnum.Oracle => ((PlSqlParser)_parser).sql_script(),
            ParserEnum.SqlServer => ((TSqlParser)_parser).tsql_file(),
            _ => ((SQLiteParser)_parser).sql_stmt_list()
        };
    }
}