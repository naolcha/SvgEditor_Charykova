using System.Globalization;
using System.Text;

namespace SvgIconEditor.Core;

public interface ISvgElement
{
    string StrokeColor { get; set; }

    string FillColor { get; set; }

    double StrokeThickness { get; set; }

    string ToSvg();
}

public abstract class SvgElementBase : ISvgElement
{
    public string StrokeColor { get; set; } = "#000000";

    public string FillColor { get; set; } = "none";

    public double StrokeThickness { get; set; } = 2;

    public abstract string ToSvg();

    protected static string N(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}

public sealed class SvgLineElement : SvgElementBase
{
    public double X1 { get; set; }

    public double Y1 { get; set; }

    public double X2 { get; set; }

    public double Y2 { get; set; }

    public override string ToSvg()
    {
        return $"<line x1=\"{N(X1)}\" y1=\"{N(Y1)}\" x2=\"{N(X2)}\" y2=\"{N(Y2)}\" stroke=\"{StrokeColor}\" stroke-width=\"{N(StrokeThickness)}\" fill=\"none\" />";
    }
}

public sealed class SvgRectangleElement : SvgElementBase
{
    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public override string ToSvg()
    {
        return $"<rect x=\"{N(X)}\" y=\"{N(Y)}\" width=\"{N(Width)}\" height=\"{N(Height)}\" stroke=\"{StrokeColor}\" stroke-width=\"{N(StrokeThickness)}\" fill=\"{FillColor}\" />";
    }
}

public sealed class SvgEllipseElement : SvgElementBase
{
    public double CenterX { get; set; }

    public double CenterY { get; set; }

    public double RadiusX { get; set; }

    public double RadiusY { get; set; }

    public override string ToSvg()
    {
        return $"<ellipse cx=\"{N(CenterX)}\" cy=\"{N(CenterY)}\" rx=\"{N(RadiusX)}\" ry=\"{N(RadiusY)}\" stroke=\"{StrokeColor}\" stroke-width=\"{N(StrokeThickness)}\" fill=\"{FillColor}\" />";
    }
}

public sealed class SvgDocument
{
    private readonly List<ISvgElement> elements = new();

    public int Width { get; set; } = 1600;

    public int Height { get; set; } = 1000;

    public string BackgroundColor { get; set; } = "#FFFFFF";

    public IReadOnlyList<ISvgElement> Elements => elements;

    public void AddElement(ISvgElement element)
    {
        elements.Add(element);
    }

    public void Clear()
    {
        elements.Clear();
    }

    public string ToSvg()
    {
        var builder = new StringBuilder();

        builder.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Width}\" height=\"{Height}\" viewBox=\"0 0 {Width} {Height}\">");
        builder.AppendLine($"  <rect width=\"100%\" height=\"100%\" fill=\"{BackgroundColor}\" />");

        foreach (ISvgElement element in elements)
        {
            builder.AppendLine("  " + element.ToSvg());
        }

        builder.AppendLine("</svg>");

        return builder.ToString();
    }
}