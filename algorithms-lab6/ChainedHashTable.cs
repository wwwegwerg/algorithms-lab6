using System;
using System.Collections.Generic;

namespace algorithms_lab6;

public class ChainedHashTable<K, V> {
    private readonly IHashStrategy<K> _hash;
    private List<HashTableEntry<K, V>>[] _buckets;

    public int Count { get; private set; }
    public int Capacity => _buckets.Length;
    public double MaxLoadFactor { get; }

    public ChainedHashTable(IHashStrategy<K> hashStrategy, int capacity = 16, double maxLoadFactor = 0.75) {
        if (hashStrategy is null) {
            throw new ArgumentNullException(nameof(hashStrategy));
        }

        if (capacity <= 0) {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (maxLoadFactor <= 0 || maxLoadFactor >= 1) {
            throw new ArgumentOutOfRangeException(nameof(maxLoadFactor));
        }

        _hash = hashStrategy;
        _buckets = new List<HashTableEntry<K, V>>[capacity];
        MaxLoadFactor = maxLoadFactor;
    }

    public void AddOrUpdate(K key, V value) {
        if (NeedsResize(Count + 1)) {
            Resize(Capacity * 2);
        }

        var idx = _hash.Index(key, Capacity);
        var bucket = _buckets[idx] ??= [];

        for (var i = 0; i < bucket.Count; i++) {
            if (EqualityComparer<K>.Default.Equals(bucket[i].Key, key)) {
                bucket[i].Value = value;
                return;
            }
        }

        bucket.Add(new HashTableEntry<K, V>(key, value));
        Count++;
    }

    public bool TryGetValue(K key, out V value) {
        var idx = _hash.Index(key, Capacity);
        var bucket = _buckets[idx];

        if (bucket != null) {
            for (var i = 0; i < bucket.Count; i++) {
                if (EqualityComparer<K>.Default.Equals(bucket[i].Key, key)) {
                    value = bucket[i].Value;
                    return true;
                }
            }
        }

        value = default!;
        return false;
    }

    public bool Remove(K key) {
        var idx = _hash.Index(key, Capacity);
        var bucket = _buckets[idx];
        if (bucket == null) {
            return false;
        }

        for (var i = 0; i < bucket.Count; i++) {
            if (EqualityComparer<K>.Default.Equals(bucket[i].Key, key)) {
                bucket.RemoveAt(i);
                Count--;
                return true;
            }
        }

        return false;
    }

    private bool NeedsResize(int newCount) {
        return (double)newCount / Capacity > MaxLoadFactor;
    }

    private void Resize(int newCapacity) {
        var old = _buckets;
        _buckets = new List<HashTableEntry<K, V>>[newCapacity];
        Count = 0;

        for (var i = 0; i < old.Length; i++) {
            var bucket = old[i];
            if (bucket == null) {
                continue;
            }

            for (var j = 0; j < bucket.Count; j++) {
                AddOrUpdate(bucket[j].Key, bucket[j].Value);
            }
        }
    }
}