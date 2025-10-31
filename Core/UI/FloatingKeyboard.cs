using System;
using System.Collections.Generic;
using Godot;

namespace TableCore.Core.UI
{
    /// <summary>
    /// A reusable floating on-screen keyboard designed for touch HUDs.
    /// </summary>
    public partial class FloatingKeyboard : Control
    {
        private static readonly string[][] DefaultLayout =
        {
            new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
            new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" },
            new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" },
            new[] { "Z", "X", "C", "V", "B", "N", "M" },
            new[] { FloatingKeyboardSpecialKeys.Space, FloatingKeyboardSpecialKeys.Backspace, FloatingKeyboardSpecialKeys.Clear, FloatingKeyboardSpecialKeys.Enter }
        };

        private readonly FloatingKeyboardModel _model = new();
        private VBoxContainer? _rootContainer;
        private LineEdit? _textPreview;

        /// <summary>
        /// When true, a read-only text preview is displayed above the keyboard.
        /// </summary>
        [Export]
        public bool ShowPreview { get; set; } = true;

        /// <summary>
        /// Gets the current text composed by the keyboard.
        /// </summary>
        public string Text => _model.Text;

        /// <summary>
        /// Raised whenever a key is pressed.
        /// </summary>
        public event Action<string> KeyPressed
        {
            add => _model.KeyPressed += value;
            remove => _model.KeyPressed -= value;
        }

        /// <summary>
        /// Raised whenever the composed text changes.
        /// </summary>
        public event Action<string> TextChanged
        {
            add => _model.TextChanged += value;
            remove => _model.TextChanged -= value;
        }

        /// <summary>
        /// Raised when the composed text is committed.
        /// </summary>
        public event Action<string> TextCommitted
        {
            add => _model.TextCommitted += value;
            remove => _model.TextCommitted -= value;
        }

        public FloatingKeyboard()
        {
            _model.TextChanged += UpdatePreview;
        }

        public override void _Ready()
        {
            base._Ready();
            BuildKeyboardIfNeeded();
            UpdatePreview(_model.Text);
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
        /// Replaces the current text value.
        /// </summary>
        public void SetText(string value) => _model.SetText(value);

        /// <summary>
        /// Clears the text buffer.
        /// </summary>
        public void Clear() => _model.Clear();

        /// <summary>
        /// Emits the current text to listeners.
        /// </summary>
        public void Commit() => _model.Commit();

        private void BuildKeyboardIfNeeded()
        {
            if (_rootContainer != null)
            {
                return;
            }

            _rootContainer = new VBoxContainer
            {
                Name = "KeyboardRoot",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                Alignment = BoxContainer.AlignmentMode.Center
            };

            _rootContainer.AddThemeConstantOverride("separation", 12);
            AddChild(_rootContainer);

            if (ShowPreview)
            {
                _textPreview = new LineEdit
                {
                    Editable = false,
                    CaretBlink = true,
                    PlaceholderText = "Tap keys to enter text",
                    SizeFlagsHorizontal = SizeFlags.ExpandFill
                };
                _rootContainer.AddChild(_textPreview);
            }

            foreach (var row in DefaultLayout)
            {
                _rootContainer.AddChild(CreateRow(row));
            }
        }

        private HBoxContainer CreateRow(IEnumerable<string> keys)
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Alignment = BoxContainer.AlignmentMode.Center
            };

            row.AddThemeConstantOverride("separation", 8);
            foreach (var key in keys)
            {
                row.AddChild(CreateKeyButton(key));
            }

            return row;
        }

        private Button CreateKeyButton(string key)
        {
            var button = new Button
            {
                Text = GetDisplayText(key),
                FocusMode = FocusModeEnum.None,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                CustomMinimumSize = key == FloatingKeyboardSpecialKeys.Space
                    ? new Vector2(220, 80)
                    : new Vector2(90, 80)
            };

            if (key == FloatingKeyboardSpecialKeys.Space)
            {
                button.SizeFlagsStretchRatio = 3f;
            }
            else if (key is FloatingKeyboardSpecialKeys.Backspace or FloatingKeyboardSpecialKeys.Clear or FloatingKeyboardSpecialKeys.Enter)
            {
                button.SizeFlagsStretchRatio = 1.5f;
            }

            var capturedKey = key;
            button.Pressed += () => OnKeyButtonPressed(capturedKey);

            return button;
        }

        private void OnKeyButtonPressed(string key)
        {
            _model.ApplyKey(key);
        }

        private void UpdatePreview(string text)
        {
            if (_textPreview == null)
            {
                return;
            }

            _textPreview.Text = text;
            _textPreview.CaretColumn = text.Length;
        }
        
        private void UpdatePivotOffset()
        {
            PivotOffset = Size / 2f;
        }

        private static string GetDisplayText(string key)
        {
            return key switch
            {
                FloatingKeyboardSpecialKeys.Backspace => "⌫",
                FloatingKeyboardSpecialKeys.Space => "Space",
                FloatingKeyboardSpecialKeys.Clear => "Clear",
                FloatingKeyboardSpecialKeys.Enter => "Enter",
                _ => key
            };
        }
    }
}
