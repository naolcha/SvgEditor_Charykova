using Microsoft.Win32;
using SvgIconEditor.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
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

    private UIElement? selectedElement;
    private bool isDraggingElement;
    private Point lastMousePosition;

    private Color strokeColor = Colors.Black;
    private Color fillColor = Color.FromArgb(120, 167, 243, 208);
    private Color backgroundColor = Colors.White;

    public MainWindow()
    {
        InitializeComponent();

        UpdateToolText();
        UpdateColorPreviews();
        UpdateSelectedObjectText();
    }

    private void ToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not string tag)
        {
            return;
        }

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

        DrawingCanvas.Cursor = currentTool == ToolKind.Select
            ? Cursors.Arrow
            : Cursors.Cross;
    }

    private void UpdateColorPreviews()
    {
        StrokePreview.Background =
            new SolidColorBrush(strokeColor);

        FillPreview.Background =
            new SolidColorBrush(fillColor);

        BackgroundPreview.Background =
            new SolidColorBrush(backgroundColor);
    }

    private void UpdateSelectedObjectText()
    {
        SelectedObjectText.Text = selectedElement switch
        {
            Line => "Линия",
            Rectangle => "Прямоугольник",
            Ellipse => "Эллипс",
            _ => "Объект не выбран"
        };
    }

    private void DrawingCanvas_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        DrawingCanvas.Focus();

        Point position = e.GetPosition(DrawingCanvas);

        if (currentTool == ToolKind.Select)
        {
            UIElement? clickedElement =
                GetCanvasElement(e.OriginalSource as DependencyObject);

            SelectElement(clickedElement);

            if (selectedElement != null)
            {
                isDraggingElement = true;
                lastMousePosition = position;

                DrawingCanvas.CaptureMouse();
            }

            return;
        }

        ClearSelection();

        startPoint = position;
        isDrawing = true;

        previewShape = CreateShapeForTool(currentTool);

        if (previewShape == null)
            return;

        ApplyShapeStyle(previewShape);

        DrawingCanvas.Children.Add(previewShape);

        UpdateShapeGeometry(
            previewShape,
            startPoint,
            startPoint);

        DrawingCanvas.CaptureMouse();
    }

    private void DrawingCanvas_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        Point currentPoint =
            e.GetPosition(DrawingCanvas);

        if (isDrawing && previewShape != null)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            UpdateShapeGeometry(
                previewShape,
                startPoint,
                currentPoint);

            return;
        }

        if (!isDraggingElement ||
            selectedElement == null ||
            e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Vector movement =
            currentPoint - lastMousePosition;

        MoveElement(
            selectedElement,
            movement);

        lastMousePosition = currentPoint;
    }

    private void DrawingCanvas_MouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        isDrawing = false;
        previewShape = null;
        isDraggingElement = false;

        if (DrawingCanvas.IsMouseCaptured)
        {
            DrawingCanvas.ReleaseMouseCapture();
        }
    }

    private UIElement? GetCanvasElement(
        DependencyObject? source)
    {
        while (source != null &&
               source != DrawingCanvas)
        {
            if (source is UIElement element &&
                DrawingCanvas.Children.Contains(element))
            {
                return element;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private void SelectElement(UIElement? element)
    {
        ClearSelection();

        selectedElement = element;

        if (selectedElement != null)
        {
            selectedElement.Effect =
                new DropShadowEffect
                {
                    Color = Color.FromRgb(0, 140, 255),
                    BlurRadius = 14,
                    ShadowDepth = 0,
                    Opacity = 0.9
                };
        }

        UpdateSelectedObjectText();
    }

    private void ClearSelection()
    {
        if (selectedElement != null)
        {
            selectedElement.Effect = null;
        }

        selectedElement = null;

        UpdateSelectedObjectText();
    }

    private void MoveElement(
        UIElement element,
        Vector movement)
    {
        if (element is Line line)
        {
            line.X1 += movement.X;
            line.Y1 += movement.Y;
            line.X2 += movement.X;
            line.Y2 += movement.Y;

            return;
        }

        double left = Canvas.GetLeft(element);
        double top = Canvas.GetTop(element);

        if (double.IsNaN(left))
            left = 0;

        if (double.IsNaN(top))
            top = 0;

        Canvas.SetLeft(
            element,
            left + movement.X);

        Canvas.SetTop(
            element,
            top + movement.Y);
    }

    private Shape? CreateShapeForTool(
        ToolKind tool)
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
        shape.Stroke =
            new SolidColorBrush(strokeColor);

        shape.StrokeThickness = 2;

        if (FillEnabledCheckBox.IsChecked == true &&
            shape is not Line)
        {
            shape.Fill =
                new SolidColorBrush(fillColor);
        }
        else
        {
            shape.Fill = Brushes.Transparent;
        }
    }

    private void UpdateShapeGeometry(
        Shape shape,
        Point start,
        Point end)
    {
        if (shape is Line line)
        {
            line.X1 = start.X;
            line.Y1 = start.Y;
            line.X2 = end.X;
            line.Y2 = end.Y;

            return;
        }

        double left =
            Math.Min(start.X, end.X);

        double top =
            Math.Min(start.Y, end.Y);

        double width =
            Math.Abs(end.X - start.X);

        double height =
            Math.Abs(end.Y - start.Y);

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

    private void DeleteSelected_Click(
        object sender,
        RoutedEventArgs e)
    {
        DeleteSelectedElement();
    }

    private void DeleteSelectedElement()
    {
        if (selectedElement == null)
            return;

        DrawingCanvas.Children.Remove(selectedElement);

        selectedElement = null;

        UpdateSelectedObjectText();
        DrawingCanvas.Focus();
    }

    private void StrokeColor_Click(
        object sender,
        RoutedEventArgs e)
    {
        using var dialog =
            new WinForms.ColorDialog();

        dialog.Color =
            System.Drawing.Color.FromArgb(
                strokeColor.A,
                strokeColor.R,
                strokeColor.G,
                strokeColor.B);

        if (dialog.ShowDialog() !=
            WinForms.DialogResult.OK)
        {
            return;
        }

        strokeColor = Color.FromArgb(
            dialog.Color.A,
            dialog.Color.R,
            dialog.Color.G,
            dialog.Color.B);

        if (selectedElement is Shape shape)
        {
            shape.Stroke =
                new SolidColorBrush(strokeColor);
        }

        UpdateColorPreviews();
        DrawingCanvas.Focus();
    }

    private void FillColor_Click(
        object sender,
        RoutedEventArgs e)
    {
        using var dialog =
            new WinForms.ColorDialog();

        dialog.Color =
            System.Drawing.Color.FromArgb(
                fillColor.A,
                fillColor.R,
                fillColor.G,
                fillColor.B);

        if (dialog.ShowDialog() !=
            WinForms.DialogResult.OK)
        {
            return;
        }

        fillColor = Color.FromArgb(
            dialog.Color.A,
            dialog.Color.R,
            dialog.Color.G,
            dialog.Color.B);

        if (selectedElement is Shape shape &&
            shape is not Line)
        {
            shape.Fill =
                new SolidColorBrush(fillColor);
        }

        UpdateColorPreviews();
        DrawingCanvas.Focus();
    }

    private void BackgroundColor_Click(
        object sender,
        RoutedEventArgs e)
    {
        using var dialog =
            new WinForms.ColorDialog();

        dialog.Color =
            System.Drawing.Color.FromArgb(
                backgroundColor.A,
                backgroundColor.R,
                backgroundColor.G,
                backgroundColor.B);

        if (dialog.ShowDialog() !=
            WinForms.DialogResult.OK)
        {
            return;
        }

        backgroundColor = Color.FromArgb(
            dialog.Color.A,
            dialog.Color.R,
            dialog.Color.G,
            dialog.Color.B);

        DrawingCanvas.Background =
            new SolidColorBrush(backgroundColor);

        UpdateColorPreviews();
        DrawingCanvas.Focus();
    }

    private void ClearCanvas_Click(
        object sender,
        RoutedEventArgs e)
    {
        DrawingCanvas.Children.Clear();

        selectedElement = null;
        isDrawing = false;
        isDraggingElement = false;
        previewShape = null;

        UpdateSelectedObjectText();
        DrawingCanvas.Focus();
    }

    private void ExportSvg_Click(
        object sender,
        RoutedEventArgs e)
    {
        ClearSelection();

        var dialog = new SaveFileDialog
        {
            Filter = "SVG file (*.svg)|*.svg",
            FileName = "icon.svg"
        };

        if (dialog.ShowDialog() != true)
            return;

        SvgDocument document =
            BuildDocumentFromCanvas();

        File.WriteAllText(
            dialog.FileName,
            document.ToSvg());
    }

    private SvgDocument BuildDocumentFromCanvas()
    {
        var document = new SvgDocument
        {
            Width = (int)DrawingCanvas.Width,
            Height = (int)DrawingCanvas.Height,
            BackgroundColor =
                ColorToHex(backgroundColor)
        };

        foreach (UIElement element
                 in DrawingCanvas.Children)
        {
            if (element is Line line)
            {
                document.AddElement(
                    new SvgLineElement
                    {
                        X1 = line.X1,
                        Y1 = line.Y1,
                        X2 = line.X2,
                        Y2 = line.Y2,
                        StrokeColor =
                            BrushToSvg(line.Stroke),
                        StrokeThickness =
                            line.StrokeThickness,
                        FillColor = "none"
                    });
            }
            else if (element is Rectangle rectangle)
            {
                double x =
                    GetCanvasLeft(rectangle);

                double y =
                    GetCanvasTop(rectangle);

                document.AddElement(
                    new SvgRectangleElement
                    {
                        X = x,
                        Y = y,
                        Width = rectangle.Width,
                        Height = rectangle.Height,
                        StrokeColor =
                            BrushToSvg(rectangle.Stroke),
                        FillColor =
                            BrushToSvg(rectangle.Fill),
                        StrokeThickness =
                            rectangle.StrokeThickness
                    });
            }
            else if (element is Ellipse ellipse)
            {
                double x =
                    GetCanvasLeft(ellipse);

                double y =
                    GetCanvasTop(ellipse);

                document.AddElement(
                    new SvgEllipseElement
                    {
                        CenterX =
                            x + ellipse.Width / 2,

                        CenterY =
                            y + ellipse.Height / 2,

                        RadiusX =
                            ellipse.Width / 2,

                        RadiusY =
                            ellipse.Height / 2,

                        StrokeColor =
                            BrushToSvg(ellipse.Stroke),

                        FillColor =
                            BrushToSvg(ellipse.Fill),

                        StrokeThickness =
                            ellipse.StrokeThickness
                    });
            }
        }

        return document;
    }

    private static double GetCanvasLeft(
        UIElement element)
    {
        double value = Canvas.GetLeft(element);

        return double.IsNaN(value)
            ? 0
            : value;
    }

    private static double GetCanvasTop(
        UIElement element)
    {
        double value = Canvas.GetTop(element);

        return double.IsNaN(value)
            ? 0
            : value;
    }

    private static string BrushToSvg(
        Brush? brush)
    {
        if (brush is SolidColorBrush solid)
        {
            if (solid.Color.A == 0)
                return "none";

            return ColorToHex(solid.Color);
        }

        return "none";
    }

    private static string ColorToHex(
        Color color)
    {
        return
            $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void Help_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowHelp();
    }

    private void ShowHelp()
    {
        MessageBox.Show(
            "Выбери фигуру и протяни мышкой по холсту.\n\n" +
            "В режиме «Выбор» можно выбрать и переместить объект.\n" +
            "Кнопка «Удалить» и клавиша Delete удаляют выбранный объект.\n" +
            "Цвет линии и заливки применяется к выбранному объекту.\n" +
            "F1 открывает справку.",
            "Справка",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        DrawingCanvas.Focus();
    }

    private void Window_KeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            DeleteSelectedElement();
            e.Handled = true;

            return;
        }

        if (e.Key == Key.Escape)
        {
            ClearSelection();
            e.Handled = true;

            return;
        }

        if (e.Key == Key.F1)
        {
            ShowHelp();
            e.Handled = true;
        }
    }
}
