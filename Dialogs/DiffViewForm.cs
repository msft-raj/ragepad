using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RagePad.Dialogs;

/// <summary>
/// Side-by-side diff view using two Scintilla editors.
/// </summary>
public sealed class DiffViewForm : Form
{
    private readonly Scintilla _leftEditor;
    private readonly Scintilla _rightEditor;
    private readonly Label _leftLabel;
    private readonly Label _rightLabel;
    private readonly ToolStrip _toolStrip;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;
    private readonly ToolStripStatusLabel _navLabel;
    
    private bool _isSyncing;
    private readonly List<int> _diffLineNumbers = new();
    private int _currentDiffIndex = -1;
    
    // Marker numbers for line highlighting
    private const int MarkerDeleted = 20;
    private const int MarkerInserted = 21;
    private const int MarkerModified = 22;
    private const int MarkerImagery = 23;  // For imaginary lines (padding)

    // Colors
    private static readonly Color DeletedBackground = Color.FromArgb(255, 220, 220);
    private static readonly Color InsertedBackground = Color.FromArgb(220, 255, 220);
    private static readonly Color ModifiedBackground = Color.FromArgb(255, 255, 200);
    private static readonly Color ImageryBackground = Color.FromArgb(230, 230, 230);

    public DiffViewForm(string leftContent, string leftName, string rightContent, string rightName, Font editorFont)
    {
        Text = $"Compare: {leftName} ↔ {rightName}";
        Width = 1400;
        Height = 900;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;
        
        // Create toolbar
        _toolStrip = CreateToolStrip();
        
        // Create editors
        _leftEditor = CreateDiffEditor(editorFont);
        _rightEditor = CreateDiffEditor(editorFont);
        
        // Create labels
        _leftLabel = new Label
        {
            Text = leftName,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(240, 240, 240),
            Padding = new Padding(5, 0, 0, 0),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        
        _rightLabel = new Label
        {
            Text = rightName,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(240, 240, 240),
            Padding = new Padding(5, 0, 0, 0),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        
        // Status bar
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _navLabel = new ToolStripStatusLabel("") { AutoSize = false, Width = 100, TextAlign = ContentAlignment.MiddleRight };
        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.Items.Add(_navLabel);
        
        // Layout
        LayoutControls();
        
        // Synchronize scrolling
        _leftEditor.UpdateUI += OnLeftEditorScroll;
        _rightEditor.UpdateUI += OnRightEditorScroll;
        
        // Perform diff
        PerformDiff(leftContent, rightContent);
    }
    
    private ToolStrip CreateToolStrip()
    {
        var strip = new ToolStrip { ImageScalingSize = new Size(16, 16) };
        
        strip.Items.Add(new ToolStripButton("⏮ First", null, (s, e) => GoToFirstDiff()) { ToolTipText = "First Difference (Ctrl+Home)" });
        strip.Items.Add(new ToolStripButton("◀ Prev", null, (s, e) => GoToPreviousDiff()) { ToolTipText = "Previous Difference (F7)" });
        strip.Items.Add(new ToolStripButton("Next ▶", null, (s, e) => GoToNextDiff()) { ToolTipText = "Next Difference (F8)" });
        strip.Items.Add(new ToolStripButton("Last ⏭", null, (s, e) => GoToLastDiff()) { ToolTipText = "Last Difference (Ctrl+End)" });
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(new ToolStripLabel("F7/F8: Navigate | Esc: Close"));
        
        return strip;
    }

    private SplitContainer _splitContainer = null!;

    private void LayoutControls()
    {
        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 4
        };
        
        // Left panel
        var leftPanel = new Panel { Dock = DockStyle.Fill };
        leftPanel.Controls.Add(_leftEditor);
        leftPanel.Controls.Add(_leftLabel);
        _splitContainer.Panel1.Controls.Add(leftPanel);
        
        // Right panel
        var rightPanel = new Panel { Dock = DockStyle.Fill };
        rightPanel.Controls.Add(_rightEditor);
        rightPanel.Controls.Add(_rightLabel);
        _splitContainer.Panel2.Controls.Add(rightPanel);
        
        Controls.Add(_splitContainer);
        Controls.Add(_toolStrip);
        Controls.Add(_statusStrip);
    }
    
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Set splitter to 50% after form is shown and sized
        _splitContainer.SplitterDistance = _splitContainer.Width / 2;
    }

    private static Scintilla CreateDiffEditor(Font font)
    {
        var editor = new Scintilla
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            WrapMode = WrapMode.None,
            TabWidth = 4,
            BufferedDraw = false,
            Technology = Technology.DirectWrite
        };
        
        // Line numbers margin
        editor.Margins[0].Width = 50;
        editor.Margins[0].Type = MarginType.Number;
        
        // Configure markers for line backgrounds
        editor.Markers[MarkerDeleted].Symbol = MarkerSymbol.Background;
        editor.Markers[MarkerDeleted].SetBackColor(DeletedBackground);
        
        editor.Markers[MarkerInserted].Symbol = MarkerSymbol.Background;
        editor.Markers[MarkerInserted].SetBackColor(InsertedBackground);
        
        editor.Markers[MarkerModified].Symbol = MarkerSymbol.Background;
        editor.Markers[MarkerModified].SetBackColor(ModifiedBackground);
        
        editor.Markers[MarkerImagery].Symbol = MarkerSymbol.Background;
        editor.Markers[MarkerImagery].SetBackColor(ImageryBackground);
        
        // Apply font
        editor.StyleResetDefault();
        editor.Styles[Style.Default].Font = font.Name;
        editor.Styles[Style.Default].Size = (int)font.Size;
        editor.StyleClearAll();
        
        // Line number style
        editor.Styles[Style.LineNumber].ForeColor = Color.Gray;
        editor.Styles[Style.LineNumber].BackColor = Color.FromArgb(240, 240, 240);
        
        return editor;
    }

    private void PerformDiff(string leftContent, string rightContent)
    {
        var differ = new Differ();
        var builder = new SideBySideDiffBuilder(differ);
        var result = builder.BuildDiffModel(leftContent, rightContent);
        
        int insertedCount = 0;
        int deletedCount = 0;
        int modifiedCount = 0;
        
        // Build text and track line types for left side
        var leftText = new System.Text.StringBuilder();
        var leftLineTypes = new System.Collections.Generic.List<ChangeType>();
        
        foreach (var line in result.OldText.Lines)
        {
            leftText.AppendLine(line.Text ?? "");
            leftLineTypes.Add(line.Type);
            
            if (line.Type == ChangeType.Deleted) deletedCount++;
            else if (line.Type == ChangeType.Modified) modifiedCount++;
        }
        
        // Build text and track line types for right side
        var rightText = new System.Text.StringBuilder();
        var rightLineTypes = new System.Collections.Generic.List<ChangeType>();
        
        foreach (var line in result.NewText.Lines)
        {
            rightText.AppendLine(line.Text ?? "");
            rightLineTypes.Add(line.Type);
            
            if (line.Type == ChangeType.Inserted) insertedCount++;
        }
        
        // Set text (temporarily make non-readonly)
        _leftEditor.ReadOnly = false;
        _leftEditor.Text = leftText.ToString().TrimEnd('\r', '\n');
        _leftEditor.ReadOnly = true;
        
        _rightEditor.ReadOnly = false;
        _rightEditor.Text = rightText.ToString().TrimEnd('\r', '\n');
        _rightEditor.ReadOnly = true;
        
        // Apply line markers and track diff lines
        _diffLineNumbers.Clear();
        
        for (int i = 0; i < leftLineTypes.Count && i < _leftEditor.Lines.Count; i++)
        {
            var lineType = leftLineTypes[i];
            var line = _leftEditor.Lines[i];
            
            switch (lineType)
            {
                case ChangeType.Deleted:
                    line.MarkerAdd(MarkerDeleted);
                    if (_diffLineNumbers.Count == 0 || _diffLineNumbers[_diffLineNumbers.Count - 1] != i)
                        _diffLineNumbers.Add(i);
                    break;
                case ChangeType.Modified:
                    line.MarkerAdd(MarkerModified);
                    if (_diffLineNumbers.Count == 0 || _diffLineNumbers[_diffLineNumbers.Count - 1] != i)
                        _diffLineNumbers.Add(i);
                    break;
                case ChangeType.Imaginary:
                    line.MarkerAdd(MarkerImagery);
                    break;
            }
        }
        
        for (int i = 0; i < rightLineTypes.Count && i < _rightEditor.Lines.Count; i++)
        {
            var lineType = rightLineTypes[i];
            var line = _rightEditor.Lines[i];
            
            switch (lineType)
            {
                case ChangeType.Inserted:
                    line.MarkerAdd(MarkerInserted);
                    if (_diffLineNumbers.Count == 0 || _diffLineNumbers[_diffLineNumbers.Count - 1] != i)
                        _diffLineNumbers.Add(i);
                    break;
                case ChangeType.Modified:
                    line.MarkerAdd(MarkerModified);
                    break;
                case ChangeType.Imaginary:
                    line.MarkerAdd(MarkerImagery);
                    break;
            }
        }
        
        // Sort and remove duplicates
        _diffLineNumbers.Sort();
        
        // Update status
        _statusLabel.Text = $"Differences: {insertedCount} added, {deletedCount} deleted, {modifiedCount} modified";
        UpdateNavLabel();
    }

    private void OnLeftEditorScroll(object? sender, UpdateUIEventArgs e)
    {
        if (_isSyncing) return;
        if ((e.Change & UpdateChange.VScroll) == 0 && (e.Change & UpdateChange.HScroll) == 0) return;
        
        _isSyncing = true;
        try
        {
            // Sync vertical scroll
            int firstVisible = _leftEditor.FirstVisibleLine;
            _rightEditor.FirstVisibleLine = firstVisible;
            
            // Sync horizontal scroll
            int xOffset = _leftEditor.XOffset;
            _rightEditor.XOffset = xOffset;
        }
        finally
        {
            _isSyncing = false;
        }
    }
    
    private void OnRightEditorScroll(object? sender, UpdateUIEventArgs e)
    {
        if (_isSyncing) return;
        if ((e.Change & UpdateChange.VScroll) == 0 && (e.Change & UpdateChange.HScroll) == 0) return;
        
        _isSyncing = true;
        try
        {
            // Sync vertical scroll
            int firstVisible = _rightEditor.FirstVisibleLine;
            _leftEditor.FirstVisibleLine = firstVisible;
            
            // Sync horizontal scroll
            int xOffset = _rightEditor.XOffset;
            _leftEditor.XOffset = xOffset;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Escape:
                Close();
                return true;
            case Keys.F7:
                GoToPreviousDiff();
                return true;
            case Keys.F8:
                GoToNextDiff();
                return true;
            case Keys.Control | Keys.Home:
                GoToFirstDiff();
                return true;
            case Keys.Control | Keys.End:
                GoToLastDiff();
                return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    
    #region Navigation
    
    private void GoToFirstDiff()
    {
        if (_diffLineNumbers.Count == 0) return;
        _currentDiffIndex = 0;
        NavigateToCurrentDiff();
    }
    
    private void GoToLastDiff()
    {
        if (_diffLineNumbers.Count == 0) return;
        _currentDiffIndex = _diffLineNumbers.Count - 1;
        NavigateToCurrentDiff();
    }
    
    private void GoToNextDiff()
    {
        if (_diffLineNumbers.Count == 0) return;
        _currentDiffIndex++;
        if (_currentDiffIndex >= _diffLineNumbers.Count)
            _currentDiffIndex = 0; // Wrap around
        NavigateToCurrentDiff();
    }
    
    private void GoToPreviousDiff()
    {
        if (_diffLineNumbers.Count == 0) return;
        _currentDiffIndex--;
        if (_currentDiffIndex < 0)
            _currentDiffIndex = _diffLineNumbers.Count - 1; // Wrap around
        NavigateToCurrentDiff();
    }
    
    private void NavigateToCurrentDiff()
    {
        if (_currentDiffIndex < 0 || _currentDiffIndex >= _diffLineNumbers.Count) return;
        
        int lineNumber = _diffLineNumbers[_currentDiffIndex];
        
        // Scroll to make the line visible (centered if possible)
        _leftEditor.GotoPosition(_leftEditor.Lines[lineNumber].Position);
        _rightEditor.GotoPosition(_rightEditor.Lines[lineNumber].Position);
        
        // Center the line in view
        int linesOnScreen = _leftEditor.LinesOnScreen;
        int firstLine = Math.Max(0, lineNumber - linesOnScreen / 2);
        _leftEditor.FirstVisibleLine = firstLine;
        _rightEditor.FirstVisibleLine = firstLine;
        
        UpdateNavLabel();
    }
    
    private void UpdateNavLabel()
    {
        if (_diffLineNumbers.Count == 0)
        {
            _navLabel.Text = "No differences";
        }
        else if (_currentDiffIndex >= 0)
        {
            _navLabel.Text = $"Diff {_currentDiffIndex + 1} of {_diffLineNumbers.Count}";
        }
        else
        {
            _navLabel.Text = $"{_diffLineNumbers.Count} differences";
        }
    }
    
    #endregion
}
