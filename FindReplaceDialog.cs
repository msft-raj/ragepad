using ScintillaNET;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace RagePad;

internal sealed class FindReplaceDialog : Form
{
    private readonly MainForm _mainForm;
    private readonly TextBox _findText;
    private readonly TextBox _replaceText;
    private readonly CheckBox _matchCase;
    private readonly CheckBox _wholeWord;
    private readonly CheckBox _wrapAround;
    private readonly Button _findNext;
    private readonly Button _findPrev;
    private readonly Button _replace;
    private readonly Button _replaceAll;
    private readonly Label _replaceLabel;
    private readonly Label _statusLabel;
    private bool _showReplace;

    public FindReplaceDialog(MainForm mainForm, bool showReplace)
    {
        _mainForm = mainForm;
        _showReplace = showReplace;

        Text = showReplace ? "Replace" : "Find";
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.CenterParent;
        Width = 400;
        Height = showReplace ? 200 : 150;
        ShowInTaskbar = false;
        KeyPreview = true;

        // Find label + textbox
        var findLabel = new Label { Text = "Find:", Left = 10, Top = 15, Width = 60 };
        _findText = new TextBox { Left = 75, Top = 12, Width = 220 };
        _findText.KeyDown += FindText_KeyDown;

        // Replace label + textbox
        _replaceLabel = new Label { Text = "Replace:", Left = 10, Top = 45, Width = 60, Visible = showReplace };
        _replaceText = new TextBox { Left = 75, Top = 42, Width = 220, Visible = showReplace };

        // Options
        int optionsTop = showReplace ? 75 : 45;
        _matchCase = new CheckBox { Text = "Match case", Left = 75, Top = optionsTop, Width = 90 };
        _wholeWord = new CheckBox { Text = "Whole word", Left = 170, Top = optionsTop, Width = 90 };
        _wrapAround = new CheckBox { Text = "Wrap", Left = 265, Top = optionsTop, Width = 60, Checked = true };

        // Buttons
        int btnTop = showReplace ? 105 : 75;
        _findNext = new Button { Text = "Find Next", Left = 10, Top = btnTop, Width = 80 };
        _findPrev = new Button { Text = "Find Prev", Left = 95, Top = btnTop, Width = 80 };
        _replace = new Button { Text = "Replace", Left = 180, Top = btnTop, Width = 80, Visible = showReplace };
        _replaceAll = new Button { Text = "Replace All", Left = 265, Top = btnTop, Width = 80, Visible = showReplace };

        // Status
        _statusLabel = new Label { Left = 10, Top = btnTop + 30, Width = 300, ForeColor = Color.Gray };

        _findNext.Click += (s, e) => FindNext();
        _findPrev.Click += (s, e) => FindPrevious();
        _replace.Click += (s, e) => ReplaceNext();
        _replaceAll.Click += (s, e) => ReplaceAllOccurrences();

        Controls.AddRange(new Control[] {
            findLabel, _findText,
            _replaceLabel, _replaceText,
            _matchCase, _wholeWord, _wrapAround,
            _findNext, _findPrev, _replace, _replaceAll,
            _statusLabel
        });

        // Pre-fill with selected text
        var editor = _mainForm.CurrentEditor;
        if (editor != null && !string.IsNullOrEmpty(editor.SelectedText) && !editor.SelectedText.Contains('\n'))
        {
            _findText.Text = editor.SelectedText;
        }
        _findText.SelectAll();
    }

    private void FindText_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            if (e.Shift)
                FindPrevious();
            else
                FindNext();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    private SearchFlags GetSearchFlags()
    {
        var flags = SearchFlags.None;
        if (_matchCase.Checked) flags |= SearchFlags.MatchCase;
        if (_wholeWord.Checked) flags |= SearchFlags.WholeWord;
        return flags;
    }

    private void FindNext()
    {
        var editor = _mainForm.CurrentEditor;
        if (editor == null || string.IsNullOrEmpty(_findText.Text)) return;

        editor.SearchFlags = GetSearchFlags();
        editor.TargetStart = editor.CurrentPosition;
        editor.TargetEnd = editor.TextLength;

        int pos = editor.SearchInTarget(_findText.Text);
        if (pos < 0 && _wrapAround.Checked)
        {
            editor.TargetStart = 0;
            editor.TargetEnd = editor.CurrentPosition;
            pos = editor.SearchInTarget(_findText.Text);
        }

        if (pos >= 0)
        {
            editor.SetSelection(editor.TargetEnd, editor.TargetStart);
            editor.ScrollCaret();
            _statusLabel.Text = "";
        }
        else
        {
            _statusLabel.Text = "Not found";
        }
    }

    private void FindPrevious()
    {
        var editor = _mainForm.CurrentEditor;
        if (editor == null || string.IsNullOrEmpty(_findText.Text)) return;

        editor.SearchFlags = GetSearchFlags();
        
        // Search backwards from current selection start
        int searchStart = editor.SelectionStart > 0 ? editor.SelectionStart - 1 : editor.TextLength - 1;
        editor.TargetStart = searchStart;
        editor.TargetEnd = 0;

        int pos = editor.SearchInTarget(_findText.Text);
        if (pos < 0 && _wrapAround.Checked)
        {
            editor.TargetStart = editor.TextLength;
            editor.TargetEnd = searchStart;
            pos = editor.SearchInTarget(_findText.Text);
        }

        if (pos >= 0)
        {
            editor.SetSelection(editor.TargetEnd, editor.TargetStart);
            editor.ScrollCaret();
            _statusLabel.Text = "";
        }
        else
        {
            _statusLabel.Text = "Not found";
        }
    }

    private void ReplaceNext()
    {
        var editor = _mainForm.CurrentEditor;
        if (editor == null || string.IsNullOrEmpty(_findText.Text)) return;

        // If current selection matches search text, replace it
        if (string.Equals(editor.SelectedText, _findText.Text, 
            _matchCase.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
        {
            editor.ReplaceSelection(_replaceText.Text);
        }

        FindNext();
    }

    private void ReplaceAllOccurrences()
    {
        var editor = _mainForm.CurrentEditor;
        if (editor == null || string.IsNullOrEmpty(_findText.Text)) return;

        editor.SearchFlags = GetSearchFlags();
        int count = 0;

        editor.BeginUndoAction();
        try
        {
            editor.TargetStart = 0;
            editor.TargetEnd = editor.TextLength;

            while (editor.SearchInTarget(_findText.Text) >= 0)
            {
                editor.ReplaceTarget(_replaceText.Text);
                count++;
                editor.TargetStart = editor.TargetEnd;
                editor.TargetEnd = editor.TextLength;
            }
        }
        finally
        {
            editor.EndUndoAction();
        }

        _statusLabel.Text = $"Replaced {count} occurrence(s)";
    }
}
