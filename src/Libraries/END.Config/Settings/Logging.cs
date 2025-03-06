// ReSharper disable CheckNamespace
// ReSharper disable ConvertConstructorToMemberInitializers
// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
namespace END.Config;

public class Logging
{
    public LogLevel LogLevel { get; set; }

    public Logging()
    {
        LogLevel = new();
    }
}