using Microsoft.Win32;
using SvgIconEditor.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using File = System.IO.File;
using WinForms = System.Windows.Forms;

namespace SvgIconEditor.App;

public partial class MainWindow : Window
{
    private enum ToolKind
    {
        Select,
        Line,
        Rectangle,
        Ellipse
    }

    private ToolKind currentTool = ToolKind.Select;

    private Point startPoint;
    private bool isDrawing;
    private Shape? previewShape;

    private Color strokeColor = Colors.Black;
    private Color fillColor = Color.FromArgb(120, 167, 243, 208);
    private Color backgroundColor = Colors.White;

    public MainWindow()
    {
        InitializeComponent();

        UpdateToolText();
        UpdateColorPreviews();
    }

    private void ToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag)
            return;

        currentTool = tag switch
        {
            "Select" => ToolKind.Select,
            "Line" => ToolKind.Line,
            "Rectangle" => ToolKind.Rectangle,
            "Ellipse" => ToolKind.Ellipse,
            _ => ToolKind.Select
        };

        UpdateToolText();
        DrawingCanvas.Focus();
    }

    private void UpdateToolText()
    {
        ToolInfoText.Text = currentTool switch
        {
            ToolKind.Select => "Выбор",
            ToolKind.Line => "Линия",
            ToolKind.Rectangle => "Прямоугольник",
            ToolKind.Ellipse => "Эллипс",
            _ => "Выбор"
        };
    }

    private void UpdateColorPreviews()
    {
        StrokePreview.Background = new SolidColorBrush(strokeColor);
        FillPreview.Background = new SolidColorBrush(fillColor);
        BackgroundPreview.Background = new SolidColorBrush(backgroundColor);
    }

    private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DrawingCanvas.Focus();

        if (currentTool == ToolKind.Select)
            return;

        startPoint = e.GetPosition(DrawingCanvas);
        isDrawing = true;

        previewShape = CreateShapeForTool(currentTool);

        if (previewShape == null)
            return;

        ApplyShapeStyle(previewShape);
        DrawingCanvas.Children.Add(previewShape);

        UpdateShapeGeometry(previewShape, startPoint, startPoint);
        DrawingCanvas.CaptureMouse();
    }

    private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!isDrawing || previewShape == null)
            return;

        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        Point currentPoint = e.GetPosition(DrawingCanvas);
        UpdateShapeGeometry(previewShape, startPoint, currentPoint);
    }

    private void DrawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        isDrawing = false;
        previewShape = null;

        if (DrawingCanvas.IsMouseCaptured)
            DrawingCanvas.ReleaseMouseCapture();
    }

    private Shape? CreateShapeForTool(ToolKind tool)
    {
        return tool switch
        {
            ToolKind.Line => new Line(),
            ToolKind.Rectangle => new Rectangle(),
            ToolKind.Ellipse => new Ellipse(),
            _ => null
        };
    }

    private void ApplyShapeStyle(Shape shape)
    {
        shape.Stroke = new SolidColorBrush(strokeColor);
        shape.StrokeThickness = 2;

        if (FillEnabledCheckBox.IsChecked == true && shape is not Line)
            shape.Fill = new SolidColorBrush(fillColor);
        else
            shape.Fill = Brushes.Transparent;
    }

    private void UpdateShapeGeometry(Shape shape, Point start, Point end)
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

    private void StrokeColor_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new WinForms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(strokeColor.A, strokeColor.R, strokeColor.G, strokeColor.B);

        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
        {
            strokeColor = Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
            UpdateColorPreviews();
        }
    }

    private void FillColor_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new WinForms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(fillColor.A, fillColor.R, fillColor.G, fillColor.B);

        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
        {
            fillColor = Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
            UpdateColorPreviews();
        }
    }

    private void BackgroundColor_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new WinForms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(backgroundColor.A, backgroundColor.R, backgroundColor.G, backgroundColor.B);

        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
        {
            backgroundColor = Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
            DrawingCanvas.Background = new SolidColorBrush(backgroundColor);
            UpdateColorPreviews();
        }
    }

    private void ClearCanvas_Click(object sender, RoutedEventArgs e)
    {
        DrawingCanvas.Children.Clear();
        DrawingCanvas.Focus();
    }

    private void ExportSvg_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "SVG file (*.svg)|*.svg",
            FileName = "icon.svg"
        };

        if (dialog.ShowDialog() == true)
        {
            SvgDocument document = BuildDocumentFromCanvas();
            File.WriteAllText(dialog.FileName, document.ToSvg());
        }
    }

    private SvgDocument BuildDocumentFromCanvas()
    {
        var document = new SvgDocument
        {
            Width = (int)DrawingCanvas.Width,
            Height = (int)DrawingCanvas.Height,
            BackgroundColor = ColorToHex(backgroundColor)
        };

        foreach (UIElement element in DrawingCanvas.Children)
        {
            if (element is Line line)
            {
                document.AddElement(new SvgLineElement
                {
                    X1 = line.X1,
                    Y1 = line.Y1,
                    X2 = line.X2,
                    Y2 = line.Y2,
                    StrokeColor = BrushToSvg(line.Stroke),
                    StrokeThickness = line.StrokeThickness,
                    FillColor = "none"
                });
            }
            else if (element is Rectangle rectangle)
            {
                double x = GetCanvasLeft(rectangle);
                double y = GetCanvasTop(rectangle);

                document.AddElement(new SvgRectangleElement
                {
                    X = x,
                    Y = y,
                    Width = rectangle.Width,
                    Height = rectangle.Height,
                    StrokeColor = BrushToSvg(rectangle.Stroke),
                    FillColor = BrushToSvg(rectangle.Fill),
                    StrokeThickness = rectangle.StrokeThickness
                });
            }
            else if (element is Ellipse ellipse)
            {
                double x = GetCanvasLeft(ellipse);
                double y = GetCanvasTop(ellipse);

                document.AddElement(new SvgEllipseElement
                {
                    CenterX = x + ellipse.Width / 2,
                    CenterY = y + ellipse.Height / 2,
                    RadiusX = ellipse.Width / 2,
                    RadiusY = ellipse.Height / 2,
                    StrokeColor = BrushToSvg(ellipse.Stroke),
                    FillColor = BrushToSvg(ellipse.Fill),
                    StrokeThickness = ellipse.StrokeThickness
                });
            }
        }

        return document;
    }

    private static double GetCanvasLeft(UIElement element)
    {
        double value = Canvas.GetLeft(element);
        return double.IsNaN(value) ? 0 : value;
    }

    private static double GetCanvasTop(UIElement element)
    {
        double value = Canvas.GetTop(element);
        return double.IsNaN(value) ? 0 : value;
    }

    private static string BrushToSvg(Brush? brush)
    {
        if (brush is SolidColorBrush solid)
        {
            if (solid.Color.A == 0)
                return "none";

            return ColorToHex(solid.Color);
        }

        return "none";
    }

    private static string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        ShowHelp();
    }

    private void ShowHelp()
    {
        MessageBox.Show(
            "SVG-редактор иконок.\n\n" +
            "1. Выбери инструмент слева.\n" +
            "2. Протяни мышкой по холсту, чтобы нарисовать фигуру.\n" +
            "3. Можно менять цвет линии, заливки и фона.\n" +
            "4. Кнопка «Экспорт SVG» сохраняет рисунок в SVG-файл.\n" +
            "5. F1 открывает справку.",
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
