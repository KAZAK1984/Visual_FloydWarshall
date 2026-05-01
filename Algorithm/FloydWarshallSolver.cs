namespace Visual_FloydWarshall.Algorithm
{
	public static class FloydWarshallSolver
	{
		public const long Inf = long.MaxValue / 2;

		/// <summary>
		/// Computes all-pairs shortest paths and optional iteration diagnostics.
		/// </summary>
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
			var snapshots = trackIterationChanges
				? new List<FloydWarshallSnapshot>(vertexCount + 1)
				: null;

			snapshots?.Add(new FloydWarshallSnapshot(-1, CloneDistances(distances)));

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

				snapshots?.Add(new FloydWarshallSnapshot(k, CloneDistances(distances)));
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
				logs is null ? Array.Empty<FloydWarshallIterationLog>() : logs,
				snapshots is null ? Array.Empty<FloydWarshallSnapshot>() : snapshots);
		}

		/// <summary>
		/// Clones the distance matrix to freeze a snapshot for visualization.
		/// </summary>
		private static long[,] CloneDistances(long[,] source)
		{
			var size0 = source.GetLength(0);
			var size1 = source.GetLength(1);
			var clone = new long[size0, size1];

			for (var i = 0; i < size0; i++)
			{
				for (var j = 0; j < size1; j++)
				{
					clone[i, j] = source[i, j];
				}
			}

			return clone;
		}

		/// <summary>
		/// Initializes distance and predecessor matrices from the input graph.
		/// </summary>
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
