namespace Visual_FloydWarshall.Algorithm;

public abstract class AlgorithmConfig(string algorithmName, string description, int collectionSize)
{
    public string AlgorithmName { get; } = algorithmName;
    public string Description { get; } = description;
    public int CollectionSize { get; } = collectionSize;
}
