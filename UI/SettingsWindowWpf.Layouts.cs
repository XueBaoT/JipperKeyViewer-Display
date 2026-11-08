using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RadioButton = System.Windows.Controls.RadioButton;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{
    private readonly List<RadioButton> _layoutButtons = new();


    private void AboutTabLangSync(object sender, SelectionChangedEventArgs e)
    {
        if (Tabs.SelectedItem is TabItem ti && (ti.Header as string) is "关于" or "About" or "정보")
            UI.UiLanguage.Apply(ti, UI.UiLanguage.Current);
    }

    private void BuildLayoutGroups()
    {
        BuildLayoutGroupsInto(spLayoutGroups, "L");
        BuildLayoutGroupsInto(spLayoutGroups2, "L2");
    }

    private void BuildLayoutGroupsInto(System.Windows.Controls.Panel host, string groupName)
    {
        if (host == null) return;
        host.Children.Clear();
        var sections = new (string Title, Config.LayoutCategory Cat)[]
        {
            ("按键布局", Config.LayoutCategory.General),
            ("osu!mania布局", Config.LayoutCategory.Mania),
            ("全键盘布局", Config.LayoutCategory.FullKeyboard),
        };
        foreach (var (title, cat) in sections)
        {
            var defs = Config.Layouts.ForCategory(cat).ToList();
            if (defs.Count == 0) continue;

            Graphics.GraphicsLog.Write($"布局分区[{groupName}] {title}：{string.Join(" / ", defs.ConvertAll(d => d.Name))}");
            host.Children.Add(new TextBlock
            {
                Text = UiLanguage.S(title),
                Style = (Style)FindResource("Sec"),
            });

            var grid = new UniformGrid { Columns = 5, Margin = new Thickness(0, 0, 0, 6) };
            foreach (var def in defs)
            {

                var rb = new RadioButton
                {
                    Content = def.Name,
                    Style = (Style)FindResource("Cell"),
                    Tag = def.Id.ToString(),
                };
                rb.Checked += LayoutChecked;
                grid.Children.Add(rb);
                _layoutButtons.Add(rb);
            }
            host.Children.Add(grid);
        }
    }


    private bool _syncingLayout;

    private void SyncLayoutButtons(int layout)
    {
        if (_syncingLayout) return;
        _syncingLayout = true;
        try
        {
            foreach (var rb in _layoutButtons)
                rb.IsChecked = rb.Tag?.ToString() == layout.ToString();
        }
        finally { _syncingLayout = false; }
    }


    private void SyncTabCells() => SyncLayoutButtons(_curLayout);


    private int _curLayout;

}
