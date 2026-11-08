using System.Windows.Controls;
using Slider = System.Windows.Controls.Slider;
using TextBlock = System.Windows.Controls.TextBlock;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{

    private void WireSliders()
    {
        Wire(sldKeyFont, valKeyFont, "F0");
        Wire(sldGlowSize, valGlowSize, "F0"); Wire(sldGlowOp, valGlowOp, "F0", v => $"{v:F0}%");
        Wire(sldOutlineW, valOutlineW, "F2", v => (v / 100f).ToString("F2"));
        Wire(sldShadowX, valShadowX, "F0"); Wire(sldShadowY, valShadowY, "F0"); Wire(sldShadowSoft, valShadowSoft, "F0");
        Wire(sldRainSp1, valRainSp1, "F0"); Wire(sldRainSp2, valRainSp2, "F0"); Wire(sldRainSp3, valRainSp3, "F0");
        Wire(sldRainH1, valRainH1, "F0"); Wire(sldRainH2, valRainH2, "F0"); Wire(sldRainH3, valRainH3, "F0");
        Wire(sldRainW1, valRainW1, "F0"); Wire(sldRainW2, valRainW2, "F0"); Wire(sldRainW3, valRainW3, "F0");
        Wire(sldRainY1, valRainY1, "F0"); Wire(sldRainY2, valRainY2, "F0"); Wire(sldRainY3, valRainY3, "F0");
        Wire(sldRainFadeT, valRainFadeT, "F2", v => (v / 100f).ToString("F2"));
        Wire(sldRainFadePx, valRainFadePx, "F0");
        Wire(sldRainShX1, valRainShX1, "F0"); Wire(sldRainShY1, valRainShY1, "F0");
        Wire(sldRainShX2, valRainShX2, "F0"); Wire(sldRainShY2, valRainShY2, "F0");
        Wire(sldRainShX3, valRainShX3, "F0"); Wire(sldRainShY3, valRainShY3, "F0");
        Wire(sldRainOlW1, valRainOlW1, "F1"); Wire(sldRainOlW2, valRainOlW2, "F1");
        Wire(sldRainOlW3, valRainOlW3, "F1");

        Wire(sldGhostY1, valGhostY1, "F0"); Wire(sldGhostY2, valGhostY2, "F0"); Wire(sldGhostY3, valGhostY3, "F0");
        Wire(sldGRShX1, valGRShX1, "F0"); Wire(sldGRShY1, valGRShY1, "F0");
        Wire(sldGRShX2, valGRShX2, "F0"); Wire(sldGRShY2, valGRShY2, "F0");
        Wire(sldGRShX3, valGRShX3, "F0"); Wire(sldGRShY3, valGRShY3, "F0");
        Wire(sldGROlW1, valGROlW1, "F1"); Wire(sldGROlW2, valGROlW2, "F1"); Wire(sldGROlW3, valGROlW3, "F1");
        Wire(sldPressScale, valPressScale, "F2");
        Wire(sldPressDur, valPressDur, "F0");
    }

    private static void Wire(Slider s, TextBlock t, string fmt, Func<double, string>? formatter = null)
    {
        s.ValueChanged += (_, _) =>
            t.Text = formatter != null ? formatter(s.Value) : s.Value.ToString(fmt);
    }


    private static System.Windows.Controls.StackPanel BuildEasingItem(string name)
    {
        var sp = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };
        sp.Children.Add(new TextBlock
        {
            Text = name,
            Width = 104,
            FontSize = 12,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        });
        sp.Children.Add(BuildEasingCurve(name));
        return sp;
    }

    private static System.Windows.Controls.Canvas BuildEasingCurve(string name)
    {
        const double cw = 46, ch = 16;
        const int N = 40;
        var cv = new System.Windows.Controls.Canvas { Width = cw, Height = ch };
        var frame = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 106, 122));

        cv.Children.Add(new System.Windows.Shapes.Rectangle
        {
            Width = cw, Height = ch,
            Stroke = frame, StrokeThickness = 1,
            Fill = System.Windows.Media.Brushes.Transparent
        });
        cv.Children.Add(new System.Windows.Shapes.Line
        {
            X1 = 1, Y1 = ch - 1, X2 = cw - 1, Y2 = 1,
            Stroke = frame, StrokeThickness = 0.5
        });

        double minV = 0.0, maxV = 1.0;
        for (int i = 0; i <= N; i++)
        {
            double v = Core.KvEasing.Ease(name, (float)((double)i / N));
            if (v < minV) minV = v;
            if (v > maxV) maxV = v;
        }
        double span = Math.Max(0.0001, maxV - minV);

        var pts = new System.Windows.Media.PointCollection();
        for (int i = 0; i <= N; i++)
        {
            double t = (double)i / N;
            double v = Core.KvEasing.Ease(name, (float)t);
            pts.Add(new System.Windows.Point(1 + t * (cw - 2), ch - 1 - (v - minV) / span * (ch - 2)));
        }
        cv.Children.Add(new System.Windows.Shapes.Polyline
        {
            Points = pts,
            Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(126, 190, 255)),
            StrokeThickness = 1.2
        });
        return cv;
    }

}
