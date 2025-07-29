// <copyright file="CodeWriter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Scripting.Exceptions;
using BitcoderCZ.Fancade.Editing.Scripting.Placers;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Fancade.Editing.Scripting.Terminals;
using BitcoderCZ.Fancade.Editing.Scripting.TerminalStores;
using BitcoderCZ.Fancade.Editing.Scripting.Utils;
using BitcoderCZ.Fancade.Editing.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Editing.Scripting;

/// <summary>
/// A helper class for writing fancade code.
/// </summary>
public sealed partial class CodeWriter
{
    private readonly IScopedCodePlacer _codePlacer;

    private readonly TerminalConnector _connector;

    private readonly Dictionary<string, object?> _labels = [];
    private readonly List<(ITerminalStore Store, string LabelName)> _gotos = [];
    private readonly Queue<string> _labelsToProcess = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeWriter"/> class.
    /// </summary>
    /// <param name="codePlacer">The <see cref="ICodePlacer"/> used to place blocks.</param>
    /// <param name="connector">The <see cref="TerminalConnector"/> to use to connect statements.</param>
    public CodeWriter(ICodePlacer codePlacer, TerminalConnector connector)
    {
        _codePlacer = codePlacer is IScopedCodePlacer scoped ? scoped : new ScopedCodePlacerWrapper(codePlacer);
        _connector = connector;
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
        /// <returns>An <see cref="ITerminal"/> representing the written output of the expression.</returns>
        ITerminal WriteTo(CodeWriter writer);
    }

    /// <summary>
    /// Gets the underlying <see cref="ICodePlacer"/>.
    /// </summary>
    /// <value>The underlying <see cref="ICodePlacer"/>.</value>
    public ICodePlacer Placer => _codePlacer;

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
        if (!_labels.ContainsKey(name))
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

        _connector.SetLast(NopTerminalStore.Instance);
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

        var block = _codePlacer.PlaceBlock(StockBlocks.Game.Win);

        _codePlacer.SetSetting(block, 0, (byte)delay);

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Game.Lose"/> block.
    /// </summary>
    /// <param name="delay">Time to lose (in frames).</param>
    public void Lose(int delay)
    {
        ThrowIfGreaterThan(delay, 120);
        ThrowIfLessThan(delay, 0);

        var block = _codePlacer.PlaceBlock(StockBlocks.Game.Lose);

        _codePlacer.SetSetting(block, 0, (byte)delay);

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Game.SetScore"/> block.
    /// </summary>
    /// <param name="ranking">Determines how players are ranked.</param>
    /// <param name="score">The new score. If <paramref name="ranking"/> is <see cref="Ranking.FastestTime"/> or <see cref="Ranking.LongestTime"/>, time is specified in frames (60 - 1s).</param>
    /// <param name="coins">The new amount of coins.</param>
    public void SetScore(Ranking ranking, IExpression score, IExpression coins)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Game.SetScore);

        _codePlacer.SetSetting(block, 0, (byte)ranking);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(score.WriteTo(this), new BlockTerminal(block, "Score"));
            _codePlacer.Connect(coins.WriteTo(this), new BlockTerminal(block, "Coins"));
        }

        ConnectorAdd(new TerminalStore(block));
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
        var block = _codePlacer.PlaceBlock(StockBlocks.Game.SetCamera);

        _codePlacer.SetSetting(block, 0, (byte)(perpective ? 1 : 0));

        using (ExpressionBlock())
        {
            _codePlacer.Connect(position.WriteTo(this), new BlockTerminal(block, "Position"));
            _codePlacer.Connect(rotation.WriteTo(this), new BlockTerminal(block, "Rotation"));
            _codePlacer.Connect(range.WriteTo(this), new BlockTerminal(block, "Range"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Game.SetLight"/> block.
    /// </summary>
    /// <param name="position">Position of the light, <strong>currently unused</strong>.</param>
    /// <param name="rotation">Direction of the light.</param>
    public void SetLight(IExpression position, IExpression rotation)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Game.SetLight);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(position.WriteTo(this), new BlockTerminal(block, "Position"));
            _codePlacer.Connect(rotation.WriteTo(this), new BlockTerminal(block, "Rotation"));
        }

        ConnectorAdd(new TerminalStore(block));
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

        var block = _codePlacer.PlaceBlock(StockBlocks.Game.MenuItem);

        _codePlacer.SetSetting(block, 0, name);
        _codePlacer.SetSetting(block, 1, (byte)maxBuyCount);
        _codePlacer.SetSetting(block, 2, (byte)priceIncrease);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(variable.WriteTo(this), new BlockTerminal(block, "Variable"));
            _codePlacer.Connect(picture.WriteTo(this), new BlockTerminal(block, "Picture"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Objects.SetPos"/> block.
    /// </summary>
    /// <param name="object">The object whose position and rotation is to be set.</param>
    /// <param name="position">The new position.</param>
    /// <param name="rotation">The new rotation.</param>
    public void SetPosition(IExpression @object, IExpression position, IExpression rotation)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Objects.SetPos);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(@object.WriteTo(this), new BlockTerminal(block, "Object"));
            _codePlacer.Connect(position.WriteTo(this), new BlockTerminal(block, "Position"));
            _codePlacer.Connect(rotation.WriteTo(this), new BlockTerminal(block, "Rotation"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Objects.SetPos"/> block.
    /// </summary>
    /// <param name="object">The object whose visibility is to be set.</param>
    /// <param name="visible">The new visibility of the object.</param>
    public void SetVisible(IExpression @object, IExpression visible)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Objects.SetVisible);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(@object.WriteTo(this), new BlockTerminal(block, "Object"));
            _codePlacer.Connect(visible.WriteTo(this), new BlockTerminal(block, "Visible"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Objects.CreateObject"/> block.
    /// </summary>
    /// <param name="object">The object to clone.</param>
    /// <returns>A copy of <paramref name="object"/>.</returns>
    public ITerminal CreateObject(IExpression @object)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Objects.CreateObject);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(@object.WriteTo(this), new BlockTerminal(block, "Object"));
        }

        ConnectorAdd(new TerminalStore(block));

        return new BlockTerminal(block, "Copy");
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Objects.DestroyObject"/> block.
    /// </summary>
    /// <param name="object">The object to be destroyed.</param>
    public void DestroyObject(IExpression @object)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Objects.DestroyObject);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(@object.WriteTo(this), new BlockTerminal(block, "Object"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Sound.PlaySound"/> block.
    /// </summary>
    /// <param name="sound">The sound to play.</param>
    /// <param name="volume">Volume of the sound.</param>
    /// <param name="pitch">Pitch of the sound.</param>
    /// <returns>The channel on which the sound is playing.</returns>
    public ITerminal PlaySound(FcSound sound, IExpression volume, IExpression pitch)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Sound.PlaySound);

        _codePlacer.SetSetting(block, 0, (byte)sound);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(volume.WriteTo(this), new BlockTerminal(block, "Volume"));
            _codePlacer.Connect(pitch.WriteTo(this), new BlockTerminal(block, "Pitch"));
        }

        ConnectorAdd(new TerminalStore(block));

        return new BlockTerminal(block, "Channel");
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Sound.VolumePitch"/> block.
    /// </summary>
    /// <param name="channel">The channel whose sound should be adjusted.</param>
    /// <param name="volume">The channel's new volume.</param>
    /// <param name="pitch">The channel's new pitch.</param>
    public void VolumePitch(IExpression channel, IExpression volume, IExpression pitch)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Sound.VolumePitch);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(channel.WriteTo(this), new BlockTerminal(block, "Channel"));
            _codePlacer.Connect(volume.WriteTo(this), new BlockTerminal(block, "Volume"));
            _codePlacer.Connect(pitch.WriteTo(this), new BlockTerminal(block, "Pitch"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Sound.StopSound"/> block.
    /// </summary>
    /// <param name="channel">The channel whose sound should be stopped.</param>
    public void StopSound(IExpression channel)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Sound.StopSound);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(channel.WriteTo(this), new BlockTerminal(block, "Channel"));
        }

        ConnectorAdd(new TerminalStore(block));
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
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.AddForce);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(@object.WriteTo(this), new BlockTerminal(block, "Object"));
            _codePlacer.Connect(force.WriteTo(this), new BlockTerminal(block, "Force"));
            _codePlacer.Connect(applyAt.WriteTo(this), new BlockTerminal(block, "Apply at"));
            _codePlacer.Connect(torque.WriteTo(this), new BlockTerminal(block, "Torque"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.SetVelocity"/> block.
    /// </summary>
    /// <param name="object">The object whose velocity is to be set.</param>
    /// <param name="velocity">The new velocity of <paramref name="object"/>.</param>
    /// <param name="spin">The new rotational velocity of <paramref name="object"/>.</param>
    public void SetVelocity(IExpression @object, IExpression velocity, IExpression spin)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.SetVelocity);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(@object.WriteTo(this), new BlockTerminal(block, "Object"));
            _codePlacer.Connect(velocity.WriteTo(this), new BlockTerminal(block, "Velocity"));
            _codePlacer.Connect(spin.WriteTo(this), new BlockTerminal(block, "Spin"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.SetLocked"/> block.
    /// </summary>
    /// <param name="object">The object whose movement is to be restricted.</param>
    /// <param name="position">The movement multiplier.</param>
    /// <param name="rotation">The rotation multiplier.</param>
    public void SetLocked(IExpression @object, IExpression position, IExpression rotation)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.SetLocked);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(@object.WriteTo(this), new BlockTerminal(block, "Object"));
            _codePlacer.Connect(position.WriteTo(this), new BlockTerminal(block, "Position"));
            _codePlacer.Connect(rotation.WriteTo(this), new BlockTerminal(block, "Rotation"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.SetMass"/> block.
    /// </summary>
    /// <param name="object">The object whose mass is to be set.</param>
    /// <param name="mass">The new mass of the object.</param>
    public void SetMass(IExpression @object, IExpression mass)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.SetMass);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(@object.WriteTo(this), new BlockTerminal(block, "Object"));
            _codePlacer.Connect(mass.WriteTo(this), new BlockTerminal(block, "Mass"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.SetFriction"/> block.
    /// </summary>
    /// <param name="object">The object whose friction is to be set.</param>
    /// <param name="friction">The new friction of the object.</param>
    public void SetFriction(IExpression @object, IExpression friction)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.SetFriction);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(@object.WriteTo(this), new BlockTerminal(block, "Object"));
            _codePlacer.Connect(friction.WriteTo(this), new BlockTerminal(block, "Friction"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.SetBounciness"/> block.
    /// </summary>
    /// <param name="object">The object whose bounciness is to be set.</param>
    /// <param name="bounciness">The new bounciness of the object.</param>
    public void SetBounciness(IExpression @object, IExpression bounciness)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.SetBounciness);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(@object.WriteTo(this), new BlockTerminal(block, "Object"));
            _codePlacer.Connect(bounciness.WriteTo(this), new BlockTerminal(block, "Bounciness"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.SetGravity"/> block.
    /// </summary>
    /// <param name="gravity">The new gravity.</param>
    public void SetGravity(IExpression gravity)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.SetGravity);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(gravity.WriteTo(this), new BlockTerminal(block, "Gravity"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.AddConstraint"/> block.
    /// </summary>
    /// <param name="base">The base of the constraint.</param>
    /// <param name="part">The part of the constraint.</param>
    /// <param name="pivot">The pivot of the constraint.</param>
    /// <returns>The constraint.</returns>
    public ITerminal AddConstraint(IExpression @base, IExpression part, IExpression pivot)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.AddConstraint);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(@base.WriteTo(this), new BlockTerminal(block, "Base"));
            _codePlacer.Connect(part.WriteTo(this), new BlockTerminal(block, "Part"));
            _codePlacer.Connect(pivot.WriteTo(this), new BlockTerminal(block, "Pivot"));
        }

        ConnectorAdd(new TerminalStore(block));

        return new BlockTerminal(block, "Constraint");
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.LinearLimits"/> block.
    /// </summary>
    /// <param name="constraint">The constraint whose linear limits should be set.</param>
    /// <param name="lower">The lower limit.</param>
    /// <param name="upper">The upper limit.</param>
    public void LinearLimits(IExpression constraint, IExpression lower, IExpression upper)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.LinearLimits);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(constraint.WriteTo(this), new BlockTerminal(block, "Constraint"));
            _codePlacer.Connect(lower.WriteTo(this), new BlockTerminal(block, "Lower"));
            _codePlacer.Connect(upper.WriteTo(this), new BlockTerminal(block, "Upper"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.AngularLimits"/> block.
    /// </summary>
    /// <param name="constraint">The constraint whose angular limits should be set.</param>
    /// <param name="lower">The lower limit.</param>
    /// <param name="upper">The upper limit.</param>
    public void AngularLimits(IExpression constraint, IExpression lower, IExpression upper)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.AngularLimits);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(constraint.WriteTo(this), new BlockTerminal(block, "Constraint"));
            _codePlacer.Connect(lower.WriteTo(this), new BlockTerminal(block, "Lower"));
            _codePlacer.Connect(upper.WriteTo(this), new BlockTerminal(block, "Upper"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.LinearSpring"/> block.
    /// </summary>
    /// <param name="constraint">The constraint whose linear spring should be set.</param>
    /// <param name="stiffness">The spring's stiffness.</param>
    /// <param name="damping">The spring's damping.</param>
    public void LinearSpring(IExpression constraint, IExpression stiffness, IExpression damping)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.LinearSpring);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(constraint.WriteTo(this), new BlockTerminal(block, "Constraint"));
            _codePlacer.Connect(stiffness.WriteTo(this), new BlockTerminal(block, "Stiffness"));
            _codePlacer.Connect(damping.WriteTo(this), new BlockTerminal(block, "Damping"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.AngularSpring"/> block.
    /// </summary>
    /// <param name="constraint">The constraint whose angular spring should be set.</param>
    /// <param name="stiffness">The spring's stiffness.</param>
    /// <param name="damping">The spring's damping.</param>
    public void AngularSpring(IExpression constraint, IExpression stiffness, IExpression damping)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.AngularSpring);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(constraint.WriteTo(this), new BlockTerminal(block, "Constraint"));
            _codePlacer.Connect(stiffness.WriteTo(this), new BlockTerminal(block, "Stiffness"));
            _codePlacer.Connect(damping.WriteTo(this), new BlockTerminal(block, "Damping"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.LinearMotor"/> block.
    /// </summary>
    /// <param name="constraint">The constraint whose linear motor should be set.</param>
    /// <param name="speed">The motor's speed.</param>
    /// <param name="force">The motor's force.</param>
    public void LinearMotor(IExpression constraint, IExpression speed, IExpression force)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.LinearMotor);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(constraint.WriteTo(this), new BlockTerminal(block, "Constraint"));
            _codePlacer.Connect(speed.WriteTo(this), new BlockTerminal(block, "Speed"));
            _codePlacer.Connect(force.WriteTo(this), new BlockTerminal(block, "Force"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Physics.AngularMotor"/> block.
    /// </summary>
    /// <param name="constraint">The constraint whose angular motor should be set.</param>
    /// <param name="speed">The motor's speed.</param>
    /// <param name="force">The motor's force.</param>
    public void AngularMotor(IExpression constraint, IExpression speed, IExpression force)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Physics.AngularMotor);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(constraint.WriteTo(this), new BlockTerminal(block, "Constraint"));
            _codePlacer.Connect(speed.WriteTo(this), new BlockTerminal(block, "Speed"));
            _codePlacer.Connect(force.WriteTo(this), new BlockTerminal(block, "Force"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.If"/> block.
    /// </summary>
    /// <param name="condition">Condition of the if.</param>
    /// <param name="true">Writes what should be executed when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <param name="false">Writes what should be executed when <paramref name="condition"/> is <see langword="false"/>.</param>
    public void If(IExpression condition, Action<CodeWriter>? @true, Action<CodeWriter>? @false)
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.If);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(condition.WriteTo(this), new BlockTerminal(block, "Condition"));
        }

        ConnectorAdd(new TerminalStore(block));

        ConnectOut(new BlockTerminal(block, "True"), @true);
        ConnectOut(new BlockTerminal(block, "False"), @false);

        _connector.SetLast(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.PlaySensor"/> block.
    /// </summary>
    /// <param name="onPlay">Writes what should be executed only on the first frame.</param>
    public void PlaySensor(Action<CodeWriter>? onPlay)
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.PlaySensor);

        ConnectorAdd(new TerminalStore(block));

        ConnectOut(new BlockTerminal(block, "On Play"), onPlay);

        _connector.SetLast(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.LateUpdate"/> block.
    /// </summary>
    /// <param name="afterPhysics">Writes what should be executed only after physics but before rendering.</param>
    public void LateUpdate(Action<CodeWriter>? afterPhysics)
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.LateUpdate);

        ConnectorAdd(new TerminalStore(block));

        ConnectOut(new BlockTerminal(block, "After Physics"), afterPhysics);

        _connector.SetLast(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.BoxArtSensor"/> block.
    /// </summary>
    /// <param name="onScreenshot">Writes what should be executed only when taking boxart.</param>
    public void BoxArtSensor(Action<CodeWriter>? onScreenshot)
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.BoxArtSensor);

        ConnectorAdd(new TerminalStore(block));

        ConnectOut(new BlockTerminal(block, "On Screenshot"), onScreenshot);

        _connector.SetLast(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.TouchSensor"/> block.
    /// </summary>
    /// <param name="touchState">The <see cref="TouchState"/> to detect.</param>
    /// <param name="touchFinger">Index of the finger to detect, 0 - 2.</param>
    /// <param name="touched">Writes what should be executed when touch is detected.</param>
    /// <returns>The x and y position of the touch.</returns>
    public (ITerminal ScreenX, ITerminal ScreenY) TouchSensor(TouchState touchState, int touchFinger, Action<CodeWriter, ITerminal, ITerminal>? touched)
    {
        if (touchFinger < 0 || touchFinger > FancadeConstants.TouchSensorMaxFingerIndex)
        {
            ThrowArgumentOutOfRangeException(nameof(touchFinger));
        }

        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.TouchSensor);

        _codePlacer.SetSetting(block, 0, (byte)touchState);
        _codePlacer.SetSetting(block, 1, (byte)touchFinger);

        ConnectorAdd(new TerminalStore(block));

        ConnectOut(new BlockTerminal(block, "Touched"), touched, new BlockTerminal(block, "Screen X"), new BlockTerminal(block, "Screen Y"));

        _connector.SetLast(new TerminalStore(block));

        return (new BlockTerminal(block, "Screen X"), new BlockTerminal(block, "Screen Y"));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.SwipeSensor"/> block.
    /// </summary>
    /// <param name="swiped">Writes what should be executed when swipe is detected.</param>
    /// <returns>Direction of the swipe.</returns>
    public ITerminal SwipeSensor(Action<CodeWriter, ITerminal>? swiped)
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.SwipeSensor);

        ConnectorAdd(new TerminalStore(block));

        ConnectOut(new BlockTerminal(block, "Swiped"), swiped, new BlockTerminal(block, "Direction"));

        _connector.SetLast(new TerminalStore(block));

        return new BlockTerminal(block, "Direction");
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.Button"/> block.
    /// </summary>
    /// <param name="buttonType">Type of the button.</param>
    /// <param name="button">Writes what should be executed when the button is pressed.</param>
    public void Button(ButtonType buttonType, Action<CodeWriter>? button)
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.Button);

        _codePlacer.SetSetting(block, 0, (byte)buttonType);

        ConnectorAdd(new TerminalStore(block));

        ConnectOut(new BlockTerminal(block, "Button"), button);

        _connector.SetLast(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.Collision"/> block.
    /// </summary>
    /// <param name="firstObject">The object whose collisions should be detected.</param>
    /// <param name="collided">Writes what should be executed when <paramref name="firstObject"/> collides with another object.</param>
    /// <returns>The object <paramref name="firstObject"/> collided with, impulse of the collision and the normal of the collision.</returns>
    public (ITerminal SecondObjectTerminal, ITerminal ImpulseTerminal, ITerminal NormalTerminal) Collision(IExpression firstObject, Action<CodeWriter, ITerminal, ITerminal, ITerminal>? collided)
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.Collision);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(firstObject.WriteTo(this), new BlockTerminal(block, "1st Object"));
        }

        ConnectorAdd(new TerminalStore(block));

        ConnectOut(new BlockTerminal(block, "Collided"), collided, new BlockTerminal(block, "2nd Object"), new BlockTerminal(block, "Impulse"), new BlockTerminal(block, "Normal"));

        _connector.SetLast(new TerminalStore(block));

        return (new BlockTerminal(block, "2nd Object"), new BlockTerminal(block, "Impulse"), new BlockTerminal(block, "Normal"));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Control.Loop"/> block.
    /// </summary>
    /// <param name="start">The start value (inclusive).</param>
    /// <param name="stop">The end value (exclusive).</param>
    /// <param name="do">Writes what should be executed in the loop.</param>
    /// <returns>The current value of the loop.</returns>
    public ITerminal Loop(IExpression start, IExpression stop, Action<CodeWriter, ITerminal>? @do)
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.Loop);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(start.WriteTo(this), new BlockTerminal(block, "Start"));
            _codePlacer.Connect(stop.WriteTo(this), new BlockTerminal(block, "Stop"));
        }

        ConnectorAdd(new TerminalStore(block));

        ConnectOut(new BlockTerminal(block, "Do"), @do, new BlockTerminal(block, "Counter"));

        _connector.SetLast(new TerminalStore(block));

        return new BlockTerminal(block, "Counter");
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Math.RandomSeed"/> block.
    /// </summary>
    /// <param name="seed">The new random seed.</param>
    public void RandomSeed(IExpression seed)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Math.RandomSeed);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(seed.WriteTo(this), new BlockTerminal(block, "Seed"));
        }

        ConnectorAdd(new TerminalStore(block));
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
            Block block = _codePlacer.PlaceBlock(StockBlocks.Values.Comment);
            _codePlacer.SetSetting(block, 0, new string(span[lineRange]));
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
        var block = _codePlacer.PlaceBlock(StockBlocks.Values.InspectByType(type));

        using (ExpressionBlock())
        {
            _codePlacer.Connect(value.WriteTo(this), new BlockTerminal(block, 1));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the set variable block.
    /// </summary>
    /// <param name="variable">The variable whose value should be set.</param>
    /// <param name="value">The new value of the variable.</param>
    public void SetVariable(Variable variable, IExpression value)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Variables.SetVariableByType(variable.Type));

        _codePlacer.SetSetting(block, 0, variable.Name);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(value.WriteTo(this), new BlockTerminal(block, "Value"));
        }

        ConnectorAdd(new TerminalStore(block));
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
        var block = _codePlacer.PlaceBlock(StockBlocks.Variables.SetPtrByType(variableType));

        using (ExpressionBlock())
        {
            _codePlacer.Connect(variable.WriteTo(this), new BlockTerminal(block, "Variable"));
            _codePlacer.Connect(value.WriteTo(this), new BlockTerminal(block, "Value"));
        }

        ConnectorAdd(new TerminalStore(block));
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

        ITerminal? lastElementTerminal = null;

        var variableEx = Expressions.Variable(variable);

        for (int i = 0; i < values.Length; i++)
        {
            if (i == 0 && IsLiteralOfValue(startIndex, 0f))
            {
                SetVariable(variable, Expressions.Literal(values[i]));
            }
            else
            {
                Block setBlock = _codePlacer.PlaceBlock(StockBlocks.Variables.SetPtrByType(signalType));

                ConnectorAdd(new TerminalStore(setBlock));

                using (ExpressionBlock())
                {
                    Block listBlock = _codePlacer.PlaceBlock(StockBlocks.Variables.ListByType(signalType));

                    _codePlacer.Connect(TerminalStore.CreateOut(listBlock, listBlock.Type["Element"]), TerminalStore.CreateIn(setBlock, setBlock.Type["Variable"]));

                    using (ExpressionBlock())
                    {
                        lastElementTerminal ??= variableEx.WriteTo(this);

                        _codePlacer.Connect(lastElementTerminal, TerminalStore.CreateIn(listBlock, listBlock.Type["Variable"]));

                        lastElementTerminal = new BlockTerminal(listBlock, "Element");

                        _codePlacer.Connect(i == 0 ? startIndex.WriteTo(this) : Expressions.Number(1f).WriteTo(this), TerminalStore.CreateIn(listBlock, listBlock.Type["Index"]));
                    }

                    _codePlacer.Connect(Expressions.Literal(values[i]).WriteTo(this), TerminalStore.CreateIn(setBlock, setBlock.Type["Value"]));
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
        Block block = _codePlacer.PlaceBlock(StockBlocks.Variables.IncrementNumber);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(variable.WriteTo(this), new BlockTerminal(block, "Variable"));
        }

        ConnectorAdd(new TerminalStore(block));
    }

    /// <summary>
    /// Writes the <see cref="StockBlocks.Variables.DecrementNumber"/> block.
    /// </summary>
    /// <param name="variable">The variable that should be decremented.</param>
    public void DecrementNumber(IExpression variable)
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Variables.DecrementNumber);

        using (ExpressionBlock())
        {
            _codePlacer.Connect(variable.WriteTo(this), new BlockTerminal(block, "Variable"));
        }

        ConnectorAdd(new TerminalStore(block));
    }
    #endregion

    /// <summary>
    /// Flushes the <see cref="CodeWriter"/>.
    /// </summary>
    /// <remarks>
    /// Processes gotos and calls <see cref="ICodePlacer.Flush"/> on the underlying <see cref="ICodePlacer"/>.
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
                    case ITerminal terminal:
                        _codePlacer.Connect(store, terminal);
                        goto nextGoto;
                    case string label:
                        gotoLabel = label;
                        break;
                    case null:
                        goto nextGoto; // a label was defined, but nothing was placed after it
                    default:
                        Debug.Fail($"Expected labelTarget to be {nameof(ITerminal)} or string, but it was: {labelTarget.GetType().FullName}");
                        goto nextGoto;
                }
            }

            throw new GotoRecursionException(item.LabelName, [.. encounteredLabels]);

        nextGoto:;
        }

        _gotos.Clear();
        _codePlacer.Flush();
    }

    #region Blocks

    /// <summary>
    /// Enters a statement block.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/>, that when disposed exits the statement block.</returns>
    public IDisposable StatementBlock()
        => _codePlacer.StatementBlock();

    /// <summary>
    /// Enters an expression block.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/>, that when disposed exits the expression block.</returns>
    public IDisposable ExpressionBlock()
        => _codePlacer.ExpressionBlock();

    /// <summary>
    /// Enters a highlight block.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/>, that when disposed exits the highlight block.</returns>
    public IDisposable HighlightBlock()
        => _codePlacer.HighlightBlock();

    #endregion

    private static bool IsLiteralOfValue(IExpression expression, float value)
        => expression is Expressions.LiteralExpression literal && literal.Type == SignalType.Float && (float)literal._value == value;

    private void ConnectorAdd(TerminalStore store)
    {
        _connector.Add(store);

        while (_labelsToProcess.TryDequeue(out string? labelName))
        {
            _labels[labelName] = store.In;
        }
    }

    private void ConnectOut(ITerminal terminal, Action<CodeWriter>? writeFunc)
    {
        if (writeFunc is null)
        {
            return;
        }

        _connector.SetLast(new TerminalStore(NopTerminal.Instance, [terminal]));
        using (_codePlacer.StatementBlock())
        {
            writeFunc(this);
        }
    }

    private void ConnectOut(ITerminal terminal, Action<CodeWriter, ITerminal>? writeFunc, ITerminal arg1)
    {
        if (writeFunc is null)
        {
            return;
        }

        _connector.SetLast(new TerminalStore(NopTerminal.Instance, [terminal]));
        using (_codePlacer.StatementBlock())
        {
            writeFunc(this, arg1);
        }
    }

    private void ConnectOut(ITerminal terminal, Action<CodeWriter, ITerminal, ITerminal>? writeFunc, ITerminal arg1, ITerminal arg2)
    {
        if (writeFunc is null)
        {
            return;
        }

        _connector.SetLast(new TerminalStore(NopTerminal.Instance, [terminal]));
        using (_codePlacer.StatementBlock())
        {
            writeFunc(this, arg1, arg2);
        }
    }

    private void ConnectOut(ITerminal terminal, Action<CodeWriter, ITerminal, ITerminal, ITerminal>? writeFunc, ITerminal arg1, ITerminal arg2, ITerminal arg3)
    {
        if (writeFunc is null)
        {
            return;
        }

        _connector.SetLast(new TerminalStore(NopTerminal.Instance, [terminal]));
        using (_codePlacer.StatementBlock())
        {
            writeFunc(this, arg1, arg2, arg3);
        }
    }
}
