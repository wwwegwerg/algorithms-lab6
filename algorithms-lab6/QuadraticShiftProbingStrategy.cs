using System;

namespace algorithms_lab6;

public sealed class QuadraticShiftProbingStrategy<K> : IProbingStrategy<K> {
    private readonly IHashStrategy<K> _hash;
    private readonly int _shift;

    public QuadraticShiftProbingStrategy(
        IHashStrategy<K> hashStrategy,
        int shift = 7
    ) {
        if (hashStrategy is null) {
            throw new ArgumentNullException(nameof(hashStrategy));
        }

        _hash = hashStrategy;
        _shift = shift;
    }

    public int Index(K key, int i, int capacity) {
        if (capacity <= 0) {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (i < 0 || i >= capacity) {
            throw new ArgumentOutOfRangeException(nameof(i));
        }

        // h(k,i) = (h'(k) + i^2 + shift * i) mod m
        var baseIdx = _hash.Index(key, capacity);
        return (baseIdx + i * i + _shift * i) % capacity;
    }
}
