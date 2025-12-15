namespace algorithms_lab6;

public sealed class HashTableEntry<K, V> {
    public K Key { get; }
    public V Value { get; set; }

    public HashTableEntry(K key, V value) {
        Key = key;
        Value = value;
    }
}