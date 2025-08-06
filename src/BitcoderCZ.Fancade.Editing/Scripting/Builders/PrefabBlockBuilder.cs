using BitcoderCZ.Maths.Vectors;
using System.Numerics;

namespace BitcoderCZ.Fancade.Editing.Scripting.Builders;

/// <summary>
/// <see cref="BlockBuilder"/> that writes to a <see cref="Prefab"/>.
/// </summary>
public sealed class PrefabBlockBuilder : BlockBuilder
{
    private readonly Prefab _prefab;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabBlockBuilder"/> class.
    /// </summary>
    /// <param name="prefab">The prefab in which the blocks should be placed.</param>
    public PrefabBlockBuilder(Prefab prefab)
    {
        _prefab = prefab;
    }

    /// <summary>
    /// Builds the blocks, connections and settings into the prefab passed to <see cref="PrefabBlockBuilder(Prefab)"/>.
    /// </summary>
    /// <param name="buildPos">The position at which blocks should be placed.</param>
    /// <returns>The prefab passed to <see cref="PrefabBlockBuilder(Prefab)"/>.</returns>
#if NET5_0_OR_GREATER
    public override Prefab Build(int3 buildPos)
#else
    public override object Build(int3 buildPos)
#endif
    {
        Block[] blocks = PreBuild(buildPos, false);

        for (int i = 0; i < blocks.Length; i++)
        {
            Block block = blocks[i];

            _prefab.Blocks.SetPrefab(block.Position, block.Type.Prefab);
        }

        for (int i = 0; i < settings.Count; i++)
        {
            SettingRecord set = settings[i];
            _prefab.Settings.Add((ushort3)set.Block.Position, new PrefabSetting(
                index: (byte)set.SettingIndex,
                type: set.Value switch
                {
                    byte => SettingType.Byte,
                    ushort => SettingType.Ushort,
                    float => SettingType.Float,
                    float3 or Vector3 => SettingType.Vec3,
                    Rotation => SettingType.Vec3,
                    string => SettingType.String,
                    _ => throw new InvalidDataException($"Unsupported type of value: '{set.Value.GetType()}'."),
                },
                pos: (ushort3)set.Block.Position,
                value: set.Value is Rotation rot ? rot.Value : set.Value));
        }

        for (int i = 0; i < connections.Count; i++)
        {
            ConnectionRecord con = connections[i];
            _prefab.Connections.Add(new Connection()
            {
                From = (ushort3)con.From.BlockPosition,
                FromVoxel = (ushort3)(con.From.VoxelPosition ?? ChooseTerminalVoxelPos(con.From.BlockPosition)),
                To = (ushort3)con.To.BlockPosition,
                ToVoxel = (ushort3)(con.To.VoxelPosition ?? ChooseTerminalVoxelPos(con.To.BlockPosition)),
            });
        }

        return _prefab;
    }
}
