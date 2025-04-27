using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting.Settings;
using FancadeLoaderLib.Runtime.Syntax;
using FancadeLoaderLib.Runtime.Syntax.Control;
using FancadeLoaderLib.Runtime.Syntax.Game;
using FancadeLoaderLib.Runtime.Syntax.Math;
using FancadeLoaderLib.Runtime.Syntax.Objects;
using FancadeLoaderLib.Runtime.Syntax.Physics;
using FancadeLoaderLib.Runtime.Syntax.Sound;
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
        public static SyntaxNode? CreateNode(ushort id, ushort3 pos, ParseContext ctx)
        {
            switch (id)
            {
                // **************************************** Game ****************************************
                case 252:
                    {
                        return new WinStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.TryGetSettingOfType(pos, 0, SettingType.Byte, out object? delay) ? (byte)delay : 0);
                    }

                case 256:
                    {
                        return new LoseStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.TryGetSettingOfType(pos, 0, SettingType.Byte, out object? delay) ? (byte)delay : 0);
                    }

                case 260:
                    {
                        return new SetScoreStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)), ctx.TryGetSettingOfType(pos, 0, SettingType.Byte, out object? delay) ? (Ranking)(byte)delay : Ranking.MostPoints);
                    }

                case 268:
                    {
                        return new SetCameraStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)), ctx.TryGetSettingOfType(pos, 0, SettingType.Byte, out object? delay) && (byte)delay != 0);
                    }

                case 274:
                    return new SetLightStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 220:
                    return new ScreenSizeExpressionSyntax(id, pos);
                case 224:
                    return new AccelerometerExpressionSyntax(id, pos);
                case 564:
                    return new CurrentFrameExpressionSyntax(id, pos);
                case 584:
                    {
                        return new MenuItemStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)), ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? name) ? (string)name : string.Empty, ctx.TryGetSettingOfType(pos, 1, SettingType.Byte, out object? maxItems) ? new MaxBuyCount((byte)maxItems) : MaxBuyCount.OnOff, ctx.TryGetSettingOfType(pos, 2, SettingType.Byte, out object? priceIncrease) ? (PriceIncrease)(byte)priceIncrease : PriceIncrease.Fixed10);
                    }

                // **************************************** Objects ****************************************
                case 278:
                    return new GetPositionExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 282:
                    return new SetPositionStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 228:
                    return new RaycastExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)));
                case 489:
                    return new GetSizeExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 306:
                    return new SetVisibleStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 316:
                    return new CreateObjectStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 320:
                    return new DestroyObjectStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));

                // **************************************** Sound ****************************************
                case 264:
                    {
                        return new PlaySoundStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)), ctx.TryGetSettingOfType(pos, 2, SettingType.Byte, out object? sound) ? (FcSound)(byte)sound : FcSound.Chirp);
                    }

                case 397:
                    return new StopSoundStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 391:
                    return new VolumePitchStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));

                // **************************************** Physics ****************************************
                case 298:
                    return new AddForceStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 4)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 4)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 4)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(3, 4)));
                case 288:
                    return new GetVelocityExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 292:
                    return new SetVelocityStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 310:
                    return new SetLockedStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 328:
                    return new SetMassStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 332:
                    return new SetFrictionStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 336:
                    return new SetBouncinessStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 324:
                    return new SetGravityStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 340:
                    return new AddConstraintStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 346:
                    return new LinearLimitsStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 352:
                    return new AngularLimitsStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 358:
                    return new LinearSpringStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 364:
                    return new AngularSpringStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 370:
                    return new LinearMotorStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 376:
                    return new AngularMotorStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));

                // **************************************** Control ****************************************
                case 234:
                    return new IfStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 238:
                    return new PlaySensorStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos));
                case 566:
                    return new LateUpdateStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos));
                case 409:
                    return new BoxArtStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos));
                case 242:
                    {
                        return new TouchSensorStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.TryGetSettingOfType(pos, 0, SettingType.Byte, out object? state) ? (TouchState)(byte)state : TouchState.Touching, ctx.TryGetSettingOfType(pos, 1, SettingType.Byte, out object? fingerIndex) ? (byte)fingerIndex : 0);
                    }

                case 248:
                    return new SwipeSensorStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos));
                case 588:
                    {
                        return new ButtonStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.TryGetSettingOfType(pos, 0, SettingType.Byte, out object? state) ? (ButtonType)(byte)state : ButtonType.Direction);
                    }

                case 592:
                    {
                        return new JoystickStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.TryGetSettingOfType(pos, 0, SettingType.Byte, out object? state) ? (JoystickType)(byte)state : JoystickType.XZ);
                    }

                case 401:
                    return new CollisionStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 4)));
                case 560:
                    return new LoopStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));

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
                    return new BreakVecRotExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)));
                case 162:
                    return new MakeVecRotExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 442:
                    return new BreakVecRotExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)));

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

                case 58 or 62 or 66 or 70 or 74 or 78:
                    return new SetPointerStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));

                case 82 or 461 or 465 or 469 or 86 or 473:
                    return new ListExpressionSyntax(id, pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));

                case 556 or 558:
                    return new IncDecNumberStatementSyntax(id, pos, ctx.GetOutVoidConnections(pos), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));

                default:
                    throw new NotImplementedException($"Prefab with id {id} is not yet implemented.");
            }
        }
    }
}