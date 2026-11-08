using JipperKeyViewer.Graphics;
using Vortice.Mathematics;

namespace JipperKeyViewer.Core;


public sealed class RainDrop
{
    public float elapsedMs;
    public float startY;
    public float speed;
    public float height;
    public float fadePx;
    public float width;
    public float drawH;
    public float renderH;


    public float finalSizeY;


    public float releaseY;
    public bool growing = true;


    public bool fadingOut;
    public float fadeTimer;
    public float alpha = 1f;
    public bool removed;

    public bool isGhost;
    public Color4 MainColor;
}


public readonly struct RainSnapshot
{
    public readonly int Slot;
    public readonly int Row;
    public readonly RainDrop[] Drops;
    public RainSnapshot(int slot, int row, RainDrop[] drops)
    {
        Slot = slot; Row = row; Drops = drops;
    }
}


public sealed class RainSystem
{
    private const float RainContainerHeight = 275f;

    private const int MaxLiveDropsPerKey = 64;

    private readonly List<RainDrop>[] _drops;
    private readonly List<int> _active = new();
    private readonly HashSet<int> _activeSet = new();
    private readonly object _lock = new();

    private readonly RainDrop[][] _snapshotDrops;
    private readonly List<RainSnapshot> _snapshotBuffer = new();


    public Func<int, (float Speed, float Height, float Width, float StartY, float FadePx)>? ParamsForRow;

    public Func<int, bool>? RowEnabled;


    public bool Enabled;

    public bool FadeOnRelease = true;

    public float FadeDuration = 0.5f;

    public Color4 Rain1Color { get; set; }
    public Color4 Rain2Color { get; set; }
    public Color4 Rain3Color { get; set; }


    public Func<int, Color4?>? ColorForSlot;


    public Func<int, float>? GhostStartYForRow;

    public Func<int, Color4>? GhostColorForRow;

    public RainSystem(int slotCount)
    {
        _drops = new List<RainDrop>[Math.Max(1, slotCount)];
        for (int i = 0; i < _drops.Length; i++) _drops[i] = new List<RainDrop>(2);
        _snapshotDrops = new RainDrop[_drops.Length][];
    }


    public void ClearAll()
    {
        lock (_lock)
        {
            _active.Clear();
            _activeSet.Clear();
            for (int i = 0; i < _drops.Length; i++) _drops[i].Clear();
        }
    }

    public bool HasActive
    {
        get { lock (_lock) return _active.Count > 0; }
    }


    public void Trigger(int slotIndex, int row, bool ghost = false)
    {
        lock (_lock)
        {
            if (!Enabled || slotIndex < 0 || slotIndex >= _drops.Length) return;
            if (RowEnabled != null && !RowEnabled(row)) return;
            var (speed, height, width, startY, fadePx) =
                ParamsForRow?.Invoke(row) ?? (100f, 275f, 50f, -223f, 40f);
            if (ghost)
            {
                startY = GhostStartYForRow?.Invoke(row) ?? startY;
                fadePx = 0f;
            }

            var list = _drops[slotIndex];
            if (list.Count >= MaxLiveDropsPerKey) list.RemoveAt(0);

            Color4 color;
            if (ghost) color = GhostColorForRow?.Invoke(row) ?? Rain1Color;
            else color = ColorForSlot?.Invoke(slotIndex)
                         ?? (row == 0 ? Rain1Color : row == 1 ? Rain2Color : Rain3Color);

            list.Add(new RainDrop
            {
                elapsedMs = 0f,
                startY = startY,
                speed = Math.Max(1e-3f, speed) / 300f,
                height = Math.Clamp(height, 1f, 2000f),
                fadePx = Math.Clamp(fadePx, 0f, 200f),
                width = Math.Clamp(width, 1f, 2000f),
                growing = true,
                alpha = 1f,
                isGhost = ghost,
                MainColor = color,
            });

            if (_activeSet.Add(slotIndex))
            {
                int insert = _active.Count;
                while (insert > 0 && _active[insert - 1] > slotIndex) insert--;
                _active.Insert(insert, slotIndex);
            }
        }
    }


    public void Release(int slotIndex, bool ghost = false)
    {
        lock (_lock)
        {
            if (slotIndex < 0 || slotIndex >= _drops.Length) return;
            var list = _drops[slotIndex];
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var d = list[i];
                if (d.removed || d.isGhost != ghost) continue;
                d.growing = false;
                d.releaseY = d.elapsedMs * d.speed;
                if (FadeOnRelease)
                {
                    d.fadingOut = true;
                    d.fadeTimer = 0f;
                }
                break;
            }
        }
    }


    public void Update(float dtMs)
    {
        if (dtMs <= 0f) return;
        lock (_lock)
        {
            if (_active.Count == 0) return;
            for (int a = _active.Count - 1; a >= 0; a--)
            {
                int si = _active[a];
                var list = _drops[si];
                for (int j = list.Count - 1; j >= 0; j--)
                {
                    var d = list[j];
                    if (d.removed) { list.RemoveAt(j); continue; }
                    StepDrop(d, dtMs, si);
                    if (d.removed) list.RemoveAt(j);
                }
                if (list.Count == 0)
                {
                    _activeSet.Remove(si);
                    _active.RemoveAt(a);
                }
            }
        }
    }

    private void StepDrop(RainDrop d, float dtMs, int slotIndex)
    {
        d.elapsedMs += dtMs;
        float y = d.elapsedMs * d.speed;
        if (d.growing)
        {


            d.finalSizeY = y;
            d.renderH = Math.Min(y, d.height);
        }
        else
        {


            if (y > d.height)
            {
                float sizeY = d.finalSizeY - (y - d.height);
                d.renderH = Math.Max(0f, sizeY);
                if (d.renderH <= 0f)
                {
                    d.removed = true;
                    return;
                }
            }

        }

        if (d.fadingOut) AdvanceFade(d, dtMs, slotIndex);
    }


    private void AdvanceFade(RainDrop d, float dtMs, int slotIndex)
    {
        d.fadeTimer += dtMs / 1000f;


        float dur = FadeDuration >= 0.2f ? FadeDuration : 0.2f;
        float t = Math.Clamp(d.fadeTimer / dur, 0f, 1f);
        d.alpha = 1f - (t * (2f - t));
        if (t >= 1f)
        {
            d.removed = true;
        }
    }


    public void Snapshot(List<RainSnapshot> result)
    {
        lock (_lock)
        {
            result.Clear();
            if (result.Capacity < _active.Count) result.Capacity = _active.Count;
            for (int i = 0; i < _active.Count; i++)
            {
                int si = _active[i];
                List<RainDrop> list = _drops[si];
                RainDrop[] arr = _snapshotDrops[si];
                if (arr == null || arr.Length != list.Count)
                {
                    arr = new RainDrop[list.Count];
                    _snapshotDrops[si] = arr;
                }
                list.CopyTo(arr);
                result.Add(new RainSnapshot(si, RowOf(si), arr));
            }
        }
    }


    public Func<int, int>? SlotRow;

    private int RowOf(int slotIndex) => SlotRow?.Invoke(slotIndex) ?? slotIndex % 3;

    public static float ContainerHeight => RainContainerHeight;
}
