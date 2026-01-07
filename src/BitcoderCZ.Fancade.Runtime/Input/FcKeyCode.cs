// <copyright file="FcKeyCode.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace BitcoderCZ.Fancade.Runtime.Input;

/// <summary>
/// Fancade buttons.
/// </summary>
public enum FcKeyCode
{
    /// <summary>
    /// The up key; W, up arrow.
    /// </summary>
    Up,

    /// <summary>
    /// The down key; S, down arrow.
    /// </summary>
    Down,

    /// <summary>
    /// The left key; A, left arrow.
    /// </summary>
    Left,

    /// <summary>
    /// The right key; D, right arrow.
    /// </summary>
    Right,

    /// <summary>
    /// The second up key; I.
    /// </summary>
    Up2,

    /// <summary>
    /// The second down key; K.
    /// </summary>
    Down2,

    /// <summary>
    /// The second left key; J.
    /// </summary>
    Left2,

    /// <summary>
    /// The second right key; L.
    /// </summary>
    Right2,

    /// <summary>
    /// The action 1 key; Z.
    /// </summary>
    Action1,

    /// <summary>
    /// The action 2 key; X.
    /// </summary>
    Action2,

    /// <summary>
    /// The action 3 key; C.
    /// </summary>
    Action3,

    /// <summary>
    /// The action 4 key; V.
    /// </summary>
    Action4,

    /// <summary>
    /// The space key; spacebar.
    /// </summary>
    Space,

    /// <summary>
    /// The last value of <see cref="FcKeyCode"/>.
    /// </summary>
    [Obsolete]
    LastValue,
}