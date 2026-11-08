using JipperKeyViewer.Config;

namespace JipperKeyViewer.Render;


// Static geometry tables for every supported key layout.
internal static class LayoutGeometry
{
    internal const float KeyUnit = 50f;
    internal const float Gap = 4f;

    internal static bool Is4K(int layout) => layout == 4;
    internal static bool Is5K(int layout) => layout == 5;
    internal const float K5KeyW = 5 * KeyUnit + 4 * Gap;
    internal static bool Is6K(int layout) => layout == 6;
    internal const float K6KeyW = 6 * KeyUnit + 5 * Gap;
    internal static bool Is7K(int layout) => layout == 7;
    internal const float K7KeyW = 7 * KeyUnit + 6 * Gap;
    internal static bool Is9K(int layout) => layout == 9;
    internal const float K9KeyW = 9 * KeyUnit + 8 * Gap;

    internal const float ColStep = 56f;
    internal const float K108Gap = 6f;
    internal const float K108TopY = 580f;
    internal const float K108RightShift = 4f * ColStep;
    internal const float K108RowStep = 56f;
    internal static readonly (int Idx, float X, float Y, float W)[] Full108Table =
    {
        (0, 0f, 580f, 50f), (1, 100f, 580f, 50f), (2, 150f, 580f, 50f), (3, 200f, 580f, 50f),
        (4, 250f, 580f, 50f), (5, 325f, 580f, 50f), (6, 375f, 580f, 50f), (7, 425f, 580f, 50f),
        (8, 475f, 580f, 50f), (9, 550f, 580f, 50f), (10, 600f, 580f, 50f), (11, 650f, 580f, 50f),
        (12, 700f, 580f, 50f), (13, 750f, 580f, 50f), (14, 800f, 580f, 50f), (15, 850f, 580f, 50f),
        (16, 900f, 580f, 50f),
        (17, 0f, 524f, 50f), (18, 50f, 524f, 50f), (19, 100f, 524f, 50f), (20, 150f, 524f, 50f),
        (21, 200f, 524f, 50f), (22, 250f, 524f, 50f), (23, 300f, 524f, 50f), (24, 350f, 524f, 50f),
        (25, 400f, 524f, 50f), (26, 450f, 524f, 50f), (27, 500f, 524f, 50f), (28, 550f, 524f, 50f),
        (29, 600f, 524f, 50f), (30, 650f, 524f, 100f),
        (31, 0f, 468f, 75f), (32, 75f, 468f, 50f), (33, 125f, 468f, 50f), (34, 175f, 468f, 50f),
        (35, 225f, 468f, 50f), (36, 275f, 468f, 50f), (37, 325f, 468f, 50f), (38, 375f, 468f, 50f),
        (39, 425f, 468f, 50f), (40, 475f, 468f, 50f), (41, 525f, 468f, 50f), (42, 575f, 468f, 50f),
        (43, 625f, 468f, 50f), (44, 675f, 468f, 75f),
        (45, 0f, 412f, 87.5f), (46, 87.5f, 412f, 50f), (47, 137.5f, 412f, 50f), (48, 187.5f, 412f, 50f),
        (49, 237.5f, 412f, 50f), (50, 287.5f, 412f, 50f), (51, 337.5f, 412f, 50f), (52, 387.5f, 412f, 50f),
        (53, 437.5f, 412f, 50f), (54, 487.5f, 412f, 50f), (55, 537.5f, 412f, 50f), (56, 587.5f, 412f, 50f),
        (57, 637.5f, 412f, 112.5f),
        (58, 0f, 356f, 112.5f), (59, 112.5f, 356f, 50f), (60, 162.5f, 356f, 50f), (61, 212.5f, 356f, 50f),
        (62, 262.5f, 356f, 50f), (63, 312.5f, 356f, 50f), (64, 362.5f, 356f, 50f), (65, 412.5f, 356f, 50f),
        (66, 462.5f, 356f, 50f), (67, 512.5f, 356f, 50f), (68, 562.5f, 356f, 50f), (69, 612.5f, 356f, 137.5f),
        (70, 0f, 300f, 62.5f), (71, 62.5f, 300f, 62.5f), (72, 125f, 300f, 62.5f), (73, 187.5f, 300f, 312.5f),
        (74, 500f, 300f, 62.5f), (75, 562.5f, 300f, 62.5f), (76, 625f, 300f, 62.5f), (77, 687.5f, 300f, 62.5f),
        (78, 950f, 524f, 50f), (79, 950f, 468f, 50f), (80, 1000f, 524f, 50f), (81, 1000f, 468f, 50f),
        (82, 1050f, 524f, 50f), (83, 1050f, 468f, 50f), (84, 1000f, 356f, 50f), (85, 950f, 300f, 50f),
        (86, 1000f, 300f, 50f), (87, 1050f, 300f, 50f),
        (88, 1100f, 524f, 50f), (89, 1150f, 524f, 50f), (90, 1200f, 524f, 50f), (91, 1250f, 524f, 50f),
        (92, 1100f, 468f, 50f), (93, 1150f, 468f, 50f), (94, 1200f, 468f, 50f), (95, 1250f, 468f, 50f),
        (96, 1100f, 412f, 50f), (97, 1150f, 412f, 50f), (98, 1200f, 412f, 50f),
        (99, 1100f, 356f, 50f), (100, 1150f, 356f, 50f), (101, 1200f, 356f, 50f),
        (102, 1100f, 300f, 100f), (103, 1200f, 300f, 50f), (104, 1250f, 300f, 50f),
    };
    internal static readonly (int Idx, float X, float YTop, float W, float H)[] Full75Table =
    {
        (0, 0f, 0f, 44f, 44f), (1, 98f, 0f, 44f, 44f), (2, 147f, 0f, 44f, 44f), (3, 196f, 0f, 44f, 44f),
        (4, 245f, 0f, 44f, 44f), (5, 316f, 0f, 44f, 44f), (6, 365f, 0f, 44f, 44f), (7, 414f, 0f, 44f, 44f),
        (8, 463f, 0f, 44f, 44f), (9, 539f, 0f, 44f, 44f), (10, 588f, 0f, 44f, 44f), (11, 637f, 0f, 44f, 44f),
        (12, 686f, 0f, 44f, 44f), (13, 740f, 0f, 44f, 44f),
        (14, 0f, 55f, 44f, 44f), (15, 49f, 55f, 44f, 44f), (16, 98f, 55f, 44f, 44f), (17, 147f, 55f, 44f, 44f),
        (18, 196f, 55f, 44f, 44f), (19, 245f, 55f, 44f, 44f), (20, 294f, 55f, 44f, 44f), (21, 343f, 55f, 44f, 44f),
        (22, 392f, 55f, 44f, 44f), (23, 441f, 55f, 44f, 44f), (24, 490f, 55f, 44f, 44f), (25, 539f, 55f, 44f, 44f),
        (26, 588f, 55f, 44f, 44f), (27, 637f, 55f, 93f, 44f), (28, 740f, 55f, 44f, 44f),
        (29, 0f, 105f, 66f, 44f), (30, 71f, 105f, 44f, 44f), (31, 120f, 105f, 44f, 44f), (32, 169f, 105f, 44f, 44f),
        (33, 218f, 105f, 44f, 44f), (34, 267f, 105f, 44f, 44f), (35, 316f, 105f, 44f, 44f), (36, 365f, 105f, 44f, 44f),
        (37, 414f, 105f, 44f, 44f), (38, 463f, 105f, 44f, 44f), (39, 512f, 105f, 44f, 44f), (40, 561f, 105f, 44f, 44f),
        (41, 610f, 105f, 44f, 44f), (42, 659f, 105f, 71f, 44f), (43, 740f, 105f, 44f, 44f),
        (44, 0f, 155f, 77f, 44f), (45, 82f, 155f, 44f, 44f), (46, 131f, 155f, 44f, 44f), (47, 180f, 155f, 44f, 44f),
        (48, 229f, 155f, 44f, 44f), (49, 278f, 155f, 44f, 44f), (50, 327f, 155f, 44f, 44f), (51, 376f, 155f, 44f, 44f),
        (52, 425f, 155f, 44f, 44f), (53, 474f, 155f, 44f, 44f), (54, 523f, 155f, 44f, 44f), (55, 572f, 155f, 44f, 44f),
        (56, 621f, 155f, 109f, 44f), (57, 740f, 155f, 45f, 44f),
        (58, 0f, 205f, 99f, 44f), (59, 104f, 205f, 44f, 44f), (60, 153f, 205f, 44f, 44f), (61, 202f, 205f, 44f, 44f),
        (62, 251f, 205f, 44f, 44f), (63, 300f, 205f, 44f, 44f), (64, 349f, 205f, 44f, 44f), (65, 398f, 205f, 44f, 44f),
        (66, 447f, 205f, 44f, 44f), (67, 496f, 205f, 44f, 44f), (68, 545f, 205f, 44f, 44f), (69, 594f, 205f, 90f, 44f),
        (70, 690f, 205f, 44f, 44f), (71, 740f, 205f, 45f, 44f),
        (72, 0f, 255f, 55f, 44f), (73, 60f, 255f, 55f, 44f), (74, 120f, 255f, 55f, 44f), (75, 180f, 255f, 275f, 44f),
        (76, 460f, 255f, 55f, 44f), (77, 520f, 255f, 55f, 44f), (78, 580f, 255f, 55f, 44f), (79, 640f, 255f, 44f, 44f),
        (80, 690f, 255f, 44f, 44f), (81, 740f, 255f, 44f, 44f),
    };
    internal static readonly (int Idx, float X, float YTop, float W, float H)[] Full60Table =
    {
        (0, 0f, 0f, 44f, 44f), (1, 49f, 0f, 44f, 44f), (2, 98f, 0f, 44f, 44f), (3, 147f, 0f, 44f, 44f),
        (4, 196f, 0f, 44f, 44f), (5, 245f, 0f, 44f, 44f), (6, 294f, 0f, 44f, 44f), (7, 343f, 0f, 44f, 44f),
        (8, 392f, 0f, 44f, 44f), (9, 441f, 0f, 44f, 44f), (10, 490f, 0f, 44f, 44f), (11, 539f, 0f, 44f, 44f),
        (12, 588f, 0f, 44f, 44f), (13, 637f, 0f, 88f, 44f),
        (14, 0f, 50f, 66f, 44f), (15, 71f, 50f, 44f, 44f), (16, 120f, 50f, 44f, 44f), (17, 169f, 50f, 44f, 44f),
        (18, 218f, 50f, 44f, 44f), (19, 267f, 50f, 44f, 44f), (20, 316f, 50f, 44f, 44f), (21, 365f, 50f, 44f, 44f),
        (22, 414f, 50f, 44f, 44f), (23, 463f, 50f, 44f, 44f), (24, 512f, 50f, 44f, 44f), (25, 561f, 50f, 44f, 44f),
        (26, 610f, 50f, 44f, 44f), (27, 659f, 50f, 66f, 44f),
        (28, 0f, 100f, 77f, 44f), (29, 82f, 100f, 44f, 44f), (30, 131f, 100f, 44f, 44f), (31, 180f, 100f, 44f, 44f),
        (32, 229f, 100f, 44f, 44f), (33, 278f, 100f, 44f, 44f), (34, 327f, 100f, 44f, 44f), (35, 376f, 100f, 44f, 44f),
        (36, 425f, 100f, 44f, 44f), (37, 474f, 100f, 44f, 44f), (38, 523f, 100f, 44f, 44f), (39, 572f, 100f, 44f, 44f),
        (40, 621f, 100f, 104f, 44f),
        (41, 0f, 150f, 104f, 44f), (42, 109f, 150f, 44f, 44f), (43, 158f, 150f, 44f, 44f), (44, 207f, 150f, 44f, 44f),
        (45, 256f, 150f, 44f, 44f), (46, 305f, 150f, 44f, 44f), (47, 354f, 150f, 44f, 44f), (48, 403f, 150f, 44f, 44f),
        (49, 452f, 150f, 44f, 44f), (50, 501f, 150f, 44f, 44f), (51, 550f, 150f, 44f, 44f), (52, 600f, 150f, 125f, 44f),
        (53, 0f, 200f, 55f, 44f), (54, 60f, 200f, 55f, 44f), (55, 120f, 200f, 55f, 44f), (56, 180f, 200f, 305f, 44f),
        (57, 490f, 200f, 55f, 44f), (58, 550f, 200f, 55f, 44f), (59, 610f, 200f, 55f, 44f), (60, 670f, 200f, 55f, 44f),
    };
    internal const int Full60FnSlot = 58;


    internal static readonly (int[] Order, float[] W)[][] LayoutTable = BuildLayoutTable(false);
    internal static readonly (int[] Order, float[] W)[][] LayoutTableStd = BuildLayoutTable(true);

    private static (int[] Order, float[] W)[][] BuildLayoutTable(bool standard)
    {
        var defs = new[] { 8, 10, 12, 14, 16, 20, 24 };
        var list = new List<(int[] Order, float[] W)[]>();
        foreach (int id in defs)
        {
            var d = Layouts.Get(id);
            list.Add(standard && d.RowsStd != null ? d.RowsStd : d.Rows);
        }
        return list.ToArray();
    }

    public static int[][] GetLayoutRows(int layout, bool standardKeyWidth)
    {
        var def = Layouts.Get(layout);
        if (def.IsFullKeyboard) return Array.Empty<int[]>();

        var rows = (standardKeyWidth && def.SupportsStandardKeyWidth && def.RowsStd != null)
            ? def.RowsStd
            : def.Rows;
        var result = new int[rows.Length][];
        for (int r = 0; r < rows.Length; r++) result[r] = rows[r].Order;
        return result;
    }

}
