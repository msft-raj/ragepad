# RagePad - Copilot Implementation Guide

This document contains all implementation details for the RagePad text editor project. Use this to continue development.

## Project Overview

RagePad is a Notepad++ replacement built with:
- **Framework**: .NET 8.0 WinForms
- **Editor Component**: Scintilla5.NET (v6.1.0) - hardware-accelerated text editing
- **Goal**: Blazing fast startup, minimal memory, Notepad++ workflow

## Project Structure

```
RagePad.WinForms/
├── MainForm.cs           # Main application window, tabs, menus, session management
├── FindReplaceDialog.cs  # Find/Replace dialog with all search options
├── SyntaxHighlighter.cs  # Language detection and syntax coloring
├── Program.cs            # Entry point
└── RagePad.WinForms.csproj
```

## Key Implementation Details

### MainForm.cs

#### Fields
```csharp
private readonly MenuStrip _menu;
private readonly TabControl _tabs;
private readonly ToolStrip _toolStrip;
private readonly StatusStrip _statusStrip;
private readonly ToolStripStatusLabel _statusLabel;
private readonly ToolStripStatusLabel _positionLabel;
private Font _editorFont;                    // Default: Consolas 11pt
private int _untitledCount;                  // Counter for "Untitled N" tabs
private bool _isLoading;                     // Suppresses TextChanged during file load
private DateTime _lastTabBarClick;           // For manual double-click detection
private Point _lastTabBarClickPos;
```

#### Session Paths
```csharp
static readonly string SessionDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RagePad");
static readonly string SessionFile = Path.Combine(SessionDir, "session.txt");
static readonly string BackupDir = Path.Combine(SessionDir, "backup");
```

#### Session File Format
```
FILE:C:\path\to\saved\file.txt
BACKUP:C:\Users\...\RagePad\backup\untitled_1.txt
```

#### Editor Creation (CreateEditor method)
- `BufferedDraw = false` - Faster rendering
- `Technology = Technology.DirectWrite` - Hardware acceleration
- Line numbers margin: 40px width
- Caret line highlight: RGB(232, 242, 254)
- Events: `UpdateUI` (cursor position), `TextChanged` (dirty flag)

#### Tab Management
- `TabData` class stores: `FilePath` (null for untitled), `IsModified`
- Middle-click closes tab
- Double-click on empty tab bar creates new tab (manual detection via MouseDown)
- Modified files show `*` suffix

#### Important Flags
- `_isLoading`: Set to `true` before loading file content, `false` after. Prevents TextChanged from marking file as modified during load/syntax highlighting.

### FindReplaceDialog.cs

#### Features
- Find Next / Find Previous
- Replace / Replace All
- Match Case checkbox
- Whole Word checkbox  
- Wrap Around checkbox (default: checked)
- Pre-fills with selected text
- Escape key closes dialog
- Enter key triggers Find Next (Shift+Enter for Find Previous)

#### Search Implementation
Uses Scintilla's native search:
```csharp
editor.SearchFlags = GetSearchFlags();  // MatchCase, WholeWord
editor.TargetStart = startPos;
editor.TargetEnd = endPos;
int pos = editor.SearchInTarget(searchText);
```

### SyntaxHighlighter.cs

#### Usage
```csharp
SyntaxHighlighter.ApplyHighlighting(editor, filePath);
```

#### Language Detection
Based on file extension. Maps to Scintilla lexer names:
- `.cs` → `"cpp"` lexer with C# keywords
- `.js/.ts` → `"cpp"` lexer with JS keywords
- `.json` → `"json"` lexer
- `.xml/.html` → `"xml"` lexer
- `.css` → `"css"` lexer
- `.py` → `"python"` lexer
- `.sql` → `"sql"` lexer
- `.md` → `"markdown"` lexer
- `.bat/.cmd` → `"batch"` lexer
- `.ps1` → `"powershell"` lexer

#### Scintilla5.NET Lexer API
```csharp
editor.LexerName = "cpp";  // Set lexer by name (not enum!)
editor.SetKeywords(0, "keyword list...");  // Primary keywords
editor.SetKeywords(1, "type list...");     // Secondary keywords (types)
editor.Styles[Style.Cpp.Comment].ForeColor = Color.Green;
```

### Performance Optimizations

#### In .csproj
```xml
<TieredCompilation>true</TieredCompilation>
<TieredCompilationQuickJit>true</TieredCompilationQuickJit>
<PublishReadyToRun>true</PublishReadyToRun>
<InvariantGlobalization>true</InvariantGlobalization>
```

#### In Code
- `DoubleBuffered = true` on Form
- `BufferedDraw = false` on Scintilla (counterintuitive but faster)
- `Technology = Technology.DirectWrite` for GPU rendering
- Synchronous file I/O (faster for small-medium files)

## Known Patterns

### Loading Files Without Dirty Flag
```csharp
_isLoading = true;
editor.Text = content;
SyntaxHighlighter.ApplyHighlighting(editor, path);
_isLoading = false;
((TabData)tab.Tag!).IsModified = false;
```

### Double-Click Detection on TabControl
TabControl doesn't fire MouseDoubleClick on empty areas. Solution:
```csharp
_tabs.MouseDown += Tabs_MouseDown;

private void Tabs_MouseDown(object? sender, MouseEventArgs e) {
    // Check if in tab header area (not content)
    if (e.Y > _tabs.ItemSize.Height + 4) return;
    
    // Check not on a tab
    for (int i = 0; i < _tabs.TabCount; i++)
        if (_tabs.GetTabRect(i).Contains(e.Location)) return;
    
    // Manual double-click detection
    var elapsed = (DateTime.Now - _lastTabBarClick).TotalMilliseconds;
    if (elapsed < SystemInformation.DoubleClickTime) {
        NewTab();
    }
    _lastTabBarClick = DateTime.Now;
}
```

## Future Enhancements (Not Yet Implemented)

1. **More syntax languages** - Add support for more file types
2. **Themes** - Dark mode, custom color schemes
3. **Code folding** - Collapse/expand code blocks
4. **Auto-indent** - Smart indentation
5. **Encoding support** - UTF-8, UTF-16, etc. with BOM detection
6. **Line ending display** - Show/convert CRLF/LF
7. **Recent files menu** - Quick access to recently opened files
8. **Zoom** - Ctrl+scroll to zoom
9. **Split view** - View same file in split panes
10. **Plugin system** - Extensibility

## Debugging Tips

1. **Files show dirty on open**: Check `_isLoading` flag is set before AND after text/highlighting changes
2. **Session not restoring**: Check `%LOCALAPPDATA%\RagePad\session.txt` exists and has correct format
3. **Syntax not working**: Ensure `LexerName` is set (string, not enum) and styles are applied AFTER setting lexer
4. **Double-click not working**: TabControl consumes double-clicks on tabs; use MouseDown for empty area detection

## Dependencies

- **Scintilla5.NET** (v6.1.0) - NuGet package `Scintilla5.NET`
  - Includes native Scintilla and Lexilla DLLs
  - Namespace: `ScintillaNET`
  - Key types: `Scintilla`, `Style`, `Lexer` (enum for reference only, use `LexerName` string property)
