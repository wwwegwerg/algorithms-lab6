using System;

namespace algorithms_lab6;

public sealed class FnvLikeHashStrategy : IHashStrategy<int> {
    public int Index(int key, int capacity) {
        if (capacity <= 0) {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        unchecked {
            uint hash = 2166136261;
            const uint prime = 16777619;

            hash ^= (uint)key;
            hash *= prime;

            return (int)(hash % capacity);
        }
    }
}
