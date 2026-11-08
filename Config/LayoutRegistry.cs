using System;
using System.Collections.Generic;

namespace JipperKeyViewer.Config;


[Flags]
public enum LayoutCategory
{
    None = 0,

    General = 1,

    Mania = 2,

    FullKeyboard = 4,
}


public enum LayoutFamily
{

    Standard,

    Custom4K,

    Custom5K,

    Custom6K,

    Custom7K,

    Custom9K,
    Full108,
    Full75,
    Full60,
}


public sealed class LayoutDef
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public LayoutFamily Family { get; init; } = LayoutFamily.Standard;


    public LayoutCategory Categories { get; init; } = LayoutCategory.General;


    public Func<AppConfig, int[]> GetKeys { get; init; } = _ => Array.Empty<int>();
    public Action<AppConfig, int[]> SetKeys { get; init; } = (_, _) => { };

    public Func<AppConfig, string[]> GetTexts { get; init; } = _ => Array.Empty<string>();
    public Action<AppConfig, string[]> SetTexts { get; init; } = (_, _) => { };

    public Func<int[]> DefaultKeys { get; init; } = () => Array.Empty<int>();


    public (int[] Order, float[] W)[] Rows { get; init; } = Array.Empty<(int[], float[])>();

    public (int[] Order, float[] W)[]? RowsStd { get; init; }


    public bool InlineInfo { get; init; }

    public bool SupportsStandardKeyWidth { get; init; }

    public float InlineKpsW { get; init; } = 77f;

    public float InlineKpsWStd { get; init; } = 104f;


    public float? FixedWidth { get; init; }

    public float? FixedHeight { get; init; }

    public bool IsFullKeyboard => Family is LayoutFamily.Full108 or LayoutFamily.Full75 or LayoutFamily.Full60;

    public bool SupportsRain => !IsFullKeyboard;

    public bool SupportsPerKey => !IsFullKeyboard;
}


public static class Layouts
{
    public const int DefaultId = 16;


    private static readonly float[] W1 = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };
    private static readonly float[] W2 = { 1f, 1f };
    private static readonly float[] W10 = { 2.58f, 2.58f };
    private static readonly float[] WInline = { 1f, 1.54f, 1.54f, 1f };

    private static int K(Core.KeyCode k) => (int)k;


    public static readonly LayoutDef[] All =
    {

        new()
        {
            Id = 8, Name = "8K", Categories = LayoutCategory.General | LayoutCategory.Mania,
            GetKeys = c => c.Key8, SetKeys = (c, v) => c.Key8 = v,
            GetTexts = c => c.Key8Text, SetTexts = (c, v) => c.Key8Text = v,
            DefaultKeys = () => new[]
            {
                K(Core.KeyCode.Tab), K(Core.KeyCode.Alpha1), K(Core.KeyCode.Alpha2), K(Core.KeyCode.E),
                K(Core.KeyCode.P), K(Core.KeyCode.Equals), K(Core.KeyCode.Backspace), K(Core.KeyCode.Backslash),
            },
            Rows = new[] { (new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, W1) },
        },

        new()
        {
            Id = 10, Name = "10K", Categories = LayoutCategory.General,
            GetKeys = c => c.Key10, SetKeys = (c, v) => c.Key10 = v,
            GetTexts = c => c.Key10Text, SetTexts = (c, v) => c.Key10Text = v,
            DefaultKeys = () => new[]
            {
                K(Core.KeyCode.Tab), K(Core.KeyCode.Alpha1), K(Core.KeyCode.Alpha2), K(Core.KeyCode.E),
                K(Core.KeyCode.P), K(Core.KeyCode.Equals), K(Core.KeyCode.Backspace), K(Core.KeyCode.Backslash),
                K(Core.KeyCode.Space), K(Core.KeyCode.Comma),
            },
            Rows = new[]
            {
                (new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, W1),
                (new[] { 8, 9 }, W10),
            },
            RowsStd = new[]
            {
                (new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, W1),
                (new[] { 8, 9 }, W2),
            },
            InlineInfo = true, SupportsStandardKeyWidth = true,
            InlineKpsW = 79f, InlineKpsWStd = 158f,
        },

        new()
        {
            Id = 12, Name = "12K", Categories = LayoutCategory.General,
            GetKeys = c => c.Key12, SetKeys = (c, v) => c.Key12 = v,
            GetTexts = c => c.Key12Text, SetTexts = (c, v) => c.Key12Text = v,
            DefaultKeys = () => new[]
            {
                K(Core.KeyCode.Tab), K(Core.KeyCode.Alpha1), K(Core.KeyCode.Alpha2), K(Core.KeyCode.E),
                K(Core.KeyCode.P), K(Core.KeyCode.Equals), K(Core.KeyCode.Backspace), K(Core.KeyCode.Backslash),
                K(Core.KeyCode.Space), K(Core.KeyCode.C), K(Core.KeyCode.Comma), K(Core.KeyCode.Period),
            },
            Rows = new[]
            {
                (new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, W1),
                (new[] { 9, 8, 10, 11 }, WInline),
            },
            RowsStd = new[]
            {
                (new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, W1),
                (new[] { 9, 8, 10, 11 }, new[] { 1f, 1f, 1f, 1f }),
            },
            InlineInfo = true, SupportsStandardKeyWidth = true,
            InlineKpsW = 77f, InlineKpsWStd = 104f,
        },

        new()
        {
            Id = 14, Name = "14K", Categories = LayoutCategory.General,
            GetKeys = c => c.Key14, SetKeys = (c, v) => c.Key14 = v,
            GetTexts = c => c.Key14Text, SetTexts = (c, v) => c.Key14Text = v,
            DefaultKeys = () => new[]
            {
                K(Core.KeyCode.Tab), K(Core.KeyCode.Alpha1), K(Core.KeyCode.Alpha2), K(Core.KeyCode.E),
                K(Core.KeyCode.P), K(Core.KeyCode.Equals), K(Core.KeyCode.Backspace), K(Core.KeyCode.Backslash),
                K(Core.KeyCode.Space), K(Core.KeyCode.C), K(Core.KeyCode.Comma), K(Core.KeyCode.Period),
                K(Core.KeyCode.CapsLock), K(Core.KeyCode.LeftShift),
            },
            Rows = new[]
            {
                (new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, W1),
                (new[] { 13, 9, 8, 10, 11, 12 }, new[] { 1f, 1f, 1f, 1f, 1f, 1f }),
            },
        },

        new()
        {
            Id = 16, Name = "16K", Categories = LayoutCategory.General,
            GetKeys = c => c.Key16, SetKeys = (c, v) => c.Key16 = v,
            GetTexts = c => c.Key16Text, SetTexts = (c, v) => c.Key16Text = v,
            DefaultKeys = () => new[]
            {
                K(Core.KeyCode.Tab), K(Core.KeyCode.Alpha1), K(Core.KeyCode.Alpha2), K(Core.KeyCode.E),
                K(Core.KeyCode.P), K(Core.KeyCode.Equals), K(Core.KeyCode.Backspace), K(Core.KeyCode.Backslash),
                K(Core.KeyCode.Space), K(Core.KeyCode.C), K(Core.KeyCode.Comma), K(Core.KeyCode.Period),
                K(Core.KeyCode.CapsLock), K(Core.KeyCode.LeftShift), K(Core.KeyCode.Return), K(Core.KeyCode.H),
            },
            Rows = new[]
            {
                (new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, W1),
                (new[] { 12, 13, 9, 8, 10, 11, 14, 15 }, W1),
            },
        },

        new()
        {
            Id = 20, Name = "20K", Categories = LayoutCategory.General,
            GetKeys = c => c.Key20, SetKeys = (c, v) => c.Key20 = v,
            GetTexts = c => c.Key20Text, SetTexts = (c, v) => c.Key20Text = v,
            DefaultKeys = () => new[]
            {
                K(Core.KeyCode.Tab), K(Core.KeyCode.Alpha1), K(Core.KeyCode.Alpha2), K(Core.KeyCode.E),
                K(Core.KeyCode.P), K(Core.KeyCode.Equals), K(Core.KeyCode.Backspace), K(Core.KeyCode.Backslash),
                K(Core.KeyCode.Space), K(Core.KeyCode.C), K(Core.KeyCode.Comma), K(Core.KeyCode.Period),
                K(Core.KeyCode.CapsLock), K(Core.KeyCode.LeftShift), K(Core.KeyCode.Return), K(Core.KeyCode.H),
                K(Core.KeyCode.LeftControl), K(Core.KeyCode.D), K(Core.KeyCode.RightShift), K(Core.KeyCode.Semicolon),
            },
            Rows = new[]
            {
                (new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, W1),
                (new[] { 12, 13, 9, 8, 10, 11, 14, 15 }, W1),
                (new[] { 17, 16, 18, 19 }, WInline),
            },
            RowsStd = new[]
            {
                (new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, W1),
                (new[] { 12, 13, 9, 8, 10, 11, 14, 15 }, W1),
                (new[] { 17, 16, 18, 19 }, new[] { 1f, 1f, 1f, 1f }),
            },
            InlineInfo = true, SupportsStandardKeyWidth = true,
            InlineKpsW = 77f, InlineKpsWStd = 104f,
        },

        new()
        {
            Id = 24, Name = "24K", Categories = LayoutCategory.General,
            GetKeys = c => c.Key24, SetKeys = (c, v) => c.Key24 = v,
            GetTexts = c => c.Key24Text, SetTexts = (c, v) => c.Key24Text = v,
            DefaultKeys = () => new[]
            {
                K(Core.KeyCode.Tab), K(Core.KeyCode.Alpha1), K(Core.KeyCode.Alpha2), K(Core.KeyCode.E),
                K(Core.KeyCode.P), K(Core.KeyCode.Equals), K(Core.KeyCode.Backspace), K(Core.KeyCode.Backslash),
                K(Core.KeyCode.Space), K(Core.KeyCode.C), K(Core.KeyCode.Comma), K(Core.KeyCode.Period),
                K(Core.KeyCode.CapsLock), K(Core.KeyCode.LeftShift), K(Core.KeyCode.Return), K(Core.KeyCode.H),
                K(Core.KeyCode.LeftControl), K(Core.KeyCode.D), K(Core.KeyCode.RightShift), K(Core.KeyCode.Semicolon),
                K(Core.KeyCode.Q), K(Core.KeyCode.Z), K(Core.KeyCode.X), K(Core.KeyCode.V),
            },
            Rows = new[]
            {
                (new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, W1),
                (new[] { 12, 13, 9, 8, 10, 11, 14, 15 }, W1),
                (new[] { 17, 16, 18, 19, 21, 20, 22, 23 }, W1),
            },
        },

        new()
        {
            Id = 4, Name = "4K", Family = LayoutFamily.Custom4K, Categories = LayoutCategory.Mania,
            GetKeys = c => c.Key4, SetKeys = (c, v) => c.Key4 = v,
            GetTexts = c => c.Key4Text, SetTexts = (c, v) => c.Key4Text = v,
            DefaultKeys = () => new[]
            {
                K(Core.KeyCode.Alpha2), K(Core.KeyCode.E), K(Core.KeyCode.P), K(Core.KeyCode.Equals),
            },
            Rows = new[] { (new[] { 0, 1, 2, 3 }, W1) },
            FixedWidth = 428f, FixedHeight = 104f,
        },

        new()
        {
            Id = 5, Name = "5K", Family = LayoutFamily.Custom5K, Categories = LayoutCategory.Mania,
            GetKeys = c => c.Key5, SetKeys = (c, v) => c.Key5 = v,
            GetTexts = c => c.Key5Text, SetTexts = (c, v) => c.Key5Text = v,
            DefaultKeys = () => new[]
            {
                K(Core.KeyCode.Alpha2), K(Core.KeyCode.E), K(Core.KeyCode.Space),
                K(Core.KeyCode.P), K(Core.KeyCode.Equals),
            },
            Rows = new[] { (new[] { 0, 1, 2, 3, 4 }, W1) },
            FixedWidth = 428f, FixedHeight = 104f,
        },

        new()
        {
            Id = 6, Name = "6K", Family = LayoutFamily.Custom6K, Categories = LayoutCategory.Mania,
            GetKeys = c => c.Key6, SetKeys = (c, v) => c.Key6 = v,
            GetTexts = c => c.Key6Text, SetTexts = (c, v) => c.Key6Text = v,
            DefaultKeys = () => new[]
            {
                K(Core.KeyCode.Alpha1), K(Core.KeyCode.Alpha2), K(Core.KeyCode.E),
                K(Core.KeyCode.P), K(Core.KeyCode.Equals), K(Core.KeyCode.Backspace),
            },
            Rows = new[] { (new[] { 0, 1, 2, 3, 4, 5 }, W1) },
            FixedWidth = 428f, FixedHeight = 86f,
        },

        new()
        {
            Id = 7, Name = "7K", Family = LayoutFamily.Custom7K, Categories = LayoutCategory.Mania,
            GetKeys = c => c.Key7, SetKeys = (c, v) => c.Key7 = v,
            GetTexts = c => c.Key7Text, SetTexts = (c, v) => c.Key7Text = v,
            DefaultKeys = () => new[]
            {
                K(Core.KeyCode.Alpha1), K(Core.KeyCode.Alpha2), K(Core.KeyCode.E), K(Core.KeyCode.Space),
                K(Core.KeyCode.P), K(Core.KeyCode.Equals), K(Core.KeyCode.Backspace),
            },
            Rows = new[] { (new[] { 0, 1, 2, 3, 4, 5, 6 }, W1) },
            FixedWidth = 428f, FixedHeight = 86f,
        },

        new()
        {
            Id = 9, Name = "9K", Family = LayoutFamily.Custom9K, Categories = LayoutCategory.Mania,
            GetKeys = c => c.Key9, SetKeys = (c, v) => c.Key9 = v,
            GetTexts = c => c.Key9Text, SetTexts = (c, v) => c.Key9Text = v,
            DefaultKeys = () => new[]
            {
                K(Core.KeyCode.Tab), K(Core.KeyCode.Alpha1), K(Core.KeyCode.Alpha2), K(Core.KeyCode.E),
                K(Core.KeyCode.Space), K(Core.KeyCode.P), K(Core.KeyCode.Equals), K(Core.KeyCode.Backspace),
                K(Core.KeyCode.Backslash),
            },
            Rows = new[] { (new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, W1) },
            FixedWidth = 482f, FixedHeight = 86f,
        },

        new()
        {
            Id = 60, Name = "60K", Family = LayoutFamily.Full60, Categories = LayoutCategory.FullKeyboard,
            GetKeys = c => c.Key60, SetKeys = (c, v) => c.Key60 = v,
            DefaultKeys = AppConfig.CreateDefault60,
            FixedWidth = 725f, FixedHeight = 244f,
        },
        new()
        {
            Id = 75, Name = "75K", Family = LayoutFamily.Full75, Categories = LayoutCategory.FullKeyboard,
            GetKeys = c => c.Key75, SetKeys = (c, v) => c.Key75 = v,
            DefaultKeys = AppConfig.CreateDefault75,
            FixedWidth = 785f, FixedHeight = 299f,
        },
        new()
        {
            Id = 108, Name = "108K", Family = LayoutFamily.Full108, Categories = LayoutCategory.FullKeyboard,
            GetKeys = c => c.Key108, SetKeys = (c, v) => c.Key108 = v,
            DefaultKeys = AppConfig.CreateDefault108,
            FixedWidth = 1226f, FixedHeight = 330f,
        },
    };

    private static readonly Dictionary<int, LayoutDef> ById = BuildIndex();

    private static Dictionary<int, LayoutDef> BuildIndex()
    {
        var map = new Dictionary<int, LayoutDef>();
        foreach (var def in All) map[def.Id] = def;
        return map;
    }


    private static readonly int[] GeneralOrder = { 16, 10, 12, 8, 20, 24, 14 };


    public static IEnumerable<LayoutDef> ForCategory(LayoutCategory cat)
    {
        if (cat == LayoutCategory.General)
        {
            var ordered = new List<LayoutDef>();
            foreach (int id in GeneralOrder)
                if (ById.TryGetValue(id, out var d) && (d.Categories & LayoutCategory.General) != 0) ordered.Add(d);
            return ordered;
        }
        var list = new List<LayoutDef>();
        foreach (var d in All)
            if ((d.Categories & cat) != 0) list.Add(d);
        list.Sort((a, b) => a.Id.CompareTo(b.Id));
        return list;
    }


    public static LayoutDef Get(int id) => ById.TryGetValue(id, out var d) ? d : ById[DefaultId];


    public static bool IsKnown(int id) => ById.ContainsKey(id);


    public static IEnumerable<int> Ids
    {
        get { foreach (var d in All) yield return d.Id; }
    }
}
