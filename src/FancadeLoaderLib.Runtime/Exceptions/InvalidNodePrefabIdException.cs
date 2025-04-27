namespace FancadeLoaderLib.Runtime.Exceptions;

public sealed class InvalidNodePrefabIdException : Exception
{
    public InvalidNodePrefabIdException(ushort prefabId)
        : base($"'{prefabId}' is not a valid script prefab id.")
    {
    }
}
