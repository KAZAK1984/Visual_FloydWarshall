using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Visual_FloydWarshall.Rendering
{
    public static class GraphRenderingFactory
    {
        const double topOffset = 30;
        const double nodeRadius = 14;

        public static IReadOnlyList<UIElement> CreateCircularGraphElements(
            int vertexCount,
            double canvasWidth,
            double canvasHeight,
            long?[,]? adjacencyMatrix = null,
            IReadOnlySet<int>? changedVertices = null,
            IReadOnlySet<int>? fastestPathVertices = null)
        {
            var elements = new List<UIElement>
            {
                new TextBlock { Margin = new Thickness(5), Text = "Граф (Визуализация)" }
            };

            if (vertexCount <= 0)
                return elements;

            var center = new Point(canvasWidth / 2, topOffset + (canvasHeight - topOffset) / 2);
            var layoutRadius = Math.Min(canvasWidth, canvasHeight - topOffset) * 0.35;

            var points = new Point[vertexCount];
            for (var i = 0; i < vertexCount; i++)
            {
                var angle = 2 * Math.PI * i / vertexCount - Math.PI / 2;
                points[i] = new Point(
                    center.X + layoutRadius * Math.Cos(angle),
                    center.Y + layoutRadius * Math.Sin(angle));
            }

            if (adjacencyMatrix is null)
            {
                for (var i = 0; i < vertexCount; i++)
                {
                    var from = points[i];
                    var to = points[(i + 1) % vertexCount];
                    elements.Add(new Line
                    {
                        X1 = from.X,
                        Y1 = from.Y,
                        X2 = to.X,
                        Y2 = to.Y,
                        Stroke = Brushes.DimGray,
                        StrokeThickness = 1.5
                    });
                }
            }
            else
            {
                for (var i = 0; i < vertexCount; i++)
                {
                    for (var j = i + 1; j < vertexCount; j++)
                    {
                        var weight = adjacencyMatrix[i, j];
                        if (!weight.HasValue)
                            continue;

                        var from = points[i];
                        var to = points[j];

                        elements.Add(new Line
                        {
                            X1 = from.X,
                            Y1 = from.Y,
                            X2 = to.X,
                            Y2 = to.Y,
                            Stroke = Brushes.DimGray,
                            StrokeThickness = 1.5
                        });

                        var midX = (from.X + to.X) / 2;
                        var midY = (from.Y + to.Y) / 2;

                        var weightLabel = new TextBlock
                        {
                            Text = weight.Value.ToString(),
                            FontSize = 11,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = Brushes.Black,
                            Background = Brushes.WhiteSmoke,
                            Padding = new Thickness(2, 0, 2, 0)
                        };

                        Canvas.SetLeft(weightLabel, midX - 10);
                        Canvas.SetTop(weightLabel, midY - 8);
                        elements.Add(weightLabel);
                    }
                }
            }

            for (var i = 0; i < vertexCount; i++)
            {
                var p = points[i];
                var isChanged = changedVertices?.Contains(i) == true;
                var isOnFastestPath = fastestPathVertices?.Contains(i) == true;

                var fill = isChanged
                    ? Brushes.IndianRed
                    : isOnFastestPath
                        ? Brushes.MediumSeaGreen
                        : Brushes.CornflowerBlue;

                var node = new Ellipse
                {
                    Width = nodeRadius * 2,
                    Height = nodeRadius * 2,
                    Fill = fill,
                    Stroke = Brushes.MidnightBlue,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(node, p.X - nodeRadius);
                Canvas.SetTop(node, p.Y - nodeRadius);
                elements.Add(node);

                var label = new TextBlock
                {
                    Text = i.ToString(),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Width = nodeRadius * 2,
                    TextAlignment = TextAlignment.Center
                };

                Canvas.SetLeft(label, p.X - nodeRadius);
                Canvas.SetTop(label, p.Y - 8);
                elements.Add(label);
            }

            return elements;
        }
    }
}