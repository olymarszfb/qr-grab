using System.Diagnostics;
using System.Text.RegularExpressions;

namespace QRGrab;

internal sealed class MainForm : Form
{
    private const string AppDisplayName = "QR Grab";
    private const int ScanHotKeyId = 0x5152;
    private static readonly Regex DomainLikeRegex = new(
        @"^[\w.-]+\.[A-Za-z]{2,}([/:?#].*)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Color AppBackground = Color.FromArgb(243, 246, 250);
    private static readonly Color Surface = Color.White;
    private static readonly Color Primary = Color.FromArgb(37, 99, 235);
    private static readonly Color PrimaryHover = Color.FromArgb(29, 78, 216);
    private static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);
    private static readonly Color TextMuted = Color.FromArgb(71, 85, 105);
    private static readonly Color Border = Color.FromArgb(203, 213, 225);

    private readonly Button scanButton = new();
    private readonly Button copyButton = new();
    private readonly Button openButton = new();
    private readonly CheckBox promptLinksCheckBox = new();
    private readonly TextBox resultTextBox = new();
    private readonly Label statusLabel = new();
    private readonly Label resultMetaLabel = new();
    private readonly ToolTip toolTip = new();
    private readonly NotifyIcon trayIcon = new();
    private readonly ContextMenuStrip trayMenu = new();

    private bool isScanning;
    private bool hotKeyRegistered;
    private bool isExiting;

    public MainForm()
    {
        InitializeUi();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RegisterScanHotKey();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (hotKeyRegistered)
        {
            NativeHotKey.UnregisterHotKey(Handle, ScanHotKeyId);
            hotKeyRegistered = false;
        }

        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeHotKey.WmHotKey && m.WParam.ToInt32() == ScanHotKeyId)
        {
            _ = BeginScanAsync();
            return;
        }

        base.WndProc(ref m);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (WindowState == FormWindowState.Minimized)
        {
            HideToTray($"{AppDisplayName} is still running. Press Ctrl+Alt+Q to scan.");
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!isExiting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray($"{AppDisplayName} is still running in the tray.");
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        trayIcon.Visible = false;
        trayIcon.Dispose();
        trayMenu.Dispose();
        toolTip.Dispose();
        base.OnFormClosed(e);
    }

    private void InitializeUi()
    {
        Text = AppDisplayName;
        Icon = LoadAppIcon();
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 470);
        Size = new Size(760, 520);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        BackColor = AppBackground;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(22),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 96,
            Margin = new Padding(0, 0, 0, 18),
            Padding = new Padding(18),
            BackColor = Surface
        };
        header.Paint += (_, e) => DrawPanelBorder(e.Graphics, header.ClientRectangle);

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = AppDisplayName,
            Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point(18, 14)
        };

        var subtitleLabel = new Label
        {
            AutoSize = true,
            Text = "Select any area on your screen, decode locally, then open or copy the result.",
            Font = new Font(Font.FontFamily, 9.5F, FontStyle.Regular),
            ForeColor = TextMuted,
            Location = new Point(20, 53)
        };

        var shortcutLabel = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = false,
            Text = "Ctrl + Alt + Q",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 64, 175),
            BackColor = Color.FromArgb(219, 234, 254),
            Size = new Size(118, 30),
            Location = new Point(header.Width - 140, 18)
        };
        header.Resize += (_, _) => shortcutLabel.Left = header.Width - shortcutLabel.Width - 18;

        header.Controls.Add(titleLabel);
        header.Controls.Add(subtitleLabel);
        header.Controls.Add(shortcutLabel);

        var commandRow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, 14),
            WrapContents = true
        };

        scanButton.Text = "Scan Region";
        scanButton.Size = new Size(126, 40);
        scanButton.Margin = new Padding(0, 0, 10, 0);
        scanButton.Click += async (_, _) => await BeginScanAsync();
        StyleButton(scanButton, primary: true);
        toolTip.SetToolTip(scanButton, "Start a screen region selection. Global hotkey: Ctrl+Alt+Q.");

        copyButton.Text = "Copy";
        copyButton.Size = new Size(96, 40);
        copyButton.Margin = new Padding(0, 0, 10, 0);
        copyButton.Enabled = false;
        copyButton.Click += (_, _) => CopyResultToClipboard();
        StyleButton(copyButton, primary: false);

        openButton.Text = "Open";
        openButton.Size = new Size(96, 40);
        openButton.Margin = new Padding(0, 0, 18, 0);
        openButton.Enabled = false;
        openButton.Click += (_, _) => OpenResult();
        StyleButton(openButton, primary: false);

        promptLinksCheckBox.AutoSize = true;
        promptLinksCheckBox.Checked = true;
        promptLinksCheckBox.Text = "Ask before opening links";
        promptLinksCheckBox.ForeColor = TextMuted;
        promptLinksCheckBox.Margin = new Padding(0, 11, 0, 0);
        toolTip.SetToolTip(promptLinksCheckBox, "When a QR code is a link, show a confirmation prompt before opening it.");

        commandRow.Controls.Add(scanButton);
        commandRow.Controls.Add(copyButton);
        commandRow.Controls.Add(openButton);
        commandRow.Controls.Add(promptLinksCheckBox);

        var resultHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 7)
        };
        resultHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        resultHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var resultLabel = new Label
        {
            AutoSize = true,
            Text = "Decoded result",
            Font = new Font(Font.FontFamily, 10F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Margin = new Padding(0)
        };

        resultMetaLabel.AutoSize = true;
        resultMetaLabel.Dock = DockStyle.Right;
        resultMetaLabel.Text = "No scan yet";
        resultMetaLabel.TextAlign = ContentAlignment.MiddleRight;
        resultMetaLabel.ForeColor = TextMuted;
        resultMetaLabel.Margin = new Padding(0);

        resultHeader.Controls.Add(resultLabel, 0, 0);
        resultHeader.Controls.Add(resultMetaLabel, 1, 0);

        var resultCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            Padding = new Padding(14),
            Margin = new Padding(0, 0, 0, 14)
        };
        resultCard.Paint += (_, e) => DrawPanelBorder(e.Graphics, resultCard.ClientRectangle);

        resultTextBox.Dock = DockStyle.Fill;
        resultTextBox.Multiline = true;
        resultTextBox.ReadOnly = true;
        resultTextBox.ScrollBars = ScrollBars.Vertical;
        resultTextBox.BorderStyle = BorderStyle.None;
        resultTextBox.BackColor = Surface;
        resultTextBox.ForeColor = TextPrimary;
        resultTextBox.Font = new Font("Cascadia Mono", 10F, FontStyle.Regular);
        resultTextBox.Text = "Scan a QR code to see the decoded text here.";
        resultTextBox.Margin = new Padding(0);

        statusLabel.AutoSize = true;
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.Text = "Ready. Press Ctrl+Alt+Q to scan from anywhere.";
        statusLabel.ForeColor = TextMuted;
        statusLabel.Margin = new Padding(0);

        resultCard.Controls.Add(resultTextBox);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(commandRow, 0, 1);
        root.Controls.Add(resultHeader, 0, 2);
        root.Controls.Add(resultCard, 0, 3);
        root.Controls.Add(statusLabel, 0, 4);

        Controls.Add(root);
        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        trayMenu.Items.Add("Scan Region", null, async (_, _) => await BeginScanAsync());
        trayMenu.Items.Add("Show", null, (_, _) => RestoreMainWindow());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, (_, _) => ExitApplication());

        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Icon = Icon;
        trayIcon.Text = AppDisplayName;
        trayIcon.Visible = true;
        trayIcon.DoubleClick += (_, _) => RestoreMainWindow();
    }

    private async Task BeginScanAsync()
    {
        if (isScanning)
        {
            return;
        }

        isScanning = true;
        scanButton.Enabled = false;
        statusLabel.Text = "Drag a rectangle around the QR code. Press Esc to cancel.";

        try
        {
            Hide();
            await Task.Delay(180);

            using var snapshot = ScreenCaptureService.CaptureVirtualScreen();
            using var selector = new SelectionOverlayForm(snapshot.Image, snapshot.Bounds);
            var dialogResult = selector.ShowDialog();

            RestoreMainWindow();

            if (dialogResult != DialogResult.OK || selector.SelectedRegion.IsEmpty)
            {
                statusLabel.Text = "Scan canceled.";
                return;
            }

            using var region = snapshot.Crop(selector.SelectedRegion);
            var decoded = QrDecoder.Decode(region);
            if (decoded is null)
            {
                SetResult(null, "No QR found");
                statusLabel.Text = "No QR code found in the selected region.";
                return;
            }

            SetResult(decoded.Text, decoded.Format.ToString());
            statusLabel.Text = $"Decoded {decoded.Format}.";
            MaybePromptToOpen(decoded.Text);
        }
        catch (Exception ex)
        {
            RestoreMainWindow();
            statusLabel.Text = "Scan failed.";
            MessageBox.Show(this, ex.Message, AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            scanButton.Enabled = true;
            isScanning = false;
        }
    }

    private void RegisterScanHotKey()
    {
        hotKeyRegistered = NativeHotKey.RegisterHotKey(
            Handle,
            ScanHotKeyId,
            GlobalHotKeyModifiers.Control | GlobalHotKeyModifiers.Alt | GlobalHotKeyModifiers.NoRepeat,
            Keys.Q);

        if (!hotKeyRegistered)
        {
            statusLabel.Text = "Ready. Ctrl+Alt+Q is already in use.";
        }
    }

    private void SetResult(string? value, string meta)
    {
        resultTextBox.Text = value ?? "Scan a QR code to see the decoded text here.";
        resultTextBox.ForeColor = value is null ? TextMuted : TextPrimary;
        resultMetaLabel.Text = meta;
        copyButton.Enabled = !string.IsNullOrWhiteSpace(value);
        openButton.Enabled = TryCreateOpenableUri(value, out _);
    }

    private void CopyResultToClipboard()
    {
        if (string.IsNullOrWhiteSpace(resultTextBox.Text))
        {
            return;
        }

        Clipboard.SetText(resultTextBox.Text);
        statusLabel.Text = "Copied.";
    }

    private void OpenResult()
    {
        if (!TryCreateOpenableUri(resultTextBox.Text, out var uri))
        {
            statusLabel.Text = "The decoded text is not an openable URL.";
            return;
        }

        OpenUri(uri);
        statusLabel.Text = "Opened decoded link.";
    }

    private void RestoreMainWindow()
    {
        if (!Visible)
        {
            Show();
        }

        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        Activate();
        BringToFront();
    }

    private void HideToTray(string message)
    {
        Hide();
        WindowState = FormWindowState.Normal;
        trayIcon.ShowBalloonTip(2500, AppDisplayName, message, ToolTipIcon.Info);
    }

    private void ExitApplication()
    {
        isExiting = true;
        Close();
    }

    private void MaybePromptToOpen(string decodedText)
    {
        if (!promptLinksCheckBox.Checked || !TryCreateOpenableUri(decodedText, out var uri))
        {
            return;
        }

        using var prompt = new OpenLinkPromptForm(uri, decodedText);
        var result = prompt.ShowDialog(this);

        if (result == DialogResult.OK)
        {
            OpenUri(uri);
            statusLabel.Text = "Opened decoded link.";
        }
        else if (result == DialogResult.Yes)
        {
            Clipboard.SetText(decodedText);
            statusLabel.Text = "Copied decoded link.";
        }
        else
        {
            statusLabel.Text = "Decoded link is ready.";
        }
    }

    private static void OpenUri(Uri uri)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = uri.AbsoluteUri,
            UseShellExecute = true
        });
    }

    private static bool TryCreateOpenableUri(string? value, out Uri uri)
    {
        uri = null!;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var candidate) && DomainLikeRegex.IsMatch(trimmed))
        {
            Uri.TryCreate($"https://{trimmed}", UriKind.Absolute, out candidate);
        }

        if (candidate is null)
        {
            return false;
        }

        if (candidate.Scheme is not ("http" or "https" or "mailto"))
        {
            return false;
        }

        uri = candidate;
        return true;
    }

    private static void StyleButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? Primary : Border;
        button.BackColor = primary ? Primary : Surface;
        button.ForeColor = primary ? Color.White : TextPrimary;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;

        button.MouseEnter += (_, _) =>
        {
            if (button.Enabled)
            {
                button.BackColor = primary ? PrimaryHover : Color.FromArgb(248, 250, 252);
            }
        };

        button.MouseLeave += (_, _) =>
        {
            button.BackColor = primary ? Primary : Surface;
        };
    }

    private static void DrawPanelBorder(Graphics graphics, Rectangle bounds)
    {
        bounds.Width -= 1;
        bounds.Height -= 1;
        using var pen = new Pen(Border);
        graphics.DrawRectangle(pen, bounds);
    }

    private static Icon LoadAppIcon()
    {
        return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Information;
    }
}
