using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Maths.Vectors;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade;

/// <summary>
/// An optimized collection of <see cref="PrefabSetting"/>s.
/// </summary>
public readonly struct PrefabSettings : IEnumerable<PrefabSetting?>, IEquatable<PrefabSettings>
{
    /// <summary>
    /// An empty <see cref="PrefabSettings"/> instance.
    /// </summary>
    public static readonly PrefabSettings Empty = default;

    // a lot of blocks have only 1 setting, so don't allocate an array for them
    private readonly PrefabSetting? _firstSetting;

    private readonly PrefabSetting?[]? _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabSettings"/> struct with a single item.
    /// </summary>
    /// <param name="setting">The item to be assigned to the <see cref="PrefabSettings"/>.</param>
    public PrefabSettings(PrefabSetting setting)
    {
        _firstSetting = setting;
        _settings = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabSettings"/> struct.
    /// </summary>
    /// <param name="settings">The collection whose elements are copied to the new <see cref="PrefabSettings"/>.</param>
    public PrefabSettings(IEnumerable<PrefabSetting?> settings)
    {
        if (settings is PrefabSettings ps)
        {
            _firstSetting = ps._firstSetting;
            _settings = (PrefabSetting?[]?)ps._settings?.Clone();
            return;
        }

        // TODO: most likely will be an array, but still could probably be optimized
        int count = settings.Count();

        if (count == 0)
        {
            _firstSetting = null;
            _settings = null;
        }
        else if (count == 1)
        {
            _firstSetting = settings.First();
            _settings = null;
        }
        else
        {
            _firstSetting = default;

            _settings = new PrefabSetting?[count - 1];

            int index = -1;
            foreach (var setting in settings)
            {
                if (index == -1)
                {
                    _firstSetting = setting;
                }
                else
                {
                    _settings[index] = setting;
                }

                index++;
            }
        }
    }

    private PrefabSettings(IEnumerable<RawPrefabSetting> settings)
    {
        _firstSetting = default;
        _settings = null;

        foreach (var setting in settings)
        {
            if (setting.Index == 0)
            {
                _firstSetting = new(setting.Type, setting.Value);
                continue;
            }

            int index = setting.Index - 1;

            if (_settings is null)
            {
                _settings = new PrefabSetting?[index + 1];
            }
            else if (_settings.Length < index + 1)
            {
                Array.Resize(ref _settings, index + 1);
            }

            _settings[index] = new(setting.Type, setting.Value);
        }
    }

    private PrefabSettings(PrefabSetting? firstSetting, PrefabSetting?[]? settings)
    {
        _firstSetting = firstSetting;
        _settings = settings;
    }

    /// <summary>
    /// Gets a value indicating whether the <see cref="PrefabSettings"/> has any items.
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="PrefabSettings"/> contains 1 or more items; otherwise, <see langword="false"/>.</value>
    public bool Any => _firstSetting is not null || _settings is not null;

    /// <summary>
    /// Gets the number of items in the <see cref="PrefabSettings"/>.
    /// </summary>
    /// <value>Number of items in the <see cref="PrefabSettings"/>.</value>
    public int Count => _settings is null
        ? _firstSetting is null
            ? 0
            : 1
        : _settings.Length + 1;

    /// <summary>
    /// Gets the <see cref="PrefabSetting"/> at the specified index.
    /// </summary>
    /// <param name="index">Index of the <see cref="PrefabSetting"/>.</param>
    /// <returns>The <see cref="PrefabSetting"/> at <paramref name="index"/>; or <see langword="null"/> if <paramref name="index"/> is out of bounds or no setting is at <paramref name="index"/>.</returns>
    public PrefabSetting? this[int index]
    {
        get
        {
            if (index < 0)
            {
                return null;
            }

            if (index == 0)
            {
                return _firstSetting;
            }
            else if (_settings is not null && index - 1 < _settings.Length)
            {
                return _settings[index - 1];
            }

            return null;
        }
    }

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
    public IEnumerable<RawPrefabSetting> ToRaw(ushort3 position)
    {
        if (!Any)
        {
            yield break;
        }

        if (_firstSetting is { } fistSetting)
        {
            yield return new RawPrefabSetting(0, fistSetting.Type, position, fistSetting.Value);
        }

        if (_settings is not null)
        {
            for (int i = 0; i < _settings.Length; i++)
            {
                var item = _settings[i];
                if (item is { } setting)
                {
                    yield return new RawPrefabSetting((byte)(i + 1), setting.Type, position, setting.Value);
                }
            }
        }
    }

    /// <summary>
    /// Creates a copy of the <see cref="PrefabSettings"/> with the specified value at the specified index.
    /// </summary>
    /// <param name="index">Index to set <paramref name="value"/> at.</param>
    /// <param name="value">The new value.</param>
    /// <returns>The new <see cref="PrefabSettings"/>.</returns>
    public PrefabSettings WithValueAt(int index, PrefabSetting value)
    {
        if (index < 0)
        {
            ThrowArgumentOutOfRangeException(nameof(index), $"nameof(index) must be non-negative.");
        }

        int count = Count;
        if (count == 0)
        {
            if (index == 0)
            {
                return new PrefabSettings(value);
            }
            else
            {
                var settings = new PrefabSetting?[index];
                settings[index - 1] = value;
                return new PrefabSettings(null, settings);
            }
        }

        if (index == 0)
        {
            return new PrefabSettings(value, (PrefabSetting?[]?)_settings?.Clone());
        }
        else if (_settings is null)
        {
            var settings = new PrefabSetting?[index];
            settings[index - 1] = value;
            return new PrefabSettings(_firstSetting, settings);
        }
        else
        {
            var settings = new PrefabSetting?[Math.Max(index, _settings.Length)];
            _settings.AsSpan().CopyTo(settings);
            settings[index - 1] = value;
            return new PrefabSettings(_firstSetting, settings);
        }
    }

    /// <summary>
    /// Creates a copy of the <see cref="PrefabSettings"/> with the value at the specified index removed.
    /// </summary>
    /// <param name="index">Index to remove the value at.</param>
    /// <returns>The new <see cref="PrefabSettings"/>.</returns>
    public PrefabSettings WithoutValueAt(int index)
    {
        if (index < 0)
        {
            ThrowArgumentOutOfRangeException(nameof(index), $"nameof(index) must be non-negative.");
        }

        int count = Count;
        if (count == 0 || index >= count)
        {
            return this; // nothing to remove
        }

        if (index == 0)
        {
            return new PrefabSettings(null, (PrefabSetting?[]?)_settings?.Clone());
        }
        else if (_settings is null)
        {
            return this; // nothing to remove
        }
        else if (index == _settings.Length)
        {
            int newArrayLength;
            for (newArrayLength = _settings.Length - 2; newArrayLength >= 0; newArrayLength--)
            {
                if (_settings[newArrayLength] is not null)
                {
                    break;
                }
            }

            var settings = new PrefabSetting?[newArrayLength + 1];
            _settings.AsSpan(0, settings.Length).CopyTo(settings);

            return new PrefabSettings(_firstSetting, settings);
        }
        else
        {
            var settings = new PrefabSetting?[_settings.Length];
            for (int i = 0; i < _settings.Length; i++)
            {
                if (i != index - 1)
                {
                    settings[i] = _settings[i];
                }
            }

            return new PrefabSettings(_firstSetting, settings);
        }
    }

    /// <summary>
    /// Gets if the <see cref="PrefabSettings"/> contains a setting at the specified index.
    /// </summary>
    /// <param name="index">The index to get the presence of a <see cref="PrefabSetting"/> of.</param>
    /// <returns><see langword="true"/> if a <see cref="PrefabSetting"/> exists at <paramref name="index"/>; otherwise, <see langword="false"/>.</returns>
    public bool Contains(int index)
    {
        if (index < 0)
        {
            return false;
        }

        if (index == 0)
        {
            return _firstSetting is not null;
        }
        else if (_settings is not null && index - 1 < _settings.Length)
        {
            return _settings[index - 1] is not null;
        }

        return false;
    }

    /// <summary>
    /// Attempts to retrieve the setting at the specified index.
    /// </summary>
    /// <param name="index">The index of the setting to retrieve.</param>
    /// <param name="setting">The setting at the specified index.</param>
    /// <returns><see langword="true"/> if a setting exists at the specified index; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue(int index, out PrefabSetting setting)
    {
        var item = this[index];

        if (item is not null)
        {
            setting = item.Value;
            return true;
        }

        setting = default;
        return false;
    }

    /// <inheritdoc/>
    public IEnumerator<PrefabSetting?> GetEnumerator()
        => new Enumerator(this);

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
        private PrefabSetting? _firstSetting;

        private PrefabSetting?[]? _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="Builder"/> struct.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the <see cref="Builder"/>.</param>
        public Builder(int initialCapacity)
        {
            if (initialCapacity < 0)
            {
                ThrowArgumentOutOfRangeException(nameof(initialCapacity), $"{nameof(initialCapacity)} must be non-negative.");
            }

            if (initialCapacity > 1)
            {
                _settings = new PrefabSetting?[initialCapacity - 1];
            }
        }

        /// <summary>
        /// Sets the setting at the specified index.
        /// </summary>
        /// <param name="index">Index of the setting to set.</param>
        /// <param name="value">The new setting.</param>
        public void SetValueAt(int index, PrefabSetting value)
        {
            if (index < 0)
            {
                ThrowArgumentOutOfRangeException(nameof(index), $"{nameof(index)} must be non-negative.");
            }

            if (index == 0)
            {
                _firstSetting = value;
                return;
            }

            int settingsIndex = index - 1;
            if (_settings is null)
            {
                _settings = new PrefabSetting?[settingsIndex + 1];
            }
            else if (_settings.Length <= settingsIndex)
            {
                Array.Resize(ref _settings, settingsIndex + 1);
            }

            _settings[settingsIndex] = value;
        }

        /// <summary>
        /// Builds the <see cref="PrefabSettings"/> from the contents of the <see cref="Builder"/> and clears the contents of the <see cref="Builder"/>.
        /// </summary>
        /// <returns>The built <see cref="PrefabSettings"/>.</returns>
        public PrefabSettings BuildAndClear()
        {
            var settings = new PrefabSettings(_firstSetting, _settings);
            Clear();
            return settings;
        }

        /// <summary>
        /// Clears the contents of the <see cref="Builder"/>.
        /// </summary>
        public void Clear()
        {
            _firstSetting = null;
            _settings = null;
        }
    }

    private struct Enumerator : IEnumerator<PrefabSetting?>
    {
        private readonly PrefabSettings _settings;
        private int _index = -2;
        private PrefabSetting? _value;

        public Enumerator(PrefabSettings settings)
        {
            _settings = settings;
        }

        /// <inheritdoc/>
        public readonly PrefabSetting? Current => _value;

        /// <inheritdoc/>
        readonly object? IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            _index++;

            if (_index == -1)
            {
                _value = _settings._firstSetting;
                return _value is not null || _settings._settings is not null;
            }

            if (_settings._settings is not null && _index < _settings._settings.Length)
            {
                _value = _settings._settings[_index];
                return true;
            }

            _value = null;
            return false;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _index = -2;
            _value = null;
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
        }
    }
}
