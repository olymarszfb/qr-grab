namespace qr_screen_scanner_app;

internal sealed class OpenLinkPromptForm : Form
{
    private static readonly Color Background = Color.FromArgb(248, 250, 252);
    private static readonly Color Surface = Color.White;
    private static readonly Color Primary = Color.FromArgb(37, 99, 235);
    private static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);
    private static readonly Color TextMuted = Color.FromArgb(71, 85, 105);
    private static readonly Color Border = Color.FromArgb(203, 213, 225);

    public OpenLinkPromptForm(Uri uri, string rawValue)
    {
        Text = "Open decoded link?";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(520, 250);
        BackColor = Background;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20),
            BackColor = Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            AutoSize = true,
            Text = "Open this decoded link?",
            Font = new Font(Font.FontFamily, 14F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Margin = new Padding(0, 0, 0, 6)
        };

        var message = new Label
        {
            AutoSize = true,
            Text = $"Destination: {uri.Host}",
            ForeColor = TextMuted,
            Margin = new Padding(0, 0, 0, 14)
        };

        var valueBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Surface,
            ForeColor = TextPrimary,
            Font = new Font("Cascadia Mono", 9F, FontStyle.Regular),
            Text = rawValue,
            Margin = new Padding(0, 0, 0, 18),
            ScrollBars = ScrollBars.Vertical
        };

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0)
        };

        var openButton = CreateButton("Open", primary: true);
        openButton.DialogResult = DialogResult.OK;

        var copyButton = CreateButton("Copy", primary: false);
        copyButton.DialogResult = DialogResult.Yes;

        var cancelButton = CreateButton("Not now", primary: false);
        cancelButton.DialogResult = DialogResult.Cancel;

        actions.Controls.Add(openButton);
        actions.Controls.Add(copyButton);
        actions.Controls.Add(cancelButton);

        AcceptButton = openButton;
        CancelButton = cancelButton;

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(message, 0, 1);
        root.Controls.Add(valueBox, 0, 2);
        root.Controls.Add(actions, 0, 3);
        Controls.Add(root);
    }

    private static Button CreateButton(string text, bool primary)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(96, 36),
            Margin = new Padding(8, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Primary : Surface,
            ForeColor = primary ? Color.White : TextPrimary,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? Primary : Border;
        return button;
    }
}
