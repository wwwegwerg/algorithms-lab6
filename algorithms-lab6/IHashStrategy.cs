namespace algorithms_lab6;

public interface IHashStrategy<K> {
    int Index(K key, int capacity);
}