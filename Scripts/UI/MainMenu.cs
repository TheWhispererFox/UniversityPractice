using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        var background = new ColorRect { Color = new Color(0.025f, 0.045f, 0.075f) };
        AddChild(background);
        UiLayout.Fill(background);
        var center = new CenterContainer();
        AddChild(center);
        UiLayout.Fill(center);
        var box = UiLayout.Card(center, "MenuCard", 540);
        UiLayout.Text(box, "Eyebrow", "UNIVERSITY PRACTICE / FLIGHT LAB", 18).Modulate = UiLayout.Accent;
        UiLayout.Text(box, "Title", "DRONE DELIVERY", 40);
        UiLayout.Text(box, "Description", "Collect cargo. Fly carefully. Complete every route.", 20);
        UiLayout.Text(box, "Controls", "WASD — move    Q/E — turn\nSpace / Shift — climb / descend\n1 / 2 / 3 — difficulty (starts a new run)", 18);
        UiLayout.Button(box, "StartGame", "Start Game", StartGame).GrabFocus();
        UiLayout.Button(box, "Exit", "Exit", () => GetTree().Quit());
    }

    public void StartGame() => GetTree().ChangeSceneToFile("res://Main.tscn");
}
