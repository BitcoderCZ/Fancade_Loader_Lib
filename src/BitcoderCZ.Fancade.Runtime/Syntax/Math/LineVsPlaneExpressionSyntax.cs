using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

/// <summary>
/// A <see cref="SyntaxNode"/> for the line vs plane prefab.
/// </summary>
public sealed class LineVsPlaneExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineVsPlaneExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="lineFrom">The line from terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="lineTo">The line to terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="planePoint">The plane point terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="planeNormal">The plane normal terminal; or <see langword="null"/>, if it is not connected.</param>
    public LineVsPlaneExpressionSyntax(ushort prefabId, ushort3 position, SyntaxTerminal? lineFrom, SyntaxTerminal? lineTo, SyntaxTerminal? planePoint, SyntaxTerminal? planeNormal)
        : base(prefabId, position)
    {
        LineFrom = lineFrom;
        LineTo = lineTo;
        PlanePoint = planePoint;
        PlaneNormal = planeNormal;
    }

    /// <summary>
    /// Gets the line from terminal.
    /// </summary>
    /// <value>The line from terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? LineFrom { get; }

    /// <summary>
    /// Gets the line to  terminal.
    /// </summary>
    /// <value>The line to  terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? LineTo { get; }

    /// <summary>
    /// Gets the plane point terminal.
    /// </summary>
    /// <value>The plane point terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? PlanePoint { get; }

    /// <summary>
    /// Gets the plane normal terminal.
    /// </summary>
    /// <value>The plane normal terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? PlaneNormal { get; }
}
