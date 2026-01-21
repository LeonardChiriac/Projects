using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {


            string connectionString = "Server=localhost;Database=firma;Uid=root;Pwd=root;";

            BD.CitireDinBDUtilizator(connectionString);
            BD.CitireDinBDAdmin(connectionString);
            BD.CitireDinBDSuperBoxuri(connectionString);
            BD.CitireDinBdListaAsteptare(connectionString);
            BD.CitireDinBDComenzi(connectionString);



            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Login());


        }
    }
}