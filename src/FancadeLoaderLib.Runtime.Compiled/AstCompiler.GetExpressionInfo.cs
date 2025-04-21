using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using FancadeLoaderLib.Runtime.Syntax;
using FancadeLoaderLib.Runtime.Syntax.Control;
using FancadeLoaderLib.Runtime.Syntax.Game;
using FancadeLoaderLib.Runtime.Syntax.Math;
using FancadeLoaderLib.Runtime.Syntax.Values;
using FancadeLoaderLib.Runtime.Syntax.Variables;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Compiled;

public partial class AstCompiler
{
    private static ExpressionInfo GetExpressionInfo(SyntaxTerminal terminal, bool asReference)
    {
        // faster than switching on type
        switch (terminal.Node.PrefabId)
        {
            // **************************************** Game ****************************************
            case 220:
                {
                    Debug.Assert(terminal.Node is ScreenSizeExpressionSyntax, $"{nameof(terminal)}.{nameof(terminal.Node)} should be {nameof(ScreenSizeExpressionSyntax)}");

                    return new ExpressionInfo(SignalType.Float);
                }

            case 224:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is AccelerometerExpressionSyntax, $"{nameof(terminal)}.{nameof(terminal.Node)} should be {nameof(AccelerometerExpressionSyntax)}");

                    return new ExpressionInfo(SignalType.Vec3);
                }

            case 564:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is CurrentFrameExpressionSyntax, $"{nameof(terminal)}.{nameof(terminal.Node)} should be {nameof(CurrentFrameExpressionSyntax)}");

                    return new ExpressionInfo(SignalType.Float);
                }

            // **************************************** Control ****************************************
            case 242:
                {
                    Debug.Assert(terminal.Node is TouchSensorStatementSyntax);
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(1, 2, 3) || terminal.Position == TerminalDef.GetOutPosition(2, 2, 3));

                    return new ExpressionInfo(SignalType.Float);
                }

            case 248:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is SwipeSensorStatementSyntax);

                    return new ExpressionInfo(SignalType.Vec3);
                }

            case 592:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is JoystickStatementSyntax);

                    return new ExpressionInfo(SignalType.Vec3);
                }

            case 401:
                {
                    Debug.Assert(terminal.Node is CollisionStatementSyntax);

                    return terminal.Position == TerminalDef.GetOutPosition(1, 2, 4)
                        ? new ExpressionInfo(SignalType.Obj)
                        : terminal.Position == TerminalDef.GetOutPosition(2, 2, 4)
                        ? new ExpressionInfo(SignalType.Float)
                        : terminal.Position == TerminalDef.GetOutPosition(3, 2, 4)
                        ? new ExpressionInfo(SignalType.Vec3)
                        : throw new InvalidTerminalException(terminal.Position);
                }

            case 560:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is LoopStatementSyntax);

                    return new ExpressionInfo(SignalType.Float);
                }

            // **************************************** Math ****************************************
            case 90 or 144 or 440 or 413 or 453 or 184 or 186 or 188 or 455 or 578:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is UnaryExpressionSyntax);

                    var outType = terminal.Node.PrefabId switch
                    {
                        90 => SignalType.Float,
                        144 => SignalType.Bool,
                        440 => SignalType.Rot,
                        413 => SignalType.Float,
                        453 => SignalType.Float,
                        184 => SignalType.Float,
                        186 => SignalType.Float,
                        188 => SignalType.Float,
                        455 => SignalType.Float,
                        578 => SignalType.Vec3,
                        _ => throw new UnreachableException(),
                    };
                    return new ExpressionInfo(outType);
                }

            case 92 or 96 or 100 or 104 or 108 or 112 or 116 or 120 or 124 or 172 or 457 or 132 or 136 or 140 or 421 or 146 or 417 or 128 or 481 or 168 or 176 or 180 or 580 or 570 or 574 or 190 or 200 or 204:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is BinaryExpressionSyntax);

                    var outType = terminal.Node.PrefabId switch
                    {
                        146 => SignalType.Bool,
                        417 => SignalType.Bool,
                        92 => SignalType.Float,
                        96 => SignalType.Vec3,
                        100 => SignalType.Float,
                        104 => SignalType.Vec3,
                        108 => SignalType.Float,
                        112 => SignalType.Vec3,
                        116 => SignalType.Vec3,
                        120 => SignalType.Rot,
                        124 => SignalType.Float,
                        172 => SignalType.Float,
                        457 => SignalType.Float,
                        132 => SignalType.Bool,
                        136 => SignalType.Bool,
                        140 => SignalType.Bool,
                        421 => SignalType.Bool,
                        128 => SignalType.Bool,
                        481 => SignalType.Bool,
                        168 => SignalType.Float,
                        176 => SignalType.Float,
                        180 => SignalType.Float,
                        580 => SignalType.Float,
                        570 => SignalType.Float,
                        574 => SignalType.Vec3,
                        190 => SignalType.Vec3,
                        200 => SignalType.Rot,
                        204 => SignalType.Rot,
                        _ => throw new UnreachableException(),
                    };
                    return new ExpressionInfo(outType);
                }

            case 194:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is LerpExpressionSyntax);

                    return new ExpressionInfo(SignalType.Rot);
                }

            case 216:
                {
                    Debug.Assert(terminal.Node is ScreenToWorldExpressionSyntax);

                    return new ExpressionInfo(SignalType.Vec3);
                }

            case 477:
                {
                    Debug.Assert(terminal.Node is WorldToScreenExpressionSyntax);

                    return new ExpressionInfo(SignalType.Float);
                }

            case 208:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 4), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is LineVsPlaneExpressionSyntax);

                    return new ExpressionInfo(SignalType.Vec3);
                }

            case 150 or 162:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var makeVecRot = (MakeVecRotExpressionSyntax)terminal.Node;

                    return makeVecRot.PrefabId switch
                    {
                        150 => new ExpressionInfo(SignalType.Vec3),
                        162 => new ExpressionInfo(SignalType.Rot),
                        _ => throw new UnreachableException(),
                    };
                }

            case 156 or 442:
                {
                    Debug.Assert(terminal.Node is BreakVecRotExpressionnSyntax);

                    return new ExpressionInfo(SignalType.Float);
                }

            // **************************************** Value ****************************************
            case 36 or 38 or 42 or 449 or 451:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, terminal.Node.PrefabId is 38 or 42 ? 2 : 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is LiteralExpressionSyntax);
                    Debug.Assert(!asReference);

                    return terminal.Node.PrefabId switch
                    {
                        36 => new ExpressionInfo(SignalType.Float),
                        38 => new ExpressionInfo(SignalType.Vec3),
                        42 => new ExpressionInfo(SignalType.Rot),
                        449 or 451 => new ExpressionInfo(SignalType.Bool),
                        _ => throw new UnreachableException(),
                    };
                }

            // **************************************** Variables ****************************************
            case 46 or 48 or 50 or 52 or 54 or 56:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var getVariable = (GetVariableExpressionSyntax)terminal.Node;

                    return new ExpressionInfo(getVariable.Variable);
                }

            case 82 or 461 or 465 or 469 or 86 or 473:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var list = (ListExpressionSyntax)terminal.Node;

                    var type = terminal.Node.PrefabId switch
                    {
                        82 => SignalType.Float,
                        461 => SignalType.Vec3,
                        465 => SignalType.Rot,
                        469 => SignalType.Bool,
                        86 => SignalType.Obj,
                        473 => SignalType.Con,
                        _ => throw new UnreachableException(),
                    };

                    if (list.Variable is null)
                    {
                        return new ExpressionInfo(type);
                    }

                    if (list.Index is null)
                    {
                        return GetExpressionInfo(list.Variable, asReference);
                    }
                    else if (list.Variable.Node is GetVariableExpressionSyntax getVariable)
                    {
                        return new ExpressionInfo(getVariable.Variable);
                    }

                    var varInfo = GetExpressionInfo(list.Variable, true);

                    return varInfo;
                }

            default:
                throw new NotImplementedException($"Prefab with id {terminal.Node.PrefabId} is not implemented.");
        }
    }
}
