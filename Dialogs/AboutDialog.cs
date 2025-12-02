using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RagePad.Services;

namespace RagePad.Dialogs;

/// <summary>
/// About dialog showing app info and credits.
/// </summary>
internal sealed class AboutDialog : Form
{
    public AboutDialog()
    {
        Text = "About RagePad";
        Width = 400;
        Height = 320;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        InitializeControls();
    }

    private void InitializeControls()
    {
        int yOffset = 20;

        // Logo image
        var imagePath = Path.Combine(AppInfo.BaseDirectory, "RagePad.png");
        if (File.Exists(imagePath))
        {
            var pictureBox = new PictureBox
            {
                Image = Image.FromFile(imagePath),
                SizeMode = PictureBoxSizeMode.Zoom,
                Left = 50,
                Top = yOffset,
                Width = 280,
                Height = 150
            };
            Controls.Add(pictureBox);
            yOffset = 180;
        }

        // Version
        var versionLabel = new Label
        {
            Text = $"Version {AppInfo.GetVersion()}",
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.Gray,
            Left = 0,
            Top = yOffset,
            Width = ClientSize.Width,
            Height = 25,
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(versionLabel);

        // Author
        var authorLabel = new Label
        {
            Text = "Author: Rajorshi Biswas",
            Font = new Font("Segoe UI", 10f),
            Left = 0,
            Top = versionLabel.Bottom + 10,
            Width = ClientSize.Width,
            Height = 22,
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(authorLabel);

        // Email link
        var emailLink = new LinkLabel
        {
            Text = "ragebiswas@gmail.com",
            Font = new Font("Segoe UI", 10f),
            Left = 0,
            Top = authorLabel.Bottom,
            Width = ClientSize.Width,
            Height = 22,
            TextAlign = ContentAlignment.MiddleCenter
        };
        emailLink.LinkClicked += (s, e) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo("mailto:ragebiswas@gmail.com") 
                { 
                    UseShellExecute = true 
                });
            }
            catch { }
        };
        Controls.Add(emailLink);

        // OK button
        var btnOk = new Button
        {
            Text = "OK",
            Left = (ClientSize.Width - 80) / 2,
            Top = emailLink.Bottom + 15,
            Width = 80,
            DialogResult = DialogResult.OK
        };
        Controls.Add(btnOk);
        AcceptButton = btnOk;
    }
}
