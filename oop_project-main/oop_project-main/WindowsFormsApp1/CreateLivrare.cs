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
    public partial class CreateLivrare : Form
    {
        public CreateLivrare()
        {
            InitializeComponent();



            foreach (Cont cont in Global.EzBox.GetConturi())
            {
                if (cont.GetUserName() != Global.cont.GetUserName())
                {
                    comboBox1.Items.Add(cont.GetUserName());
                }
            }

            foreach (SuperBox SuperBox in Global.EzBox.GetSuperBox())
            {
                comboBox2.Items.Add(SuperBox.GetLocatie());
            }

            foreach (SuperBox SuperBox in Global.EzBox.GetSuperBox())
            {
                comboBox3.Items.Add(SuperBox.GetLocatie());
            }


        }

        private void button1_Click(object sender, EventArgs e)
        {
            Cont aux = null;
            SuperBox SuperBox1 = null;
            SuperBox SuperBox2 = null;

            if (comboBox2.Text == comboBox3.Text)
            {
                MessageBox.Show("SuperBox-ul de trimitere nu poate coincide cu cel de livrare!");
            }
            else
            {

                foreach (Cont cont in Global.EzBox.GetConturi())
                {
                    if (cont.GetUserName() == comboBox1.Text)
                    {
                        aux = cont;
                    }
                }

                foreach (SuperBox superbox in Global.EzBox.GetSuperBox())
                {
                    if (superbox.GetLocatie() == comboBox2.Text)
                    {
                        SuperBox1 = superbox;
                    }

                    if (superbox.GetLocatie() == comboBox3.Text)
                    {
                        SuperBox2 = superbox;
                    }
                }


                Random random = new Random();
                int randomNumber = random.Next(10000, 99999);
                string idComanda = "AWB" + randomNumber;

                if (checkBox1.Checked)
                {
                    DateTime dataTrimitere = DateTime.Today;
                    DateTime dataLivrare = dataTrimitere.AddDays(5);
                    Comanda comanda = new Comanda(idComanda, SuperBox1, SuperBox2, Global.cont, aux, true, false, dataTrimitere, dataLivrare);
                    Global.EzBox.AdaugaComanda(comanda);
                    MessageBox.Show("Livrare plasata cu succes!");
                }
                else
                {
                    DateTime dataTrimitere = DateTime.Today;
                    DateTime dataLivrare = dataTrimitere.AddDays(10);
                    Comanda comanda = new Comanda(idComanda, SuperBox1, SuperBox2, Global.cont, aux, false, false, dataTrimitere, dataLivrare);
                    Global.EzBox.AdaugaComanda(comanda);
                    MessageBox.Show("Livrare plasata cu succes!");
                }
                this.Close();



            }
        }


    }
}
