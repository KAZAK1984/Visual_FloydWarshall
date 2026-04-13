namespace Visual_FloydWarshall.Algorithm
{
    public sealed class FloydWarshallResult(long[,] distances, int[,] predecessors, bool hasNegativeCycle, IReadOnlyList<FloydWarshallIterationLog> iterationLogs)
	{
		public long[,] Distances { get; } = distances;
		public int[,] Predecessors { get; } = predecessors;
		public bool HasNegativeCycle { get; } = hasNegativeCycle;
		public IReadOnlyList<FloydWarshallIterationLog> IterationLogs { get; } = iterationLogs;

		public IReadOnlyList<int> RestorePath(int startVertex, int endVertex)
        {
            var vertexCount = Distances.GetLength(0);

            if (startVertex < 0 || startVertex >= vertexCount)
                throw new ArgumentOutOfRangeException(nameof(startVertex));

            if (endVertex < 0 || endVertex >= vertexCount)
                throw new ArgumentOutOfRangeException(nameof(endVertex));

            if (startVertex == endVertex)
				return [startVertex];

            if (Predecessors[startVertex, endVertex] == -1)
				return [];

            var path = new List<int>(vertexCount) { endVertex };
            var current = endVertex;

            while (current != startVertex)
            {
                current = Predecessors[startVertex, current];

                if (current == -1)
                    return [];

                path.Add(current);

                if (path.Count > vertexCount)
                    throw new InvalidOperationException("Path restoration failed due to inconsistent predecessor matrix.");
            }

            path.Reverse();
            return path;
        }
    }
}
