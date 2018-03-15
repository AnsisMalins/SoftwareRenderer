using System.Collections;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using UnityEngine;

namespace RasterizationTest
{
    public partial class MainWindow : Window
    {
        private static readonly Vector3[] vertices = { new Vector3(1, 1, 1), new Vector3(6, 2, 1),
            new Vector3(2, 4, 1), new Vector3(4.5f, 0.5f, 1), new Vector3(6.25f, 0.25f, 1),
            new Vector3(6.25f, 1.25f, 1), new Vector3(5.25f, 1.25f, 1), new Vector3(7.5f, 0.5f, 1),
            new Vector3(7.5f, 1.5f, 1), new Vector3(6.5f, 1.5f, 1), new Vector3(9.75f, 0.75f, 1),
            new Vector3(11.75f, 2.5f, 1), new Vector3(7.75f, 2.5f, 1), new Vector3(9.5f, 5.25f, 1),
            new Vector3(15, 0, 1), new Vector3(14.5f, 2.5f, 1), new Vector3(13.5f, 1.5f, 1),
            new Vector3(14.5f, 4.5f, 1), new Vector3(7, 4, 1), new Vector3(9.5f, 5.5f, 1),
            new Vector3(8, 7, 1), new Vector3(5, 6, 1), new Vector3(1, 6, 1),
            new Vector3(11.5f, 4.5f, 1), new Vector3(12.5f, 5.5f, 1), new Vector3(11.5f, 6.5f, 1),
            new Vector3(13.5f, 5.5f, 1), new Vector3(15.5f, 5.5f, 1), new Vector3(13.5f, 7.5f, 1),
            new Vector3(15.5f, 7.5f, 1), new Vector3(9.5f, 7.5f, 1), new Vector3(10.5f, 7.5f, 1),
            new Vector3(9.5f, 9.5f, 1) };

        private static readonly int[] indices = { 0, 1, 2, 3, 3, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            12, 11, 13, 14, 15, 16, 16, 15, 17, 18, 19, 20, 18, 20, 21, 18, 21, 22, 23, 24, 25, 26,
            27, 28, 27, 29, 28, 30, 31, 32 };

        private static readonly SolidColorBrush[] lineBrushes = { Brushes.DarkBlue,
            Brushes.DarkCyan, Brushes.DarkGoldenrod, Brushes.DarkGreen, Brushes.DarkKhaki,
            Brushes.DarkMagenta, Brushes.DarkOliveGreen, Brushes.DarkOrange, Brushes.DarkOrchid,
            Brushes.DarkRed, Brushes.DarkSalmon, Brushes.DarkSeaGreen, Brushes.DarkSlateBlue,
            Brushes.DarkTurquoise, Brushes.DarkViolet };

        private static readonly SolidColorBrush[] crossBrushes = { Brushes.Blue, Brushes.Cyan,
            Brushes.Goldenrod, Brushes.Green, Brushes.Khaki, Brushes.Magenta, Brushes.Olive,
            Brushes.Orange, Brushes.Orchid, Brushes.Red, Brushes.Salmon, Brushes.SeaGreen,
            Brushes.SlateBlue, Brushes.Turquoise, Brushes.Violet };

        private Color32[] buffer = new Color32[16 * 8];
        private float[] depth = new float[16 * 8];

        private IEnumerator fillTriangles;

        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < indices.Length; i += 3)
            {
                var brush = lineBrushes[i / 3];
                DrawLine(indices[i], indices[i + 1], brush);
                DrawLine(indices[i + 1], indices[i + 2], brush);
                DrawLine(indices[i], indices[i + 2], brush);
            }

            fillTriangles = FillTriangles();
        }

        private IEnumerator FillTriangles()
        {
            for (int i = 0; i < indices.Length; i += 3)
            {
                var brush = crossBrushes[i / 3];
                var color = brush.Color;

                foreach (var pixel in Rasterization.FillTriangle(
                    vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]],
                    color, buffer, depth, 16, 0, 8, 0, 16))
                {
                    DrawCross(pixel.Item1, pixel.Item2, brush);
                    yield return null;
                }
            }
        }

        private void canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            fillTriangles.MoveNext();
        }

        private void DrawLine(int i1, int i2, Brush brush)
        {
            var line = new Line();
            line.Stroke = brush;
            line.StrokeThickness = 3;
            line.X1 = vertices[i1].x * 40 + 8;
            line.Y1 = vertices[i1].y * 40 + 7;
            line.X2 = vertices[i2].x * 40 + 8;
            line.Y2 = vertices[i2].y * 40 + 7;
            canvas.Children.Add(line);
        }

        private void DrawCross(double x, double y, Brush brush)
        {
            var line = new Line();
            line.Stroke = brush;
            line.StrokeThickness = 3;
            line.X1 = x * 40 + 28 - 5;
            line.Y1 = y * 40 + 27 - 5;
            line.X2 = x * 40 + 28 + 5;
            line.Y2 = y * 40 + 27 + 5;
            canvas.Children.Add(line);

            line = new Line();
            line.Stroke = brush;
            line.StrokeThickness = 3;
            line.X1 = x * 40 + 28 + 5;
            line.Y1 = y * 40 + 27 - 5;
            line.X2 = x * 40 + 28 - 5;
            line.Y2 = y * 40 + 27 + 5;
            canvas.Children.Add(line);
        }
    }
}