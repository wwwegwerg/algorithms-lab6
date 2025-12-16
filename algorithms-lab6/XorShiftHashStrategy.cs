using System;

namespace algorithms_lab6;

public sealed class XorShiftHashStrategy : IHashStrategy<int> {
    public int Index(int key, int capacity) {
        if (capacity <= 0) {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        unchecked {
            uint x = (uint)key;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;

            return (int)(x % capacity);
        }
    }
}
