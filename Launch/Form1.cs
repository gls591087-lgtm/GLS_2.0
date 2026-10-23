
using KartRider.Data;
using KartRider.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LocalServerLauncher;

public partial class Form1 : Form
{
    private const string DefaultExecutable = "KartRider.exe";

    private Task? serverTask;

    public Form1()
    {
        InitializeComponent();
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
    }

    // ============================================================
    // SERVER START
    // ============================================================


    private async void button1_Click(object sender, EventArgs e)
    {
        if (KartRider.Net.Server.IsAlive)
        {
            MessageBox.Show(
                "The server is already running.",
                "Server",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        button1.Enabled = false;

        try
        {
            EnsureSelectedAccountExists();

            string baseDir = AppContext.BaseDirectory;

            // ----------------------------------------------------
            // Create Configuration.ini
            // ----------------------------------------------------

            string cfg =
                "[Server]\r\n" +
                $"ExternalIP={ServerHostSettings.ExternalIP}\r\n" +
                "Port=3305\r\n" +
                "AutoRestartTime=0\r\n" +
                "DataPath=Data\r\n" +
                "\r\n" +
                "[Database]\r\n" +
                "Host=local\r\n" +
                "Schema=kcdb\r\n" +
                "Nickname=player\r\n" +
                "Password=player\r\n";

            string configPath =
                Path.Combine(
                    baseDir,
                    "Configuration.ini");

            // Always save the current server settings
            File.WriteAllText(
                configPath,
                cfg,
                Encoding.UTF8);

            // ----------------------------------------------------
            // Initialize launcher settings
            // ----------------------------------------------------

            Settings.Initialize();

            Database.Host = "local";

            if (!Database.LocalMode)
            {
                Database.EnableLocalMode(
                    ServerHostSettings.AccountCount);
            }

            Database.SetLocalLoginAccount(
                opithon.PlayerNumber);

            // ----------------------------------------------------
            // Start the server
            // ----------------------------------------------------

            serverTask = Task.Run(
                KartRider.Net.Server.Start);

            // ----------------------------------------------------
            // Wait for the server without blocking the UI thread
            // ----------------------------------------------------

            DateTime deadline =
                DateTime.UtcNow.AddSeconds(15);

            while (!KartRider.Net.Server.IsAlive &&
                   !serverTask.IsCompleted &&
                   DateTime.UtcNow < deadline)
            {
                await Task.Delay(100);
            }

            if (!KartRider.Net.Server.IsAlive)
            {
                throw new InvalidOperationException(
                    "The server did not become ready within 15 seconds. Check Configuration.ini, the Data folder, and the server console for errors.");
            }

            // ----------------------------------------------------
            // Launch KartRider.exe
            // ----------------------------------------------------

            if (opithon.Checkbox1Enabled)
            {
                LaunchKartRider();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Server Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            button1.Enabled = true;
        }
    }

    // ============================================================
    // OPTION WINDOW
    // ============================================================

    private void button2_Click(
        object sender,
        EventArgs e)
    {
        using opithon option =
            new opithon();

        option.ShowDialog(this);
    }

    // ============================================================
    // FORM LOAD
    // ============================================================

    private void Form1_Load(
        object sender,
        EventArgs e)
    {
    }

    // ============================================================
    // KART RIDER LAUNCH
    // ============================================================

    private static void LaunchKartRider()
    {
        EnsureSelectedAccountExists();

        string baseDir =
            AppContext.BaseDirectory;

        string exePath =
            Path.Combine(
                baseDir,
                DefaultExecutable);

        if (!File.Exists(exePath))
        {
            MessageBox.Show(
                $"KartRider.exe was not found.\n\n{exePath}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        try
        {
            CreateLauncherProfile(baseDir);

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "-profile:launcher",
                WorkingDirectory = baseDir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "KartRider Launch Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void EnsureSelectedAccountExists()
    {
        if (opithon.PlayerNumber > ServerHostSettings.AccountCount)
        {
            throw new InvalidOperationException(
                $"Account player{opithon.PlayerNumber} is not configured. Increase the account count to at least {opithon.PlayerNumber} in Server Settings.");
        }
    }

    // ============================================================
    // CREATE PROFILE
    // ============================================================


    private static void CreateLauncherProfile(string baseDir)
    {
        string profileDir =
            Path.Combine(
                baseDir,
                "Profile",
                "kr");

        Directory.CreateDirectory(profileDir);

        string profilePath =
            Path.Combine(
                profileDir,
                "launcher.xml");

        int playerNumber = opithon.PlayerNumber;

        string username = $"player{playerNumber}";
        string password = $"player{playerNumber}";

        string profileXml =
            "<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n" +
            "<profile>\r\n" +
            $"  <username>{username}</username>\r\n" +
            $"  <password>{password}</password>\r\n" +
            "</profile>";

        File.WriteAllText(
            profilePath,
            profileXml,
            Encoding.Unicode);
    }



    // ============================================================
    // OPEN FORM2
    // ============================================================

    private void button3_Click(
        object sender,
        EventArgs e)
    {
        Form2 form =
            new Form2();

        form.Show();
    }

    // ============================================================
    // OPEN USER SET
    // ============================================================

    private void button5_Click(
        object sender,
        EventArgs e)
    {
        userset form =
            new userset();

        form.Show();
    }

    // ============================================================
    // MANUAL KART RIDER LAUNCH
    // ============================================================

    private void button4_Click(
        object sender,
        EventArgs e)
    {
        try
        {
            LaunchKartRider();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "KartRider Launch Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
