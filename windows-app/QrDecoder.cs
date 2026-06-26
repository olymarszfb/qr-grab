using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.Common;

namespace QRGrab;

internal sealed record DecodedQr(string Text, BarcodeFormat Format);

internal static class QrDecoder
{
    private static readonly BarcodeReaderGeneric Reader = new()
    {
        AutoRotate = true,
        Options = new DecodingOptions
        {
            TryHarder = true,
            TryInverted = true,
            PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
        }
    };

    internal static DecodedQr? Decode(Bitmap bitmap)
    {
        if (bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            return null;
        }

        using var normalized = Normalize(bitmap);
        var pixels = CopyBgraPixels(normalized);
        var result = Reader.Decode(
            pixels,
            normalized.Width,
            normalized.Height,
            RGBLuminanceSource.BitmapFormat.BGRA32);

        return result is null ? null : new DecodedQr(result.Text, result.BarcodeFormat);
    }

    private static Bitmap Normalize(Bitmap source)
    {
        var normalized = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(normalized);
        graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
        graphics.DrawImage(source, new Rectangle(0, 0, normalized.Width, normalized.Height));
        return normalized;
    }

    private static byte[] CopyBgraPixels(Bitmap bitmap)
    {
        var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            const int bytesPerPixel = 4;
            var rowLength = bitmap.Width * bytesPerPixel;
            var pixels = new byte[rowLength * bitmap.Height];

            for (var y = 0; y < bitmap.Height; y++)
            {
                var sourceRow = IntPtr.Add(data.Scan0, y * data.Stride);
                Marshal.Copy(sourceRow, pixels, y * rowLength, rowLength);
            }

            return pixels;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
