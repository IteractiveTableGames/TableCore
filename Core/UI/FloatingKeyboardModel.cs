using System;
using System.Text;

namespace TableCore.Core.UI
{
    /// <summary>
    /// Encapsulates the text buffer and input handling logic for the floating on-screen keyboard.
    /// </summary>
    public sealed class FloatingKeyboardModel
    {
        private readonly StringBuilder _buffer = new();

        /// <summary>
        /// Raised whenever a logical key (character or command) is activated.
        /// </summary>
        public event Action<string>? KeyPressed;

        /// <summary>
        /// Raised whenever the composed text changes.
        /// </summary>
        public event Action<string>? TextChanged;

        /// <summary>
        /// Raised when the current text is committed (e.g. Enter pressed).
        /// </summary>
        public event Action<string>? TextCommitted;

        /// <summary>
        /// Gets the current text accumulated by the keyboard.
        /// </summary>
        public string Text => _buffer.ToString();

        /// <summary>
        /// Applies the specified key to the text buffer, triggering relevant events.
        /// </summary>
        /// <param name="key">The logical key name.</param>
        public void ApplyKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            KeyPressed?.Invoke(key);

            switch (key)
            {
                case FloatingKeyboardSpecialKeys.Space:
                    Append(" ");
                    break;
                case FloatingKeyboardSpecialKeys.Backspace:
                    Backspace();
                    break;
                case FloatingKeyboardSpecialKeys.Clear:
                    Clear();
                    break;
                case FloatingKeyboardSpecialKeys.Enter:
                    Commit();
                    break;
                default:
                    Append(key);
                    break;
            }
        }

        /// <summary>
        /// Replaces the current text without signalling a key press.
        /// </summary>
        /// <param name="value">The new text value.</param>
        public void SetText(string value)
        {
            value ??= string.Empty;

            if (value.Equals(Text, StringComparison.Ordinal))
            {
                return;
            }

            _buffer.Clear();
            _buffer.Append(value);
            TextChanged?.Invoke(Text);
        }

        /// <summary>
        /// Clears the current text buffer.
        /// </summary>
        public void Clear()
        {
            if (_buffer.Length == 0)
            {
                return;
            }

            _buffer.Clear();
            TextChanged?.Invoke(string.Empty);
        }

        /// <summary>
        /// Emits the current text content to listeners.
        /// </summary>
        public void Commit()
        {
            TextCommitted?.Invoke(Text);
        }

        private void Append(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            _buffer.Append(value);
            TextChanged?.Invoke(Text);
        }

        private void Backspace()
        {
            if (_buffer.Length == 0)
            {
                return;
            }

            _buffer.Remove(_buffer.Length - 1, 1);
            TextChanged?.Invoke(Text);
        }
    }

    /// <summary>
    /// Provides the logical key names used by the floating keyboard.
    /// </summary>
    public static class FloatingKeyboardSpecialKeys
    {
        public const string Space = "Space";
        public const string Backspace = "Backspace";
        public const string Clear = "Clear";
        public const string Enter = "Enter";
    }
}
