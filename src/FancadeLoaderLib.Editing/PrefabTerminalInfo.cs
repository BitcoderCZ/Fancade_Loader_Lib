using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace FancadeLoaderLib.Editing;

/// <summary>
/// Info about the terminals of a prefab.
/// </summary>
public readonly struct PrefabTerminalInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabTerminalInfo"/> struct.
    /// </summary>
    /// <param name="terminals">Terminals of the prefab.</param>
    public PrefabTerminalInfo(ImmutableArray<TerminalInfo> terminals)
    {
        Terminals = terminals;
        VoidTerminalCount = Terminals.Count(terminal => terminal.Type == SignalType.Void);
    }

    /// <summary>
    /// Gets the terminals of the prefab.
    /// </summary>
    /// <value>Terminals of the prefab.</value>
    public readonly ImmutableArray<TerminalInfo> Terminals { get; }

    /// <summary>
    /// Gets the amount of terminals of type <see cref="SignalType.Void"/>.
    /// </summary>
    /// <value>Amount of terminals of type <see cref="SignalType.Void"/>.</value>
    public readonly int VoidTerminalCount { get; }

    /// <summary>
    /// Gets the input terminals of the prefab.
    /// </summary>
    /// <value>Input terminals of the prefab.</value>
    public readonly IEnumerable<TerminalInfo> InputTerminals => Terminals.Where(info => info.IsInput);

    /// <summary>
    /// Gets the output terminals of the prefab.
    /// </summary>
    /// <value>Output terminals of the prefab.</value>
    public readonly IEnumerable<TerminalInfo> OutputTerminals => Terminals.Where(info => !info.IsInput);

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabTerminalInfo"/> struct from a <see cref="Prefab"/>.
    /// </summary>
    /// <param name="prefab">The <see cref="Prefab"/> to create the <see cref="PrefabTerminalInfo"/> from.</param>
    /// <returns>The <see cref="PrefabTerminalInfo"/> created from <paramref name="prefab"/>.</returns>
    public static PrefabTerminalInfo Create(Prefab prefab)
    {
        ImmutableArray<TerminalInfo>.Builder infoBuilder = ImmutableArray.CreateBuilder<TerminalInfo>(2);

        foreach (var (pos, settings) in prefab.Settings)
        {
            if ((pos.X | pos.Y | pos.Z) > byte.MaxValue)
            {
                continue;
            }

            foreach (var setting in settings)
            {
                if (setting.Type < SettingType.VoidTerminal)
                {
                    continue;
                }

                var (type, isInput) = SettingTypeUtils.ToTerminalSignalType(setting.Type);

                TerminalDirection dir = prefab.GetTerminalDirection((byte3)pos);

                // isInput always true for terminals of custom prefabs for... reasons???
                if (prefab.Id >= RawGame.CurrentNumbStockPrefabs)
                {
                    isInput = dir is TerminalDirection.PositiveZ or TerminalDirection.NegativeX; 
                }
                else
                {
                    if (type == SignalType.Void && dir is TerminalDirection.PositiveZ or TerminalDirection.NegativeX)
                    {
                        isInput = true;
                    }
                }

                infoBuilder.Add(new TerminalInfo((byte3)pos, type, dir, isInput));
            }
        }

        return new PrefabTerminalInfo(infoBuilder.DrainToImmutable());
    }

    /// <summary>
    /// Creates a <see cref="FrozenDictionary{TKey, TValue}"/> of prefab id to <see cref="PrefabTerminalInfo"/> for <see cref="IEnumerable{T}"/> of <see cref="Prefab"/>s.
    /// </summary>
    /// <param name="prefabs">The <see cref="Prefab"/>s to create the <see cref="FrozenDictionary{TKey, TValue}"/> from.</param>
    /// <returns>The <see cref="FrozenDictionary{TKey, TValue}"/> created from <paramref name="prefabs"/>.</returns>
    public static FrozenDictionary<ushort, PrefabTerminalInfo> Create(IEnumerable<Prefab> prefabs)
    {
        Dictionary<ushort, PrefabTerminalInfo> terminalInfos = prefabs.TryGetNonEnumeratedCount(out int count)
            ? new(count)
            : new();

        foreach (var prefab in prefabs)
        {
            terminalInfos.Add(prefab.Id, Create(prefab));
        }

        return terminalInfos.ToFrozenDictionary();
    }
}