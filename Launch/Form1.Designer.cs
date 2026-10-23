using Org.BouncyCastle.Asn1.Crmf;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace LocalServerLauncher;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
        button1 = new Button();
        button2 = new Button();
        button3 = new Button();
        button4 = new Button();
        button5 = new Button();
        label1 = new Label();
        SuspendLayout();
        // 
        // button1
        // 
        button1.Location = new Point(12, 12);
        button1.Name = "button1";
        button1.Size = new Size(154, 30);
        button1.TabIndex = 0;
        button1.Text = "server/game start";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click;
        // 
        // button2
        // 
        button2.Location = new Point(12, 48);
        button2.Name = "button2";
        button2.Size = new Size(154, 30);
        button2.TabIndex = 1;
        button2.Text = "Launcher option";
        button2.UseVisualStyleBackColor = true;
        button2.Click += button2_Click;
        // 
        // button3
        // 
        button3.Location = new Point(12, 84);
        button3.Name = "button3";
        button3.Size = new Size(154, 30);
        button3.TabIndex = 2;
        button3.Text = "set pin file";
        button3.UseVisualStyleBackColor = true;
        button3.Click += button3_Click;
        // 
        // button4
        // 
        button4.Location = new Point(12, 120);
        button4.Name = "button4";
        button4.Size = new Size(154, 30);
        button4.TabIndex = 3;
        button4.Text = "join game";
        button4.UseVisualStyleBackColor = true;
        button4.Click += button4_Click;
        // 
        // button5
        // 
        button5.Location = new Point(12, 156);
        button5.Name = "button5";
        button5.Size = new Size(154, 30);
        button5.TabIndex = 4;
        button5.Text = "server setting";
        button5.UseVisualStyleBackColor = true;
        button5.Click += button5_Click;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(339, 171);
        label1.Name = "label1";
        label1.Size = new Size(45, 15);
        label1.TabIndex = 5;
        label1.Text = "by GLS";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(390, 195);
        Controls.Add(label1);
        Controls.Add(button5);
        Controls.Add(button4);
        Controls.Add(button3);
        Controls.Add(button1);
        Controls.Add(button2);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "Form1";
        Text = "GLS 2.0.0";
        Load += Form1_Load;
        ResumeLayout(false);
        PerformLayout();
    }

    private Button button1;
    private Button button2;
    private Button button3;
    private Button button4;
    private Button button5;
    private Label label1;
}