using System.Drawing.Drawing2D;

namespace QRGrab;

internal sealed class SelectionOverlayForm : Form
{
    private static readonly Brush DimBrush = new SolidBrush(Color.FromArgb(120, Color.Black));
    private static readonly Brush InfoBrush = new SolidBrush(Color.FromArgb(230, 15, 23, 42));
    private static readonly Pen SelectionPen = new(Color.FromArgb(56, 189, 248), 2);

    private readonly Bitmap screenImage;
    private readonly Rectangle virtualBounds;
    private bool isDragging;
    private Point startPoint;
    private Point currentPoint;

    public SelectionOverlayForm(Bitmap screenImage, Rectangle virtualBounds)
    {
        this.screenImage = screenImage;
        this.virtualBounds = virtualBounds;

        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        Bounds = virtualBounds;
        Cursor = Cursors.Cross;
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.None;
        KeyPreview = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
    }

    public Rectangle SelectedRegion { get; private set; } = Rectangle.Empty;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        isDragging = true;
        startPoint = e.Location;
        currentPoint = e.Location;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!isDragging)
        {
            return;
        }

        currentPoint = e.Location;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (!isDragging || e.Button != MouseButtons.Left)
        {
            return;
        }

        isDragging = false;
        currentPoint = e.Location;
        var selection = GetSelectionRectangle();

        if (selection.Width < 6 || selection.Height < 6)
        {
            SelectedRegion = Rectangle.Empty;
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        SelectedRegion = new Rectangle(
            selection.X + virtualBounds.X,
            selection.Y + virtualBounds.Y,
            selection.Width,
            selection.Height);
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode != Keys.Escape)
        {
            return;
        }

        SelectedRegion = Rectangle.Empty;
        DialogResult = DialogResult.Cancel;
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.DrawImageUnscaled(screenImage, Point.Empty);
        e.Graphics.FillRectangle(DimBrush, ClientRectangle);

        var selection = GetSelectionRectangle();
        if (selection.Width <= 0 || selection.Height <= 0)
        {
            DrawHint(e.Graphics);
            return;
        }

        e.Graphics.SetClip(selection);
        e.Graphics.DrawImageUnscaled(screenImage, Point.Empty);
        e.Graphics.ResetClip();

        var border = selection;
        border.Width -= 1;
        border.Height -= 1;
        e.Graphics.DrawRectangle(SelectionPen, border);
        DrawDimensions(e.Graphics, selection);
    }

    private Rectangle GetSelectionRectangle()
    {
        if (!isDragging && startPoint == currentPoint)
        {
            return Rectangle.Empty;
        }

        var left = Math.Min(startPoint.X, currentPoint.X);
        var top = Math.Min(startPoint.Y, currentPoint.Y);
        var right = Math.Max(startPoint.X, currentPoint.X);
        var bottom = Math.Max(startPoint.Y, currentPoint.Y);
        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private void DrawHint(Graphics graphics)
    {
        const string hint = "Drag to select a QR code. Esc cancels.";
        using var font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Regular);
        var size = TextRenderer.MeasureText(hint, font);
        var box = new Rectangle(24, 24, size.Width + 24, size.Height + 16);
        graphics.FillRectangle(InfoBrush, box);
        TextRenderer.DrawText(graphics, hint, font, box, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private static void DrawDimensions(Graphics graphics, Rectangle selection)
    {
        var text = $"{selection.Width} x {selection.Height}";
        using var font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular);
        var size = TextRenderer.MeasureText(text, font);
        var x = selection.Left;
        var y = selection.Top - size.Height - 10;

        if (y < 8)
        {
            y = selection.Bottom + 8;
        }

        var box = new Rectangle(x, y, size.Width + 16, size.Height + 8);
        graphics.FillRectangle(InfoBrush, box);
        TextRenderer.DrawText(graphics, text, font, box, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
