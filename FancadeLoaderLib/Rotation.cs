using MathUtils.Vectors;

namespace FancadeLoaderLib;

/// <summary>
/// Wrapper over <see cref="float3"/> to represent rotation.
/// </summary>
public struct Rotation
{
	/// <summary>
	/// The value of this rotation.
	/// </summary>
	public float3 Value;

	/// <summary>
	/// Initializes a new instance of the <see cref="Rotation"/> struct.
	/// </summary>
	/// <param name="value">Value of this rotation.</param>
	public Rotation(float3 value)
	{
		Value = value;
	}

	public static explicit operator float3(Rotation a)
		=> a.Value;

	public static explicit operator Rotation(float3 a)
		=> new Rotation(a);
}
