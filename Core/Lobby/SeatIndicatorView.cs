using Godot;
namespace TableCore.Lobby;

/// <summary>
/// Scene-driven seat indicator view that keeps the bar and name aligned regardless of rotation.
/// </summary>
public partial class SeatIndicatorView : Control
{
	private Panel? _body;
	private Label? _nameLabel;
	private StyleBoxFlat? _templateStyle;
	private Vector2 _templateSize = new Vector2(220f, 72f);

	public Vector2 TemplateSize => _templateSize;

	public override void _Ready()
	{
		InitializeNodes();
		_templateSize = Size;
		if (_templateSize == Vector2.Zero)
		{
			_templateSize = new Vector2(220f, 72f);
		}

		if (_body != null && _templateStyle == null)
		{
			_templateStyle = _body.GetThemeStylebox("panel") as StyleBoxFlat;
		}
	}

	public void Configure(string displayName, Color barColor)
	{
		InitializeNodes();

		Size = _templateSize;
		CustomMinimumSize = _templateSize;

		if (_body != null)
		{
			var style = (_templateStyle ?? _body.GetThemeStylebox("panel")) as StyleBoxFlat;
			if (style != null)
			{
				var clone = (StyleBoxFlat)style.Duplicate();
				clone.BgColor = barColor;
				_body.AddThemeStyleboxOverride("panel", clone);
			}
		}

		if (_nameLabel != null)
		{
			_nameLabel.Text = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName;
		}
	}

	private void InitializeNodes()
	{
		_body ??= GetNodeOrNull<Panel>("Body");
		_nameLabel ??= GetNodeOrNull<Label>("Body/NameLabel");
	}
}
