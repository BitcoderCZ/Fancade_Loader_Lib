using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using FancadeLoaderLib.Runtime.Syntax;
using FancadeLoaderLib.Runtime.Syntax.Control;
using FancadeLoaderLib.Runtime.Syntax.Game;
using FancadeLoaderLib.Runtime.Syntax.Math;
using FancadeLoaderLib.Runtime.Syntax.Values;
using FancadeLoaderLib.Runtime.Syntax.Variables;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using static FancadeLoaderLib.Editing.StockBlocks;
using static FancadeLoaderLib.Runtime.Utils.ThrowHelper;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime;

public sealed class Interpreter
{
    private static readonly byte3 PosOut02 = TerminalDef.GetOutPosition(0, 2, 2);
    private static readonly byte3 PosOut12 = TerminalDef.GetOutPosition(1, 2, 2);

    private static readonly byte3 PosOut03 = TerminalDef.GetOutPosition(0, 2, 3);
    private static readonly byte3 PosOut13 = TerminalDef.GetOutPosition(1, 2, 3);
    private static readonly byte3 PosOut23 = TerminalDef.GetOutPosition(2, 2, 3);

    private static readonly byte3 PosOut04 = TerminalDef.GetOutPosition(0, 2, 4);
    private static readonly byte3 PosOut14 = TerminalDef.GetOutPosition(1, 2, 4);
    private static readonly byte3 PosOut24 = TerminalDef.GetOutPosition(2, 2, 4);
    private static readonly byte3 PosOut34 = TerminalDef.GetOutPosition(3, 2, 4);

    private readonly Environment[] _environments;
    private readonly IRuntimeContext _ctx;

    private readonly InterpreterVariableAccessor _variableAccessor;

    public Interpreter(AST ast, IRuntimeContext ctx)
        : this(ast, ctx, 4)
    {
    }

    public Interpreter(AST ast, IRuntimeContext ctx, int maxDepth)
    {
        ThrowIfNull(ast, nameof(ast));
        ThrowIfNull(ast, nameof(ctx));

        _ctx = ctx;

        List<Environment> environments = [];
        List<ImmutableArray<Variable>> variables = [];

        var mainEnvironment = new Environment(ast, 0, -1, ushort3.Zero);
        environments.Add(mainEnvironment);
        variables.Add(mainEnvironment.AST.Variables);

        InitEnvironments(mainEnvironment, environments, variables, maxDepth);

        _environments = [.. environments];

        _variableAccessor = new InterpreterVariableAccessor(ast.GlobalVariables, variables.Select((vars, index) => (index, (IEnumerable<Variable>)vars)));
    }

    public IVariableAccessor VariableAccessor => _variableAccessor;

    public Action RunFrame()
    {
        var lateUpdateQueue = new Queue<EntryPoint>();

        foreach (var environment in _environments)
        {
            foreach (var entryPoint in environment.AST.NotConnectedVoidInputs)
            {
                Execute(new EntryPoint(environment.Index, entryPoint.BlockPosition, entryPoint.TerminalPosition), lateUpdateQueue);
            }
        }

        return () =>
        {
            while (lateUpdateQueue.TryDequeue(out var entryPoint))
            {
                Execute(entryPoint, null);
            }
        };
    }

    private static void InitEnvironments(Environment outer, List<Environment> environments, List<ImmutableArray<Variable>> variables, int maxDepth, int depth = 1)
    {
        if (depth > maxDepth)
        {
            throw new EnvironmentDepthLimitReachedException();
        }

        foreach (var node in outer.AST.Nodes.Values)
        {
            if (node is CustomStatementSyntax customStatement)
            {
                var environment = new Environment(customStatement.AST, environments.Count, outer.Index, customStatement.Position);
                environments.Add(environment);
                variables.Add(environment.AST.Variables);
                outer.BlockData[customStatement.Position] = environment;

                InitEnvironments(environment, environments, variables, maxDepth, depth + 1);
            }
        }
    }

    private void Execute(EntryPoint entryPoint, Queue<EntryPoint>? lateUpdateQueue)
    {
        Span<byte3> executeNextSpan = stackalloc byte3[16];

        Stack<EntryPoint> executeNext = new();

        executeNext.Push(entryPoint);

        while (executeNext.TryPop(out var item))
        {
            var (environmentIndex, blockPos, terminalPos) = item;
            var environment = _environments[environmentIndex];
            var statement = (StatementSyntax)environment.AST.Nodes[blockPos];

            int nextCount = 0;
            executeNextSpan[nextCount++] = TerminalDef.AfterPosition;

            // faster than switching on type
            switch (statement.PrefabId)
            {
                // **************************************** Control ****************************************
                case 234:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var ifStatement = (IfStatementSyntax)statement;

                        if (ifStatement.Condition is not null)
                        {
                            if (GetValue(ifStatement.Condition, environment).Bool)
                            {
                                executeNextSpan[nextCount++] = PosOut02;
                            }
                            else
                            {
                                executeNextSpan[nextCount++] = PosOut12;
                            }
                        }
                    }

                    break;
                case 238:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        Debug.Assert(statement is PlaySensorStatementSyntax, $"{nameof(statement)} should be {nameof(PlaySensorStatementSyntax)}");

                        if (_ctx.CurrentFrame == 0)
                        {
                            executeNextSpan[nextCount++] = PosOut02;
                        }
                    }

                    break;
                case 566:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        Debug.Assert(statement is LateUpdateStatementSyntax, $"{nameof(statement)} should be {nameof(LateUpdateStatementSyntax)}");

                        if (lateUpdateQueue is not null)
                        {
                            foreach (var connection in statement.OutVoidConnections)
                            {
                                if (connection.FromVoxel == PosOut02)
                                {
                                    lateUpdateQueue.Enqueue(new(environmentIndex, connection.To, (byte3)connection.ToVoxel));
                                }
                            }
                        }
                    }

                    break;
                case 409:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        Debug.Assert(statement is BoxArtStatementSyntax, $"{nameof(statement)} should be {nameof(BoxArtStatementSyntax)}");

                        if (_ctx.TakingBoxArt)
                        {
                            executeNextSpan[nextCount++] = PosOut02;
                        }
                    }

                    break;
                case 242:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var touchSensor = (TouchSensorStatementSyntax)statement;

                        if (_ctx.TryGetTouch(touchSensor.State, touchSensor.FingerIndex, out var touchPos))
                        {
                            environment.BlockData[touchSensor.Position] = touchPos;
                            executeNextSpan[nextCount++] = PosOut03;
                        }
                    }

                    break;
                case 248:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        Debug.Assert(statement is SwipeSensorStatementSyntax, $"{nameof(statement)} should be {nameof(SwipeSensorStatementSyntax)}");

                        if (_ctx.TryGetSwipe(out var direction))
                        {
                            environment.BlockData[statement.Position] = direction;
                            executeNextSpan[nextCount++] = PosOut02;
                        }
                    }

                    break;
                case 588:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var button = (ButtonStatementSyntax)statement;

                        if (_ctx.GetButtonPressed(button.Type))
                        {
                            executeNextSpan[nextCount++] = PosOut02;
                        }
                    }

                    break;
                case 592:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var joystick = (JoystickStatementSyntax)statement;

                        environment.BlockData[joystick.Position] = _ctx.GetJoystickDirection(joystick.Type);
                    }

                    break;
                case 401:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var collision = (CollisionStatementSyntax)statement;

                        if (collision.FirstObject is not null && _ctx.TryGetCollision(GetValue(collision.FirstObject, environment).Int, out int secondObject, out float impulse, out float3 normal))
                        {
                            environment.BlockData[collision.Position] = (secondObject, impulse, normal);
                            executeNextSpan[nextCount++] = PosOut04;
                        }
                    }

                    break;
                case 560:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var loop = (LoopStatementSyntax)statement;

                        int start = (int)GetValue(loop.Start, environment).Float;
                        int stop = (int)MathF.Ceiling(GetValue(loop.Stop, environment).Float);

                        int step = stop.CompareTo(start);
                        int value = start - step;

                        environment.BlockData[loop.Position] = value;

                        if (step == 0)
                        {
                            break;
                        }

                        while (true)
                        {
                            stop = (int)MathF.Ceiling(GetValue(loop.Stop, environment).Float);

                            int nextVal = value + step;
                            if (step > 0 ? nextVal >= stop : nextVal <= stop)
                            {
                                break;
                            }

                            value = nextVal;
                            environment.BlockData[loop.Position] = value;

                            foreach (var connection in loop.OutVoidConnections)
                            {
                                if (connection.FromVoxel == PosOut02)
                                {
                                    Execute(new(environment.Index, connection.To, (byte3)connection.ToVoxel), lateUpdateQueue);
                                }
                            }
                        }
                    }

                    break;

                // **************************************** Math ****************************************
                case 485:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var randomSeed = (RandomSeedStatementSyntax)statement;
                        if (randomSeed.Seed is not null)
                        {
                            _ctx.SetRandomSeed(GetValue(randomSeed.Seed, environment).Float);
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
                            _ctx.InspectValue(GetValue(inspect.Input, environment), inspect.Type, environment.AST.PrefabId, inspect.Position);
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
                            _variableAccessor.SetVariableValue(_variableAccessor.GetVariableId(environment, setVar.Variable), 0, GetValue(setVar.Value, environment));
                        }
                    }

                    break;
                case 58 or 62 or 66 or 70 or 74 or 78:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var setPointer = (SetPointerStatementSyntax)statement;

                        if (setPointer.Variable is not null && setPointer.Value is not null)
                        {
                            var varRef = GetOutput(setPointer.Variable, environment).Reference;

                            _variableAccessor.SetVariableValue(varRef.VariableId, varRef.Index, GetValue(setPointer.Value, environment));
                        }
                    }

                    break;
                case 556 or 558:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(1), $"{nameof(terminalPos)} should be valid.");
                        var incDecNumber = (IncDecNumberStatementSyntax)statement;

                        if (incDecNumber.Variable is not null)
                        {
                            var varRef = GetOutput(incDecNumber.Variable, environment).Reference;

                            _variableAccessor.SetVariableValue(varRef.VariableId, varRef.Index, new(_variableAccessor.GetVariableValue(varRef.VariableId, varRef.Index).Float + incDecNumber.PrefabId switch
                            {
                                556 => 1f,
                                558 => -1f,
                                _ => throw new UnreachableException(),
                            }));
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
                                Execute(new EntryPoint(customEnvironment.Index, con.BlockPosition, con.TerminalPosition), lateUpdateQueue);
                            }
                        }
                    }

                    break;
            }

            foreach (var nextTerminal in executeNextSpan[..nextCount])
            {
                PushAfter(statement, nextTerminal, environment, executeNext);
            }
        }
    }

    private void PushAfter(StatementSyntax statement, byte3 terminalPos, Environment environment, Stack<EntryPoint> stack)
    {
        foreach (var connection in statement.OutVoidConnections)
        {
            if (connection.FromVoxel == terminalPos)
            {
                if (connection.IsToOutside)
                {
                    var outerEnvironment = _environments[environment.OuterEnvironmentIndex];

                    PushAfter((StatementSyntax)outerEnvironment.AST.Nodes[environment.OuterPosition], (byte3)connection.ToVoxel, outerEnvironment, stack);
                }
                else
                {
                    stack.Push(new(environment.Index, connection.To, (byte3)connection.ToVoxel));
                }
            }
        }
    }

    private RuntimeValue GetValue(SyntaxTerminal? terminal, Environment environment)
        => GetOutput(terminal, environment).GetValue(_variableAccessor);

    private TerminalOutput GetOutput(SyntaxTerminal? terminal, Environment environment)
    {
        if (terminal is null)
        {
            return TerminalOutput.Disconnected;
        }

        // faster than switching on type
        switch (terminal.Node.PrefabId)
        {
            // **************************************** Game ****************************************
            case 564:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is CurrentFrameExpressionSyntax, $"{nameof(terminal)}.{nameof(terminal.Node)} should be {nameof(CurrentFrameExpressionSyntax)}");

                    return new TerminalOutput(new RuntimeValue(_ctx.CurrentFrame));
                }

            // **************************************** Control ****************************************
            case 242:
                {
                    var touchSensor = (TouchSensorStatementSyntax)terminal.Node;

                    var touchPos = (float2)environment.BlockData.GetValueOrDefault(touchSensor.Position, float2.Zero);

                    float val;
                    if (terminal.Position == PosOut13)
                    {
                        val = touchPos.X;
                    }
                    else if (terminal.Position == PosOut23)
                    {
                        val = touchPos.Y;
                    }
                    else
                    {
                        throw new InvalidTerminalException(terminal.Position);
                    }

                    return new TerminalOutput(new RuntimeValue(val));
                }

            case 248:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var swipeSensor = (SwipeSensorStatementSyntax)terminal.Node;

                    var direction = (float3)environment.BlockData.GetValueOrDefault(swipeSensor.Position, float3.Zero);

                    return new TerminalOutput(new RuntimeValue(direction));
                }

            case 592:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var joystick = (JoystickStatementSyntax)terminal.Node;

                    var direction = (float3)environment.BlockData.GetValueOrDefault(joystick.Position, float3.Zero);

                    return new TerminalOutput(new RuntimeValue(direction));
                }

            case 401:
                {
                    var collision = (CollisionStatementSyntax)terminal.Node;

                    var (otherObject, impulse, normal) = ((int, float, float3))environment.BlockData.GetValueOrDefault(collision.Position, (0, 0f, float3.Zero));

                    RuntimeValue val;
                    if (terminal.Position == PosOut14)
                    {
                        val = new(otherObject);
                    }
                    else if (terminal.Position == PosOut24)
                    {
                        val = new(impulse);
                    }
                    else if (terminal.Position == PosOut34)
                    {
                        val = new(normal);
                    }
                    else
                    {
                        throw new InvalidTerminalException(terminal.Position);
                    }

                    return new TerminalOutput(val);
                }

            case 560:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var loop = (LoopStatementSyntax)terminal.Node;

                    float value = (int)environment.BlockData.GetValueOrDefault(loop.Position, 0);

                    return new TerminalOutput(new RuntimeValue(value));
                }

            // **************************************** Math ****************************************
            case 90 or 144 or 440 or 413 or 453 or 184 or 186 or 188 or 455 or 578:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var unary = (UnaryExpressionSyntax)terminal.Node;
                    var input = GetValue(unary.Input, environment);
                    RuntimeValue value;

                    value = terminal.Node.PrefabId switch
                    {
                        90 => new(-input.Float),
                        144 => new(!input.Bool),
                        440 => new(Quaternion.Inverse(input.Quaternion)),
                        413 => new(MathF.Sin(input.Float)),
                        453 => new(MathF.Cos(input.Float)),
                        184 => new(MathF.Round(input.Float)),
                        186 => new(MathF.Floor(input.Float)),
                        188 => new(MathF.Ceiling(input.Float)),
                        455 => new(MathF.Abs(input.Float)),
                        578 => new(input.Float3.Normalized()),
                        _ => throw new UnreachableException(),
                    };

                    return new TerminalOutput(value);
                }

            case 92 or 96 or 100 or 104 or 108 or 112 or 116 or 120 or 124 or 172 or 457 or 132 or 136 or 140 or 421 or 146 or 417 or 128 or 481 or 168 or 176 or 180 or 580 or 570 or 574 or 190 or 200 or 204:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var binary = (BinaryExpressionSyntax)terminal.Node;
                    var input1 = GetValue(binary.Input1, environment);

                    // optimize by not getting the second value when not needed
                    switch (terminal.Node.PrefabId)
                    {
                        case 146:
                            return new TerminalOutput(new RuntimeValue(input1.Bool && GetValue(binary.Input2, environment).Bool));
                        case 417:
                            return new TerminalOutput(new RuntimeValue(input1.Bool || GetValue(binary.Input2, environment).Bool));
                    }

                    var input2 = GetValue(binary.Input2, environment);
                    RuntimeValue value;

                    const float EqualsNumbersMaxDiff = 0.001f;
                    const float EqualsVectorsMaxDiff = 1.0000001e-06f;

                    value = terminal.Node.PrefabId switch
                    {
                        92 => new(input1.Float + input2.Float),
                        96 => new(input1.Float3 + input2.Float3),
                        100 => new(input1.Float - input2.Float),
                        104 => new(input1.Float3 - input2.Float3),
                        108 => new(input1.Float * input2.Float),
                        112 => new(input1.Float3 * input2.Float),
                        116 => new(Vector3.Transform(input1.Float3.ToNumerics(), input2.Quaternion).ToFloat3()),
                        120 => new(input1.Quaternion * input2.Quaternion),
                        124 => new(input1.Float / input2.Float),
                        172 => new(input1.Float % input2.Float),
                        457 => new(MathF.Pow(input1.Float, input2.Float)),
                        132 => new(MathF.Abs(input1.Float - input2.Float) < EqualsNumbersMaxDiff),
                        136 => new((input1.Float3 - input2.Float3).LengthSquared < EqualsVectorsMaxDiff),
                        140 => new(input1.Int == input2.Int),
                        421 => new(input1.Bool == input2.Bool),
                        128 => new(input1.Float < input2.Float),
                        481 => new(input1.Float > input2.Float),
                        168 => new(_ctx.GetRandomValue(input1.Float, input2.Float)),
                        176 => new(MathF.Min(input1.Float, input2.Float)),
                        180 => new(MathF.Max(input1.Float, input2.Float)),
                        580 => new(MathF.Log(input1.Float, input2.Float)),
                        570 => new(float3.Dot(input1.Float3, input2.Float3)),
                        574 => new(float3.Cross(input1.Float3, input2.Float3)),
                        190 => new((input1.Float3 - input2.Float3).Length),
                        200 => new(QuaternionUtils.AxisAngle(input1.Float3.ToNumerics(), input2.Float)),
                        204 => new(QuaternionUtils.LookRotation(input1.Float3.ToNumerics(), binary.Input2 is null ? Vector3.UnitY : input2.Float3.ToNumerics())),
                        _ => throw new UnreachableException(),
                    };

                    return new TerminalOutput(value);
                }

            case 194:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var lerp = (LerpExpressionSyntax)terminal.Node;

                    return new TerminalOutput(new RuntimeValue(Quaternion.Lerp(GetValue(lerp.From, environment).Quaternion, GetValue(lerp.To, environment).Quaternion, GetValue(lerp.Amount, environment).Float)));
                }

            case 216:
                {
                    var screenToWorld = (ScreenToWorldExpressionSyntax)terminal.Node;

                    var (near, far) = _ctx.ScreenToWorld(new float2(GetValue(screenToWorld.ScreenX, environment).Float, GetValue(screenToWorld.ScreenY, environment).Float));

                    float3 val = default;
                    if (terminal.Position == PosOut02)
                    {
                        val = near;
                    }
                    else if (terminal.Position == PosOut12)
                    {
                        val = far;
                    }
                    else
                    {
                        ThrowInvalidTerminalException(terminal.Position);
                    }

                    return new TerminalOutput(new RuntimeValue(val));
                }

            case 477:
                {
                    var worldToScreen = (WorldToScreenExpressionSyntax)terminal.Node;

                    var screenPos = _ctx.WorldToScreen(GetValue(worldToScreen.WorldPos, environment).Float3);

                    float val = default;
                    if (terminal.Position == PosOut02)
                    {
                        val = screenPos.X;
                    }
                    else if (terminal.Position == PosOut12)
                    {
                        val = screenPos.Y;
                    }
                    else
                    {
                        ThrowInvalidTerminalException(terminal.Position);
                    }

                    return new TerminalOutput(new RuntimeValue(val));
                }

            case 208:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 4), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var lineVsPlane = (LineVsPlaneExpressionSyntax)terminal.Node;

                    Vector3 lineFrom = GetValue(lineVsPlane.LineFrom, environment).Float3.ToNumerics();
                    Vector3 lineTo = GetValue(lineVsPlane.LineTo, environment).Float3.ToNumerics();
                    Vector3 planePoint = GetValue(lineVsPlane.PlanePoint, environment).Float3.ToNumerics();
                    Vector3 planeNormal = GetValue(lineVsPlane.PlaneNormal, environment).Float3.ToNumerics();

                    float t = Vector3.Dot(planePoint - lineFrom, planeNormal) / Vector3.Dot(lineTo - lineFrom, planeNormal);
                    return new TerminalOutput(new RuntimeValue((lineFrom + (t * (lineTo - lineFrom))).ToFloat3()));
                }

            case 150 or 162:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var makeVecRot = (MakeVecRotExpressionSyntax)terminal.Node;

                    float x = GetValue(makeVecRot.X, environment).Float;
                    float y = GetValue(makeVecRot.Y, environment).Float;
                    float z = GetValue(makeVecRot.Z, environment).Float;

                    return new TerminalOutput(makeVecRot.PrefabId switch
                    {
                        150 => new RuntimeValue(new float3(x, y, z)),
                        162 => new RuntimeValue(Quaternion.CreateFromYawPitchRoll(y, x, z)),
                        _ => throw new UnreachableException(),
                    });
                }

            case 156 or 442:
                {
                    var breakVecRot = (BreakVecRotExpressionnSyntax)terminal.Node;

                    var vecRot = GetValue(breakVecRot.VecRot, environment);

                    float val;
                    switch (breakVecRot.PrefabId)
                    {
                        case 156:
                            var vec = vecRot.Float3;
                            if (terminal.Position == PosOut03)
                            {
                                val = vec.X;
                            }
                            else if (terminal.Position == PosOut13)
                            {
                                val = vec.Y;
                            }
                            else if (terminal.Position == PosOut23)
                            {
                                val = vec.Z;
                            }
                            else
                            {
                                throw new InvalidTerminalException(terminal.Position);
                            }

                            break;
                        case 442:
                            var rot = vecRot.Quaternion;
                            if (terminal.Position == PosOut03)
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
                            else if (terminal.Position == PosOut13)
                            {
                                float xx = rot.X * rot.X;
                                float yy = rot.Y * rot.Y;
                                float zz = rot.Z * rot.Z;
                                float ww = rot.W * rot.W;

                                val = MathF.Atan2(2.0f * ((rot.Y * rot.Z) + (rot.W * rot.X)), ww + xx - yy - zz);
                            }
                            else if (terminal.Position == PosOut23)
                            {
                                float xx = rot.X * rot.X;
                                float yy = rot.Y * rot.Y;
                                float zz = rot.Z * rot.Z;
                                float ww = rot.W * rot.W;

                                val = MathF.Atan2(2.0f * ((rot.X * rot.Y) + (rot.W * rot.Z)), ww - xx - yy + zz);
                            }
                            else
                            {
                                throw new InvalidTerminalException(terminal.Position);
                            }

                            val *= 180f / MathF.PI; // rad to deg
                            break;
                        default:
                            throw new UnreachableException();
                    }

                    return new TerminalOutput(new RuntimeValue(val));
                }

            // **************************************** Value ****************************************
            case 36 or 38 or 42 or 449 or 451:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, terminal.Node.PrefabId is 38 or 42 ? 2 : 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var literal = (LiteralExpressionSyntax)terminal.Node;

                    return new TerminalOutput(literal.Value);
                }

            // **************************************** Variables ****************************************
            case 46 or 48 or 50 or 52 or 54 or 56:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var getVar = (GetVariableExpressionSyntax)terminal.Node;

                    return new TerminalOutput(new VariableReference(_variableAccessor.GetVariableId(environment, getVar.Variable), 0));
                }

            case 82 or 461 or 465 or 469 or 86 or 473:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var list = (ListExpressionSyntax)terminal.Node;

                    if (list.Variable is null)
                    {
                        return TerminalOutput.Disconnected;
                    }

                    var varRef = GetOutput(list.Variable, environment).Reference;

                    return new TerminalOutput(new VariableReference(varRef.VariableId, varRef.Index + (int)GetValue(list.Index, environment).Float));
                }

            default:
                {
                    if (terminal.Node is OuterExpressionSyntax)
                    {
                        var outerEnvironment = _environments[environment.OuterEnvironmentIndex];

                        var customStatement = (CustomStatementSyntax)outerEnvironment.AST.Nodes[environment.OuterPosition];

                        foreach (var (termPos, term) in customStatement.ConnectedInputTerminals)
                        {
                            if (termPos == terminal.Position)
                            {
                                return GetOutput(term, outerEnvironment);
                            }
                        }

                        return TerminalOutput.Disconnected;
                    }
                    else if (terminal.Node is CustomStatementSyntax custom)
                    {
                        var customEnvironment = (Environment)environment.BlockData[custom.Position];

                        foreach (var con in custom.AST.NonVoidOutputs)
                        {
                            if (con.OutsidePosition == terminal.Position)
                            {
                                return GetOutput(new SyntaxTerminal(custom.AST.Nodes[con.BlockPosition], con.TerminalPosition), customEnvironment);
                            }
                        }

                        return TerminalOutput.Disconnected;
                    }
                    else
                    {
                        throw new NotImplementedException($"Prefab with id {terminal.Node.PrefabId} is not implemented.");
                    }
                }
        }
    }

    private readonly struct EntryPoint
    {
        public readonly int EnvironmentIndex;
        public readonly ushort3 BlockPos;
        public readonly byte3 TerminalPos;

        public EntryPoint(int environmentIndex, ushort3 blockPos, byte3 terminalPos)
        {
            EnvironmentIndex = environmentIndex;
            BlockPos = blockPos;
            TerminalPos = terminalPos;
        }

        public void Deconstruct(out int environmentIndex, out ushort3 blockPos, out byte3 terminalPos)
        {
            environmentIndex = EnvironmentIndex;
            blockPos = BlockPos;
            terminalPos = TerminalPos;
        }
    }

    private sealed class InterpreterVariableAccessor : IVariableAccessor
    {
        private readonly FrozenDictionary<Variable, int> _globalVariableToId;
        private readonly FrozenDictionary<(int, Variable), int> _variableToId;
        private readonly VariableManager _variableManager;

        public InterpreterVariableAccessor(IEnumerable<Variable> globalVariables, IEnumerable<(int EnvironmentIndex, IEnumerable<Variable> Variables)> variables)
        {
            int varId = 0;
            _globalVariableToId = globalVariables.ToFrozenDictionary(var => var, _ => varId++);
            Dictionary<(int, Variable), int> variableToId = [];

            foreach (var (environmentIndex, environmentVariables) in variables)
            {
                foreach (var variable in environmentVariables)
                {
                    variableToId.Add((environmentIndex, variable), varId++);
                }
            }

            _variableToId = variableToId.ToFrozenDictionary();

            _variableManager = new VariableManager(_globalVariableToId.Count + _variableToId.Count);
        }

        public int GetVariableId(Environment environment, Variable variable)
            => variable.IsGlobal ? _globalVariableToId[variable] : _variableToId[(environment.Index, variable)];

        public RuntimeValue GetVariableValue(int variableId, int index)
            => _variableManager.GetVariableValue(variableId, index);

        public void SetVariableValue(int variableId, int index, RuntimeValue value)
            => _variableManager.SetVariableValue(variableId, index, value);
    }

    private sealed class Environment
    {
        public Environment(AST ast, int index, int outerEnvironmentIndex, ushort3 outerPosition)
        {
            Index = index;
            OuterEnvironmentIndex = outerEnvironmentIndex;
            AST = ast;
            OuterPosition = outerPosition;
        }

        public AST AST { get; }

        public int Index { get; }

        public int OuterEnvironmentIndex { get; }

        public ushort3 OuterPosition { get; }

        public Dictionary<ushort3, object> BlockData { get; } = [];
    }
}
