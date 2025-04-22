using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Compiled.Utils;
using FancadeLoaderLib.Runtime.Exceptions;
using FancadeLoaderLib.Runtime.Syntax;
using FancadeLoaderLib.Runtime.Syntax.Control;
using FancadeLoaderLib.Runtime.Syntax.Game;
using FancadeLoaderLib.Runtime.Syntax.Math;
using FancadeLoaderLib.Runtime.Syntax.Objects;
using FancadeLoaderLib.Runtime.Syntax.Values;
using FancadeLoaderLib.Runtime.Syntax.Variables;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Compiled;

public partial class AstCompiler
{
    private ExpressionInfo WriteExpressionOrDefault(SyntaxTerminal? terminal, SignalType type, Environment environment, IndentedTextWriter writer)
    {
        if (terminal is null)
        {
            writer.Write(GetDefaultValue(type));

            return new ExpressionInfo(type);
        }

        return WriteExpression(terminal, type.IsPointer(), environment, writer);
    }

    private ExpressionInfo WriteExpression(SyntaxTerminal terminal, bool asReference, Environment environment, IndentedTextWriter writer)
        => WriteExpression(terminal, asReference, environment, false, writer);

    private ExpressionInfo WriteExpression(SyntaxTerminal terminal, bool asReference, Environment environment, bool direct, IndentedTextWriter writer)
    {
        if (!direct)
        {
            int conFromCount = 0;

            foreach (var con in environment.AST.ConnectionsFrom[terminal.Node.Position])
            {
                if (con.FromVoxel == terminal.Position)
                {
                    conFromCount++;
                }
            }

            if (conFromCount > 1)
            {
                var entryPoint = new EntryPoint(environment.Index, terminal.Node.Position, terminal.Position);
                writer.WriteInv($"{GetEntryPointMethodName(entryPoint, asReference)}()");
                ExpressionInfo info = GetExpressionInfo(terminal, asReference, environment);
                _nodesToWrite.Enqueue((terminal, environment.Index, asReference ? info.PtrType : info.Type));
                return info;
            }
        }

        // faster than switching on type
        switch (terminal.Node.PrefabId)
        {
            // **************************************** Game ****************************************
            case 220:
                {
                    Debug.Assert(terminal.Node is ScreenSizeExpressionSyntax, $"{nameof(terminal)}.{nameof(terminal.Node)} should be {nameof(ScreenSizeExpressionSyntax)}");

                    writer.Write("_ctx.ScreenSize");

                    if (terminal.Position == TerminalDef.GetOutPosition(0, 2, 2))
                    {
                        writer.Write(".X");
                    }
                    else if (terminal.Position == TerminalDef.GetOutPosition(1, 2, 2))
                    {
                        writer.Write(".Y");
                    }
                    else
                    {
                        throw new InvalidTerminalException(terminal.Position);
                    }

                    return new ExpressionInfo(SignalType.Float);
                }

            case 224:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is AccelerometerExpressionSyntax, $"{nameof(terminal)}.{nameof(terminal.Node)} should be {nameof(AccelerometerExpressionSyntax)}");

                    writer.Write("_ctx.Accelerometer");
                    return new ExpressionInfo(SignalType.Vec3);
                }
            case 564:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is CurrentFrameExpressionSyntax, $"{nameof(terminal)}.{nameof(terminal.Node)} should be {nameof(CurrentFrameExpressionSyntax)}");

                    writer.Write("(float)_ctx.CurrentFrame");

                    return new ExpressionInfo(SignalType.Float);
                }

            // **************************************** Objects ****************************************
            case 278:
                {
                    var getPosition = (GetPositionExpressionSyntax)terminal.Node;

                    if (getPosition.Object is null)
                    {
                        if (terminal.Position == TerminalDef.GetOutPosition(0, 2, 2))
                        {
                            writer.Write(GetDefaultValue(SignalType.Float));
                            return new ExpressionInfo(SignalType.Float);
                        }
                        else if (terminal.Position == TerminalDef.GetOutPosition(1, 2, 2))
                        {
                            writer.Write(GetDefaultValue(SignalType.Rot));
                            return new ExpressionInfo(SignalType.Rot);
                        }
                        else
                        {
                            throw new InvalidTerminalException(terminal.Position);
                        }
                    }
                    else
                    {
                        writer.Write("_ctx.GetObjectPosition(");

                        WriteExpression(getPosition.Object, false, environment, writer);

                        writer.Write(')');

                        if (terminal.Position == TerminalDef.GetOutPosition(0, 2, 2))
                        {
                            writer.Write(".Position");
                            return new ExpressionInfo(SignalType.Float);
                        }
                        else if (terminal.Position == TerminalDef.GetOutPosition(1, 2, 2))
                        {
                            writer.Write(".Rotation");
                            return new ExpressionInfo(SignalType.Rot);
                        }
                        else
                        {
                            throw new InvalidTerminalException(terminal.Position);
                        }
                    }
                }

            case 228:
                {
                    var raycast = (RaycastExpressionSyntax)terminal.Node;

                    writer.Write("_ctx.Raycast(");
                    WriteExpressionOrDefault(raycast.From, SignalType.Vec3, environment, writer);
                    writer.Write(", ");
                    WriteExpressionOrDefault(raycast.To, SignalType.Vec3, environment, writer);
                    writer.Write(')');

                    if (terminal.Position == TerminalDef.GetOutPosition(0, 2, 3))
                    {
                        writer.Write(".Hit");
                        return new ExpressionInfo(SignalType.Bool);
                    }
                    else if (terminal.Position == TerminalDef.GetOutPosition(1, 2, 3))
                    {
                        writer.Write(".HitPos");
                        return new ExpressionInfo(SignalType.Vec3);
                    }
                    else if (terminal.Position == TerminalDef.GetOutPosition(2, 2, 3))
                    {
                        writer.Write(".HitObjId");
                        return new ExpressionInfo(SignalType.Obj);
                    }
                    else
                    {
                        throw new InvalidTerminalException(terminal.Position);
                    }
                }

            case 489:
                {
                    Debug.Assert(!asReference);
                    var getSize = (GetSizeExpressionSyntax)terminal.Node;

                    if (getSize.Object is null)
                    {
                        if (terminal.Position == TerminalDef.GetOutPosition(0, 2, 2) || terminal.Position == TerminalDef.GetOutPosition(1, 2, 2))
                        {
                            writer.Write(GetDefaultValue(SignalType.Float));
                            return new ExpressionInfo(SignalType.Float);
                        }
                        else
                        {
                            throw new InvalidTerminalException(terminal.Position);
                        }
                    }
                    else
                    {
                        writer.Write("_ctx.GetSize(");
                        WriteExpression(getSize.Object, false, environment, writer);
                        writer.Write(')');

                        if (terminal.Position == TerminalDef.GetOutPosition(0, 2, 2))
                        {
                            writer.Write(".Min");
                            return new ExpressionInfo(SignalType.Float);
                        }
                        else if (terminal.Position == TerminalDef.GetOutPosition(1, 2, 2))
                        {
                            writer.Write(".Max");
                            return new ExpressionInfo(SignalType.Float);
                        }
                        else
                        {
                            throw new InvalidTerminalException(terminal.Position);
                        }
                    }
                }

            case 316:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(!asReference);
                    var createObject = (CreateObjectStatementSyntax)terminal.Node;

                    if (createObject.Original is null)
                    {
                        writer.Write(GetDefaultValue(SignalType.Obj));

                        return new ExpressionInfo(SignalType.Obj);
                    }
                    else
                    {
                        string objectVarName = GetStateStoreVarName(environment.Index, createObject.Position, "create_object_object");

                        writer.Write(objectVarName);

                        return new ExpressionInfo(SignalType.Obj);
                    }
                }

            // **************************************** Control ****************************************
            case 242:
                {
                    var touchSensor = (TouchSensorStatementSyntax)terminal.Node;

                    writer.Write(GetStateStoreVarName(environment.Index, touchSensor.Position, "touch_pos"));

                    if (terminal.Position == TerminalDef.GetOutPosition(1, 2, 3))
                    {
                        writer.Write(".X");
                    }
                    else if (terminal.Position == TerminalDef.GetOutPosition(2, 2, 3))
                    {
                        writer.Write(".Y");
                    }
                    else
                    {
                        throw new InvalidTerminalException(terminal.Position);
                    }

                    return new ExpressionInfo(SignalType.Float);
                }

            case 248:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var swipeSensor = (SwipeSensorStatementSyntax)terminal.Node;

                    writer.Write(GetStateStoreVarName(environment.Index, swipeSensor.Position, "swipe_direction"));

                    return new ExpressionInfo(SignalType.Vec3);
                }

            case 592:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var joystick = (JoystickStatementSyntax)terminal.Node;

                    writer.Write(GetStateStoreVarName(environment.Index, joystick.Position, "joystick_direction"));

                    return new ExpressionInfo(SignalType.Vec3);
                }

            case 401:
                {
                    var collision = (CollisionStatementSyntax)terminal.Node;

                    if (terminal.Position == TerminalDef.GetOutPosition(1, 2, 4))
                    {
                        writer.Write(GetStateStoreVarName(environment.Index, collision.Position, "collision_second_object"));
                        return new ExpressionInfo(SignalType.Obj);
                    }
                    else if (terminal.Position == TerminalDef.GetOutPosition(2, 2, 4))
                    {
                        writer.Write(GetStateStoreVarName(environment.Index, collision.Position, "collision_impulse"));
                        return new ExpressionInfo(SignalType.Float);
                    }
                    else if (terminal.Position == TerminalDef.GetOutPosition(3, 2, 4))
                    {
                        writer.Write(GetStateStoreVarName(environment.Index, collision.Position, "collision_normal"));
                        return new ExpressionInfo(SignalType.Vec3);
                    }
                    else
                    {
                        throw new InvalidTerminalException(terminal.Position);
                    }
                }

            case 560:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var loop = (LoopStatementSyntax)terminal.Node;
                    string valueVarName = GetStateStoreVarName(environment.Index, loop.Position, "loop_value");

                    _stateStoreVariables.Add((valueVarName, "int", null));

                    writer.WriteInv($"(float){valueVarName}");

                    return new ExpressionInfo(SignalType.Float);
                }

            // **************************************** Math ****************************************
            case 90 or 144 or 440 or 413 or 453 or 184 or 186 or 188 or 455 or 578:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var unary = (UnaryExpressionSyntax)terminal.Node;

                    SignalType outType;
                    switch (terminal.Node.PrefabId)
                    {
                        case 90:
                            outType = SignalType.Float;
                            writer.Write('-');
                            WriteExpressionOrDefault(unary.Input, SignalType.Float, environment, writer);

                            break;
                        case 144:
                            outType = SignalType.Bool;
                            writer.Write('!');
                            WriteExpressionOrDefault(unary.Input, SignalType.Bool, environment, writer);

                            break;
                        case 440:
                            outType = SignalType.Rot;
                            writer.Write("Quaternion.Inverse(");
                            WriteExpressionOrDefault(unary.Input, SignalType.Rot, environment, writer);
                            writer.Write(')');

                            break;
                        case 413:
                            outType = SignalType.Float;
                            writer.Write("MathF.Sin(");
                            WriteExpressionOrDefault(unary.Input, SignalType.Float, environment, writer);
                            writer.Write(')');

                            break;
                        case 453:
                            outType = SignalType.Float;
                            writer.Write("MathF.Cos(");
                            WriteExpressionOrDefault(unary.Input, SignalType.Float, environment, writer);
                            writer.Write(')');

                            break;
                        case 184:
                            outType = SignalType.Float;
                            writer.Write("MathF.Round(");
                            WriteExpressionOrDefault(unary.Input, SignalType.Float, environment, writer);
                            writer.Write(')');

                            break;
                        case 186:
                            outType = SignalType.Float;
                            writer.Write("MathF.Floor(");
                            WriteExpressionOrDefault(unary.Input, SignalType.Float, environment, writer);
                            writer.Write(')');

                            break;
                        case 188:
                            outType = SignalType.Float;
                            writer.Write("MathF.Ceiling(");
                            WriteExpressionOrDefault(unary.Input, SignalType.Float, environment, writer);
                            writer.Write(')');

                            break;
                        case 455:
                            outType = SignalType.Float;
                            writer.Write("MathF.Abs(");
                            WriteExpressionOrDefault(unary.Input, SignalType.Float, environment, writer);
                            writer.Write(')');

                            break;
                        case 578:
                            outType = SignalType.Vec3;
                            WriteExpressionOrDefault(unary.Input, SignalType.Vec3, environment, writer);
                            writer.Write(".Normalized()");

                            break;
                        default:
                            throw new UnreachableException();
                    }

                    return new ExpressionInfo(outType);
                }

            case 92 or 96 or 100 or 104 or 108 or 112 or 116 or 120 or 124 or 172 or 457 or 132 or 136 or 140 or 421 or 146 or 417 or 128 or 481 or 168 or 176 or 180 or 580 or 570 or 574 or 190 or 200 or 204:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var binary = (BinaryExpressionSyntax)terminal.Node;

                    const float EqualsNumbersMaxDiff = 0.001f;
                    const float EqualsVectorsMaxDiff = 1.0000001e-06f;

                    writer.Write('(');

                    SignalType outType;

                    switch (terminal.Node.PrefabId)
                    {
                        case 146:
                            outType = SignalType.Bool;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Bool, environment, writer);
                            writer.Write(" && ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Bool, environment, writer);
                            break;
                        case 417:
                            outType = SignalType.Bool;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Bool, environment, writer);
                            writer.Write(" || ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Bool, environment, writer);
                            break;
                        case 92:
                            outType = SignalType.Float;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Float, environment, writer);
                            writer.Write(" + ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            break;
                        case 96:
                            outType = SignalType.Vec3;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Vec3, environment, writer);
                            writer.Write(" + ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Vec3, environment, writer);
                            break;
                        case 100:
                            outType = SignalType.Float;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Float, environment, writer);
                            writer.Write(" - ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            break;
                        case 104:
                            outType = SignalType.Vec3;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Vec3, environment, writer);
                            writer.Write(" - ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Vec3, environment, writer);
                            break;
                        case 108:
                            outType = SignalType.Float;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Float, environment, writer);
                            writer.Write(" * ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            break;
                        case 112:
                            outType = SignalType.Vec3;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Vec3, environment, writer);
                            writer.Write(" * ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            break;
                        case 116:
                            outType = SignalType.Vec3;
                            writer.Write("Vector3.Transform(");
                            WriteExpressionOrDefault(binary.Input1, SignalType.Vec3, environment, writer);
                            writer.Write(".ToNumerics(), ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Rot, environment, writer);
                            writer.Write(").ToFloat3()");
                            break;
                        case 120:
                            outType = SignalType.Rot;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Rot, environment, writer);
                            writer.Write(" * ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Rot, environment, writer);
                            break;
                        case 124:
                            outType = SignalType.Float;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Float, environment, writer);
                            writer.Write(" / ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            break;
                        case 172:
                            outType = SignalType.Float;
                            writer.Write("NumberUtils.FcMod(");
                            WriteExpressionOrDefault(binary.Input1, SignalType.Float, environment, writer);
                            writer.Write(", ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            writer.Write(')');
                            break;
                        case 457:
                            outType = SignalType.Float;
                            writer.Write("MathF.Pow(");
                            WriteExpressionOrDefault(binary.Input1, SignalType.Float, environment, writer);
                            writer.Write(", ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            writer.Write(')');
                            break;
                        case 132:
                            // equals numbers
                            outType = SignalType.Bool;
                            writer.Write("MathF.Abs(");
                            WriteExpressionOrDefault(binary.Input1, SignalType.Float, environment, writer);
                            writer.Write(" - ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            writer.WriteInv($") < {EqualsNumbersMaxDiff}");
                            break;
                        case 136:
                            // equals vectors
                            outType = SignalType.Bool;
                            writer.Write('(');
                            WriteExpressionOrDefault(binary.Input1, SignalType.Vec3, environment, writer);
                            writer.Write(" - ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Vec3, environment, writer);
                            writer.WriteInv($").LengthSquared < {EqualsVectorsMaxDiff}");
                            break;
                        case 140:
                            // equals objects
                            outType = SignalType.Bool;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Obj, environment, writer);
                            writer.Write(" == ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Obj, environment, writer);
                            break;
                        case 421:
                            // equals bools
                            outType = SignalType.Bool;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Bool, environment, writer);
                            writer.Write(" == ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Bool, environment, writer);
                            break;
                        case 128:
                            outType = SignalType.Bool;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Float, environment, writer);
                            writer.Write(" < ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            break;
                        case 481:
                            outType = SignalType.Bool;
                            WriteExpressionOrDefault(binary.Input1, SignalType.Float, environment, writer);
                            writer.Write(" > ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            break;
                        case 168:
                            outType = SignalType.Float;
                            writer.Write("_ctx.GetRandomValue(");
                            WriteExpressionOrDefault(binary.Input1, SignalType.Float, environment, writer);
                            writer.Write(", ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            writer.Write(')');
                            break;
                        case 176:
                            outType = SignalType.Float;
                            writer.Write("MathF.Min(");
                            WriteExpressionOrDefault(binary.Input1, SignalType.Float, environment, writer);
                            writer.Write(", ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            writer.Write(')');
                            break;
                        case 180:
                            outType = SignalType.Float;
                            writer.Write("MathF.Max(");
                            WriteExpressionOrDefault(binary.Input1, SignalType.Float, environment, writer);
                            writer.Write(", ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            writer.Write(')');
                            break;
                        case 580:
                            outType = SignalType.Float;
                            writer.Write("MathF.Log(");
                            WriteExpressionOrDefault(binary.Input1, SignalType.Float, environment, writer);
                            writer.Write(", ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            writer.Write(')');
                            break;
                        case 570:
                            outType = SignalType.Float;
                            writer.Write("float3.Dot(");
                            WriteExpressionOrDefault(binary.Input1, SignalType.Vec3, environment, writer);
                            writer.Write(", ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Vec3, environment, writer);
                            writer.Write(')');
                            break;
                        case 574:
                            outType = SignalType.Vec3;
                            writer.Write("float3.Cross(");
                            WriteExpressionOrDefault(binary.Input1, SignalType.Vec3, environment, writer);
                            writer.Write(", ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Vec3, environment, writer);
                            writer.Write(')');
                            break;
                        case 190:
                            outType = SignalType.Vec3;
                            writer.Write('(');
                            WriteExpressionOrDefault(binary.Input1, SignalType.Vec3, environment, writer);
                            writer.Write(" - ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Vec3, environment, writer);
                            writer.Write(").Length");
                            break;
                        case 200:
                            outType = SignalType.Rot;
                            writer.Write("QuaternionUtils.AxisAngle(");
                            WriteExpressionOrDefault(binary.Input1, SignalType.Vec3, environment, writer);
                            writer.Write(".ToNumerics(), ");
                            WriteExpressionOrDefault(binary.Input2, SignalType.Float, environment, writer);
                            writer.Write(')');
                            break;
                        case 204:
                            outType = SignalType.Rot;
                            writer.Write("QuaternionUtils.LookRotation(");
                            WriteExpressionOrDefault(binary.Input1, SignalType.Vec3, environment, writer);
                            writer.Write(".ToNumerics(), ");
                            if (binary.Input2 is null)
                            {
                                writer.Write("Vector3.UnitY");
                            }
                            else
                            {
                                WriteExpression(binary.Input2, false, environment, writer);
                                writer.Write(".ToNumerics()");
                            }

                            writer.Write(')');
                            break;
                        default:
                            throw new UnreachableException();
                    }

                    writer.Write(')');

                    return new ExpressionInfo(outType);
                }

            case 194:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var lerp = (LerpExpressionSyntax)terminal.Node;

                    writer.Write("Quaternion.Lerp(");
                    WriteExpressionOrDefault(lerp.From, SignalType.Rot, environment, writer);
                    writer.Write(", ");
                    WriteExpressionOrDefault(lerp.To, SignalType.Rot, environment, writer);
                    writer.Write(", ");
                    WriteExpressionOrDefault(lerp.Amount, SignalType.Float, environment, writer);
                    writer.Write(')');
                    return new ExpressionInfo(SignalType.Rot);
                }

            case 216:
                {
                    var screenToWorld = (ScreenToWorldExpressionSyntax)terminal.Node;

                    writer.Write("_ctx.ScreenToWorld(new float2(");
                    WriteExpressionOrDefault(screenToWorld.ScreenX, SignalType.Float, environment, writer);
                    writer.Write(", ");
                    WriteExpressionOrDefault(screenToWorld.ScreenY, SignalType.Float, environment, writer);
                    writer.Write(')');

                    if (terminal.Position == TerminalDef.GetOutPosition(0, 2, 2))
                    {
                        writer.Write(".WorldNear");
                    }
                    else if (terminal.Position == TerminalDef.GetOutPosition(1, 2, 2))
                    {
                        writer.Write(".WorldFar");
                    }
                    else
                    {
                        throw new InvalidTerminalException(terminal.Position);
                    }

                    return new ExpressionInfo(SignalType.Vec3);
                }

            case 477:
                {
                    var worldToScreen = (WorldToScreenExpressionSyntax)terminal.Node;

                    writer.Write("_ctx.WorldToScreen(");
                    WriteExpressionOrDefault(worldToScreen.WorldPos, SignalType.Vec3, environment, writer);
                    writer.Write(')');

                    if (terminal.Position == TerminalDef.GetOutPosition(0, 2, 2))
                    {
                        writer.Write(".X");
                    }
                    else if (terminal.Position == TerminalDef.GetOutPosition(1, 2, 2))
                    {
                        writer.Write(".Y");
                    }
                    else
                    {
                        throw new InvalidTerminalException(terminal.Position);
                    }

                    return new ExpressionInfo(SignalType.Float);
                }

            case 208:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 4), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var lineVsPlane = (LineVsPlaneExpressionSyntax)terminal.Node;

                    writer.Write("VectorUtils.LineVsPlane(");
                    WriteExpressionOrDefault(lineVsPlane.LineFrom, SignalType.Vec3, environment, writer);
                    writer.Write(".ToNumerics(), ");
                    WriteExpressionOrDefault(lineVsPlane.LineTo, SignalType.Vec3, environment, writer);
                    writer.Write(".ToNumerics(), ");
                    WriteExpressionOrDefault(lineVsPlane.PlanePoint, SignalType.Vec3, environment, writer);
                    writer.Write(".ToNumerics(), ");
                    WriteExpressionOrDefault(lineVsPlane.PlaneNormal, SignalType.Vec3, environment, writer);
                    writer.Write(')');

                    return new ExpressionInfo(SignalType.Vec3);
                }

            case 150 or 162:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var makeVecRot = (MakeVecRotExpressionSyntax)terminal.Node;

                    switch (makeVecRot.PrefabId)
                    {
                        case 150:
                            writer.Write("new float3(");
                            WriteExpressionOrDefault(makeVecRot.X, SignalType.Float, environment, writer);
                            writer.Write(", ");
                            WriteExpressionOrDefault(makeVecRot.Y, SignalType.Float, environment, writer);
                            writer.Write(", ");
                            WriteExpressionOrDefault(makeVecRot.Z, SignalType.Float, environment, writer);
                            writer.Write(')');
                            return new ExpressionInfo(SignalType.Vec3);
                        case 162:
                            writer.Write("Quaternion.CreateFromYawPitchRoll(");
                            WriteExpressionOrDefault(makeVecRot.Y, SignalType.Float, environment, writer);
                            writer.Write(", ");
                            WriteExpressionOrDefault(makeVecRot.X, SignalType.Float, environment, writer);
                            writer.Write(", ");
                            WriteExpressionOrDefault(makeVecRot.Z, SignalType.Float, environment, writer);
                            writer.Write(')');
                            return new ExpressionInfo(SignalType.Rot);
                        default:
                            throw new UnreachableException();
                    }
                }

            case 156 or 442:
                {
                    var breakVecRot = (BreakVecRotExpressionnSyntax)terminal.Node;

                    switch (breakVecRot.PrefabId)
                    {
                        case 156:
                            WriteExpressionOrDefault(breakVecRot.VecRot, SignalType.Vec3, environment, writer);
                            if (terminal.Position == TerminalDef.GetOutPosition(0, 2, 3))
                            {
                                writer.Write(".X");
                            }
                            else if (terminal.Position == TerminalDef.GetOutPosition(1, 2, 3))
                            {
                                writer.Write(".Y");
                            }
                            else if (terminal.Position == TerminalDef.GetOutPosition(2, 2, 3))
                            {
                                writer.Write(".Z");
                            }
                            else
                            {
                                throw new InvalidTerminalException(terminal.Position);
                            }

                            break;
                        case 442:
                            WriteExpressionOrDefault(breakVecRot.VecRot, SignalType.Rot, environment, writer);
                            if (terminal.Position == TerminalDef.GetOutPosition(0, 2, 3))
                            {
                                writer.Write(".GetEulerX()");
                            }
                            else if (terminal.Position == TerminalDef.GetOutPosition(1, 2, 3))
                            {
                                writer.Write(".GetEulerY()");
                            }
                            else if (terminal.Position == TerminalDef.GetOutPosition(2, 2, 3))
                            {
                                writer.Write(".GetEulerZ()");
                            }
                            else
                            {
                                throw new InvalidTerminalException(terminal.Position);
                            }

                            break;
                        default:
                            throw new UnreachableException();
                    }

                    return new ExpressionInfo(SignalType.Float);
                }

            // **************************************** Value ****************************************
            case 36 or 38 or 42 or 449 or 451:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, terminal.Node.PrefabId is 38 or 42 ? 2 : 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(!asReference);
                    var literal = (LiteralExpressionSyntax)terminal.Node;

                    switch (terminal.Node.PrefabId)
                    {
                        case 36:
                            writer.WriteInv($"{literal.Value.Float}f");
                            return new ExpressionInfo(SignalType.Float);
                        case 38:
                            var vec = literal.Value.Float3;
                            writer.WriteInv($"new float3({vec.X}f, {vec.Y}f, {vec.Z}f)");
                            return new ExpressionInfo(SignalType.Vec3);
                        case 42:
                            var rot = literal.Value.Quaternion;
                            writer.WriteInv($"new Quaternion({rot.X}f, {rot.Y}f, {rot.Z}f, {rot.W}f)");
                            return new ExpressionInfo(SignalType.Rot);
                        case 449 or 451:
                            writer.Write(literal.Value.Bool ? "true" : "false");
                            return new ExpressionInfo(SignalType.Bool);
                        default:
                            throw new UnreachableException();
                    }
                }

            // **************************************** Variables ****************************************
            case 46 or 48 or 50 or 52 or 54 or 56:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var getVariable = (GetVariableExpressionSyntax)terminal.Node;

                    if (asReference)
                    {
                        writer.WriteInv($"""
                            new FcList<{GetCSharpName(getVariable.Variable.Type.ToNotPointer())}>.Ref({GetVariableName(environment.Index, getVariable.Variable)}, 0)
                            """);
                    }
                    else
                    {
                        writer.WriteInv($"""
                            {GetVariableName(environment.Index, getVariable.Variable)}[0]
                            """);
                    }

                    return new ExpressionInfo(getVariable.Variable);
                }

            case 82 or 461 or 465 or 469 or 86 or 473:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var list = (ListExpressionSyntax)terminal.Node;

                    var listType = terminal.Node.PrefabId switch
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
                        var resultType = asReference ? listType.ToPointer() : listType;

                        writer.Write(GetDefaultValue(resultType));

                        return new ExpressionInfo(resultType);
                    }

                    if (list.Index is null)
                    {
                        return WriteExpression(list.Variable, asReference, environment, writer);
                    }
                    else if (list.Variable.Node is GetVariableExpressionSyntax getVariable)
                    {
                        if (asReference)
                        {
                            writer.WriteInv($"""
                                new FcList<{GetCSharpName(getVariable.Variable.Type.ToNotPointer())}>.Ref({GetVariableName(environment.Index, getVariable.Variable)}, (int)
                                """);

                            WriteExpression(list.Index, false, environment, writer);

                            writer.Write(')');
                        }
                        else
                        {
                            writer.WriteInv($"""
                                {GetVariableName(environment.Index, getVariable.Variable)}[(int)
                                """);

                            WriteExpression(list.Index, false, environment, writer);

                            writer.Write(']');
                        }

                        return new ExpressionInfo(getVariable.Variable);
                    }

                    var varInfo = WriteExpression(list.Variable, true, environment, writer);

                    writer.Write("""
                            .Add((int)
                            """);

                    WriteExpression(list.Index, false, environment, writer);

                    writer.Write(")");

                    if (!asReference)
                    {
                        writer.Write(".Value");
                    }

                    return varInfo;
                }

            default:
                {
                    switch (terminal.Node)
                    {
                        case OuterExpressionSyntax:
                            {
                                var outerEnvironment = _environments[environment.OuterEnvironmentIndex];

                                var customStatement = (CustomStatementSyntax)outerEnvironment.AST.Statements[environment.OuterPosition];

                                foreach (var (innerPos, outerTerminal) in customStatement.ConnectedInputTerminals)
                                {
                                    if (innerPos == terminal.Position && outerTerminal is not null)
                                    {
                                        return WriteExpression(outerTerminal, asReference, outerEnvironment, writer);
                                    }
                                }

                                foreach (var info in outerEnvironment.AST.TerminalInfo.InputTerminals)
                                {
                                    if (info.Position == terminal.Position)
                                    {
                                        writer.Write(GetDefaultValue(info.Type));
                                        return new ExpressionInfo(info.Type);
                                    }
                                }

                                // TODO: throw
                                return new ExpressionInfo(SignalType.Error);
                            }

                        case CustomStatementSyntax custom:
                            {
                                var customEnvironment = (Environment)environment.BlockData[custom.Position];

                                foreach (var (con, conTerm) in custom.AST.NonVoidOutputs)
                                {
                                    if (con.OutsidePosition == terminal.Position && conTerm is not null)
                                    {
                                        return WriteExpression(conTerm, asReference, customEnvironment, writer);
                                    }
                                }

                                foreach (var info in customEnvironment.AST.TerminalInfo.OutputTerminals)
                                {
                                    if (info.Position == terminal.Position)
                                    {
                                        return new ExpressionInfo(info.Type);
                                    }
                                }

                                // TODO: throw
                                return new ExpressionInfo(SignalType.Error);
                            }

                        case ObjectExpressionSyntax:
                            {
                                writer.WriteInv($"_ctx.GetObjectId(terminal.Node.Position, terminal.Position)");
                                return new ExpressionInfo(SignalType.Obj);
                            }

                        default:
                            throw new NotImplementedException($"Prefab with id {terminal.Node.PrefabId} is not implemented.");
                    }
                }
        }
    }
}
