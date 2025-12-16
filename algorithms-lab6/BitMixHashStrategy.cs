using System;

namespace algorithms_lab6;

public sealed class BitMixHashStrategy : IHashStrategy<int> {
    public int Index(int key, int capacity) {
        if (capacity <= 0) {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        unchecked {
            int x = key;
            x ^= x >> 16;
            x *= 0x45d9f3b;
            x ^= x >> 16;
            x *= 0x45d9f3b;
            x ^= x >> 16;

            if (x < 0) {
                x = -x;
            }

            return x % capacity;
        }
    }
}
