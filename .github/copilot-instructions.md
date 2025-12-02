# RagePad - Copilot Implementation Guide

This document contains all implementation details for the RagePad text editor project. Use this to continue development.

## Project Overview

RagePad is a Notepad++ replacement built with:
- **Framework**: .NET 8.0 WinForms
- **Editor Component**: Scintilla5.NET (v6.1.0) - hardware-accelerated text editing
- **Goal**: Blazing fast startup, minimal memory, Notepad++ workflow

## Project Structure

```
RagePad/
├── MainForm.cs              # Main window - UI orchestration, tabs, menus
├── FindReplaceDialog.cs     # Find/Replace dialog with all search options
├── SyntaxHighlighter.cs     # Language detection and syntax coloring
├── Program.cs               # Entry point
├── RagePad.csproj           # Project file
├── RagePad.sln              # Solution file
├── version.txt              # Version number (read by AboutDialog)
├── RagePad.png              # Logo for About dialog
├── RagePadLogo.png          # Icon for window
├── publish-release.ps1      # Build and package release script
│
├── Models/
│   └── TabData.cs           # Tab metadata (FilePath, IsModified, UntitledNumber)
│
├── Services/
│   ├── AppInfo.cs           # Application paths and version info
│   ├── EditorFactory.cs     # Scintilla editor creation and configuration
│   └── SessionManager.cs    # Session save/restore logic
│
└── Dialogs/
    ├── AboutDialog.cs       # About dialog with logo and credits
    └── GoToLineDialog.cs    # Go to line number dialog
```

## Key Components

### MainForm.cs

The main window orchestrates UI components. Organized into regions:

- **Fields** - UI controls, services, state
- **Initialization** - Form setup, icon, layout
- **Menu & Toolbar** - Menu creation (File, Edit, View, Help)
- **Tab Management** - NewTab, CloseTab, tab events
- **File Operations** - Open, Save, SaveAs
- **Dialogs** - Find/Replace, GoToLine, About, Font
- **Editor Events** - UpdateUI, TextChanged handlers
- **Session Management** - Save/Load via SessionManager
- **Form Overrides** - OnLoad, OnClosing, Drag/Drop
- **IMessageFilter** - Double-click detection on tab bar

#### Key Fields
```csharp
private readonly TabControl _tabs;
private readonly SessionManager _sessionManager;
private Font _editorFont;           // Default: Consolas 11pt
private int _untitledCount;         // Counter for "Untitled N" tabs
private bool _isLoading;            // Suppresses TextChanged during file load
```

### Models/TabData.cs

Stores metadata for each open tab:
```csharp
public string? FilePath { get; set; }      // null for untitled files
public bool IsModified { get; set; }       // Has unsaved changes
public int UntitledNumber { get; set; }    // For "Untitled N" naming
public string DisplayName { get; }         // Computed: filename or "Untitled N"
```

### Services/AppInfo.cs

Centralized application paths and info:
```csharp
AppInfo.BaseDirectory      // exe location
AppInfo.SessionDir         // %LOCALAPPDATA%\RagePad
AppInfo.SessionFile        // session.txt path
AppInfo.BackupDir          // backup folder path
AppInfo.GetVersion()       // reads version.txt
```

### Services/EditorFactory.cs

Creates and configures Scintilla editors:
```csharp
EditorFactory.Create(font)           // Creates new configured editor
EditorFactory.ApplyFont(editor, font) // Applies font to existing editor
```

Configuration:
- `BufferedDraw = false` - Faster rendering
- `Technology = Technology.DirectWrite` - Hardware acceleration
- Line numbers margin: 40px width
- Caret line highlight: RGB(232, 242, 254)

### Services/SessionManager.cs

Handles session persistence:
```csharp
sessionManager.Save(tabs)    // Save current session
sessionManager.Load()        // Returns IEnumerable<TabEntry>
```

Session file format:
```
FILE:C:\path\to\saved\file.txt
BACKUP:1:C:\Users\...\RagePad\backup\untitled_1.txt
```

### Dialogs/AboutDialog.cs

Shows app info:
- RagePad.png logo
- Version from version.txt
- Author: Rajorshi Biswas
- Clickable email link

### Dialogs/GoToLineDialog.cs

Simple line number input:
- Pre-fills current line
- Validates range
- Returns `SelectedLine` property

### FindReplaceDialog.cs

Find/Replace features:
- Find Next / Find Previous
- Replace / Replace All
- Match Case, Whole Word, Wrap Around checkboxes
- Pre-fills with selected text
- Escape closes, Enter finds

Uses Scintilla's native search:
```csharp
editor.SearchFlags = GetSearchFlags();  // MatchCase, WholeWord
editor.TargetStart = startPos;
editor.TargetEnd = endPos;
int pos = editor.SearchInTarget(searchText);
```

### SyntaxHighlighter.cs

Language detection by extension:
```csharp
SyntaxHighlighter.ApplyHighlighting(editor, filePath);
```

Supported: `.cs`, `.js/.ts`, `.json`, `.xml/.html`, `.css`, `.py`, `.sql`, `.md`, `.bat/.cmd`, `.ps1`

#### Scintilla5.NET Lexer API
```csharp
editor.LexerName = "cpp";  // Set lexer by name (not enum!)
editor.SetKeywords(0, "keyword list...");  // Primary keywords
editor.SetKeywords(1, "type list...");     // Secondary keywords (types)
editor.Styles[Style.Cpp.Comment].ForeColor = Color.Green;
```

## Important Patterns

### Loading Files Without Dirty Flag
```csharp
_isLoading = true;
editor.Text = content;
SyntaxHighlighter.ApplyHighlighting(editor, path);
_isLoading = false;
((TabData)tab.Tag!).IsModified = false;
```

### Double-Click on Empty Tab Bar
TabControl doesn't fire MouseDoubleClick on empty areas. Solution uses IMessageFilter:
```csharp
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
```

### Consistent Untitled Naming
Each tab stores its `UntitledNumber` in TabData. On session restore, the number is preserved. Global `_untitledCount` tracks the highest used number.
```

## Performance Optimizations

### In .csproj
```xml
<TieredCompilation>true</TieredCompilation>
<TieredCompilationQuickJit>true</TieredCompilationQuickJit>
<PublishReadyToRun>true</PublishReadyToRun>
<InvariantGlobalization>true</InvariantGlobalization>
```

### In Code
- `DoubleBuffered = true` on Form
- `BufferedDraw = false` on Scintilla (counterintuitive but faster)
- `Technology = Technology.DirectWrite` for GPU rendering
- Synchronous file I/O (faster for small-medium files)

## Building & Releasing

### Debug Build
```bash
dotnet build
dotnet run
```

### Create Release
```powershell
powershell -ExecutionPolicy Bypass -File publish-release.ps1
```
Creates `RagePad-v{version}-win-x64.zip` ready for GitHub release.

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
4. **Double-click not working**: IMessageFilter intercepts WM_LBUTTONDBLCLK (0x0203)
5. **Untitled names changing**: Check TabData.UntitledNumber is preserved through session save/restore

## Dependencies

- **Scintilla5.NET** (v6.1.0) - NuGet package `Scintilla5.NET`
  - Includes native Scintilla and Lexilla DLLs
  - Namespace: `ScintillaNET`
  - Key types: `Scintilla`, `Style`, `Lexer` (enum for reference only, use `LexerName` string property)
