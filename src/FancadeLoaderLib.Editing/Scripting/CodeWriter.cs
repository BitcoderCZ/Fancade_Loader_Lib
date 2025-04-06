// <copyright file="CodeWriter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Placers;
using FancadeLoaderLib.Editing.Scripting.Settings;
using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using FancadeLoaderLib.Editing.Scripting.Utils;
using FancadeLoaderLib.Editing.Utils;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing.Scripting;

/// <summary>
/// A helper class for writing fancade code.
/// </summary>
public sealed class CodeWriter
{
    private readonly IScopedCodePlacer _codePlacer;

    private readonly Dictionary<string, BreakBlockCache> _vectorBreakCache = [];
    private readonly Dictionary<string, BreakBlockCache> _rotationBreakCache = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeWriter"/> class.
    /// </summary>
    /// <param name="codePlacer">The <see cref="ICodePlacer"/> used to place blocks.</param>
    public CodeWriter(ICodePlacer codePlacer)
    {
        _codePlacer = codePlacer is IScopedCodePlacer scoped ? scoped : new ScopedCodePlacerWrapper(codePlacer);
    }

    /// <summary>
    /// Places a literal value.
    /// </summary>
    /// <remarks>
    /// <paramref name="value"/> can be one of: <see cref="bool"/>, <see cref="float"/>, <see cref="float3"/>, <see cref="Rotation"/>.
    /// </remarks>
    /// <param name="value">The value to place.</param>
    /// <returns>The out terminal of the literal block.</returns>
    public ITerminal PlaceLiteral(object value)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Values.ValueByType(value));

        if (value is not bool)
        {
            _codePlacer.SetSetting(block, 0, value);
        }

        return new BlockTerminal(block, block.Type.Terminals[0]);
    }

    /// <summary>
    /// Places a variable.
    /// </summary>
    /// <param name="name">Name of the variable to place.</param>
    /// <param name="type">Type of the variable to place.</param>
    /// <returns>The out terminal of the variable block.</returns>
    public ITerminal PlaceVariable(string name, SignalType type)
    {
        var block = _codePlacer.PlaceBlock(StockBlocks.Variables.GetVariableByType(type));

        _codePlacer.SetSetting(block, 0, name);

        return new BlockTerminal(block, block.Type.Terminals[0]);
    }

    /// <summary>
    /// Places a comment.
    /// </summary>
    /// <remarks>
    /// If <paramref name="text"/> is too long, multiple comment blocks are placed.
    /// </remarks>
    /// <param name="text">Text of the comment.</param>
    public void PlaceComment(string text)
    {
        var span = text.AsSpan();

        foreach (var lineRange in StringUtils.SplitByMaxLength(span, FancadeConstants.MaxCommentLength))
        {
            Block block = _codePlacer.PlaceBlock(StockBlocks.Values.Comment);
            _codePlacer.SetSetting(block, 0, new string(span[lineRange]));
        }
    }

    /// <summary>
    /// Sets a variable to a value.
    /// </summary>
    /// <param name="name">Name of the variable.</param>
    /// <param name="value">The value to set the variable to.</param>
    /// <returns>The before and after terminals of the set variable block.</returns>
    public ITerminalStore SetVariable(string name, object value)
    {
        ThrowIfNull(value, nameof(value));

        var signalType = SignalTypeUtils.FromType(value.GetType());

        var block = _codePlacer.PlaceBlock(StockBlocks.Variables.SetVariableByType(signalType));

        _codePlacer.SetSetting(block, 0, name);

        using (_codePlacer.ExpressionBlock())
        {
            var valueTerminal = PlaceLiteral(value);

            _codePlacer.Connect(valueTerminal, new BlockTerminal(block, "Value"));
        }

        return new TerminalStore(block);
    }

    /// <summary>
    /// Sets a variable to a terminal.
    /// </summary>
    /// <param name="name">Name of the variable.</param>
    /// <param name="terminal">The terminal to connect to the set variable input.</param>
    /// <returns>The before and after terminals of the set variable block.</returns>
    public ITerminalStore SetVariable(string name, ITerminal terminal)
    {
        ThrowIfNull(terminal, nameof(terminal));

        var block = _codePlacer.PlaceBlock(StockBlocks.Variables.SetVariableByType(terminal.SignalType));

        _codePlacer.Connect(terminal, new BlockTerminal(block, "Value"));

        _codePlacer.SetSetting(block, 0, name);

        return new TerminalStore(block);
    }

    /// <summary>
    /// Sets a variable to a terminal.
    /// </summary>
    /// <param name="name">Name of the variable.</param>
    /// <param name="getTerminalFunc">A method to get the terminal.</param>
    /// <param name="terminalType">Type of the variable.</param>
    /// <returns>The before and after terminals of the set variable block.</returns>
    public ITerminalStore SetVariable(string name, Func<ITerminal> getTerminalFunc, SignalType terminalType)
    {
        ThrowIfNull(getTerminalFunc, nameof(getTerminalFunc));

        var block = _codePlacer.PlaceBlock(StockBlocks.Variables.SetVariableByType(terminalType));

        _codePlacer.Connect(getTerminalFunc(), new BlockTerminal(block, "Value"));

        _codePlacer.SetSetting(block, 0, name);

        return new TerminalStore(block);
    }

    /// <summary>
    /// Sets a range of a fancade list.
    /// </summary>
    /// <typeparam name="T">The type of the list elements.</typeparam>
    /// <param name="variableName">Name of the variable.</param>
    /// <param name="values">The values to set.</param>
    /// <param name="startIndex">The index to start to set at.</param>
    /// <returns>A <see cref="ITerminalStore"/> representing the beginning and end of the operation.</returns>
    public ITerminalStore SetListRange<T>(string variableName, ReadOnlySpan<T> values, int startIndex)
        where T : notnull
    {
        var signalType = SignalTypeUtils.FromType(typeof(T));

        TerminalConnector connector = new TerminalConnector(_codePlacer.Connect);

        ITerminal? lastElementTerminal = null;

        for (int i = 0; i < values.Length; i++)
        {
            if (i == 0 && startIndex == 0)
            {
                connector.Add(SetVariable(variableName, values[i]));
            }
            else
            {
                Block setBlock = _codePlacer.PlaceBlock(StockBlocks.Variables.SetPtrByType(signalType));

                connector.Add(new TerminalStore(setBlock));

                using (ExpressionBlock())
                {
                    Block listBlock = _codePlacer.PlaceBlock(StockBlocks.Variables.ListByType(signalType));

                    _codePlacer.Connect(TerminalStore.CreateOut(listBlock, listBlock.Type["Element"]), TerminalStore.CreateIn(setBlock, setBlock.Type["Variable"]));

                    using (ExpressionBlock())
                    {
                        lastElementTerminal ??= PlaceVariable(variableName, signalType);

                        _codePlacer.Connect(lastElementTerminal, TerminalStore.CreateIn(listBlock, listBlock.Type["Variable"]));

                        lastElementTerminal = new BlockTerminal(listBlock, "Element");

                        _codePlacer.Connect(i == 0 ? PlaceLiteral((float)startIndex) : PlaceLiteral(1f), TerminalStore.CreateIn(listBlock, listBlock.Type["Index"]));
                    }

                    _codePlacer.Connect(PlaceLiteral(values[i]), TerminalStore.CreateIn(setBlock, setBlock.Type["Value"]));
                }
            }
        }

        return connector.Store;
    }

    /// <summary>
    /// Sets a range of a fancade list.
    /// </summary>
    /// <typeparam name="T">The type of the list elements.</typeparam>
    /// <param name="variableName">Name of the variable.</param>
    /// <param name="values">The values to set.</param>
    /// <param name="startIndexVariableName">Name of the fancade number variable used as the index to start to set at.</param>
    /// <returns>A <see cref="ITerminalStore"/> representing the beginning and end of the operation.</returns>
    public ITerminalStore SetListRange<T>(string variableName, ReadOnlySpan<T> values, string startIndexVariableName)
        where T : notnull
    {
        var signalType = SignalTypeUtils.FromType(typeof(T));

        TerminalConnector connector = new TerminalConnector(_codePlacer.Connect);

        ITerminal? lastElementTerminal = null;

        for (int i = 0; i < values.Length; i++)
        {
            Block setBlock = _codePlacer.PlaceBlock(StockBlocks.Variables.SetPtrByType(signalType));

            connector.Add(new TerminalStore(setBlock));

            using (ExpressionBlock())
            {
                Block listBlock = _codePlacer.PlaceBlock(StockBlocks.Variables.ListByType(signalType));

                _codePlacer.Connect(TerminalStore.CreateOut(listBlock, listBlock.Type["Element"]), TerminalStore.CreateIn(setBlock, setBlock.Type["Variable"]));

                using (ExpressionBlock())
                {
                    lastElementTerminal ??= PlaceVariable(variableName, signalType);

                    _codePlacer.Connect(lastElementTerminal, TerminalStore.CreateIn(listBlock, listBlock.Type["Variable"]));

                    lastElementTerminal = new BlockTerminal(listBlock, "Element");

                    _codePlacer.Connect(i == 0 ? PlaceVariable(startIndexVariableName, SignalType.Float) : PlaceLiteral(1f), TerminalStore.CreateIn(listBlock, listBlock.Type["Index"]));
                }

                _codePlacer.Connect(PlaceLiteral(values[i]), TerminalStore.CreateIn(setBlock, setBlock.Type["Value"]));
            }
        }

        return connector.Store;
    }

    /// <summary>
    /// Breaks a vector or rotation variable.
    /// </summary>
    /// <param name="variableName">Name of the variable.</param>
    /// <param name="type">Type of the variable, must be <see cref="SignalType.Vec3"/>(Ptr) or <see cref="SignalType.Rot"/>(Ptr).</param>
    /// <param name="cache">If the break block should be cached.</param>
    /// <returns>The X, Y and Z out terminal.</returns>
    /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="type"/> is not <see cref="SignalType.Vec3"/>(Ptr) or <see cref="SignalType.Rot"/>(Ptr).</exception>
    public (ITerminal X, ITerminal Y, ITerminal Z) BreakVector(string variableName, SignalType type, bool cache = true)
    {
        type = type.ToNotPointer();

        switch (type)
        {
            case SignalType.Vec3:
            case SignalType.Rot:
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SignalType));
        }

        Block? block;

        if (cache)
        {
            BreakBlockCache blockCache = (type == SignalType.Vec3 ? _vectorBreakCache : _rotationBreakCache)
                .AddIfAbsent(variableName, new BreakBlockCache(null, FancadeConstants.MaxWireSplits));

            if (!blockCache.TryGet(out block))
            {
                block = _codePlacer.PlaceBlock(type == SignalType.Vec3 ? StockBlocks.Math.Break_Vector : StockBlocks.Math.Break_Rotation);
                blockCache.SetNewBlock(block);

                using (ExpressionBlock())
                {
                    ITerminal variableTerminal = PlaceVariable(variableName, type);
                    _codePlacer.Connect(variableTerminal, new BlockTerminal(block, 3));
                }
            }
        }
        else
        {
            block = _codePlacer.PlaceBlock(StockBlocks.Math.BreakByType(type));
        }

        using (ExpressionBlock())
        {
            ITerminal terminal = PlaceVariable(variableName, type);
            _codePlacer.Connect(terminal, new BlockTerminal(block, 3));
        }

        return (
            new BlockTerminal(block, 2),
            new BlockTerminal(block, 1),
            new BlockTerminal(block, 0)
        );
    }

    /// <summary>
    /// Places the if block.
    /// </summary>
    /// <param name="getConditionTerminalFunc">A method to get the bool terminal to connect to the condition input.</param>
    /// <returns>A <see cref="ITerminalStore"/> with the before and after terminals of the if block and the true and false terminals.</returns>
    public (ITerminalStore Store, ITerminal TrueTerminal, ITerminal FalseTerminal) PlaceIf(Func<ITerminal> getConditionTerminalFunc)
    {
        ThrowIfNull(getConditionTerminalFunc, nameof(getConditionTerminalFunc));

        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.If);

        ITerminal condition = getConditionTerminalFunc();

        _codePlacer.Connect(condition, new BlockTerminal(block, "Condition"));

        return (new TerminalStore(block), new BlockTerminal(block, "True"), new BlockTerminal(block, "False"));
    }

    /// <summary>
    /// Places the play sensor block.
    /// </summary>
    /// <returns>A <see cref="ITerminalStore"/> with the before and after terminals of the play sensor block and the on play terminal.</returns>
    public (ITerminalStore Store, ITerminal OnPlayTerminal) PlacePlaySensor()
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.PlaySensor);

        return (new TerminalStore(block), new BlockTerminal(block, "On Play"));
    }

    /// <summary>
    /// Places the late update block.
    /// </summary>
    /// <returns>A <see cref="ITerminalStore"/> with the before and after terminals of the late update block and the after physics terminal.</returns>
    public (ITerminalStore Store, ITerminal AfterPhysicsTerminal) PlaceLateUpdate()
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.LateUpdate);

        return (new TerminalStore(block), new BlockTerminal(block, "After Physics"));
    }

    /// <summary>
    /// Places the box art sensor block.
    /// </summary>
    /// <returns>A <see cref="ITerminalStore"/> with the before and after terminals of the box art sensor block and the on screenshot terminal.</returns>
    public (ITerminalStore Store, ITerminal OnScreenshotTerminal) PlaceBoxArtSensor()
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.BoxArtSensor);

        return (new TerminalStore(block), new BlockTerminal(block, "On Screenshot"));
    }

    /// <summary>
    /// Places the touch sensor block.
    /// </summary>
    /// <param name="touchState">The state of touch to detect.</param>
    /// <param name="touchFinger">The finger to detect.</param>
    /// <returns>A <see cref="ITerminalStore"/> with the before and after terminals of the touch sensor block and the touched, screen x and screen y terminals.</returns>
    public (ITerminalStore Store, ITerminal TouchedTerminal, ITerminal ScreenXTerminal, ITerminal ScreenYTerminal) PlaceTouchSensor(TouchState touchState, int touchFinger)
    {
        if (touchFinger < 0 || touchFinger > FancadeConstants.TouchSensorMaxFingerIndex)
        {
            ThrowArgumentOutOfRangeException(nameof(touchFinger));
        }

        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.TouchSensor);

        _codePlacer.SetSetting(block, 0, (byte)touchState);
        _codePlacer.SetSetting(block, 1, (byte)touchFinger);

        return (new TerminalStore(block), new BlockTerminal(block, "Touched"), new BlockTerminal(block, "Screen X"), new BlockTerminal(block, "Screen Y"));
    }

    /// <summary>
    /// Places the swipe sensor block.
    /// </summary>
    /// <returns>A <see cref="ITerminalStore"/> with the before and after terminals of the swipe sensor block and the swiped and direction terminals.</returns>
    public (ITerminalStore Store, ITerminal SwipedTerminal, ITerminal DirectionTerminal) PlaceSwipeSensor()
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.SwipeSensor);

        return (new TerminalStore(block), new BlockTerminal(block, "Swiped"), new BlockTerminal(block, "Direction"));
    }

    /// <summary>
    /// Places the button block.
    /// </summary>
    /// <param name="buttonType">Type of the button.</param>
    /// <returns>A <see cref="ITerminalStore"/> with the before and after terminals of the button block and the button terminal.</returns>
    public (ITerminalStore Store, ITerminal ButtonTerminal) PlaceButton(ButtonType buttonType)
    {
        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.Button);

        _codePlacer.SetSetting(block, 0, (byte)buttonType);

        return (new TerminalStore(block), new BlockTerminal(block, "Button"));
    }

    /// <summary>
    /// Places the collision block.
    /// </summary>
    /// <param name="getFirstObjectTerminalFunc">A method to get the object terminal to connect to the first object terminal.</param>
    /// <returns>A <see cref="ITerminalStore"/> with the before and after terminals of the collision block and the collided, second object, impulse and normal terminals.</returns>
    public (ITerminalStore Store, ITerminal CollidedTerminal, ITerminal SecondObjectTerminal, ITerminal ImpulseTerminal, ITerminal NormalTerminal) PlaceCollision(Func<ITerminal> getFirstObjectTerminalFunc)
    {
        ThrowIfNull(getFirstObjectTerminalFunc, nameof(getFirstObjectTerminalFunc));

        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.Collision);

        _codePlacer.Connect(getFirstObjectTerminalFunc(), new BlockTerminal(block, "1st Object"));

        return (new TerminalStore(block), new BlockTerminal(block, "Collided"), new BlockTerminal(block, "2nd Object"), new BlockTerminal(block, "Impulse"), new BlockTerminal(block, "Normal"));
    }

    /// <summary>
    /// Places the loop block.
    /// </summary>
    /// <param name="getStartTerminalFunc">A method to get the number terminal to connect to the start terminal.</param>
    /// <param name="getStopTerminalFunc">A method to get the number terminal to connect to the stop terminal.</param>
    /// <returns>A <see cref="ITerminalStore"/> with the before and after terminals of the loop block and the do and counter terminals.</returns>
    public (ITerminalStore Store, ITerminal DoTerminal, ITerminal CounterTerminal) PlaceLoop(Func<ITerminal> getStartTerminalFunc, Func<ITerminal> getStopTerminalFunc)
    {
        ThrowIfNull(getStartTerminalFunc, nameof(getStartTerminalFunc));
        ThrowIfNull(getStopTerminalFunc, nameof(getStopTerminalFunc));

        Block block = _codePlacer.PlaceBlock(StockBlocks.Control.Loop);

        _codePlacer.Connect(getStartTerminalFunc(), new BlockTerminal(block, "Start"));
        _codePlacer.Connect(getStopTerminalFunc(), new BlockTerminal(block, "Stop"));

        return (new TerminalStore(block), new BlockTerminal(block, "Do"), new BlockTerminal(block, "Counter"));
    }

    /// <summary>
    /// Places the increase number block.
    /// </summary>
    /// <param name="getVariableTerminalFunc">A method to get a terminal to connect to the variable terminal.</param>
    /// <returns>A <see cref="ITerminalStore"/> with the before and after terminals of the increase number block.</returns>
    public ITerminalStore PlaceIncrementNumber(Func<ITerminal> getVariableTerminalFunc)
    {
        ThrowIfNull(getVariableTerminalFunc, nameof(getVariableTerminalFunc));

        Block block = _codePlacer.PlaceBlock(StockBlocks.Variables.IncrementNumber);

        _codePlacer.Connect(getVariableTerminalFunc(), new BlockTerminal(block, "Variable"));

        return new TerminalStore(block);
    }

    /// <summary>
    /// Places the decrease number block.
    /// </summary>
    /// <param name="getVariableTerminalFunc">A method to get a terminal to connect to the variable terminal.</param>
    /// <returns>A <see cref="ITerminalStore"/> with the before and after terminals of the decrease number block.</returns>
    public ITerminalStore PlaceDecrementNumber(Func<ITerminal> getVariableTerminalFunc)
    {
        ThrowIfNull(getVariableTerminalFunc, nameof(getVariableTerminalFunc));

        Block block = _codePlacer.PlaceBlock(StockBlocks.Variables.DecrementNumber);

        _codePlacer.Connect(getVariableTerminalFunc(), new BlockTerminal(block, "Variable"));

        return new TerminalStore(block);
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
}
