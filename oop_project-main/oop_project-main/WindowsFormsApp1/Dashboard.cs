using MySql.Data.MySqlClient;
using Org.BouncyCastle.Tls.Crypto;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace WindowsFormsApp1
{
    public partial class Dashboard : Form
    {
        public Dashboard()
        {
            InitializeComponent();
           


            if (Global.cont is Admin)
            {
                button4.Visible = true;
                button6.Visible = true;
            }

            this.FormClosing += Form3_FormClosing;


            BD.Trimise(flowLayoutPanel2);


        }


        private void button1_Click(object sender, EventArgs e)
        {
            BD.Trimise(flowLayoutPanel2);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            BD.Destinate(flowLayoutPanel2);
        }

        private void button4_Click(object sender, EventArgs e)
        {

            BD.InboxButton(flowLayoutPanel2);
           
        }

        private void button3_Click(object sender, EventArgs e)
        {
            CreateLivrare from4 = new CreateLivrare();
            from4.Show();
            flowLayoutPanel2.Controls.Clear();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            CreateSuperbox from5 = new CreateSuperbox();
            from5.Show();
        }

        string connectionString = "Server=localhost;Database=firma;Uid=root;Pwd=root;";
        private void Form3_FormClosing(object sender, FormClosingEventArgs e)
        {

            BD.ScriereinBDAdmin(connectionString);
            BD.SaveComenziToDatabase(connectionString);

           Login form = new Login();
            form.Show();
            

        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.Close();
            Login form = new Login();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            BD.ToateComenzile(flowLayoutPanel2);
        }
    }
}