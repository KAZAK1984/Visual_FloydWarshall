namespace Visual_FloydWarshall.Algorithm
{
    public static class FloydWarshallSolver
    {
        public const long Inf = long.MaxValue / 2;

        public static FloydWarshallResult Solve(long?[,] adjacencyMatrix, bool trackIterationChanges = false)
        {
            ArgumentNullException.ThrowIfNull(adjacencyMatrix);

            var vertexCount = adjacencyMatrix.GetLength(0);
            if (vertexCount != adjacencyMatrix.GetLength(1))
                throw new ArgumentException("Adjacency matrix must be square.", nameof(adjacencyMatrix));

            var distances = new long[vertexCount, vertexCount];
            var predecessors = new int[vertexCount, vertexCount];

            InitializeMatrices(adjacencyMatrix, distances, predecessors);

            var logs = trackIterationChanges
                ? new List<FloydWarshallIterationLog>(vertexCount)
                : null;

            for (var k = 0; k < vertexCount; k++)
            {
                List<FloydWarshallCellChange>? iterationChanges = trackIterationChanges
                    ? []
                    : null;

                for (var i = 0; i < vertexCount; i++)
                {
                    for (var j = 0; j < vertexCount; j++)
                    {
                        if (distances[i, k] == Inf || distances[k, j] == Inf)
                            continue;

                        var throughK = distances[i, k] + distances[k, j];

                        if (throughK >= distances[i, j])
                            continue;

                        var oldDistance = distances[i, j];
                        var oldPredecessor = predecessors[i, j];

                        distances[i, j] = throughK;
                        predecessors[i, j] = predecessors[k, j];

                        iterationChanges?.Add(
                            new FloydWarshallCellChange(
                                i,
                                j,
                                oldDistance,
                                throughK,
                                oldPredecessor,
                                predecessors[i, j]));
                    }
                }

                if (iterationChanges is not null)
                {
                    logs!.Add(new FloydWarshallIterationLog(k, iterationChanges));
                }
            }

            var hasNegativeCycle = false;
            for (var i = 0; i < vertexCount; i++)
            {
                if (distances[i, i] < 0)
                {
                    hasNegativeCycle = true;
                    break;
                }
            }

            return new FloydWarshallResult(
                distances,
                predecessors,
                hasNegativeCycle,
                logs is null ? Array.Empty<FloydWarshallIterationLog>() : logs);
        }

        private static void InitializeMatrices(long?[,] adjacencyMatrix, long[,] distances, int[,] predecessors)
        {
            var vertexCount = adjacencyMatrix.GetLength(0);

            for (var i = 0; i < vertexCount; i++)
            {
                for (var j = 0; j < vertexCount; j++)
                {
                    if (i == j)
                    {
                        distances[i, j] = 0;
                        predecessors[i, j] = i;
                        continue;
                    }

                    var weight = adjacencyMatrix[i, j];
                    if (weight.HasValue)
                    {
                        if (weight.Value >= Inf)
                            throw new ArgumentOutOfRangeException(nameof(adjacencyMatrix), $"Edge weight at [{i},{j}] must be less than {Inf}.");

                        distances[i, j] = weight.Value;
                        predecessors[i, j] = i;
                    }
                    else
                    {
                        distances[i, j] = Inf;
                        predecessors[i, j] = -1;
                    }
                }
            }
        }
    }
}
