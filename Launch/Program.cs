using LocalServerLauncher;
using System;
using System.Windows.Forms;

namespace MyApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.Run(new Form1());
        }
    }
}