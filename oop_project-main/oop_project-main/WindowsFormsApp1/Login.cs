using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace WindowsFormsApp1
{

    public partial class Login : Form
    {
        public Login()
        {

            InitializeComponent();
            this.FormClosing += Form1_FormClosing;

        }


        private void button1_Click(object sender, EventArgs e)
        {
            string Username = null;
            string Password = null;

            Username = textBox1.Text;
            Password = textBox2.Text;
            int ok = 0;

            foreach (Cont cont in Global.EzBox.GetConturi())
            {
                if ((cont.GetUserName() == Username || cont.GetEmail() == Username) && (cont.GetPassword() == Password))
                {
                    textBox1.Text = null;
                    textBox2.Text = null;
                    ok = 1;
                    Global.cont = cont.DeepCopy();
                    Dashboard form3 = new Dashboard();
                    form3.Show();
                    this.Hide();
                    break;
                }

            }

            if (ok == 0)
            {
                MessageBox.Show("UserName sau Parola Gresita!");
                textBox1.Text = null;
                textBox2.Text = null;
            }

        }

        private void label4_Click(object sender, EventArgs e)
        {
            CreateAcc form2 = new CreateAcc();
            form2.Show();
        }
        string connectionString = "Server=localhost;Database=firma;Uid=root;Pwd=root;";

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            BD.ScriereinBDListaAsteptare(connectionString);
            Application.Exit();

        }

        private void Login_Load(object sender, EventArgs e)
        {

        }
    }
}

