using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Control;

/// <summary>
/// A <see cref="SyntaxNode"/> for the joystick prefab.
/// </summary>
public sealed class JoystickStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JoystickStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="type">Type of the joystick.</param>
    public JoystickStatementSyntax(ushort prefabId, ushort3 position, ImmutableArray<Connection> outVoidConnections, JoystickType type)
        : base(prefabId, position, outVoidConnections)
    {
        Type = type;
    }

    /// <summary>
    /// Gets the type of the joystick.
    /// </summary>
    /// <value>Type of the joystick.</value>
    public JoystickType Type { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
