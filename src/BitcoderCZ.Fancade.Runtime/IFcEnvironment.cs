using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// Represents a fancade script environment, an instance of a block with scripts inside of it.
/// </summary>
public interface IFcEnvironment
{
    /// <summary>
    /// Gets the id of the prefab this <see cref="IFcEnvironment"/> instance represents.
    /// </summary>
    /// <value>Id of the prefab this <see cref="IFcEnvironment"/> instance represents.</value>
    ushort PrefabId { get; }

    /// <summary>
    /// Gets the environment's index.
    /// </summary>
    /// <value>The environment's index.</value>
    int Index { get; }

    /// <summary>
    /// Gets the index of the outer environment.
    /// </summary>
    /// <value>Index of the outer environment; or <c>-1</c>, if this is the outer-most environment.</value>
    int OuterEnvironmentIndex { get; }

    /// <summary>
    /// Gets the position of this environment in the outer environment.
    /// </summary>
    /// <value>Position of this environment in the outer environment; or <see cref="int3.Zero"/>, if this is the outer-most environment.</value>
    int3 OuterPosition { get; }
}