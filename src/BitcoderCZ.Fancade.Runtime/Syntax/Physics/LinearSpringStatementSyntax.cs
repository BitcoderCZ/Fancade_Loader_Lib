// <copyright file="LinearSpringStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

/// <summary>
/// A <see cref="SyntaxNode"/> for the linear spring prefab.
/// </summary>
public sealed class LinearSpringStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LinearSpringStatementSyntax"/> class.
    /// </summary>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="constraint">The constraint terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="stiffness">The stiffness terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="damping">The damping terminal; or <see langword="null"/>, if it is not connected.</param>
    public LinearSpringStatementSyntax(int3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? constraint, SyntaxTerminal? stiffness, SyntaxTerminal? damping)
        : base(358, position, outVoidConnections)
    {
        Constraint = constraint;
        Stiffness = stiffness;
        Damping = damping;
    }

    /// <summary>
    /// Gets the constraint terminal.
    /// </summary>
    /// <value>The constraint terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Constraint { get; }

    /// <summary>
    /// Gets the position terminal.
    /// </summary>
    /// <value>The position terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Stiffness { get; }

    /// <summary>
    /// Gets the damping terminal.
    /// </summary>
    /// <value>The damping terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Damping { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
