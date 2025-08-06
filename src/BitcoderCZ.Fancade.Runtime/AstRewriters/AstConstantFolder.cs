using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Runtime.Exceptions;
using BitcoderCZ.Fancade.Runtime.Syntax;
using BitcoderCZ.Fancade.Runtime.Syntax.Math;
using BitcoderCZ.Fancade.Runtime.Syntax.Objects;
using BitcoderCZ.Fancade.Runtime.Syntax.Physics;
using BitcoderCZ.Fancade.Runtime.Syntax.Values;
using BitcoderCZ.Fancade.Runtime.Utils;
using BitcoderCZ.Maths.Vectors;
using System.Diagnostics;
using System.Numerics;
using static BitcoderCZ.Fancade.Runtime.Utils.SyntaxNodeFactory;

namespace BitcoderCZ.Fancade.Runtime.AstRewriters;

internal sealed class AstConstantFolder : AstRewriter
{
    protected override (SyntaxNode Node, byte3 TerminalPosition) RewriteGetPositionExpression(GetPositionExpressionSyntax node, byte3 terminalPos)
    {
        var newNode = (GetPositionExpressionSyntax)base.RewriteGetPositionExpression(node, terminalPos).Node;

        return !IsNullOrZero(newNode.Object)
            ? (newNode, terminalPos)
            : terminalPos == TerminalDef.GetOutPosition(0, 2, 2)
            ? Literal(node.Position, Vector3.Zero)
            : terminalPos == TerminalDef.GetOutPosition(1, 2, 2)
            ? Literal(node.Position, Quaternion.Identity)
            : throw new InvalidTerminalException(terminalPos);
    }

    protected override (SyntaxNode Node, byte3 TerminalPosition) RewriteGetSizeExpression(GetSizeExpressionSyntax node, byte3 terminalPos)
    {
        var newNode = (GetSizeExpressionSyntax)base.RewriteGetSizeExpression(node, terminalPos).Node;

        return !IsNullOrZero(newNode.Object)
          ? (newNode, terminalPos)
          : terminalPos == TerminalDef.GetOutPosition(0, 2, 2) || terminalPos == TerminalDef.GetOutPosition(1, 2, 2)
          ? Literal(node.Position, 0f)
          : throw new InvalidTerminalException(terminalPos);
    }

    protected override (SyntaxNode Node, byte3 TerminalPosition) RewriteGetVelocityExpression(GetVelocityExpressionSyntax node, byte3 terminalPos)
    {
        var newNode = (GetVelocityExpressionSyntax)base.RewriteGetVelocityExpression(node, terminalPos).Node;

        return !IsNullOrZero(newNode.Object)
          ? (newNode, terminalPos)
          : terminalPos == TerminalDef.GetOutPosition(0, 2, 2) || terminalPos == TerminalDef.GetOutPosition(1, 2, 2)
          ? Literal(node.Position, Vector3.Zero)
          : throw new InvalidTerminalException(terminalPos);
    }

    protected override (SyntaxNode Node, byte3 TerminalPosition) RewriteUnaryExpression(UnaryExpressionSyntax node, byte3 terminalPos)
    {
        var newNode = (UnaryExpressionSyntax)base.RewriteUnaryExpression(node, terminalPos).Node;

        return !TryGetValue(newNode.Input, out var input)
            ? ((SyntaxNode Node, byte3 TerminalPosition))(newNode, terminalPos)
            : newNode.PrefabId switch
            {
                90 => Literal(newNode.Position, -input.Float),
                144 => Literal(newNode.Position, !input.Bool),
                440 => Literal(newNode.Position, Quaternion.Inverse(input.Quaternion)),
                413 => Literal(newNode.Position, MathF.Sin(input.Float)),
                453 => Literal(newNode.Position, MathF.Cos(input.Float)),
                184 => Literal(newNode.Position, MathF.Round(input.Float)),
                186 => Literal(newNode.Position, MathF.Floor(input.Float)),
                188 => Literal(newNode.Position, MathF.Ceiling(input.Float)),
                455 => Literal(newNode.Position, MathF.Abs(input.Float)),
                578 => Literal(newNode.Position, Vector3.Normalize(input.Float3)),
                _ => throw new UnreachableException(),
            };
    }

    /*protected override (SyntaxNode Node, byte3 TerminalPosition) RewriteBinaryExpression(BinaryExpressionSyntax node, byte3 terminalPos)
    {
        var newNode = (BinaryExpressionSyntax)base.RewriteBinaryExpression(node, terminalPos).Node;

        if (!TryGetValue(node.Input1, out var input1) || !TryGetValue(node.Input2, out var input2))
        {
            return (newNode, terminalPos);
        }

        return newNode.PrefabId switch
        {
            146 => Literal(newNode.Position, input1.Bool && input2.Bool),
            417 => Literal(newNode.Position, input1.Bool || input2.Bool),
            92 => Literal(newNode.Position, input1.Float + input2.Float),
            96 => Literal(newNode.Position, input1.Float3 + input2.Float3),
            100 => Literal(newNode.Position, input1.Float - input2.Float),
            104 => Literal(newNode.Position, input1.Float3 - input2.Float3),
            108 => Literal(newNode.Position, input1.Float * input2.Float),
            112 => Literal(newNode.Position, input1.Float3 * input2.Float),
            116 => Literal(newNode.Position, Vector3.Transform(input1.Float3.ToNumerics(), input2.Quaternion).ToFloat3()),
            120 => Literal(newNode.Position, input1.Quaternion * input2.Quaternion),
            124 => Literal(newNode.Position, input1.Float / input2.Float),
            172 => Literal(newNode.Position, FcMod(input1.Float, input2.Float)),
            457 => Literal(newNode.Position, MathF.Pow(input1.Float, input2.Float)),
            132 => Literal(newNode.Position, MathF.Abs(input1.Float - input2.Float) < Constants.EqualsNumbersMaxDiff), // equals numbers
            136 => Literal(newNode.Position, (input1.Float3 - input2.Float3).LengthSquared < Constants.EqualsVectorsMaxDiff), // equals vectors
            140 => Literal(newNode.Position, input1.Int == input2.Int), // equals objects
            421 => Literal(newNode.Position, input1.Bool == input2.Bool), // equals bools
            128 => Literal(newNode.Position, input1.Float < input2.Float),
            481 => Literal(newNode.Position, input1.Float > input2.Float),
            176 => Literal(newNode.Position, MathF.Min(input1.Float, input2.Float)),
            180 => Literal(newNode.Position, MathF.Max(input1.Float, input2.Float)),
            580 => Literal(newNode.Position, MathF.Log(input1.Float, input2.Float)),
            570 => Literal(newNode.Position, Vector3.Dot(input1.Float3, input2.Float3)),
            574 => Literal(newNode.Position, Vector3.Cross(input1.Float3, input2.Float3)),
            190 => Literal(newNode.Position, (input1.Float3 - input2.Float3).Length),
            200 => Literal(newNode.Position, QuaternionUtils.AxisAngle(input1.Float3.ToNumerics(), input2.Float)),
            204 => Literal(newNode.Position, QuaternionUtils.LookRotation(input1.Float3.ToNumerics(), newNode.Input2 is null ? Vector3.UnitY : input2.Float3.ToNumerics())),
            _ => (newNode, terminalPos),
        };
        float FcMod(float a, float b)
        {
            float res = a % b;

            return res >= 0f ? res : b + res;
        }
    }*/

    protected override (SyntaxNode Node, byte3 TerminalPosition) RewriteLerpExpression(LerpExpressionSyntax node, byte3 terminalPos)
    {
        var newNode = (LerpExpressionSyntax)base.RewriteLerpExpression(node, terminalPos).Node;

        return TryGetValue(newNode.From, out var from) && TryGetValue(newNode.To, out var to) && TryGetValue(newNode.Amount, out var amount)
            ? Literal(newNode.Position, Quaternion.Lerp(from.Quaternion, to.Quaternion, amount.Float))
            : (newNode, terminalPos);
    }

    protected override (SyntaxNode Node, byte3 TerminalPosition) RewriteLineVsPlaneExpression(LineVsPlaneExpressionSyntax node, byte3 terminalPos)
    {
        var newNode = (LineVsPlaneExpressionSyntax)base.RewriteLineVsPlaneExpression(node, terminalPos).Node;

        if (TryGetValue(newNode.LineFrom, out var lineFromVal) && TryGetValue(newNode.LineTo, out var lineToVal) && TryGetValue(newNode.PlanePoint, out var planePointVal) && TryGetValue(newNode.PlaneNormal, out var planeNormalVal))
        {
            var lineFrom = lineFromVal.Float3.ToNumerics();
            var lineTo = lineToVal.Float3.ToNumerics();
            var planePoint = planePointVal.Float3.ToNumerics();
            var planeNormal = planeNormalVal.Float3.ToNumerics();

            float t = Vector3.Dot(planePoint - lineFrom, planeNormal) / Vector3.Dot(lineTo - lineFrom, planeNormal);

            return Literal(newNode.Position, (lineFrom + (t * (lineTo - lineFrom))).ToFloat3());
        }
        else
        {
            return (newNode, terminalPos);
        }
    }

    protected override (SyntaxNode Node, byte3 TerminalPosition) RewriteMakeVecRotExpression(MakeVecRotExpressionSyntax node, byte3 terminalPos)
    {
        var newNode = (MakeVecRotExpressionSyntax)base.RewriteMakeVecRotExpression(node, terminalPos).Node;

        return TryGetValue(newNode.X, out var x) && TryGetValue(newNode.Y, out var y) && TryGetValue(newNode.Z, out var z)
            ? newNode.PrefabId switch
            {
                150 => Literal(newNode.Position, new Vector3(x.Float, y.Float, z.Float)),
                162 => Literal(newNode.Position, Quaternion.CreateFromYawPitchRoll(x.Float, y.Float, z.Float)),
                _ => throw new UnreachableException(),
            }
            : (newNode, terminalPos);
    }

    protected override (SyntaxNode Node, byte3 TerminalPosition) RewriteBreakVecRotExpression(BreakVecRotExpressionSyntax node, byte3 terminalPos)
    {
        var newNode = (BreakVecRotExpressionSyntax)base.RewriteBreakVecRotExpression(node, terminalPos).Node;

        if (TryGetValue(newNode.VecRot, out var vecRot))
        {
            float val;
            switch (newNode.PrefabId)
            {
                case 156:
                    var vec = vecRot.Float3;
                    if (terminalPos == TerminalDef.GetOutPosition(0, 2, 3))
                    {
                        val = vec.X;
                    }
                    else if (terminalPos == TerminalDef.GetOutPosition(1, 2, 3))
                    {
                        val = vec.Y;
                    }
                    else if (terminalPos == TerminalDef.GetOutPosition(2, 2, 3))
                    {
                        val = vec.Z;
                    }
                    else
                    {
                        throw new InvalidTerminalException(terminalPos);
                    }

                    break;
                case 442:
                    var rot = vecRot.Quaternion;
                    if (terminalPos == TerminalDef.GetOutPosition(0, 2, 3))
                    {
                        float pitchSin = 2.0f * ((rot.W * rot.Y) - (rot.Z * rot.X));

                        if (pitchSin > 1.0f)
                        {
                            val = MathF.PI / 2; // 90 degrees
                        }
                        else if (pitchSin < -1.0f)
                        {
                            val = -MathF.PI / 2; // -90 degrees
                        }
                        else
                        {
                            val = MathF.Asin(pitchSin);
                        }
                    }
                    else if (terminalPos == TerminalDef.GetOutPosition(1, 2, 3))
                    {
                        float xx = rot.X * rot.X;
                        float yy = rot.Y * rot.Y;
                        float zz = rot.Z * rot.Z;
                        float ww = rot.W * rot.W;

                        val = MathF.Atan2(2.0f * ((rot.Y * rot.Z) + (rot.W * rot.X)), ww + xx - yy - zz);
                    }
                    else if (terminalPos == TerminalDef.GetOutPosition(2, 2, 3))
                    {
                        float xx = rot.X * rot.X;
                        float yy = rot.Y * rot.Y;
                        float zz = rot.Z * rot.Z;
                        float ww = rot.W * rot.W;

                        val = MathF.Atan2(2.0f * ((rot.X * rot.Y) + (rot.W * rot.Z)), ww - xx - yy + zz);
                    }
                    else
                    {
                        throw new InvalidTerminalException(terminalPos);
                    }

                    val *= 180f / MathF.PI; // rad to deg
                    break;
                default:
                    throw new UnreachableException();
            }

            return Literal(newNode.Position, val);
        }
        else
        {
            return (newNode, terminalPos);
        }
    }

    private static bool IsNullOrZero(SyntaxTerminal? terminal)
        => terminal is null || (terminal.Node is LiteralExpressionSyntax literal && literal.Value == RuntimeValue.Zero);

    private static bool TryGetValue(SyntaxTerminal? terminal, out RuntimeValue value)
    {
        if (terminal is null)
        {
            value = RuntimeValue.Zero;
            return true;
        }
        else if (terminal.Node is LiteralExpressionSyntax literal)
        {
            value = literal.Value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }
}
