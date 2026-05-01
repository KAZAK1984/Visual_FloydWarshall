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
		description ?? "Алгоритм Флойда-Уоршелла — это динамический алгоритм для поиска кратчайших путей между всеми парами вершин взвешенного графа.\n" +
					   "Он работает с графами, содержащими отрицательные рёбра (но без отрицательных циклов), и имеет временную сложность O(V³), " +
					   "где V — количество вершин. Алгоритм последовательно улучшает оценки расстояний, используя промежуточные вершины.\n" +
					   "Результатом является матрица кратчайших расстояний между всеми парами вершин и матрица предшественников для восстановления путей.\n" +
					   "Алгоритм также может обнаруживать отрицательные циклы в графе.",
		collectionSize)
{
	public int StartVertex { get; } = startVertex;
	public int EndVertex { get; } = endVertex;
	public long?[,] AdjacencyMatrix { get; } = adjacencyMatrix;
	public bool TrackIterationChanges { get; } = trackIterationChanges;
}
