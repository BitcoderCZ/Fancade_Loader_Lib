using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting.Settings;
using FancadeLoaderLib.Runtime.Compiled.Utils;
using FancadeLoaderLib.Runtime.Syntax;
using FancadeLoaderLib.Runtime.Syntax.Control;
using FancadeLoaderLib.Runtime.Syntax.Game;
using FancadeLoaderLib.Runtime.Syntax.Objects;
using FancadeLoaderLib.Runtime.Syntax.Sound;
using FancadeLoaderLib.Runtime.Syntax.Values;
using FancadeLoaderLib.Runtime.Syntax.Variables;
using MathUtils.Vectors;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Compiled;

public partial class AstCompiler
{
    private StatementSyntax WriteStatement(ushort3 pos, byte3 terminalPos, Environment environment, out byte3 executeNext, IndentedTextWriter writer)
    {
        var statement = environment.AST.Statements[pos];

        executeNext = TerminalDef.AfterPosition;

        // faster than switching on type
        switch (statement.PrefabId)
        {
            // **************************************** Game ****************************************
            case 252:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var win = (WinStatementSyntax)statement;

                    writer.WriteLineInv($"_ctx.{nameof(IRuntimeContext.Win)}({win.Delay});");
                }

                break;
            case 256:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var lose = (LoseStatementSyntax)statement;

                    writer.WriteLineInv($"_ctx.{nameof(IRuntimeContext.Lose)}({lose.Delay});");
                }

                break;
            case 260:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var setScore = (SetScoreStatementSyntax)statement;

                    writer.WriteInv($"_ctx.{nameof(IRuntimeContext.SetScore)}(");
                    WriteExpressionOrNull(setScore.Score, SignalType.Float, environment, writer);
                    writer.Write(", ");
                    WriteExpressionOrNull(setScore.Coins, SignalType.Float, environment, writer);
                    writer.WriteLineInv($", Ranking.{Enum.GetName(typeof(Ranking), setScore.Ranking)});");
                }

                break;
            case 268:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be valid.");
                    var setCamera = (SetCameraStatementSyntax)statement;

                    writer.WriteInv($"_ctx.{nameof(IRuntimeContext.SetCamera)}(");
                    WriteExpressionOrNull(setCamera.PositionTerminal, SignalType.Vec3Ptr, environment, writer);
                    writer.Write(", ");
                    WriteExpressionOrNull(setCamera.RotationTerminal, SignalType.Rot, environment, writer);
                    writer.Write(", ");
                    WriteExpressionOrNull(setCamera.RangeTerminal, SignalType.Float, environment, writer);
                    writer.WriteLineInv($", {(setCamera.Perspective ? "true" : "false")});");
                }

                break;
            case 274:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var setLight = (SetLightStatementSyntax)statement;

                    writer.WriteInv($"_ctx.{nameof(IRuntimeContext.SetLight)}(");
                    WriteExpressionOrNull(setLight.PositionTerminal, SignalType.Vec3, environment, writer);
                    writer.Write(", ");
                    WriteExpressionOrNull(setLight.RotationTerminal, SignalType.Rot, environment, writer);
                    writer.WriteLine(");");
                }

                break;
            case 584:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var menuItem = (MenuItemStatementSyntax)statement;

                    writer.WriteInv($"_ctx.{nameof(IRuntimeContext.MenuItem)}(");
                    WriteExpressionOrNull(menuItem.Variable, SignalType.FloatPtr, environment, writer);
                    writer.Write(", ");
                    WriteExpressionOrDefault(menuItem.Picture, SignalType.Obj, environment, writer);
                    writer.WriteLineInv($"""
                        , "{menuItem.Name}", new MaxBuyCount({menuItem.MaxBuyCount.Value}), PriceIncrease.{menuItem.PriceIncrease});
                        """);

                }

                break;

            // **************************************** Objects ****************************************
            case 282:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be valid.");
                    var setPosition = (SetPositionStatementSyntax)statement;

                    if (setPosition.ObjectTerminal is not null)
                    {
                        writer.WriteInv($"_ctx.{nameof(IRuntimeContext.SetPosition)}(");
                        WriteExpression(setPosition.ObjectTerminal, false, environment, writer);
                        writer.Write(", ");
                        WriteExpressionOrNull(setPosition.PositionTerminal, SignalType.Vec3, environment, writer);
                        writer.Write(", ");
                        WriteExpressionOrNull(setPosition.RotationTerminal, SignalType.Rot, environment, writer);
                        writer.WriteLine(");");
                    }
                }

                break;
            case 306:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var setVisible = (SetVisibleStatementSyntax)statement;

                    if (setVisible.Object is not null)
                    {
                        writer.WriteInv($"_ctx.{nameof(IRuntimeContext.SetPosition)}(");

                        WriteExpression(setVisible.Object, false, environment, writer);

                        writer.Write(", ");

                        WriteExpressionOrDefault(setVisible.Visible, SignalType.Bool, environment, writer);

                        writer.WriteLine(");");
                    }
                }

                break;
            case 316:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var createObject = (CreateObjectStatementSyntax)statement;

                    if (createObject.Original is not null)
                    {
                        string objectVarName = GetStateStoreVarName(environment.Index, createObject.Position, "create_object_object");
                        _stateStoreVariables.Add((objectVarName, "int", null));

                        writer.WriteInv($"{objectVarName} = _ctx.{nameof(IRuntimeContext.CreateObject)}(");

                        WriteExpression(createObject.Original, false, environment, writer);

                        writer.WriteLine(");");
                    }
                }

                break;
            case 320:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var destroyObject = (DestroyObjectStatementSyntax)statement;

                    if (destroyObject.Object is not null)
                    {
                        writer.WriteInv($"_ctx.{nameof(IRuntimeContext.DestroyObject)}(");

                        WriteExpression(destroyObject.Object, false, environment, writer);

                        writer.WriteLine(");");
                    }
                }

                break;

            // **************************************** Sound ****************************************
            case 264:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var playSound = (PlaySoundStatementSyntax)statement;

                    string soundVaName = GetStateStoreVarName(environment.Index, playSound.Position, "play_sound_sound");
                    _stateStoreVariables.Add((soundVaName, "float", "-1f"));

                    writer.WriteInv($"{soundVaName} = _ctx.{nameof(IRuntimeContext.PlaySound)}(");

                    // TODO: figure out if the defaults are correct
                    if (playSound.Volume is null)
                    {
                        writer.Write("1f");
                    }
                    else
                    {
                        WriteExpression(playSound.Volume, false, environment, writer);
                    }

                    writer.Write(", ");

                    if (playSound.Pitch is null)
                    {
                        writer.Write("1f");
                    }
                    else
                    {
                        WriteExpression(playSound.Pitch, false, environment, writer);
                    }

                    writer.WriteLineInv($", {nameof(FcSound)}.{playSound.Sound});");
                }

                break;
            case 397:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var stopSound = (StopSoundStatementSyntax)statement;

                    if (stopSound.Channel is not null)
                    {
                        writer.WriteInv($"_ctx.{nameof(IRuntimeContext.StopSound)}(");
                        WriteExpression(stopSound.Channel, false, environment, writer);
                        writer.WriteLine(");");
                    }
                }

                break;
            case 391:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var volumePitch = (VolumePitchStatementSyntax)statement;

                    if (volumePitch.Channel is not null)
                    {
                        writer.WriteInv($"_ctx.{nameof(IRuntimeContext.AdjustVolumePitch)}(");
                        WriteExpression(volumePitch.Channel, false, environment, writer);
                        writer.Write(", ");
                        WriteExpressionOrNull(volumePitch.Volume, SignalType.Float, environment, writer);
                        writer.Write(", ");
                        WriteExpressionOrNull(volumePitch.Pitch, SignalType.Float, environment, writer);
                        writer.WriteLine(");");
                    }
                }

                break;

            // **************************************** Control ****************************************
            case 234:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var ifStatement = (IfStatementSyntax)statement;

                    if (ifStatement.Condition is not null)
                    {
                        writer.Write("if (");

                        WriteExpression(ifStatement.Condition, false, environment, writer);

                        writer.WriteLine(')');

                        using (writer.CurlyIndent(newLine: false))
                        {
                            WriteConnected(ifStatement, TerminalDef.GetOutPosition(0, 2, 2), environment, writer);
                        }

                        writer.WriteLine("else");

                        using (writer.CurlyIndent())
                        {
                            WriteConnected(ifStatement, TerminalDef.GetOutPosition(1, 2, 2), environment, writer);
                        }
                    }
                }

                break;
            case 238:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    Debug.Assert(statement is PlaySensorStatementSyntax, $"{nameof(statement)} should be {nameof(PlaySensorStatementSyntax)}");

                    using (writer.CurlyIndent($"if (_ctx.{nameof(IRuntimeContext.CurrentFrame)} == 0)"))
                    {
                        WriteConnected(statement, TerminalDef.GetOutPosition(0, 2, 2), environment, writer);
                    }
                }

                break;
            case 566:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    Debug.Assert(statement is LateUpdateStatementSyntax, $"{nameof(statement)} should be {nameof(LateUpdateStatementSyntax)}");

                    using (writer.CurlyIndent("if (lateUpdateQueue is not null)"))
                    {
                        foreach (var connection in statement.OutVoidConnections)
                        {
                            if (connection.FromVoxel == TerminalDef.GetOutPosition(0, 2, 2))
                            {
                                if (connection.IsToOutside)
                                {
                                    throw new NotImplementedException();
                                }
                                else
                                {
                                    writer.WriteLine("""
                                        lateUpdateQueue.Enqueue(() =>
                                        """);
                                    writer.WriteLine('{');
                                    writer.Indent++;
                                    WriteEntryPoint(new(environment.Index, connection.To, (byte3)connection.ToVoxel), false, writer);
                                    writer.Indent--;
                                    writer.WriteLine("});");
                                }
                            }
                        }
                    }
                }

                break;
            case 409:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    Debug.Assert(statement is BoxArtStatementSyntax, $"{nameof(statement)} should be {nameof(BoxArtStatementSyntax)}");

                    using (writer.CurlyIndent($"if (_ctx.{nameof(IRuntimeContext.TakingBoxArt)})"))
                    {
                        WriteConnected(statement, TerminalDef.GetOutPosition(0, 2, 2), environment, writer);
                    }
                }

                break;
            case 242:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var touchSensor = (TouchSensorStatementSyntax)statement;

                    string touchPosVarName = GetStateStoreVarName(environment.Index, touchSensor.Position, "touch_pos");
                    _stateStoreVariables.Add((touchPosVarName, nameof(float2), null));

                    using (writer.CurlyIndent($"if (_ctx.{nameof(IRuntimeContext.TryGetTouch)}({nameof(TouchState)}.{touchSensor.State}, {touchSensor.FingerIndex}, out var touchPos{_localVarCounter}))"))
                    {
                        writer.WriteLineInv($"{touchPosVarName} = touchPos{_localVarCounter};");

                        _localVarCounter++;

                        WriteConnected(statement, TerminalDef.GetOutPosition(0, 2, 3), environment, writer);
                    }
                }

                break;
            case 248:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var swipeSensor = (SwipeSensorStatementSyntax)statement;

                    string directionVarName = GetStateStoreVarName(environment.Index, swipeSensor.Position, "swipe_direction");
                    _stateStoreVariables.Add((directionVarName, nameof(float3), null));

                    using (writer.CurlyIndent($"if (_ctx.{nameof(IRuntimeContext.TryGetSwipe)}(out var direction{_localVarCounter}))"))
                    {
                        writer.WriteLineInv($"{directionVarName} = direction{_localVarCounter};");

                        _localVarCounter++;

                        WriteConnected(statement, TerminalDef.GetOutPosition(0, 2, 2), environment, writer);
                    }
                }

                break;
            case 588:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var button = (ButtonStatementSyntax)statement;

                    using (writer.CurlyIndent($"if (_ctx.{nameof(IRuntimeContext.GetButtonPressed)}({nameof(ButtonType)}.{button.Type}))"))
                    {
                        WriteConnected(statement, TerminalDef.GetOutPosition(0, 2, 2), environment, writer);
                    }
                }

                break;
            case 592:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var joystick = (JoystickStatementSyntax)statement;

                    string directionVarName = GetStateStoreVarName(environment.Index, joystick.Position, "joystick_direction");
                    _stateStoreVariables.Add((directionVarName, nameof(float3), null));

                    writer.WriteLineInv($"{directionVarName} = _ctx.{nameof(IRuntimeContext.GetJoystickDirection)}({nameof(JoystickType)}.{joystick.Type});");
                }

                break;
            case 401:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var collision = (CollisionStatementSyntax)statement;

                    if (collision.FirstObject is not null)
                    {
                        string secondObjectVarName = GetStateStoreVarName(environment.Index, collision.Position, "collision_second_object");
                        string impulseVarName = GetStateStoreVarName(environment.Index, collision.Position, "collision_impulse");
                        string normalVarName = GetStateStoreVarName(environment.Index, collision.Position, "collision_normal");
                        _stateStoreVariables.Add((secondObjectVarName, "int", null));
                        _stateStoreVariables.Add((impulseVarName, "float", null));
                        _stateStoreVariables.Add((normalVarName, nameof(float3), null));

                        writer.WriteInv($"_ctx.{nameof(IRuntimeContext.TryGetCollision)}(");
                        WriteExpression(collision.FirstObject, false, environment, writer);

                        using (writer.CurlyIndent($", out int secondObject{_localVarCounter}, out float impulse{_localVarCounter}, out float3 normal{_localVarCounter})"))
                        {
                            writer.WriteLineInv($"{secondObjectVarName} = secondObject{_localVarCounter};");
                            writer.WriteLineInv($"{impulseVarName} = impulse{_localVarCounter};");
                            writer.WriteLineInv($"{normalVarName} = normal{_localVarCounter};");

                            _localVarCounter++;

                            WriteConnected(statement, TerminalDef.GetOutPosition(0, 2, 4), environment, writer);
                        }
                    }
                }

                break;
            case 560:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var loop = (LoopStatementSyntax)statement;

                    string valueVarName = GetStateStoreVarName(environment.Index, pos, "loop_value");
                    string localValueVarName = $"value{_localVarCounter}";
                    string startVarName = $"start{_localVarCounter}";
                    string stepVarName = $"step{_localVarCounter}";
                    _localVarCounter++;

                    _stateStoreVariables.Add((valueVarName, "int", null));

                    writer.WriteInv($"int {startVarName} = (int)");
                    WriteExpressionOrDefault(loop.Start, SignalType.Float, environment, writer);
                    writer.WriteLine(';');

                    writer.WriteInv($"int {stepVarName} = (int)MathF.Ceiling(");

                    WriteExpressionOrDefault(loop.Stop, SignalType.Float, environment, writer);

                    writer.WriteLineInv($").CompareTo({startVarName});");

                    writer.WriteLineInv($"int {localValueVarName} = {startVarName} - {stepVarName};");
                    writer.WriteLineInv($"{valueVarName} = {localValueVarName};");

                    using (writer.CurlyIndent($"if ({stepVarName} != 0)"))
                    {
                        using (writer.CurlyIndent("while (true)"))
                        {
                            writer.Write("int stop = (int)MathF.Ceiling(");
                            WriteExpressionOrDefault(loop.Stop, SignalType.Float, environment, writer);
                            writer.WriteLine(");");

                            writer.WriteLineInv($"int nextVal = {localValueVarName} + {stepVarName};");

                            using (writer.CurlyIndent($"if ({stepVarName} > 0 ? nextVal >= stop : nextVal <= stop)"))
                            {
                                writer.WriteLine("break;");
                            }

                            writer.WriteLineInv($"{valueVarName} = {localValueVarName} = nextVal;");

                            WriteConnected(loop, TerminalDef.GetOutPosition(0, 2, 2), environment, writer);
                        }
                    }
                }

                break;

            // **************************************** Value ****************************************
            case 16 or 20 or 24 or 28 or 32:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var inspect = (InspectStatementSyntax)statement;
                    if (inspect.Input is not null)
                    {
                        writer.Write($"_ctx.{nameof(IRuntimeContext.InspectValue)}(new RuntimeValue(");

                        var info = WriteExpression(inspect.Input, false, environment, writer);

                        writer.WriteLineInv($"""
                            ), SignalType.{info.Type}, {(info.VariableName is null ? "null" : $"\"{info.VariableName}\"")}, {environment.AST.PrefabId}, new ushort3({pos.X}, {pos.Y}, {pos.Z}));
                            """);
                    }
                }

                break;

            // **************************************** Variables ****************************************
            case 428 or 430 or 432 or 434 or 436 or 438:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(1), $"{nameof(terminalPos)} should be valid.");
                    var setVar = (SetVaribleStatementSyntax)statement;

                    if (setVar.Value is not null)
                    {
                        writer.WriteInv($"""
                            {GetVariableName(environment.Index, setVar.Variable)}[0] = 
                            """);

                        WriteExpression(setVar.Value, false, environment, writer);

                        writer.WriteLine(";");
                    }
                }

                break;
            case 58 or 62 or 66 or 70 or 74 or 78:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                    var setPointer = (SetPointerStatementSyntax)statement;

                    if (setPointer.Variable is not null && setPointer.Value is not null)
                    {
                        if (!TryWriteDirrectRef(setPointer.Variable, environment, writer))
                        {
                            WriteExpression(setPointer.Variable, true, environment, writer);

                            writer.Write("""
                            .Value
                            """);
                        }

                        writer.Write(" = ");

                        WriteExpression(setPointer.Value, false, environment, writer);

                        writer.WriteLine(';');
                    }
                }

                break;
            case 556 or 558:
                {
                    Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(1), $"{nameof(terminalPos)} should be valid.");
                    var incDecNumber = (IncDecNumberStatementSyntax)statement;

                    if (incDecNumber.Variable is not null)
                    {
                        if (!TryWriteDirrectRef(incDecNumber.Variable, environment, writer))
                        {
                            WriteExpression(incDecNumber.Variable, true, environment, writer);

                            writer.Write(".Value");
                        }

                        writer.WriteLine(incDecNumber.PrefabId switch
                        {
                            556 => "++;",
                            558 => "--;",
                            _ => throw new UnreachableException(),
                        });
                    }
                }

                break;
            default:
                {
                    if (statement is not CustomStatementSyntax custom)
                    {
                        throw new NotImplementedException($"Prefab with id {statement.PrefabId} is not implemented.");
                    }

                    var customEnvironment = (Environment)environment.BlockData[custom.Position];

                    foreach (var con in custom.AST.VoidInputs)
                    {
                        if (con.OutsidePosition == terminalPos)
                        {
                            WriteEntryPoint(new EntryPoint(customEnvironment.Index, con.BlockPosition, con.TerminalPosition), false, writer);
                        }
                    }
                }

                break;
        }

        return statement;
    }
}
