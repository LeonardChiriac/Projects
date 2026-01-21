using MySql.Data.MySqlClient;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{

    class BD
    {
        public static void ScriereinBDAdmin(string connectionString)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    foreach (Cont cont in Global.EzBox.GetConturi())
                    {
                        if (cont is Admin)
                        {
                            Admin admin = (Admin)cont;

                            string checkQuery = "SELECT COUNT(*) FROM Conturi WHERE UserName = @UserName OR Email = @Email";
                            try
                            {
                                using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
                                {
                                    checkCmd.Parameters.AddWithValue("@UserName", admin.GetUserName());
                                    checkCmd.Parameters.AddWithValue("@Email", admin.GetEmail());

                                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                                    if (count == 0)
                                    {
                                        string query = "INSERT INTO Conturi (UserName, Email, Parola, IsAdmin) VALUES (@UserName, @Email, @Parola, @IsAdmin)";
                                        try
                                        {
                                            using (MySqlCommand cmd = new MySqlCommand(query, connection))
                                            {
                                                cmd.Parameters.AddWithValue("@UserName", admin.GetUserName());
                                                cmd.Parameters.AddWithValue("@Email", admin.GetEmail());
                                                cmd.Parameters.AddWithValue("@Parola", admin.GetPassword());
                                                cmd.Parameters.AddWithValue("@IsAdmin", admin.GetIsAdmin());

                                                cmd.ExecuteNonQuery();
                                            }

                                            Console.WriteLine("Administratorul a fost adăugat cu succes!");
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Eroare la inserarea administratorului: {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Administratorul există deja în baza de date.");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Eroare la verificarea existentei administratorului: {ex.Message}");
                            }
                        }
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la conectarea la baza de date: {ex.Message}");
            }
        }

        public static void ScriereInBDUtilizator(string connectionString)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    foreach (Cont cont in Global.EzBox.GetConturi())
                    {
                        if (cont is Utilizator)
                        {
                            Utilizator ut = (Utilizator)cont;

                            string checkQuery = "SELECT COUNT(*) FROM Conturi WHERE UserName = @UserName OR Email = @Email";
                            try
                            {
                                using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
                                {
                                    checkCmd.Parameters.AddWithValue("@UserName", ut.GetUserName());
                                    checkCmd.Parameters.AddWithValue("@Email", ut.GetEmail());

                                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                                    if (count == 0)
                                    {
                                        string query = "INSERT INTO Conturi (TipCont, UserName, Email, Parola, IdUtilizator) " +
                                                       "VALUES (@TipCont, @UserName, @Email, @Parola, @IdUtilizator)";
                                        try
                                        {
                                            using (MySqlCommand cmd = new MySqlCommand(query, connection))
                                            {
                                                cmd.Parameters.AddWithValue("@TipCont", "Utilizator");
                                                cmd.Parameters.AddWithValue("@UserName", ut.GetUserName());
                                                cmd.Parameters.AddWithValue("@Email", ut.GetEmail());
                                                cmd.Parameters.AddWithValue("@Parola", ut.GetPassword());
                                                cmd.Parameters.AddWithValue("@IdUtilizator", ut.GetIdUtilizator());

                                                cmd.ExecuteNonQuery();
                                            }

                                            Console.WriteLine("Utilizatorul a fost adăugat cu succes!");
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Eroare la inserarea utilizatorului: {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Utilizatorul există deja în baza de date.");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Eroare la verificarea existenței utilizatorului: {ex.Message}");
                            }
                        }
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la conectarea la baza de date: {ex.Message}");
            }
        }







        public static void CitireDinBDComenzi(string connectionString)
        {
            string query = @"
        SELECT 
        c.IdComanda AS ComandaId,
        c.Urgenta AS Urgenta,
        c.Ridicata AS Ridicata,
        c.DataTrimitere AS DataTrimitere,
        c.DataLivrare AS DataLivrare,

        u1.Id AS IdTrimitere,
        u1.UserName AS UserNameTrimitere,
        u1.Email AS EmailTrimitere,
        u1.TipCont AS TipContTrimitere,
        u1.Parola AS ParolaContTrimitere,
        u1.Idutilizator AS IdUtilizatorTrimitere,
        u1.IsAdmin AS IsAdminTrimitere,

        u2.Id AS IdLivrare,
        u2.UserName AS UserNameLivrare,
        u2.Email AS EmailLivrare,
        u2.TipCont AS TipContLivrare,
        u2.Parola AS ParolaContLivrare,
        u2.Idutilizator AS IdUtilizatorLivrare,
        u2.IsAdmin AS IsAdminLivrare,

        sb1.Idsuperbox AS IdSuperBoxOrigine,
        sb1.Locatie AS LocatieOrigine,
        sb2.Idsuperbox AS IdSuperBoxDestinatie,
        sb2.Locatie AS LocatieDestinatie
        FROM Comenzi c
        JOIN Conturi u1 ON c.IdUtilizatorTrimitere = u1.Id
        JOIN Conturi u2 ON c.IdUtilizatorLivrare = u2.Id
        JOIN SuperBox sb1 ON c.SuperBoxOrigine = sb1.Id
        JOIN SuperBox sb2 ON c.SuperBoxDestinatie = sb2.Id;";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    string IDComanda = reader["ComandaId"].ToString();

                                    SuperBox origine = new SuperBox(
                                        reader["IdSuperBoxOrigine"].ToString(),
                                        reader["LocatieOrigine"].ToString()
                                    );

                                    SuperBox destinatie = new SuperBox(
                                        reader["IdSuperBoxDestinatie"].ToString(),
                                        reader["LocatieDestinatie"].ToString()
                                    );

                                    Cont trimitere;
                                    string tipContTrimitere = reader["TipContTrimitere"].ToString();
                                    if (tipContTrimitere == "Admin")
                                    {
                                        trimitere = new Admin(
                                            reader["UserNameTrimitere"].ToString(),
                                            reader["EmailTrimitere"].ToString(),
                                            reader["ParolaContTrimitere"].ToString(),
                                            bool.Parse(reader["IsAdminTrimitere"].ToString())
                                        );
                                    }
                                    else
                                    {
                                        trimitere = new Utilizator(
                                            reader["UserNameTrimitere"].ToString(),
                                            reader["EmailTrimitere"].ToString(),
                                            reader["ParolaContTrimitere"].ToString(),
                                            reader["IdUtilizatorTrimitere"].ToString()
                                        );
                                    }

                                    Cont livrare;
                                    string tipContLivrare = reader["TipContLivrare"].ToString();
                                    if (tipContLivrare == "Admin")
                                    {
                                        livrare = new Admin(
                                            reader["UserNameLivrare"].ToString(),
                                            reader["EmailLivrare"].ToString(),
                                            reader["ParolaContLivrare"].ToString(),
                                            bool.Parse(reader["IsAdminLivrare"].ToString())
                                        );
                                    }
                                    else
                                    {
                                        livrare = new Utilizator(
                                            reader["UserNameLivrare"].ToString(),
                                            reader["EmailLivrare"].ToString(),
                                            reader["ParolaContLivrare"].ToString(),
                                            reader["IdUtilizatorLivrare"].ToString()
                                        );
                                    }

                                    bool Urgenta = bool.Parse(reader["Urgenta"].ToString());
                                    bool Ridicata = bool.Parse(reader["Ridicata"].ToString());
                                    DateTime dataTrimitere = DateTime.ParseExact(
                                        reader["DataTrimitere"].ToString(),
                                        "dd-MM-yyyy",
                                        System.Globalization.CultureInfo.InvariantCulture
                                    );

                                    DateTime dataLivrare = DateTime.ParseExact(
                                        reader["DataLivrare"].ToString(),
                                        "dd-MM-yyyy",
                                        System.Globalization.CultureInfo.InvariantCulture
                                    );

                                    Comanda comanda = new Comanda(IDComanda, origine, destinatie, trimitere, livrare, Urgenta, Ridicata, dataTrimitere, dataLivrare);

                                    Global.EzBox.AdaugaComanda(comanda);
                                }
                                catch (Exception innerEx)
                                {
                                    Console.WriteLine($"Eroare la procesarea unei comenzi: {innerEx.Message}");
                                }
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la conectarea sau citirea din baza de date: {ex.Message}");
            }
        }





        public static void ScriereinBDListaAsteptare(string connectionString)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

               
                    string deleteQuery = "DELETE FROM listaasteptare";
                    using (MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCmd.ExecuteNonQuery();
                    }

                    foreach (Admin admin in Global.EzBox.GetListaAsteptare())
                    {
                        string userName = admin.GetUserName();
                        string email = admin.GetEmail();

                        try
                        {
                            if (!ListaAsteptareExists(userName, email, connection))
                            {
                                string query = "INSERT INTO listaasteptare (UserName, Email, Parola, IsAdmin) " +
                                               "VALUES (@UserName, @Email, @Parola, @IsAdmin)";
                                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                                {
                                    cmd.Parameters.AddWithValue("@UserName", userName);
                                    cmd.Parameters.AddWithValue("@Email", email);
                                    cmd.Parameters.AddWithValue("@Parola", admin.GetPassword());
                                    cmd.Parameters.AddWithValue("@IsAdmin", admin.GetIsAdmin());

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                        catch (Exception innerEx)
                        {
                            Console.WriteLine($"Eroare la inserarea unui admin în lista de așteptare: {innerEx.Message}");
                        }
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la conectarea sau procesarea bazei de date: {ex.Message}");
            }
        }



        private static bool ListaAsteptareExists(string userName, string email, MySqlConnection connection)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM listaasteptare WHERE UserName = @UserName OR Email = @Email";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", userName);
                    command.Parameters.AddWithValue("@Email", email);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la verificarea existenței utilizatorului în lista de așteptare: {ex.Message}");
                return false;  
            }
        }





        public static void CitireDinBDAdmin(string connectionString)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query2 = "SELECT * FROM Conturi WHERE TipCont = 'Admin'";
                    using (MySqlCommand cmd = new MySqlCommand(query2, connection))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string UserName = reader["UserName"].ToString();
                                string Email = reader["Email"].ToString();
                                string Parola = reader["Parola"].ToString();
                                bool IsAdmin = Convert.ToBoolean(reader["IsAdmin"]);

                                Admin admin = new Admin(UserName, Email, Parola, IsAdmin);
                                Global.EzBox.AdaugaCont(admin);
                            }
                        }
                    }

                    connection.Close();
                }
            }
            catch (MySqlException ex)
            {

                Console.WriteLine("Eroare la baza de date: " + ex.Message);
            }
            catch (Exception ex)
            {

                Console.WriteLine("A apărut o eroare: " + ex.Message);
            }
        }

        public static void CitireDinBDUtilizator(string connectionString)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query3 = "SELECT * FROM Conturi WHERE TipCont = 'Utilizator'";
                    using (MySqlCommand cmd = new MySqlCommand(query3, connection))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string UserName = reader["UserName"].ToString();
                                string Email = reader["Email"].ToString();
                                string Parola = reader["Parola"].ToString();
                                string IdUtilizator = reader["Idutilizator"].ToString();

                                Utilizator utilizator = new Utilizator(UserName, Email, Parola, IdUtilizator);
                                Global.EzBox.AdaugaCont(utilizator);
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (MySqlException ex)
            {

                Console.WriteLine("Eroare la baza de date: " + ex.Message);
            }
            catch (Exception ex)
            {

                Console.WriteLine("A apărut o eroare: " + ex.Message);
            }
        }


        public static void ScriereInDBSuperBox(string connectionString)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                List<SuperBox> dbSuperBoxList = new List<SuperBox>();
                string selectQuery = "SELECT IdSuperBox, Locatie FROM superbox";

                using (MySqlCommand selectCommand = new MySqlCommand(selectQuery, connection))
                using (MySqlDataReader reader = selectCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string idSuperBox = reader["IdSuperBox"].ToString();
                        string locatie = reader["Locatie"].ToString();

                        dbSuperBoxList.Add(new SuperBox(idSuperBox, locatie));
                    }
                }

                foreach (var dbSuperBox in dbSuperBoxList)
                {
                    bool foundInList = Global.EzBox.GetSuperBox().Any(superBox => superBox.GetId() == dbSuperBox.GetId() && superBox.GetLocatie() == dbSuperBox.GetLocatie());

                    if (!foundInList)
                    {
                        string deleteQuery = "DELETE FROM superbox WHERE IdSuperBox = @IdSuperBox AND Locatie = @Locatie";
                        using (MySqlCommand deleteCommand = new MySqlCommand(deleteQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@IdSuperBox", dbSuperBox.GetId());
                            deleteCommand.Parameters.AddWithValue("@Locatie", dbSuperBox.GetLocatie());
                            deleteCommand.ExecuteNonQuery();
                        }
                    }
                }

                foreach (SuperBox superbox in Global.EzBox.GetSuperBox())
                {
                    string idSuperBox = superbox.GetId();
                    string locatie = superbox.GetLocatie();

                    if (!SuperBoxExists(idSuperBox, locatie, connection))
                    {
                        string insertQuery = "INSERT INTO superbox (IdSuperBox, Locatie) VALUES (@IdSuperBox, @Locatie)";
                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@IdSuperBox", idSuperBox);
                            insertCommand.Parameters.AddWithValue("@Locatie", locatie);
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }

                connection.Close();
            }
        }



        private static bool SuperBoxExists(string idSuperBox, string locatie, MySqlConnection connection)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM superbox WHERE IdSuperBox = @IdSuperBox OR Locatie = @Locatie";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdSuperBox", idSuperBox);
                    command.Parameters.AddWithValue("@Locatie", locatie);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
            catch (MySqlException ex)
            {
               
                Console.WriteLine("Eroare la baza de date: " + ex.Message);
                return false; 
            }
            catch (Exception ex)
            {
                Console.WriteLine("A apărut o eroare: " + ex.Message);
                return false; 
            }
        }







        public static void CitireDinBDSuperBoxuri(string connectionString)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query4 = "SELECT * FROM superbox";
                    using (MySqlCommand cmd = new MySqlCommand(query4, connection))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string id = reader["Idsuperbox"].ToString();
                                string locatie = reader["Locatie"].ToString();

                                SuperBox superbox = new SuperBox(id, locatie);
                                Global.EzBox.AdaugaSuperBox(superbox);
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Eroare la baza de date: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("A apărut o eroare: " + ex.Message);
            }
        }


        public static void CitireDinBdListaAsteptare(string connectionString)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query5 = "SELECT * FROM listaasteptare";
                    using (MySqlCommand cmd = new MySqlCommand(query5, connection))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string UserName = reader["UserName"].ToString();
                                string Email = reader["Email"].ToString();
                                string Parola = reader["Parola"].ToString();
                                bool IsAdmin = Convert.ToBoolean(reader["IsAdmin"]);

                                Admin admin = new Admin(UserName, Email, Parola, IsAdmin);
                                Global.EzBox.AdaugaAdminListaAsteptare(admin);
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Eroare la baza de date: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("A apărut o eroare: " + ex.Message);
            }
        }


        public static void SaveComenziToDatabase(string connectionString)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string deleteQuery = "DELETE FROM Comenzi";
                    using (MySqlCommand deleteCommand = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCommand.ExecuteNonQuery();
                    }

                    foreach (Comanda comanda in Global.EzBox.GetComenzi())
                    {
                        string doarDataT = comanda.GetdataTrimitere().ToString("dd-MM-yyyy");
                        string doarDataL = comanda.GetdataLivrare().ToString("dd-MM-yyyy");
                        string idComanda = comanda.GetId();

                        int idSuperBoxOrigine = GetSuperBoxId(comanda.GetSuperBoxOrigine().GetLocatie(), connection);
                        int idSuperBoxDestinatie = GetSuperBoxId(comanda.GetSuperBoxDestinatie().GetLocatie(), connection);
                        int idContTrimitere = GetContId(comanda.GetContTrimitere().GetUserName(), connection);
                        int idContLivrare = GetContId(comanda.GetContPrimire().GetUserName(), connection);

                        if (!ComandaExists(idComanda, idContTrimitere, idContLivrare, idSuperBoxOrigine, idSuperBoxDestinatie, connection))
                        {
                            string query = @"
                    INSERT INTO Comenzi 
                    (IdComanda, IdUtilizatorTrimitere, IdUtilizatorLivrare, SuperBoxOrigine, SuperBoxDestinatie, DataTrimitere, DataLivrare, Urgenta, Ridicata) 
                    VALUES 
                    (@IdComanda, @IdUtilizatorTrimitere, @IdUtilizatorLivrare, @SuperBoxOrigine, @SuperBoxDestinatie, @DataTrimitere, @DataLivrare, @Urgenta, @Ridicata)";

                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@IdComanda", idComanda);
                                command.Parameters.AddWithValue("@IdUtilizatorTrimitere", idContTrimitere);
                                command.Parameters.AddWithValue("@IdUtilizatorLivrare", idContLivrare);
                                command.Parameters.AddWithValue("@SuperBoxOrigine", idSuperBoxOrigine);
                                command.Parameters.AddWithValue("@SuperBoxDestinatie", idSuperBoxDestinatie);
                                command.Parameters.AddWithValue("@DataTrimitere", doarDataT);
                                command.Parameters.AddWithValue("@DataLivrare", doarDataL);
                                command.Parameters.AddWithValue("@Urgenta", comanda.GetIsUrgent());
                                command.Parameters.AddWithValue("@Ridicata", comanda.GetRidicata());

                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    connection.Close();
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Eroare la baza de date: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("A apărut o eroare: " + ex.Message);
            }
        }



        private static bool ComandaExists(string idComanda, int idUtilizatorTrimitere, int idUtilizatorLivrare, int superBoxOrigine, int superBoxDestinatie, MySqlConnection connection)
        {
            string query = "SELECT COUNT(*) FROM Comenzi WHERE IdComanda = @IdComanda OR (IdUtilizatorTrimitere = @IdUtilizatorTrimitere AND IdUtilizatorLivrare = @IdUtilizatorLivrare AND SuperBoxOrigine = @SuperBoxOrigine AND SuperBoxDestinatie = @SuperBoxDestinatie)";

            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@IdComanda", idComanda);
                command.Parameters.AddWithValue("@IdUtilizatorTrimitere", idUtilizatorTrimitere);
                command.Parameters.AddWithValue("@IdUtilizatorLivrare", idUtilizatorLivrare);
                command.Parameters.AddWithValue("@SuperBoxOrigine", superBoxOrigine);
                command.Parameters.AddWithValue("@SuperBoxDestinatie", superBoxDestinatie);

                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0; 
            }
        }


        private static int GetSuperBoxId(string locatie, MySqlConnection connection)
        {
            string query = "SELECT Id FROM SuperBox WHERE Locatie = @Locatie";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Locatie", locatie);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static int GetContId(string userName, MySqlConnection connection)
        {
            string query = "SELECT Id FROM Conturi WHERE UserName = @UserName";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserName", userName);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }



        public static void InboxButton(FlowLayoutPanel flowLayoutPanel2)
        {
            flowLayoutPanel2.Controls.Clear();

            foreach (Admin admin in Global.EzBox.GetListaAsteptare())
            {
                Label label = new Label()
                {
                    Text = admin.ToString(),
                    AutoSize = false,
                    ForeColor = Color.White,
                    Height = 30,
                    Width = 780
                };

                Button btn = new Button()
                {
                    Text = "Accept",
                    Width = 300,
                    Height = 50,
                    ForeColor = Color.White,
                    Name = admin.GetUserName()
                };

                btn.Click += (s, ev) =>
                {
                    flowLayoutPanel2.Controls.Clear();

                    foreach (Admin admin2 in Global.EzBox.GetListaAsteptare())
                    {
                        if (admin2.GetUserName() == btn.Name)
                        {
                            admin2.SetAdminTrue();
                            Global.EzBox.RemoveAdminListaAsteptare(admin2);
                            Global.EzBox.AdaugaCont(admin2);
                            MessageBox.Show("Cont acceptat cu succes!");
                            break;
                        }
                    }


                };


                Button btn2 = new Button()
                {
                    Text = "Reject",
                    Width = 300,
                    Height = 50,
                    ForeColor = Color.White,
                    Name = admin.GetUserName()
                };
                btn2.Click += (s, ev) =>
                {
                    flowLayoutPanel2.Controls.Clear();
                    foreach (Admin admin2 in Global.EzBox.GetListaAsteptare())
                    {
                        if (admin2.GetUserName() == btn.Name)
                        {
                            Global.EzBox.RemoveAdminListaAsteptare(admin2);
                            MessageBox.Show("Cont a fost respins!");
                            break;
                        }
                    }
                };

                flowLayoutPanel2.Controls.Add(label);
                flowLayoutPanel2.Controls.Add(btn);
                flowLayoutPanel2.Controls.Add(btn2);


            }
        }


        public static void Destinate(FlowLayoutPanel flowLayoutPanel2)
        {
            flowLayoutPanel2.Controls.Clear();

            foreach (Comanda comanda in Global.EzBox.GetComenzi())
            {

                if (comanda.GetContPrimire().GetUserName() == Global.cont.GetUserName())
                {
                    Button btn = new Button()
                    {
                        Text = comanda.ToString(),
                        Width = 780,
                        Height = 140,
                        ForeColor = Color.White,
                        Name = comanda.GetId(),
                    };

                    btn.Click += (s3, ev3) =>
                    {

                        flowLayoutPanel2.Controls.Clear();

                        Label lbl = new Label()
                        {
                            Text = comanda.ToString(),
                            AutoSize = false,
                            Width = 780,
                            Height = 140,
                            ForeColor = Color.White
                        };

                        Button btnRidica = new Button()
                        {
                            Text = "Ridica Comanda",
                            Width = 200,
                            Height = 30,
                            ForeColor = Color.White,
                            Name = btn.Name
                        };
                        btnRidica.Click += (s5, ev5) =>
                        {
                            if (comanda.GetdataLivrare() > DateTime.Now)
                            {

                                MessageBox.Show("Comanda nu poate fi ridicata inca nu a fost livrata!");

                            }
                            else
                            {
                                if (comanda.GetRidicata() == true)
                                {
                                    MessageBox.Show("Comanda a fost deja ridicata");
                                }
                                else
                                {
                                    foreach (Comanda comanda7 in Global.EzBox.GetComenzi())
                                    {

                                        if (comanda7.GetId() == btnRidica.Name)
                                        {
                                            comanda7.SetRidicata(true);
                                            flowLayoutPanel2.Controls.Clear();
                                            MessageBox.Show("Comanda a fost ridicata cu succes!");
                                            break;

                                        }
                                    }
                                }
                            }
                        };

                        flowLayoutPanel2.Controls.Add(lbl);
                        flowLayoutPanel2.Controls.Add(btnRidica);

                    };

                    flowLayoutPanel2.Controls.Add(btn);
                }
            }
        }


        public static void Trimise(FlowLayoutPanel flowLayoutPanel2)
        {
            flowLayoutPanel2.Controls.Clear();

            foreach (Comanda comanda in Global.EzBox.GetComenzi())
            {
                try
                {
                    if (comanda.GetContTrimitere().GetUserName() == Global.cont.GetUserName())
                    {

                        Button btn = new Button()
                        {
                            Text = comanda.ToString(),
                            Width = 780,
                            Height = 140,
                            ForeColor = Color.White,
                            Name = comanda.GetId()
                        };

                        btn.Click += (s3, ev3) =>
                        {

                            flowLayoutPanel2.Controls.Clear();

                            Label lbl = new Label()
                            {
                                Text = comanda.ToString(),
                                AutoSize = false,
                                Width = 780,
                                Height = 140,
                                ForeColor = Color.White
                            };

                            Button btnRemove = new Button()
                            {
                                Text = "Anuleaza Comanda",
                                Width = 200,
                                Height = 30,
                                ForeColor = Color.White,
                                Name = btn.Name
                            };
                            btnRemove.Click += (s4, ev4) =>
                            {
                                if (comanda.GetdataLivrare() < DateTime.Today)
                                {
                                    MessageBox.Show("Comanda a fost deja livrata nu se mai poate anula");
                                }
                                else
                                {
                                    foreach (Comanda comanda6 in Global.EzBox.GetComenzi())
                                    {
                                        if (comanda6.GetId() == btnRemove.Name)
                                        {
                                            Global.EzBox.GetComenzi().Remove(comanda6);
                                            flowLayoutPanel2.Controls.Clear();
                                            MessageBox.Show("Comanda a fost anulata cu succes");
                                            string connectionString = "Server=localhost;Database=firma;Uid=root;Pwd=Proiectfacultate;";
                                            BD.SaveComenziToDatabase(connectionString);
                                            break;

                                        }
                                    }
                                }

                            };

                            flowLayoutPanel2.Controls.Add(lbl);
                            flowLayoutPanel2.Controls.Add(btnRemove);

                        };


                        flowLayoutPanel2.Controls.Add(btn);

                    }



                }
                catch (Exception isnull)
                {
                    MessageBox.Show("boss");
                }
            }
        }


        public static void ToateComenzile(FlowLayoutPanel flowLayoutPanel2)
        {
            flowLayoutPanel2.Controls.Clear();

            foreach (Comanda comanda in Global.EzBox.GetComenzi())
            {
                try
                {



                    Button btn = new Button()
                    {
                        Text = comanda.ToString(),
                        Width = 780,
                        Height = 140,
                        ForeColor = Color.White,
                        Name = comanda.GetId()
                    };

                    btn.Click += (s3, ev3) =>
                    {

                        flowLayoutPanel2.Controls.Clear();

                        Label lbl = new Label()
                        {
                            Text = comanda.ToString(),
                            AutoSize = false,
                            Width = 780,
                            Height = 140,
                            ForeColor = Color.White
                        };

                        Button btnRemove = new Button()
                        {
                            Text = "Livreaza comanda",
                            Width = 200,
                            Height = 30,
                            ForeColor = Color.White,
                            Name = btn.Name
                        };
                        btnRemove.Click += (s4, ev4) =>
                        {
                            if (comanda.GetdataLivrare() < DateTime.Today)
                            {
                                MessageBox.Show("Comanda a fost deja livrata");
                            }
                            else
                            {
                                foreach (Comanda comanda6 in Global.EzBox.GetComenzi())
                                {
                                    if (comanda6.GetId() == btnRemove.Name)
                                    {
                                        comanda6.SetdataLivrare(DateTime.Today);
                                        flowLayoutPanel2.Controls.Clear();
                                        MessageBox.Show("Comanda a fost livrata");
                                        string connectionString = "Server=localhost;Database=firma;Uid=root;Pwd=Proiectfacultate;";
                                        BD.SaveComenziToDatabase(connectionString);
                                        break;

                                    }
                                }
                            }

                        };

                        flowLayoutPanel2.Controls.Add(lbl);
                        flowLayoutPanel2.Controls.Add(btnRemove);

                    };


                    flowLayoutPanel2.Controls.Add(btn);


                }



                catch (Exception isnull)
                {

                }
            }
        }

    }

}
