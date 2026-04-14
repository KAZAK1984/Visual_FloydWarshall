using System.Windows;
using Visual_FloydWarshall.Rendering;

namespace Visual_FloydWarshall
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void CreateGraph_Click(object sender, RoutedEventArgs e)
		{
			DrawDefaultGraph();
		}

		private void DrawDefaultGraph(int vertexCount = 10)
		{
			GraphCanvas.Children.Clear();
			var canvasWidth = GraphCanvas.ActualWidth > 0 ? GraphCanvas.ActualWidth : 300;
			var canvasHeight = GraphCanvas.ActualHeight > 0 ? GraphCanvas.ActualHeight : 250;

			foreach (var element in GraphRenderingFactory.CreateCircularGraphElements(vertexCount, canvasWidth, canvasHeight))
			{
				GraphCanvas.Children.Add(element);
			}
		}
	}
}