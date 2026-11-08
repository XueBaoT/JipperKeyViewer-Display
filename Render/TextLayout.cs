using JipperKeyViewer.Graphics;
using Vortice.Mathematics;

namespace JipperKeyViewer.Render;


// Shared baseline and line-box metrics for KPS/Total text.
internal static class TextLayout
{
    public const float SlimBarHeight = 30f;
    public const float InlineValueHeight = 32f;
    public const float InlineCountHeight = 16f;

    public static float SlimFontSize(float fontSize, float s, float scale)
        => Math.Min(fontSize, SlimBarHeight / 1.2f * s * scale);

    public static float InlineCountFontSize(float fontSize, float s, float scale)
        => Math.Min(fontSize, InlineCountHeight / 1.2f * s * scale);

    public static Rect InlineValueRect(Rect rect, float s, float scale)
        => new(rect.X, rect.Y + 2f * s * scale, rect.Width, InlineValueHeight * s * scale);

    public static Rect InlineCountRect(Rect rect, float s, float scale)
        => new(rect.X, rect.Y + rect.Height - 2f * s * scale - InlineCountHeight * s * scale,
            rect.Width, InlineCountHeight * s * scale);

    public static (Rect Label, Rect Count) SlimColumns(Rect rect, float s, float scale)
    {
        Rect label = new(rect.X + 7.5f * s * scale, rect.Y,
            rect.Width / 2f - 7.5f * s * scale, rect.Height);
        Rect count = new(rect.X + rect.Width / 2f, rect.Y,
            rect.Width / 2f - 10f * s * scale, rect.Height);
        return (label, count);
    }

    public static float SharedBaseline(IRenderBackend r, Rect rect, float fontSize, int fontStyleFlags)
    {
        var (lineHeight, ascent) = r.GetTextVerticalMetrics(fontSize, fontStyleFlags);
        return rect.Y + (rect.Height - lineHeight) / 2f + ascent;
    }
}
