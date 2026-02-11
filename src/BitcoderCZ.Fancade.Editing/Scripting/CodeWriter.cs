// <copyright file="CodeWriter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Scripting.Exceptions;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Fancade.Editing.Utils;
using System.Diagnostics;
using static BitcoderCZ.Utils.ThrowHelper;
using Terminal = BitcoderCZ.Fancade.Editing.Scripting.Node.Terminal;
using TerminalStore = BitcoderCZ.Fancade.Editing.Scripting.Node.TerminalStore;

namespace BitcoderCZ.Fancade.Editing.Scripting;

/// <summary>
/// A helper class for writing fancade code.
/// </summary>
public sealed partial class CodeWriter : IDisposable
{
    private readonly CodeGraph.Builder _codeBuilder;

    private readonly TerminalConnector _connector;

    private readonly Dictionary<string, object?> _labels = [];
    private readonly List<(TerminalStore Store, string LabelName)> _gotos = [];
    private readonly Queue<string> _labelsToProcess = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeWriter"/> class.
    /// </summary>
    /// <param name="builder">The <see cref="CodeGraph.Builder"/> used to place blocks.</param>
    public CodeWriter(CodeGraph.Builder builder)
    {
        _codeBuilder = builder;
        _connector = new TerminalConnector(_codeBuilder);
    }

    /// <summary>
    /// Represents a non-void output.
    /// </summary>
    public interface IExpression
    {
        /// <summary>
        /// Gets the type of the <see cref="IExpression"/>.
        /// </summary>
        /// <value>Type of the <see cref="IExpression"/>.</value>
        SignalType Type { get; }

        /// <summary>
        /// Writes the <see cref="IExpression"/> to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="CodeWriter"/> to write the <see cref="IExpression"/> to.</param>
        /// <returns>An <see cref="Terminal"/> representing the written output of the expression.</returns>
        Terminal WriteTo(CodeWriter writer);
    }

    /// <summary>
    /// Gets the underlying <see cref="CodeGraph.Builder"/>.
    /// </summary>
    /// <value>The underlying <see cref="CodeGraph.Builder"/>.</value>
    public CodeGraph.Builder Builder => _codeBuilder;

    /// <summary>
    /// Gets the underlying <see cref="TerminalConnector"/>.
    /// </summary>
    /// <value>The underlying <see cref="TerminalConnector"/>.</value>
    public TerminalConnector Connector => _connector;

    /// <summary>
    /// Marks the next statement with a name, execution can later be jumped to the label using <see cref="Goto(string)"/>.
    /// </summary>
    /// <param name="name">Name of the label, must be unique.</param>
    /// <exception cref="InvalidOperationException">A label with same name as <paramref name="name"/> was already defined.</exception>
    public void Label(string name)
    {
        if (_labels.ContainsKey(name))
        {
            throw new InvalidOperationException($"Label '{name}' was already defined.");
        }

        _labels.Add(name, null);
        _labelsToProcess.Enqueue(name);
    }

    /// <summary>
    /// Jumps the current execution to a label. No statements besides labels should be placed after the goto.
    /// </summary>
    /// <param name="labelName">Name of the label to jump to.</param>
    public void Goto(string labelName)
    {
        // TODO: is using .Store here ok, or should I expose LastStore
        _gotos.Add((_connector.Store, labelName));

        while (_labelsToProcess.TryDequeue(out string? label))
        {
            _labels[label] = labelName;
        }

        _connector.SetLast(default);
    }

    /// <summary>
    /// Places a custom block.
    /// </summary>
    /// <param name="blockDef">The block to place.</param>
    /// <param name="expressions">Inputs to the block.</param>
    /// <returns>Outputs of the placed block.</returns>
    public IEnumerable<Terminal> CustomBlock(BlockDef blockDef, params ReadOnlySpan<IExpression> expressions)
        => CustomBlock(blockDef, expressions, null);

    /// <summary>
    /// Places a custom block.
    /// </summary>
    /// <param name="blockDef">The block to place.</param>
    /// <param name="expressions">Inputs to the block.</param>
    /// <param name="voidTerminalCallback">Callback for void outputs.</param>
    /// <returns>Outputs of the placed block.</returns>
    public IEnumerable<Terminal> CustomBlock(BlockDef blockDef, ReadOnlySpan<IExpression> expressions, Action<CodeWriter, TerminalDef, Node>? voidTerminalCallback)
    {
        if (blockDef.BlockType is not ScriptBlockType.Active)
        {
            ThrowArgumentException($"{nameof(blockDef.BlockType)} must be {nameof(ScriptBlockType)}.{nameof(ScriptBlockType.Active)}", nameof(blockDef));
        }

        var block = _codeBuilder.Place(blockDef);

        if (expressions.Length > 0)
        {
            int exprIndex = 0;
            CodeGraph.Builder.ExpressionScopeDisposable? exprDisposable = null;
            foreach (var terminal in block.Type.Terminals)
            {
                if (terminal is not { Type: TerminalType.In, SignalType: not SignalType.Error and not SignalType.Void })
                {
                    continue;
                }

                exprDisposable ??= ExpressionScope();

                _codeBuilder.Connect(expressions[exprIndex].WriteTo(this), new Terminal(block, terminal));

                exprIndex++;

                if (exprIndex >= expressions.Length)
                {
                    break;
                }
            }

            exprDisposable?.Dispose();
        }

        /*int settingIndex = 0;
        foreach (object setting in settings)
        {
            _codeBuilder.SetSetting(block, settingIndex++, setting);
        }*/

        ConnectorAddInternal(new TerminalStore(block));

        if (voidTerminalCallback is not null)
        {
            foreach (var terminal in block.Type.Terminals)
            {
                if (terminal is not { Type: TerminalType.Out, SignalType: SignalType.Void })
                {
                    continue;
                }

                _connector.SetLast(TerminalStore.CreateOut(new Terminal(block, terminal)));
                using (StatementScope())
                {
                    voidTerminalCallback(this, terminal, block);
                }
            }

            _connector.SetLast(new TerminalStore(block));
        }

        return blockDef.Terminals
            .Where(terminal => terminal is { SignalType: not SignalType.Error and not SignalType.Void, Type: TerminalType.In })
            .Select(terminal => new Terminal(block, terminal));
    }

    #region Statements

    /// <summary>
    /// Writes the <see cref="StockBlocks.Game.Win"/> block.
    /// </summary>
    /// <param name="delay">Time to win (in frames).</param>
    public void Win(int delay)
    {
        ThrowIfGreaterThan(delay, 120);
        ThrowIfLessThan(delay, 0);

        var block = _codeBuilder.Place(StockBlocks.Game.Win);

        _codeBuilder.SetSetting(block, new(0, (byte)delay));

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Game.Lose"/> block.
    /// </summary>
    /// <param name="delay">Time to lose (in frames).</param>
    public void Lose(int delay)
    {
        ThrowIfGreaterThan(delay, 120);
        ThrowIfLessThan(delay, 0);

        var block = _codeBuilder.Place(StockBlocks.Game.Lose);

        _codeBuilder.SetSetting(block, new(0, (byte)delay));

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Game.SetScore"/> block.
    /// </summary>
    /// <param name="ranking">Determines how players are ranked.</param>
    /// <param name="score">The new score. If <paramref name="ranking"/> is <see cref="Ranking.FastestTime"/> or <see cref="Ranking.LongestTime"/>, time is specified in frames (60 - 1s).</param>
    /// <param name="coins">The new amount of coins.</param>
    public void SetScore(Ranking ranking, IExpression score, IExpression coins)
    {
        var block = _codeBuilder.Place(StockBlocks.Game.SetScore);

        _codeBuilder.SetSetting(block, new(0, (byte)ranking));

        using (ExpressionScope())
        {
            _codeBuilder.Connect(score.WriteTo(this), new Terminal(block, "Score"));
            _codeBuilder.Connect(coins.WriteTo(this), new Terminal(block, "Coins"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Game.SetCamera"/> block.
    /// </summary>
    /// <param name="perpective">If <see langword="true"/>, the camera will be in perspective mode; otherwise, it will be in orthographic mode.</param>
    /// <param name="position">The new position of the camera.</param>
    /// <param name="rotation">The new rotation of the camera.</param>
    /// <param name="range">
    /// <list type="bullet">
    ///     <item>If in orthographic (isometric) mode, determines how wide the view frustum is.</item>
    ///     <item>If in perspective mode specifies half of the field of view.</item>
    /// </list>
    /// </param>
    public void SetCamera(bool perpective, IExpression position, IExpression rotation, IExpression range)
    {
        var block = _codeBuilder.Place(StockBlocks.Game.SetCamera);

        _codeBuilder.SetSetting(block, new(0, (byte)(perpective ? 1 : 0)));

        using (ExpressionScope())
        {
            _codeBuilder.Connect(position.WriteTo(this), new Terminal(block, "Position"));
            _codeBuilder.Connect(rotation.WriteTo(this), new Terminal(block, "Rotation"));
            _codeBuilder.Connect(range.WriteTo(this), new Terminal(block, "Range"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Game.SetLight"/> block.
    /// </summary>
    /// <param name="position">Position of the light, <strong>currently unused</strong>.</param>
    /// <param name="rotation">Direction of the light.</param>
    public void SetLight(IExpression position, IExpression rotation)
    {
        var block = _codeBuilder.Place(StockBlocks.Game.SetLight);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(position.WriteTo(this), new Terminal(block, "Position"));
            _codeBuilder.Connect(rotation.WriteTo(this), new Terminal(block, "Rotation"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Game.MenuItem"/> block.
    /// </summary>
    /// <param name="name">Name of the item.</param>
    /// <param name="maxBuyCount">The maximum number of times the item can be bought.</param>
    /// <param name="priceIncrease">Specifies what the initial price is and how it increases.</param>
    /// <param name="variable">
    /// The variable to store the amount of times bought in, should have the saved modifier.
    /// <para>If <see cref="Expressions.None"/> is connected, shows as a title on a new shop page.</para>
    /// </param>
    /// <param name="picture">Determines object to display for the item.</param>
    public void MenuItem(string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease, IExpression variable, IExpression picture)
    {
        ThrowIfNull(name);

        var block = _codeBuilder.Place(StockBlocks.Game.MenuItem);

        _codeBuilder.SetSetting(block, new(0, SettingType.String, name));
        _codeBuilder.SetSetting(block, new(1, (byte)maxBuyCount));
        _codeBuilder.SetSetting(block, new(2, (byte)priceIncrease));

        using (ExpressionScope())
        {
            _codeBuilder.Connect(variable.WriteTo(this), new Terminal(block, "Variable"));
            _codeBuilder.Connect(picture.WriteTo(this), new Terminal(block, "Picture"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Objects.SetPos"/> block.
    /// </summary>
    /// <param name="object">The object whose position and rotation is to be set.</param>
    /// <param name="position">The new position.</param>
    /// <param name="rotation">The new rotation.</param>
    public void SetPosition(IExpression @object, IExpression position, IExpression rotation)
    {
        var block = _codeBuilder.Place(StockBlocks.Objects.SetPos);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(@object.WriteTo(this), new Terminal(block, "Object"));
            _codeBuilder.Connect(position.WriteTo(this), new Terminal(block, "Position"));
            _codeBuilder.Connect(rotation.WriteTo(this), new Terminal(block, "Rotation"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Objects.SetPos"/> block.
    /// </summary>
    /// <param name="object">The object whose visibility is to be set.</param>
    /// <param name="visible">The new visibility of the object.</param>
    public void SetVisible(IExpression @object, IExpression visible)
    {
        var block = _codeBuilder.Place(StockBlocks.Objects.SetVisible);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(@object.WriteTo(this), new Terminal(block, "Object"));
            _codeBuilder.Connect(visible.WriteTo(this), new Terminal(block, "Visible"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Objects.CreateObject"/> block.
    /// </summary>
    /// <param name="object">The object to clone.</param>
    /// <returns>A copy of <paramref name="object"/>.</returns>
    public Terminal CreateObject(IExpression @object)
    {
        var block = _codeBuilder.Place(StockBlocks.Objects.CreateObject);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(@object.WriteTo(this), new Terminal(block, "Object"));
        }

        ConnectorAddInternal(new TerminalStore(block));

        return new Terminal(block, "Copy");
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Objects.DestroyObject"/> block.
    /// </summary>
    /// <param name="object">The object to be destroyed.</param>
    public void DestroyObject(IExpression @object)
    {
        var block = _codeBuilder.Place(StockBlocks.Objects.DestroyObject);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(@object.WriteTo(this), new Terminal(block, "Object"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Sound.PlaySound"/> block.
    /// </summary>
    /// <param name="sound">The sound to play.</param>
    /// <param name="volume">Volume of the sound.</param>
    /// <param name="pitch">Pitch of the sound.</param>
    /// <returns>The channel on which the sound is playing.</returns>
    public Terminal PlaySound(FcSound sound, IExpression volume, IExpression pitch)
    {
        var block = _codeBuilder.Place(StockBlocks.Sound.PlaySound);

        _codeBuilder.SetSetting(block, new(0, (byte)sound));

        using (ExpressionScope())
        {
            _codeBuilder.Connect(volume.WriteTo(this), new Terminal(block, "Volume"));
            _codeBuilder.Connect(pitch.WriteTo(this), new Terminal(block, "Pitch"));
        }

        ConnectorAddInternal(new TerminalStore(block));

        return new Terminal(block, "Channel");
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Sound.VolumePitch"/> block.
    /// </summary>
    /// <param name="channel">The channel whose sound should be adjusted.</param>
    /// <param name="volume">The channel's new volume.</param>
    /// <param name="pitch">The channel's new pitch.</param>
    public void VolumePitch(IExpression channel, IExpression volume, IExpression pitch)
    {
        var block = _codeBuilder.Place(StockBlocks.Sound.VolumePitch);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(channel.WriteTo(this), new Terminal(block, "Channel"));
            _codeBuilder.Connect(volume.WriteTo(this), new Terminal(block, "Volume"));
            _codeBuilder.Connect(pitch.WriteTo(this), new Terminal(block, "Pitch"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Sound.StopSound"/> block.
    /// </summary>
    /// <param name="channel">The channel whose sound should be stopped.</param>
    public void StopSound(IExpression channel)
    {
        var block = _codeBuilder.Place(StockBlocks.Sound.StopSound);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(channel.WriteTo(this), new Terminal(block, "Channel"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.AddForce"/> block.
    /// </summary>
    /// <param name="object">The object to which the force should be applied to.</param>
    /// <param name="force">The force to apply to <paramref name="object"/>.</param>
    /// <param name="applyAt">Where on <paramref name="object"/> should <paramref name="force"/> be applied at (center of mass by default).</param>
    /// <param name="torque">The rotational force to apply to <paramref name="object"/>.</param>
    public void AddForce(IExpression @object, IExpression force, IExpression applyAt, IExpression torque)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.AddForce);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(@object.WriteTo(this), new Terminal(block, "Object"));
            _codeBuilder.Connect(force.WriteTo(this), new Terminal(block, "Force"));
            _codeBuilder.Connect(applyAt.WriteTo(this), new Terminal(block, "Apply at"));
            _codeBuilder.Connect(torque.WriteTo(this), new Terminal(block, "Torque"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.SetVelocity"/> block.
    /// </summary>
    /// <param name="object">The object whose velocity is to be set.</param>
    /// <param name="velocity">The new velocity of <paramref name="object"/>.</param>
    /// <param name="spin">The new rotational velocity of <paramref name="object"/>.</param>
    public void SetVelocity(IExpression @object, IExpression velocity, IExpression spin)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.SetVelocity);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(@object.WriteTo(this), new Terminal(block, "Object"));
            _codeBuilder.Connect(velocity.WriteTo(this), new Terminal(block, "Velocity"));
            _codeBuilder.Connect(spin.WriteTo(this), new Terminal(block, "Spin"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.SetLocked"/> block.
    /// </summary>
    /// <param name="object">The object whose movement is to be restricted.</param>
    /// <param name="position">The movement multiplier.</param>
    /// <param name="rotation">The rotation multiplier.</param>
    public void SetLocked(IExpression @object, IExpression position, IExpression rotation)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.SetLocked);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(@object.WriteTo(this), new Terminal(block, "Object"));
            _codeBuilder.Connect(position.WriteTo(this), new Terminal(block, "Position"));
            _codeBuilder.Connect(rotation.WriteTo(this), new Terminal(block, "Rotation"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.SetMass"/> block.
    /// </summary>
    /// <param name="object">The object whose mass is to be set.</param>
    /// <param name="mass">The new mass of the object.</param>
    public void SetMass(IExpression @object, IExpression mass)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.SetMass);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(@object.WriteTo(this), new Terminal(block, "Object"));
            _codeBuilder.Connect(mass.WriteTo(this), new Terminal(block, "Mass"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.SetFriction"/> block.
    /// </summary>
    /// <param name="object">The object whose friction is to be set.</param>
    /// <param name="friction">The new friction of the object.</param>
    public void SetFriction(IExpression @object, IExpression friction)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.SetFriction);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(@object.WriteTo(this), new Terminal(block, "Object"));
            _codeBuilder.Connect(friction.WriteTo(this), new Terminal(block, "Friction"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.SetBounciness"/> block.
    /// </summary>
    /// <param name="object">The object whose bounciness is to be set.</param>
    /// <param name="bounciness">The new bounciness of the object.</param>
    public void SetBounciness(IExpression @object, IExpression bounciness)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.SetBounciness);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(@object.WriteTo(this), new Terminal(block, "Object"));
            _codeBuilder.Connect(bounciness.WriteTo(this), new Terminal(block, "Bounciness"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.SetGravity"/> block.
    /// </summary>
    /// <param name="gravity">The new gravity.</param>
    public void SetGravity(IExpression gravity)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.SetGravity);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(gravity.WriteTo(this), new Terminal(block, "Gravity"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.AddConstraint"/> block.
    /// </summary>
    /// <param name="base">The base of the constraint.</param>
    /// <param name="part">The part of the constraint.</param>
    /// <param name="pivot">The pivot of the constraint.</param>
    /// <returns>The constraint.</returns>
    public Terminal AddConstraint(IExpression @base, IExpression part, IExpression pivot)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.AddConstraint);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(@base.WriteTo(this), new Terminal(block, "Base"));
            _codeBuilder.Connect(part.WriteTo(this), new Terminal(block, "Part"));
            _codeBuilder.Connect(pivot.WriteTo(this), new Terminal(block, "Pivot"));
        }

        ConnectorAddInternal(new TerminalStore(block));

        return new Terminal(block, "Constraint");
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.LinearLimits"/> block.
    /// </summary>
    /// <param name="constraint">The constraint whose linear limits should be set.</param>
    /// <param name="lower">The lower limit.</param>
    /// <param name="upper">The upper limit.</param>
    public void LinearLimits(IExpression constraint, IExpression lower, IExpression upper)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.LinearLimits);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(constraint.WriteTo(this), new Terminal(block, "Constraint"));
            _codeBuilder.Connect(lower.WriteTo(this), new Terminal(block, "Lower"));
            _codeBuilder.Connect(upper.WriteTo(this), new Terminal(block, "Upper"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.AngularLimits"/> block.
    /// </summary>
    /// <param name="constraint">The constraint whose angular limits should be set.</param>
    /// <param name="lower">The lower limit.</param>
    /// <param name="upper">The upper limit.</param>
    public void AngularLimits(IExpression constraint, IExpression lower, IExpression upper)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.AngularLimits);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(constraint.WriteTo(this), new Terminal(block, "Constraint"));
            _codeBuilder.Connect(lower.WriteTo(this), new Terminal(block, "Lower"));
            _codeBuilder.Connect(upper.WriteTo(this), new Terminal(block, "Upper"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.LinearSpring"/> block.
    /// </summary>
    /// <param name="constraint">The constraint whose linear spring should be set.</param>
    /// <param name="stiffness">The spring's stiffness.</param>
    /// <param name="damping">The spring's damping.</param>
    public void LinearSpring(IExpression constraint, IExpression stiffness, IExpression damping)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.LinearSpring);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(constraint.WriteTo(this), new Terminal(block, "Constraint"));
            _codeBuilder.Connect(stiffness.WriteTo(this), new Terminal(block, "Stiffness"));
            _codeBuilder.Connect(damping.WriteTo(this), new Terminal(block, "Damping"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.AngularSpring"/> block.
    /// </summary>
    /// <param name="constraint">The constraint whose angular spring should be set.</param>
    /// <param name="stiffness">The spring's stiffness.</param>
    /// <param name="damping">The spring's damping.</param>
    public void AngularSpring(IExpression constraint, IExpression stiffness, IExpression damping)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.AngularSpring);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(constraint.WriteTo(this), new Terminal(block, "Constraint"));
            _codeBuilder.Connect(stiffness.WriteTo(this), new Terminal(block, "Stiffness"));
            _codeBuilder.Connect(damping.WriteTo(this), new Terminal(block, "Damping"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.LinearMotor"/> block.
    /// </summary>
    /// <param name="constraint">The constraint whose linear motor should be set.</param>
    /// <param name="speed">The motor's speed.</param>
    /// <param name="force">The motor's force.</param>
    public void LinearMotor(IExpression constraint, IExpression speed, IExpression force)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.LinearMotor);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(constraint.WriteTo(this), new Terminal(block, "Constraint"));
            _codeBuilder.Connect(speed.WriteTo(this), new Terminal(block, "Speed"));
            _codeBuilder.Connect(force.WriteTo(this), new Terminal(block, "Force"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.AngularMotor"/> block.
    /// </summary>
    /// <param name="constraint">The constraint whose angular motor should be set.</param>
    /// <param name="speed">The motor's speed.</param>
    /// <param name="force">The motor's force.</param>
    public void AngularMotor(IExpression constraint, IExpression speed, IExpression force)
    {
        var block = _codeBuilder.Place(StockBlocks.Physics.AngularMotor);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(constraint.WriteTo(this), new Terminal(block, "Constraint"));
            _codeBuilder.Connect(speed.WriteTo(this), new Terminal(block, "Speed"));
            _codeBuilder.Connect(force.WriteTo(this), new Terminal(block, "Force"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.If"/> block.
    /// </summary>
    /// <param name="condition">Condition of the if.</param>
    /// <param name="true">Writes what should be executed when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <param name="false">Writes what should be executed when <paramref name="condition"/> is <see langword="false"/>.</param>
    public void If(IExpression condition, Action<CodeWriter>? @true, Action<CodeWriter>? @false)
    {
        var block = _codeBuilder.Place(StockBlocks.Control.If);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(condition.WriteTo(this), new Terminal(block, "Condition"));
        }

        ConnectorAddInternal(new TerminalStore(block));

        ConnectOutInternal(new Terminal(block, "True"), @true);
        ConnectOutInternal(new Terminal(block, "False"), @false);

        _connector.SetLast(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.PlaySensor"/> block.
    /// </summary>
    /// <param name="onPlay">Writes what should be executed only on the first frame.</param>
    public void PlaySensor(Action<CodeWriter>? onPlay)
    {
        var block = _codeBuilder.Place(StockBlocks.Control.PlaySensor);

        ConnectorAddInternal(new TerminalStore(block));

        ConnectOutInternal(new Terminal(block, "On Play"), onPlay);

        _connector.SetLast(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.LateUpdate"/> block.
    /// </summary>
    /// <param name="afterPhysics">Writes what should be executed only after physics but before rendering.</param>
    public void LateUpdate(Action<CodeWriter>? afterPhysics)
    {
        var block = _codeBuilder.Place(StockBlocks.Control.LateUpdate);

        ConnectorAddInternal(new TerminalStore(block));

        ConnectOutInternal(new Terminal(block, "After Physics"), afterPhysics);

        _connector.SetLast(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.BoxArtSensor"/> block.
    /// </summary>
    /// <param name="onScreenshot">Writes what should be executed only when taking boxart.</param>
    public void BoxArtSensor(Action<CodeWriter>? onScreenshot)
    {
        var block = _codeBuilder.Place(StockBlocks.Control.BoxArtSensor);

        ConnectorAddInternal(new TerminalStore(block));

        ConnectOutInternal(new Terminal(block, "On Screenshot"), onScreenshot);

        _connector.SetLast(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.TouchSensor"/> block.
    /// </summary>
    /// <param name="touchState">The <see cref="TouchState"/> to detect.</param>
    /// <param name="touchFinger">Index of the finger to detect, 0 - 2.</param>
    /// <param name="touched">Writes what should be executed when touch is detected.</param>
    /// <returns>The x and y position of the touch.</returns>
    public (Terminal ScreenX, Terminal ScreenY) TouchSensor(TouchState touchState, int touchFinger, Action<CodeWriter, Terminal, Terminal>? touched)
    {
        if (touchFinger < 0 || touchFinger > FancadeConstants.TouchSensorMaxFingerIndex)
        {
            ThrowArgumentOutOfRangeException(nameof(touchFinger));
        }

        var block = _codeBuilder.Place(StockBlocks.Control.TouchSensor);

        _codeBuilder.SetSetting(block, new(0, (byte)touchState));
        _codeBuilder.SetSetting(block, new(1, (byte)touchFinger));

        ConnectorAddInternal(new TerminalStore(block));

        ConnectOutInternal(new Terminal(block, "Touched"), touched, new Terminal(block, "Screen X"), new Terminal(block, "Screen Y"));

        _connector.SetLast(new TerminalStore(block));

        return (new Terminal(block, "Screen X"), new Terminal(block, "Screen Y"));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.SwipeSensor"/> block.
    /// </summary>
    /// <param name="swiped">Writes what should be executed when swipe is detected.</param>
    /// <returns>Direction of the swipe.</returns>
    public Terminal SwipeSensor(Action<CodeWriter, Terminal>? swiped)
    {
        var block = _codeBuilder.Place(StockBlocks.Control.SwipeSensor);

        ConnectorAddInternal(new TerminalStore(block));

        ConnectOutInternal(new Terminal(block, "Swiped"), swiped, new Terminal(block, "Direction"));

        _connector.SetLast(new TerminalStore(block));

        return new Terminal(block, "Direction");
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.Button"/> block.
    /// </summary>
    /// <param name="buttonType">Type of the button.</param>
    /// <param name="button">Writes what should be executed when the button is pressed.</param>
    public void Button(ButtonType buttonType, Action<CodeWriter>? button)
    {
        var block = _codeBuilder.Place(StockBlocks.Control.Button);

        _codeBuilder.SetSetting(block,new( 0, (byte)buttonType));

        ConnectorAddInternal(new TerminalStore(block));

        ConnectOutInternal(new Terminal(block, "Button"), button);

        _connector.SetLast(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.Collision"/> block.
    /// </summary>
    /// <param name="firstObject">The object whose collisions should be detected.</param>
    /// <param name="collided">Writes what should be executed when <paramref name="firstObject"/> collides with another object.</param>
    /// <returns>The object <paramref name="firstObject"/> collided with, impulse of the collision and the normal of the collision.</returns>
    public (Terminal SecondObjectTerminal, Terminal ImpulseTerminal, Terminal NormalTerminal) Collision(IExpression firstObject, Action<CodeWriter, Terminal, Terminal, Terminal>? collided)
    {
        var block = _codeBuilder.Place(StockBlocks.Control.Collision);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(firstObject.WriteTo(this), new Terminal(block, "1st Object"));
        }

        ConnectorAddInternal(new TerminalStore(block));

        ConnectOutInternal(new Terminal(block, "Collided"), collided, new Terminal(block, "2nd Object"), new Terminal(block, "Impulse"), new Terminal(block, "Normal"));

        _connector.SetLast(new TerminalStore(block));

        return (new Terminal(block, "2nd Object"), new Terminal(block, "Impulse"), new Terminal(block, "Normal"));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.Loop"/> block.
    /// </summary>
    /// <param name="start">The start value (inclusive).</param>
    /// <param name="stop">The end value (exclusive).</param>
    /// <param name="do">Writes what should be executed in the loop.</param>
    /// <returns>The current value of the loop.</returns>
    public Terminal Loop(IExpression start, IExpression stop, Action<CodeWriter, Terminal>? @do)
    {
        var block = _codeBuilder.Place(StockBlocks.Control.Loop);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(start.WriteTo(this), new Terminal(block, "Start"));
            _codeBuilder.Connect(stop.WriteTo(this), new Terminal(block, "Stop"));
        }

        ConnectorAddInternal(new TerminalStore(block));

        ConnectOutInternal(new Terminal(block, "Do"), @do, new Terminal(block, "Counter"));

        _connector.SetLast(new TerminalStore(block));

        return new Terminal(block, "Counter");
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Math.RandomSeed"/> block.
    /// </summary>
    /// <param name="seed">The new random seed.</param>
    public void RandomSeed(IExpression seed)
    {
        var block = _codeBuilder.Place(StockBlocks.Math.RandomSeed);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(seed.WriteTo(this), new Terminal(block, "Seed"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes a comment.
    /// </summary>
    /// <remarks>
    /// If <paramref name="text"/> is too long, multiple comment blocks are placed.
    /// </remarks>
    /// <param name="text">Text of the comment.</param>
    public void Comment(string text)
    {
        var span = text.AsSpan();

        foreach (var lineRange in StringUtils.SplitByMaxLength(span, FancadeConstants.MaxCommentLength))
        {
            var block = _codeBuilder.Place(StockBlocks.Values.Comment);
            _codeBuilder.SetSetting(block, new(0, SettingType.String, new string(span[lineRange])));
        }
    }

    /// <summary>
    /// Writes the inspect block.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    public void Inspect(IExpression value)
        => Inspect(value, value.Type);

    /// <summary>
    /// Writes the inspect block of a specified type.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <param name="type">Type of the inspect block.</param>
    public void Inspect(IExpression value, SignalType type)
    {
        var block = _codeBuilder.Place(StockBlocks.Values.InspectByType(type));

        using (ExpressionScope())
        {
            _codeBuilder.Connect(value.WriteTo(this), new Terminal(block, 1));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the set variable block.
    /// </summary>
    /// <param name="variable">The variable whose value should be set.</param>
    /// <param name="value">The new value of the variable.</param>
    public void SetVariable(Variable variable, IExpression value)
    {
        var block = _codeBuilder.Place(StockBlocks.Variables.SetVariableByType(variable.Type));

        _codeBuilder.SetSetting(block, new(0, SettingType.String, variable.Name));

        using (ExpressionScope())
        {
            _codeBuilder.Connect(value.WriteTo(this), new Terminal(block, "Value"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the set variable block.
    /// </summary>
    /// <param name="variable">The variable whose value should be set.</param>
    /// <param name="value">The new value of the variable.</param>
    public void SetVariable(IExpression variable, IExpression value)
        => SetVariable(variable, value, variable.Type);

    /// <summary>
    /// Writes the set variable block.
    /// </summary>
    /// <param name="variable">The variable whose value should be set.</param>
    /// <param name="value">The new value of the variable.</param>
    /// <param name="variableType">Type of the variable.</param>
    public void SetVariable(IExpression variable, IExpression value, SignalType variableType)
    {
        var block = _codeBuilder.Place(StockBlocks.Variables.SetPtrByType(variableType));

        using (ExpressionScope())
        {
            _codeBuilder.Connect(variable.WriteTo(this), new Terminal(block, "Variable"));
            _codeBuilder.Connect(value.WriteTo(this), new Terminal(block, "Value"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Sets a range of a list.
    /// </summary>
    /// <typeparam name="T">Type of the values.</typeparam>
    /// <param name="variable">The variable that should be assigned.</param>
    /// <param name="values">The values to set to <paramref name="variable"/>.</param>
    /// <param name="startIndex">The index at which to start assigning to <paramref name="variable"/>.</param>
    public void SetListRange<T>(Variable variable, ReadOnlySpan<T> values, IExpression startIndex)
        where T : notnull
    {
        var signalType = SignalTypeUtils.FromType(typeof(T));

        Terminal? lastElementTerminal = null;

        var variableEx = Expressions.Variable(variable);

        for (int i = 0; i < values.Length; i++)
        {
            if (i == 0 && IsLiteralOfValue(startIndex, 0f))
            {
                SetVariable(variable, Expressions.Literal(values[i]));
            }
            else
            {
                var setBlock = _codeBuilder.Place(StockBlocks.Variables.SetPtrByType(signalType));

                ConnectorAddInternal(new TerminalStore(setBlock));

                using (ExpressionScope())
                {
                    var listBlock = _codeBuilder.Place(StockBlocks.Variables.ListByType(signalType));

                    _codeBuilder.Connect(TerminalStore.CreateOut(new Terminal(listBlock, "Element")), TerminalStore.CreateIn(new(setBlock, "Variable")));

                    using (ExpressionScope())
                    {
                        lastElementTerminal ??= variableEx.WriteTo(this);

                        _codeBuilder.Connect(lastElementTerminal.Value, TerminalStore.CreateIn(new(listBlock, "Variable")));

                        lastElementTerminal = new Terminal(listBlock, "Element");

                        _codeBuilder.Connect(i == 0 ? startIndex.WriteTo(this) : Expressions.Number(1f).WriteTo(this), TerminalStore.CreateIn(new(listBlock, "Index")));
                    }

                    _codeBuilder.Connect(Expressions.Literal(values[i]).WriteTo(this), TerminalStore.CreateIn(new(setBlock, "Value")));
                }
            }
        }
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Variables.IncrementNumber"/> block.
    /// </summary>
    /// <param name="variable">The variable that should be incremented.</param>
    public void IncrementNumber(IExpression variable)
    {
        var block = _codeBuilder.Place(StockBlocks.Variables.IncrementNumber);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(variable.WriteTo(this), new Terminal(block, "Variable"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Variables.DecrementNumber"/> block.
    /// </summary>
    /// <param name="variable">The variable that should be decremented.</param>
    public void DecrementNumber(IExpression variable)
    {
        var block = _codeBuilder.Place(StockBlocks.Variables.DecrementNumber);

        using (ExpressionScope())
        {
            _codeBuilder.Connect(variable.WriteTo(this), new Terminal(block, "Variable"));
        }

        ConnectorAddInternal(new TerminalStore(block));
    }
    #endregion

    /// <summary>
    /// Flushes the <see cref="CodeWriter"/>.
    /// </summary>
    /// <remarks>
    /// Processes gotos.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">Thrown when a goto targets a label that was not defined.</exception>
    /// <exception cref="GotoRecursionException">Thrown when a recursive goto is encountered.</exception>
    public void Flush()
    {
        // TODO: for stuff like if-true/false, return like a new scope or something, so that a label at the end of it does not get connected to something else, but ends up as null
        HashSet<string> encounteredLabels = [];
        foreach (var item in _gotos)
        {
            encounteredLabels.Clear();

            var (store, gotoLabel) = item;

            while (encounteredLabels.Add(gotoLabel))
            {
                if (!_labels.TryGetValue(gotoLabel, out object? labelTarget))
                {
                    throw new KeyNotFoundException($"A goto targets laabel '{gotoLabel}', but it was not defined.");
                }

                switch (labelTarget)
                {
                    case Terminal terminal:
                        _codeBuilder.Connect(store, terminal);
                        goto nextGoto;
                    case string label:
                        gotoLabel = label;
                        break;
                    case null:
                        goto nextGoto; // a label was defined, but nothing was placed after it
                    default:
                        Debug.Fail($"Expected labelTarget to be {nameof(Terminal)} or string, but it was: {labelTarget.GetType().FullName}");
                        goto nextGoto;
                }
            }

            throw new GotoRecursionException(item.LabelName, [.. encounteredLabels]);

        nextGoto:;
        }

        _gotos.Clear();
    }

    /// <inheritdoc/>
    public void Dispose()
        => Flush();

    #region Blocks

    /// <summary>
    /// Enters a statement block.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/>, that when disposed exits the statement block.</returns>
    public CodeGraph.Builder.StatementScopeDisposable StatementScope()
        => _codeBuilder.StatementScope();

    /// <summary>
    /// Enters an expression block.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/>, that when disposed exits the expression block.</returns>
    public CodeGraph.Builder.ExpressionScopeDisposable ExpressionScope()
        => _codeBuilder.ExpressionScope();

    #endregion

#pragma warning disable SA1629 // Documentation text should end with a period
    /// <summary>
    /// Sets the block as the last execution block and points all queue labels to it.
    /// </summary>
    /// <remarks>
    /// Should only be used when adding custom block extension methods.
    /// </remarks>
    /// <example>
    /// // place a "win" block
    /// var block = writer.Builder.Place(StockBlocks.Game.Win);
    /// 
    /// writer.Builder.SetSetting(block, new(0, (byte)delay));
    /// 
    /// writer.ConnectorAddInternal(new TerminalStore(block));
    /// </example>
    /// <param name="store">The last placed block.</param>
#pragma warning restore SA1629 // Documentation text should end with a period
    public void ConnectorAddInternal(TerminalStore store)
    {
        _connector.Add(store);

        while (_labelsToProcess.TryDequeue(out string? labelName))
        {
            _labels[labelName] = store.In;
        }
    }

#pragma warning disable SA1629 // Documentation text should end with a period
    /// <summary>
    /// Sets the last execution terminal and calls <paramref name="writeFunc"/> in a statement block, if <paramref name="writeFunc"/> is not <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Usefull when a block has multiple out execution terminals.
    /// Should only be used when adding custom block extension methods.
    /// </remarks>
    /// <example>
    /// // place an "on play" block
    /// Block block = writer.Builder.Place(StockBlocks.Control.PlaySensor);
    /// 
    /// writer.ConnectorAddInternal(new TerminalStore(block));
    /// 
    /// writer.ConnectOutInternal(new Terminal(block, "On Play"), onPlay);
    /// 
    /// writer.Connector.SetLast(new TerminalStore(block));
    /// </example>
    /// <param name="terminal">The last execution terminal.</param>
    /// <param name="writeFunc">The callback function.</param>
#pragma warning restore SA1629 // Documentation text should end with a period
    public void ConnectOutInternal(Terminal terminal, Action<CodeWriter>? writeFunc)
    {
        if (writeFunc is null)
        {
            return;
        }

        _connector.SetLast(TerminalStore.CreateOut(terminal));
        using (StatementScope())
        {
            writeFunc(this);
        }
    }

#pragma warning disable SA1629 // Documentation text should end with a period
    /// <summary>
    /// Sets the last execution terminal and calls <paramref name="writeFunc"/> in a statement block, if <paramref name="writeFunc"/> is not <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Usefull when a block has multiple out execution terminals.
    /// Should only be used when adding custom block extension methods.
    /// </remarks>
    /// <example>
    /// // place an "on play" block
    /// Block block = writer.Builder.Place(StockBlocks.Control.PlaySensor);
    /// 
    /// writer.ConnectorAddInternal(new TerminalStore(block));
    /// 
    /// writer.ConnectOutInternal(new Terminal(block, "On Play"), onPlay);
    /// 
    /// writer.Connector.SetLast(new TerminalStore(block));
    /// </example>
    /// <param name="terminal">The last execution terminal.</param>
    /// <param name="writeFunc">The callback function.</param>
    /// <param name="arg1">The first argument to the function.</param>
#pragma warning restore SA1629 // Documentation text should end with a period
    public void ConnectOutInternal(Terminal terminal, Action<CodeWriter, Terminal>? writeFunc, Terminal arg1)
    {
        if (writeFunc is null)
        {
            return;
        }

        _connector.SetLast(TerminalStore.CreateOut(terminal));
        using (StatementScope())
        {
            writeFunc(this, arg1);
        }
    }

#pragma warning disable SA1629 // Documentation text should end with a period
    /// <summary>
    /// Sets the last execution terminal and calls <paramref name="writeFunc"/> in a statement block, if <paramref name="writeFunc"/> is not <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Usefull when a block has multiple out execution terminals.
    /// Should only be used when adding custom block extension methods.
    /// </remarks>
    /// <example>
    /// // place an "on play" block
    /// Block block = writer.Builder.Place(StockBlocks.Control.PlaySensor);
    /// 
    /// writer.ConnectorAddInternal(new TerminalStore(block));
    /// 
    /// writer.ConnectOutInternal(new Terminal(block, "On Play"), onPlay);
    /// 
    /// writer.Connector.SetLast(new TerminalStore(block));
    /// </example>
    /// <param name="terminal">The last execution terminal.</param>
    /// <param name="writeFunc">The callback function.</param>
    /// <param name="arg1">The first argument to the function.</param>
    /// <param name="arg2">The second argument to the function.</param>
#pragma warning restore SA1629 // Documentation text should end with a period
    public void ConnectOutInternal(Terminal terminal, Action<CodeWriter, Terminal, Terminal>? writeFunc, Terminal arg1, Terminal arg2)
    {
        if (writeFunc is null)
        {
            return;
        }

        _connector.SetLast(TerminalStore.CreateOut(terminal));
        using (StatementScope())
        {
            writeFunc(this, arg1, arg2);
        }
    }

#pragma warning disable SA1629 // Documentation text should end with a period
    /// <summary>
    /// Sets the last execution terminal and calls <paramref name="writeFunc"/> in a statement block, if <paramref name="writeFunc"/> is not <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Usefull when a block has multiple out execution terminals.
    /// Should only be used when adding custom block extension methods.
    /// </remarks>
    /// <example>
    /// // place an "on play" block
    /// Block block = writer.Builder.Place(StockBlocks.Control.PlaySensor);
    /// 
    /// writer.ConnectorAddInternal(new TerminalStore(block));
    /// 
    /// writer.ConnectOutInternal(new Terminal(block, "On Play"), onPlay);
    /// 
    /// writer.Connector.SetLast(new TerminalStore(block));
    /// </example>
    /// <param name="terminal">The last execution terminal.</param>
    /// <param name="writeFunc">The callback function.</param>
    /// <param name="arg1">The first argument to the function.</param>
    /// <param name="arg2">The second argument to the function.</param>
    /// <param name="arg3">The third argument to the function.</param>
#pragma warning restore SA1629 // Documentation text should end with a period
    public void ConnectOutInternal(Terminal terminal, Action<CodeWriter, Terminal, Terminal, Terminal>? writeFunc, Terminal arg1, Terminal arg2, Terminal arg3)
    {
        if (writeFunc is null)
        {
            return;
        }

        _connector.SetLast(TerminalStore.CreateOut(terminal));
        using (StatementScope())
        {
            writeFunc(this, arg1, arg2, arg3);
        }
    }

    private static bool IsLiteralOfValue(IExpression expression, float value)
        => expression is Expressions.LiteralExpression literal && literal.Type == SignalType.Float && (float)literal._value == value;
}
