# RagePad

A blazing-fast, lightweight text editor built with WinForms and Scintilla. Designed as a Notepad++ replacement with focus on speed and simplicity.

## Features

### Core Editing
- **Scintilla-powered editor** - Fast, reliable text editing with DirectWrite hardware acceleration
- **Tabbed interface** - Open multiple files in tabs
- **Syntax highlighting** - Automatic highlighting for popular languages
- **Find & Replace** - Full find/replace with match case, whole word, and wrap options
- **Go to Line** - Quick navigation (Ctrl+G)
- **Word wrap toggle** - View menu option

### File Management
- **Session persistence** - Automatically restores your previous session on startup
- **Auto-backup** - Untitled files are automatically saved and restored (no prompts!)
- **Drag & drop** - Drop files onto the window to open them
- **Multiple file open** - Select multiple files in the Open dialog

### User Experience
- **Notepad++ behavior** - Close without save prompts for files that can be restored
- **Middle-click close** - Middle-click on a tab to close it
- **Double-click new tab** - Double-click empty tab bar area to create new file
- **Font selection** - Choose your preferred monospace font (default: Consolas 11pt)
- **Line numbers** - Always visible line number margin
- **Current line highlight** - Subtle highlight on the current line
- **Status bar** - Shows cursor position (Line, Column)

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New file |
| Ctrl+O | Open file(s) |
| Ctrl+S | Save |
| Ctrl+Shift+S | Save As |
| Ctrl+W | Close tab |
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| Ctrl+X | Cut |
| Ctrl+C | Copy |
| Ctrl+V | Paste |
| Ctrl+A | Select All |
| Ctrl+F | Find |
| Ctrl+H | Find & Replace |
| Ctrl+G | Go to Line |

## Supported Languages (Syntax Highlighting)

- C# (.cs)
- C/C++ (.c, .cpp, .h, .hpp)
- JavaScript/TypeScript (.js, .ts, .jsx, .tsx)
- JSON (.json)
- XML/HTML (.xml, .html, .htm, .xaml, .csproj, .config)
- CSS (.css, .scss, .less)
- Python (.py)
- SQL (.sql)
- Markdown (.md, .markdown)
- Batch (.bat, .cmd)
- PowerShell (.ps1, .psm1)

## Building

Requires .NET 8.0 SDK.

```bash
dotnet build
dotnet run
```

For optimized release build:
```bash
dotnet publish -c Release
```

## Data Storage

RagePad stores session data in:
- Windows: `%LOCALAPPDATA%\RagePad\`
  - `session.txt` - List of open files
  - `backup\` - Auto-saved untitled file contents

## Requirements

- Windows 10/11
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (download the "Desktop Runtime" for Windows x64)

## License

MIT
