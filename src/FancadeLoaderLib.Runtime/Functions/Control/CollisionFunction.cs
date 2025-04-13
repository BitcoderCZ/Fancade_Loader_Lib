using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Functions.Control;

public sealed class CollisionFunction : IActiveFunction
{
    private static readonly byte3 CollidedPos = TerminalDef.GetOutPosition(0, 2, 4);
    private static readonly byte3 SecondObjectPos = TerminalDef.GetOutPosition(1, 2, 4);
    private static readonly byte3 ImpulsePos = TerminalDef.GetOutPosition(2, 2, 4);
    private static readonly byte3 NormalPos = TerminalDef.GetOutPosition(3, 2, 4);

    private readonly RuntimeTerminal _firstObject;
    private int _secondObject;
    private float _impulse;
    private float3 _normal;

    public CollisionFunction(RuntimeTerminal firstObject)
    {
        _firstObject = firstObject;
    }

    public int Execute(byte3 terminalPos, IRuntimeContext context, Span<byte3> executeNext)
    {
        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(4), $"{nameof(terminalPos)} should be the before terminal.");

        int exeCount = 0;
        executeNext[exeCount++] = TerminalDef.AfterPosition;

        var firstObjectOutput = _firstObject.GetOutput(context);
        if (firstObjectOutput.IsConnected && context.TryGetCollision(firstObjectOutput.GetValue(context).Int, out int secondObject, out float impulse, out float3 normal))
        {
            _secondObject = secondObject;
            _impulse = impulse;
            _normal = normal;
            executeNext[exeCount++] = CollidedPos;
        }

        return exeCount;
    }

    public TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
        => terminalPos == SecondObjectPos
           ? new TerminalOutput(new RuntimeValue(_secondObject))
           : terminalPos == ImpulsePos
           ? new TerminalOutput(new RuntimeValue(_impulse))
           : terminalPos == NormalPos
           ? new TerminalOutput(new RuntimeValue(_normal))
           : throw new InvalidTerminalException(terminalPos);
}
