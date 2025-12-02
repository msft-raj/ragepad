using ScintillaNET;
using System.Drawing;

namespace RagePad.Services;

/// <summary>
/// Factory for creating and configuring Scintilla editors.
/// </summary>
internal static class EditorFactory
{
    /// <summary>
    /// Creates a new configured Scintilla editor instance.
    /// </summary>
    public static Scintilla Create(Font font)
    {
        var editor = new Scintilla
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            WrapMode = WrapMode.None,
            IndentationGuides = IndentView.LookBoth,
            TabWidth = 4,
            UseTabs = false,
            BufferedDraw = false, // Faster rendering
            Technology = Technology.DirectWrite // Hardware accelerated
        };

        // Line numbers margin
        editor.Margins[0].Width = 40;
        editor.Margins[0].Type = MarginType.Number;

        // Apply font
        ApplyFont(editor, font);

        // Caret styling
        editor.CaretForeColor = Color.Black;
        editor.CaretLineBackColor = Color.FromArgb(255, 232, 242, 254);

        return editor;
    }

    /// <summary>
    /// Applies font settings to an editor.
    /// </summary>
    public static void ApplyFont(Scintilla editor, Font font)
    {
        editor.StyleResetDefault();
        editor.Styles[Style.Default].Font = font.Name;
        editor.Styles[Style.Default].Size = (int)font.Size;
        editor.StyleClearAll();

        // Line number style
        editor.Styles[Style.LineNumber].ForeColor = Color.Gray;
        editor.Styles[Style.LineNumber].BackColor = Color.FromArgb(240, 240, 240);
    }
}
