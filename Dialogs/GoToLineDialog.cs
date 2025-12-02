using System.Drawing;
using System.Windows.Forms;

namespace RagePad.Dialogs;

/// <summary>
/// Dialog for jumping to a specific line number.
/// </summary>
internal sealed class GoToLineDialog : Form
{
    private readonly TextBox _lineNumberBox;
    
    public int SelectedLine { get; private set; }

    public GoToLineDialog(int currentLine, int maxLine)
    {
        Text = "Go to Line";
        Width = 300;
        Height = 120;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        var label = new Label 
        { 
            Text = "Line number:", 
            Left = 10, 
            Top = 15, 
            Width = 80 
        };
        Controls.Add(label);

        _lineNumberBox = new TextBox 
        { 
            Left = 95, 
            Top = 12, 
            Width = 170, 
            Text = currentLine.ToString() 
        };
        _lineNumberBox.SelectAll();
        Controls.Add(_lineNumberBox);

        var btnOk = new Button 
        { 
            Text = "OK", 
            Left = 110, 
            Top = 45, 
            Width = 75, 
            DialogResult = DialogResult.OK 
        };
        btnOk.Click += (s, e) =>
        {
            if (int.TryParse(_lineNumberBox.Text, out int line))
            {
                SelectedLine = System.Math.Max(1, System.Math.Min(line, maxLine));
            }
            else
            {
                SelectedLine = currentLine;
            }
        };
        Controls.Add(btnOk);

        AcceptButton = btnOk;
    }
}
