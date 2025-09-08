using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// An <see cref="IFcEnvironment"/> implementation.
/// </summary>
public sealed class FcEnvironment : IFcEnvironment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FcEnvironment"/> class.
    /// </summary>
    /// <param name="ast">The <see cref="FcAST"/> representing the environment.</param>
    /// <param name="index">The environment's index.</param>
    /// <param name="outerEnvironmentIndex">Index of the outer environment; or <c>-1</c>, if this is the outer-most environment.</param>
    /// <param name="outerPosition">Position of this environment in the outer environment; or <see cref="int3.Zero"/>, if this is the outer-most environment.</param>
    public FcEnvironment(FcAST ast, int index, int outerEnvironmentIndex, int3 outerPosition)
    {
        Index = index;
        OuterEnvironmentIndex = outerEnvironmentIndex;
        AST = ast;
        OuterPosition = outerPosition;
    }

    /// <summary>
    /// Gets the <see cref="FcAST"/> representing the environment.
    /// </summary>
    /// <value>The <see cref="FcAST"/> representing the environment.</value>
    public FcAST AST { get; }

    /// <inheritdoc/>
    public int Index { get; }

    /// <inheritdoc/>
    public int OuterEnvironmentIndex { get; }

    /// <inheritdoc/>
    public int3 OuterPosition { get; }

    /// <summary>
    /// Gets a <see cref="Dictionary{TKey, TValue}"/> for storing a prefab's data.
    /// </summary>
    /// <value>A <see cref="Dictionary{TKey, TValue}"/> for storing a prefab's data.</value>
    public Dictionary<int3, object> BlockData { get; } = [];

    /// <summary>
    /// Gets the id of the prefab this <see cref="FcEnvironment"/> represents.
    /// </summary>
    /// <value>Id of the prefab this <see cref="FcEnvironment"/> represents.</value>
    public ushort PrefabId => AST.PrefabId;
}