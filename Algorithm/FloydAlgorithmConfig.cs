namespace Visual_FloydWarshall.Algorithm;

public sealed class FloydAlgorithmConfig(
    int collectionSize,
    int startVertex,
    int endVertex,
    long?[,] adjacencyMatrix,
    bool trackIterationChanges = true,
    string? algorithmName = null,
    string? description = null)
    : AlgorithmConfig(
        algorithmName ?? "Алгоритм Флойда-Уоршелла",
        description ?? "Поиск кратчайших путей между всеми парами вершин во взвешенном графе.",
        collectionSize)
{
    public int StartVertex { get; } = startVertex;
    public int EndVertex { get; } = endVertex;
    public long?[,] AdjacencyMatrix { get; } = adjacencyMatrix;
    public bool TrackIterationChanges { get; } = trackIterationChanges;
}
