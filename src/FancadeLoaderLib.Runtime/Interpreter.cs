using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
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
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using static FancadeLoaderLib.Runtime.Utils.ThrowHelper;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime;

public sealed class Interpreter : IAstRunner
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

    private readonly TimeSpan _timeout;
    private readonly Stopwatch? _timeoutWatch;

    public Interpreter(AST ast, IRuntimeContext ctx)
        : this(ast, ctx, TimeSpan.FromSeconds(3))
    {
    }

    public Interpreter(AST ast, IRuntimeContext ctx, TimeSpan timeout)
        : this(ast, ctx, timeout, 4)
    {
    }

    public Interpreter(AST ast, IRuntimeContext ctx, TimeSpan timeout, int maxDepth)
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

        _timeout = timeout;

        if (_timeout != Timeout.InfiniteTimeSpan)
        {
            _timeoutWatch = new Stopwatch();
        }
    }

    public IVariableAccessor VariableAccessor => _variableAccessor;

    public IEnumerable<Variable> GlobalVariables => _variableAccessor.GlobalVariables.Select(item => item.Key);

    public Action RunFrame()
    {
        if (_timeoutWatch is not null)
        {
            if (_timeoutWatch.IsRunning)
            {
                throw new InvalidOperationException("This method cannot be called concurrently.");
            }

            _timeoutWatch.Start();
        }

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

            _timeoutWatch?.Reset();
        };
    }

    public Span<RuntimeValue> GetGlobalVariableValue(Variable variable)
    {
        if (!variable.IsGlobal)
        {
            ThrowArgumentException($"{nameof(variable)} must be global.", nameof(variable));
        }

        return _variableAccessor.GetVariableValues(_variableAccessor.GetVariableId(_environments[0], variable));
    }

    private static void InitEnvironments(Environment outer, List<Environment> environments, List<ImmutableArray<Variable>> variables, int maxDepth, int depth = 1)
    {
        if (depth > maxDepth)
        {
            throw new EnvironmentDepthLimitReachedException();
        }

        foreach (var node in outer.AST.Statements.Values)
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
            ThrowIfTimeout();

            var (environmentIndex, blockPos, terminalPos) = item;
            var environment = _environments[environmentIndex];
            var statement = environment.AST.Statements[blockPos];

            int nextCount = 0;
            executeNextSpan[nextCount++] = TerminalDef.AfterPosition;

            // faster than switching on type
            switch (statement.PrefabId)
            {
                // **************************************** Game ****************************************
                case 252:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var win = (WinStatementSyntax)statement;

                        _ctx.Win(win.Delay);
                    }

                    break;
                case 256:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var lose = (LoseStatementSyntax)statement;

                        _ctx.Lose(lose.Delay);
                    }

                    break;
                case 260:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var setScore = (SetScoreStatementSyntax)statement;

                        _ctx.SetScore(setScore.Score is null ? null : GetValue(setScore.Score, environment).Float, setScore.Coins is null ? null : GetValue(setScore.Coins, environment).Float, setScore.Ranking);
                    }

                    break;
                case 268:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be valid.");
                        var setCamera = (SetCameraStatementSyntax)statement;

                        _ctx.SetCamera(setCamera.PositionTerminal is null ? null : GetValue(setCamera.PositionTerminal, environment).Float3, setCamera.RotationTerminal is null ? null : GetValue(setCamera.RotationTerminal, environment).Quaternion, setCamera.RangeTerminal is null ? null : GetValue(setCamera.RangeTerminal, environment).Float, setCamera.Perspective);
                    }

                    break;
                case 274:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var setLight = (SetLightStatementSyntax)statement;

                        _ctx.SetLight(setLight.PositionTerminal is null ? null : GetValue(setLight.PositionTerminal, environment).Float3, setLight.RotationTerminal is null ? null : GetValue(setLight.RotationTerminal, environment).Quaternion);
                    }

                    break;
                case 584:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var menuItem = (MenuItemStatementSyntax)statement;

                        _ctx.MenuItem(menuItem.Variable is null ? null : GetOutput(menuItem.Variable, environment).Reference, (FcObject)GetValue(menuItem.Picture, environment).Int, menuItem.Name, menuItem.MaxBuyCount, menuItem.PriceIncrease);
                    }

                    break;

                // **************************************** Objects ****************************************
                case 282:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be valid.");
                        var setPosition = (SetPositionStatementSyntax)statement;

                        if (setPosition.ObjectTerminal is not null)
                        {
                            _ctx.SetPosition((FcObject)GetValue(setPosition.ObjectTerminal, environment).Int, setPosition.PositionTerminal is null ? null : GetValue(setPosition.PositionTerminal, environment).Float3, setPosition.RotationTerminal is null ? null : GetValue(setPosition.RotationTerminal, environment).Quaternion);
                        }
                    }

                    break;
                case 306:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var setVisible = (SetVisibleStatementSyntax)statement;

                        if (setVisible.Object is not null)
                        {
                            _ctx.SetVisible((FcObject)GetValue(setVisible.Object, environment).Int, GetValue(setVisible.Visible, environment).Bool);
                        }
                    }

                    break;
                case 316:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var createObject = (CreateObjectStatementSyntax)statement;

                        if (createObject.Original is not null)
                        {
                            environment.BlockData[createObject.Position] = _ctx.CreateObject((FcObject)GetValue(createObject.Original, environment).Int);
                        }
                    }

                    break;
                case 320:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var destroyObject = (DestroyObjectStatementSyntax)statement;

                        if (destroyObject.Object is not null)
                        {
                            _ctx.DestroyObject((FcObject)GetValue(destroyObject.Object, environment).Int);
                        }
                    }

                    break;

                // **************************************** Sound ****************************************
                case 264:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var playSound = (PlaySoundStatementSyntax)statement;

                        // TODO: figure out if the defaults are correct
                        float volume = playSound.Volume is null ? 1f : GetValue(playSound.Volume, environment).Float;
                        float pitch = playSound.Pitch is null ? 1f : GetValue(playSound.Pitch, environment).Float;

                        environment.BlockData[playSound.Position] = _ctx.PlaySound(volume, pitch, playSound.Sound);
                    }

                    break;
                case 397:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var stopSound = (StopSoundStatementSyntax)statement;

                        if (stopSound.Channel is not null)
                        {
                            _ctx.StopSound(GetValue(stopSound.Channel, environment).Float);
                        }
                    }

                    break;
                case 391:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var volumePitch = (VolumePitchStatementSyntax)statement;

                        if (volumePitch.Channel is not null)
                        {
                            _ctx.AdjustVolumePitch(GetValue(volumePitch.Channel, environment).Float, volumePitch.Volume is null ? null : GetValue(volumePitch.Volume, environment).Float, volumePitch.Pitch is null ? null : GetValue(volumePitch.Pitch, environment).Float);
                        }
                    }

                    break;

                // **************************************** Physics ****************************************
                case 298:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(4), $"{nameof(terminalPos)} should be valid.");
                        var addForce = (AddForceStatementSyntax)statement;

                        if (addForce.Object is not null)
                        {
                            _ctx.AddForce((FcObject)GetValue(addForce.Object, environment).Int, addForce.Force is null ? null : GetValue(addForce.Force, environment).Float3, addForce.ApplyAt is null ? null : GetValue(addForce.ApplyAt, environment).Float3, addForce.Torque is null ? null : GetValue(addForce.Torque, environment).Float3);
                        }
                    }

                    break;
                case 292:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be valid.");
                        var setVelocity = (SetVelocityStatementSyntax)statement;

                        if (setVelocity.Object is not null)
                        {
                            _ctx.SetVelocity((FcObject)GetValue(setVelocity.Object, environment).Int, setVelocity.Velocity is null ? null : GetValue(setVelocity.Velocity, environment).Float3, setVelocity.Spin is null ? null : GetValue(setVelocity.Spin, environment).Float3);
                        }
                    }

                    break;
                case 310:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be valid.");
                        var setLocked = (SetLockedStatementSyntax)statement;

                        if (setLocked.Object is not null)
                        {
                            _ctx.SetLocked((FcObject)GetValue(setLocked.Object, environment).Int, setLocked.PositionTerminal is null ? null : GetValue(setLocked.PositionTerminal, environment).Float3, setLocked.RotationTerminal is null ? null : GetValue(setLocked.RotationTerminal, environment).Float3);
                        }
                    }

                    break;
                case 328:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var setMass = (SetMassStatementSyntax)statement;

                        if (setMass.Object is not null && setMass.Mass is not null)
                        {
                            _ctx.SetMass((FcObject)GetValue(setMass.Object, environment).Int, GetValue(setMass.Mass, environment).Float);
                        }
                    }

                    break;
                case 332:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var setFriction = (SetFrictionStatementSyntax)statement;

                        if (setFriction.Object is not null && setFriction.Friction is not null)
                        {
                            _ctx.SetFriction((FcObject)GetValue(setFriction.Object, environment).Int, GetValue(setFriction.Friction, environment).Float);
                        }
                    }

                    break;
                case 336:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var setBounciness = (SetBouncinessStatementSyntax)statement;

                        if (setBounciness.Object is not null && setBounciness.Bounciness is not null)
                        {
                            _ctx.SetBounciness((FcObject)GetValue(setBounciness.Object, environment).Int, GetValue(setBounciness.Bounciness, environment).Float);
                        }
                    }

                    break;
                case 324:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(2), $"{nameof(terminalPos)} should be valid.");
                        var setGravity = (SetGravityStatementSyntax)statement;

                        if (setGravity.Gravity is not null)
                        {
                            _ctx.SetGravity(GetValue(setGravity.Gravity, environment).Float3);
                        }
                    }

                    break;
                case 340:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be valid.");
                        var addConstraint = (AddConstraintStatementSyntax)statement;

                        if (addConstraint.Base is not null && addConstraint.Part is not null)
                        {
                            environment.BlockData[addConstraint.Position] = _ctx.AddConstraint((FcObject)GetValue(addConstraint.Base, environment).Int, (FcObject)GetValue(addConstraint.Part, environment).Int, addConstraint.Pivot is null ? null : GetValue(addConstraint.Pivot, environment).Float3);
                        }
                    }

                    break;
                case 346:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be valid.");
                        var linearLimits = (LinearLimitsStatementSyntax)statement;

                        if (linearLimits.Constraint is not null)
                        {
                            _ctx.LinearLimits((FcConstraint)GetValue(linearLimits.Constraint, environment).Int, linearLimits.Lower is null ? null : GetValue(linearLimits.Lower, environment).Float3, linearLimits.Upper is null ? null : GetValue(linearLimits.Upper, environment).Float3);
                        }
                    }

                    break;
                case 352:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be valid.");
                        var angularLimits = (AngularLimitsStatementSyntax)statement;

                        if (angularLimits.Constraint is not null)
                        {
                            _ctx.AngularLimits((FcConstraint)GetValue(angularLimits.Constraint, environment).Int, angularLimits.Lower is null ? null : GetValue(angularLimits.Lower, environment).Float3, angularLimits.Upper is null ? null : GetValue(angularLimits.Upper, environment).Float3);
                        }
                    }

                    break;
                case 358:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be valid.");
                        var linearSpring = (LinearSpringStatementSyntax)statement;

                        if (linearSpring.Constraint is not null)
                        {
                            _ctx.LinearSpring((FcConstraint)GetValue(linearSpring.Constraint, environment).Int, linearSpring.Stiffness is null ? null : GetValue(linearSpring.Stiffness, environment).Float3, linearSpring.Damping is null ? null : GetValue(linearSpring.Damping, environment).Float3);
                        }
                    }

                    break;
                case 364:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be valid.");
                        var angularSpring = (AngularSpringStatementSyntax)statement;

                        if (angularSpring.Constraint is not null)
                        {
                            _ctx.AngularSpring((FcConstraint)GetValue(angularSpring.Constraint, environment).Int, angularSpring.Stiffness is null ? null : GetValue(angularSpring.Stiffness, environment).Float3, angularSpring.Damping is null ? null : GetValue(angularSpring.Damping, environment).Float3);
                        }
                    }

                    break;
                case 370:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be valid.");
                        var linearMotor = (LinearMotorStatementSyntax)statement;

                        if (linearMotor.Constraint is not null)
                        {
                            _ctx.LinearMotor((FcConstraint)GetValue(linearMotor.Constraint, environment).Int, linearMotor.Speed is null ? null : GetValue(linearMotor.Speed, environment).Float3, linearMotor.Force is null ? null : GetValue(linearMotor.Force, environment).Float3);
                        }
                    }

                    break;
                case 376:
                    {
                        Debug.Assert(terminalPos == TerminalDef.GetBeforePosition(3), $"{nameof(terminalPos)} should be valid.");
                        var angularMotor = (AngularMotorStatementSyntax)statement;

                        if (angularMotor.Constraint is not null)
                        {
                            _ctx.AngularMotor((FcConstraint)GetValue(angularMotor.Constraint, environment).Int, angularMotor.Speed is null ? null : GetValue(angularMotor.Speed, environment).Float3, angularMotor.Force is null ? null : GetValue(angularMotor.Force, environment).Float3);
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

                        if (collision.FirstObject is not null && _ctx.TryGetCollision((FcObject)GetValue(collision.FirstObject, environment).Int, out FcObject secondObject, out float impulse, out float3 normal))
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
                            ThrowIfTimeout();

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
                            var output = GetOutput(inspect.Input, environment);

                            _ctx.InspectValue(output.GetValue(_variableAccessor), inspect.Type, output.IsReference ? _variableAccessor.GetVariable(output.Reference.VariableId).Variable.Name : null, environment.AST.PrefabId, inspect.Position);
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
        for (int i = statement.OutVoidConnections.Length - 1; i >= 0; i--)
        {
            var connection = statement.OutVoidConnections[i];

            if (connection.FromVoxel == terminalPos)
            {
                if (connection.IsToOutside)
                {
                    var outerEnvironment = _environments[environment.OuterEnvironmentIndex];

                    PushAfter(outerEnvironment.AST.Statements[environment.OuterPosition], (byte3)connection.ToVoxel, outerEnvironment, stack);
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

        ThrowIfTimeout();

        // faster than switching on type
        switch (terminal.Node.PrefabId)
        {
            // **************************************** Game ****************************************
            case 220:
                {
                    Debug.Assert(terminal.Node is ScreenSizeExpressionSyntax, $"{nameof(terminal)}.{nameof(terminal.Node)} should be {nameof(ScreenSizeExpressionSyntax)}");

                    var size = _ctx.ScreenSize;

                    float val;
                    if (terminal.Position == PosOut02)
                    {
                        val = size.X;
                    }
                    else if (terminal.Position == PosOut12)
                    {
                        val = size.Y;
                    }
                    else
                    {
                        throw new InvalidTerminalException(terminal.Position);
                    }

                    return new TerminalOutput(new RuntimeValue(val));
                }

            case 224:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is AccelerometerExpressionSyntax, $"{nameof(terminal)}.{nameof(terminal.Node)} should be {nameof(AccelerometerExpressionSyntax)}");

                    return new TerminalOutput(new RuntimeValue(_ctx.Accelerometer));
                }

            case 564:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    Debug.Assert(terminal.Node is CurrentFrameExpressionSyntax, $"{nameof(terminal)}.{nameof(terminal.Node)} should be {nameof(CurrentFrameExpressionSyntax)}");

                    return new TerminalOutput(new RuntimeValue(_ctx.CurrentFrame));
                }

            // **************************************** Objects ****************************************
            case 278:
                {
                    var getPosition = (GetPositionExpressionSyntax)terminal.Node;

                    RuntimeValue val;
                    if (getPosition.Object is null)
                    {
                        if (terminal.Position == PosOut02)
                        {
                            val = new(float3.Zero);
                        }
                        else if (terminal.Position == PosOut12)
                        {
                            val = new(Quaternion.Identity);
                        }
                        else
                        {
                            throw new InvalidTerminalException(terminal.Position);
                        }
                    }
                    else
                    {
                        var (position, rotation) = _ctx.GetObjectPosition((FcObject)GetValue(getPosition.Object, environment).Int);

                        if (terminal.Position == PosOut02)
                        {
                            val = new(position);
                        }
                        else if (terminal.Position == PosOut12)
                        {
                            val = new(rotation);
                        }
                        else
                        {
                            throw new InvalidTerminalException(terminal.Position);
                        }
                    }

                    return new TerminalOutput(val);
                }

            case 228:
                {
                    var raycast = (RaycastExpressionSyntax)terminal.Node;

                    var (hit, hitPos, hitObj) = _ctx.Raycast(GetValue(raycast.From, environment).Float3, GetValue(raycast.To, environment).Float3);

                    RuntimeValue val;
                    if (terminal.Position == PosOut03)
                    {
                        val = new RuntimeValue(hit);
                    }
                    else if (terminal.Position == PosOut13)
                    {
                        val = new RuntimeValue(hitPos);
                    }
                    else if (terminal.Position == PosOut23)
                    {
                        val = new RuntimeValue(hitObj.Value);
                    }
                    else
                    {
                        throw new InvalidTerminalException(terminal.Position);
                    }

                    return new TerminalOutput(val);
                }

            case 489:
                {
                    var getSize = (GetSizeExpressionSyntax)terminal.Node;

                    float3 val;
                    if (getSize.Object is null)
                    {
                        if (terminal.Position == PosOut02 || terminal.Position == PosOut12)
                        {
                            val = float3.Zero;
                        }
                        else
                        {
                            throw new InvalidTerminalException(terminal.Position);
                        }
                    }
                    else
                    {
                        var (min, max) = _ctx.GetSize((FcObject)GetValue(getSize.Object, environment).Int);

                        if (terminal.Position == PosOut02)
                        {
                            val = min;
                        }
                        else if (terminal.Position == PosOut12)
                        {
                            val = max;
                        }
                        else
                        {
                            throw new InvalidTerminalException(terminal.Position);
                        }
                    }

                    return new TerminalOutput(new RuntimeValue(val));
                }

            case 316:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var createObject = (CreateObjectStatementSyntax)terminal.Node;

                    return new TerminalOutput(new RuntimeValue((int)environment.BlockData.GetValueOrDefault(createObject.Position, 0)));
                }

            // **************************************** Sound ****************************************
            case 264:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var playSound = (PlaySoundStatementSyntax)terminal.Node;

                    return new TerminalOutput(new RuntimeValue((float)environment.BlockData.GetValueOrDefault(playSound.Position, -1f)));
                }

            // **************************************** Physics ****************************************
            case 288:
                {
                    var getVelocity = (GetVelocityExpressionSyntax)terminal.Node;

                    float3 val;
                    if (getVelocity.Object is null)
                    {
                        if (terminal.Position == PosOut02 || terminal.Position == PosOut12)
                        {
                            val = float3.Zero;
                        }
                        else
                        {
                            throw new InvalidTerminalException(terminal.Position);
                        }
                    }
                    else
                    {
                        var (velocity, spin) = _ctx.GetVelocity((FcObject)GetValue(getVelocity.Object, environment).Int);

                        if (terminal.Position == PosOut02)
                        {
                            val = velocity;
                        }
                        else if (terminal.Position == PosOut12)
                        {
                            val = spin;
                        }
                        else
                        {
                            throw new InvalidTerminalException(terminal.Position);
                        }
                    }

                    return new TerminalOutput(new RuntimeValue(val));
                }

            case 340:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var addConstraint = (AddConstraintStatementSyntax)terminal.Node;

                    var constraint = (FcConstraint)environment.BlockData.GetValueOrDefault(addConstraint.Position, FcConstraint.Null);

                    return new TerminalOutput(new RuntimeValue(constraint.Value));
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

                    var (otherObject, impulse, normal) = ((FcObject, float, float3))environment.BlockData.GetValueOrDefault(collision.Position, (FcObject.Null, 0f, float3.Zero));

                    RuntimeValue val;
                    if (terminal.Position == PosOut14)
                    {
                        val = new(otherObject.Value);
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
                        172 => new(FcMod(input1.Float, input2.Float)),
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

                    float FcMod(float a, float b)
                    {
                        float res = a % b;

                        return res >= 0f ? res : b + res;
                    }
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
                    switch (terminal.Node)
                    {
                        case OuterExpressionSyntax:
                            {
                                var outerEnvironment = _environments[environment.OuterEnvironmentIndex];

                                var customStatement = (CustomStatementSyntax)outerEnvironment.AST.Statements[environment.OuterPosition];

                                foreach (var (termPos, term) in customStatement.ConnectedInputTerminals)
                                {
                                    if (termPos == terminal.Position)
                                    {
                                        return GetOutput(term, outerEnvironment);
                                    }
                                }

                                return TerminalOutput.Disconnected;
                            }

                        case CustomStatementSyntax custom:
                            {
                                var customEnvironment = (Environment)environment.BlockData[custom.Position];

                                foreach (var (con, conTerm) in custom.AST.NonVoidOutputs)
                                {
                                    if (con.OutsidePosition == terminal.Position)
                                    {
                                        return GetOutput(conTerm, customEnvironment);
                                    }
                                }

                                return TerminalOutput.Disconnected;
                            }

                        case ObjectExpressionSyntax:
                            {
                                return new TerminalOutput(new RuntimeValue(_ctx.GetObject(terminal.Node.Position, terminal.Position).Value));
                            }

                        default:
                            throw new NotImplementedException($"Prefab with id {terminal.Node.PrefabId} is not implemented.");
                    }
                }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfTimeout()
    {
        if (_timeout != Timeout.InfiniteTimeSpan)
        {
            Debug.Assert(_timeoutWatch is not null, $"{nameof(_timeoutWatch)} should not be null when {nameof(_timeout)} is not infinite.");

            if (_timeoutWatch.Elapsed > _timeout)
            {
                ThrowTimeoutException();
            }
        }
    }

    private sealed class InterpreterVariableAccessor : IVariableAccessor
    {
        private readonly FrozenDictionary<Variable, int> _globalVariableToId;
        private readonly FrozenDictionary<(int, Variable), int> _variableToId;
        private readonly (int, Variable)[] _variables;

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

            _variables =
            [
                .. _globalVariableToId.OrderBy(item => item.Value).Select(item => (-1, item.Key)),
                .. _variableToId.OrderBy(item => item.Value).Select(item => item.Key),
            ];

            _variableManager = new VariableManager(_globalVariableToId.Count + _variableToId.Count);
        }

        public IEnumerable<KeyValuePair<Variable, int>> GlobalVariables => _globalVariableToId;

        public int GetVariableId(Environment environment, Variable variable)
            => variable.IsGlobal ? _globalVariableToId[variable] : _variableToId[(environment.Index, variable)];

        public (int EnvironmentIndex, Variable Variable) GetVariable(int variableId)
            => _variables[variableId];

        public RuntimeValue GetVariableValue(int variableId, int index)
            => _variableManager.GetVariableValue(variableId, index);

        public void SetVariableValue(int variableId, int index, RuntimeValue value)
            => _variableManager.SetVariableValue(variableId, index, value);

        public Span<RuntimeValue> GetVariableValues(int variableId)
            => _variableManager.GetVariableValues(variableId);
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
