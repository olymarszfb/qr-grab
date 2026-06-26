using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.Common;

namespace QRGrab;

internal static class QrSelfTest
{
    internal static bool Run()
    {
        const string payload = "https://example.test/qr-grab-self-test";

        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Height = 240,
                Width = 240,
                Margin = 2
            }
        };

        var pixelData = writer.Write(payload);
        using var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppArgb);
        var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            Marshal.Copy(pixelData.Pixels, 0, data.Scan0, pixelData.Pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return QrDecoder.Decode(bitmap)?.Text == payload;
    }
}
