using System.Drawing.Imaging;

namespace QRGrab;

internal sealed class ScreenSnapshot : IDisposable
{
    public ScreenSnapshot(Bitmap image, Rectangle bounds)
    {
        Image = image;
        Bounds = bounds;
    }

    public Bitmap Image { get; }

    public Rectangle Bounds { get; }

    public Bitmap Crop(Rectangle screenRegion)
    {
        var imageBounds = new Rectangle(Point.Empty, Image.Size);
        var sourceRegion = new Rectangle(
            screenRegion.X - Bounds.X,
            screenRegion.Y - Bounds.Y,
            screenRegion.Width,
            screenRegion.Height);

        sourceRegion.Intersect(imageBounds);
        if (sourceRegion.Width <= 0 || sourceRegion.Height <= 0)
        {
            throw new InvalidOperationException("The selected region is outside the captured screen area.");
        }

        var cropped = new Bitmap(sourceRegion.Width, sourceRegion.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(cropped);
        graphics.DrawImage(
            Image,
            new Rectangle(0, 0, cropped.Width, cropped.Height),
            sourceRegion,
            GraphicsUnit.Pixel);
        return cropped;
    }

    public void Dispose()
    {
        Image.Dispose();
    }
}

internal static class ScreenCaptureService
{
    internal static ScreenSnapshot CaptureVirtualScreen()
    {
        var bounds = GetVirtualScreenBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException("No screen area was available to capture.");
        }

        var image = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(image);
        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
        return new ScreenSnapshot(image, bounds);
    }

    private static Rectangle GetVirtualScreenBounds()
    {
        var screens = Screen.AllScreens;
        if (screens.Length == 0)
        {
            return Rectangle.Empty;
        }

        var bounds = screens[0].Bounds;
        foreach (var screen in screens.Skip(1))
        {
            bounds = Rectangle.Union(bounds, screen.Bounds);
        }

        return bounds;
    }
}
