using Vortice.Mathematics;

namespace JipperKeyViewer.Graphics;


public interface IRenderBackend
{
    int ClientWidth { get; }
    int ClientHeight { get; }

    void BeginFrame(Color4 clear);
    void FillRoundRect(Rect r, Color4 color, float radius);
    void DrawRoundRect(Rect r, Color4 color, float radius, float stroke = 1f);
    void FillRoundRectGradient(Rect r, Color4 top, Color4 bottom, float radius);
    void DrawRoundRectGradient(Rect r, Color4 top, Color4 bottom, float radius, float stroke = 1f);
    void FillGlow(Rect r, float pad, Color4 color, float radius = 0f);
    void DrawRainBar(Rect r, Color4 color, float alpha, float dNear, float dFar, float trackH, float fadePx);

    void DrawText(string text, Rect rect, Color4 color, float fontSize, float maxWidth = 0f,
        TextAlign align = TextAlign.Center, int fontStyleFlags = 0, TextStyleFx fx = default,
        float? baselineY = null);

    (float LineHeight, float Ascent) GetTextVerticalMetrics(float fontSize, int fontStyleFlags);
}
