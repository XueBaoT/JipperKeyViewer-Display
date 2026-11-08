using System;
using System.Windows;
using System.Windows.Controls;

using Panel = System.Windows.Controls.Panel;
using Size = System.Windows.Size;
using Rect = System.Windows.Rect;

namespace JipperKeyViewer.UI;


public sealed class EvenTabPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double availW = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;

        double total = 0;
        double maxH1 = 0, maxH2 = 0;
        int n = InternalChildren.Count;


        var widths = new double[n];
        var heights = new double[n];
        for (int i = 0; i < n; i++)
        {
            var c = InternalChildren[i];
            c.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            widths[i] = c.DesiredSize.Width;
            heights[i] = c.DesiredSize.Height;
            total += widths[i];
        }

        bool oneRow = availW <= 0 || total <= availW;
        Split(oneRow, availW, widths, out int row1Count);

        for (int i = 0; i < n; i++)
        {
            if (i < row1Count) maxH1 = Math.Max(maxH1, heights[i]);
            else maxH2 = Math.Max(maxH2, heights[i]);
        }

        double w = oneRow ? total : availW;
        double h = maxH1 + (row1Count < n ? maxH2 : 0);
        return new Size(w, h);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int n = InternalChildren.Count;
        if (n == 0) return finalSize;

        var widths = new double[n];
        double total = 0;
        for (int i = 0; i < n; i++)
        {
            widths[i] = InternalChildren[i].DesiredSize.Width;
            total += widths[i];
        }

        bool oneRow = finalSize.Width <= 0 || total <= finalSize.Width;
        Split(oneRow, finalSize.Width, widths, out int row1Count);

        double h1 = 0, h2 = 0;
        for (int i = 0; i < n; i++)
        {
            double hh = InternalChildren[i].DesiredSize.Height;
            if (i < row1Count) h1 = Math.Max(h1, hh); else h2 = Math.Max(h2, hh);
        }

        double x = 0, y = 0;
        for (int i = 0; i < n; i++)
        {
            if (i == row1Count) { x = 0; y = h1; }
            var c = InternalChildren[i];
            c.Arrange(new Rect(x, y, widths[i], i < row1Count ? h1 : h2));
            x += widths[i];
        }
        return finalSize;
    }


    private static void Split(bool oneRow, double availW, double[] widths, out int row1Count)
    {
        int n = widths.Length;
        if (oneRow) { row1Count = n; return; }

        int k = Math.Max(1, n / 2);
        double acc = 0;
        for (int i = 0; i < k; i++) acc += widths[i];
        while (k > 1 && availW > 0 && acc > availW)
        {
            k--;
            acc -= widths[k];
        }
        row1Count = Math.Clamp(k, 1, Math.Max(1, n - 1));
    }
}
