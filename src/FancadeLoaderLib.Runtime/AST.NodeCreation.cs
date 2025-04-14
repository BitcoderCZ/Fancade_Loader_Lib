using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Syntax;
using FancadeLoaderLib.Runtime.Syntax.Game;
using FancadeLoaderLib.Runtime.Syntax.Math;
using FancadeLoaderLib.Runtime.Syntax.Values;
using FancadeLoaderLib.Runtime.Syntax.Variables;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Numerics;

namespace FancadeLoaderLib.Runtime;

public sealed partial class AST
{
    private static class NodeCreation
    {
        // TODO: source generator
        public static SyntaxNode? CreateNode(ushort id, ushort3 pos, ParseContext ctx)
        {
            switch (id)
            {
                // **************************************** Game ****************************************
                case 564:
                    return new CurrentFrameExpressionSyntax(id, pos);

                //// **************************************** Control ****************************************
                //case 234:
                //    return new IfFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                //case 238:
                //    return new PlaySensorFunction();
                //case 566:
                //    return new LateUpdateFunction();
                //case 409:
                //    return new BoxArtSensorFunction();
                //case 242:
                //    {
                //        return new TouchSensorFunction(ctx.TryGetSettingOfType(pos, 0, SettingType.Byte, out object? state) ? (TouchState)(byte)state : TouchState.Touching, ctx.TryGetSettingOfType(pos, 1, SettingType.Byte, out object? fingerIndex) ? (byte)fingerIndex : 0);
                //    }

                //case 248:
                //    return new SwipeSensorFunction();
                //case 588:
                //    {
                //        return new ButtonFunction(ctx.TryGetSettingOfType(pos, 0, SettingType.Byte, out object? state) ? (ButtonType)(byte)state : ButtonType.Direction);
                //    }

                //case 592:
                //    {
                //        return new JoystickFunction(ctx.TryGetSettingOfType(pos, 0, SettingType.Byte, out object? state) ? (JoystickType)(byte)state : JoystickType.XZ);
                //    }

                //case 401:
                //    return new CollisionFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 4)));
                //case 560:
                //    return new LoopFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));

                //// **************************************** Math ****************************************
                case 90 or 144 or 440 or 413 or 453 or 184 or 186 or 188 or 455 or 578:
                    return new UnaryExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                case 92 or 96 or 100 or 104 or 108 or 112 or 116 or 120 or 124 or 172 or 457 or 132 or 136 or 140 or 421 or 146 or 417 or 128 or 481 or 168 or 176 or 180 or 580 or 570 or 574 or 190 or 200 or 204:
                    return new BinaryExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));

                case 485:
                    return new RandomSeedStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 194:
                    return new LerpExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 216:
                    return new ScreenToWorldExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 477:
                    return new WorldToScreenExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 208:
                    return new LineVsPlaneExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 4)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 4)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 4)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(3, 4)));
                case 150:
                    return new MakeVecRotExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 156:
                    return new BreakVecRotExpressionnSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)));
                case 162:
                    return new MakeVecRotExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 442:
                    return new BreakVecRotExpressionnSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)));

                // **************************************** Value ****************************************
                case 16 or 20 or 24 or 28 or 32:
                    return new InspectStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), (SignalType)(((id - 16) / 2) + 2), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 36:
                    {
                        return new LiteralExpressionSyntax(id, pos, SignalType.Float, new RuntimeValue(ctx.TryGetSettingOfType(pos, 0, SettingType.Float, out object? value) ? (float)value : 0f));
                    }

                case 38: // vec3
                    {
                        return new LiteralExpressionSyntax(id, pos, SignalType.Vec3, new RuntimeValue(ctx.TryGetSettingOfType(pos, 0, SettingType.Vec3, out object? value) ? (float3)value : float3.Zero));
                    }

                case 42: // rot
                    {
                        return new LiteralExpressionSyntax(id, pos, SignalType.Rot, new RuntimeValue(ctx.TryGetSettingOfType(pos, 0, SettingType.Vec3, out object? value) ? ((float3)value).ToQuatDeg() : Quaternion.Identity));
                    }

                case 449:
                    return new LiteralExpressionSyntax(id, pos, SignalType.Bool, new RuntimeValue(true));
                case 451:
                    return new LiteralExpressionSyntax(id, pos, SignalType.Bool, new RuntimeValue(false));

                // **************************************** Variables ****************************************
                case 46:
                    {
                        return new GetVariableExpressionSyntax(id, pos, new Variable(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Float));
                    }

                case 48:
                    {
                        return new GetVariableExpressionSyntax(id, pos, new Variable(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Vec3));
                    }

                case 50:
                    {
                        return new GetVariableExpressionSyntax(id, pos, new Variable(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Rot));
                    }

                case 52:
                    {
                        return new GetVariableExpressionSyntax(id, pos, new Variable(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Bool));
                    }

                case 54:
                    {
                        return new GetVariableExpressionSyntax(id, pos, new Variable(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Obj));
                    }

                case 56:
                    {
                        return new GetVariableExpressionSyntax(id, pos, new Variable(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Con));
                    }

                case 428:
                    {
                        return new SetVaribleStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), new Variable(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Float), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                    }

                case 430:
                    {
                        return new SetVaribleStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), new Variable(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Vec3), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                    }

                case 432:
                    {
                        return new SetVaribleStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), new Variable(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Rot), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                    }

                case 434:
                    {
                        return new SetVaribleStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), new Variable(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Bool), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                    }

                case 436:
                    {
                        return new SetVaribleStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), new Variable(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Obj), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                    }

                case 438:
                    {
                        return new SetVaribleStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), new Variable(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Con), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                    }

                //case 58 or 62 or 66 or 70 or 74 or 78:
                //    return new SetPointerFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));

                //case 82 or 461 or 465 or 469 or 86 or 473:
                //    return new ListFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));

                //case 556:
                //    return new IncreaseNumberFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                //case 558:
                //    return new DecreaseNumberFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));

                default:
                    throw new NotImplementedException($"Prefab with id {id} is not yet implemented.");
            }
        }
    }
}