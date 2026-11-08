using System;
using System.Runtime.InteropServices;

namespace JipperKeyViewer.Graphics;


internal static class DirectComp
{
    private const string IID_IDCompositionDevice = "C37EA93A-E7AA-450D-B16F-9746CB0407F3";

    [DllImport("dcomp.dll", ExactSpelling = true, PreserveSig = true)]
    private static extern int DCompositionCreateDevice(
        IntPtr dxgiDevice,
        [MarshalAs(UnmanagedType.LPStruct)] Guid iid,
        out IntPtr dcompositionDevice);


    public static IDCompositionDevice? CreateDevice(IntPtr dxgiDevice)
    {
        try
        {
            var iid = new Guid(IID_IDCompositionDevice);
            int hr = DCompositionCreateDevice(dxgiDevice, iid, out IntPtr p);
            if (hr != 0 || p == IntPtr.Zero)
            {
                GraphicsLog.Write($"DCompositionCreateDevice 失败 HRESULT=0x{hr:X8}");
                return null;
            }
            return (IDCompositionDevice)Marshal.GetObjectForIUnknown(p);
        }
        catch (Exception ex)
        {
            GraphicsLog.Write("DCompositionCreateDevice 异常：" + ex.Message);
            return null;
        }
    }
}

[ComImport, Guid("C37EA93A-E7AA-450D-B16F-9746CB0407F3"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDCompositionDevice
{

    [PreserveSig] int Commit();

    [PreserveSig] int WaitForCommitCompletion();

    [PreserveSig] int GetFrameStatistics(IntPtr stats);

    [PreserveSig] int CreateTargetForHwnd(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool topmost,
        out IDCompositionTarget target);

    [PreserveSig] int CreateVisual(out IDCompositionVisual visual);
}

[ComImport, Guid("EACDD04C-117E-4E17-88F4-D1B12B0E3D89"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDCompositionTarget
{
    [PreserveSig] int SetRoot(IntPtr visual);
}

[ComImport, Guid("4D93059D-097B-4651-9A60-F0F25116E2F3"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDCompositionVisual
{
    [PreserveSig] int SetOffsetX(IntPtr animation);
    [PreserveSig] int SetOffsetXFloat(float offset);
    [PreserveSig] int SetOffsetY(IntPtr animation);
    [PreserveSig] int SetOffsetYFloat(float offset);
    [PreserveSig] int SetTransform(IntPtr animation);
    [PreserveSig] int SetTransformMatrix(IntPtr matrix);
    [PreserveSig] int SetTransformParent(IntPtr visual);
    [PreserveSig] int SetEffect(IntPtr effect);
    [PreserveSig] int SetBitmapInterpolationMode(int mode);
    [PreserveSig] int SetBorderMode(int mode);
    [PreserveSig] int SetClip(IntPtr rect);
    [PreserveSig] int SetClipAnimation(IntPtr animation);

    [PreserveSig] int SetContent(IntPtr content);
}
