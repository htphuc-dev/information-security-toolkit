namespace information_security_toolkit.WinForms;

internal static class Ui
{
    public static TextBox TextBox(string name, bool readOnly = false, bool multiline = true) => new()
    {
        AccessibleName = name, Dock = DockStyle.Fill, Multiline = multiline,
        ReadOnly = readOnly, ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
        AcceptsReturn = multiline, WordWrap = true, Margin = new Padding(0, 0, 0, 8)
    };

    public static Label Label(string text) => new()
    {
        Text = text, AutoSize = true, Dock = DockStyle.Fill,
        Margin = new Padding(0, 3, 0, 6)
    };

    public static TableLayoutPanel Column(params (Control Control, float Weight)[] rows)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = rows.Length,
            Margin = Padding.Empty, AutoSize = true
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int index = 0; index < rows.Length; index++)
        {
            panel.RowStyles.Add(new RowStyle(rows[index].Weight > 0 ? SizeType.Percent : SizeType.AutoSize,
                rows[index].Weight));
            panel.Controls.Add(rows[index].Control, 0, index);
        }
        return panel;
    }

    public static Control Field(string caption, Control control) => Column((Label(caption), 0), (control, 100));

    public static FlowLayoutPanel Buttons(params Control[] buttons)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = Padding.Empty
        };
        panel.Controls.AddRange(buttons);
        return panel;
    }

    public static TabPage Tab(string title, Control content)
    {
        var page = new TabPage(title) { Padding = new Padding(14), UseVisualStyleBackColor = true };
        page.Controls.Add(content);
        return page;
    }

    public static Control PathRow(TextBox field, Button browse)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = Padding.Empty };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.Controls.Add(field, 0, 0);
        panel.Controls.Add(browse, 1, 0);
        return panel;
    }
}
