using System;
using System.Net;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace LocalServerLauncher
{
    public partial class userset : Form
    {
        public userset()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.Size = new System.Drawing.Size(215, 202);
            this.MaximizeBox = false;
            this.MinimizeBox = true;

            //215, 202
            //579, 340
            LoadSettingsToUI();
        }

        private void userset_Load(object sender, EventArgs e)
        {
            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            textBox1.Text = ServerHostSettings.ExternalIP;
            numericUpDown1.Value = ServerHostSettings.AccountCount;

            textBox3.Text = ServerHostSettings.DragFactorBonus.ToString();
            textBox4.Text = ServerHostSettings.ForwardAccelForceBonus.ToString();
            textBox5.Text = ServerHostSettings.BackwardAccelForceBonus.ToString();
            textBox6.Text = ServerHostSettings.GripBrakeForceBonus.ToString();
            textBox7.Text = ServerHostSettings.SlipBrakeForceBonus.ToString();
            textBox8.Text = ServerHostSettings.DriftEscapeForceBonus.ToString();
            textBox9.Text = ServerHostSettings.CornerDrawFactorBonus.ToString();
            textBox10.Text = ServerHostSettings.DriftMaxGaugeBonus.ToString();
            textBox11.Text = ServerHostSettings.BoostAccelFactorBonus.ToString();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
        }

        private void numericUpDown1_ValueChanged(
            object sender,
            EventArgs e)
        {
            ServerHostSettings.AccountCount =
                (int)numericUpDown1.Value;
        }

        // button1 = Save IP and account settings
        private void button1_Click(
            object sender,
            EventArgs e)
        {
            string ip = textBox1.Text.Trim();

            IPAddress temp;

            if (!IPAddress.TryParse(ip, out temp))
            {
                MessageBox.Show(
                    "Enter a valid IP address.\nExample: 127.0.0.1",
                    "Invalid IP",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            ServerHostSettings.ExternalIP = ip;

            ServerHostSettings.AccountCount =
                (int)numericUpDown1.Value;

            MessageBox.Show(
                "Settings saved.\n\n" +
                "Host IP: " +
                ServerHostSettings.ExternalIP +
                "\nAccount count: " +
                ServerHostSettings.AccountCount,
                "Settings Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Close();
        }

        // button2 = Apply kart adjustments


        private void button3_Click(object sender, EventArgs e)
        {
            if (this.Size == new System.Drawing.Size(215, 202))
            {
                this.Size = new System.Drawing.Size(579, 340);
                button3.Text = "close Adjustments";
            }
            else
            {
                this.Size = new System.Drawing.Size(215, 202);
                button3.Text = "Open Adjustments";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            float drag;
            float forward;
            float backward;
            float gripBrake;
            float slipBrake;
            float driftEscape;
            float cornerDraw;
            float driftGauge;
            float boostAccel;

            if (!float.TryParse(textBox3.Text, out drag) ||
                !float.TryParse(textBox4.Text, out forward) ||
                !float.TryParse(textBox5.Text, out backward) ||
                !float.TryParse(textBox6.Text, out gripBrake) ||
                !float.TryParse(textBox7.Text, out slipBrake) ||
                !float.TryParse(textBox8.Text, out driftEscape) ||
                !float.TryParse(textBox9.Text, out cornerDraw) ||
                !float.TryParse(textBox10.Text, out driftGauge) ||
                !float.TryParse(textBox11.Text, out boostAccel))
            {
                MessageBox.Show(
                    "Adjustment values must contain numbers only.",
                    "Input Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            ServerHostSettings.DragFactorBonus = drag;
            ServerHostSettings.ForwardAccelForceBonus = forward;
            ServerHostSettings.BackwardAccelForceBonus = backward;
            ServerHostSettings.GripBrakeForceBonus = gripBrake;
            ServerHostSettings.SlipBrakeForceBonus = slipBrake;
            ServerHostSettings.DriftEscapeForceBonus = driftEscape;
            ServerHostSettings.CornerDrawFactorBonus = cornerDraw;
            ServerHostSettings.DriftMaxGaugeBonus = driftGauge;
            ServerHostSettings.BoostAccelFactorBonus = boostAccel;

            MessageBox.Show(

                "Drag: +" + drag +
                "\nForwardAccel: +" + forward +
                "\nBackwardAccel: +" + backward +
                "\nGripBrake: +" + gripBrake +
                "\nSlipBrake: +" + slipBrake +
                "\nDriftEscape: +" + driftEscape +
                "\nCornerDraw: +" + cornerDraw +
                "\nDriftMaxGauge: +" + driftGauge +
                "\nBoostAccel: +" + boostAccel,
                "",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    public static class ServerHostSettings
    {
        // Server IP address
        public static string ExternalIP { get; set; }
            = "127.0.0.1";

        // Number of accounts to create
        public static int AccountCount { get; set; }
            = 1;

        // ============================================================
        // Kart physics adjustments
        // ============================================================

        public static float DragFactorBonus { get; set; }
            = 0.004f;

        public static float ForwardAccelForceBonus { get; set; }
            = 370.0f;

        public static float BackwardAccelForceBonus { get; set; }
            = 380.0f;

        public static float GripBrakeForceBonus { get; set; }
            = 330.0f;

        public static float SlipBrakeForceBonus { get; set; }
            = 195.0f;

        public static float DriftEscapeForceBonus { get; set; }
            = 1400.0f;

        public static float CornerDrawFactorBonus { get; set; }
            = 0.01f;

        public static float DriftMaxGaugeBonus { get; set; }
            = 1240.0f;

        public static float BoostAccelFactorBonus { get; set; }
            = 0.126f;
    }
}
/*
         outPacket.WriteEncryptedFloat(Kart.Mass);
        outPacket.WriteEncryptedFloat(Kart.AirFriction);
        outPacket.WriteEncryptedFloat(Kart.DragFactor + 0.004f);          // approximately 0.671
        outPacket.WriteEncryptedFloat(Kart.ForwardAccelForce + 370.0f);   // remains 2674 (already sufficient)
        outPacket.WriteEncryptedFloat(Kart.BackwardAccelForce + 380.0f);  // approximately 2205
        outPacket.WriteEncryptedFloat(Kart.GripBrakeForce + 330.0f);      // 2400
        outPacket.WriteEncryptedFloat(Kart.SlipBrakeForce + 195.0f);      // 1610
        outPacket.WriteEncryptedFloat(Kart.MaxSteerAngle);
        outPacket.WriteEncryptedFloat(Kart.SteerConstraint + 0.5f);       // approximately 25.11
        outPacket.WriteEncryptedFloat(Kart.FrontGripFactor);
        outPacket.WriteEncryptedFloat(Kart.RearGripFactor);
        outPacket.WriteEncryptedFloat(Kart.DriftTriggerFactor);
        outPacket.WriteEncryptedFloat(Kart.DriftTriggerTime);
        outPacket.WriteEncryptedFloat(Kart.DriftSlipFactor);
        outPacket.WriteEncryptedFloat(Kart.DriftEscapeForce + 1400.0f);   // remains 5600 (already excessive)
        outPacket.WriteEncryptedFloat(Kart.CornerDrawFactor + 0.01f);     // 0.264
        outPacket.WriteEncryptedFloat(Kart.DriftLeanFactor);
        outPacket.WriteEncryptedFloat(Kart.SteerLeanFactor);
        outPacket.WriteEncryptedFloat(Kart.DriftMaxGauge + 1240.0f);      // 5100
        outPacket.WriteEncryptedFloat(Kart.NormalBoosterTime + 100.0f);   // 3000
        outPacket.WriteEncryptedFloat(Kart.TeamBoosterTime + 150.0f);     // 4500
        outPacket.WriteEncryptedFloat(Kart.BoostAccelFactor + 0.126f);    // 1.62
*/
