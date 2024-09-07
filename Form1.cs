using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Text.RegularExpressions;

namespace checks
{
    public partial class Form1 : Form
    {
        public string selectedPath;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selectedPath = dialog.SelectedPath;
                    PerformChecks();
                }
            }
        }

        private async void PerformChecks()
        {
            string[] directories = selectedPath.Split(Path.DirectorySeparatorChar);

            for (int i = 1; i < directories.Length; i++)
            {
                string dir = directories[i];

                // Replace spaces with underscores
                if (dir.Contains(" "))
                {
                    directories[i] = dir.Replace(" ", "_");
                    MessageBox.Show($"Renamed '{dir}' to '{directories[i]}' due to spaces.");
                }

                // Check if the directory starts or ends with an underscore
                if (dir.StartsWith("_") || dir.EndsWith("_"))
                {
                    MessageBox.Show($"Directory '{dir}' starts or ends with an underscore.");
                    return;
                }

                // Check if the directory starts or ends with a digit
                if (char.IsDigit(dir[0]) || char.IsDigit(dir[dir.Length-1])) 
                {
                    MessageBox.Show($"Directory '{dir}' starts or ends with a digit.");
                    return;
                }

                // Check if the directory contains special characters
                if (ContainsSpecialCharacter(dir))
                {
                    MessageBox.Show($"Directory '{dir}' contains special characters.");
                    checkBox3.Visible = true;
                    return;
                }
            }

            await UpdateUI();
            selectedPath = string.Join(Path.DirectorySeparatorChar.ToString(), directories);
        }

        private bool ContainsSpecialCharacter(string directory)
        {
            return Regex.IsMatch(directory, @"[^a-zA-Z0-9_ ]");
        }


        private async Task UpdateUI()
        {
            // Simulate UI update delays
            await Task.Delay(500);
            checkBox1.Visible = true;
            checkBox1.Text = " Selected Folder Blanked Checked!";
            checkBox1.Checked = true;

            await Task.Delay(500);
            checkBox2.Visible = true;
            checkBox2.Text = " Selected Folder Digit checked!";
            checkBox2.Checked = true;

            await Task.Delay(500);
            checkBox3.Visible = true;
            checkBox3.Text = "Selected Folder Underscore Checked!";
            checkBox3.Checked = true;

            await Task.Delay(500);
            checkBox4.Visible = true;
            checkBox4.Text = "Selected Path Length checked!";
            checkBox4.Checked = true;

            await Task.Delay(500);
            checkBox5.Visible = true;
            checkBox5.Text = "Selected Folder White Spaces checked!";
            checkBox5.Checked = true;
            textBox1.Text = selectedPath;
        }
    }
}
