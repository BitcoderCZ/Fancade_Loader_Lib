namespace BitcoderCZ.Fancade.Runtime.Exceptions;

/// <summary>
/// A <see cref="FancadeException"/> thrown when a non-script stock prefab is executed.
/// </summary>
public sealed class InvalidNodePrefabIdException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidNodePrefabIdException"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab which was attempted to be executed.</param>
    public InvalidNodePrefabIdException(ushort prefabId)
        : base($"'{prefabId}' is not a valid script prefab id.")
    {
    }
}
