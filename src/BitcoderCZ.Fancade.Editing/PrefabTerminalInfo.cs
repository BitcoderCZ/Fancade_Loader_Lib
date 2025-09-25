using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Editing;

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
    /// Initializes a new instance of the <see cref="PrefabTerminalInfo"/> struct from a <see cref="Prefab"/>, creating terminals only from <paramref name="prefab"/>'s setting, ignoring connections.
    /// </summary>
    /// <param name="prefab">The <see cref="Prefab"/> to create the <see cref="PrefabTerminalInfo"/> from.</param>
    /// <returns>The <see cref="PrefabTerminalInfo"/> created from <paramref name="prefab"/>.</returns>
    public static PrefabTerminalInfo CreateFromSettingsOnly(Prefab prefab)
    {
        ImmutableArray<TerminalInfo>.Builder infoBuilder = ImmutableArray.CreateBuilder<TerminalInfo>(2);

        foreach (var (pos, settings) in prefab.Settings)
        {
            if ((pos.X | pos.Y | pos.Z) > byte.MaxValue)
            {
                continue;
            }

            // TODO: only use index 0?
            foreach (var item in settings)
            {
                if (item is not { } setting)
                {
                    continue;
                }

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

                infoBuilder.Add(new TerminalInfo((byte3)pos, type, dir, isInput)
                {
                    Name = setting.Value as string,
                });
            }
        }

        return new PrefabTerminalInfo(infoBuilder.DrainToImmutable());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabTerminalInfo"/> struct from a <see cref="Prefab"/>.
    /// </summary>
    /// <param name="prefab">The <see cref="Prefab"/> to create the <see cref="PrefabTerminalInfo"/> from.</param>
    /// <param name="prefabList">A <see cref="PrefabList"/> used to resolve a terminal's type.</param>
    /// <returns>The <see cref="PrefabTerminalInfo"/> created from <paramref name="prefab"/>.</returns>
    public static PrefabTerminalInfo Create(Prefab prefab, PrefabList prefabList)
        => Create(prefab, id => prefabList.TryGetSegment(id, out var segment) && prefabList.TryGetPrefab(segment.PrefabId, out var prefab) ? prefab : null);

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabTerminalInfo"/> struct from a <see cref="Prefab"/>.
    /// </summary>
    /// <param name="prefab">The <see cref="Prefab"/> to create the <see cref="PrefabTerminalInfo"/> from.</param>
    /// <param name="getPrefab">Gets a <see cref="Prefab"/>, from a segment id.</param>
    /// <returns>The <see cref="PrefabTerminalInfo"/> created from <paramref name="prefab"/>.</returns>
    public static PrefabTerminalInfo Create(Prefab prefab, Func<ushort, Prefab?> getPrefab)
    {
        ImmutableArray<TerminalInfo>.Builder infoBuilder = ImmutableArray.CreateBuilder<TerminalInfo>(2);

        foreach (var (pos, settings) in prefab.Settings)
        {
            if ((pos.X | pos.Y | pos.Z) > byte.MaxValue)
            {
                continue;
            }

            // TODO: only use index 0?
            foreach (var item in settings)
            {
                if (item is not { } setting)
                {
                    continue;
                }

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

                infoBuilder.Add(new TerminalInfo((byte3)pos, type, dir, isInput)
                {
                    Name = setting.Value as string,
                });
            }
        }

        foreach (var connection in prefab.Connections)
        {
            if (connection.IsFromOutside && !infoBuilder.Any(terminal => terminal.Position == connection.FromVoxel))
            {
                var insidePrefab = getPrefab(prefab.Blocks.GetBlockOrDefault(connection.To));

                if (insidePrefab is not null)
                {
                    infoBuilder.Add(new TerminalInfo((byte3)connection.FromVoxel, ResolveBlockTerminalType(insidePrefab, (byte3)connection.ToVoxel, getPrefab), prefab.GetTerminalDirection((byte3)connection.FromVoxel), true));
                }
            }
            else if (connection.IsToOutside && !infoBuilder.Any(terminal => terminal.Position == connection.ToVoxel))
            {
                var insidePrefab = getPrefab(prefab.Blocks.GetBlockOrDefault(connection.From));

                if (insidePrefab is not null)
                {
                    infoBuilder.Add(new TerminalInfo((byte3)connection.ToVoxel, ResolveBlockTerminalType(insidePrefab, (byte3)connection.FromVoxel, getPrefab), prefab.GetTerminalDirection((byte3)connection.ToVoxel), true));
                }
            }
        }

        return new PrefabTerminalInfo(infoBuilder.DrainToImmutable());
    }

    /// <summary>
    /// Creates a <see cref="FrozenDictionary{TKey, TValue}"/> of prefab id to <see cref="PrefabTerminalInfo"/> for <see cref="IEnumerable{T}"/> of <see cref="Prefab"/>s.
    /// </summary>
    /// <param name="prefabs">The <see cref="Prefab"/>s to create the <see cref="FrozenDictionary{TKey, TValue}"/> from.</param>
    /// <returns>The <see cref="FrozenDictionary{TKey, TValue}"/> created from <paramref name="prefabs"/>.</returns>
    public static FrozenDictionary<ushort, PrefabTerminalInfo> Create(PrefabList prefabs)
    {
        Dictionary<ushort, PrefabTerminalInfo> terminalInfos = new(prefabs.PrefabCount);

        foreach (var prefab in prefabs)
        {
            terminalInfos.Add(prefab.Id, Create(prefab, prefabs));
        }

        return terminalInfos.ToFrozenDictionary();
    }

    /// <summary>
    /// Creates a <see cref="FrozenDictionary{TKey, TValue}"/> of prefab id to <see cref="PrefabTerminalInfo"/> for <see cref="IEnumerable{T}"/> of <see cref="Prefab"/>s.
    /// </summary>
    /// <param name="prefabs">The <see cref="Prefab"/>s to create the <see cref="FrozenDictionary{TKey, TValue}"/> from.</param>
    /// <param name="getPrefab">Gets a <see cref="Prefab"/>, from a segment id.</param>
    /// <returns>The <see cref="FrozenDictionary{TKey, TValue}"/> created from <paramref name="prefabs"/>.</returns>
    public static FrozenDictionary<ushort, PrefabTerminalInfo> Create(IEnumerable<Prefab> prefabs, Func<ushort, Prefab?> getPrefab)
    {
        Dictionary<ushort, PrefabTerminalInfo> terminalInfos = prefabs.TryGetNonEnumeratedCount(out int count)
            ? new(count)
            : new();

        foreach (var prefab in prefabs)
        {
            terminalInfos.Add(prefab.Id, Create(prefab, getPrefab));
        }

        return terminalInfos.ToFrozenDictionary();
    }

    private static SignalType ResolveBlockTerminalType(Prefab prefab, byte3 terminalPos, Func<ushort, Prefab?> getPrefab, int depth = 0)
    {
        if (depth > 6)
        {
            return SignalType.Error;
        }

        if (prefab.Settings.TryGetValue(terminalPos, out var settings))
        {
            // TODO: only use index 0?
            foreach (var item in settings)
            {
                if (item is not { } setting)
                {
                    continue;
                }

                if (setting.Type < SettingType.VoidTerminal)
                {
                    continue;
                }

                return SettingTypeUtils.ToTerminalSignalType(setting.Type).SignalType;
            }
        }

        // TODO: incorrectly identifies object connections to self as terminals
        foreach (var connection in prefab.Connections)
        {
            if (connection.IsFromOutside && connection.FromVoxel == terminalPos)
            {
                var insidePrefab = getPrefab(prefab.Blocks.GetBlockOrDefault(connection.To));

                if (insidePrefab is not null)
                {
                    return ResolveBlockTerminalType(insidePrefab, (byte3)connection.ToVoxel, getPrefab, depth + 1);
                }
            }
            else if (connection.IsToOutside && connection.ToVoxel == terminalPos)
            {
                var insidePrefab = getPrefab(prefab.Blocks.GetBlockOrDefault(connection.From));

                if (insidePrefab is not null)
                {
                    return ResolveBlockTerminalType(insidePrefab, (byte3)connection.FromVoxel, getPrefab, depth + 1);
                }
            }
        }

        return SignalType.Error;
    }
}