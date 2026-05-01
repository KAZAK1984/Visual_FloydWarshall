using System.IO;
using Visual_FloydWarshall.Algorithm;
using Visual_FloydWarshall.Logging;

namespace Visual_FloydWarshall.Utility
{
	internal static class MainWindowSupport
	{
		public static (IAlgorithmRunner Runner, ILogger Logger) CreateDependencies()
		{
			var logger = CreateDefaultLogger();
			return (new FloydAlgorithmRunner(logger), logger);
		}

		public static void ValidateDependencies(IAlgorithmRunner algorithmRunner, ILogger logger)
		{
			ArgumentNullException.ThrowIfNull(algorithmRunner);
			ArgumentNullException.ThrowIfNull(logger);
		}

		public static List<CalculationRow> BuildCalculationRows(IReadOnlyList<FloydWarshallIterationLog> iterationLogs)
		{
			var rows = new List<CalculationRow>();
			foreach (var iteration in iterationLogs)
			{
				foreach (var change in iteration.Changes)
				{
					rows.Add(new CalculationRow(
						iteration.IntermediateVertex,
						change.FromVertex,
						change.ToVertex,
						FormatDistance(change.OldDistance),
						FormatDistance(change.NewDistance),
						change.OldPredecessor,
						change.NewPredecessor));
				}
			}

			return rows;
		}

		/// <summary>
		/// Creates a randomized adjacency matrix for demo visualization.
		/// </summary>
		public static long?[,] BuildDefaultAdjacencyMatrix(int vertexCount)
		{
			var matrix = new long?[vertexCount, vertexCount];
			const double edgeProbability = 0.35;

			for (var i = 0; i < vertexCount; i++)
			{
				matrix[i, i] = 0;

				for (var j = i + 1; j < vertexCount; j++)
				{
					if (Random.Shared.NextDouble() > edgeProbability)
						continue;

					var weight = Random.Shared.Next(1, 21);
					matrix[i, j] = weight;
					matrix[j, i] = weight;
				}
			}

			return matrix;
		}

		private static FileLogger CreateDefaultLogger()
		{
			var logPath = Path.Combine(AppContext.BaseDirectory, "visual_floyd.log");
			return new FileLogger(logPath);
		}

		private static string FormatDistance(long distance) =>
			distance >= FloydWarshallSolver.Inf ? "∞" : distance.ToString();
	}
	internal sealed record CalculationRow(
		int Step,
		int From,
		int To,
		string OldDistance,
		string NewDistance,
		int OldPredecessor,
		int NewPredecessor);
}
