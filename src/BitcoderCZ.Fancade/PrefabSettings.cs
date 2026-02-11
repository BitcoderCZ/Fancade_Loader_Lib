// <copyright file="PrefabSettings.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Buffers;
using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Maths.Vectors;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static BitcoderCZ.Utils.ThrowHelper;
using SettingsCollection = BitcoderCZ.Buffers.ImmutableInlineArray<BitcoderCZ.Buffers.FixedArray2<BitcoderCZ.Fancade.PrefabSetting>, BitcoderCZ.Fancade.PrefabSetting>;

namespace BitcoderCZ.Fancade;

/// <summary>
/// An optimized collection of <see cref="PrefabSetting"/>s.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct PrefabSettings : IReadOnlyCollection<PrefabSetting>, IEquatable<PrefabSettings>
{
    /// <summary>
    /// An empty <see cref="PrefabSettings"/> instance.
    /// </summary>
    public static readonly PrefabSettings Empty = default;

    // a lot of blocks have only 1-2 setting, so don't allocate an array for them
    private readonly SettingsCollection _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabSettings"/> struct with a single item.
    /// </summary>
    /// <param name="setting">The item to be assigned to the <see cref="PrefabSettings"/>.</param>
    public PrefabSettings(PrefabSetting setting)
    {
        _settings = ImmutableInlineArray.Create<FixedArray2<PrefabSetting>, PrefabSetting>(setting);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabSettings"/> struct.
    /// </summary>
    /// <param name="settings">The collection whose elements are copied to the new <see cref="PrefabSettings"/>.</param>
    public PrefabSettings(params ReadOnlySpan<PrefabSetting> settings)
    {
        _settings = ImmutableInlineArray.Create<FixedArray2<PrefabSetting>, PrefabSetting>(settings);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabSettings"/> struct.
    /// </summary>
    /// <param name="settings">The collection whose elements are copied to the new <see cref="PrefabSettings"/>.</param>
    [OverloadResolutionPriority(-1)]
    public PrefabSettings(IEnumerable<PrefabSetting> settings)
    {
        if (settings is PrefabSettings ps)
        {
            _settings = ps._settings;
            return;
        }

        _settings = ImmutableInlineArray.CreateRange<FixedArray2<PrefabSetting>, PrefabSetting>(settings);
    }

    private PrefabSettings(IEnumerable<RawPrefabSetting> settings)
    {
        _settings = ImmutableInlineArray.CreateRange<FixedArray2<PrefabSetting>, PrefabSetting>(settings.Select(setting => new PrefabSetting(setting.Index, setting.Type, setting.Value)));
    }

    private PrefabSettings(SettingsCollection settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Gets a value indicating whether the <see cref="PrefabSettings"/> has any items.
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="PrefabSettings"/> contains 1 or more items; otherwise, <see langword="false"/>.</value>
    public bool Any => _settings.Count > 0;

    /// <summary>
    /// Gets the number of items in the <see cref="PrefabSettings"/>.
    /// </summary>
    /// <value>Number of items in the <see cref="PrefabSettings"/>.</value>
    public int Count => _settings.Count;

    /// <summary>Returns a value that indicates whether the 2 <see cref="PrefabSettings"/> are equal.</summary>
    /// <param name="left">The first <see cref="PrefabSettings"/> to compare.</param>
    /// <param name="right">The second <see cref="PrefabSettings"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(PrefabSettings left, PrefabSettings right)
        => left.Count == right.Count && left.SequenceEqual(right);

    /// <summary>Returns a value that indicates whether the 2 <see cref="PrefabSettings"/> are not equal.</summary>
    /// <param name="left">The first <see cref="PrefabSettings"/> to compare.</param>
    /// <param name="right">The second <see cref="PrefabSettings"/> to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(PrefabSettings left, PrefabSettings right)
        => !(left == right);

    /// <summary>
    /// Creates <see cref="PrefabSettings"/> from <see cref="RawPrefabSetting"/>s.
    /// </summary>
    /// <param name="settings">The <see cref="RawPrefabSetting"/>s to convert.</param>
    /// <returns>The converted <see cref="PrefabSettings"/>.</returns>
    public static PrefabSettings FromRaw(IEnumerable<RawPrefabSetting> settings)
        => new PrefabSettings(settings);

    /// <summary>
    /// Converts the <see cref="PrefabSettings"/> into <see cref="RawPrefabSetting"/>s.
    /// </summary>
    /// <param name="position">The position to assign to the <see cref="RawPrefabSetting"/>s.</param>
    /// <returns>The converted <see cref="RawPrefabSetting"/>s.</returns>
    public IEnumerable<RawPrefabSetting> ToRaw(int3 position)
    {
        // todo: custom struct enumerable
        foreach (var setting in _settings)
        {
            yield return new RawPrefabSetting(setting.Index, setting.Type, (ushort3)position, setting.Value);
        }
    }

    /// <summary>
    /// Gets if the <see cref="PrefabSettings"/> contains the specified setting.
    /// </summary>
    /// <param name="value">The setting to locate in the <see cref="PrefabSettings"/>.</param>
    /// <returns><see langword="true"/> if the <see cref="PrefabSettings"/> contains the setting; otherwise, <see langword="false"/>.</returns>
    public bool Contains(PrefabSetting value)
        => _settings.Contains(value);

    /// <summary>
    /// Creates a copy of the <see cref="PrefabSettings"/> with the specified value.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <returns>The new <see cref="PrefabSettings"/>.</returns>
    public PrefabSettings Add(PrefabSetting value)
        => new PrefabSettings(_settings.Add(value));

    /// <summary>
    /// Creates a copy of the <see cref="PrefabSettings"/> without the specified value.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <param name="removed">Indicates whether the value was found and removed.</param>
    /// <returns>The new <see cref="PrefabSettings"/>.</returns>
    public PrefabSettings Remove(PrefabSetting value, out bool removed)
        => new PrefabSettings(_settings.Remove(value, EqualityComparer<PrefabSetting>.Default, out removed));

    /// <summary>
    /// Attempts to get the value at the specified index as <typeparamref name="T"/>.
    /// <para>
    /// To get a <see cref="string"/> value, use <see cref="TryGetStringValue"/> or <see cref="TryGetTerminalName"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// If <typeparamref name="T"/> is not the corresponding <see cref="SettingType"/> of <see cref="Type"/>, the value is bitcasted to <typeparamref name="T"/>.
    /// </remarks>
    /// <param name="index">Index of the value to get.</param>
    /// <param name="value">The retreived value.</param>
    /// <typeparam name="T">The type to get the value as.</typeparam>
    /// <returns><see langword="true"/> if a numerical value was present at the specified index; otherwise, <see langword="false"/>.</returns>
    public bool TryGetNumericValue<T>(int index, out T value)
        where T : unmanaged
    {
        foreach (var setting in _settings)
        {
            if (setting.Index == index && setting.Value is not string)
            {
                value = setting.GetValue<T>();
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to get the string value at the specified index.
    /// <para>
    /// To get a terminal name, use <see cref="TryGetTerminalName"/>.
    /// To get a numeric value, use <see cref="TryGetNumericValue"/>.
    /// </para>
    /// </summary>
    /// <param name="index">Index of the value to get.</param>
    /// <param name="value">The retreived value.</param>
    /// <returns><see langword="true"/> if a string value was present at the specified index; otherwise, <see langword="false"/>.</returns>
    public bool TryGetStringValue(int index, [MaybeNullWhen(false)] out string value)
    {
        foreach (var setting in _settings)
        {
            if (setting.Index == index && setting.Type is SettingType.String)
            {
                value = setting.GetValueAsString()!;
                return true;
            }
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Attempts to get the terminal name at the specified index.
    /// <para>
    /// To get a string, use <see cref="TryGetStringValue"/>.
    /// To get a numeric value, use <see cref="TryGetNumericValue"/>.
    /// </para>
    /// </summary>
    /// <param name="index">Index of the value to get.</param>
    /// <param name="name">The retreived terminal name.</param>
    /// <param name="type">Type of the retreived setting.</param>
    /// <returns><see langword="true"/> if a string value was present at the specified index; otherwise, <see langword="false"/>.</returns>
    public bool TryGetTerminalName(int index, [MaybeNullWhen(false)] out string name, out SettingType type)
    {
        foreach (var setting in _settings)
        {
            if (setting.Index == index && setting.Type > SettingType.String)
            {
                name = setting.GetValueAsString()!;
                type = setting.Type;
                return true;
            }
        }

        name = null;
        type = default;
        return false;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public Enumerator GetEnumerator()
        => new Enumerator(_settings.GetEnumerator());

    /// <inheritdoc/>
    IEnumerator<PrefabSetting> IEnumerable<PrefabSetting>.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public bool Equals(PrefabSettings other)
        => this == other;

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Count);

        foreach (var setting in this)
        {
            hash.Add(setting);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is PrefabSettings settings && Equals(settings);

    /// <summary>
    /// A builder for <see cref="PrefabSettings"/>.
    /// </summary>
    public struct Builder
    {
        private SettingsCollection.Builder _builder;

        /// <summary>
        /// Initializes a new instance of the <see cref="Builder"/> struct.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the <see cref="Builder"/>.</param>
        public Builder(int initialCapacity)
        {
            _builder = new SettingsCollection.Builder(initialCapacity);
        }

        /// <summary>
        /// Adds a setting.
        /// </summary>
        /// <param name="value">The setting to add.</param>
        public void Add(PrefabSetting value)
            => _builder.Add(value);

        /// <summary>
        /// Builds the <see cref="PrefabSettings"/> from the contents of the <see cref="Builder"/> and clears the contents of the <see cref="Builder"/>.
        /// </summary>
        /// <returns>The built <see cref="PrefabSettings"/>.</returns>
        public PrefabSettings BuildAndClear()
            => new PrefabSettings(_builder.DrainToImmutable());

        /// <summary>
        /// Clears the contents of the <see cref="Builder"/>.
        /// </summary>
        public void Clear()
            => _builder.Clear();
    }

    /// <summary>
    /// <see cref="IEnumerator{T}"/> for <see cref="PrefabSettings"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<PrefabSetting>
    {
        private SettingsCollection.Enumerator _enumerator;

        internal Enumerator(SettingsCollection.Enumerator enumerator)
        {
            _enumerator = enumerator;
        }

        /// <inheritdoc/>
        public readonly PrefabSetting Current => _enumerator.Current;

        /// <inheritdoc/>
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
            => _enumerator.MoveNext();

        /// <inheritdoc/>
        public void Reset()
            => _enumerator.Reset();

        /// <inheritdoc/>
        public readonly void Dispose()
        {
        }
    }
}
