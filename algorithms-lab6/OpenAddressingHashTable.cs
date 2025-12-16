using System;
using System.Collections.Generic;

namespace algorithms_lab6;

public class OpenAddressingHashTable<K, V> {
    private readonly IProbingStrategy<K> _probe;
    private readonly HashTableEntry<K, V>?[] _entries;
    private readonly SlotState[] _states;

    public int Count { get; private set; }
    public int Capacity => _entries.Length;
    public double LoadFactor => (double)Count / Capacity;

    private enum SlotState : byte {
        Empty = 0,
        Occupied = 1,
        Deleted = 2
    }

    public OpenAddressingHashTable(IHashStrategy<K> hashStrategy, int capacity)
        : this(new LinearProbingStrategy<K>(hashStrategy), capacity) {
    }

    public OpenAddressingHashTable(IProbingStrategy<K> probingStrategy, int capacity) {
        if (probingStrategy is null) {
            throw new ArgumentNullException(nameof(probingStrategy));
        }

        if (capacity <= 0) {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _probe = probingStrategy;
        _entries = new HashTableEntry<K, V>?[capacity];
        _states = new SlotState[capacity];
    }

    public void AddOrUpdate(K key, V value) {
        var firstDeleted = -1;

        for (var i = 0; i < Capacity; i++) {
            var idx = _probe.Index(key, i, Capacity);
            var state = _states[idx];

            if (state == SlotState.Empty) {
                var insertIdx = firstDeleted >= 0 ? firstDeleted : idx;
                _entries[insertIdx] = new HashTableEntry<K, V>(key, value);
                _states[insertIdx] = SlotState.Occupied;
                Count++;
                return;
            }

            if (state == SlotState.Deleted) {
                if (firstDeleted < 0) {
                    firstDeleted = idx;
                }

                continue;
            }

            var e = _entries[idx];
            if (e != null && EqualityComparer<K>.Default.Equals(e.Key, key)) {
                e.Value = value;
                return;
            }
        }

        if (firstDeleted >= 0) {
            _entries[firstDeleted] = new HashTableEntry<K, V>(key, value);
            _states[firstDeleted] = SlotState.Occupied;
            Count++;
            return;
        }

        throw new InvalidOperationException("переполнение хеш-таблицы");
    }

    public bool Search(K key, out V value, out int comparisons) {
        comparisons = 0;

        for (var i = 0; i < Capacity; i++) {
            comparisons++;
            var idx = _probe.Index(key, i, Capacity);
            var state = _states[idx];

            if (state == SlotState.Empty) {
                value = default!;
                return false;
            }

            if (state != SlotState.Occupied) {
                continue;
            }

            var e = _entries[idx];
            if (e != null && EqualityComparer<K>.Default.Equals(e.Key, key)) {
                value = e.Value;
                return true;
            }
        }

        value = default!;
        return false;
    }

    public bool Remove(K key) {
        for (var i = 0; i < Capacity; i++) {
            var idx = _probe.Index(key, i, Capacity);
            var state = _states[idx];

            if (state == SlotState.Empty) {
                return false;
            }

            if (state != SlotState.Occupied) {
                continue;
            }

            var e = _entries[idx];
            if (e != null && EqualityComparer<K>.Default.Equals(e.Key, key)) {
                _entries[idx] = null;
                _states[idx] = SlotState.Deleted;
                Count--;
                return true;
            }
        }

        return false;
    }

    public int GetMaxClusterLength() {
        if (Count == 0) {
            return 0;
        }

        if (Count == Capacity) {
            return Capacity;
        }

        var start = -1;
        for (var i = 0; i < Capacity; i++) {
            if (_states[i] != SlotState.Occupied) {
                start = i;
                break;
            }
        }

        if (start < 0) {
            return Capacity;
        }

        var max = 0;
        var current = 0;

        for (var step = 1; step <= Capacity; step++) {
            var idx = (start + step) % Capacity;
            if (_states[idx] == SlotState.Occupied) {
                current++;
                if (current > max) {
                    max = current;
                }

                continue;
            }

            current = 0;
        }

        return max;
    }
}
