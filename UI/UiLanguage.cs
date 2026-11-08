using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace JipperKeyViewer.UI;


internal static class UiLanguage
{
    public const string Zh = "中文";
    public const string En = "English";
    public const string Ko = "한국어";

    private static readonly ConditionalWeakTable<DependencyObject, string> Orig = new();
    private static readonly ConditionalWeakTable<Expander, object> Hooks = new();
    private static string _current = Zh;


    public static string Normalize(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return Zh;
        string v = lang.Trim();
        if (v.Equals(En, StringComparison.OrdinalIgnoreCase) || v.Equals("en", StringComparison.OrdinalIgnoreCase)
            || v.Equals("english", StringComparison.OrdinalIgnoreCase)) return En;
        if (v == Ko || v.Equals("ko", StringComparison.OrdinalIgnoreCase)
            || v.Equals("korean", StringComparison.OrdinalIgnoreCase)) return Ko;
        return Zh;
    }


    public static void Apply(DependencyObject root, string? lang)
    {
        _current = Normalize(lang);
        var seen = new HashSet<DependencyObject>(ReferenceEqualityComparer.Instance);
        Visit(root, seen);
    }


    public static string S(string zh, string? lang = null)
        => T(zh, string.IsNullOrWhiteSpace(lang) ? _current : lang!);

    public static string Current => _current;


    public static void SetCurrent(string? lang) => _current = Normalize(lang);

    private static string T(string zh, string lang)
    {
        if (string.IsNullOrEmpty(zh) || lang == Zh) return zh;

        if (zh.StartsWith('/')) return "";
        if (UiTextTable.Map.TryGetValue(zh, out var v)) return lang == Ko ? v.Ko : v.En;


        foreach (var kv in UiTextTable.Map)
        {
            string key = kv.Key;
            if (key.Length < 2 || (!key.EndsWith('：') && !key.EndsWith(':'))) continue;
            if (zh.StartsWith(key, StringComparison.Ordinal))
                return (lang == Ko ? kv.Value.Ko : kv.Value.En) + zh[key.Length..];
        }
        return zh;
    }


    public static bool Translatable(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        if (text.StartsWith('/')) return true;
        if (UiTextTable.Map.ContainsKey(text)) return true;
        foreach (var kv in UiTextTable.Map)
        {
            string key = kv.Key;
            if (key.Length < 2 || (!key.EndsWith('：') && !key.EndsWith(':'))) continue;
            if (text.StartsWith(key, StringComparison.Ordinal)) return true;
        }
        return false;
    }


    private static string Pick(DependencyObject d, string current)
    {
        if (Orig.TryGetValue(d, out var o)) return T(o, _current);
        if (!Translatable(current)) return current;
        Orig.Add(d, current);
        return T(current, _current);
    }


    private static bool TextIsDerived(TextBlock tb)
    {
        try
        {
            if (BindingOperations.IsDataBound(tb, TextBlock.TextProperty)) return true;

            return tb.TemplatedParent is FrameworkElement;
        }
        catch { return false; }
    }

    private static void Visit(DependencyObject node, HashSet<DependencyObject> seen)
    {
        if (!seen.Add(node)) return;


        if (node is System.Windows.Controls.ComboBox) return;

        switch (node)
        {
            case TextBlock tb:
                if (!TextIsDerived(tb)) tb.Text = Pick(tb, tb.Text);
                break;
            case TabItem ti when ti.Header is string th:
                ti.Header = Pick(ti, th);
                break;
            case Expander ex:
                if (ex.Header is string eh) ex.Header = Pick(ex, eh);
                if (!Hooks.TryGetValue(ex, out _))
                {
                    Hooks.Add(ex, new object());
                    ex.Expanded += (s, _) => { if (s is Expander e2) Apply(e2, _current); };
                }
                break;
            case ContentControl cc when cc.Content is string cs:
                cc.Content = Pick(cc, cs);
                break;
        }


        System.Collections.IEnumerable lo;
        try { lo = LogicalTreeHelper.GetChildren(node); }
        catch { lo = Array.Empty<object>(); }
        foreach (object o in lo)
            if (o is DependencyObject ld) Visit(ld, seen);


        if (node is Visual || node is Visual3D)
        {
            int n = 0;
            try { n = VisualTreeHelper.GetChildrenCount(node); } catch { n = 0; }
            for (int i = 0; i < n; i++)
            {
                DependencyObject vc;
                try { vc = VisualTreeHelper.GetChild(node, i); } catch { continue; }
                Visit(vc, seen);
            }
        }
    }
}
