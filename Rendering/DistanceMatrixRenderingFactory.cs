using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Visual_FloydWarshall.Algorithm;

namespace Visual_FloydWarshall.Rendering
{
	public static class DistanceMatrixRenderingFactory
	{
		public static IReadOnlyList<UIElement> CreateSnapshotElements(
			long[,]? distances,
			int snapshotStep,
			int selectedFromVertex,
			int selectedToVertex,
			int selectedIntermediateVertex,
			IReadOnlyList<int>? fastestPath,
			FloydWarshallCellChange? selectedChange)
		{
			var rootPanel = new StackPanel
			{
				Margin = new Thickness(11)
			};

			rootPanel.Children.Add(new TextBlock { Text = "Матрица расстояний D" });

			if (distances is null)
			{
				return [rootPanel];
			}

			var vertexCount = distances.GetLength(0);

			rootPanel.Children.Add(new TextBlock
			{
				Text = snapshotStep < 0 ? "Снимок до итераций (k = -1)" : $"Снимок после шага k = {snapshotStep}",
				FontWeight = FontWeights.Bold,
				Margin = new Thickness(0, 8, 0, 8)
			});

			var distanceText = distances[selectedFromVertex, selectedToVertex] >= FloydWarshallSolver.Inf
				? "∞"
				: distances[selectedFromVertex, selectedToVertex].ToString();

			rootPanel.Children.Add(new TextBlock
			{
				Text = $"Проверка тройки: k={selectedIntermediateVertex}, i={selectedFromVertex}, j={selectedToVertex}",
				Margin = new Thickness(0, 0, 0, 8)
			});

			rootPanel.Children.Add(new TextBlock
			{
				Text = $"Текущее значение D[i,j] = D[{selectedFromVertex},{selectedToVertex}] = {distanceText}",
				Margin = new Thickness(0, 0, 0, 8)
			});

			var dij = distances[selectedFromVertex, selectedToVertex];
			var dik = distances[selectedFromVertex, selectedIntermediateVertex];
			var dkj = distances[selectedIntermediateVertex, selectedToVertex];
			var candidate = TrySumDistances(dik, dkj);
			var formulaText =
				$"Проверка: D[i,j]({FormatDistance(dij)}) > D[i,k]({FormatDistance(dik)}) + D[k,j]({FormatDistance(dkj)}) = {FormatDistance(candidate)}";

			rootPanel.Children.Add(new TextBlock
			{
				Text = formulaText,
				Margin = new Thickness(0, 0, 0, 8)
			});

			var updateText = selectedChange is null
				? "Обновление на этой операции: нет"
				: $"Обновление: {FormatDistance(selectedChange.OldDistance)} -> {FormatDistance(selectedChange.NewDistance)}";

			rootPanel.Children.Add(new TextBlock
			{
				Text = updateText,
				Margin = new Thickness(0, 0, 0, 8),
				Foreground = selectedChange is null ? Brushes.DimGray : Brushes.IndianRed,
				FontWeight = selectedChange is null ? FontWeights.Normal : FontWeights.SemiBold
			});

			var fastestPathText = fastestPath is null || fastestPath.Count == 0
				? "Быстрейший путь: нет"
				: $"Быстрейший путь: {string.Join(" -> ", fastestPath)}";

			rootPanel.Children.Add(new TextBlock
			{
				Text = fastestPathText,
				Margin = new Thickness(0, 0, 0, 8)
			});

			var matrixGrid = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Left
			};

			matrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			matrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			for (var index = 0; index < vertexCount; index++)
			{
				matrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
				matrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
				AddCell(matrixGrid, 0, index + 1, index.ToString(), true);
				AddCell(matrixGrid, index + 1, 0, index.ToString(), true);
			}

			for (var row = 0; row < vertexCount; row++)
			{
				for (var col = 0; col < vertexCount; col++)
				{
					var value = distances[row, col];
					var text = value >= FloydWarshallSolver.Inf ? "∞" : value.ToString();
					var isTarget = row == selectedFromVertex && col == selectedToVertex;
					var isIk = row == selectedFromVertex && col == selectedIntermediateVertex;
					var isKj = row == selectedIntermediateVertex && col == selectedToVertex;
					var background = isTarget
						? Brushes.LightSkyBlue
						: isIk || isKj
							? Brushes.Plum
							: Brushes.White;

					AddCell(matrixGrid, row + 1, col + 1, text, false, background);
				}
			}

			rootPanel.Children.Add(matrixGrid);
			return [rootPanel];
		}

		private static void AddCell(Grid grid, int row, int column, string text, bool isHeader, Brush? background = null)
		{
			var border = new Border
			{
				BorderBrush = Brushes.DimGray,
				BorderThickness = new Thickness(0.5),
				Background = isHeader ? Brushes.Gainsboro : background ?? Brushes.White,
				Padding = new Thickness(6, 3, 6, 3),
				MinWidth = 36
			};

			border.Child = new TextBlock
			{
				Text = text,
				TextAlignment = TextAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				FontWeight = isHeader ? FontWeights.Bold : FontWeights.Regular
			};

			Grid.SetRow(border, row);
			Grid.SetColumn(border, column);
			grid.Children.Add(border);
		}

		private static string FormatDistance(long distance) =>
			distance >= FloydWarshallSolver.Inf ? "∞" : distance.ToString();

		private static long TrySumDistances(long left, long right)
		{
			if (left >= FloydWarshallSolver.Inf || right >= FloydWarshallSolver.Inf)
				return FloydWarshallSolver.Inf;

			var sum = left + right;
			return sum >= FloydWarshallSolver.Inf ? FloydWarshallSolver.Inf : sum;
		}
	}
}
