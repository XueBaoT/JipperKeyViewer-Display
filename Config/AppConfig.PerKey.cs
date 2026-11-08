namespace JipperKeyViewer.Config;


public partial class AppConfig
{


    public float[] EnsurePerKeyFontSize(int n)
    {
        if (PerKeyFontSize == null || PerKeyFontSize.Length != n)
        {
            var f = new float[n];
            if (PerKeyFontSize != null)
                Array.Copy(PerKeyFontSize, f, Math.Min(PerKeyFontSize.Length, n));
            PerKeyFontSize = f;
        }
        return PerKeyFontSize;
    }


    public void EnsurePerKeyColors(int n)
    {
        if (n <= 0) return;
        PerKeyBackground        = EnsureColorArray(PerKeyBackground, n, Background);
        PerKeyBackgroundClicked = EnsureColorArray(PerKeyBackgroundClicked, n, BackgroundClicked);
        PerKeyOutline           = EnsureColorArray(PerKeyOutline, n, Outline);
        PerKeyOutlineClicked    = EnsureColorArray(PerKeyOutlineClicked, n, OutlineClicked);
        PerKeyText              = EnsureColorArray(PerKeyText, n, Text);
        PerKeyTextClicked       = EnsureColorArray(PerKeyTextClicked, n, TextClicked);
        PerKeyRainColor         = EnsureColorArray(PerKeyRainColor, n, Rain1Color);
    }

    private static float[][] EnsureColorArray(float[][]? src, int n, float[] fallback)
    {
        if (src != null && src.Length == n)
        {

            for (int i = 0; i < n; i++) if (src[i] == null || src[i].Length < 3) src[i] = CopyColor(fallback);
            return src;
        }
        var dst = new float[n][];
        for (int i = 0; i < n; i++)
        {
            if (src != null && i < src.Length && src[i] != null && src[i].Length >= 3) dst[i] = src[i];
            else dst[i] = CopyColor(fallback);
        }
        return dst;
    }

    private static float[] CopyColor(float[] c)
        => c.Length >= 4 ? new[] { c[0], c[1], c[2], c[3] } : new[] { c[0], c[1], c[2], 1f };


    public void ResetPerKeyColorsAt(int idx)
    {
        if (idx < 0) return;
        SetAt(PerKeyBackground, idx, Background);
        SetAt(PerKeyBackgroundClicked, idx, BackgroundClicked);
        SetAt(PerKeyOutline, idx, Outline);
        SetAt(PerKeyOutlineClicked, idx, OutlineClicked);
        SetAt(PerKeyText, idx, Text);
        SetAt(PerKeyTextClicked, idx, TextClicked);
        SetAt(PerKeyRainColor, idx, Rain1Color);
        static void SetAt(float[][] arr, int i, float[] c) { if (arr != null && i >= 0 && i < arr.Length) arr[i] = CopyColor(c); }
    }

}
