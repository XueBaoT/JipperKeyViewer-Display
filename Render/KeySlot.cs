using JipperKeyViewer.Core;

namespace JipperKeyViewer.Render;


internal readonly struct KeySlot
{
    public readonly KeyCode Key;
    public readonly float X;
    public readonly int Row;
    public readonly float W;
    public readonly float Y;
    public readonly float H;
    public readonly int Idx;

    public KeySlot(KeyCode key, float x, int row, float w, float y, float h, int idx)
    {
        Key = key;
        X = x;
        Row = row;
        W = w;
        Y = y;
        H = h;
        Idx = idx;
    }

    public static implicit operator KeySlot((KeyCode Key, float X, int Row, float W, float Y, float H, int Idx) t)
        => new(t.Key, t.X, t.Row, t.W, t.Y, t.H, t.Idx);

    public void Deconstruct(out KeyCode key, out float x, out int row, out float w, out float y, out float h, out int idx)
    {
        key = Key; x = X; row = Row; w = W; y = Y; h = H; idx = Idx;
    }
}
