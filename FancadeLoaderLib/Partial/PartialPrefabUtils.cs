using FancadeLoaderLib.Raw;

namespace FancadeLoaderLib.Partial
{
	/// <summary>
	/// Util funcitons for <see cref="PartialPrefab"/>.
	/// </summary>
	public static class PartialPrefabUtils
	{
		public static PartialPrefab ToPartial(this Prefab prefab)
			=> new PartialPrefab(prefab);

		public static PartialPrefab ToPartial(this RawPrefab prefab)
			=> new PartialPrefab(prefab);
	}
}
