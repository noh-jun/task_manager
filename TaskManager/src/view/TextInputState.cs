namespace TaskManager.View;

public sealed class TextInputState
{
    private string _text   = string.Empty;
    private int    _cursor = 0;

    public string Text           => _text;
    public int    CursorPosition => _cursor;

    public void SetText(string text)
    {
        _text   = text ?? string.Empty;
        _cursor = _text.Length;
    }

    public void Insert(char c)
    {
        _text = _text.Insert(_cursor, c.ToString());
        _cursor++;
    }

    public void DeleteBefore()
    {
        if (_cursor > 0)
        {
            _text = _text.Remove(_cursor - 1, 1);
            _cursor--;
        }
    }

    public void DeleteAt()
    {
        if (_cursor < _text.Length)
            _text = _text.Remove(_cursor, 1);
    }

    public void MoveLeft()
    {
        if (_cursor > 0) _cursor--;
    }

    public void MoveRight()
    {
        if (_cursor < _text.Length) _cursor++;
    }

    public void MoveToStart() => _cursor = 0;
    public void MoveToEnd()   => _cursor = _text.Length;

    public string ToDisplayText()
    {
        return _text[.._cursor] + "▌" + _text[_cursor..];
    }
}
