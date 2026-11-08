using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SDColor = System.Drawing.Color;

using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace JipperKeyViewer.UI
{


    public partial class ColorPickerWindow : Window
    {

        public SDColor ResultColor { get; private set; }

        private double _h;
        private double _s;
        private double _v;
        private byte _a;

        private const int SvW = 320, SvH = 224;
        private const int VW = 26, VH = 224;
        private WriteableBitmap _svBmp;
        private WriteableBitmap _vBmp;
        private bool _hslMode;
        private bool _updating;

        private readonly bool _dark;
    public ColorPickerWindow(SDColor initial)
        {
            InitializeComponent();
        _dark = AppTheme.IsSystemDark();
        AppTheme.ApplyPalette(Resources, _dark);
        SourceInitialized += (_, _) => AppTheme.ApplyTitleBar(this, _dark);
            Title = UiLanguage.S("颜色");
            Loaded += (_, _) => UiLanguage.Apply(this, UiLanguage.Current);
            ResultColor = initial;
            swCurrent.Background = new SolidColorBrush(ToMediaColor(initial));
            _a = initial.A;
            ToHsv(initial, out _h, out _s, out _v);

            _svBmp = new WriteableBitmap(SvW, SvH, 96, 96, PixelFormats.Bgra32, null);
            _vBmp = new WriteableBitmap(VW, VH, 96, 96, PixelFormats.Bgra32, null);
            svImage.Source = _svBmp;
            vImage.Source = _vBmp;


            cmbMode.SelectedIndex = 0;
        }


        private static SDColor ColorFromHsv(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs(h / 60.0 % 2 - 1));
            double m = v - c;
            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }
            return SDColor.FromArgb(255,
                (int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
        }

        private static void ToHsv(SDColor c, out double h, out double s, out double v)
        {


            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            double d = max - min;
            v = max;
            if (d < 1e-9) { h = 0; s = 0; return; }
            s = d / max;
            if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h *= 60;
        }

        private static void ToHsl(SDColor c, out double h, out double sl, out double l)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            l = (max + min) / 2;
            if (Math.Abs(max - min) < 1e-9) { h = 0; sl = 0; return; }
            double d = max - min;
            sl = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h *= 60;
        }

        private static SDColor ColorFromHsl(double h, double sl, double l)
        {
            if (sl <= 0) { byte v = (byte)Math.Round(l * 255); return SDColor.FromArgb(255, v, v, v); }
            double q = l < 0.5 ? l * (1 + sl) : l + sl - l * sl;
            double p = 2 * l - q;
            double H = h / 360.0;
            double R = HueToRgb(p, q, H + 1 / 3.0);
            double G = HueToRgb(p, q, H);
            double B = HueToRgb(p, q, H - 1 / 3.0);
            return SDColor.FromArgb(255,
                (int)Math.Round(R * 255), (int)Math.Round(G * 255), (int)Math.Round(B * 255));
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1 / 6.0) return p + (q - p) * 6 * t;
            if (t < 1 / 2.0) return q;
            if (t < 2 / 3.0) return p + (q - p) * (2 / 3.0 - t) * 6;
            return p;
        }

        private SDColor CurrentRgb() => ColorFromHsv(_h, _s, _v);

        private static System.Windows.Media.Color ToMediaColor(SDColor c) =>
            System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B);

        private static SDColor ApplyAlpha(SDColor c, byte a) => SDColor.FromArgb(a, c.R, c.G, c.B);


        private unsafe void RenderSv()
        {
            _svBmp.Lock();
            byte* px = (byte*)_svBmp.BackBuffer;
            for (int y = 0; y < SvH; y++)
            {
                double s = 1.0 - y / (double)(SvH - 1);
                for (int x = 0; x < SvW; x++)
                {
                    double h = x / (double)(SvW - 1) * 360.0;

                    SDColor c = ColorFromHsv(h, s, 1.0);
                    int o = (y * SvW + x) * 4;
                    px[o] = c.B; px[o + 1] = c.G; px[o + 2] = c.R; px[o + 3] = 255;
                }
            }
            _svBmp.AddDirtyRect(new Int32Rect(0, 0, SvW, SvH));
            _svBmp.Unlock();
        }

        private unsafe void RenderV()
        {
            _vBmp.Lock();
            byte* px = (byte*)_vBmp.BackBuffer;
            for (int y = 0; y < VH; y++)
            {
                double v = 1.0 - y / (double)(VH - 1);
                SDColor c = ColorFromHsv(_h, _s, v);
                for (int x = 0; x < VW; x++)
                {
                    int o = (y * VW + x) * 4;
                    px[o] = c.B; px[o + 1] = c.G; px[o + 2] = c.R; px[o + 3] = 255;
                }
            }
            _vBmp.AddDirtyRect(new Int32Rect(0, 0, VW, VH));
            _vBmp.Unlock();
        }


        private void RefreshAll()
        {
            _updating = true;
            try
            {
                RenderSv(); RenderV();
                PositionMarkers();
                RefreshInputs();
                RefreshPreview();
                ResultColor = ApplyAlpha(CurrentRgb(), _a);
            }
            finally { _updating = false; }
        }

        private void PositionMarkers()
        {
            double w = svImage.ActualWidth > 0 ? svImage.ActualWidth : SvW;
            double hh = svImage.ActualHeight > 0 ? svImage.ActualHeight : SvH;
            if (svCanvas != null)
            {
                Canvas.SetLeft(svMarker, _h / 360.0 * w - svMarker.Width / 2);
                Canvas.SetTop(svMarker, (1 - _s) * hh - svMarker.Height / 2);
            }
            double vh = vImage.ActualHeight > 0 ? vImage.ActualHeight : VH;
            Canvas.SetTop(vMarker, (1 - _v) * vh - vMarker.Height / 2);
        }

        private void RefreshInputs()
        {
            var c = CurrentRgb();
            if (!_hslMode)
            {
                lbl1.Text = "红色(R):"; lbl2.Text = "绿色(G):"; lbl3.Text = "蓝色(B):";
                txt1.Text = c.R.ToString(); txt2.Text = c.G.ToString(); txt3.Text = c.B.ToString();
            }
            else
            {
                ToHsl(c, out double h, out double s, out double l);
                lbl1.Text = "色调(U):"; lbl2.Text = "饱和度(S):"; lbl3.Text = "亮度(L):";
                txt1.Text = ((int)Math.Round(h)).ToString();
                txt2.Text = ((int)Math.Round(s * 255)).ToString();
                txt3.Text = ((int)Math.Round(l * 255)).ToString();
            }
            txtHex.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}{_a:X2}";
            txtAlphaPct.Text = ((int)Math.Round(_a / 255.0 * 100)).ToString();
            sldAlpha.Value = _a / 255.0 * 100;
        }

        private void RefreshPreview()
        {
            swNew.Background = new SolidColorBrush(ToMediaColor(ResultColor));
        }


        private void SvMouseDown(object sender, MouseButtonEventArgs e) { svImage.CaptureMouse(); SvUpdate(e); }
        private void SvMouseMove(object sender, MouseEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) SvUpdate(e); }
        private void SvMouseUp(object sender, MouseButtonEventArgs e) { svImage.ReleaseMouseCapture(); }

        private void SvUpdate(MouseEventArgs e)
        {
            var p = e.GetPosition(svImage);
            double w = svImage.ActualWidth, h = svImage.ActualHeight;
            if (w <= 0 || h <= 0) return;
            _h = Math.Clamp(p.X / w, 0, 1) * 360;
            _s = 1 - Math.Clamp(p.Y / h, 0, 1);
            RefreshAll();
        }

        private void VMouseDown(object sender, MouseButtonEventArgs e) { vImage.CaptureMouse(); VUpdate(e); }
        private void VMouseMove(object sender, MouseEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) VUpdate(e); }
        private void VMouseUp(object sender, MouseButtonEventArgs e) { vImage.ReleaseMouseCapture(); }

        private void VUpdate(MouseEventArgs e)
        {
            var p = e.GetPosition(vImage);
            double h = vImage.ActualHeight;
            if (h <= 0) return;
            _v = 1 - Math.Clamp(p.Y / h, 0, 1);
            RefreshAll();
        }


        private void ModeChanged(object sender, SelectionChangedEventArgs e)
        {
            _hslMode = cmbMode.SelectedIndex == 1;
            RefreshAll();
        }

        private void SpinClick(object sender, RoutedEventArgs e)
        {
            if (sender is RepeatButton b && b.Tag is string tag && tag.Length >= 2)
            {
                int idx = tag[0] - '0'; bool up = tag[1] == '+';
                ApplyNumeric(idx, up ? 1 : -1);
            }
        }

        private void ApplyNumeric(int idx, int delta)
        {
            if (!_hslMode)
            {
                var c = CurrentRgb();
                byte nv = (byte)Math.Clamp(idx == 1 ? c.R + delta : idx == 2 ? c.G + delta : c.B + delta, 0, 255);
                SetFromRgb(idx == 1 ? nv : c.R, idx == 2 ? nv : c.G, idx == 3 ? nv : c.B);
            }
            else
            {
                ToHsl(CurrentRgb(), out double h, out double s, out double l);
                if (idx == 1) h = Math.Clamp(h + delta, 0, 359);
                else if (idx == 2) s = Math.Clamp(s + delta / 255.0, 0, 1);
                else l = Math.Clamp(l + delta / 255.0, 0, 1);
                SetFromRgb(ColorFromHsl(h, s, l));
            }
        }

        private void NumKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox t) ApplyNumText(t, t.Tag as string ?? "1");
        }

        private void NumLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox t) ApplyNumText(t, t.Tag as string ?? "1");
        }

        private void ApplyNumText(TextBox t, string tag)
        {
            if (t.Text.Length == 0) return;
            int idx = tag[0] - '0';
            if (!int.TryParse(t.Text, out int val)) { t.Text = RefreshSingle(idx); return; }
            if (!_hslMode)
            {
                var c = CurrentRgb();
                byte nv = (byte)Math.Clamp(val, 0, 255);
                SetFromRgb(idx == 1 ? nv : c.R, idx == 2 ? nv : c.G, idx == 3 ? nv : c.B);
            }
            else
            {
                ToHsl(CurrentRgb(), out double h, out double s, out double l);
                if (idx == 1) h = Math.Clamp(val, 0, 359);
                else if (idx == 2) s = Math.Clamp(val, 0, 255) / 255.0;
                else l = Math.Clamp(val, 0, 255) / 255.0;
                SetFromRgb(ColorFromHsl(h, s, l));
            }
        }

        private string RefreshSingle(int idx)
        {
            var c = CurrentRgb();
            if (!_hslMode) return idx == 1 ? c.R.ToString() : idx == 2 ? c.G.ToString() : c.B.ToString();
            ToHsl(c, out double h, out double s, out double l);
            return idx == 1 ? ((int)Math.Round(h)).ToString()
                 : idx == 2 ? ((int)Math.Round(s * 255)).ToString()
                 : ((int)Math.Round(l * 255)).ToString();
        }

        private void SetFromRgb(byte r, byte g, byte b) => SetFromRgb(SDColor.FromArgb(255, r, g, b));

        private void SetFromRgb(SDColor rgb)
        {
            ToHsv(rgb, out _h, out _s, out _v);
            RefreshAll();
        }


        private void HexKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ApplyHex();
        }

        private void HexLostFocus(object sender, RoutedEventArgs e) => ApplyHex();

        private void ApplyHex()
        {
            string s = txtHex.Text.Trim().TrimStart('#');
            if (s.Length != 6 && s.Length != 8) { RefreshHexOnly(); return; }
            if (!uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out uint v))
            { RefreshHexOnly(); return; }
            if (s.Length == 8) { _a = (byte)(v & 0xFF); v >>= 8; }
            SetFromRgb(SDColor.FromArgb(255, (byte)(v >> 16), (byte)(v >> 8), (byte)v));
        }


        private void AlphaChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updating) return;
            _a = (byte)Math.Clamp((int)(sldAlpha.Value / 100.0 * 255), 0, 255);
            txtAlphaPct.Text = ((int)Math.Round(sldAlpha.Value)).ToString();
            ResultColor = ApplyAlpha(CurrentRgb(), _a);
            RefreshPreview();
            RefreshHexOnly();
        }

        private void AlphaPctKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ApplyAlphaPct();
        }

        private void AlphaPctLostFocus(object sender, RoutedEventArgs e) => ApplyAlphaPct();

        private void ApplyAlphaPct()
        {
            if (int.TryParse(txtAlphaPct.Text, out int pct))
            {
                _a = (byte)Math.Clamp((int)(pct / 100.0 * 255), 0, 255);
                sldAlpha.Value = _a / 255.0 * 100;
                ResultColor = ApplyAlpha(CurrentRgb(), _a);
                RefreshPreview();
                RefreshHexOnly();
            }
            else txtAlphaPct.Text = ((int)Math.Round(_a / 255.0 * 100)).ToString();
        }

        private void RefreshHexOnly()
        {
            var c = CurrentRgb();
            txtHex.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}{_a:X2}";
        }


        private void OkClick(object sender, RoutedEventArgs e)
        {
            ResultColor = ApplyAlpha(CurrentRgb(), _a);
            DialogResult = true;
        }

        private void CancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
