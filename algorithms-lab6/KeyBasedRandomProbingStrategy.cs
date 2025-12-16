using System;

namespace algorithms_lab6;

public sealed class PseudoRandomProbingStrategy<K> : IProbingStrategy<K> {
    private readonly IHashStrategy<K> _hash;

    public PseudoRandomProbingStrategy(IHashStrategy<K> hashStrategy) {
        if (hashStrategy is null) {
            throw new ArgumentNullException(nameof(hashStrategy));
        }

        _hash = hashStrategy;
    }

    public int Index(K key, int i, int capacity) {
        if (capacity <= 0) {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (i < 0 || i >= capacity) {
            throw new ArgumentOutOfRangeException(nameof(i));
        }

        // h(k,i) = (h'(k) + step(k) * i) mod m
        // step(k) — детерминированный псевдослучайный шаг
        var baseIdx = _hash.Index(key, capacity);

        unchecked {
            var step = (baseIdx * 31 + 17) % capacity;
            if (step == 0) {
                step = 1;
            }

            return (baseIdx + step * i) % capacity;
        }
    }
}
