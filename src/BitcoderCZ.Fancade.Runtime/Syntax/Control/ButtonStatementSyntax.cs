using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

/// <summary>
/// A <see cref="SyntaxNode"/> for the button prefab.
/// </summary>
public sealed class ButtonStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ButtonStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="type">Type of the button.</param>
    public ButtonStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, ButtonType type)
        : base(prefabId, position, outVoidConnections)
    {
        Type = type;
    }

    /// <summary>
    /// Gets the type of the button.
    /// </summary>
    /// <value>Type of the button.</value>
    public ButtonType Type { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
