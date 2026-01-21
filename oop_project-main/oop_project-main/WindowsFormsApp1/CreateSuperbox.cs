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
    public partial class CreateSuperbox : Form
    {
        public CreateSuperbox()
        {

            InitializeComponent();
            this.FormClosing += Form5_FormClosing;


        }
        private void button1_Click(object sender, EventArgs e)
        {

            label2.Visible = true;
            button1.Visible = false;
            button2.Visible = false;
            textBox2.Visible = true;
            button4.Visible = true;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Visible = false;
            button2.Visible = false;
            comboBox1.Visible = true;
            button3.Visible = true;

            foreach (SuperBox box in Global.EzBox.GetSuperBox())
            {
                comboBox1.Items.Add(box.GetLocatie());
            }

        }


        private void button4_Click(object sender, EventArgs e)
        {
            string Locatie = textBox2.Text;

            int ok = 0;

            foreach (SuperBox super in Global.EzBox.GetSuperBox())
            {
                if (super.GetLocatie() == Locatie)
                {
                    MessageBox.Show("Exista deja un super box cu Locatia introdusa!");
                    ok = 1;
                    break;
                }
            }

            if (ok == 0)
            {

                Random random = new Random();
                int randomNumber = random.Next(10000, 99999);
                string Idaux = "SuperBox" + randomNumber;
                SuperBox superbox = new SuperBox(Idaux, Locatie);
                Global.EzBox.AdaugaSuperBox(superbox);
                MessageBox.Show("SuperBox adaugat cu succes!");
                this.Close();
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            string Locaiteaux = comboBox1.Text;
            int ok = 1;
            foreach (SuperBox item in Global.EzBox.GetSuperBox())
            {

                foreach(Comanda comanda in Global.EzBox.GetComenzi())
                {
                    if(comanda.GetSuperBoxOrigine().GetLocatie() == Locaiteaux || comanda.GetSuperBoxDestinatie().GetLocatie() == Locaiteaux)
                    {
                        ok = 0;
                        break;
                    }
                }

                if(Locaiteaux == item.GetLocatie() && ok == 0)
                {
                    MessageBox.Show("SuperBox ul nu poate fi eliminat exista comenzi care depind de acesta!");
                    this.Close();
                }

                else if (Locaiteaux == item.GetLocatie() && ok == 1)
                {
                    Global.EzBox.RemoveSuperBox(item);
                    MessageBox.Show("Superbox eliminat cu succes!");
                    this.Close();
                    break;
                }
            }


        }
        string connectionString = "Server=localhost;Database=firma;Uid=root;Pwd=root;";
        private void Form5_FormClosing(object sender, FormClosingEventArgs e)
        {

            BD.ScriereInDBSuperBox(connectionString);

        }


    }
}
