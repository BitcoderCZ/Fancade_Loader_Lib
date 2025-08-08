using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

/// <summary>
/// A <see cref="SyntaxNode"/> for the set camera prefab.
/// </summary>
public sealed class SetCameraStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetCameraStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="positionTerminal">The position terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="rotationTerminal">The rotation terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="rangeTerminal">The range terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="perspective">If <see langword="true"/>, the camera will be in perspective mode; otherwise, the camera will be in orthographic mode.</param>
    public SetCameraStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? positionTerminal, SyntaxTerminal? rotationTerminal, SyntaxTerminal? rangeTerminal, bool perspective)
        : base(prefabId, position, outVoidConnections)
    {
        PositionTerminal = positionTerminal;
        RotationTerminal = rotationTerminal;
        RangeTerminal = rangeTerminal;
        Perspective = perspective;
    }

    /// <summary>
    /// Gets the position terminal.
    /// </summary>
    /// <value>The position terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? PositionTerminal { get; }

    /// <summary>
    /// Gets the rotation terminal.
    /// </summary>
    /// <value>The rotation terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? RotationTerminal { get; }

    /// <summary>
    /// Gets the range terminal.
    /// </summary>
    /// <value>The range terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? RangeTerminal { get; }

    /// <summary>
    /// Gets a value indicating whether the camera is in perspective mode.
    /// </summary>
    /// <value>If <see langword="true"/>, the camera will be in perspective mode; otherwise, the camera will be in orthographic mode.</value>
    public bool Perspective { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
