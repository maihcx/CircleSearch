using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;

namespace CircleSearch.Overlay.Helpers;

public static class ScreenCapture
{
    [DllImport("user32.dll")] private static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hDC);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int w, int h);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObj);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr dest, int dX, int dY, int dW, int dH,
        IntPtr src, int sX, int sY, uint rop);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hDC);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObj);

    private const uint SRCCOPY = 0x00CC0020;

    /// <summary>Capture entire virtual screen (all monitors)</summary>
    public static Bitmap CaptureScreen()
    {
        int x = (int)SystemParameters.VirtualScreenLeft;
        int y = (int)SystemParameters.VirtualScreenTop;
        int w = (int)SystemParameters.VirtualScreenWidth;
        int h = (int)SystemParameters.VirtualScreenHeight;

        var desktop = GetDesktopWindow();
        var hdcSrc = GetWindowDC(desktop);
        var hdcDest = CreateCompatibleDC(hdcSrc);
        var hBitmap = CreateCompatibleBitmap(hdcSrc, w, h);
        var hOld = SelectObject(hdcDest, hBitmap);

        BitBlt(hdcDest, 0, 0, w, h, hdcSrc, x, y, SRCCOPY);

        SelectObject(hdcDest, hOld);
        DeleteDC(hdcDest);
        ReleaseDC(desktop, hdcSrc);

        // Convert sang managed Bitmap TRƯỚC khi DeleteObject
        // Image.FromHbitmap tạo unmanaged-backed bitmap → GC không biết size thật
        Bitmap managedBmp;
        using (var gdiBmp = Image.FromHbitmap(hBitmap))
        {
            // Clone sang PixelFormat chuẩn → fully managed memory
            managedBmp = new Bitmap(gdiBmp.Width, gdiBmp.Height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(managedBmp);
            g.DrawImage(gdiBmp, 0, 0);
        } // gdiBmp.Dispose() gọi ở đây → giải phóng unmanaged buffer

        DeleteObject(hBitmap); // xóa GDI handle
        return managedBmp;     // trả về fully managed bitmap
    }

    /// <summary>Crop a region from a bitmap</summary>
    public static Bitmap CropRegion(Bitmap source, System.Drawing.Rectangle rect)
    {
        // Clamp to source bounds
        rect.Intersect(new System.Drawing.Rectangle(0, 0, source.Width, source.Height));
        if (rect.Width <= 0 || rect.Height <= 0)
            return new Bitmap(1, 1);

        var cropped = new Bitmap(rect.Width, rect.Height);
        using var g = Graphics.FromImage(cropped);
        g.DrawImage(source, 0, 0, rect, GraphicsUnit.Pixel);
        return cropped;
    }

    /// <summary>Convert System.Drawing.Bitmap to WPF BitmapSource</summary>
    public static BitmapSource ToBitmapSource(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        ms.Position = 0;
        var bi = new BitmapImage();
        bi.BeginInit();
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.StreamSource = ms;
        bi.EndInit();
        bi.Freeze();
        return bi;
    }
}
