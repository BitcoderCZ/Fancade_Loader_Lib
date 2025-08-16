// <copyright file="PlaceBlockHelper.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Partial;
using BitcoderCZ.Maths.Vectors;
using System.Diagnostics;
using System.Numerics;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Editing;

/// <summary>
/// Helper methods for placing blocks.
/// </summary>
public static partial class PlaceBlockHelper
{
    /// <summary>
    /// Places a literal block at the specified position.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Rotation"/> to place a rotation literal.
    /// </remarks>
    /// <param name="prefab">The prefab to set the block in.</param>
    /// <param name="pos">The position to place the value at.</param>
    /// <param name="value">The value to place.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not a valid fancade literal.</exception>
    public static void SetValue(this Prefab prefab, int3 pos, object value)
    {
        ThrowIfNull(prefab, nameof(prefab));

        PartialPrefab block;
        bool hasSetting = true;

        switch (value)
        {
            case bool b:
                block = b ? StockBlocks.Values.True.Prefab : StockBlocks.Values.False.Prefab;
                hasSetting = false;
                break;
            case float:
                block = StockBlocks.Values.Number.Prefab;
                break;
            case float3:
            case Vector3:
                block = StockBlocks.Values.Vector.Prefab;
                break;
            case Rotation:
                block = StockBlocks.Values.Rotation.Prefab;
                break;
            default:
                ThrowArgumentException($"{nameof(value)} is not a valid fancade literal.", nameof(value));
                return;
        }

        prefab.Blocks.SetPrefab(pos, block);

        if (hasSetting)
        {
            SettingType settingType;
            switch (value)
            {
                case float:
                    settingType = SettingType.Float;
                    break;
                case float3 or Vector3 or Rotation:
                    settingType = SettingType.Vec3;
                    break;
                default:
                    Debug.Fail("Unknown value type.");
                    return;
            }

            prefab.Settings[(ushort3)pos] = new PrefabSettings(new PrefabSetting(settingType, value));
        }
    }
}
