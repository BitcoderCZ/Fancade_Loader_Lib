using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Runtime.Syntax;
using BitcoderCZ.Fancade.Runtime.Syntax.Control;
using BitcoderCZ.Fancade.Runtime.Syntax.Game;
using BitcoderCZ.Fancade.Runtime.Syntax.Math;
using BitcoderCZ.Fancade.Runtime.Syntax.Objects;
using BitcoderCZ.Fancade.Runtime.Syntax.Physics;
using BitcoderCZ.Fancade.Runtime.Syntax.Sound;
using BitcoderCZ.Fancade.Runtime.Syntax.Values;
using BitcoderCZ.Fancade.Runtime.Syntax.Variables;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;

namespace BitcoderCZ.Fancade.Runtime.AstRewriters;

internal abstract class AstRewriter
{
    protected readonly Dictionary<ushort, FcAST> RewrittenAsts = [];

    public virtual FcAST RewriteAst(FcAST ast)
    {
        if (RewrittenAsts.TryGetValue(ast.PrefabId, out var rewritten))
        {
            return rewritten;
        }

        Dictionary<ushort3, StatementSyntax> statements = new(ast.Statements.Count);
        var nonVoidOutputs = ImmutableArray.CreateBuilder<(FcAST.OutsideConnection Connection, SyntaxTerminal? InsideTerminal)>(ast.NonVoidOutputs.Length);

        foreach (var (_, statement) in ast.Statements)
        {
            var newStatement = RewriteStatement(statement);

            statements.Add(newStatement.Position, newStatement);
        }

        foreach (var (connection, insideTerminal) in ast.NonVoidOutputs)
        {
            var newTerminal = RewriteExpression(insideTerminal);

            nonVoidOutputs.Add((connection, newTerminal));
        }

        rewritten = new FcAST(ast.PrefabId, ast.TerminalInfo, ast.EntryPointTerminals, statements.ToFrozenDictionary(), ast.GlobalVariables, ast.Variables, ast.VoidInputs, nonVoidOutputs.DrainToImmutable(), ast.ConnectionsFrom, ast.ConnectionsTo);

        RewrittenAsts[ast.PrefabId] = rewritten;

        return rewritten;
    }

    #region Statement
    public virtual StatementSyntax RewriteStatement(StatementSyntax node)
    {
        Debug.Assert(node is not null, $"{nameof(node)} shouldn't be null.");

        return node switch
        {
            // **************************************** Game ****************************************
            WinStatementSyntax winStatement => RewriteWinStatement(winStatement),
            LoseStatementSyntax loseStatement => RewriteLoseStatement(loseStatement),
            SetScoreStatementSyntax setScore => RewriteSetScoreStatement(setScore),
            SetCameraStatementSyntax setCamera => RewriteSetCameraStatement(setCamera),
            SetLightStatementSyntax setLight => RewriteSetLightStatement(setLight),
            MenuItemStatementSyntax menuItem => RewriteMenuItemStatement(menuItem),

            // **************************************** Objects ****************************************
            SetPositionStatementSyntax setPosition => RewriteSetPositionStatement(setPosition),
            SetVisibleStatementSyntax setVisible => RewriteSetVisibleStatement(setVisible),
            CreateObjectStatementSyntax createObject => RewriteCreateObjectStatement(createObject),
            DestroyObjectStatementSyntax destroyObject => RewriteDestroyObjectStatement(destroyObject),

            // **************************************** Sound ****************************************
            PlaySoundStatementSyntax playSound => RewritePlaySoundStatement(playSound),
            StopSoundStatementSyntax stopSound => RewriteStopSoundStatement(stopSound),
            VolumePitchStatementSyntax volumePitch => RewriteVolumePitchStatement(volumePitch),

            // **************************************** Physics ****************************************
            AddForceStatementSyntax addForce => RewriteAddForceStatement(addForce),
            SetVelocityStatementSyntax setVelocity => RewriteSetVelocityStatement(setVelocity),
            SetLockedStatementSyntax setLocked => RewriteSetLockedStatement(setLocked),
            SetMassStatementSyntax setMass => RewriteSetMassStatement(setMass),
            SetFrictionStatementSyntax setFriction => RewriteSetFrictionStatement(setFriction),
            SetBouncinessStatementSyntax setBounciness => RewriteSetBouncinessStatement(setBounciness),
            SetGravityStatementSyntax setGravity => RewriteSetGravityStatement(setGravity),
            AddConstraintStatementSyntax addConstraint => RewriteAddConstraintStatement(addConstraint),
            LinearLimitsStatementSyntax linearLimits => RewriteLinearLimitsStatement(linearLimits),
            AngularLimitsStatementSyntax angularLimits => RewriteAngularLimitsStatement(angularLimits),
            LinearSpringStatementSyntax linearSpring => RewriteLinearSpringStatement(linearSpring),
            AngularSpringStatementSyntax angularSpring => RewriteAngularSpringStatement(angularSpring),
            LinearMotorStatementSyntax linearMotor => RewriteLinearMotorStatement(linearMotor),
            AngularMotorStatementSyntax angularMotor => RewriteAngularMotorStatement(angularMotor),

            // **************************************** Control ****************************************
            IfStatementSyntax ifStatement => RewriteIfStatement(ifStatement),
            PlaySensorStatementSyntax playSensor => RewritePlaySensorStatement(playSensor),
            LateUpdateStatementSyntax lateUpdate => RewriteLateUpdateStatement(lateUpdate),
            BoxArtStatementSyntax boxArt => RewriteBoxArtStatement(boxArt),
            TouchSensorStatementSyntax touchSensor => RewriteTouchSensorStatement(touchSensor),
            SwipeSensorStatementSyntax swipeSensor => RewriteSwipeSensorStatement(swipeSensor),
            ButtonStatementSyntax button => RewriteButtonStatement(button),
            JoystickStatementSyntax joystick => RewriteJoystickStatement(joystick),
            CollisionStatementSyntax collision => RewriteCollisionStatement(collision),
            LoopStatementSyntax loop => RewriteLoopStatement(loop),

            // **************************************** Math ****************************************
            RandomSeedStatementSyntax randomSeed => RewriteRandomSeedStatement(randomSeed),

            // **************************************** Value ****************************************
            InspectStatementSyntax inspect => RewriteInspectStatement(inspect),

            // **************************************** Variables ****************************************
            SetVaribleStatementSyntax setVariable => RewriteSetVaribleStatement(setVariable),
            SetPointerStatementSyntax setPointer => RewriteSetPointerStatement(setPointer),
            IncDecNumberStatementSyntax incDecNumber => RewriteIncDecNumberStatement(incDecNumber),

            CustomStatementSyntax customStatement => RewriteCustomStatement(customStatement),

            _ => throw new UnreachableException(),
        };
    }

    #region Game
    protected virtual StatementSyntax RewriteWinStatement(WinStatementSyntax node)
        => node;

    protected virtual StatementSyntax RewriteLoseStatement(LoseStatementSyntax node)
        => node;

    protected virtual StatementSyntax RewriteSetScoreStatement(SetScoreStatementSyntax node)
    {
        var score = RewriteExpression(node.Score);
        var coins = RewriteExpression(node.Coins);

        return score == node.Score && coins == node.Coins
            ? node
            : new SetScoreStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, score, coins, node.Ranking);
    }

    protected virtual StatementSyntax RewriteSetCameraStatement(SetCameraStatementSyntax node)
    {
        var position = RewriteExpression(node.PositionTerminal);
        var rotation = RewriteExpression(node.RotationTerminal);
        var range = RewriteExpression(node.RangeTerminal);

        return position == node.PositionTerminal && rotation == node.RotationTerminal && range == node.RangeTerminal
            ? node
            : new SetCameraStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, position, rotation, range, node.Perspective);
    }

    protected virtual StatementSyntax RewriteSetLightStatement(SetLightStatementSyntax node)
    {
        var position = RewriteExpression(node.PositionTerminal);
        var rotation = RewriteExpression(node.RotationTerminal);

        return position == node.PositionTerminal && rotation == node.RotationTerminal
            ? node
            : new SetLightStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, position, rotation);
    }

    protected virtual StatementSyntax RewriteMenuItemStatement(MenuItemStatementSyntax node)
    {
        var variable = RewriteExpression(node.Variable);
        var picture = RewriteExpression(node.Picture);

        return variable == node.Variable && picture == node.Picture
            ? node
            : new MenuItemStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, variable, picture, node.Name, node.MaxBuyCount, node.PriceIncrease);
    }

    #endregion
    #region Objects
    protected virtual StatementSyntax RewriteSetPositionStatement(SetPositionStatementSyntax node)
    {
        var @object = RewriteExpression(node.ObjectTerminal);
        var position = RewriteExpression(node.PositionTerminal);
        var rotation = RewriteExpression(node.RotationTerminal);

        return @object == node.ObjectTerminal && position == node.PositionTerminal && rotation == node.RotationTerminal
            ? node
            : new SetPositionStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, @object, position, rotation);
    }

    protected virtual StatementSyntax RewriteSetVisibleStatement(SetVisibleStatementSyntax node)
    {
        var @object = RewriteExpression(node.Object);
        var visible = RewriteExpression(node.Visible);

        return @object == node.Object && visible == node.Visible
            ? node
            : new SetVisibleStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, @object, visible);
    }

    protected virtual StatementSyntax RewriteCreateObjectStatement(CreateObjectStatementSyntax node)
    {
        var original = RewriteExpression(node.Original);

        return original == node.Original
            ? node
            : new CreateObjectStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, original);
    }

    protected virtual StatementSyntax RewriteDestroyObjectStatement(DestroyObjectStatementSyntax node)
    {
        var @object = RewriteExpression(node.Object);

        return @object == node.Object
            ? node
            : new DestroyObjectStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, @object);
    }

    #endregion
    #region Sound
    protected virtual StatementSyntax RewritePlaySoundStatement(PlaySoundStatementSyntax node)
    {
        var volume = RewriteExpression(node.Volume);
        var pitch = RewriteExpression(node.Pitch);

        return volume == node.Volume && pitch == node.Pitch
            ? node
            : new PlaySoundStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, volume, pitch, node.Sound);
    }

    protected virtual StatementSyntax RewriteStopSoundStatement(StopSoundStatementSyntax node)
    {
        var channel = RewriteExpression(node.Channel);

        return channel == node.Channel
            ? node
            : new StopSoundStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, channel);
    }

    protected virtual StatementSyntax RewriteVolumePitchStatement(VolumePitchStatementSyntax node)
    {
        var channel = RewriteExpression(node.Channel);
        var volume = RewriteExpression(node.Volume);
        var pitch = RewriteExpression(node.Pitch);

        return channel == node.Channel && volume == node.Volume && pitch == node.Pitch
            ? node
            : new VolumePitchStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, channel, volume, pitch);
    }

    #endregion
    #region Physics
    protected virtual StatementSyntax RewriteAddForceStatement(AddForceStatementSyntax node)
    {
        var @object = RewriteExpression(node.Object);
        var force = RewriteExpression(node.Force);
        var applyAt = RewriteExpression(node.ApplyAt);
        var torque = RewriteExpression(node.Torque);

        return @object == node.Object && force == node.Force && applyAt == node.ApplyAt && torque == node.Torque
            ? node
            : new AddForceStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, @object, force, applyAt, torque);
    }

    protected virtual StatementSyntax RewriteSetVelocityStatement(SetVelocityStatementSyntax node)
    {
        var @object = RewriteExpression(node.Object);
        var velocity = RewriteExpression(node.Velocity);
        var spin = RewriteExpression(node.Spin);

        return @object == node.Object && velocity == node.Velocity && spin == node.Spin
            ? node
            : new SetVelocityStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, @object, velocity, spin);
    }

    protected virtual StatementSyntax RewriteSetLockedStatement(SetLockedStatementSyntax node)
    {
        var @object = RewriteExpression(node.Object);
        var position = RewriteExpression(node.PositionTerminal);
        var rotation = RewriteExpression(node.RotationTerminal);

        return @object == node.Object && position == node.PositionTerminal && rotation == node.RotationTerminal
            ? node
            : new SetLockedStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, @object, position, rotation);
    }

    protected virtual StatementSyntax RewriteSetMassStatement(SetMassStatementSyntax node)
    {
        var @object = RewriteExpression(node.Object);
        var mass = RewriteExpression(node.Mass);

        return @object == node.Object && mass == node.Mass
            ? node
            : new SetMassStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, @object, mass);
    }

    protected virtual StatementSyntax RewriteSetFrictionStatement(SetFrictionStatementSyntax node)
    {
        var @object = RewriteExpression(node.Object);
        var friction = RewriteExpression(node.Friction);

        return @object == node.Object && friction == node.Friction
            ? node
            : new SetFrictionStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, @object, friction);
    }

    protected virtual StatementSyntax RewriteSetBouncinessStatement(SetBouncinessStatementSyntax node)
    {
        var @object = RewriteExpression(node.Object);
        var bounciness = RewriteExpression(node.Bounciness);

        return @object == node.Object && bounciness == node.Bounciness
            ? node
            : new SetBouncinessStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, @object, bounciness);
    }

    protected virtual StatementSyntax RewriteSetGravityStatement(SetGravityStatementSyntax node)
    {
        var gravity = RewriteExpression(node.Gravity);

        return gravity == node.Gravity
            ? node
            : new SetGravityStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, gravity);
    }

    protected virtual StatementSyntax RewriteAddConstraintStatement(AddConstraintStatementSyntax node)
    {
        var @base = RewriteExpression(node.Base);
        var part = RewriteExpression(node.Part);
        var pivot = RewriteExpression(node.Pivot);

        return @base == node.Base && part == node.Part && pivot == node.Pivot
            ? node
            : new AddConstraintStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, @base, part, pivot);
    }

    protected virtual StatementSyntax RewriteLinearLimitsStatement(LinearLimitsStatementSyntax node)
    {
        var constraint = RewriteExpression(node.Constraint);
        var lower = RewriteExpression(node.Lower);
        var upper = RewriteExpression(node.Upper);

        return constraint == node.Constraint && lower == node.Lower && upper == node.Upper
            ? node
            : new LinearLimitsStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, constraint, lower, upper);
    }

    protected virtual StatementSyntax RewriteAngularLimitsStatement(AngularLimitsStatementSyntax node)
    {
        var constraint = RewriteExpression(node.Constraint);
        var lower = RewriteExpression(node.Lower);
        var upper = RewriteExpression(node.Upper);

        return constraint == node.Constraint && lower == node.Lower && upper == node.Upper
            ? node
            : new AngularLimitsStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, constraint, lower, upper);
    }

    protected virtual StatementSyntax RewriteLinearSpringStatement(LinearSpringStatementSyntax node)
    {
        var constraint = RewriteExpression(node.Constraint);
        var stiffness = RewriteExpression(node.Stiffness);
        var damping = RewriteExpression(node.Damping);

        return constraint == node.Constraint && stiffness == node.Stiffness && damping == node.Damping
            ? node
            : new LinearSpringStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, constraint, stiffness, damping);
    }

    protected virtual StatementSyntax RewriteAngularSpringStatement(AngularSpringStatementSyntax node)
    {
        var constraint = RewriteExpression(node.Constraint);
        var stiffness = RewriteExpression(node.Stiffness);
        var damping = RewriteExpression(node.Damping);

        return constraint == node.Constraint && stiffness == node.Stiffness && damping == node.Damping
            ? node
            : new AngularSpringStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, constraint, stiffness, damping);
    }

    protected virtual StatementSyntax RewriteLinearMotorStatement(LinearMotorStatementSyntax node)
    {
        var constraint = RewriteExpression(node.Constraint);
        var speed = RewriteExpression(node.Speed);
        var force = RewriteExpression(node.Force);

        return constraint == node.Constraint && speed == node.Speed && force == node.Force
            ? node
            : new LinearMotorStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, constraint, speed, force);
    }

    protected virtual StatementSyntax RewriteAngularMotorStatement(AngularMotorStatementSyntax node)
    {
        var constraint = RewriteExpression(node.Constraint);
        var speed = RewriteExpression(node.Speed);
        var force = RewriteExpression(node.Force);

        return constraint == node.Constraint && speed == node.Speed && force == node.Force
            ? node
            : new AngularMotorStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, constraint, speed, force);
    }

    #endregion
    #region Control
    protected virtual StatementSyntax RewriteIfStatement(IfStatementSyntax node)
    {
        var condition = RewriteExpression(node.Condition);

        return condition == node.Condition
            ? node
            : new IfStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, condition);
    }

    protected virtual StatementSyntax RewritePlaySensorStatement(PlaySensorStatementSyntax node)
        => node;

    protected virtual StatementSyntax RewriteLateUpdateStatement(LateUpdateStatementSyntax node)
        => node;

    protected virtual StatementSyntax RewriteBoxArtStatement(BoxArtStatementSyntax node)
        => node;

    protected virtual StatementSyntax RewriteTouchSensorStatement(TouchSensorStatementSyntax node)
        => node;

    protected virtual StatementSyntax RewriteSwipeSensorStatement(SwipeSensorStatementSyntax node)
        => node;

    protected virtual StatementSyntax RewriteButtonStatement(ButtonStatementSyntax node)
        => node;

    protected virtual StatementSyntax RewriteJoystickStatement(JoystickStatementSyntax node)
        => node;

    protected virtual StatementSyntax RewriteCollisionStatement(CollisionStatementSyntax node)
        => node;

    protected virtual StatementSyntax RewriteLoopStatement(LoopStatementSyntax node)
    {
        var start = RewriteExpression(node.Start);
        var stop = RewriteExpression(node.Stop);

        return start == node.Start && stop == node.Stop
            ? node
            : new LoopStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, start, stop);
    }

    #endregion
    #region Math
    protected virtual StatementSyntax RewriteRandomSeedStatement(RandomSeedStatementSyntax node)
    {
        var seed = RewriteExpression(node.Seed);

        return seed == node.Seed
            ? node
            : new RandomSeedStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, seed);
    }

    #endregion
    #region Value
    protected virtual StatementSyntax RewriteInspectStatement(InspectStatementSyntax node)
    {
        var input = RewriteExpression(node.Input);

        return input == node.Input
            ? node
            : new InspectStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, node.Type, input);
    }

    #endregion
    #region Variables
    protected virtual StatementSyntax RewriteSetVaribleStatement(SetVaribleStatementSyntax node)
    {
        var value = RewriteExpression(node.Value);

        return value == node.Value
            ? node
            : new SetVaribleStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, node.Variable, value);
    }

    protected virtual StatementSyntax RewriteSetPointerStatement(SetPointerStatementSyntax node)
    {
        var variable = RewriteExpression(node.Variable);
        var value = RewriteExpression(node.Value);

        return variable == node.Variable && value == node.Value
            ? node
            : new SetPointerStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, variable, value);
    }

    protected virtual StatementSyntax RewriteIncDecNumberStatement(IncDecNumberStatementSyntax node)
    {
        var variable = RewriteExpression(node.Variable);

        return variable == node.Variable
            ? node
            : new IncDecNumberStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, variable);
    }

    #endregion

    protected virtual StatementSyntax RewriteCustomStatement(CustomStatementSyntax node)
    {
        var ast = RewriteAst(node.AST);

        ImmutableArray<(byte3 TerminalPosition, SyntaxTerminal? ConnectedTerminal)>.Builder? builder = null;

        for (int i = 0; i < node.ConnectedInputTerminals.Length; i++)
        {
            var oldTerminal = node.ConnectedInputTerminals[i].ConnectedTerminal;
            var newTerminal = RewriteExpression(oldTerminal);
            if (newTerminal != oldTerminal)
            {
                if (builder is null)
                {
                    builder = ImmutableArray.CreateBuilder<(byte3, SyntaxTerminal?)>(node.ConnectedInputTerminals.Length);

                    for (int j = 0; j < i; j++)
                    {
                        builder.Add(node.ConnectedInputTerminals[j]);
                    }
                }
            }

            builder?.Add((node.ConnectedInputTerminals[i].TerminalPosition, newTerminal));
        }

        return ast == node.AST && builder is null
            ? node
            : new CustomStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, ast, builder is null ? node.ConnectedInputTerminals : builder.DrainToImmutable());
    }

    #endregion

    #region Expression
#pragma warning disable SA1202 // Elements should be ordered by access
    public virtual SyntaxTerminal? RewriteExpression(SyntaxTerminal? terminal)
#pragma warning restore SA1202 // Elements should be ordered by access
    {
        if (terminal is null)
        {
            return null;
        }

        var (newNode, newTerminalPosition) = terminal.Node switch
        {
            // **************************************** Game ****************************************
            ScreenSizeExpressionSyntax screenSize => RewriteScreenSizeExpression(screenSize, terminal.Position),
            AccelerometerExpressionSyntax accelerometer => RewriteAccelerometerExpression(accelerometer, terminal.Position),
            CurrentFrameExpressionSyntax currentFrame => RewriteCurrentFrameExpression(currentFrame, terminal.Position),

            // **************************************** Objects ****************************************
            GetPositionExpressionSyntax getPosition => RewriteGetPositionExpression(getPosition, terminal.Position),
            RaycastExpressionSyntax raycast => RewriteRaycastExpression(raycast, terminal.Position),
            GetSizeExpressionSyntax getSize => RewriteGetSizeExpression(getSize, terminal.Position),
            CreateObjectStatementSyntax createObject => RewriteCreateObjectExpression(createObject, terminal.Position),

            // **************************************** Sound ****************************************
            PlaySoundStatementSyntax playSound => RewritePlaySoundExpression(playSound, terminal.Position),

            // **************************************** Physics ****************************************
            GetVelocityExpressionSyntax getVelocity => RewriteGetVelocityExpression(getVelocity, terminal.Position),
            AddConstraintStatementSyntax addConstraint => RewriteAddConstraintExpression(addConstraint, terminal.Position),

            // **************************************** Control ****************************************
            TouchSensorStatementSyntax touchSensor => RewriteTouchSensorExpression(touchSensor, terminal.Position),
            SwipeSensorStatementSyntax swipeSensor => RewriteSwipeSensorExpression(swipeSensor, terminal.Position),
            JoystickStatementSyntax joystickSensor => RewriteJoystickSensorExpression(joystickSensor, terminal.Position),
            CollisionStatementSyntax collision => RewriteCollisionSensorExpression(collision, terminal.Position),
            LoopStatementSyntax loop => RewriteLoopSensorExpression(loop, terminal.Position),

            // **************************************** Math ****************************************
            UnaryExpressionSyntax unary => RewriteUnaryExpression(unary, terminal.Position),
            BinaryExpressionSyntax binary => RewriteBinaryExpression(binary, terminal.Position),
            LerpExpressionSyntax lerp => RewriteLerpExpression(lerp, terminal.Position),
            ScreenToWorldExpressionSyntax screenToWorld => RewriteScreenToWorldExpression(screenToWorld, terminal.Position),
            WorldToScreenExpressionSyntax worldToScreen => RewriteWorldToScreenExpression(worldToScreen, terminal.Position),
            LineVsPlaneExpressionSyntax lineVsPlane => RewriteLineVsPlaneExpression(lineVsPlane, terminal.Position),
            MakeVecRotExpressionSyntax makeVecRot => RewriteMakeVecRotExpression(makeVecRot, terminal.Position),
            BreakVecRotExpressionSyntax breakVecRot => RewriteBreakVecRotExpression(breakVecRot, terminal.Position),

            // **************************************** Value ****************************************
            LiteralExpressionSyntax literal => RewriteLiteralExpression(literal, terminal.Position),
            GetVariableExpressionSyntax getVariable => RewriteGetVariableExpression(getVariable, terminal.Position),
            ListExpressionSyntax list => RewriteListExpression(list, terminal.Position),

            OuterExpressionSyntax outer => RewriteOuterExpression(outer, terminal.Position),
            CustomStatementSyntax custom => RewriteCustomExpression(custom, terminal.Position),
            ObjectExpressionSyntax @object => RewriteObjectExpression(@object, terminal.Position),

            _ => throw new UnreachableException(),
        };

        return newNode == terminal.Node && newTerminalPosition == terminal.Position
            ? terminal
            : new SyntaxTerminal(newNode, newTerminalPosition);
    }

    #region Game
    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteScreenSizeExpression(ScreenSizeExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2) || terminalPos == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminalPos)} should be valid.");

        return (node, terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteAccelerometerExpression(AccelerometerExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        return (node, terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteCurrentFrameExpression(CurrentFrameExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminalPos)} should be valid.");

        return (node, terminalPos);
    }

    #endregion
    #region Objects
    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteGetPositionExpression(GetPositionExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2) || terminalPos == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminalPos)} should be valid.");

        var @object = RewriteExpression(node.Object);

        return @object == node.Object
            ? (node, terminalPos)
            : (new GetPositionExpressionSyntax(node.PrefabId, node.Position, @object), terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteRaycastExpression(RaycastExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 3) || terminalPos == TerminalDef.GetOutPosition(1, 2, 3) || terminalPos == TerminalDef.GetOutPosition(2, 2, 3), $"{nameof(terminalPos)} should be valid.");

        var from = RewriteExpression(node.From);
        var to = RewriteExpression(node.To);

        return from == node.From && to == node.To
            ? (node, terminalPos)
            : (new RaycastExpressionSyntax(node.PrefabId, node.Position, from, to), terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteGetSizeExpression(GetSizeExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2) || terminalPos == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminalPos)} should be valid.");

        var @object = RewriteExpression(node.Object);

        return @object == node.Object
            ? (node, terminalPos)
            : (new GetSizeExpressionSyntax(node.PrefabId, node.Position, @object), terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteCreateObjectExpression(CreateObjectStatementSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        var original = RewriteExpression(node.Original);

        return original == node.Original
            ? (node, terminalPos)
            : (new CreateObjectStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, original), terminalPos);
    }

    #endregion
    #region Sound
    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewritePlaySoundExpression(PlaySoundStatementSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        var volume = RewriteExpression(node.Volume);
        var pitch = RewriteExpression(node.Pitch);

        return volume == node.Volume && pitch == node.Pitch
            ? (node, terminalPos)
            : (new PlaySoundStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, volume, pitch, node.Sound), terminalPos);
    }

    #endregion
    #region Physics
    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteGetVelocityExpression(GetVelocityExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2) || terminalPos == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminalPos)} should be valid.");

        var @object = RewriteExpression(node.Object);

        return @object == node.Object
            ? (node, terminalPos)
            : (new GetVelocityExpressionSyntax(node.PrefabId, node.Position, @object), terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteAddConstraintExpression(AddConstraintStatementSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminalPos)} should be valid.");

        var @base = RewriteExpression(node.Base);
        var part = RewriteExpression(node.Part);
        var pivot = RewriteExpression(node.Pivot);

        return @base == node.Base && part == node.Part && pivot == node.Pivot
            ? (node, terminalPos)
            : (new AddConstraintStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, @base, part, pivot), terminalPos);
    }

    #endregion
    #region Control
    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteTouchSensorExpression(TouchSensorStatementSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(1, 2, 3) || terminalPos == TerminalDef.GetOutPosition(2, 2, 3), $"{nameof(terminalPos)} should be valid.");

        return (node, terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteSwipeSensorExpression(SwipeSensorStatementSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminalPos)} should be valid.");

        return (node, terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteJoystickSensorExpression(JoystickStatementSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        return (node, terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteCollisionSensorExpression(CollisionStatementSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(1, 2, 4) || terminalPos == TerminalDef.GetOutPosition(2, 2, 4) || terminalPos == TerminalDef.GetOutPosition(3, 2, 4), $"{nameof(terminalPos)} should be valid.");

        var firstObject = RewriteExpression(node.FirstObject);

        return firstObject == node.FirstObject
            ? (node, terminalPos)
            : (new CollisionStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, firstObject), terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteLoopSensorExpression(LoopStatementSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminalPos)} should be valid.");

        var start = RewriteExpression(node.Start);
        var stop = RewriteExpression(node.Stop);

        return start == node.Start && stop == node.Stop
            ? (node, terminalPos)
            : (new LoopStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, start, stop), terminalPos);
    }

    #endregion
    #region Math
    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteUnaryExpression(UnaryExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminalPos)} should be valid.");

        var input = RewriteExpression(node.Input);

        return input == node.Input
            ? (node, terminalPos)
            : (new UnaryExpressionSyntax(node.PrefabId, node.Position, input), terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteBinaryExpression(BinaryExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        var input1 = RewriteExpression(node.Input1);
        var input2 = RewriteExpression(node.Input2);

        return input1 == node.Input1 && input2 == node.Input2
            ? (node, terminalPos)
            : (new BinaryExpressionSyntax(node.PrefabId, node.Position, input1, input2), terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteLerpExpression(LerpExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminalPos)} should be valid.");

        var from = RewriteExpression(node.From);
        var to = RewriteExpression(node.To);
        var amount = RewriteExpression(node.Amount);

        return from == node.From && to == node.To && amount == node.Amount
            ? (node, terminalPos)
            : (new LerpExpressionSyntax(node.PrefabId, node.Position, from, to, amount), terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteScreenToWorldExpression(ScreenToWorldExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2) || terminalPos == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminalPos)} should be valid.");

        var screenX = RewriteExpression(node.ScreenX);
        var screenY = RewriteExpression(node.ScreenY);

        return screenX == node.ScreenX && screenY == node.ScreenY
            ? (node, terminalPos)
            : (new ScreenToWorldExpressionSyntax(node.PrefabId, node.Position, screenX, screenY), terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteWorldToScreenExpression(WorldToScreenExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2) || terminalPos == TerminalDef.GetOutPosition(1, 2, 2), $"{nameof(terminalPos)} should be valid.");

        var worldPos = RewriteExpression(node.WorldPos);

        return worldPos == node.WorldPos
            ? (node, terminalPos)
            : (new WorldToScreenExpressionSyntax(node.PrefabId, node.Position, worldPos), terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteLineVsPlaneExpression(LineVsPlaneExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 4), $"{nameof(terminalPos)} should be valid.");

        var lineFrom = RewriteExpression(node.LineFrom);
        var lineTo = RewriteExpression(node.LineTo);
        var planePoint = RewriteExpression(node.PlanePoint);
        var planeNormal = RewriteExpression(node.PlaneNormal);

        return lineFrom == node.LineFrom && lineTo == node.LineTo && planePoint == node.PlanePoint && planeNormal == node.PlaneNormal
            ? (node, terminalPos)
            : (new LineVsPlaneExpressionSyntax(node.PrefabId, node.Position, lineFrom, lineTo, planePoint, planeNormal), terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteMakeVecRotExpression(MakeVecRotExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminalPos)} should be valid.");

        var x = RewriteExpression(node.X);
        var y = RewriteExpression(node.Y);
        var z = RewriteExpression(node.Z);

        return x == node.X && y == node.Y && z == node.Z
            ? (node, terminalPos)
            : (new MakeVecRotExpressionSyntax(node.PrefabId, node.Position, x, y, z), terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteBreakVecRotExpression(BreakVecRotExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 3) || terminalPos == TerminalDef.GetOutPosition(1, 2, 3) || terminalPos == TerminalDef.GetOutPosition(2, 2, 3), $"{nameof(terminalPos)} should be valid.");

        var vecRot = RewriteExpression(node.VecRot);

        return vecRot == node.VecRot
            ? (node, terminalPos)
            : (new BreakVecRotExpressionSyntax(node.PrefabId, node.Position, vecRot), terminalPos);
    }

    #endregion
    #region Value
    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteLiteralExpression(LiteralExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, node.PrefabId is 38 or 42 ? 2 : 1), $"{nameof(terminalPos)} should be valid.");

        return (node, terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteGetVariableExpression(GetVariableExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminalPos)} should be valid.");

        return (node, terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteListExpression(ListExpressionSyntax node, byte3 terminalPos)
    {
        Debug.Assert(terminalPos == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminalPos)} should be valid.");

        var variable = RewriteExpression(node.Variable);
        var index = RewriteExpression(node.Index);

        return variable == node.Variable && index == node.Index
            ? (node, terminalPos)
            : (new ListExpressionSyntax(node.PrefabId, node.Position, variable, index), terminalPos);
    }

    #endregion

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteOuterExpression(OuterExpressionSyntax node, byte3 terminalPos)
        => (node, terminalPos);

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteCustomExpression(CustomStatementSyntax node, byte3 terminalPos)
    {
        var ast = RewriteAst(node.AST);

        ImmutableArray<(byte3 TerminalPosition, SyntaxTerminal? ConnectedTerminal)>.Builder? builder = null;

        for (int i = 0; i < node.ConnectedInputTerminals.Length; i++)
        {
            var oldTerminal = node.ConnectedInputTerminals[i].ConnectedTerminal;
            var newTerminal = RewriteExpression(oldTerminal);
            if (newTerminal != oldTerminal)
            {
                if (builder is null)
                {
                    builder = ImmutableArray.CreateBuilder<(byte3, SyntaxTerminal?)>(node.ConnectedInputTerminals.Length);

                    for (int j = 0; j < i; j++)
                    {
                        builder.Add(node.ConnectedInputTerminals[j]);
                    }
                }
            }

            builder?.Add((node.ConnectedInputTerminals[i].TerminalPosition, newTerminal));
        }

        return ast == node.AST && builder is null
            ? (node, terminalPos)
            : (new CustomStatementSyntax(node.PrefabId, node.Position, node.OutVoidConnections, ast, builder is null ? node.ConnectedInputTerminals : builder.DrainToImmutable()), terminalPos);
    }

    protected virtual (SyntaxNode Node, byte3 TerminalPosition) RewriteObjectExpression(ObjectExpressionSyntax node, byte3 terminalPos)
        => (node, terminalPos);
    #endregion
}
