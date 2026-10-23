using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LocalServerLauncher
{
    public partial class opithon : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        public static bool Checkbox1Enabled { get; private set; }

        // Current numericUpDown1 value
        public static int PlayerNumber { get; private set; } = 1;

        public opithon()
        {
            InitializeComponent();

            Checkbox1Enabled = checkBox1.Checked;
            PlayerNumber = (int)numericUpDown1.Value;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Checkbox1Enabled = checkBox1.Checked;
        }

        private void opithon_Load(object sender, EventArgs e)
        {
            Checkbox1Enabled = checkBox1.Checked;
            PlayerNumber = (int)numericUpDown1.Value;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (checkBox2.Checked)
                {
                    AllocConsole();
                    Console.WriteLine("open console!");
                }
                else
                {
                    FreeConsole();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            PlayerNumber = (int)numericUpDown1.Value;
        }
    }
}

