using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Visual_FloydWarshall.Algorithm;
using Visual_FloydWarshall.Logging;
using Visual_FloydWarshall.Rendering;
using Visual_FloydWarshall.Utility;

namespace Visual_FloydWarshall
{
	public partial class MainWindow : Window
	{
		private readonly IAlgorithmRunner _algorithmRunner;
		private readonly ILogger _logger;

		private long?[,]? _currentAdjacencyMatrix;
		private int _currentVertexCount;
		private int _currentStartVertex;
		private int _currentEndVertex;
		private bool _suppressLoopSliderRedraw;
		private CancellationTokenSource? _autoModeCts;
		private int? _lastAutoDelay;

		public MainWindow() : this(CreateDependencies()) { }

		private MainWindow((IAlgorithmRunner Runner, ILogger Logger) dependencies) : this(dependencies.Runner, dependencies.Logger) { }

		internal MainWindow(IAlgorithmRunner algorithmRunner, ILogger logger)
		{
			ValidateDependencies(algorithmRunner, logger);

			_algorithmRunner = algorithmRunner;
			_logger = logger;

			InitializeComponent();

			_currentVertexCount = int.Parse(VertexCountTextBox.Text);
			_currentStartVertex = 0;
			_currentEndVertex = 0;
			_currentAdjacencyMatrix = MainWindowSupport.BuildDefaultAdjacencyMatrix(_currentVertexCount);

			DrawCurrentVisualization();
		}

		private static (IAlgorithmRunner Runner, ILogger Logger) CreateDependencies() =>
			 MainWindowSupport.CreateDependencies();

		private static void ValidateDependencies(IAlgorithmRunner algorithmRunner, ILogger logger) =>
			MainWindowSupport.ValidateDependencies(algorithmRunner, logger);

		private void CreateGraph_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (_autoModeCts is not null)
				{
					AutoModeButton.Content = "Авто: ВЫКЛ";
					_autoModeCts.Cancel();
					_autoModeCts.Dispose();
					_autoModeCts = null;
					_logger.Info("Авто-режим остановлен при создании нового графа.");
				}

				_currentVertexCount = int.Parse(VertexCountTextBox.Text);
				_currentStartVertex = int.Parse(CalculateStartPos.Text);
				_currentEndVertex = int.Parse(CalculateEndPos.Text);

				_currentAdjacencyMatrix = MainWindowSupport.BuildDefaultAdjacencyMatrix(_currentVertexCount);
				_algorithmRunner.Reset();

				CalculationTable.ItemsSource = null;
				UpdateLoopSliderBounds();
				SetLoopSelection(0, 0, 0);

				DrawCurrentVisualization();
				_logger.Info($"Новый граф создан. Кол-во вершин: {_currentVertexCount}.");
			}
			catch (Exception ex)
			{
				_logger.Error("Ошибка создания графа.", ex);
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
					_logger.Info("Отказано в расчете путей: некорректные индексы вершин.");
					MessageBox.Show("Стартовый и конечный узлы должны быть меньше количества вершин графа.");
					return;
				}

				_currentAdjacencyMatrix ??= MainWindowSupport.BuildDefaultAdjacencyMatrix(_currentVertexCount);

				var config = new FloydAlgorithmConfig(
					_currentVertexCount,
					_currentStartVertex,
					_currentEndVertex,
					_currentAdjacencyMatrix,
					trackIterationChanges: true);

				_algorithmRunner.Execute(config);
				ApplyRunnerStateToView();

				DrawCurrentVisualization();
				_logger.Info("Расчет путей завершен.");
			}
			catch (Exception ex)
			{
				_logger.Error("Ошибка расчета путей.", ex);
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
				_logger.Info($"Результаты сохранены: {dialog.FileName}");
				MessageBox.Show("Результаты сохранены.");
			}
			catch (Exception ex)
			{
				_logger.Error("Ошибка сохранения результатов.", ex);
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
				_logger.Info($"Результаты загружены: {dialog.FileName}");
				MessageBox.Show("Результаты загружены.");
			}
			catch (Exception ex)
			{
				_logger.Error("Ошибка загрузки результатов.", ex);
				MessageBox.Show($"Ошибка загрузки: {ex.Message}");
			}
		}

		private void ShowDescriptionMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var description = _algorithmRunner.CurrentConfig?.Description
				?? "Алгоритм Флойда-Уоршелла — это динамический алгоритм для поиска кратчайших путей между всеми парами вершин взвешенного графа.\n" +
				"Он работает с графами, содержащими отрицательные рёбра (но без отрицательных циклов), и имеет временную сложность O(V³), " +
				"где V — количество вершин. Алгоритм последовательно улучшает оценки расстояний, используя промежуточные вершины.\n" +
				"Результатом является матрица кратчайших расстояний между всеми парами вершин и матрица предшественников для восстановления путей.\n" +
			   "Алгоритм также может обнаруживать отрицательные циклы в графе.";

			MessageBox.Show(description, "Описание алгоритма", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void ShowVisualizationGuideMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var visualizationGuide =
				"Как читать визуализацию:\n" +
				"1) Внизу выбери тройку (k, i, j) или нажимай 'Следующая операция (k,i,j)'.\n" +
				"2) На графе показана текущая проверка: можно ли улучшить D[i,j] через вершину k.\n" +
				"3) Цвета на графе:\n" +
				"   - оранжевый узел: k (промежуточная вершина),\n" +
				"   - красный узел: i (начальная вершина пары),\n" +
				"   - зелёный узел: j (конечная вершина пары),\n" +
				"   - фиолетовые рёбра: i-k и k-j (кандидатный путь через k),\n" +
				"   - синее пунктирное ребро: i-j (текущее расстояние, которое пытаемся улучшить).\n" +
				"4) В матрице D подсвечиваются ячейки D[i,j], D[i,k], D[k,j], а выше выводится формула сравнения.\n" +
				"5) Если найдено улучшение, показывается переход OldDistance -> NewDistance.";

			MessageBox.Show(visualizationGuide, "Как читать визуализацию", MessageBoxButton.OK, MessageBoxImage.Information);
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
				UpdateLoopSliderBounds();
				SetLoopSelection(0, 0, 0);
				return;
			}

			CalculationTable.ItemsSource = MainWindowSupport.BuildCalculationRows(result.IterationLogs);
			UpdateLoopSliderBounds();
			SetLoopSelection(0, 0, 0);

			if (result.HasNegativeCycle)
				MessageBox.Show("Обнаружен отрицательный цикл в графе.");

		}

		private void DrawCurrentVisualization()
		{
			var step = (int)LoopKSlider.Value;
			var fromVertex = (int)LoopISlider.Value;
			var toVertex = (int)LoopJSlider.Value;
			var changedVertices = new HashSet<int> { step, fromVertex, toVertex };
			var fastestPath = GetFastestPath(fromVertex, toVertex);
			var fastestPathVertices = fastestPath.Count > 0 ? new HashSet<int>(fastestPath) : null;
			var selectedChange = GetSelectedChange(step, fromVertex, toVertex);

			DrawDefaultGraph(changedVertices, fastestPathVertices, fastestPath, step, fromVertex, toVertex, selectedChange is not null);
			DrawDistanceSnapshot(fastestPath, step, fromVertex, toVertex, selectedChange);
		}

		private void DrawDefaultGraph(
			   IReadOnlySet<int>? changedVertices = null,
			   IReadOnlySet<int>? fastestPathVertices = null,
			   IReadOnlyList<int>? fastestPath = null,
			  int currentStep = -1,
			   int currentFromVertex = -1,
			   int currentToVertex = -1,
			   bool hasImprovement = false)
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
			 fastestPathVertices,
				fastestPath,
			   currentStep,
				currentFromVertex,
				currentToVertex,
				hasImprovement))
				MainCanvas.Children.Add(element);
		}

		private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			DrawCurrentVisualization();
		}

		private void LoopSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (_suppressLoopSliderRedraw)
				return;

			DrawCurrentVisualization();
		}

		private void LoopSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			_logger.Info($"Выбран шаг: k={(int)LoopKSlider.Value}, i={(int)LoopISlider.Value}, j={(int)LoopJSlider.Value}");
		}

		private void AutoSpeedTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Return)
				return;

			if (!int.TryParse(AutoSpeedTextBox.Text, out var parsedSpeed))
				return;

			parsedSpeed = Math.Clamp(parsedSpeed, 50, 5000);
			AutoSpeedTextBox.Text = parsedSpeed.ToString();
			_logger.Info($"Скорость авто-режима обновлена: {parsedSpeed}");
		}

		private async void AutoMod_Click(object sender, RoutedEventArgs e)
		{
			if (_autoModeCts is not null)
			{
				AutoModeButton.Content = "Авто: ВЫКЛ";
				_autoModeCts.Cancel();
				_autoModeCts.Dispose();
				_autoModeCts = null;
			}
			else
			{
				if (_algorithmRunner.CurrentResult is null)
				{
					_logger.Info("Отказано в запуске авто-режима: матрица расстояний не рассчитана.");
					MessageBox.Show("Сначала выполните расчет, чтобы включить авто-режим.");
					return;
				}

				AutoModeButton.Content = "Авто: ВКЛ";
				var cts = new CancellationTokenSource();
				_autoModeCts = cts;
				var token = cts.Token;
				_logger.Info("Авто-режим запущен.");

				try
				{
					while (!token.IsCancellationRequested)
					{
						MoveToNextOperation();
						int delay = (int.TryParse(AutoSpeedTextBox.Text, out var parsedDelay) && parsedDelay >= 50) ? parsedDelay : 50;
						if (_lastAutoDelay != delay)
						{
							_lastAutoDelay = delay;
							_logger.Info($"Скорость авто-режима обновлена: {delay}");
						}
						await Task.Delay(delay, token);
					}
				}
				catch (OperationCanceledException)
				{
					_logger.Info("Авто-режим остановлен.");
				}
				finally
				{
					if (ReferenceEquals(_autoModeCts, cts))
					{
						_autoModeCts.Dispose();
						_autoModeCts = null;
						AutoModeButton.Content = "Авто: ВЫКЛ";
						_lastAutoDelay = null;
					}
				}
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			_autoModeCts?.Cancel();
			_autoModeCts?.Dispose();
			_autoModeCts = null;
			base.OnClosed(e);
		}

		private void NextOperation_Click(object sender, RoutedEventArgs e)
		{
			MoveToNextOperation();
			_logger.Info($"Следующая операция вручную: k={(int)LoopKSlider.Value}, i={(int)LoopISlider.Value}, j={(int)LoopJSlider.Value}");
		}

		private void VertexSelection_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Return)
				return;

			if (!TryUpdateSelectedVertices())
				return;

			SetLoopSelection((int)LoopKSlider.Value, _currentStartVertex, _currentEndVertex);
			_logger.Info($"Выбраны вершины: старт={_currentStartVertex}, конец={_currentEndVertex}");
			DrawCurrentVisualization();
		}

		private void DrawDistanceSnapshot(
			 IReadOnlyList<int> fastestPath,
			 int step,
			 int fromVertex,
			 int toVertex,
			 FloydWarshallCellChange? selectedChange)
		{
			DistanceMatrixCanvas.Children.Clear();
			var snapshot = GetSnapshotForStep(step);
			var distances = snapshot?.Distances;
			var snapshotStep = snapshot?.Step ?? -1;

			foreach (var element in DistanceMatrixRenderingFactory.CreateSnapshotElements(
				distances,
				snapshotStep,
				fromVertex,
				toVertex,
				step,
				fastestPath,
				selectedChange))
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

		private FloydWarshallSnapshot? GetSnapshotForStep(int step)
		{
			if (_algorithmRunner.CurrentResult is null)
				return null;

			var exact = _algorithmRunner.CurrentResult.Snapshots.FirstOrDefault(x => x.Step == step);
			if (exact is not null)
				return exact;

			return _algorithmRunner.CurrentResult.Snapshots.OrderBy(x => x.Step).LastOrDefault();
		}

		private bool TryUpdateSelectedVertices()
		{
			if (CalculateStartPos is null || CalculateEndPos is null)
				return false;

			if (!int.TryParse(CalculateStartPos.Text, out var startVertex))
				return false;

			if (!int.TryParse(CalculateEndPos.Text, out var endVertex))
				return false;

			if (startVertex < 0 || endVertex < 0)
				return false;

			if (startVertex >= _currentVertexCount || endVertex >= _currentVertexCount)
				return false;

			if (_currentStartVertex == startVertex && _currentEndVertex == endVertex)
				return false;

			_currentStartVertex = startVertex;
			_currentEndVertex = endVertex;
			return true;
		}

		private FloydWarshallCellChange? GetSelectedChange(int step, int fromVertex, int toVertex)
		{
			if (_algorithmRunner.CurrentResult is null || step < 0)
				return null;

			var iteration = _algorithmRunner.CurrentResult.IterationLogs.FirstOrDefault(x => x.IntermediateVertex == step);
			if (iteration is null)
				return null;

			return iteration.Changes.FirstOrDefault(x => x.FromVertex == fromVertex && x.ToVertex == toVertex);
		}

		private IReadOnlyList<int> GetFastestPath(int startVertex, int endVertex)
		{
			if (_algorithmRunner.CurrentResult is null || _algorithmRunner.CurrentResult.HasNegativeCycle)
				return [];

			try
			{
				return _algorithmRunner.CurrentResult.RestorePath(startVertex, endVertex);
			}
			catch
			{
				return [];
			}
		}

		private void UpdateLoopSliderBounds()
		{
			var maxVertexIndex = Math.Max(0, _currentVertexCount - 1);
			LoopKSlider.Minimum = 0;
			LoopKSlider.Maximum = maxVertexIndex;
			LoopISlider.Minimum = 0;
			LoopISlider.Maximum = maxVertexIndex;
			LoopJSlider.Minimum = 0;
			LoopJSlider.Maximum = maxVertexIndex;

			LoopKSlider.Value = ClampSliderValue(LoopKSlider.Value, LoopKSlider.Minimum, LoopKSlider.Maximum);
			LoopISlider.Value = ClampSliderValue(LoopISlider.Value, LoopISlider.Minimum, LoopISlider.Maximum);
			LoopJSlider.Value = ClampSliderValue(LoopJSlider.Value, LoopJSlider.Minimum, LoopJSlider.Maximum);
		}

		private void MoveToNextOperation()
		{
			var maxVertexIndex = Math.Max(0, _currentVertexCount - 1);
			var k = (int)LoopKSlider.Value;
			var i = (int)LoopISlider.Value;
			var j = (int)LoopJSlider.Value;

			j++;
			if (j > maxVertexIndex)
			{
				j = 0;
				i++;
			}

			if (i > maxVertexIndex)
			{
				i = 0;
				k++;
			}

			if (k > maxVertexIndex)
				k = 0;

			SetLoopSelection(k, i, j);

			DrawCurrentVisualization();
		}

		private void SetLoopSelection(int k, int i, int j)
		{
			_suppressLoopSliderRedraw = true;
			LoopKSlider.Value = ClampSliderValue(k, LoopKSlider.Minimum, LoopKSlider.Maximum);
			LoopISlider.Value = ClampSliderValue(i, LoopISlider.Minimum, LoopISlider.Maximum);
			LoopJSlider.Value = ClampSliderValue(j, LoopJSlider.Minimum, LoopJSlider.Maximum);
			_suppressLoopSliderRedraw = false;
		}

		private static double ClampSliderValue(double value, double min, double max)
		{
			if (value < min)
				return min;

			if (value > max)
				return max;

			return value;
		}
	}
}