using Godot;

// Small shared styling for ordinary Godot containers; no gameplay state here.
public static class UiLayout
{
    public static readonly Color Accent = new(0.25f, 0.9f, 0.85f);
    public static void Fill(Control control) => control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

    public static StyleBoxFlat PanelStyle()
    {
        var style = new StyleBoxFlat { BgColor = new Color(0.025f, 0.055f, 0.09f, 0.96f), BorderColor = new Color(0.15f, 0.4f, 0.46f) };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(12);
        style.ContentMarginLeft = style.ContentMarginRight = 28;
        style.ContentMarginTop = style.ContentMarginBottom = 24;
        return style;
    }

    public static VBoxContainer Card(Control parent, string name, float width)
    {
        var panel = new PanelContainer { Name = name, CustomMinimumSize = new Vector2(width, 0) };
        panel.AddThemeStyleboxOverride("panel", PanelStyle());
        parent.AddChild(panel);
        var box = new VBoxContainer { Name = "Content" };
        box.AddThemeConstantOverride("separation", 14);
        panel.AddChild(box);
        return box;
    }

    public static Label Text(Node parent, string name, string text, int size = 22)
    {
        var label = new Label { Name = name, Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        parent.AddChild(label);
        return label;
    }

    public static Button Button(Node parent, string name, string text, System.Action pressed)
    {
        var button = new Button { Name = name, Text = text, CustomMinimumSize = new Vector2(0, 52) };
        button.AddThemeFontSizeOverride("font_size", 22);
        parent.AddChild(button);
        button.Pressed += pressed;
        return button;
    }
}
