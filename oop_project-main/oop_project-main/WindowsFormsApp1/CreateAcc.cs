using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class CreateAcc : Form
    {
        public CreateAcc()
        {
            InitializeComponent();

            this.FormClosing += Form2_FormClosing;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string Username = textBox1.Text;
            string Email = textBox2.Text;
            string Password = textBox3.Text;
            int ok = 0;
            int ok2 = 0;
            foreach (Cont cont in Global.EzBox.GetConturi())
            {

                if (cont.GetUserName() == Username)
                {
                    ok = 1;
                }

                if (cont.GetEmail() == Email)
                {
                    ok2 = 2;
                }
            }

            if (ok == 1 && ok2 == 0)
            {
                MessageBox.Show("UserName-ul este atribuit deja altui cont");
                textBox1.Text = null;
                textBox2.Text = null;
                textBox3.Text = null;
            }
            else if (ok == 0 && ok2 == 2)
            {
                MessageBox.Show("Email-ul este atribuit deja altui cont");
                textBox1.Text = null;
                textBox2.Text = null;
                textBox3.Text = null;
            }
            else if (ok + ok2 == 3)
            {
                MessageBox.Show("UserName- si Emailul sunt atribuit deja altui cont");
                textBox1.Text = null;
                textBox2.Text = null;
                textBox3.Text = null;
            }
            else if (ok == 0 && ok2 == 0 && (!checkBox1.Checked))
            {
                Random random = new Random();
                int randomNumber = random.Next(10000, 99999);
                string IdUtilizator = "Ut" + randomNumber;
                Console.WriteLine($"Număr între 1 și 10: {randomNumber}");
                Utilizator utilizator = new Utilizator(Username, Email, Password, IdUtilizator);
                Global.EzBox.AdaugaCont(utilizator);
                MessageBox.Show("Contul a fost creat cu succes! Apasati Ok pentru a va intoarce la pagina de Sign in!");
                this.Close();
            }
            else if (ok == 0 && ok2 == 0 && checkBox1.Checked)
            {
                Admin admin = new Admin(Username, Email, Password, false);
                Global.EzBox.AdaugaAdminListaAsteptare(admin);
                MessageBox.Show("Contul a fost trimis la aprobare!");
                this.Close();

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        string connectionString = "Server=localhost;Database=firma;Uid=root;Pwd=root;";
        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {

            BD.ScriereInBDUtilizator(connectionString);
        }


    }
}
