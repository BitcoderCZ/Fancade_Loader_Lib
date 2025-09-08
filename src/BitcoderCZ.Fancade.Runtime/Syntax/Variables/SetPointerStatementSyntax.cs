using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Variables;

/// <summary>
/// A <see cref="SyntaxNode"/> for any set pointer prefab.
/// </summary>
public sealed class SetPointerStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetPointerStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="variable">The variable terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="value">The value terminal; or <see langword="null"/>, if it is not connected.</param>
    public SetPointerStatementSyntax(ushort prefabId, int3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? variable, SyntaxTerminal? value)
        : base(prefabId, position, outVoidConnections)
    {
        if (prefabId is not (58 or 62 or 66 or 70 or 74 or 78))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 58 or 62 or 66 or 70 or 74 or 78.");
        }

        Variable = variable;
        Value = value;
    }

    /// <summary>
    /// Gets the variable terminal.
    /// </summary>
    /// <value>The variable terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Variable { get; }

    /// <summary>
    /// Gets the value terminal.
    /// </summary>
    /// <value>The value terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Value { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
