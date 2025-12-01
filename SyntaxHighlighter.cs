using ScintillaNET;
using System.Drawing;
using System.IO;

namespace RagePad;

internal static class SyntaxHighlighter
{
    public static void ApplyHighlighting(Scintilla editor, string? filePath)
    {
        var ext = filePath != null ? Path.GetExtension(filePath).ToLowerInvariant() : "";
        
        switch (ext)
        {
            case ".cs":
                ApplyCSharp(editor);
                break;
            case ".js":
            case ".ts":
            case ".jsx":
            case ".tsx":
                ApplyJavaScript(editor);
                break;
            case ".json":
                ApplyJson(editor);
                break;
            case ".xml":
            case ".xaml":
            case ".csproj":
            case ".config":
            case ".html":
            case ".htm":
                ApplyXml(editor);
                break;
            case ".css":
            case ".scss":
            case ".less":
                ApplyCss(editor);
                break;
            case ".py":
                ApplyPython(editor);
                break;
            case ".sql":
                ApplySql(editor);
                break;
            case ".cpp":
            case ".c":
            case ".h":
            case ".hpp":
                ApplyCpp(editor);
                break;
            case ".md":
            case ".markdown":
                ApplyMarkdown(editor);
                break;
            case ".bat":
            case ".cmd":
                ApplyBatch(editor);
                break;
            case ".ps1":
            case ".psm1":
                ApplyPowerShell(editor);
                break;
            default:
                ApplyPlainText(editor);
                break;
        }
    }

    private static void ApplyPlainText(Scintilla editor)
    {
        editor.LexerName = "null";
    }

    private static void ApplyCSharp(Scintilla editor)
    {
        editor.LexerName = "cpp";

        // Colors
        editor.Styles[Style.Cpp.Default].ForeColor = Color.Black;
        editor.Styles[Style.Cpp.Comment].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Cpp.CommentLine].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Cpp.CommentDoc].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Cpp.Number].ForeColor = Color.FromArgb(0, 128, 128);
        editor.Styles[Style.Cpp.Word].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Cpp.Word2].ForeColor = Color.FromArgb(43, 145, 175);
        editor.Styles[Style.Cpp.String].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Cpp.Character].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Cpp.Verbatim].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Cpp.Operator].ForeColor = Color.Black;
        editor.Styles[Style.Cpp.Preprocessor].ForeColor = Color.FromArgb(128, 128, 128);

        // Keywords
        editor.SetKeywords(0, "abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while async await var dynamic nameof when where yield global partial record init required file scoped");
        editor.SetKeywords(1, "Console WriteLine ReadLine Write Exception Task List Dictionary String Int32 Boolean Object Array Func Action IEnumerable LINQ DateTime Guid StringBuilder File Directory Path Environment");
    }

    private static void ApplyJavaScript(Scintilla editor)
    {
        editor.LexerName = "cpp";

        editor.Styles[Style.Cpp.Default].ForeColor = Color.Black;
        editor.Styles[Style.Cpp.Comment].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Cpp.CommentLine].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Cpp.Number].ForeColor = Color.FromArgb(0, 128, 128);
        editor.Styles[Style.Cpp.Word].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Cpp.Word2].ForeColor = Color.FromArgb(43, 145, 175);
        editor.Styles[Style.Cpp.String].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Cpp.Character].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Cpp.Operator].ForeColor = Color.Black;

        editor.SetKeywords(0, "break case catch class const continue debugger default delete do else enum export extends false finally for function if import in instanceof let new null return static super switch this throw true try typeof var void while with yield async await of");
        editor.SetKeywords(1, "Array Boolean Date Error Function JSON Math Number Object Promise RegExp String Symbol Map Set WeakMap WeakSet console document window undefined NaN Infinity parseInt parseFloat isNaN isFinite");
    }

    private static void ApplyJson(Scintilla editor)
    {
        editor.LexerName = "json";

        editor.Styles[Style.Json.Default].ForeColor = Color.Black;
        editor.Styles[Style.Json.Number].ForeColor = Color.FromArgb(0, 128, 128);
        editor.Styles[Style.Json.String].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Json.PropertyName].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Json.Keyword].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Json.Operator].ForeColor = Color.Black;
        editor.Styles[Style.Json.Error].ForeColor = Color.Red;
    }

    private static void ApplyXml(Scintilla editor)
    {
        editor.LexerName = "xml";

        editor.Styles[Style.Xml.Default].ForeColor = Color.Black;
        editor.Styles[Style.Xml.Tag].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Xml.TagEnd].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Xml.Attribute].ForeColor = Color.Red;
        editor.Styles[Style.Xml.DoubleString].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Xml.SingleString].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Xml.Comment].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Xml.Entity].ForeColor = Color.FromArgb(128, 0, 128);
        editor.Styles[Style.Xml.CData].ForeColor = Color.FromArgb(128, 128, 128);
    }

    private static void ApplyCss(Scintilla editor)
    {
        editor.LexerName = "css";

        editor.Styles[Style.Css.Default].ForeColor = Color.Black;
        editor.Styles[Style.Css.Tag].ForeColor = Color.FromArgb(128, 0, 0);
        editor.Styles[Style.Css.Class].ForeColor = Color.FromArgb(128, 0, 0);
        editor.Styles[Style.Css.Id].ForeColor = Color.FromArgb(128, 0, 0);
        editor.Styles[Style.Css.Attribute].ForeColor = Color.Red;
        editor.Styles[Style.Css.Value].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Css.Comment].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Css.Operator].ForeColor = Color.Black;
    }

    private static void ApplyPython(Scintilla editor)
    {
        editor.LexerName = "python";

        editor.Styles[Style.Python.Default].ForeColor = Color.Black;
        editor.Styles[Style.Python.CommentLine].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Python.Number].ForeColor = Color.FromArgb(0, 128, 128);
        editor.Styles[Style.Python.String].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Python.Character].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Python.Word].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Python.Triple].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Python.TripleDouble].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Python.ClassName].ForeColor = Color.FromArgb(43, 145, 175);
        editor.Styles[Style.Python.DefName].ForeColor = Color.FromArgb(43, 145, 175);
        editor.Styles[Style.Python.Operator].ForeColor = Color.Black;
        editor.Styles[Style.Python.Decorator].ForeColor = Color.FromArgb(128, 0, 128);

        editor.SetKeywords(0, "and as assert async await break class continue def del elif else except finally for from global if import in is lambda None not or pass raise return try True False while with yield nonlocal");
    }

    private static void ApplySql(Scintilla editor)
    {
        editor.LexerName = "sql";

        editor.Styles[Style.Sql.Default].ForeColor = Color.Black;
        editor.Styles[Style.Sql.Comment].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Sql.CommentLine].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Sql.Number].ForeColor = Color.FromArgb(0, 128, 128);
        editor.Styles[Style.Sql.Word].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Sql.String].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Sql.Character].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Sql.Operator].ForeColor = Color.Black;

        editor.SetKeywords(0, "SELECT FROM WHERE AND OR NOT IN LIKE BETWEEN IS NULL ORDER BY ASC DESC GROUP BY HAVING INSERT INTO VALUES UPDATE SET DELETE CREATE TABLE ALTER DROP INDEX PRIMARY KEY FOREIGN REFERENCES UNIQUE CHECK DEFAULT CONSTRAINT JOIN INNER LEFT RIGHT OUTER FULL CROSS ON AS DISTINCT TOP LIMIT OFFSET UNION ALL EXCEPT INTERSECT CASE WHEN THEN ELSE END CAST CONVERT COALESCE NULLIF EXISTS ANY SOME COUNT SUM AVG MIN MAX UPPER LOWER TRIM SUBSTRING LEN LENGTH GETDATE NOW DATEADD DATEDIFF YEAR MONTH DAY HOUR MINUTE SECOND");
    }

    private static void ApplyCpp(Scintilla editor)
    {
        editor.LexerName = "cpp";

        editor.Styles[Style.Cpp.Default].ForeColor = Color.Black;
        editor.Styles[Style.Cpp.Comment].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Cpp.CommentLine].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Cpp.CommentDoc].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Cpp.Number].ForeColor = Color.FromArgb(0, 128, 128);
        editor.Styles[Style.Cpp.Word].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Cpp.Word2].ForeColor = Color.FromArgb(43, 145, 175);
        editor.Styles[Style.Cpp.String].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Cpp.Character].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.Cpp.Preprocessor].ForeColor = Color.FromArgb(128, 128, 128);
        editor.Styles[Style.Cpp.Operator].ForeColor = Color.Black;

        editor.SetKeywords(0, "auto break case char const continue default do double else enum extern float for goto if inline int long register restrict return short signed sizeof static struct switch typedef union unsigned void volatile while _Bool _Complex _Imaginary bool true false class public private protected virtual override final explicit friend mutable namespace new delete operator template this throw try catch using typename");
        editor.SetKeywords(1, "std cout cin endl string vector map set list queue stack pair make_pair printf scanf malloc free nullptr NULL size_t ptrdiff_t");
    }

    private static void ApplyMarkdown(Scintilla editor)
    {
        editor.LexerName = "markdown";

        editor.Styles[Style.Markdown.Default].ForeColor = Color.Black;
        editor.Styles[Style.Markdown.Header1].ForeColor = Color.FromArgb(0, 0, 128);
        editor.Styles[Style.Markdown.Header1].Bold = true;
        editor.Styles[Style.Markdown.Header2].ForeColor = Color.FromArgb(0, 0, 128);
        editor.Styles[Style.Markdown.Header2].Bold = true;
        editor.Styles[Style.Markdown.Header3].ForeColor = Color.FromArgb(0, 0, 128);
        editor.Styles[Style.Markdown.Header4].ForeColor = Color.FromArgb(0, 0, 128);
        editor.Styles[Style.Markdown.Header5].ForeColor = Color.FromArgb(0, 0, 128);
        editor.Styles[Style.Markdown.Header6].ForeColor = Color.FromArgb(0, 0, 128);
        editor.Styles[Style.Markdown.Code].ForeColor = Color.FromArgb(128, 0, 128);
        editor.Styles[Style.Markdown.Code].BackColor = Color.FromArgb(245, 245, 245);
        editor.Styles[Style.Markdown.Code2].ForeColor = Color.FromArgb(128, 0, 128);
        editor.Styles[Style.Markdown.Code2].BackColor = Color.FromArgb(245, 245, 245);
        editor.Styles[Style.Markdown.Strong1].Bold = true;
        editor.Styles[Style.Markdown.Strong2].Bold = true;
        editor.Styles[Style.Markdown.Em1].Italic = true;
        editor.Styles[Style.Markdown.Em2].Italic = true;
        editor.Styles[Style.Markdown.Link].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Markdown.Link].Underline = true;
    }

    private static void ApplyBatch(Scintilla editor)
    {
        editor.LexerName = "batch";

        editor.Styles[Style.Batch.Default].ForeColor = Color.Black;
        editor.Styles[Style.Batch.Comment].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.Batch.Word].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.Batch.Label].ForeColor = Color.FromArgb(128, 0, 0);
        editor.Styles[Style.Batch.Hide].ForeColor = Color.FromArgb(128, 128, 128);
        editor.Styles[Style.Batch.Command].ForeColor = Color.FromArgb(0, 0, 128);
        editor.Styles[Style.Batch.Identifier].ForeColor = Color.FromArgb(43, 145, 175);
        editor.Styles[Style.Batch.Operator].ForeColor = Color.Black;
    }

    private static void ApplyPowerShell(Scintilla editor)
    {
        editor.LexerName = "powershell";

        editor.Styles[Style.PowerShell.Default].ForeColor = Color.Black;
        editor.Styles[Style.PowerShell.Comment].ForeColor = Color.FromArgb(0, 128, 0);
        editor.Styles[Style.PowerShell.String].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.PowerShell.Character].ForeColor = Color.FromArgb(163, 21, 21);
        editor.Styles[Style.PowerShell.Number].ForeColor = Color.FromArgb(0, 128, 128);
        editor.Styles[Style.PowerShell.Variable].ForeColor = Color.FromArgb(0, 128, 128);
        editor.Styles[Style.PowerShell.Operator].ForeColor = Color.Black;
        editor.Styles[Style.PowerShell.Keyword].ForeColor = Color.FromArgb(0, 0, 255);
        editor.Styles[Style.PowerShell.Cmdlet].ForeColor = Color.FromArgb(0, 0, 128);
        editor.Styles[Style.PowerShell.Alias].ForeColor = Color.FromArgb(43, 145, 175);

        editor.SetKeywords(0, "begin break catch class continue data define do dynamicparam else elseif end exit filter finally for foreach from function if in param process return switch throw trap try until using var while workflow");
    }
}
