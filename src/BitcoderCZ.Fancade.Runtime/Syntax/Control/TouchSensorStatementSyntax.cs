using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

/// <summary>
/// A <see cref="SyntaxNode"/> for the touch sensor prefab.
/// </summary>
public sealed class TouchSensorStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TouchSensorStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="state">State of the touch to detect.</param>
    /// <param name="fingerIndex">The finger whose touch should be detected.</param>
    public TouchSensorStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, TouchState state, int fingerIndex)
        : base(prefabId, position, outVoidConnections)
    {
        if (fingerIndex < 0 || fingerIndex > 2)
        {
            ThrowArgumentOutOfRangeException($"{nameof(fingerIndex)} must be between 0 and 2.", nameof(fingerIndex));
        }

        State = state;
        FingerIndex = fingerIndex;
    }

    /// <summary>
    /// Gets the state of the touch to detect.
    /// </summary>
    /// <value>State of the touch to detect.</value>
    public TouchState State { get; }

    /// <summary>
    /// Gets the finger whose touch should be detected.
    /// </summary>
    /// <value>The finger whose touch should be detected.</value>
    public int FingerIndex { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(3)];
}
