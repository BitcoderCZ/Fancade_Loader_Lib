namespace FancadeLoaderLib.Runtime;

public interface IActiveFunction : IFunction
{
    int Execute(IRuntimeContext context, Span<string> executeNext);
}
