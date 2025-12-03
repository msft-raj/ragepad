using ScintillaNET;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RagePad.Models;
using RagePad.Services;
using RagePad.Dialogs;

namespace RagePad;

/// <summary>
/// Main application window with tabbed text editing.
/// </summary>
public sealed class MainForm : Form, IMessageFilter
{
    #region Fields

    private readonly TabControl _tabs;
    private readonly MenuStrip _menu;
    private readonly ToolStrip _toolStrip;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;
    private readonly ToolStripStatusLabel _positionLabel;
    private readonly SessionManager _sessionManager;
    
    private Font _editorFont;
    private int _untitledCount;
    private bool _isLoading;

    // Compare fields
    private string? _compareFirstFileContent;
    private string? _compareFirstFileName;

    #endregion

    #region Constructor

    public MainForm()
    {
        InitializeForm();
        
        _sessionManager = new SessionManager();
        _editorFont = new Font("Consolas", 11f, FontStyle.Regular);

        _menu = CreateMenu();
        _toolStrip = CreateToolStrip();
        _tabs = CreateTabControl();
        (_statusStrip, _statusLabel, _positionLabel) = CreateStatusBar();

        LayoutControls();
        
        Application.AddMessageFilter(this);
        KeyPreview = true;
    }

    #endregion

    #region Initialization

    private void InitializeForm()
    {
        Text = "RagePad";
        Width = 1200;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;
        // DoubleBuffered = true; // Removed to prevent conflict with Scintilla painting

        SetWindowIcon();
    }

    private void SetWindowIcon()
    {
        var iconPath = Path.Combine(AppInfo.BaseDirectory, "RagePadLogo.png");
        if (File.Exists(iconPath))
        {
            using var img = Image.FromFile(iconPath);
            using var bmp = new Bitmap(img, 32, 32);
            Icon = Icon.FromHandle(bmp.GetHicon());
        }
    }

    private TabControl CreateTabControl()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f),
            Padding = new Point(12, 4)
        };
        
        tabs.SelectedIndexChanged += (s, e) => { UpdateTitle(); CurrentEditor?.Focus(); };
        tabs.MouseClick += Tabs_MouseClick;
        
        return tabs;
    }

    private (StatusStrip strip, ToolStripStatusLabel status, ToolStripStatusLabel position) CreateStatusBar()
    {
        var strip = new StatusStrip();
        var status = new ToolStripStatusLabel("Ready") 
        { 
            Spring = true, 
            TextAlign = ContentAlignment.MiddleLeft 
        };
        var position = new ToolStripStatusLabel("Ln 1, Col 1") 
        { 
            AutoSize = false, 
            Width = 120, 
            TextAlign = ContentAlignment.MiddleRight 
        };
        
        strip.Items.Add(status);
        strip.Items.Add(position);
        
        return (strip, status, position);
    }

    private void LayoutControls()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(_tabs);

        Controls.Add(panel);
        Controls.Add(_toolStrip);
        Controls.Add(_menu);
        Controls.Add(_statusStrip);
        MainMenuStrip = _menu;
    }

    #endregion

    #region Menu & Toolbar

    private MenuStrip CreateMenu()
    {
        var menu = new MenuStrip();

        menu.Items.Add(CreateFileMenu());
        menu.Items.Add(CreateEditMenu());
        menu.Items.Add(CreateViewMenu());
        menu.Items.Add(CreateCompareMenu());
        menu.Items.Add(CreateHelpMenu());

        return menu;
    }

    private ToolStripMenuItem _compareWithMenuItem = null!;

    private ToolStripMenuItem CreateCompareMenu()
    {
        var compare = new ToolStripMenuItem("&Compare");
        compare.DropDownItems.Add(new ToolStripMenuItem("Select &First to Compare", null, (s, e) => SelectFirstToCompare(), Keys.Control | Keys.D1));
        _compareWithMenuItem = new ToolStripMenuItem("Compare with First", null, (s, e) => CompareWithFirst(), Keys.Control | Keys.D2);
        _compareWithMenuItem.Enabled = false;
        compare.DropDownItems.Add(_compareWithMenuItem);
        compare.DropDownItems.Add(new ToolStripSeparator());
        compare.DropDownItems.Add(new ToolStripMenuItem("Clear Selection", null, (s, e) => ClearCompareSelection()));
        return compare;
    }

    private ToolStripMenuItem CreateFileMenu()
    {
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(new ToolStripMenuItem("&New", null, (s, e) => NewTab(), Keys.Control | Keys.N));
        file.DropDownItems.Add(new ToolStripMenuItem("&Open...", null, (s, e) => OpenFile(), Keys.Control | Keys.O));
        file.DropDownItems.Add(new ToolStripMenuItem("&Save", null, (s, e) => SaveFile(), Keys.Control | Keys.S));
        file.DropDownItems.Add(new ToolStripMenuItem("Save &As...", null, (s, e) => SaveFileAs(), Keys.Control | Keys.Shift | Keys.S));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("Close Tab", null, (s, e) => CloseTab(), Keys.Control | Keys.W));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (s, e) => Close(), Keys.Alt | Keys.F4));
        return file;
    }

    private ToolStripMenuItem CreateEditMenu()
    {
        var edit = new ToolStripMenuItem("&Edit");
        edit.DropDownItems.Add(new ToolStripMenuItem("&Undo", null, (s, e) => CurrentEditor?.Undo(), Keys.Control | Keys.Z));
        edit.DropDownItems.Add(new ToolStripMenuItem("&Redo", null, (s, e) => CurrentEditor?.Redo(), Keys.Control | Keys.Y));
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add(new ToolStripMenuItem("Cu&t", null, (s, e) => CurrentEditor?.Cut(), Keys.Control | Keys.X));
        edit.DropDownItems.Add(new ToolStripMenuItem("&Copy", null, (s, e) => CurrentEditor?.Copy(), Keys.Control | Keys.C));
        edit.DropDownItems.Add(new ToolStripMenuItem("&Paste", null, (s, e) => CurrentEditor?.Paste(), Keys.Control | Keys.V));
        edit.DropDownItems.Add(new ToolStripMenuItem("Select &All", null, (s, e) => CurrentEditor?.SelectAll(), Keys.Control | Keys.A));
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add(new ToolStripMenuItem("&Find...", null, (s, e) => ShowFindReplace(false), Keys.Control | Keys.F));
        edit.DropDownItems.Add(new ToolStripMenuItem("&Replace...", null, (s, e) => ShowFindReplace(true), Keys.Control | Keys.H));
        edit.DropDownItems.Add(new ToolStripMenuItem("&Go to Line...", null, (s, e) => ShowGoToLine(), Keys.Control | Keys.G));
        return edit;
    }

    private ToolStripMenuItem CreateViewMenu()
    {
        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add(new ToolStripMenuItem("&Font...", null, (s, e) => ChangeFont()));
        view.DropDownItems.Add(new ToolStripMenuItem("&Word Wrap", null, ToggleWordWrap));
        return view;
    }

    private ToolStripMenuItem CreateHelpMenu()
    {
        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(new ToolStripMenuItem("&About RagePad", null, (s, e) => ShowAboutDialog()));
        return help;
    }

    private ToolStrip CreateToolStrip()
    {
        var strip = new ToolStrip { ImageScalingSize = new Size(16, 16) };
        
        strip.Items.Add(new ToolStripButton("New", CreateEmojiIcon("📄"), (s, e) => NewTab()) { ToolTipText = "New (Ctrl+N)" });
        strip.Items.Add(new ToolStripButton("Open", CreateEmojiIcon("📂"), (s, e) => OpenFile()) { ToolTipText = "Open (Ctrl+O)" });
        strip.Items.Add(new ToolStripButton("Save", CreateEmojiIcon("💾"), (s, e) => SaveFile()) { ToolTipText = "Save (Ctrl+S)" });
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(new ToolStripButton("Find", CreateEmojiIcon("🔍"), (s, e) => ShowFindReplace(true)) { ToolTipText = "Find/Replace (Ctrl+H)" });
        
        return strip;
    }

    private static Bitmap CreateEmojiIcon(string emoji)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        using var font = new Font("Segoe UI Emoji", 10f);
        g.DrawString(emoji, font, Brushes.Black, -2, -2);
        return bmp;
    }

    #endregion

    #region Tab Management

    public Scintilla? CurrentEditor => 
        _tabs.SelectedTab?.Controls.Count > 0 
            ? _tabs.SelectedTab.Controls[0] as Scintilla 
            : null;

    private static Scintilla? GetEditor(TabPage tab) => 
        tab.Controls.Count > 0 ? tab.Controls[0] as Scintilla : null;

    private TabPage CreateTab(string title, string? filePath = null)
    {
        var tab = new TabPage(title)
        {
            Tag = new TabData { FilePath = filePath }
        };
        
        var editor = EditorFactory.Create(_editorFont);
        editor.UpdateUI += Editor_UpdateUI;
        editor.TextChanged += Editor_TextChanged;
        tab.Controls.Add(editor);
        
        return tab;
    }

    private void NewTab()
    {
        _untitledCount++;
        var tab = CreateTab($"Untitled {_untitledCount}");
        ((TabData)tab.Tag!).UntitledNumber = _untitledCount;
        _tabs.TabPages.Add(tab);
        _tabs.SelectedTab = tab;
        CurrentEditor?.Focus();
    }

    private void CloseTab()
    {
        if (_tabs.SelectedTab == null) return;

        var data = (TabData)_tabs.SelectedTab.Tag!;
        if (data.IsModified)
        {
            var result = MessageBox.Show(
                $"Do you want to save changes to {data.DisplayName}?",
                "RagePad",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                SaveFile();
                if (data.IsModified) return; // Save was cancelled
            }
            else if (result == DialogResult.Cancel)
            {
                return;
            }
        }

        var idx = _tabs.SelectedIndex;
        _tabs.TabPages.Remove(_tabs.SelectedTab);

        if (_tabs.TabCount == 0)
            NewTab();
        else if (idx >= _tabs.TabCount)
            _tabs.SelectedIndex = _tabs.TabCount - 1;
    }

    private void Tabs_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle)
        {
            for (int i = 0; i < _tabs.TabCount; i++)
            {
                if (_tabs.GetTabRect(i).Contains(e.Location))
                {
                    _tabs.SelectedIndex = i;
                    CloseTab();
                    break;
                }
            }
        }
    }

    private void UpdateTabTitle()
    {
        if (_tabs.SelectedTab == null) return;
        var data = (TabData)_tabs.SelectedTab.Tag!;
        _tabs.SelectedTab.Text = data.IsModified ? data.DisplayName + "*" : data.DisplayName;
    }

    private void UpdateTitle()
    {
        if (_tabs.SelectedTab == null)
        {
            Text = "RagePad";
            return;
        }

        var data = (TabData)_tabs.SelectedTab.Tag!;
        var name = data.FilePath ?? _tabs.SelectedTab.Text.TrimEnd('*');
        Text = $"{name} - RagePad";
    }

    #endregion

    #region File Operations

    private void OpenFile()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "All Files (*.*)|*.*|Text Files (*.txt)|*.txt|C# Files (*.cs)|*.cs|JSON (*.json)|*.json|XML (*.xml)|*.xml",
            Multiselect = true
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            foreach (var file in dlg.FileNames)
                OpenFileInTab(file);
        }
    }

    private void OpenFileInTab(string filePath)
    {
        // Check if already open
        foreach (TabPage tab in _tabs.TabPages)
        {
            var data = (TabData)tab.Tag!;
            if (string.Equals(data.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                _tabs.SelectedTab = tab;
                return;
            }
        }

        // Close empty untitled tab
        if (_tabs.TabCount == 1)
        {
            var data = (TabData)_tabs.TabPages[0].Tag!;
            var editor = GetEditor(_tabs.TabPages[0]);
            if (data.FilePath == null && !data.IsModified && editor?.TextLength == 0)
            {
                _tabs.TabPages.RemoveAt(0);
            }
        }

        var newTab = CreateTab(Path.GetFileName(filePath), filePath);
        _tabs.TabPages.Add(newTab);
        _tabs.SelectedTab = newTab;

        _isLoading = true;
        var ed = CurrentEditor!;
        ed.Text = File.ReadAllText(filePath);
        SyntaxHighlighter.ApplyHighlighting(ed, filePath);
        _isLoading = false;
        
        ((TabData)newTab.Tag!).IsModified = false;
        UpdateTitle();
        _statusLabel.Text = $"Opened: {filePath}";
    }

    private void SaveFile()
    {
        if (_tabs.SelectedTab == null) return;

        var data = (TabData)_tabs.SelectedTab.Tag!;
        if (data.FilePath == null)
        {
            SaveFileAs();
            return;
        }

        File.WriteAllText(data.FilePath, CurrentEditor!.Text);
        data.IsModified = false;
        UpdateTabTitle();
        _statusLabel.Text = $"Saved: {data.FilePath}";
    }

    private void SaveFileAs()
    {
        if (_tabs.SelectedTab == null || CurrentEditor == null) return;

        using var dlg = new SaveFileDialog
        {
            Filter = "All Files (*.*)|*.*|Text Files (*.txt)|*.txt"
        };

        var data = (TabData)_tabs.SelectedTab.Tag!;
        if (data.FilePath != null)
            dlg.FileName = Path.GetFileName(data.FilePath);

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            File.WriteAllText(dlg.FileName, CurrentEditor.Text);
            data.FilePath = dlg.FileName;
            data.IsModified = false;
            SyntaxHighlighter.ApplyHighlighting(CurrentEditor, dlg.FileName);
            UpdateTabTitle();
            UpdateTitle();
            _statusLabel.Text = $"Saved: {dlg.FileName}";
        }
    }

    #endregion

    #region Dialogs

    private void ShowFindReplace(bool showReplace)
    {
        var dlg = new FindReplaceDialog(this, showReplace);
        dlg.Show(this);
    }

    private void ShowGoToLine()
    {
        if (CurrentEditor == null) return;

        using var dlg = new GoToLineDialog(
            CurrentEditor.CurrentLine + 1, 
            CurrentEditor.Lines.Count);
        
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var line = dlg.SelectedLine - 1;
            CurrentEditor.GotoPosition(CurrentEditor.Lines[line].Position);
            CurrentEditor.Focus();
        }
    }

    private void ShowAboutDialog()
    {
        using var dlg = new AboutDialog();
        dlg.ShowDialog(this);
    }

    private void ChangeFont()
    {
        using var dlg = new FontDialog
        {
            Font = _editorFont,
            ShowEffects = false,
            FixedPitchOnly = true
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _editorFont = dlg.Font;
            foreach (TabPage tab in _tabs.TabPages)
            {
                var editor = GetEditor(tab);
                if (editor != null)
                    EditorFactory.ApplyFont(editor, _editorFont);
            }
        }
    }

    private void ToggleWordWrap(object? sender, EventArgs e)
    {
        if (CurrentEditor == null) return;
        CurrentEditor.WrapMode = CurrentEditor.WrapMode == WrapMode.None ? WrapMode.Word : WrapMode.None;
    }

    #endregion

    #region Editor Events

    private void Editor_UpdateUI(object? sender, UpdateUIEventArgs e)
    {
        if (CurrentEditor == null) return;

        var line = CurrentEditor.LineFromPosition(CurrentEditor.CurrentPosition) + 1;
        var col = CurrentEditor.GetColumn(CurrentEditor.CurrentPosition) + 1;
        
        _positionLabel.Text = $"Ln {line}, Col {col}";
    }

    private void Editor_TextChanged(object? sender, EventArgs e)
    {
        if (_isLoading || _tabs.SelectedTab == null) return;

        var data = (TabData)_tabs.SelectedTab.Tag!;
        if (!data.IsModified)
        {
            data.IsModified = true;
            UpdateTabTitle();
        }
    }

    #endregion

    #region Form Overrides

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        // Simple session restore
        try 
        {
            var tabs = _sessionManager.Load();
            if (tabs != null)
            {
                foreach (var tab in tabs)
                {
                    if (tab.FilePath != null && File.Exists(tab.FilePath))
                        OpenFileInTab(tab.FilePath);
                }
            }
        }
        catch { /* Ignore session errors */ }

        if (_tabs.TabCount == 0)
            NewTab();
    }

    public bool PreFilterMessage(ref Message m)
    {
        // WM_LBUTTONDBLCLK = 0x0203
        if (m.Msg == 0x0203)
        {
            var tabPt = _tabs.PointToClient(Cursor.Position);
            // Check if in tab header area but not on any tab
            if (tabPt.Y >= 0 && tabPt.Y < 30)
            {
                bool onTab = false;
                for (int i = 0; i < _tabs.TabCount; i++)
                    if (_tabs.GetTabRect(i).Contains(tabPt)) { onTab = true; break; }
                
                if (!onTab) { NewTab(); return true; }
            }
        }
        return false;
    }

    #endregion

    #region Compare

    private void SelectFirstToCompare()
    {
        if (CurrentEditor == null) return;
        _compareFirstFileContent = CurrentEditor.Text;
        _compareFirstFileName = GetCurrentTabTitle();
        
        // Update menu item
        _compareWithMenuItem.Text = $"Compare with '{_compareFirstFileName}'";
        _compareWithMenuItem.Enabled = true;
        
        _statusLabel.Text = $"Selected '{_compareFirstFileName}' for comparison";
    }

    private void CompareWithFirst()
    {
        if (CurrentEditor == null) return;
        
        if (_compareFirstFileContent == null || _compareFirstFileName == null)
        {
            MessageBox.Show("Please select the first file to compare first.\n\nUse Compare → Select First to Compare (Ctrl+1)", 
                "Compare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var secondFileContent = CurrentEditor.Text;
        var secondFileName = GetCurrentTabTitle();
        
        // Don't compare with itself
        if (secondFileName == _compareFirstFileName && secondFileContent == _compareFirstFileContent)
        {
            MessageBox.Show("Cannot compare a file with itself.", "Compare", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Show diff view
        using var diffForm = new DiffViewForm(
            _compareFirstFileContent, 
            _compareFirstFileName, 
            secondFileContent, 
            secondFileName,
            _editorFont);
        diffForm.ShowDialog(this);
    }
    
    private void ClearCompareSelection()
    {
        _compareFirstFileContent = null;
        _compareFirstFileName = null;
        _compareWithMenuItem.Text = "Compare with First";
        _compareWithMenuItem.Enabled = false;
        _statusLabel.Text = "Compare selection cleared";
    }

    private string GetCurrentTabTitle()
    {
        if (_tabs.SelectedTab == null) return "Untitled";
        var data = (TabData)_tabs.SelectedTab.Tag!;
        return data.DisplayName;
    }

    #endregion
}
