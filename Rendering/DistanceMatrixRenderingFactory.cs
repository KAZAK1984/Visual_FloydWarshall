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
            int step,
            int startVertex,
            int endVertex,
            IReadOnlyList<int>? fastestPath)
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
                Text = step < 0 ? "Снимок до итераций (k = -1)" : $"Снимок после шага k = {step}",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 8, 0, 8)
            });

            var distanceText = distances[startVertex, endVertex] >= FloydWarshallSolver.Inf
                ? "∞"
                : distances[startVertex, endVertex].ToString();

            rootPanel.Children.Add(new TextBlock
            {
                Text = $"D[{startVertex},{endVertex}] = {distanceText}",
                Margin = new Thickness(0, 0, 0, 8)
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
                    AddCell(matrixGrid, row + 1, col + 1, text, false);
                }
            }

            rootPanel.Children.Add(matrixGrid);
            return [rootPanel];
        }

        private static void AddCell(Grid grid, int row, int column, string text, bool isHeader)
        {
            var border = new Border
            {
                BorderBrush = Brushes.DimGray,
                BorderThickness = new Thickness(0.5),
                Background = isHeader ? Brushes.Gainsboro : Brushes.White,
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
    }
}
