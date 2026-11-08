using JipperKeyViewer.Config;
using JipperKeyViewer.Core;
using JipperKeyViewer.Graphics;
using System.Collections.Concurrent;
using Vortice.Mathematics;
using static JipperKeyViewer.Render.LayoutGeometry;

namespace JipperKeyViewer.Render;


// Cached layout plus three frame phases: update state, draw keys, draw stats/rain.
public sealed class KeyRenderer
{

    private float RainZoneHeight(int maxRow)
    {
        float maxStartY = _cfg.RainStartYRow1;
        if (maxRow >= 1) maxStartY = Math.Max(maxStartY, _cfg.RainStartYRow2);
        if (maxRow >= 2) maxStartY = Math.Max(maxStartY, _cfg.RainStartYRow3);
        return 2f * RainContainerHeight + maxStartY;
    }


    private const float SlimHeight = 30f;
    private const float InlineKpsW = 77f;
    private const int BaseCols = 8;
    private const float RainContainerHeight = 275f;

    private const float K4KeyW = 4 * KeyUnit + 3 * Gap;
    private const float K4RowH = KeyUnit;
    private const float K4CanvasH = KeyUnit + Gap + K4RowH;
    private float _inlineKpsW = InlineKpsW;
    private float _rowCenterOffset;

    private readonly AppConfig _cfg;
    private readonly KeyboardHook _hook;

    private readonly Core.RainSystem _rain;
    private readonly List<RainSnapshot> _rainSnapshots = new();
    private volatile Dictionary<KeyCode, int> _slotByKey = new();
    private int[] _slotByKeyIdx = Array.Empty<int>();
    private readonly ConcurrentQueue<(bool Down, int Index)> _ghostEvents = new();
    private long _lastUpdateMs;
    private float _frameAlpha = 1f;

    public int WindowWidth { get; private set; } = 442;
    public int WindowHeight { get; private set; } = 56;

    private KeyCode[] _keys = Array.Empty<KeyCode>();
    private string[] _keyTexts = Array.Empty<string>();
    private KeySlot[] _slots = Array.Empty<KeySlot>();

    private float[] _pressAnim = Array.Empty<float>();

    private KeySlot[]? _stdSlots;

    public KeyRenderer(AppConfig cfg, KeyboardHook hook)
    {
        _cfg = cfg;
        _hook = hook;
        _rain = new Core.RainSystem(128)
        {
            ParamsForRow = row => row switch
            {
                0 => (_cfg.RainSpeedRow1, _cfg.RainHeightRow1, _cfg.RainWidthRow1, _cfg.RainStartYRow1, _cfg.EnableRainGradient ? _cfg.RainFadePx : 0f),
                1 => (_cfg.RainSpeedRow2, _cfg.RainHeightRow2, _cfg.RainWidthRow2, _cfg.RainStartYRow2, _cfg.EnableRainGradient ? _cfg.RainFadePx : 0f),
                _ => (_cfg.RainSpeedRow3, _cfg.RainHeightRow3, _cfg.RainWidthRow3, _cfg.RainStartYRow3, _cfg.EnableRainGradient ? _cfg.RainFadePx : 0f),
            },
            RowEnabled = row => row switch
            {
                0 => _cfg.EnableRainForRow1,
                1 => _cfg.EnableRainForRow2,
                _ => _cfg.EnableRainForRow3,
            },
            SlotRow = si => si >= 0 && si < _slots.Length ? _slots[si].Row : si % 3,
            Enabled = _cfg.RainEnabled,
            FadeOnRelease = _cfg.EnableRainFade,
            FadeDuration = _cfg.RainFadeDuration,
            Rain1Color = _cfg.ToColor(_cfg.Rain1Color),
            Rain2Color = _cfg.ToColor(_cfg.Rain2Color),
            Rain3Color = _cfg.ToColor(_cfg.Rain3Color),
        };
        _hook.KeyDown += OnKeyDown;
        _hook.KeyUp += OnKeyUp;
        _rain.ColorForSlot = si =>
        {
            if (!_cfg.PerKeyColor || si < 0 || si >= _slots.Length) return null;
            int pi = _slots[si].Idx;
            var arr = _cfg.PerKeyRainColor;
            if (pi < 0 || arr == null || pi >= arr.Length || arr[pi] == null || arr[pi].Length < 3) return null;
            return _cfg.ToColor(arr[pi]);
        };
        _rain.GhostStartYForRow = row => row switch
        {
            0 => _cfg.GhostRainStartYRow1,
            1 => _cfg.GhostRainStartYRow2,
            _ => _cfg.GhostRainStartYRow3,
        };
        _rain.GhostColorForRow = row => _cfg.ToColor(row switch
        {
            0 => _cfg.GhostRainColor1,
            1 => _cfg.GhostRainColor2,
            _ => _cfg.GhostRainColor3,
        });
        _hook.GhostKeyChanged += OnGhostKeyChanged;
        RefreshLayout();
    }


    public void RefreshLayout()
    {
        _hook.SetPerKeyKpsEnabled(_cfg.PerKeyKps);
        int layout = _cfg.KeyLayout;
        var def = Layouts.Get(layout);
        bool inlineInfo = def.InlineInfo;
        _keys = _cfg.CurrentKeys.Select(k => (KeyCode)k).ToArray();
        _keyTexts = _cfg.CurrentKeyTexts;
        _cfg.EnsurePerKeyColors(_keys.Length + 2);

        if (def.IsFullKeyboard)
        {
            _stdSlots = null;
            _rowCenterOffset = 0f;
            switch (def.Family)
            {
                case LayoutFamily.Full108: BuildFull108Slots(); break;
                case LayoutFamily.Full75: BuildFull75Slots(); break;
                default: BuildFull60Slots(); break;
            }
        }
        else if (LayoutGeometry.Is9K(layout))
        {
            _stdSlots = null;
            _rowCenterOffset = 0f;
            Build9KSlots();
        }
        else if (LayoutGeometry.Is7K(layout))
        {
            _stdSlots = null;
            _rowCenterOffset = 0f;
            Build7KSlots();
        }
        else if (LayoutGeometry.Is6K(layout))
        {
            _stdSlots = null;
            _rowCenterOffset = 0f;
            Build6KSlots();
        }
        else if (LayoutGeometry.Is5K(layout))
        {
            _stdSlots = null;
            _rowCenterOffset = 0f;
            Build5KSlots();
        }
        else if (LayoutGeometry.Is4K(layout))
        {
            _stdSlots = null;
            _rowCenterOffset = 0f;
            Build4KSlots();
        }
        else
        {
            int li = layout switch { 8 => 0, 10 => 1, 12 => 2, 14 => 3, 16 => 4, 20 => 5, 24 => 6, _ => 4 };
            _inlineKpsW = _cfg.StandardKeyWidth ? (layout == 10 ? 158f : 104f)
                : (layout == 10 ? 79f : InlineKpsW);
            bool useStd = _cfg.StandardKeyWidth && li is 1 or 2 or 5;
            var rows = useStd
                ? LayoutGeometry.LayoutTableStd[li switch { 1 => 0, 2 => 1, _ => 2 }]
                : LayoutGeometry.LayoutTable[li];
            var slots = new List<KeySlot>();
            _rowCenterOffset = 0f;
            if (inlineInfo)
            {
                var (lastOrder, lastW) = rows[rows.Length - 1];
                float lastRowW = 0f;
                for (int i = 0; i < lastOrder.Length; i++)
                    lastRowW += lastW[i] * KeyUnit + (i < lastOrder.Length - 1 ? Gap : 0f);
                float baseW = BaseCols * KeyUnit + (BaseCols - 1) * Gap;
                _ = Math.Max(0f, (baseW - (lastRowW + 2f * _inlineKpsW + 2f * Gap)) / 2f);
                _rowCenterOffset = 0f;
            }
            for (int row = 0; row < rows.Length; row++)
            {
                var (order, w) = rows[row];
                float rowW = 0f;
                for (int i = 0; i < order.Length; i++)
                    rowW += w[i] * KeyUnit + (i < order.Length - 1 ? Gap : 0f);
                float baseW = BaseCols * KeyUnit + (BaseCols - 1) * Gap;
                float offX = (inlineInfo && row == rows.Length - 1)
                    ? _rowCenterOffset + _inlineKpsW + Gap
                    : Math.Max(0f, (baseW - rowW) / 2f);

                float x = offX;
                float yCenter = row * (KeyUnit + Gap) + KeyUnit / 2f;
                for (int i = 0; i < order.Length; i++)
                {
                    int idx = order[i];
                    if (idx < _keys.Length)
                        slots.Add((_keys[idx], x, row, w[i] * KeyUnit, yCenter, KeyUnit, idx));
                    x += w[i] * KeyUnit + Gap;
                }
            }
            _slots = slots.ToArray();
            _stdSlots = null;
            if (def.SupportsStandardKeyWidth && def.RowsStd != null)
            {
                var stdRows = def.RowsStd;
                float stdKpsW = def.InlineKpsWStd;
                var stdList = new List<KeySlot>();
                for (int row = 0; row < stdRows.Length; row++)
                {
                    var (order, w) = stdRows[row];
                    float rowW = 0f;
                    for (int i = 0; i < order.Length; i++)
                        rowW += w[i] * KeyUnit + (i < order.Length - 1 ? Gap : 0f);
                    float baseW = BaseCols * KeyUnit + (BaseCols - 1) * Gap;
                    float offX = (inlineInfo && row == stdRows.Length - 1)
                        ? stdKpsW + Gap
                        : Math.Max(0f, (baseW - rowW) / 2f);
                    float x = offX;
                    float yCenter = row * (KeyUnit + Gap) + KeyUnit / 2f;
                    for (int i = 0; i < order.Length; i++)
                    {
                        int idx = order[i];
                        if (idx < _keys.Length)
                            stdList.Add((_keys[idx], x, row, w[i] * KeyUnit, yCenter, KeyUnit, idx));
                        x += w[i] * KeyUnit + Gap;
                    }
                }
                _stdSlots = stdList.ToArray();
            }
        }
        var map = new Dictionary<KeyCode, int>();
        for (int i = 0; i < _slots.Length; i++)
        {
            var k = _slots[i].Key;
            if (k != KeyCode.None && !map.ContainsKey(k)) map[k] = i;
        }
        _slotByKey = map;

        var byIdx = new int[_keys.Length];
        for (int i = 0; i < byIdx.Length; i++) byIdx[i] = -1;
        for (int i = 0; i < _slots.Length; i++)
        {
            int idx = _slots[i].Idx;
            if (idx >= 0 && idx < byIdx.Length && byIdx[idx] < 0) byIdx[idx] = i;
        }
        _slotByKeyIdx = byIdx;

        var ghostArr = _cfg.EnsureCurrentGhostKeys(_keys.Length);
        _hook.RegisterGhostKeys(ghostArr.Select(g => (KeyCode)g));

        _rain.Enabled = _cfg.RainEnabled && layout is not 108 and not 75 and not 60;
        _rain.FadeOnRelease = _cfg.EnableRainFade;
        _rain.FadeDuration = _cfg.RainFadeDuration;
        _rain.Rain1Color = _cfg.ToColor(_cfg.Rain1Color);
        _rain.Rain2Color = _cfg.ToColor(_cfg.Rain2Color);
        _rain.Rain3Color = _cfg.ToColor(_cfg.Rain3Color);
        _rain.ClearAll();

        try
        {
            Graphics.GraphicsLog.Write(
                $"婵″弶鍨濈紞鍛姜椤掆偓閸?keys=[{string.Join(",", _keys.Select(x => x.ToString()))}]");
            foreach (var sl in _slots)
                Graphics.GraphicsLog.Write($"婵″弶鍨濈紞?{sl.Key} X={sl.X:0.##} Row={sl.Row} W={sl.W:0.##}");
        }
        catch { }

        float s = 1f;
        bool info = _cfg.ShowKps || _cfg.ShowTotal;
        float width, height;
        if (def.FixedWidth.HasValue)
        {
            width = def.FixedWidth.Value * s;
            height = def.FixedHeight!.Value * s;
            if (_cfg.RainEnabled && def.SupportsRain)
            {
                int maxRowX = _slots.Length == 0 ? 0 : _slots.Max(x => x.Row);
                height += RainZoneHeight(maxRowX) * s;
            }
        }
        else
{
            int li2 = layout switch { 8 => 0, 10 => 1, 12 => 2, 14 => 3, 16 => 4, 20 => 5, 24 => 6, _ => 4 };
            bool useStd2 = _cfg.StandardKeyWidth && li2 is 1 or 2 or 5;
            var rows = useStd2
                ? LayoutGeometry.LayoutTableStd[li2 switch { 1 => 0, 2 => 1, _ => 2 }]
                : LayoutGeometry.LayoutTable[li2];
            width = BaseCols * KeyUnit * s + (BaseCols - 1) * Gap * s;
            height = rows.Length * KeyUnit * s + (rows.Length - 1) * Gap * s;
            if (info && !inlineInfo) height += 6f * s + SlimHeight * s;
            if (_cfg.RainEnabled)
            {
                int maxRow = _slots.Length == 0 ? 0 : _slots.Max(x => x.Row);
                height += RainZoneHeight(maxRow) * s;
            }
        }

        WindowWidth = (int)Math.Ceiling(width);
        WindowHeight = (int)Math.Ceiling(height);
    }

    private void Build4KSlots()
    {
        var list = new List<KeySlot>();
        float baseW = BaseCols * KeyUnit + (BaseCols - 1) * Gap;
        float offX = Math.Max(0f, (baseW - K4KeyW) / 2f);
        for (int i = 0; i < 4 && i < _keys.Length; i++)
            list.Add((_keys[i], offX + i * (KeyUnit + Gap), 0, KeyUnit, KeyUnit / 2f, KeyUnit, i));
        _slots = list.ToArray();
    }

    private void Build5KSlots()
    {
        var list = new List<KeySlot>();
        float baseW = BaseCols * KeyUnit + (BaseCols - 1) * Gap;
        float offX = Math.Max(0f, (baseW - K5KeyW) / 2f);
        for (int i = 0; i < 5 && i < _keys.Length; i++)
            list.Add((_keys[i], offX + i * (KeyUnit + Gap), 0, KeyUnit, KeyUnit / 2f, KeyUnit, i));
        _slots = list.ToArray();
    }

    private void Build6KSlots()
    {
        var list = new List<KeySlot>();
        float baseW = BaseCols * KeyUnit + (BaseCols - 1) * Gap;
        float offX = Math.Max(0f, (baseW - K6KeyW) / 2f);
        for (int i = 0; i < 6 && i < _keys.Length; i++)
            list.Add((_keys[i], offX + i * (KeyUnit + Gap), 0, KeyUnit, KeyUnit / 2f, KeyUnit, i));
        _slots = list.ToArray();
    }

    private void Build7KSlots()
    {
        var list = new List<KeySlot>();
        float baseW = BaseCols * KeyUnit + (BaseCols - 1) * Gap;
        float offX = Math.Max(0f, (baseW - K7KeyW) / 2f);
        for (int i = 0; i < 7 && i < _keys.Length; i++)
            list.Add((_keys[i], offX + i * (KeyUnit + Gap), 0, KeyUnit, KeyUnit / 2f, KeyUnit, i));
        _slots = list.ToArray();
    }

    private void Build9KSlots()
    {
        var list = new List<KeySlot>();
        float offX = 0f;
        for (int i = 0; i < 9 && i < _keys.Length; i++)
            list.Add((_keys[i], offX + i * (KeyUnit + Gap), 0, KeyUnit, KeyUnit / 2f, KeyUnit, i));
        _slots = list.ToArray();
    }

    private void BuildFull108Slots()
    {
        const float U = KeyUnit;
        var list = new List<KeySlot>();
        for (int i = 0; i < Full108Table.Length && i < _keys.Length; i++)
        {
            var (idx, ux, uy, uw) = Full108Table[i];
            if (idx < 0 || idx >= _keys.Length) continue;
            bool right = idx >= 78;
            float x = ux * ColStep / U - (right ? K108RightShift : 0f);
            float w = uw * ColStep / U - K108Gap;
            if (idx == 95 || idx == 104)
            {
                float h = K108RowStep + 50f;
                float yCenter = idx == 95 ? (468f + 412f) * 0.5f : (300f + 356f) * 0.5f;
                list.Add((_keys[idx], x, -1, w, K108TopY - yCenter + KeyUnit / 2f, h, idx));
            }
            else
            {
                list.Add((_keys[idx], x, -1, w, K108TopY - uy + KeyUnit / 2f, U, idx));
            }
        }
        _slots = list.ToArray();
    }

    private void BuildFull75Slots() => BuildFullSlots(Full75Table);

    private void BuildFull60Slots() => BuildFullSlots(Full60Table);


    private void BuildFullSlots((int Idx, float X, float YTop, float W, float H)[] table)
    {
        var list = new List<KeySlot>();
        for (int i = 0; i < table.Length && i < _keys.Length; i++)
        {
            var (idx, x, yTop, w, h) = table[i];
            if (idx < 0 || idx >= _keys.Length) continue;
            list.Add((_keys[idx], x, -1, w, yTop + h / 2f, h, idx));
        }
        _slots = list.ToArray();
    }


    public void Render(IRenderBackend r, float windowOpacity)
    {
        r.BeginFrame(new Color4(0, 0, 0, 0));
        _frameAlpha = windowOpacity;


        float scale = 1f;
        float offX = 0f, offY = 0f;
        if (r.ClientWidth > 0 && r.ClientHeight > 0)
        {
            float safe = Math.Max(0, _cfg.SafeArea);
            float availW = Math.Max(1f, r.ClientWidth - 2f * safe);
            float availH = Math.Max(1f, r.ClientHeight - 2f * safe);
            float glowBase = _cfg.GlowEnabled && _cfg.GlowSize > 0f ? Math.Max(2f, _cfg.GlowSize) : 0f;
            float contentW = WindowWidth + 2f * glowBase;
            float contentH = WindowHeight + 2f * glowBase;
            scale = Math.Min(availW / contentW, availH / contentH);
            offX = (r.ClientWidth - contentW * scale) / 2f + glowBase * scale;
            offY = (r.ClientHeight - contentH * scale) / 2f + glowBase * scale;
        }

        float s = 1f;
        bool k4 = LayoutGeometry.Is4K(_cfg.KeyLayout);
        bool k5 = LayoutGeometry.Is5K(_cfg.KeyLayout);
        float gap = Gap * s * scale;
        var layoutDef = Layouts.Get(_cfg.KeyLayout);
        bool isFull = layoutDef.IsFullKeyboard;
        int maxRow = isFull ? -1 : (_slots.Length == 0 ? 0 : _slots.Max(x => x.Row));
        bool inlineInfo = layoutDef.InlineInfo;

        bool info = (_cfg.ShowKps || _cfg.ShowTotal) && !_cfg.StreamerMode;
        float infoH = info ? ((k4 || k5) ? K4RowH : inlineInfo ? KeyUnit : SlimHeight) * s * scale : 0f;
        float rainH = !isFull && _cfg.RainEnabled ? RainZoneHeight(maxRow) * s * scale : 0f;
        float keysTop = rainH + (!inlineInfo && !isFull && !_cfg.DownLocation ? infoH + 6f * s * scale : 0f);
        float infoTop = rainH + (!inlineInfo && !isFull && _cfg.DownLocation ? (maxRow + 1) * (KeyUnit * s * scale + gap) - gap * s + 6f * s * scale : 0f);

        long now = Environment.TickCount64;
        if (_lastUpdateMs == 0) _lastUpdateMs = now;
        float dt = now - _lastUpdateMs;
        _lastUpdateMs = now;

        UpdateFrameState(isFull, dt);

        float outlineW = 1.5f * s * scale;

        Rect kpsRect = default, totalRect = default;
        float barCorner = 5f * s * scale;
        if (info && !isFull)
        {
            InitInfoRects(offX, offY, keysTop, infoTop, infoH, maxRow, inlineInfo, s, scale, gap,
                out kpsRect, out totalRect);
            DrawKeyGlow(r, kpsRect.X, kpsRect.Y, kpsRect.Width, kpsRect.Height,
                _cfg.ToColor(_cfg.EffKpsBackground), s, scale, windowOpacity);
            DrawKeyGlow(r, totalRect.X, totalRect.Y, totalRect.Width, totalRect.Height,
                _cfg.ToColor(_cfg.EffTotalBackground), s, scale, windowOpacity);
        }
        if (_cfg.GlowEnabled && _cfg.GlowSize > 0f)
        {
            for (int si = 0; si < _slots.Length; si++)
            {
                var gs = _slots[si];
                bool gPressed = gs.Key != KeyCode.None && IsPressed(gs.Key);
                Color4 gBody = gPressed ? _cfg.ToColor(_cfg.BackgroundClicked) : _cfg.ToColor(_cfg.Background);
                if (isFull && _cfg.P108UnifiedColor)
                    gBody = gPressed ? _cfg.ToColor(_cfg.P108BackgroundClicked) : _cfg.ToColor(_cfg.P108Background);
                DrawKeyGlow(r, offX + gs.X * s * scale,
                    offY + keysTop + gs.Y * s * scale - gs.H * s * scale / 2f,
                    gs.W * s * scale, gs.H * s * scale, gBody, s, scale, windowOpacity);
            }
        }

        DrawKeys(r, offX, offY, keysTop, s, scale, windowOpacity, isFull, outlineW);


        if (info && !isFull)
            DrawInfoBars(r, kpsRect, totalRect, barCorner, outlineW, s, scale, windowOpacity, inlineInfo, k4, k5);


        DrawRainLayer(r, offX, offY, keysTop, s, scale, windowOpacity, isFull);
    }


    private void UpdateFrameState(bool isFull, float dt)
    {
        if (_pressAnim.Length != _slots.Length) _pressAnim = new float[_slots.Length];
        if (_cfg.EnablePressAnimation)
        {
            float dur = Math.Clamp(_cfg.PressAnimationDuration, 10f, 2000f) / 1000f;
            float step = (dt / 1000f) / dur;
            for (int i = 0; i < _slots.Length; i++)
            {
                bool pr = _slots[i].Key != KeyCode.None && IsPressed(_slots[i].Key);
                if (pr) _pressAnim[i] = Math.Min(1f, _pressAnim[i] + step);
                else _pressAnim[i] = Math.Max(0f, _pressAnim[i] - step);
            }
        }
        else if (_pressAnim.Length > 0)
        {
            Array.Clear(_pressAnim, 0, _pressAnim.Length);
        }

        if (_cfg.RainEnabled && !isFull)
        {
            while (_rainEvents.TryDequeue(out var ev))
            {
                if (ev.Down) _rain.Trigger(ev.Slot, ev.Row);
                else _rain.Release(ev.Slot);
            }
            if (_cfg.EnableGhostRain)
            {
                while (_ghostEvents.TryDequeue(out var gev))
                {
                    int gi = gev.Index;
                    int si = gi >= 0 && gi < _slotByKeyIdx.Length ? _slotByKeyIdx[gi] : -1;
                    if (si < 0 || si >= _slots.Length) continue;
                    int grow = _slots[si].Row;
                    if (grow < 0) continue;
                    if (gev.Down) _rain.Trigger(si, grow, ghost: true);
                    else _rain.Release(si, ghost: true);
                }
            }
            else
            {
                while (_ghostEvents.TryDequeue(out _)) { }
            }
            _rain.Update(dt);
        }
        else
        {
            while (_rainEvents.TryDequeue(out _)) { }
            while (_ghostEvents.TryDequeue(out _)) { }
        }

    }


    private void DrawKeys(IRenderBackend r, float offX, float offY, float keysTop,
        float s, float scale, float windowOpacity, bool isFull, float outlineW)
    {
        for (int si = 0; si < _slots.Length; si++)
        {
            var sl = _slots[si];
            var (key, x0, row, w, yCenter, hSlot, _) = sl;
            bool pressed = key != KeyCode.None && IsPressed(key);
            float kscale = 1f;
            if (_cfg.EnablePressAnimation && si < _pressAnim.Length && _pressAnim[si] > 0f)
                kscale = Math.Clamp(
                    1f + (_cfg.PressAnimationScale - 1f) * Core.KvEasing.Ease(_cfg.Easing, _pressAnim[si]),
                    0.05f, 5f);
            float x = offX + x0 * s * scale;
            float y = offY + keysTop + yCenter * s * scale - hSlot * s * scale / 2f;
            float keyW = w * s * scale, keyH = hSlot * s * scale;
            float ix = (1f - kscale) * keyW / 2f, iy = (1f - kscale) * keyH / 2f;
            Rect cell = new(x + ix, y + iy, keyW * kscale, keyH * kscale);
            float corner = 5f * s * scale;

            Color4 bg = pressed ? _cfg.ToColor(_cfg.BackgroundClicked) : _cfg.ToColor(_cfg.Background);
            Color4 outline = pressed ? _cfg.ToColor(_cfg.OutlineClicked) : _cfg.ToColor(_cfg.Outline);
            Color4 text = pressed ? _cfg.ToColor(_cfg.TextClicked) : _cfg.ToColor(_cfg.Text);
            if (isFull && _cfg.P108UnifiedColor)
            {
                bg = pressed ? _cfg.ToColor(_cfg.P108BackgroundClicked) : _cfg.ToColor(_cfg.P108Background);
                outline = pressed ? _cfg.ToColor(_cfg.P108OutlineClicked) : _cfg.ToColor(_cfg.P108Outline);
                text = pressed ? _cfg.ToColor(_cfg.P108TextClicked) : _cfg.ToColor(_cfg.P108Text);
            }
            if (_cfg.PerKeyColor && !isFull && sl.Idx >= 0)
            {
                bg = PerKeyColorAt(_cfg.PerKeyBackground, _cfg.PerKeyBackgroundClicked, sl.Idx, pressed, bg);
                outline = PerKeyColorAt(_cfg.PerKeyOutline, _cfg.PerKeyOutlineClicked, sl.Idx, pressed, outline);
                text = PerKeyColorAt(_cfg.PerKeyText, _cfg.PerKeyTextClicked, sl.Idx, pressed, text);
            }

            bg = ScaleAlpha(bg, windowOpacity);
            outline = ScaleAlpha(outline, windowOpacity);
            text = ScaleAlpha(text, windowOpacity);

            if (_cfg.BgGradientEnabled)
                r.FillRoundRectGradient(cell,
                    ScaleAlpha(_cfg.ToColor(pressed ? _cfg.BgGradientClickedTop : _cfg.BgGradientTop), windowOpacity),
                    ScaleAlpha(_cfg.ToColor(pressed ? _cfg.BgGradientClickedBottom : _cfg.BgGradientBottom), windowOpacity),
                    corner);
            else
                r.FillRoundRect(cell, bg, corner);

            if (_cfg.OutlineGradientEnabled)
                r.DrawRoundRectGradient(cell,
                    ScaleAlpha(_cfg.ToColor(pressed ? _cfg.OutlineGradientClickedTop : _cfg.OutlineGradientTop), windowOpacity),
                    ScaleAlpha(_cfg.ToColor(pressed ? _cfg.OutlineGradientClickedBottom : _cfg.OutlineGradientBottom), windowOpacity),
                    corner, outlineW);
            else
                r.DrawRoundRect(cell, outline, corner, outlineW);

            string label;
            if (isFull)
            {
                label = _cfg.KeyLayout == 60 && sl.Idx == Full60FnSlot ? "Fn" : key.ToFullLabel();
            }
            else
            {
                string? custom = sl.Idx >= 0 && sl.Idx < _keyTexts.Length ? _keyTexts[sl.Idx] : null;
                label = !string.IsNullOrEmpty(custom) ? custom : key.ToLabel();
            }
            float labelFs = _cfg.KeyFontSize;
            if (!isFull && _cfg.PerKeyFontSizeEnabled && _cfg.PerKeyFontSize != null
                && sl.Idx >= 0 && sl.Idx < _cfg.PerKeyFontSize.Length
                && _cfg.PerKeyFontSize[sl.Idx] > 0)
                labelFs = _cfg.PerKeyFontSize[sl.Idx];
            if (!string.IsNullOrEmpty(label))
            {
                if (isFull || _cfg.HideMainKeyCount)
                {
                    r.DrawText(label, cell, text, labelFs * s * scale, maxWidth: cell.Width - 4f * s * scale,
                        TextAlign.Center, _cfg.FontStyleFlags, TextFx(labelFs * s * scale, pressed));
                }
                else
                {
                    Rect keyLabelRect = new(cell.X, cell.Y + 2f * s * scale, cell.Width, 32f * s * scale);
                    r.DrawText(label, keyLabelRect, text, labelFs * s * scale,
                        maxWidth: cell.Width - 4f * s * scale, TextAlign.Top, _cfg.FontStyleFlags,
                        TextFx(labelFs * s * scale, pressed));
                    long cnt = _cfg.PerKeyKps ? _hook.GetPerKeyKps(key) : _hook.GetKeyCount(key);
                    Rect keyCountRect = new(cell.X, cell.Y + cell.Height - 2f * s * scale - 16f * s * scale,
                        cell.Width, 16f * s * scale);
                    float countFs = Math.Min(labelFs, 16f / 1.2f) * s * scale;
                    r.DrawText(CountFormatter.Format(cnt, _cfg.ThousandSeparator), keyCountRect, text, countFs,
                        maxWidth: cell.Width - 4f * s * scale, TextAlign.Top, _cfg.FontStyleFlags,
                        TextFx(countFs, pressed));
                }
            }
        }    }


    private void DrawInfoBars(IRenderBackend r, Rect kpsRect, Rect totalRect, float barCorner,
        float outlineW, float s, float scale, float windowOpacity, bool inlineInfo, bool k4, bool k5)
    {
            DrawInfoBar(r, kpsRect, barCorner, outlineW, s, scale, windowOpacity,
                _cfg.ToColor(_cfg.EffKpsBackground), _cfg.ToColor(_cfg.EffKpsOutline));
            DrawInfoBar(r, totalRect, barCorner, outlineW, s, scale, windowOpacity,
                _cfg.ToColor(_cfg.EffTotalBackground), _cfg.ToColor(_cfg.EffTotalOutline));

            var kpsTextColor = ScaleAlpha(_cfg.ToColor(_cfg.EffKpsText), windowOpacity);
            var totalTextColor = ScaleAlpha(_cfg.ToColor(_cfg.EffTotalText), windowOpacity);
            float kpsFs = _cfg.KeyFontSize * s * scale, totalFs = _cfg.KeyFontSize * s * scale;
            if (_cfg.PerKeyFontSizeEnabled && _cfg.PerKeyFontSize != null && _cfg.PerKeyFontSize.Length >= 2)
            {
                if (_cfg.PerKeyFontSize[^2] > 0) kpsFs = _cfg.PerKeyFontSize[^2] * s * scale;
                if (_cfg.PerKeyFontSize[^1] > 0) totalFs = _cfg.PerKeyFontSize[^1] * s * scale;
            }
            if (_cfg.ShowKps)
                DrawInfoText(r, kpsRect, kpsTextColor, kpsFs, _cfg.KpsLabel, _hook.Kps, inlineInfo || k4 || k5,
                    _cfg.HideKpsTotalLabel, s, scale, _cfg.ThousandSeparator, _cfg.FontStyleFlags, TextFx(kpsFs));
            if (_cfg.ShowTotal)
                DrawInfoText(r, totalRect, totalTextColor, totalFs, _cfg.TotalLabel, _hook.TotalCount, inlineInfo || k4 || k5,
                    _cfg.HideKpsTotalLabel, s, scale, _cfg.ThousandSeparator, _cfg.FontStyleFlags, TextFx(totalFs));

    }


    private void DrawRainLayer(IRenderBackend r, float offX, float offY, float keysTop,
        float s, float scale, float windowOpacity, bool isFull)
    {
        if (!_cfg.RainEnabled || isFull) return;
        _rain.Snapshot(_rainSnapshots);
        foreach (var snap in _rainSnapshots)
        {
            if (snap.Slot < 0 || snap.Slot >= _slots.Length) continue;
            var sSlot = _slots[snap.Slot];
            if (sSlot.Key == KeyCode.None || sSlot.Row < 0) continue;

            float rainKs = 1f;
            if (_cfg.EnablePressAnimationOnRain && _cfg.EnablePressAnimation
                && snap.Slot < _pressAnim.Length && _pressAnim[snap.Slot] > 0f)
                rainKs = Math.Clamp(
                    1f + (_cfg.PressAnimationScale - 1f) * Core.KvEasing.Ease(_cfg.Easing, _pressAnim[snap.Slot]),
                    0.05f, 5f);

            float keyTopY = offY + keysTop + sSlot.Y * s * scale - sSlot.H * s * scale / 2f;
            float startY = sSlot.Row switch { 0 => _cfg.RainStartYRow1, 1 => _cfg.RainStartYRow2, _ => _cfg.RainStartYRow3 };
            float dropBottom = keyTopY - (RainContainerHeight + startY - KeyUnit) * s * scale;
            var g = (_stdSlots != null && snap.Slot < _stdSlots.Length) ? _stdSlots[snap.Slot] : sSlot;
            float cx = offX + (g.X + g.W / 2f) * s * scale;
            float rainW = sSlot.Row switch { 0 => _cfg.RainWidthRow1, 1 => _cfg.RainWidthRow2, _ => _cfg.RainWidthRow3 } * s * scale;
            float rainX = cx - rainW / 2f;

            foreach (var d in snap.Drops)
            {
                if (d.removed || d.renderH <= 0f || d.alpha <= 0f) continue;
                float y = d.elapsedMs * d.speed;
                float dFar = Math.Min(y, d.height);
                float dNear = dFar - d.renderH;
                float bottomY;
                if (d.growing)
                    bottomY = dropBottom;
                else if (y <= d.height)
                    bottomY = dropBottom - (y - d.releaseY) * s * scale;
                else
                    bottomY = dropBottom - d.height * s * scale + d.renderH * s * scale;
                float h = d.renderH * s * scale;
                if (h <= 0f) continue;
                Rect rainRect = new(rainX, bottomY - h, rainW, h);
                if (rainKs != 1f)
                {
                    float cxx = rainRect.X + rainRect.Width / 2f;
                    float cyy = rainRect.Y + rainRect.Height / 2f;
                    rainRect = new(cxx - rainRect.Width * rainKs / 2f, cyy - rainRect.Height * rainKs / 2f,
                        rainRect.Width * rainKs, rainRect.Height * rainKs);
                }

                float alpha = d.alpha * windowOpacity;
                if (alpha <= 0f) continue;
                var deco = RainDecor(snap.Row, s, scale, d.isGhost);
                if (deco.Shadow && deco.ShadowColor.A > 0f)
                    r.DrawRainBar(new Rect(rainRect.X + deco.ShadowX, rainRect.Y + deco.ShadowY,
                        rainRect.Width, rainRect.Height), deco.ShadowColor, alpha, dNear, dFar, d.height, d.fadePx);
                if (deco.Outline && deco.OutlineWidth > 0f)
                {
                    float ow = deco.OutlineWidth;
                    r.DrawRainBar(new Rect(rainRect.X - ow, rainRect.Y - ow,
                            rainRect.Width + ow * 2f, rainRect.Height + ow * 2f),
                        deco.OutlineColor, alpha, dNear, dFar, d.height, d.fadePx);
                }
                r.DrawRainBar(rainRect, d.MainColor, alpha, dNear, dFar, d.height, d.fadePx);
            }
        }
    }

    private Color4 PerKeyColorAt(float[][] normal, float[][] clicked, int idx, bool pressed, Color4 fallback)
    {
        var arr = pressed ? clicked : normal;
        if (arr == null || idx < 0 || idx >= arr.Length) return fallback;
        var c = arr[idx];
        return c == null || c.Length < 3 ? fallback : _cfg.ToColor(c);
    }


    private (bool Shadow, Color4 ShadowColor, float ShadowX, float ShadowY,
             bool Outline, Color4 OutlineColor, float OutlineWidth)
        RainDecor(int row, float s, float scale, bool isGhost = false)
    {
        float k = s * scale;
        if (isGhost)
        {
            if (row == 0)
                return (_cfg.EnableGhostRainShadowRow1, _cfg.ToColor(_cfg.GhostRainShadowColorRow1),
                        _cfg.GhostRainShadowOffsetXRow1 * k, _cfg.GhostRainShadowOffsetYRow1 * k,
                        _cfg.EnableGhostRainOutlineRow1, _cfg.ToColor(_cfg.GhostRainOutlineColorRow1),
                        Math.Max(0f, _cfg.GhostRainOutlineWidthRow1) * k);
            if (row == 1)
                return (_cfg.EnableGhostRainShadowRow2, _cfg.ToColor(_cfg.GhostRainShadowColorRow2),
                        _cfg.GhostRainShadowOffsetXRow2 * k, _cfg.GhostRainShadowOffsetYRow2 * k,
                        _cfg.EnableGhostRainOutlineRow2, _cfg.ToColor(_cfg.GhostRainOutlineColorRow2),
                        Math.Max(0f, _cfg.GhostRainOutlineWidthRow2) * k);
            return (_cfg.EnableGhostRainShadowRow3, _cfg.ToColor(_cfg.GhostRainShadowColorRow3),
                    _cfg.GhostRainShadowOffsetXRow3 * k, _cfg.GhostRainShadowOffsetYRow3 * k,
                    _cfg.EnableGhostRainOutlineRow3, _cfg.ToColor(_cfg.GhostRainOutlineColorRow3),
                    Math.Max(0f, _cfg.GhostRainOutlineWidthRow3) * k);
        }
        if (row == 0)
            return (_cfg.EnableRainShadowRow1, _cfg.ToColor(_cfg.RainShadowColorRow1),
                    _cfg.RainShadowOffsetXRow1 * k, _cfg.RainShadowOffsetYRow1 * k,
                    _cfg.EnableRainOutlineRow1, _cfg.ToColor(_cfg.RainOutlineColorRow1),
                    Math.Max(0f, _cfg.RainOutlineWidthRow1) * k);
        if (row == 1)
            return (_cfg.EnableRainShadowRow2, _cfg.ToColor(_cfg.RainShadowColorRow2),
                    _cfg.RainShadowOffsetXRow2 * k, _cfg.RainShadowOffsetYRow2 * k,
                    _cfg.EnableRainOutlineRow2, _cfg.ToColor(_cfg.RainOutlineColorRow2),
                    Math.Max(0f, _cfg.RainOutlineWidthRow2) * k);
        return (_cfg.EnableRainShadowRow3, _cfg.ToColor(_cfg.RainShadowColorRow3),
                _cfg.RainShadowOffsetXRow3 * k, _cfg.RainShadowOffsetYRow3 * k,
                _cfg.EnableRainOutlineRow3, _cfg.ToColor(_cfg.RainOutlineColorRow3),
                Math.Max(0f, _cfg.RainOutlineWidthRow3) * k);
    }

    private void InitInfoRects(float offX, float offY, float keysTop, float infoTop, float infoH,
        int maxRow, bool inlineInfo, float s, float scale, float gap,
        out Rect kpsRect, out Rect totalRect)
    {
        if (LayoutGeometry.Is9K(_cfg.KeyLayout) && _slots.Length > 0)
        {
            float gap9 = Gap * s * scale;
            float rowW9 = K9KeyW * s * scale;
            float barW9 = (rowW9 - gap9) / 2f;
            float x9 = offX + _slots[0].X * s * scale;
            float y9 = offY + infoTop;
            kpsRect = new Rect(x9, y9, barW9, infoH);
            totalRect = new Rect(x9 + barW9 + gap9, y9, barW9, infoH);
            return;
        }
        if (LayoutGeometry.Is7K(_cfg.KeyLayout) && _slots.Length > 0)
        {
            float gap7 = Gap * s * scale;
            float rowW7 = K7KeyW * s * scale;
            float barW7 = (rowW7 - gap7) / 2f;
            float x7 = offX + _slots[0].X * s * scale;
            float y7 = offY + infoTop;
            kpsRect = new Rect(x7, y7, barW7, infoH);
            totalRect = new Rect(x7 + barW7 + gap7, y7, barW7, infoH);
            return;
        }
        if (LayoutGeometry.Is6K(_cfg.KeyLayout) && _slots.Length > 0)
        {
            float gap6 = Gap * s * scale;
            float rowW6 = K6KeyW * s * scale;
            float barW6 = (rowW6 - gap6) / 2f;
            float x6 = offX + _slots[0].X * s * scale;
            float y6 = offY + infoTop;
            kpsRect = new Rect(x6, y6, barW6, infoH);
            totalRect = new Rect(x6 + barW6 + gap6, y6, barW6, infoH);
            return;
        }
        if (LayoutGeometry.Is5K(_cfg.KeyLayout) && _slots.Length > 0)
        {
            float unit5 = KeyUnit * s * scale;
            float gap5 = Gap * s * scale;
            float rowW5 = K5KeyW * s * scale;
            float barW5 = (rowW5 - gap5) / 2f;
            float x5 = offX + _slots[0].X * s * scale;
            float y5 = offY + infoTop;
            kpsRect = new Rect(x5, y5, barW5, infoH);
            totalRect = new Rect(x5 + barW5 + gap5, y5, barW5, infoH);
            return;
        }
        if (LayoutGeometry.Is4K(_cfg.KeyLayout) && _slots.Length > 0)
        {
            float unit = KeyUnit * s * scale;
            float barW = unit * 2f + gap;
            float x0 = offX + _slots[0].X * s * scale;
            float y = offY + infoTop;
            kpsRect = new Rect(x0, y, barW, infoH);
            totalRect = new Rect(x0 + 2f * (unit + gap), y, barW, infoH);
            return;
        }
        if (inlineInfo)
        {
            float infoY = offY + keysTop + maxRow * (KeyUnit * s * scale + gap);
            float iw = _inlineKpsW * s * scale;
            float rowOff = _rowCenterOffset * s * scale;
            kpsRect = new Rect(offX + rowOff, infoY, iw, infoH);
            totalRect = new Rect(offX + WindowWidth * scale - iw - rowOff, infoY, iw, infoH);
        }
        else
        {
            float barW = 212f * s * scale;
            kpsRect = new Rect(offX, offY + infoTop, barW, infoH);
            totalRect = new Rect(offX + barW + Gap * s * scale, offY + infoTop,
                WindowWidth * scale - barW - Gap * s * scale, infoH);
        }
    }

    private void DrawKeyGlow(IRenderBackend r, float x, float y, float w, float h,
        Color4 body, float s, float scale, float windowOpacity)
    {
        if (!_cfg.GlowEnabled || _cfg.GlowSize <= 0f) return;
        float pad = Math.Max(2f, _cfg.GlowSize) * s * scale;
        Color4 glow = _cfg.GlowFollowColor ? body : _cfg.ToColor(_cfg.GlowColor);
        float opacity = Math.Clamp(_cfg.GlowOpacity, 0, 100) / 100f;
        glow = ScaleAlpha(glow, opacity * windowOpacity);
        if (glow.A <= 0f) return;
        r.FillGlow(new Rect(x, y, w, h), pad, glow, 5f * s * scale);
    }

    private void DrawInfoBar(IRenderBackend r, Rect rect, float corner, float outlineW,
        float s, float scale, float windowOpacity, Color4 body, Color4 outline)
    {
        if (_cfg.BgGradientEnabled)
            r.FillRoundRectGradient(rect,
                ScaleAlpha(_cfg.ToColor(_cfg.BgGradientTop), windowOpacity),
                ScaleAlpha(_cfg.ToColor(_cfg.BgGradientBottom), windowOpacity), corner);
        else
            r.FillRoundRect(rect, ScaleAlpha(body, windowOpacity), corner);
        if (_cfg.OutlineGradientEnabled)
            r.DrawRoundRectGradient(rect,
                ScaleAlpha(_cfg.ToColor(_cfg.OutlineGradientTop), windowOpacity),
                ScaleAlpha(_cfg.ToColor(_cfg.OutlineGradientBottom), windowOpacity), corner, outlineW);
        else
            r.DrawRoundRect(rect, ScaleAlpha(outline, windowOpacity), corner, outlineW);
    }


    private static void DrawInfoText(IRenderBackend r, Rect rect, Color4 color, float fs,
        string label, long value, bool inlineInfo, bool hideLabel, float s, float scale,
        bool thousandSep, int fontStyleFlags, Graphics.TextStyleFx fx = default)
    {
        if (inlineInfo)
        {
            if (hideLabel)
            {
                Rect valueRect = TextLayout.InlineValueRect(rect, s, scale);
                r.DrawText(CountFormatter.Format(value, thousandSep), valueRect, color, fs,
                    maxWidth: rect.Width - 4f * s * scale, TextAlign.Center, fontStyleFlags, fx);
                return;
            }
            Rect topRect = TextLayout.InlineValueRect(rect, s, scale);
            r.DrawText(label, topRect, color, fs, maxWidth: rect.Width - 4f * s * scale, TextAlign.Top, fontStyleFlags, fx);
            Rect bottomRect = TextLayout.InlineCountRect(rect, s, scale);
            float cfs = TextLayout.InlineCountFontSize(fs, s, scale);
            r.DrawText(CountFormatter.Format(value, thousandSep), bottomRect, color, cfs,
                maxWidth: rect.Width - 4f * s * scale, TextAlign.Top, fontStyleFlags, fx);
        }
        else
        {
            float cfs = TextLayout.SlimFontSize(fs, s, scale);
            float baselineY = TextLayout.SharedBaseline(r, rect, cfs, fontStyleFlags);
            var (left, right) = TextLayout.SlimColumns(rect, s, scale);
            r.DrawText(label, left, color, cfs, maxWidth: Math.Max(8f, left.Width - 4f * s * scale),
                TextAlign.Left, fontStyleFlags, fx, baselineY: baselineY);
            r.DrawText(CountFormatter.Format(value, thousandSep), right, color, cfs,
                maxWidth: Math.Max(8f, right.Width - 4f * s * scale), TextAlign.Right, fontStyleFlags, fx,
                baselineY: baselineY);
        }
    }

    private Graphics.TextStyleFx TextFx(float fsPx, bool pressed = false)
    {
        var fx = new Graphics.TextStyleFx();
        if (fsPx <= 0f) return fx;
        if (_cfg.TextOutline && _cfg.TextOutlineWidth > 0f)
        {
            fx.Outline = true;
            fx.OutlineColor = _cfg.ToColor(_cfg.TextOutlineColor);
            fx.OutlinePx = _cfg.TextOutlineWidth * fsPx;
        }
        if (_cfg.TextShadow)
        {
            fx.Shadow = true;
            fx.ShadowColor = _cfg.ToColor(_cfg.TextShadowColor);
            fx.ShadowX = Math.Clamp(_cfg.ShadowOffsetX, -50f, 50f);
            fx.ShadowY = Math.Clamp(_cfg.ShadowOffsetY, -50f, 50f);
            fx.ShadowSoftPx = Math.Clamp(_cfg.ShadowSoftness, 0f, 64f);
        }
        if (_cfg.TextGradientEnabled)
        {
            fx.TextGradient = true;
            fx.GradientLeft = ScaleAlpha(
                _cfg.ToColor(pressed ? _cfg.TextGradientClickedLeft : _cfg.TextGradientLeft), _frameAlpha);
            fx.GradientRight = ScaleAlpha(
                _cfg.ToColor(pressed ? _cfg.TextGradientClickedRight : _cfg.TextGradientRight), _frameAlpha);
        }
        return fx;
    }

    private readonly ConcurrentQueue<(bool Down, int Slot, int Row)> _rainEvents = new();

    private void OnGhostKeyChanged(int index, bool down)
    {
        if (!_cfg.EnableGhostRain || !_cfg.RainEnabled) return;
        _ghostEvents.Enqueue((down, index));
    }

    private void OnKeyDown(KeyCode kc)
    {
        if (!_cfg.RainEnabled) return;
        if (_slotByKey.TryGetValue(kc, out int si))
        {
            if (_slots[si].Row >= 0)
                _rainEvents.Enqueue((true, si, _slots[si].Row));
        }
    }

    private void OnKeyUp(KeyCode kc)
    {
        if (!_cfg.RainEnabled) return;
        if (_slotByKey.TryGetValue(kc, out int si))
        {
            if (_slots[si].Row >= 0)
                _rainEvents.Enqueue((false, si, _slots[si].Row));
        }
    }

    private bool IsPressed(KeyCode k)
    {
        int vk = k.ToVk();
        if (vk == 0) return false;
        return _hook.Pressed.TryGetValue(vk, out bool p) && p;
    }

    private static Color4 ScaleAlpha(Color4 c, float a)
    {
        if (a >= 1f) return c;
        return new Color4(c.R, c.G, c.B, c.A * a);
    }
}
