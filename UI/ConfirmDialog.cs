using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using Orientation = System.Windows.Controls.Orientation;

namespace JipperKeyViewer.UI;


public sealed class ConfirmDialog : Window
{
    public bool Confirmed { get; private set; }

    public ConfirmDialog(string title, string message, string iconResourceSuffix, bool yesNo, string primaryText)
    {
        bool dark = SystemIsDark();
        Title = UiLanguage.S(title);
        Width = 360;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Background = new SolidColorBrush(dark ? Color.FromRgb(0x20, 0x22, 0x25) : Color.FromRgb(0xF7, 0xF8, 0xFA));

        var grid = new Grid { Margin = new Thickness(16, 12, 16, 12) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });


        var body = new StackPanel { Orientation = Orientation.Horizontal };
        var icon = new Image
        {
            Source = LoadIcon(iconResourceSuffix),
            Width = 84,
            Height = 84,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        };
        var msg = new TextBlock
        {
            Text = UiLanguage.S(message),
            FontSize = 14,
            Foreground = new SolidColorBrush(dark ? Color.FromRgb(0xFF, 0xFF, 0xFF) : Color.FromRgb(0x1F, 0x23, 0x29)),
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Left,
            MaxWidth = 260,
            VerticalAlignment = VerticalAlignment.Center,
        };
        body.Children.Add(icon);
        body.Children.Add(msg);
        Grid.SetRow(body, 0);
        grid.Children.Add(body);


        var btns = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0),
        };
        if (yesNo)
        {
            var no = MakeButton(UiLanguage.S("否"), primary: false, dark);
            no.Click += (_, _) => { Confirmed = false; DialogResult = false; };
            btns.Children.Add(no);
            var yes = MakeButton(UiLanguage.S(primaryText), primary: true, dark);
            yes.Click += (_, _) => { Confirmed = true; DialogResult = true; };
            yes.Margin = new Thickness(8, 0, 0, 0);
            btns.Children.Add(yes);
        }
        else
        {
            var ok = MakeButton(UiLanguage.S(primaryText), primary: true, dark);
            ok.Click += (_, _) => { Confirmed = true; DialogResult = true; };
            btns.Children.Add(ok);
        }
        Grid.SetRow(btns, 1);
        grid.Children.Add(btns);

        Content = grid;
    }

    private static Button MakeButton(string text, bool primary, bool dark)
    {
        var b = new Button
        {
            Content = text,
            FontSize = 13,
            Padding = new Thickness(12, 6, 12, 6),
            MinWidth = 64,
            Cursor = System.Windows.Input.Cursors.Hand,
            Background = new SolidColorBrush(dark ? Color.FromRgb(0x2F, 0x32, 0x35) : Color.FromRgb(0xFF, 0xFF, 0xFF)),
        };
        if (primary)
        {

            b.Foreground = new SolidColorBrush(dark ? Color.FromRgb(0x4A, 0x90, 0xD9) : Color.FromRgb(0x2E, 0x6B, 0xB0));
            b.BorderBrush = new SolidColorBrush(dark ? Color.FromRgb(0x3D, 0x6E, 0xA5) : Color.FromRgb(0x3D, 0x6E, 0xA5));
            b.BorderThickness = new Thickness(2);
        }
        else
        {
            b.Foreground = new SolidColorBrush(dark ? Color.FromRgb(0xFF, 0xFF, 0xFF) : Color.FromRgb(0x1F, 0x23, 0x29));
            b.BorderBrush = new SolidColorBrush(dark ? Color.FromRgb(0x45, 0x48, 0x4D) : Color.FromRgb(0xC9, 0xCD, 0xD4));
            b.BorderThickness = new Thickness(1);
        }
        return b;
    }


    private static bool SystemIsDark()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int i && i == 0;
        }
        catch { return true; }
    }


    public static ImageSource? LoadIcon(string resourceSuffix)
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            string? res = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));
            if (res == null) return null;
            using var stream = asm.GetManifestResourceStream(res);
            if (stream == null) return null;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = stream;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }
}
