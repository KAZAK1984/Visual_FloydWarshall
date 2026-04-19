using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Visual_FloydWarshall.Rendering
{
	public static class GraphRenderingFactory
	{
		const double topOffset = 52;
		const double nodeRadius = 14;
		const double layoutPadding = 45;
		const double weightLabelSpacing = 4;

		public static IReadOnlyList<UIElement> CreateCircularGraphElements(
			int vertexCount,
			double canvasWidth,
			double canvasHeight,
			long?[,]? adjacencyMatrix = null,
			IReadOnlySet<int>? changedVertices = null,
			IReadOnlySet<int>? fastestPathVertices = null,
			IReadOnlyList<int>? fastestPath = null,
		   int currentStep = -1,
			int currentFromVertex = -1,
			int currentToVertex = -1,
			bool hasImprovement = false)
		{
			var titleText = currentStep >= 0
				? $"Текущая операция: k={currentStep}, i={currentFromVertex}, j={currentToVertex}"
				: "Текущая операция";

			var stateText = currentStep >= 0
				? hasImprovement
					? "Результат: на этом шаге найдено улучшение D[i,j]"
					: "Результат: для выбранных k/i/j улучшения нет"
				: "Результат: расчет еще не выполнен";

			var elements = new List<UIElement>
			{
				new TextBlock
				{
					Margin = new Thickness(5, 5, 5, 0),
					Text = titleText,
					FontWeight = FontWeights.SemiBold
				},
				new TextBlock
				{
					Margin = new Thickness(5, 24, 5, 0),
					Text = stateText,
					FontSize = 11,
					Foreground = Brushes.DimGray
				}
			};

			if (vertexCount <= 0)
				return elements;

			var points = CreateOptimizedLayoutPoints(vertexCount, canvasWidth, canvasHeight, adjacencyMatrix);
			var ik = NormalizeEdge(currentFromVertex, currentStep);
			var kj = NormalizeEdge(currentStep, currentToVertex);
			var ij = NormalizeEdge(currentFromVertex, currentToVertex);

			if (adjacencyMatrix is not null)
			{
				var occupiedLabelRects = new List<Rect>();

				for (var i = 0; i < vertexCount; i++)
				{
					for (var j = i + 1; j < vertexCount; j++)
					{
						var weight = adjacencyMatrix[i, j];
						if (!weight.HasValue)
							continue;

						var from = points[i];
						var to = points[j];
						var edgeKey = (i, j);
						var isViaEdge = edgeKey == ik || edgeKey == kj;
						var isTargetEdge = edgeKey == ij;

						var edgeBrush = isViaEdge
							 ? Brushes.MediumPurple
							 : isTargetEdge
								 ? Brushes.SteelBlue
								 : Brushes.DimGray;

						var edgeThickness = isViaEdge || isTargetEdge ? 3 : 1.5;

						var edge = new Line
						{
							X1 = from.X,
							Y1 = from.Y,
							X2 = to.X,
							Y2 = to.Y,
							Stroke = edgeBrush,
							StrokeThickness = edgeThickness
						};

						if (isTargetEdge)
							edge.StrokeDashArray = new DoubleCollection { 2, 2 };

						elements.Add(edge);

						var positionFactor = GetEdgeLabelPositionFactor(i, j);
						var anchorX = from.X + ((to.X - from.X) * positionFactor);
						var anchorY = from.Y + ((to.Y - from.Y) * positionFactor);
						var (offsetX, offsetY) = CalculateLabelOffset(from, to, i, j);

						var weightLabel = new TextBlock
						{
							Text = weight.Value.ToString(),
							FontSize = 11,
							FontWeight = FontWeights.SemiBold,
							Foreground = Brushes.Black,
							Padding = new Thickness(2, 0, 2, 0)
						};

						var labelContainer = new Border
						{
							Background = Brushes.WhiteSmoke,
							BorderBrush = Brushes.DimGray,
							BorderThickness = new Thickness(0.8),
							CornerRadius = new CornerRadius(2),
							Child = weightLabel
						};

						labelContainer.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
						var labelSize = labelContainer.DesiredSize;

						var labelLeft = anchorX + offsetX - (labelSize.Width / 2);
						var labelTop = anchorY + offsetY - (labelSize.Height / 2);
						var labelRect = new Rect(labelLeft, labelTop, labelSize.Width, labelSize.Height);

						if (IsOverlappingLabel(labelRect, occupiedLabelRects))
						{
							labelLeft = anchorX - offsetX - (labelSize.Width / 2);
							labelTop = anchorY - offsetY - (labelSize.Height / 2);
							labelRect = new Rect(labelLeft, labelTop, labelSize.Width, labelSize.Height);

							if (IsOverlappingLabel(labelRect, occupiedLabelRects))
								continue;
						}

						var (connectorEnd, bendPoint) = GetConnectorPoints(anchorX, anchorY, labelRect);
						AddConnectorSegment(elements, anchorX, anchorY, bendPoint.X, bendPoint.Y);
						AddConnectorSegment(elements, bendPoint.X, bendPoint.Y, connectorEnd.X, connectorEnd.Y);
						Canvas.SetLeft(labelContainer, labelLeft);
						Canvas.SetTop(labelContainer, labelTop);
						Panel.SetZIndex(labelContainer, 1000);
						elements.Add(labelContainer);
						occupiedLabelRects.Add(labelRect);
					}
				}
			}

			for (var i = 0; i < vertexCount; i++)
			{
				var p = points[i];
				var isIntermediate = i == currentStep;
				var isFrom = i == currentFromVertex;
				var isTo = i == currentToVertex;
				var isMarked = isIntermediate || isFrom || isTo;

				if (isMarked)
				{
					var marker = new Ellipse
					{
						Width = (nodeRadius * 2) + 10,
						Height = (nodeRadius * 2) + 10,
						Fill = Brushes.Transparent,
						Stroke = isIntermediate
							? Brushes.DarkOrange
							: isFrom
								? Brushes.DarkRed
								: Brushes.SeaGreen,
						StrokeThickness = 2.2
					};

					Canvas.SetLeft(marker, p.X - nodeRadius - 5);
					Canvas.SetTop(marker, p.Y - nodeRadius - 5);
					Panel.SetZIndex(marker, 950);
					elements.Add(marker);
				}

				var fill = isIntermediate
					? Brushes.Orange
					: isFrom
						? Brushes.IndianRed
						: isTo
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
				Panel.SetZIndex(node, 960);
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
				Panel.SetZIndex(label, 970);
				elements.Add(label);
			}

			return elements;
		}

		private static Point[] CreateOptimizedLayoutPoints(int vertexCount, double canvasWidth, double canvasHeight, long?[,]? adjacencyMatrix)
		{
			var order = FindLowCrossingOrder(vertexCount, adjacencyMatrix);
			var points = new Point[vertexCount];
			var usableWidth = Math.Max(1, canvasWidth - (layoutPadding * 2));
			var usableHeight = Math.Max(1, canvasHeight - topOffset - layoutPadding);

			var centerX = layoutPadding + (usableWidth / 2);
			var centerY = topOffset + (layoutPadding / 2) + (usableHeight / 2);
			var radiusX = Math.Max(nodeRadius + 8, (usableWidth / 2) - nodeRadius);
			var radiusY = Math.Max(nodeRadius + 8, (usableHeight / 2) - nodeRadius);

			for (var i = 0; i < vertexCount; i++)
			{
				var vertex = order[i];
				var angle = (-Math.PI / 2) + ((2 * Math.PI * i) / vertexCount);
				var x = centerX + (Math.Cos(angle) * radiusX);
				var y = centerY + (Math.Sin(angle) * radiusY);
				points[vertex] = new Point(x, y);
			}

			return points;
		}

		private static int[] FindLowCrossingOrder(int vertexCount, long?[,]? adjacencyMatrix)
		{
			var edges = BuildEdges(vertexCount, adjacencyMatrix);
			var order = Enumerable.Range(0, vertexCount).ToArray();

			if (edges.Count < 2)
				return order;

			var bestCrossings = CountCrossings(order, edges, vertexCount);
			var improved = true;

			while (improved)
			{
				improved = false;

				for (var i = 0; i < vertexCount - 1; i++)
				{
					Swap(order, i, i + 1);
					var currentCrossings = CountCrossings(order, edges, vertexCount);
					if (currentCrossings < bestCrossings)
					{
						bestCrossings = currentCrossings;
						improved = true;
					}
					else
					{
						Swap(order, i, i + 1);
					}
				}
			}

			return order;
		}

		private static List<(int From, int To)> BuildEdges(int vertexCount, long?[,]? adjacencyMatrix)
		{
			var edges = new List<(int From, int To)>();

			if (adjacencyMatrix is null)
			{
				for (var i = 0; i < vertexCount; i++)
					edges.Add((i, (i + 1) % vertexCount));

				return edges;
			}

			for (var i = 0; i < vertexCount; i++)
			{
				for (var j = i + 1; j < vertexCount; j++)
				{
					if (adjacencyMatrix[i, j].HasValue)
						edges.Add((i, j));
				}
			}

			return edges;
		}

		private static int CountCrossings(int[] order, IReadOnlyList<(int From, int To)> edges, int vertexCount)
		{
			var positions = new int[vertexCount];
			for (var i = 0; i < order.Length; i++)
				positions[order[i]] = i;

			var crossings = 0;
			for (var i = 0; i < edges.Count; i++)
			{
				var (a, b) = edges[i];
				var pa = positions[a];
				var pb = positions[b];

				for (var j = i + 1; j < edges.Count; j++)
				{
					var (c, d) = edges[j];
					if (a == c || a == d || b == c || b == d)
						continue;

					var pc = positions[c];
					var pd = positions[d];

					var cBetween = IsBetweenCircular(pa, pb, pc);
					var dBetween = IsBetweenCircular(pa, pb, pd);
					if (cBetween != dBetween)
						crossings++;
				}
			}

			return crossings;
		}

		private static bool IsBetweenCircular(int start, int end, int value)
		{
			if (start < end)
				return value > start && value < end;

			return value > start || value < end;
		}

		private static void Swap(int[] values, int i, int j)
		{
			(values[i], values[j]) = (values[j], values[i]);
		}

		private static (double X, double Y) CalculateLabelOffset(Point from, Point to, int fromIndex, int toIndex)
		{
			var dx = to.X - from.X;
			var dy = to.Y - from.Y;
			var length = Math.Sqrt((dx * dx) + (dy * dy));

			if (length < 0.001)
				return (0, 0);

			var normalX = -dy / length;
			var normalY = dx / length;

			var direction = ((fromIndex + toIndex) % 2 == 0) ? 1 : -1;
			var baseOffset = Math.Clamp(length * 0.05, nodeRadius + 6, nodeRadius + 10);
			var offsetMagnitude = direction * baseOffset;

			return (normalX * offsetMagnitude, normalY * offsetMagnitude);
		}

		private static double GetEdgeLabelPositionFactor(int fromIndex, int toIndex) => 0.5;

		private static (int From, int To) NormalizeEdge(int first, int second)
		{
			if (first < 0 || second < 0)
				return (-1, -1);

			return first < second ? (first, second) : (second, first);
		}

		private static bool IsOverlappingLabel(Rect candidate, IReadOnlyList<Rect> occupied)
		{
			var expandedCandidate = ExpandRect(candidate, weightLabelSpacing);

			foreach (var existing in occupied)
			{
				var expandedExisting = ExpandRect(existing, weightLabelSpacing);
				if (expandedCandidate.IntersectsWith(expandedExisting))
					return true;
			}

			return false;
		}

		private static Rect ExpandRect(Rect rect, double padding) =>
			new(rect.X - padding, rect.Y - padding, rect.Width + (padding * 2), rect.Height + (padding * 2));

		private static (Point EndPoint, Point BendPoint) GetConnectorPoints(double x, double y, Rect rect)
		{
			var sideCenters = new[]
			{
				new Point(rect.Left, rect.Top + (rect.Height / 2)),
				new Point(rect.Right, rect.Top + (rect.Height / 2)),
				new Point(rect.Left + (rect.Width / 2), rect.Top),
				new Point(rect.Left + (rect.Width / 2), rect.Bottom)
			};

			var end = sideCenters
				.OrderBy(point => DistanceSquared(x, y, point.X, point.Y))
				.First();

			var isLeftOrRightSide = Math.Abs(end.X - rect.Left) < 0.001 || Math.Abs(end.X - rect.Right) < 0.001;
			var bend = isLeftOrRightSide
				? new Point(x, end.Y)
				: new Point(end.X, y);

			return (end, bend);
		}

		private static double DistanceSquared(double x1, double y1, double x2, double y2)
		{
			var dx = x2 - x1;
			var dy = y2 - y1;
			return (dx * dx) + (dy * dy);
		}

		private static void AddConnectorSegment(List<UIElement> elements, double x1, double y1, double x2, double y2)
		{
			if (Math.Abs(x1 - x2) < 0.001 && Math.Abs(y1 - y2) < 0.001)
				return;

			var connector = new Line
			{
				X1 = x1,
				Y1 = y1,
				X2 = x2,
				Y2 = y2,
				Stroke = Brushes.DarkRed,
				StrokeThickness = 0.8
			};

			Panel.SetZIndex(connector, 999);
			elements.Add(connector);
		}
	}
}