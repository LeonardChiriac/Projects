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


    public partial class CreareComanda : Form
    {


        public string SuperBox1;
        public string SuperBox2;
        public bool Urgenta;
        public Button Buton;

        public CreareComanda()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SuperBox1 = comboBox1.Text;
            SuperBox2 = comboBox2.Text;
            if (checkBox1.Checked)
            {
                Urgenta = true;
            }
            else
            {
                Urgenta = false;
            }

           

            this.Close();
        }
    }
}
