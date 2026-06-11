using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SvgIconEditor.App;

public partial class MainWindow : Window
{
    private enum ToolKind
    {
        Line,
        Rectangle,
        Ellipse
    }

    private ToolKind currentTool = ToolKind.Line;
    private Point startPoint;
    private bool isDrawing;
    private Shape? currentShape;

    public MainWindow()
    {
        InitializeComponent();
        UpdateToolText();
    }

    private void ToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag)
            return;

        currentTool = tag switch
        {
            "Line" => ToolKind.Line,
            "Rectangle" => ToolKind.Rectangle,
            "Ellipse" => ToolKind.Ellipse,
            _ => ToolKind.Line
        };

        UpdateToolText();
        DrawingCanvas.Focus();
    }

    private void UpdateToolText()
    {
        ToolInfoText.Text = currentTool switch
        {
            ToolKind.Line => "Линия",
            ToolKind.Rectangle => "Прямоугольник",
            ToolKind.Ellipse => "Эллипс",
            _ => "Линия"
        };
    }

    private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DrawingCanvas.Focus();

        startPoint = e.GetPosition(DrawingCanvas);
        isDrawing = true;
        currentShape = CreateShape(currentTool);

        if (currentShape == null)
            return;

        currentShape.Stroke = Brushes.Black;
        currentShape.StrokeThickness = 2;
        currentShape.Fill = currentShape is Line ? Brushes.Transparent : Brushes.LightGray;

        DrawingCanvas.Children.Add(currentShape);
        UpdateShapeGeometry(currentShape, startPoint, startPoint);
        DrawingCanvas.CaptureMouse();
    }

    private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!isDrawing || currentShape == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        Point currentPoint = e.GetPosition(DrawingCanvas);
        UpdateShapeGeometry(currentShape, startPoint, currentPoint);
    }

    private void DrawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        isDrawing = false;
        currentShape = null;

        if (DrawingCanvas.IsMouseCaptured)
            DrawingCanvas.ReleaseMouseCapture();
    }

    private static Shape? CreateShape(ToolKind tool)
    {
        return tool switch
        {
            ToolKind.Line => new Line(),
            ToolKind.Rectangle => new Rectangle(),
            ToolKind.Ellipse => new Ellipse(),
            _ => null
        };
    }

    private static void UpdateShapeGeometry(Shape shape, Point start, Point end)
    {
        if (shape is Line line)
        {
            line.X1 = start.X;
            line.Y1 = start.Y;
            line.X2 = end.X;
            line.Y2 = end.Y;
            return;
        }

        double left = Math.Min(start.X, end.X);
        double top = Math.Min(start.Y, end.Y);
        double width = Math.Abs(end.X - start.X);
        double height = Math.Abs(end.Y - start.Y);

        Canvas.SetLeft(shape, left);
        Canvas.SetTop(shape, top);

        if (shape is Rectangle rectangle)
        {
            rectangle.Width = width;
            rectangle.Height = height;
        }
        else if (shape is Ellipse ellipse)
        {
            ellipse.Width = width;
            ellipse.Height = height;
        }
    }

    private void ClearCanvas_Click(object sender, RoutedEventArgs e)
    {
        DrawingCanvas.Children.Clear();
        DrawingCanvas.Focus();
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        ShowHelp();
    }

    private void ShowHelp()
    {
        MessageBox.Show(
            "Минимальная версия SVG-редактора.\n\n" +
            "1. Выбери инструмент слева.\n" +
            "2. Протяни мышкой по холсту, чтобы нарисовать фигуру.\n" +
            "3. Кнопка «Очистить» удаляет все объекты.\n" +
            "4. F1 открывает справку.",
            "Справка",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            ShowHelp();
            e.Handled = true;
        }
    }
}
