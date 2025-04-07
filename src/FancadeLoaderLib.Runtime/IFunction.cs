namespace FancadeLoaderLib.Runtime;

public interface IFunction
{
    TerminalOutput GetTerminalValue(string name, IRuntimeContext context);
}
