namespace FancadeLoaderLib.Runtime;

public readonly struct RuntimeTerminal
{
    public RuntimeTerminal(IFunction? function, string name)
    {
        Function = function;
        Name = name;
    }

    public readonly IFunction? Function { get; }

    public readonly string Name { get; }

    public readonly TerminalOutput GetOutput(IRuntimeContext context)
        => Function is null ? TerminalOutput.Disconnected : Function.GetTerminalValue(Name, context);
}
