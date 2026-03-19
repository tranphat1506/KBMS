using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KBMS.CLI;

public class LineEditor
{
    private readonly StringBuilder _buffer = new();
    private int _cursorPos = 0;
    private int _historyIndex = -1;
    private List<string> _history = new();
    private int _escCount = 0;

    public string ReadLine(string prompt, List<string> history)
    {
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine() ?? string.Empty;
        }

        _buffer.Clear();
        _cursorPos = 0;
        _history = history;
        _historyIndex = _history.Count;
        _escCount = 0;
        
        Console.Write(prompt);

        while (true)
        {
            var keyInfo = Console.ReadKey(true);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return _buffer.ToString();
            }

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                _escCount++;
                if (_escCount >= 2)
                {
                    ClearCurrentLine(prompt);
                    _escCount = 0;
                }
                continue;
            }
            else
            {
                _escCount = 0;
            }

            switch (keyInfo.Key)
            {
                case ConsoleKey.LeftArrow:
                    if (_cursorPos > 0)
                    {
                        _cursorPos--;
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (_cursorPos < _buffer.Length)
                    {
                        _cursorPos++;
                        Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
                    }
                    break;

                case ConsoleKey.UpArrow:
                    NavigateHistory(-1, prompt);
                    break;

                case ConsoleKey.DownArrow:
                    NavigateHistory(1, prompt);
                    break;

                case ConsoleKey.Backspace:
                    if (_cursorPos > 0)
                    {
                        _cursorPos--;
                        _buffer.Remove(_cursorPos, 1);
                        RenderLine(prompt);
                    }
                    break;

                case ConsoleKey.Delete:
                    if (_cursorPos < _buffer.Length)
                    {
                        _buffer.Remove(_cursorPos, 1);
                        RenderLine(prompt);
                    }
                    break;
                
                case ConsoleKey.Home:
                    ResetCursorToStart(prompt);
                    break;

                case ConsoleKey.End:
                    SetCursorToEnd(prompt);
                    break;

                default:
                    if (!char.IsControl(keyInfo.KeyChar))
                    {
                        _buffer.Insert(_cursorPos, keyInfo.KeyChar);
                        _cursorPos++;
                        RenderLine(prompt);
                    }
                    break;
            }
        }
    }

    private void RenderLine(string prompt)
    {
        int currentLeft = Console.CursorLeft;
        int currentTop = Console.CursorTop;

        // Clean up from the prompt start
        Console.SetCursorPosition(0, currentTop);
        Console.Write(new string(' ', Console.WindowWidth - 1));
        
        // Rewrite prompt and buffer
        Console.SetCursorPosition(0, currentTop);
        Console.Write(prompt + _buffer.ToString());

        // Restore cursor
        Console.SetCursorPosition(prompt.Length + _cursorPos, currentTop);
    }

    private void NavigateHistory(int direction, string prompt)
    {
        if (_history.Count == 0) return;

        int nextIndex = _historyIndex + direction;
        if (nextIndex < 0 || nextIndex > _history.Count) return;

        _historyIndex = nextIndex;
        _buffer.Clear();
        
        if (_historyIndex < _history.Count)
        {
            _buffer.Append(_history[_historyIndex]);
        }
        
        _cursorPos = _buffer.Length;
        RenderLine(prompt);
    }

    private void ClearCurrentLine(string prompt)
    {
        _buffer.Clear();
        _cursorPos = 0;
        RenderLine(prompt);
    }
    
    private void ResetCursorToStart(string prompt)
    {
        _cursorPos = 0;
        Console.SetCursorPosition(prompt.Length, Console.CursorTop);
    }
    
    private void SetCursorToEnd(string prompt)
    {
        _cursorPos = _buffer.Length;
        Console.SetCursorPosition(prompt.Length + _cursorPos, Console.CursorTop);
    }
}
