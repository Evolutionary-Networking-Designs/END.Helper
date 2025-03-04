using Antlr4.Runtime;

namespace END.QueryParser.Query;

public interface IQueryParser
{
    public ParserRuleContext GetTree();
}