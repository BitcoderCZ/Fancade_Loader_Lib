namespace FancadeLoaderLib.Editing.Utils
{
	public static class PrefabUtils
	{
		public static bool IsEmpty(this Prefab prefab)
		{
			if (prefab.Voxels is null)
				return true;

			for (int i = 0; i < prefab.Voxels.Length; i++)
				if (!prefab.Voxels[i].IsEmpty)
					return false;

			return true;
		}
	}
}
