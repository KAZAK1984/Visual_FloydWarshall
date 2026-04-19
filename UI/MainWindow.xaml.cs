using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using Visual_FloydWarshall.Algorithm;
using Visual_FloydWarshall.Logging;
using Visual_FloydWarshall.Rendering;

namespace Visual_FloydWarshall
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
        private readonly IAlgorithmRunner _algorithmRunner;
		private readonly ILogger _logger;
		private long?[,]? _currentAdjacencyMatrix;
        private int _currentVertexCount;
		private int _currentStartVertex;
		private int _currentEndVertex;

       public MainWindow() : this(CreateDependencies())
		{
		}

		private MainWindow((IAlgorithmRunner Runner, ILogger Logger) dependencies)
			: this(dependencies.Runner, dependencies.Logger)
		{
		}

		internal MainWindow(IAlgorithmRunner algorithmRunner, ILogger logger)
		{
          ValidateDependencies(algorithmRunner, logger);

			_algorithmRunner = algorithmRunner;
			_logger = logger;

			InitializeComponent();

			_currentVertexCount = int.Parse(VertexCountTextBox.Text);
         _currentStartVertex = 0;
			_currentEndVertex = 0;
            _currentAdjacencyMatrix = BuildDefaultAdjacencyMatrix(_currentVertexCount);

			DrawCurrentVisualization();
		}

        private static (IAlgorithmRunner Runner, ILogger Logger) CreateDependencies()
		{
			var logger = CreateDefaultLogger();
            return (new FloydAlgorithmRunner(logger), logger);
		}

		private static ILogger CreateDefaultLogger()
		{
			var logPath = Path.Combine(AppContext.BaseDirectory, "visual_floyd.log");
			return new FileLogger(logPath);
		}

        private static void ValidateDependencies(IAlgorithmRunner algorithmRunner, ILogger logger)
		{
			ArgumentNullException.ThrowIfNull(algorithmRunner);
			ArgumentNullException.ThrowIfNull(logger);
		}

		private void CreateGraph_Click(object sender, RoutedEventArgs e)
		{
           try
			{
				_currentVertexCount = int.Parse(VertexCountTextBox.Text);
				_currentStartVertex = int.Parse(CalculateStartPos.Text);
				_currentEndVertex = int.Parse(CalculateEndPos.Text);

				_currentAdjacencyMatrix = BuildDefaultAdjacencyMatrix(_currentVertexCount);

				CalculationTable.ItemsSource = null;
				TimelineSlider.Minimum = 0;
				TimelineSlider.Maximum = 0;
				TimelineSlider.Value = 0;

             DrawCurrentVisualization();
				_logger.Info("New graph created.");
			}
			catch (Exception ex)
			{
				_logger.Error("Graph creation failed.", ex);
				MessageBox.Show($"Ошибка создания графа: {ex.Message}");
			}
		}

		private void CalculatePaths_Click(object sender, RoutedEventArgs e)
		{
         try
			{
               _currentStartVertex = int.Parse(CalculateStartPos.Text);
				_currentEndVertex = int.Parse(CalculateEndPos.Text);

				if (_currentStartVertex >= _currentVertexCount || _currentEndVertex >= _currentVertexCount)
				{
					MessageBox.Show("Стартовый и конечный узлы должны быть меньше количества вершин графа.");
					return;
				}

             _currentAdjacencyMatrix ??= BuildDefaultAdjacencyMatrix(_currentVertexCount);

             var config = new FloydAlgorithmConfig(
					_currentVertexCount,
					_currentStartVertex,
					_currentEndVertex,
					_currentAdjacencyMatrix,
					trackIterationChanges: true);

				_algorithmRunner.Execute(config);
				ApplyRunnerStateToView();

                DrawCurrentVisualization();
			}
			catch (Exception ex)
			{
				_logger.Error("Path calculation failed.", ex);
				MessageBox.Show($"Ошибка расчета: {ex.Message}");
			}
		}

		private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var dialog = new SaveFileDialog
				{
					Filter = "Floyd files (*.floyd)|*.floyd",
					DefaultExt = ".floyd"
				};

				if (dialog.ShowDialog() != true)
					return;

				_algorithmRunner.SaveToFile(dialog.FileName);
				MessageBox.Show("Результаты сохранены.");
			}
			catch (Exception ex)
			{
				_logger.Error("Save operation failed.", ex);
				MessageBox.Show($"Ошибка сохранения: {ex.Message}");
			}
		}

		private void LoadMenuItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var dialog = new OpenFileDialog
				{
					Filter = "Floyd files (*.floyd)|*.floyd"
				};

				if (dialog.ShowDialog() != true)
					return;

				_algorithmRunner.LoadFromFile(dialog.FileName);
				ApplyRunnerStateToView();
				DrawCurrentVisualization();
				MessageBox.Show("Результаты загружены.");
			}
			catch (Exception ex)
			{
				_logger.Error("Load operation failed.", ex);
				MessageBox.Show($"Ошибка загрузки: {ex.Message}");
			}
		}

		private void ShowDescriptionMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var description = _algorithmRunner.CurrentConfig?.Description
				?? "Алгоритм Флойда-Уоршелла вычисляет кратчайшие пути между всеми парами вершин.";

			MessageBox.Show(description, "Описание алгоритма", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void ApplyRunnerStateToView()
		{
			if (_algorithmRunner.CurrentConfig is FloydAlgorithmConfig config)
			{
				_currentVertexCount = config.CollectionSize;
				_currentStartVertex = config.StartVertex;
				_currentEndVertex = config.EndVertex;
				_currentAdjacencyMatrix = config.AdjacencyMatrix;

				VertexCountTextBox.Text = _currentVertexCount.ToString();
				CalculateStartPos.Text = _currentStartVertex.ToString();
				CalculateEndPos.Text = _currentEndVertex.ToString();
			}

			var result = _algorithmRunner.CurrentResult;
			if (result is null)
			{
				CalculationTable.ItemsSource = null;
				TimelineSlider.Minimum = 0;
				TimelineSlider.Maximum = 0;
				TimelineSlider.Value = 0;
				return;
			}

			CalculationTable.ItemsSource = BuildCalculationRows(result.IterationLogs);

			TimelineSlider.Minimum = 0;
			TimelineSlider.Maximum = Math.Max(0, result.Snapshots.Count - 1);
			TimelineSlider.Value = TimelineSlider.Maximum;

			if (result.HasNegativeCycle)
				MessageBox.Show("Обнаружен отрицательный цикл в графе.");

		}

        private void DrawCurrentVisualization()
		{
			var step = GetCurrentStep();
			var changedVertices = GetChangedVerticesForStep(step);
			var fastestPath = GetFastestPath();
			var fastestPathVertices = fastestPath.Count > 0 ? new HashSet<int>(fastestPath) : null;

			DrawDefaultGraph(changedVertices, fastestPathVertices);
			DrawDistanceSnapshot(fastestPath);
		}

		private void DrawDefaultGraph(IReadOnlySet<int>? changedVertices = null, IReadOnlySet<int>? fastestPathVertices = null)
		{
			MainCanvas.Children.Clear();
			var canvasWidth = MainCanvas.ActualWidth > 0 ? MainCanvas.ActualWidth : 300;
			var canvasHeight = MainCanvas.ActualHeight > 0 ? MainCanvas.ActualHeight : 250;

          foreach (var element in GraphRenderingFactory.CreateCircularGraphElements(
				_currentVertexCount,
				canvasWidth,
				canvasHeight,
				_currentAdjacencyMatrix,
				changedVertices,
				fastestPathVertices))
				MainCanvas.Children.Add(element);
		}

		private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			DrawCurrentVisualization();
		}

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
         DrawCurrentVisualization();
		}

     private void DrawDistanceSnapshot(IReadOnlyList<int> fastestPath)
		{
			DistanceMatrixCanvas.Children.Clear();
           var snapshot = GetCurrentSnapshot();
			var distances = snapshot?.Distances;
			var step = snapshot?.Step ?? -1;

          foreach (var element in DistanceMatrixRenderingFactory.CreateSnapshotElements(distances, step, _currentStartVertex, _currentEndVertex, fastestPath))
				DistanceMatrixCanvas.Children.Add(element);

			UpdateDistanceCanvasSize();
		}

		private void UpdateDistanceCanvasSize()
		{
			double maxWidth = 0;
			double maxHeight = 0;

			foreach (UIElement child in DistanceMatrixCanvas.Children)
			{
				if (child is not FrameworkElement element)
					continue;

				element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				var left = Canvas.GetLeft(element);
				var top = Canvas.GetTop(element);

				if (double.IsNaN(left))
					left = 0;

				if (double.IsNaN(top))
					top = 0;

				maxWidth = Math.Max(maxWidth, left + element.DesiredSize.Width);
				maxHeight = Math.Max(maxHeight, top + element.DesiredSize.Height);
			}

			DistanceMatrixCanvas.Width = Math.Max(0, maxWidth);
			DistanceMatrixCanvas.Height = Math.Max(0, maxHeight);
		}

     private FloydWarshallSnapshot? GetCurrentSnapshot()
		{
            if (_algorithmRunner.CurrentResult is null)
				return null;

			var snapshotIndex = (int)TimelineSlider.Value;
         return _algorithmRunner.CurrentResult.Snapshots.ElementAtOrDefault(snapshotIndex);
		}

		private int GetCurrentStep() => GetCurrentSnapshot()?.Step ?? -1;

		private IReadOnlySet<int> GetChangedVerticesForStep(int step)
		{
            if (_algorithmRunner.CurrentResult is null || step < 0)
				return new HashSet<int>();

            var iteration = _algorithmRunner.CurrentResult.IterationLogs.FirstOrDefault(x => x.IntermediateVertex == step);
			if (iteration is null)
				return new HashSet<int>();

			var changedVertices = new HashSet<int> { step };
			foreach (var change in iteration.Changes)
			{
				changedVertices.Add(change.FromVertex);
				changedVertices.Add(change.ToVertex);
			}

			return changedVertices;
		}

		private IReadOnlyList<int> GetFastestPath()
		{
            if (_algorithmRunner.CurrentResult is null)
				return [];

         return _algorithmRunner.CurrentResult.RestorePath(_currentStartVertex, _currentEndVertex);
		}

		private static IReadOnlyList<CalculationRow> BuildCalculationRows(IReadOnlyList<FloydWarshallIterationLog> iterationLogs)
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

		private static string FormatDistance(long distance) =>
			distance >= FloydWarshallSolver.Inf ? "∞" : distance.ToString();

		private static long?[,] BuildDefaultAdjacencyMatrix(int vertexCount)
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

		private sealed record CalculationRow(
			int Step,
			int From,
			int To,
			string OldDistance,
			string NewDistance,
			int OldPredecessor,
			int NewPredecessor);
	}
}