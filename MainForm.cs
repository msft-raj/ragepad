using ScintillaNET;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RagePad;

public sealed class MainForm : Form
{
    private readonly MenuStrip _menu;
    private readonly TabControl _tabs;
    private readonly ToolStrip _toolStrip;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;
    private readonly ToolStripStatusLabel _positionLabel;
    private Font _editorFont;
    private int _untitledCount;
    private bool _isLoading; // Suppress TextChanged during load
    private DateTime _lastTabBarClick = DateTime.MinValue;
    private Point _lastTabBarClickPos;
    private static readonly string SessionDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RagePad");
    private static readonly string SessionFile = Path.Combine(SessionDir, "session.txt");
    private static readonly string BackupDir = Path.Combine(SessionDir, "backup");

    public MainForm()
    {
        // Form setup - minimal for speed
        Text = "RagePad";
        Width = 1200;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        _editorFont = new Font("Consolas", 11f, FontStyle.Regular);

        // Menu
        _menu = CreateMenu();
        _menu.Dock = DockStyle.Top;

        // Toolbar
        _toolStrip = CreateToolStrip();

        // Tabs
        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f),
            Padding = new Point(12, 4)
        };
        _tabs.SelectedIndexChanged += Tabs_SelectedIndexChanged;
        _tabs.MouseClick += Tabs_MouseClick;
        _tabs.MouseDown += Tabs_MouseDown;

        // Status bar
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _positionLabel = new ToolStripStatusLabel("Ln 1, Col 1") { AutoSize = false, Width = 120, TextAlign = ContentAlignment.MiddleRight };
        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.Items.Add(_positionLabel);

        // Panel for tabs (avoid flickering)
        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(_tabs);

        // Add controls in correct order
        Controls.Add(panel);
        Controls.Add(_toolStrip);
        Controls.Add(_menu);
        Controls.Add(_statusStrip);
        MainMenuStrip = _menu;

        // Keyboard shortcuts
        KeyPreview = true;
    }

    private MenuStrip CreateMenu()
    {
        var menu = new MenuStrip();

        // File menu
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(new ToolStripMenuItem("&New", null, (s, e) => NewTab(), Keys.Control | Keys.N));
        file.DropDownItems.Add(new ToolStripMenuItem("&Open...", null, (s, e) => OpenFile(), Keys.Control | Keys.O));
        file.DropDownItems.Add(new ToolStripMenuItem("&Save", null, (s, e) => SaveFile(), Keys.Control | Keys.S));
        file.DropDownItems.Add(new ToolStripMenuItem("Save &As...", null, (s, e) => SaveFileAs(), Keys.Control | Keys.Shift | Keys.S));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("Close Tab", null, (s, e) => CloseTab(), Keys.Control | Keys.W));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (s, e) => Close(), Keys.Alt | Keys.F4));
        menu.Items.Add(file);

        // Edit menu
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
        menu.Items.Add(edit);

        // View menu
        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add(new ToolStripMenuItem("&Font...", null, (s, e) => ChangeFont()));
        view.DropDownItems.Add(new ToolStripMenuItem("&Word Wrap", null, ToggleWordWrap));
        menu.Items.Add(view);

        // Help menu
        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(new ToolStripMenuItem("&About RagePad", null, (s, e) => ShowAboutDialog()));
        menu.Items.Add(help);

        return menu;
    }

    private ToolStrip CreateToolStrip()
    {
        var strip = new ToolStrip { ImageScalingSize = new Size(16, 16) };
        
        // Create simple icons
        var newIcon = CreateIcon("📄");
        var openIcon = CreateIcon("📂");
        var saveIcon = CreateIcon("💾");
        var findIcon = CreateIcon("🔍");
        
        strip.Items.Add(new ToolStripButton("New", newIcon, (s, e) => NewTab()) { ToolTipText = "New (Ctrl+N)" });
        strip.Items.Add(new ToolStripButton("Open", openIcon, (s, e) => OpenFile()) { ToolTipText = "Open (Ctrl+O)" });
        strip.Items.Add(new ToolStripButton("Save", saveIcon, (s, e) => SaveFile()) { ToolTipText = "Save (Ctrl+S)" });
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(new ToolStripButton("Find/Replace", findIcon, (s, e) => ShowFindReplace(true)) { ToolTipText = "Find/Replace (Ctrl+H)" });
        return strip;
    }

    private static Bitmap CreateIcon(string emoji)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        using var font = new Font("Segoe UI Emoji", 10f);
        g.DrawString(emoji, font, Brushes.Black, -2, -2);
        return bmp;
    }

    private Scintilla CreateEditor()
    {
        var editor = new Scintilla
        {
            Dock = DockStyle.Fill,
            WrapMode = WrapMode.None,
            IndentationGuides = IndentView.LookBoth,
            TabWidth = 4,
            UseTabs = false,
            BufferedDraw = false, // Faster rendering
            Technology = Technology.DirectWrite // Hardware accelerated
        };

        // Line numbers
        editor.Margins[0].Width = 40;
        editor.Margins[0].Type = MarginType.Number;

        // Apply font
        ApplyFont(editor);

        // Caret
        editor.CaretForeColor = Color.Black;
        editor.CaretLineBackColor = Color.FromArgb(255, 232, 242, 254);

        // Events
        editor.UpdateUI += Editor_UpdateUI;
        editor.TextChanged += Editor_TextChanged;

        return editor;
    }

    private void ApplyFont(Scintilla editor)
    {
        editor.StyleResetDefault();
        editor.Styles[Style.Default].Font = _editorFont.Name;
        editor.Styles[Style.Default].Size = (int)_editorFont.Size;
        editor.StyleClearAll();

        // Line number style
        editor.Styles[Style.LineNumber].ForeColor = Color.Gray;
        editor.Styles[Style.LineNumber].BackColor = Color.FromArgb(240, 240, 240);
    }

    private TabPage CreateTab(string title, string? filePath = null)
    {
        var tab = new TabPage(title)
        {
            Tag = new TabData { FilePath = filePath, IsModified = false }
        };
        var editor = CreateEditor();
        tab.Controls.Add(editor);
        return tab;
    }

    private void NewTab()
    {
        _untitledCount++;
        var tab = CreateTab($"Untitled {_untitledCount}");
        _tabs.TabPages.Add(tab);
        _tabs.SelectedTab = tab;
        CurrentEditor?.Focus();
    }

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

        // Load file fast
        _isLoading = true;
        var text = File.ReadAllText(filePath);
        var ed = CurrentEditor!;
        ed.Text = text;
        
        // Apply syntax highlighting
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
            
            // Apply syntax highlighting for new file type
            SyntaxHighlighter.ApplyHighlighting(CurrentEditor, dlg.FileName);
            
            UpdateTabTitle();
            UpdateTitle();
            _statusLabel.Text = $"Saved: {dlg.FileName}";
        }
    }

    private void CloseTab()
    {
        if (_tabs.SelectedTab == null) return;

        var data = (TabData)_tabs.SelectedTab.Tag!;
        if (data.IsModified)
        {
            var name = data.FilePath != null ? Path.GetFileName(data.FilePath) : _tabs.SelectedTab.Text.TrimEnd('*');
            var result = MessageBox.Show(
                $"Do you want to save changes to {name}?",
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
            // No = close without saving
        }

        var idx = _tabs.SelectedIndex;
        _tabs.TabPages.Remove(_tabs.SelectedTab);

        if (_tabs.TabCount == 0)
            NewTab();
        else if (idx >= _tabs.TabCount)
            _tabs.SelectedIndex = _tabs.TabCount - 1;
    }

    private void ShowFindReplace(bool showReplace)
    {
        var dlg = new FindReplaceDialog(this, showReplace);
        dlg.Show(this);
    }

    private void ShowGoToLine()
    {
        if (CurrentEditor == null) return;

        using var dlg = new Form
        {
            Text = "Go to Line",
            Width = 300,
            Height = 120,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var label = new Label { Text = "Line number:", Left = 10, Top = 15, Width = 80 };
        var textBox = new TextBox { Left = 95, Top = 12, Width = 170, Text = (CurrentEditor.CurrentLine + 1).ToString() };
        var btnOk = new Button { Text = "OK", Left = 110, Top = 45, Width = 75, DialogResult = DialogResult.OK };

        dlg.Controls.AddRange(new Control[] { label, textBox, btnOk });
        dlg.AcceptButton = btnOk;
        textBox.SelectAll();

        if (dlg.ShowDialog() == DialogResult.OK && int.TryParse(textBox.Text, out int line))
        {
            line = Math.Max(1, Math.Min(line, CurrentEditor.Lines.Count)) - 1;
            CurrentEditor.GotoPosition(CurrentEditor.Lines[line].Position);
            CurrentEditor.Focus();
        }
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
                if (editor != null) ApplyFont(editor);
            }
        }
    }

    private void ToggleWordWrap(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item)
        {
            item.Checked = !item.Checked;
            var mode = item.Checked ? WrapMode.Word : WrapMode.None;
            foreach (TabPage tab in _tabs.TabPages)
            {
                var editor = GetEditor(tab);
                if (editor != null) editor.WrapMode = mode;
            }
        }
    }

    private void ShowAboutDialog()
    {
        // Read version from file
        var version = "0.1";
        var versionFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
        if (File.Exists(versionFile))
        {
            version = File.ReadAllText(versionFile).Trim();
        }

        using var dlg = new Form
        {
            Text = "About RagePad",
            Width = 400,
            Height = 350,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.White
        };

        // Load splash image
        var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RagePad.png");
        PictureBox? pictureBox = null;
        if (File.Exists(imagePath))
        {
            pictureBox = new PictureBox
            {
                Image = Image.FromFile(imagePath),
                SizeMode = PictureBoxSizeMode.Zoom,
                Left = 50,
                Top = 20,
                Width = 280,
                Height = 150
            };
            dlg.Controls.Add(pictureBox);
        }

        var titleLabel = new Label
        {
            Text = "RagePad",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            Left = 0,
            Top = pictureBox != null ? 180 : 20,
            Width = dlg.ClientSize.Width,
            Height = 35,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var versionLabel = new Label
        {
            Text = $"Version {version}",
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.Gray,
            Left = 0,
            Top = titleLabel.Bottom,
            Width = dlg.ClientSize.Width,
            Height = 25,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var authorLabel = new Label
        {
            Text = "Author: Rajorshi Biswas",
            Font = new Font("Segoe UI", 10f),
            Left = 0,
            Top = versionLabel.Bottom + 10,
            Width = dlg.ClientSize.Width,
            Height = 22,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var emailLink = new LinkLabel
        {
            Text = "ragebiswas@gmail.com",
            Font = new Font("Segoe UI", 10f),
            Left = 0,
            Top = authorLabel.Bottom,
            Width = dlg.ClientSize.Width,
            Height = 22,
            TextAlign = ContentAlignment.MiddleCenter
        };
        emailLink.LinkClicked += (s, e) =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("mailto:ragebiswas@gmail.com") { UseShellExecute = true }); }
            catch { }
        };

        var btnOk = new Button
        {
            Text = "OK",
            Left = (dlg.ClientSize.Width - 80) / 2,
            Top = emailLink.Bottom + 15,
            Width = 80,
            DialogResult = DialogResult.OK
        };

        dlg.Controls.AddRange(new Control[] { titleLabel, versionLabel, authorLabel, emailLink, btnOk });
        dlg.AcceptButton = btnOk;
        dlg.ShowDialog(this);
    }

    private void Editor_UpdateUI(object? sender, UpdateUIEventArgs e)
    {
        if (CurrentEditor != null)
        {
            var line = CurrentEditor.CurrentLine + 1;
            var col = CurrentEditor.GetColumn(CurrentEditor.CurrentPosition) + 1;
            _positionLabel.Text = $"Ln {line}, Col {col}";
        }
    }

    private void Editor_TextChanged(object? sender, EventArgs e)
    {
        if (_isLoading) return;
        
        if (_tabs.SelectedTab != null)
        {
            var data = (TabData)_tabs.SelectedTab.Tag!;
            if (!data.IsModified)
            {
                data.IsModified = true;
                UpdateTabTitle();
            }
        }
    }

    private void Tabs_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateTitle();
        CurrentEditor?.Focus();
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

    private void Tabs_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        
        // Check if we're in the tab header area
        int tabHeaderHeight = _tabs.ItemSize.Height + 4;
        if (e.Y > tabHeaderHeight) return;
        
        // Check if clicked on a tab
        for (int i = 0; i < _tabs.TabCount; i++)
        {
            if (_tabs.GetTabRect(i).Contains(e.Location))
                return; // Clicked on a tab, not empty space
        }
        
        // Check for double-click manually
        var now = DateTime.Now;
        var elapsed = (now - _lastTabBarClick).TotalMilliseconds;
        var distance = Math.Abs(e.X - _lastTabBarClickPos.X) + Math.Abs(e.Y - _lastTabBarClickPos.Y);
        
        if (elapsed < SystemInformation.DoubleClickTime && distance < 10)
        {
            // Double-click on empty tab bar area
            NewTab();
            _lastTabBarClick = DateTime.MinValue; // Reset
        }
        else
        {
            _lastTabBarClick = now;
            _lastTabBarClickPos = e.Location;
        }
    }

    private void UpdateTabTitle()
    {
        if (_tabs.SelectedTab == null) return;
        var data = (TabData)_tabs.SelectedTab.Tag!;
        var name = data.FilePath != null ? Path.GetFileName(data.FilePath) : $"Untitled {_untitledCount}";
        _tabs.SelectedTab.Text = data.IsModified ? name + "*" : name;
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

    public Scintilla? CurrentEditor => _tabs.SelectedTab?.Controls.Count > 0 ? _tabs.SelectedTab.Controls[0] as Scintilla : null;

    private static Scintilla? GetEditor(TabPage tab) => tab.Controls.Count > 0 ? tab.Controls[0] as Scintilla : null;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Save session - remember all open files including untitled ones
        SaveSession();
        base.OnFormClosing(e);
    }

    protected override void OnDragEnter(DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
        base.OnDragEnter(e);
    }

    protected override void OnDragDrop(DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
        {
            foreach (var file in files)
            {
                if (File.Exists(file)) OpenFileInTab(file);
            }
        }
        base.OnDragDrop(e);
    }

    protected override void OnLoad(EventArgs e)
    {
        AllowDrop = true;
        
        // Restore previous session
        LoadSession();
        
        base.OnLoad(e);
    }
    
    private void SaveSession()
    {
        try
        {
            // Ensure directories exist
            if (!Directory.Exists(SessionDir))
                Directory.CreateDirectory(SessionDir);
            if (!Directory.Exists(BackupDir))
                Directory.CreateDirectory(BackupDir);
            
            // Clear old backups
            foreach (var f in Directory.GetFiles(BackupDir, "*.txt"))
                File.Delete(f);
            
            var sessionLines = new System.Collections.Generic.List<string>();
            int backupIndex = 0;
            
            foreach (TabPage tab in _tabs.TabPages)
            {
                var data = (TabData)tab.Tag!;
                var editor = GetEditor(tab);
                
                if (data.FilePath != null)
                {
                    // Regular file - just save path
                    sessionLines.Add($"FILE:{data.FilePath}");
                }
                else if (editor != null)
                {
                    // Untitled file - backup content
                    backupIndex++;
                    var backupFile = Path.Combine(BackupDir, $"untitled_{backupIndex}.txt");
                    File.WriteAllText(backupFile, editor.Text);
                    sessionLines.Add($"BACKUP:{backupFile}");
                }
            }
            
            File.WriteAllLines(SessionFile, sessionLines);
        }
        catch { /* Ignore session save errors */ }
    }
    
    private void LoadSession()
    {
        try
        {
            if (File.Exists(SessionFile))
            {
                var lines = File.ReadAllLines(SessionFile);
                foreach (var line in lines)
                {
                    if (line.StartsWith("FILE:"))
                    {
                        var path = line.Substring(5);
                        if (File.Exists(path))
                            OpenFileInTab(path);
                    }
                    else if (line.StartsWith("BACKUP:"))
                    {
                        var backupPath = line.Substring(7);
                        if (File.Exists(backupPath))
                        {
                            // Restore untitled file from backup
                            NewTab();
                            var editor = CurrentEditor;
                            if (editor != null)
                            {
                                _isLoading = true;
                                editor.Text = File.ReadAllText(backupPath);
                                _isLoading = false;
                                
                                // Mark as modified since it's unsaved content
                                if (_tabs.SelectedTab != null)
                                {
                                    var data = (TabData)_tabs.SelectedTab.Tag!;
                                    if (editor.TextLength > 0)
                                    {
                                        data.IsModified = true;
                                        UpdateTabTitle();
                                    }
                                }
                            }
                        }
                    }
                    else if (File.Exists(line))
                    {
                        // Legacy format - just a file path
                        OpenFileInTab(line);
                    }
                }
            }
        }
        catch { /* Ignore session load errors */ }
        
        // If no files were restored, ensure we have at least one tab
        if (_tabs.TabCount == 0)
            NewTab();
    }
}

internal sealed class TabData
{
    public string? FilePath { get; set; }
    public bool IsModified { get; set; }
}
