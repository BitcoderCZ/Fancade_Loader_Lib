using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Exceptions;

/// <summary>
/// The exception that is thrown when a block cannot be placed  because its position is obstructed.
/// </summary>
public sealed class BlockObstructedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockObstructedException"/> class.
    /// </summary>
    /// <param name="prefabName">Name of the prefab the obstruction was found in.</param>
    /// <param name="prefabPosition">Position of the prefab that is obstructed.</param>
    /// <param name="obstructedPosition">Position of the obstruction.</param>
    public BlockObstructedException(string prefabName, int3 prefabPosition, int3 obstructedPosition)
        : this(prefabName, prefabPosition, obstructedPosition, $"Cannot place block because its position is obstructed in prefab '{prefabName}' at position '{prefabPosition}'.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockObstructedException"/> class.
    /// </summary>
    /// <param name="prefabName">Name of the prefab the obstruction was found in.</param>
    /// <param name="prefabPosition">Position of the prefab that is obstructed.</param>
    /// <param name="obstructedPosition">Position of the obstruction.</param>
    /// <param name="message">The message that describes the error.</param>
    public BlockObstructedException(string prefabName, int3 prefabPosition, int3 obstructedPosition, string message)
        : base(message)
    {
        PrefabName = prefabName;
        PrefabPosition = prefabPosition;
        ObstructedPosition = obstructedPosition;
    }

    internal BlockObstructedException(BlockObstructionInfo info)
        : this(info, $"Cannot place block because its position is obstructed in prefab '{info.PrefabName}' at position '{info.PrefabPosition}'.")
    {
    }

    internal BlockObstructedException(BlockObstructionInfo info, string message)
        : this(info.PrefabName, info.PrefabPosition, info.ObstructedPosition, message)
    {
    }

    /// <summary>
    /// Gets the name of the prefab the obstruction was found in.
    /// </summary>
    /// <value>The name of the prefab the obstruction was found in.</value>
    public string PrefabName { get; }

    /// <summary>
    /// Gets the position of the prefab that is obstructed.
    /// </summary>
    /// <value>The position of the prefab that is obstructed.</value>
    public int3 PrefabPosition { get; }

    /// <summary>
    /// Gets the position of the obstruction.
    /// </summary>
    /// <value>The position of the obstruction.</value>
    public int3 ObstructedPosition { get; }
}
