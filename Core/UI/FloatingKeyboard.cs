using System;
using System.Collections.Generic;
using Godot;

namespace TableCore.Core.UI
{
	/// <summary>
	/// Visual floating keyboard composed of Godot buttons defined in the scene.
	/// </summary>
	public partial class FloatingKeyboard : Control
	{
		private readonly FloatingKeyboardModel _model = new();
		private readonly List<(Button Button, string Key)> _keyButtons = new();
		private VBoxContainer? _rowsContainer;
		private int _rowCount = 1;

		public event Action<string>? KeyPressed
		{
			add => _model.KeyPressed += value;
			remove => _model.KeyPressed -= value;
		}

		public event Action<string>? TextChanged
		{
			add => _model.TextChanged += value;
			remove => _model.TextChanged -= value;
		}

		public event Action<string>? TextCommitted
		{
			add => _model.TextCommitted += value;
			remove => _model.TextCommitted -= value;
		}

		/// <summary>
		/// Current text buffer maintained by the keyboard.
		/// </summary>
		public string Text => _model.Text;

		/// <summary>
		/// Number of logical rows detected in the keyboard layout.
		/// </summary>
		public int RowCount => _rowCount;

		public override void _Ready()
		{
			base._Ready();

			_rowsContainer = GetNodeOrNull<VBoxContainer>("Panel/Margin/Rows");
			CacheButtons(_rowsContainer != null ? (Node)_rowsContainer : this);
			_rowCount = Math.Max(1, _rowsContainer?.GetChildCount() ?? EstimateRowCount());

			UpdatePivotOffset();
		}

		public override void _Notification(int what)
		{
			base._Notification(what);

			if (what == NotificationResized)
			{
				UpdatePivotOffset();
			}
		}

		/// <summary>
		/// Updates the vertical size of each key to match the available HUD space.
		/// </summary>
		public void SetKeyHeight(float keyHeight)
		{
			var clamped = Mathf.Clamp(keyHeight, 20f, 48f);

			foreach (var (button, _) in _keyButtons)
			{
				var minSize = button.CustomMinimumSize;
				if (minSize == Vector2.Zero)
				{
					minSize = new Vector2(0f, clamped);
				}
				else
				{
					minSize.Y = clamped;
				}

				button.CustomMinimumSize = minSize;
			}
		}

		/// <summary>
		/// Clears the composed text buffer.
		/// </summary>
		public void Clear() => _model.Clear();

		/// <summary>
		/// Emits the current text to listeners.
		/// </summary>
		public void Commit() => _model.Commit();

		/// <summary>
		/// Replaces the keyboard text programmatically.
		/// </summary>
		public void SetText(string value) => _model.SetText(value);

		private void CacheButtons(Node parent)
		{
			if (parent is Button button)
			{
				var key = ResolveKey(button);
				if (!string.IsNullOrEmpty(key))
				{
					var capturedKey = key;
					button.FocusMode = FocusModeEnum.None;
					button.Pressed += () => OnKeyButtonPressed(capturedKey);
					_keyButtons.Add((button, capturedKey));
				}
			}

			foreach (var child in parent.GetChildren())
			{
				if (child is Node node)
				{
					CacheButtons(node);
				}
			}
		}

		private string ResolveKey(Button button)
		{
			if (button.HasMeta("Key"))
			{
				var meta = button.GetMeta("Key");
				if (meta.VariantType != Variant.Type.Nil)
				{
					return meta.AsString();
				}
			}

			if (!string.IsNullOrWhiteSpace(button.Text))
			{
				return button.Text switch
				{
					"Enter" => FloatingKeyboardSpecialKeys.Enter,
					"Clear" => FloatingKeyboardSpecialKeys.Clear,
					"⌫" or "Del" or "Back" => FloatingKeyboardSpecialKeys.Backspace,
					"Space" => FloatingKeyboardSpecialKeys.Space,
					_ => button.Text
				};
			}

			var nodeName = button.Name.ToString();
			if (!string.IsNullOrWhiteSpace(nodeName) && nodeName.StartsWith("Key_", StringComparison.Ordinal))
			{
				return nodeName.Substring(4);
			}

			return nodeName;
		}

		private int EstimateRowCount()
		{
			if (_keyButtons.Count == 0)
			{
				return 1;
			}

			var rowPositions = new HashSet<int>();
			foreach (var (button, _) in _keyButtons)
			{
				var y = Mathf.RoundToInt(button.GetGlobalPosition().Y);
				rowPositions.Add(y);
			}

			return Math.Max(1, rowPositions.Count);
		}

		private void OnKeyButtonPressed(string key)
		{
			_model.ApplyKey(key);
		}

		private void UpdatePivotOffset()
		{
			PivotOffset = Size / 2f;
		}
	}
}
