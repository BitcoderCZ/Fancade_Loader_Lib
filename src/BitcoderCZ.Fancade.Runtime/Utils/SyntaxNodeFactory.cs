using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Runtime.Syntax.Values;
using MathUtils.Vectors;
using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime.Utils;

internal static class SyntaxNodeFactory
{
    public static (LiteralExpressionSyntax Node, byte3 TerminalPosition) Literal(ushort3 position, float value)
        => (new LiteralExpressionSyntax(36, position, SignalType.Float, new RuntimeValue(value)), TerminalDef.GetOutPosition(0, 2, 1));

    public static (LiteralExpressionSyntax Node, byte3 TerminalPosition) Literal(ushort3 position, float3 value)
        => (new LiteralExpressionSyntax(38, position, SignalType.Vec3, new RuntimeValue(value)), TerminalDef.GetOutPosition(0, 2, 2));

    public static (LiteralExpressionSyntax Node, byte3 TerminalPosition) Literal(ushort3 position, Quaternion value)
        => (new LiteralExpressionSyntax(42, position, SignalType.Rot, new RuntimeValue(value)), TerminalDef.GetOutPosition(0, 2, 2));

    public static (LiteralExpressionSyntax Node, byte3 TerminalPosition) Literal(ushort3 position, bool value)
        => (new LiteralExpressionSyntax((ushort)(value ? 449 : 451), position, SignalType.Bool, new RuntimeValue(value)), TerminalDef.GetOutPosition(0, 2, 1));

    public static (LiteralExpressionSyntax Node, byte3 TerminalPosition) Literal(ushort3 position, FcObject value)
        => (new LiteralExpressionSyntax(0, position, SignalType.Bool, new RuntimeValue(value.Value)), byte3.Zero);

    public static (LiteralExpressionSyntax Node, byte3 TerminalPosition) Literal(ushort3 position, FcConstraint value)
        => (new LiteralExpressionSyntax(0, position, SignalType.Bool, new RuntimeValue(value.Value)), byte3.Zero);
}
