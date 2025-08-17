namespace BitcoderCZ.Fancade;

/// <summary>
/// Represents a face of a voxel.
/// </summary>
public readonly struct VoxelFace
{
    private readonly byte _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoxelFace"/> struct.
    /// </summary>
    /// <param name="data">The raw face data.</param>
    public VoxelFace(byte data)
    {
        _data = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VoxelFace"/> struct.
    /// </summary>
    /// <param name="color">Color of the <see cref="VoxelFace"/>.</param>
    /// <param name="hasGlue"><see langword="true"/> if the <see cref="VoxelFace"/> has "glue"; otherwise, <see langword="false"/>.</param>
    public VoxelFace(FcColor color, bool hasGlue)
    {
        _data = (byte)((int)color | (hasGlue ? 0 : 0b_1000_0000));
    }

    /// <summary>
    /// Gets a value indicating whether the <see cref="VoxelFace"/> is empty.
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="VoxelFace"/> is empty; otherwise, <see langword="false"/>.</value>
    public bool IsEmpty => _data == 0;

    /// <summary>
    /// Gets the color of the <see cref="VoxelFace"/>.
    /// </summary>
    /// <value>Color of the <see cref="VoxelFace"/>.</value>
    public FcColor Color
    {
        get => (FcColor)(_data & 0b_0111_1111);
        init => _data = (byte)((_data & 0b_1000_0000) | (int)value);
    }

    /// <summary>
    /// Gets a value indicating whether the <see cref="VoxelFace"/> has "glue".
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="VoxelFace"/> has "glue"; otherwise, <see langword="false"/>.</value>
    public bool HasGlue
    {
        get => (_data & 0b_1000_0000) == 0;
        init => _data = (byte)((_data & 0b_0111_1111) | (value ? 0 : 0b_1000_0000));
    }

    /// <summary>
    /// Converts a <see cref="VoxelFace"/> to it's raw data.
    /// </summary>
    /// <param name="value">The <see cref="VoxelFace"/> to convert.</param>
    public static implicit operator byte(VoxelFace value)
        => value._data;

    /// <summary>
    /// Converts the raw face data to <see cref="VoxelFace"/>.
    /// </summary>
    /// <param name="value">The data to convert.</param>
    public static explicit operator VoxelFace(byte value)
        => new VoxelFace(value);
}
