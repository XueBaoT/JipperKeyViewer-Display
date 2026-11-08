using JipperKeyViewer.Config;
using JipperKeyViewer.Graphics;

namespace JipperKeyViewer.App;


internal sealed class WindowState
{
    public const int CwUsedDefault = unchecked((int)0x80000000);
    private const int DefaultX = 120;
    private const int DefaultY = 120;

    public int X = CwUsedDefault;
    public int Y = CwUsedDefault;
    public int ClientW;
    public int ClientH;

    public void SetClientSize(int width, int height)
    {
        if (width > 0) ClientW = width;
        if (height > 0) ClientH = height;
    }

    public void SetPosition(int x, int y)
    {

        if (Math.Abs(x) > 30000 || Math.Abs(y) > 30000) return;
        X = x;
        Y = y;
    }


    public static void SanitizePosition(ref int x, ref int y)
    {
        bool bad = x == CwUsedDefault || y == CwUsedDefault
                   || x <= -30000 || y <= -30000 || x >= 30000 || y >= 30000;
        if (!bad) return;
        GraphicsLog.Write($"窗口位置异常（{x},{y}），已回退到默认位置 {DefaultX},{DefaultY}");
        x = DefaultX;
        y = DefaultY;
    }

    public void Save(AppConfig? cfg)
    {
        if (cfg == null) return;
        SanitizePosition(ref X, ref Y);
        cfg.WindowX = X;
        cfg.WindowY = Y;

        if (ClientW > 0) cfg.WindowW = ClientW;
        if (ClientH > 0) cfg.WindowH = ClientH;
        try { cfg.Save(); }
        catch (Exception ex) { GraphicsLog.Write("窗口记忆保存失败：" + ex.Message); }
    }
}
