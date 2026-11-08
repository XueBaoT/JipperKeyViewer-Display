using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JipperKeyViewer.Config;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{


    private void ResetClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ResetChoice choice = ShowResetDialog("重置配置", "请选择要重置的内容：");
            if (choice == ResetChoice.None) return;

            if (choice == ResetChoice.Resolution)
            {

                Graphics.GraphicsLog.Write("重置分辨率确认");
                _cfg.WindowW = 800;
                _cfg.WindowH = 600;
                try { _cfg.Save(); }
                catch (Exception ex) { Graphics.GraphicsLog.Write($"重置分辨率持久化失败: {ex}"); }
                _onResetResolution?.Invoke();
                ShowToast("窗口分辨率已重置为 800×600");
                return;
            }

            Graphics.GraphicsLog.Write("重置按钮确认");
            _cfg.ResetToDefaults();
            LoadValues();
            BuildKeyRows();
            BuildKeyTabRows();
            ApplyAndSave();
            ShowToast("已重置为默认配置");
            Graphics.GraphicsLog.Write($"重置完成 Layout={_cfg.KeyLayout} RainEnabled={_cfg.RainEnabled}");
        }
        catch (Exception ex)
        {
            Graphics.GraphicsLog.Write($"重置异常: {ex}");
            ShowToast("重置失败: " + ex.Message);
        }
    }


    private ResetChoice ShowResetDialog(string title, string message)
    {
        var dlg = new Window
        {
            Title = UiLanguage.S(title),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = (System.Windows.Media.Brush)Resources["Panel"],
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            SizeToContent = SizeToContent.WidthAndHeight,
        };
        var panel = new StackPanel { Margin = new Thickness(16, 12, 16, 12) };

        var iconRow = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10),
        };
        iconRow.Children.Add(new System.Windows.Controls.Image
        {
            Source = ConfirmDialog.LoadIcon("info.png"),
            Width = 84,
            Height = 84,
            Stretch = Stretch.Uniform,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        });
        iconRow.Children.Add(new TextBlock
        {
            Text = UiLanguage.S(message),
            FontSize = 14,
            Foreground = (System.Windows.Media.Brush)Resources["Text"],
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 190,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
        });
        panel.Children.Add(iconRow);
        var row = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
        };
        ResetChoice result = ResetChoice.None;

        var btnCancel = new Button { Content = UiLanguage.S("取消"), MinWidth = 68, Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
        var btnRes = new Button { Content = UiLanguage.S("仅重置分辨率"), MinWidth = 90, Margin = new Thickness(0, 0, 8, 0) };
        var btnAll = new Button { Content = UiLanguage.S("重置到默认配置"), MinWidth = 100, IsDefault = true };
        StyleDialogButton(btnCancel, false);
        StyleDialogButton(btnRes, false);
        StyleDialogButton(btnAll, true);
        btnCancel.Click += (_, _) => { result = ResetChoice.None; dlg.Close(); };
        btnRes.Click += (_, _) => { result = ResetChoice.Resolution; dlg.Close(); };
        btnAll.Click += (_, _) => { result = ResetChoice.All; dlg.Close(); };
        row.Children.Add(btnCancel);
        row.Children.Add(btnRes);
        row.Children.Add(btnAll);
        panel.Children.Add(row);
        dlg.Content = panel;
        dlg.ShowDialog();
        return result;
    }


    private bool? ShowConfirmDialog(string title, string message)
    {

        PlaySystemAlert();
        var dlg = new Window
        {
            Title = UiLanguage.S(title),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = (System.Windows.Media.Brush)Resources["Panel"],
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            SizeToContent = SizeToContent.WidthAndHeight,
        };
        var panel = new StackPanel { Margin = new Thickness(16, 12, 16, 12) };

        var iconRow = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10),
        };
        iconRow.Children.Add(new System.Windows.Controls.Image
        {
            Source = ConfirmDialog.LoadIcon("warning.png"),
            Width = 84,
            Height = 84,
            Stretch = Stretch.Uniform,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        });
        iconRow.Children.Add(new TextBlock
        {
            Text = UiLanguage.S(message),
            FontSize = 14,
            Foreground = (System.Windows.Media.Brush)Resources["Text"],
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 220,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
        });
        panel.Children.Add(iconRow);
        var row = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
        };

        var btnCancel = new Button { Content = UiLanguage.S("取消"), MinWidth = 68, Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
        var btnOk = new Button { Content = UiLanguage.S("确认"), MinWidth = 68, IsDefault = true };
        StyleDialogButton(btnCancel, false);
        StyleDialogButton(btnOk, true);
        btnCancel.Click += (_, _) => dlg.DialogResult = false;
        btnOk.Click += (_, _) => dlg.DialogResult = true;
        row.Children.Add(btnCancel);
        row.Children.Add(btnOk);
        panel.Children.Add(row);
        dlg.Content = panel;
        return dlg.ShowDialog();
    }


    private void StyleDialogButton(Button b, bool primary)
    {
        b.Background = (System.Windows.Media.Brush)Resources["Ctrl"];
        b.BorderBrush = (System.Windows.Media.Brush)Resources["Line2"];
        b.BorderThickness = new Thickness(1);
        b.Foreground = (System.Windows.Media.Brush)(primary ? Resources["Red"] : Resources["Text"]);
        b.FontSize = 13;
        b.Padding = new Thickness(10, 4, 10, 4);
        b.Cursor = System.Windows.Input.Cursors.Hand;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }

}
