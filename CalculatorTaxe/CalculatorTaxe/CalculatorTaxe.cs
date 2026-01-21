using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

// Comentez aici decat ce nu a fost comentat in Login.cs
public class CalculatorTaxe : Form
{
    private string conn = "datasource="+Login.datasrc+";port="+Login.port+";username="+Login.username+";password=" + Login.password; // Cream o valoare "string" care tine valorile conexiunei(nume, parola, adresa, etc)

    // Group box
    private System.Windows.Forms.GroupBox DATA_GROUP_BOX;
    private System.Windows.Forms.GroupBox BUTOATE_GROUP_BOX;

    // Label-uri
    private System.Windows.Forms.Label TAXA;
    private System.Windows.Forms.Label ID;
    private System.Windows.Forms.Label PRODUS;
    private System.Windows.Forms.Label VALOARE_PRODUS;
    private System.Windows.Forms.Label ADAUGARE_DATE;
    private System.Windows.Forms.Label INFORMATII;

    // Textbox-uri
    private System.Windows.Forms.TextBox TAXA_TEXTBOX;
    private System.Windows.Forms.TextBox ID_TEXTBOX;
    private System.Windows.Forms.TextBox PRODUS_TEXTBOX;
    private System.Windows.Forms.TextBox VALOARE_PRODUS_TEXTBOX;

    // Butoane
    private System.Windows.Forms.Button SALVARE_DATE_BUTTON;
    private System.Windows.Forms.Button UPDATE_DATE_BUTTON;
    private System.Windows.Forms.Button DELETE_DATA_BUTTON;
    private System.Windows.Forms.Button ARATA_DATE_BUTTON;
    private System.Windows.Forms.Button CALCULEAZA_TAXA;
    private System.Windows.Forms.Button CALCULEAZA_TAXE_TOATE;

    // Grid-ul care apare in dreapta cu toate valorile din sql
    private System.Windows.Forms.DataGridView VIZUALIZARE_DATE_PRODUSE;

    public CalculatorTaxe()
    {
        init();
        createComponent();
        createForm();
    }

    // O functie doar pentru a arata datele din sql in acel grid. Trebe o functie separata fata de celelalte ca nu poate fi folosita functia de la buton pentru a aparea imediat cand porneste aplicatia din cauza ca aia este un EventHandler(intelegi tu :)
    private void arata_date_actiune(object sender, EventArgs e)
    {
        arata_date();
    }

    // Calculeaza pretul dupa taxe
    private float calculeazaPretTotal(float inputTaxRate, float itemPrice)
    {
        float taxRate = inputTaxRate / 100;
        return itemPrice + (itemPrice * taxRate);
    }

    // Asta este functia care ia datele din sql si le face sa apara in grid
    private void arata_date()
    {
        // Trebuie neaparat sa fie un try and catch ca altfel ar da eroari naspa daca ceva nu merge bine
        try
        {
            string Query = "select * from taxe.taxeInfo;"; // Creaza o comanda pentru sql.
            MySqlConnection MyConn2 = new MySqlConnection(conn); // Creaza o conecsiune la sql folosind variabila definita mai sus
            MySqlCommand MyCommand2 = new MySqlCommand(Query, MyConn2); // Creaza o noua comanda folosind comanda deefinina de noi plus conexiunea creata
            MySqlDataAdapter MyAdapter = new MySqlDataAdapter(); // Creaza un adapter(naiba stie ce ii) pentru a umple grid-ul cu informatiile din sql
            MyAdapter.SelectCommand = MyCommand2; // Pune comanda creata in adapter
            DataTable dTable = new DataTable(); // Creaza un table pentru a stoca informatiile primite de la sql
            MyAdapter.Fill(dTable); // Aici stocheaza informatii in acel tabel
            VIZUALIZARE_DATE_PRODUSE.DataSource = dTable; // Doar seteaza grid-ul cread la tabelul cu informatiile
        }
        catch (Exception ex) // In caz ca ii vrei exceptie/eroare
        {
                MessageBox.Show(ex.Message); // Creaza un MessageBox care o sa apare cu eroare
        }
    }

    // Salvam informatiile noi in sql
    private void salveaza_date_actiune(object sender, EventArgs e)
    {
        if (this.ID_TEXTBOX.Text != "" && this.PRODUS_TEXTBOX.Text != "" && this.VALOARE_PRODUS_TEXTBOX.Text != "") // Da check sa vada daca casutele au valori in ele
        {
            try
            {
                string Query = "insert into taxe.taxeInfo(id,Produs,Valoare_Produs) values('" +this.ID_TEXTBOX.Text+ "','" +this.PRODUS_TEXTBOX.Text+ "','" +this.VALOARE_PRODUS_TEXTBOX.Text+ "');"; // Cream o comanda pentru a insera valori in tabelul sql(poate pare complicat dar nu e deloc, arata asa ca trebuie formatat special pentru ca sql sa inteleaga comanda. O sa explic in fisierul .sql ce fac comenzile, o sa le pun si pe asta din c# dar o sa le explic acolo)
                MySqlConnection MyConn2 = new MySqlConnection(conn);
                MySqlCommand MyCommand2 = new MySqlCommand(Query, MyConn2);
                MySqlDataReader MyReader2;
                MyConn2.Open(); // Deschidem conecsiunea. Trebuie neaparat deschisa ca altfel nu o sa putem scrie in tabelul din sql
                MyReader2 = MyCommand2.ExecuteReader(); // Executa comanda creata
                MessageBox.Show("Date salvate"); // Un MessageBox pentru a ne spune ca valorile au fost salvate
                while (MyReader2.Read()) // Trebuie creata acel while loop chiar daca nu este nimic pus in el
                {
                    
                }
                MyConn2.Close(); // Inchidem conecsiunea pentru a putea face si altele. Daca nu o inchidem si incercam sa cream alta o sa ne dea o eroare
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        } else if (this.ID_TEXTBOX.Text == "" && this.PRODUS_TEXTBOX.Text == "" && this.VALOARE_PRODUS_TEXTBOX.Text == "") // Dam check pentru fiecare casuta in parte daca este ceva scris in ea iar daca nu cream un MessageBox cu eroarea corespunzatoare(de notat ca se poate face un message class in care sa modifici MessageBoxul sa arate mai bine sau chiar creat cu totul unul nou)
        {
            MessageBox.Show("Trebuie  valori pentru a putea fi actualizate!");
        } else if(this.ID_TEXTBOX.Text == "")
        {
            MessageBox.Show("ID Invalid!");
        } else if(this.PRODUS_TEXTBOX.Text == "")
        {
            MessageBox.Show("Produs Invalid!");
        } else if(this.VALOARE_PRODUS_TEXTBOX.Text == "")
        {
            MessageBox.Show("Valore Produs Invalida!");
        }
    }

    // Actualizam informatiile din tabelul din sql. Aici singura diferenta fata de celelalte este ca accesam dee doua ori servarul sql folosind 2 comenzi diferite
    private void update_date_actiune(object sender, EventArgs e)
    {
        if (this.ID_TEXTBOX.Text != "" && this.PRODUS_TEXTBOX.Text != "" && this.VALOARE_PRODUS_TEXTBOX.Text != "") // Aici accesam in caz ca vrem sa actualizam si produsul si valorea produsului
        {
            try
            {
                string Query = "UPDATE taxe.taxeInfo SET Produs='" + this.PRODUS_TEXTBOX.Text + "'," + " Valoare_Produs='" + this.VALOARE_PRODUS_TEXTBOX.Text + "' WHERE id=" + this.ID_TEXTBOX.Text + ";"; // O comanda pentru sql ca sa ii spunem sa actualizeze tabelul cu valorile date de noi
                MySqlConnection MyConn2 = new MySqlConnection(conn);
                MySqlCommand MyCommand2 = new MySqlCommand(Query, MyConn2);
                MySqlDataReader MyReader2;
                MyConn2.Open();
                MyReader2 = MyCommand2.ExecuteReader();
                MessageBox.Show("Date actualizate");
                while (MyReader2.Read())
                {

                }
                MyConn2.Close();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        } else if (this.ID_TEXTBOX.Text != "" && this.VALOARE_PRODUS_TEXTBOX.Text != "") // Aici accesam in caz ca vrem sa schimbam decat valoarea produsului
        {
            try
            {
                string Query = "UPDATE taxe.taxeInfo SET Valoare_Produs='" + this.VALOARE_PRODUS_TEXTBOX.Text + "' WHERE id=" + this.ID_TEXTBOX.Text + ";";
                MySqlConnection MyConn2 = new MySqlConnection(conn);
                MySqlCommand MyCommand2 = new MySqlCommand(Query, MyConn2);
                MySqlDataReader MyReader2;
                MyConn2.Open();
                MyReader2 = MyCommand2.ExecuteReader();
                MessageBox.Show("Date actualizate");
                while (MyReader2.Read())
                {

                }
                MyConn2.Close();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }else if (this.ID_TEXTBOX.Text == "" && this.PRODUS_TEXTBOX.Text == "" && this.VALOARE_PRODUS_TEXTBOX.Text == "")
        {
            MessageBox.Show("Trebuie valori pentru a putea fi actualizate!");
        } else if(this.ID_TEXTBOX.Text == "")
        {
            MessageBox.Show("ID Invalid!");
        } else if(this.PRODUS_TEXTBOX.Text == "")
        {
            MessageBox.Show("Produs Invalid!");
        } else if(this.VALOARE_PRODUS_TEXTBOX.Text == "")
        {
            MessageBox.Show("Valore Produs Invalida!");
        }
    }

    // Aici stergem o valoare din tabelul sql folosind id-ul corespunzator
    private void sterge_date_actiune(object sender, EventArgs e)
    {
        try
        {
            string Query = "delete from taxe.taxeInfo where id='" + this.ID_TEXTBOX.Text + "';"; // Decat o comanda care sterge randul din tabel-ul sql
            MySqlConnection MyConn2 = new MySqlConnection(conn);
            MySqlCommand MyCommand2 = new MySqlCommand(Query, MyConn2);
            MySqlDataReader MyReader2;
            MyConn2.Open();
            MyReader2 = MyCommand2.ExecuteReader();
            MessageBox.Show("Date Sterse");
            while (MyReader2.Read())
            {

            }
            MyConn2.Close();
        }
        catch (Exception ex)
        {

            MessageBox.Show(ex.Message);
        }
    }

    // Aici calculam taxa
    private void calculeaza_taxa(object sender, EventArgs e)
    {

        if (this.ID_TEXTBOX.Text != "" && this.TAXA_TEXTBOX.Text != "")
        {
            // Cream o variabila float care stocheaza rezultatul calculului nostru. Aici ii unpic mai complicat ca trebuie sa convertim fiecare valoare la valoarea corespunzatoare(trebuie sa convertim "this.TAXA_TEXTBOX.Text" care este un string in int. Facem asta folosind "int.Parse()". Functia asta converste orice string in int decat daca valoarea string contine numere)
            // In continuare functia "calculeazaPretTotal()" mai ia si un float. Trebuyie sa luam acel float din tabelul creat de noi, nu cel din sql cel in winforms folosind "this.VIZUALIZARE_DATE_PRODUSE.Rows[rand]". De asemenea aceasta variabila ia un int iar noi trebuie sa ii dam id-ul pe care il vrem care acela este un string. Pentru al converti in int folosim dinou "int.Parse()"
            // Pentru a lua exact celula pe care o vrem folosim ".Cells[celula].Value" in continuarea la "this.VIZUALIZARE_DATE_PRODUSE.Rows[rand]"
            // Dupa toate astea valoarea inca este string pentru ca "this.VIZUALIZARE_DATE_PRODUSE.Rows[int.Parse(this.ID_TEXTBOX.Text)].Cells[2].Value" returneaza un string nu un numar. Pentru al putea folosi trebuie convertit in float. Pentru a il putea converti in float trebuie intai in double folosind "Cnvert.ToDouble()". Nu il putem lasa double pentru ca functia noastra ia float si de asemenea precizia dupa punct ii mai mare decat ne trebuie noua. Pentru a il converti in float ii dam cast(adica il introducem intr-un fel in float) folosind "(float)" inainte de toata oeratie creata
            float taxaTotala = calculeazaPretTotal(float.Parse(this.TAXA_TEXTBOX.Text), (float) Convert.ToDouble(this.VIZUALIZARE_DATE_PRODUSE.Rows[int.Parse(this.ID_TEXTBOX.Text)].Cells[2].Value));
            try
            {
                string Query = "UPDATE taxe.taxeInfo SET Valoare_Totala='" + taxaTotala + "' WHERE id=" + this.ID_TEXTBOX.Text + ";"; // Creaza o comanda pentru a pune in tabelul din sql valorea taxei aflate
                MySqlConnection MyConn2 = new MySqlConnection(conn);
                MySqlCommand MyCommand2 = new MySqlCommand(Query, MyConn2);
                MySqlDataReader MyReader2;
                MyConn2.Open();
                MyReader2 = MyCommand2.ExecuteReader();
                MessageBox.Show("Taxa actualizata!");
                while (MyReader2.Read())
                {

                }
                MyConn2.Close();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        } else if(this.ID_TEXTBOX.Text == "" && this.TAXA_TEXTBOX.Text == "")
        {
            MessageBox.Show("Trebuie valori pentru a putea fi calculata taxa!");
        } else if(this.ID_TEXTBOX.Text == "")
        {
            MessageBox.Show("ID Invalid!");
        } else if(this.TAXA_TEXTBOX.Text == "")
        {
            MessageBox.Show("Taxa Invalida!");
        }
    }

    // Aici calculam taxa la toate din tabel
    private void calculeaza_taxe(object sender, EventArgs e)
    {
        if (this.TAXA_TEXTBOX.Text != "")
        {
            for(int i = 0; i < this.VIZUALIZARE_DATE_PRODUSE.RowCount; i++) // Folosim un for loop pentru a trece prin fiecare rand din tabel. "this.VIZUALIZARE_DATE_PRODUSE.RowCount" ne da numarul maxim de randuri care exista in tabelul creat
            {
                // Calculeaza pretul dupa taxe. De data asta in loc de id ii dam "i" din loop pentru a trece prin fiecare din ele
                float taxaTotala = calculeazaPretTotal(float.Parse(this.TAXA_TEXTBOX.Text), (float) Convert.ToDouble(this.VIZUALIZARE_DATE_PRODUSE.Rows[i].Cells[2].Value));
            
                try
                {
                    string Query = "UPDATE taxe.taxeInfo SET Valoare_Totala='" + taxaTotala + "' WHERE id=" + i + ";"; // O comanda pentru sql care seteaza pretul dupa taxe la id-ul corespunzator
                    MySqlConnection MyConn2 = new MySqlConnection(conn);
                    MySqlCommand MyCommand2 = new MySqlCommand(Query, MyConn2);
                    MySqlDataReader MyReader2;
                    MyConn2.Open();
                    MyReader2 = MyCommand2.ExecuteReader();
                    while (MyReader2.Read())
                    {

                    }
                    MyConn2.Close();
                }
                catch (Exception ex)
                {

                    MessageBox.Show(ex.Message);
                }
            }
            MessageBox.Show("Taxe actualizate!");
        } else
        {
            MessageBox.Show("Taxa Invalida!");
        }
    }

    // Functia in care initializam toate obiectele din aplicatie
    private void init()
    {
        // Groupbox-uri
        this.DATA_GROUP_BOX = new System.Windows.Forms.GroupBox();
        this.BUTOATE_GROUP_BOX = new System.Windows.Forms.GroupBox();

        // Label-uri
        this.ID = new System.Windows.Forms.Label();
        this.PRODUS = new System.Windows.Forms.Label();
        this.VALOARE_PRODUS = new System.Windows.Forms.Label();
        this.ADAUGARE_DATE = new System.Windows.Forms.Label();
        this.INFORMATII = new System.Windows.Forms.Label();
        this.TAXA = new System.Windows.Forms.Label();

        // Textbox-uri
        this.TAXA_TEXTBOX = new System.Windows.Forms.TextBox();
        this.ID_TEXTBOX = new System.Windows.Forms.TextBox();
        this.PRODUS_TEXTBOX = new System.Windows.Forms.TextBox();
        this.VALOARE_PRODUS_TEXTBOX = new System.Windows.Forms.TextBox();

        // Buttons
        this.SALVARE_DATE_BUTTON = new System.Windows.Forms.Button();
        this.UPDATE_DATE_BUTTON = new System.Windows.Forms.Button();
        this.DELETE_DATA_BUTTON = new System.Windows.Forms.Button();
        this.ARATA_DATE_BUTTON = new System.Windows.Forms.Button();
        this.CALCULEAZA_TAXA = new System.Windows.Forms.Button();
        this.CALCULEAZA_TAXE_TOATE = new System.Windows.Forms.Button();

        // Grid
        this.VIZUALIZARE_DATE_PRODUSE = new System.Windows.Forms.DataGridView();
        ((System.ComponentModel.ISupportInitialize)(this.VIZUALIZARE_DATE_PRODUSE)).BeginInit();

        // Groupbox
        this.DATA_GROUP_BOX.SuspendLayout();
        this.BUTOATE_GROUP_BOX.SuspendLayout();
        this.SuspendLayout();

        arata_date();
    }

    private void createComponent()
    {
        // Taxa
        this.TAXA.AutoSize = true;
        this.TAXA.Location = new System.Drawing.Point(14, 23);
        this.TAXA.Name = "id";
        this.TAXA.Size = new System.Drawing.Size(18, 13);
        this.TAXA.TabIndex = 0;
        this.TAXA.Text = "Taxa";

        // ID
        this.ID.AutoSize = true;
        this.ID.Location = new System.Drawing.Point(14, 59);
        this.ID.Name = "taxa";
        this.ID.Size = new System.Drawing.Size(35, 13);
        this.ID.TabIndex = 1;
        this.ID.Text = "ID";

        // Produs
        this.PRODUS.AutoSize = true;
        this.PRODUS.Location = new System.Drawing.Point(14, 92);
        this.PRODUS.Name = "produs";
        this.PRODUS.Size = new System.Drawing.Size(65, 13);
        this.PRODUS.TabIndex = 2;
        this.PRODUS.Text = "Produs";

        // Valoare produs
        this.VALOARE_PRODUS.AutoSize = true;
        this.VALOARE_PRODUS.Location = new System.Drawing.Point(14, 130);
        this.VALOARE_PRODUS.Name = "valoare_produs";
        this.VALOARE_PRODUS.Size = new System.Drawing.Size(26, 13);
        this.VALOARE_PRODUS.TabIndex = 3;
        this.VALOARE_PRODUS.Text = "Valoare Produs";

        // Taxa Textbox
        this.TAXA_TEXTBOX.Location = new System.Drawing.Point(161, 20);
        this.TAXA_TEXTBOX.Name = "taxa_textbox";
        this.TAXA_TEXTBOX.Size = new System.Drawing.Size(100, 20);
        this.TAXA_TEXTBOX.TabIndex = 4;

        // ID Textbox
        this.ID_TEXTBOX.Location = new System.Drawing.Point(161, 56);
        this.ID_TEXTBOX.Name = "id_textbox";
        this.ID_TEXTBOX.Size = new System.Drawing.Size(100, 20);
        this.ID_TEXTBOX.TabIndex = 5;

        // Produs Textbox
        this.PRODUS_TEXTBOX.Location = new System.Drawing.Point(161, 92);
        this.PRODUS_TEXTBOX.Name = "produs_textbox";
        this.PRODUS_TEXTBOX.Size = new System.Drawing.Size(100, 20);
        this.PRODUS_TEXTBOX.TabIndex = 6;

        // Valore Produs Textbox
        this.VALOARE_PRODUS_TEXTBOX.Location = new System.Drawing.Point(161, 127);
        this.VALOARE_PRODUS_TEXTBOX.Name = "AgeTextBox";
        this.VALOARE_PRODUS_TEXTBOX.Size = new System.Drawing.Size(100, 20);
        this.VALOARE_PRODUS_TEXTBOX.TabIndex = 7;

        // Salvare date button
        this.SALVARE_DATE_BUTTON.Location = new System.Drawing.Point(6, 15);
        this.SALVARE_DATE_BUTTON.Name = "salvare_date";
        this.SALVARE_DATE_BUTTON.Size = new System.Drawing.Size(91, 23);
        this.SALVARE_DATE_BUTTON.TabIndex = 8;
        this.SALVARE_DATE_BUTTON.Text = "Salveaza date";
        this.SALVARE_DATE_BUTTON.UseVisualStyleBackColor = true;
        this.SALVARE_DATE_BUTTON.Click += new System.EventHandler(this.salveaza_date_actiune);

        // Update data button
        this.UPDATE_DATE_BUTTON.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
        | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right))); // Cream o ancora pentru a bloca butonul dupa valorile date(poate fi ancorat sus, jos, stanga si dreapta)
        this.UPDATE_DATE_BUTTON.Location = new System.Drawing.Point(103, 15);
        this.UPDATE_DATE_BUTTON.Name = "update_data";
        this.UPDATE_DATE_BUTTON.Size = new System.Drawing.Size(91, 23);
        this.UPDATE_DATE_BUTTON.TabIndex = 9;
        this.UPDATE_DATE_BUTTON.Text = "Update date";
        this.UPDATE_DATE_BUTTON.UseVisualStyleBackColor = true;
        this.UPDATE_DATE_BUTTON.Click += new System.EventHandler(this.update_date_actiune);

        // Sterge date button
        this.DELETE_DATA_BUTTON.Location = new System.Drawing.Point(6, 51);
        this.DELETE_DATA_BUTTON.Name = "sterge_date";
        this.DELETE_DATA_BUTTON.Size = new System.Drawing.Size(91, 23);
        this.DELETE_DATA_BUTTON.TabIndex = 10;
        this.DELETE_DATA_BUTTON.Text = "Sterge Date";
        this.DELETE_DATA_BUTTON.UseVisualStyleBackColor = true;
        this.DELETE_DATA_BUTTON.Click += new System.EventHandler(this.sterge_date_actiune);

        // Arata date button
        this.ARATA_DATE_BUTTON.Location = new System.Drawing.Point(103, 51);
        this.ARATA_DATE_BUTTON.Name = "arata_date";
        this.ARATA_DATE_BUTTON.Size = new System.Drawing.Size(91, 23);
        this.ARATA_DATE_BUTTON.TabIndex = 11;
        this.ARATA_DATE_BUTTON.Text = "Arata date";
        this.ARATA_DATE_BUTTON.UseVisualStyleBackColor = true;
        this.ARATA_DATE_BUTTON.Click += new System.EventHandler(this.arata_date_actiune);

        // Calculare taxa
        this.CALCULEAZA_TAXA.Location = new System.Drawing.Point(6, 87);
        this.CALCULEAZA_TAXA.Name = "calculeaza_taxa";
        this.CALCULEAZA_TAXA.Size = new System.Drawing.Size(91, 23);
        this.CALCULEAZA_TAXA.TabIndex = 12;
        this.CALCULEAZA_TAXA.Text = "Calculeaza Taxa";
        this.CALCULEAZA_TAXA.UseVisualStyleBackColor = true;
        this.CALCULEAZA_TAXA.Click += new System.EventHandler(this.calculeaza_taxa);

        // Calculare taxe pentru toate
        this.CALCULEAZA_TAXE_TOATE.Location = new System.Drawing.Point(103, 87);
        this.CALCULEAZA_TAXE_TOATE.Name = "calculeare_taxe_toate";
        this.CALCULEAZA_TAXE_TOATE.Size = new System.Drawing.Size(91, 23);
        this.CALCULEAZA_TAXE_TOATE.TabIndex = 13;
        this.CALCULEAZA_TAXE_TOATE.Text = "Calculeaza Taxe";
        this.CALCULEAZA_TAXE_TOATE.UseVisualStyleBackColor = true;
        this.CALCULEAZA_TAXE_TOATE.Click += new System.EventHandler(this.calculeaza_taxe);

        // Adaugare date label
        this.ADAUGARE_DATE.AutoSize = true;
        this.ADAUGARE_DATE.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
        this.ADAUGARE_DATE.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, ((System.Drawing.FontStyle)(((System.Drawing.FontStyle.Bold 
        | System.Drawing.FontStyle.Italic) 
        | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.ADAUGARE_DATE.ForeColor = System.Drawing.Color.Violet;
        this.ADAUGARE_DATE.Location = new System.Drawing.Point(34, 33);
        this.ADAUGARE_DATE.Name = "adaugare_date";
        this.ADAUGARE_DATE.Size = new System.Drawing.Size(192, 31);
        this.ADAUGARE_DATE.TabIndex = 14;
        this.ADAUGARE_DATE.Text = "Adauga date";

        // Informatii label
        this.INFORMATII.AutoSize = true;
        this.INFORMATII.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
        this.INFORMATII.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
        this.INFORMATII.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, ((System.Drawing.FontStyle)(((System.Drawing.FontStyle.Bold 
        | System.Drawing.FontStyle.Italic) 
        | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.INFORMATII.ForeColor = System.Drawing.Color.Violet;
        this.INFORMATII.Location = new System.Drawing.Point(577, 33);
        this.INFORMATII.Name = "informatii";
        this.INFORMATII.Size = new System.Drawing.Size(241, 31);
        this.INFORMATII.TabIndex = 15;
        this.INFORMATII.Text = "Informatii";

        this.VIZUALIZARE_DATE_PRODUSE.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top 
        | System.Windows.Forms.AnchorStyles.Bottom) 
        | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
        this.VIZUALIZARE_DATE_PRODUSE.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.VIZUALIZARE_DATE_PRODUSE.Location = new System.Drawing.Point(424, 75);
        this.VIZUALIZARE_DATE_PRODUSE.Name = "vizualizare_date_produse";
        this.VIZUALIZARE_DATE_PRODUSE.Size = new System.Drawing.Size(545, 338);
        this.VIZUALIZARE_DATE_PRODUSE.TabIndex = 16;

        // Group box
        this.DATA_GROUP_BOX.Controls.Add(this.TAXA);
        this.DATA_GROUP_BOX.Controls.Add(this.ID);
        this.DATA_GROUP_BOX.Controls.Add(this.PRODUS);
        this.DATA_GROUP_BOX.Controls.Add(this.VALOARE_PRODUS);
        this.DATA_GROUP_BOX.Controls.Add(this.TAXA_TEXTBOX);
        this.DATA_GROUP_BOX.Controls.Add(this.ID_TEXTBOX);
        this.DATA_GROUP_BOX.Controls.Add(this.PRODUS_TEXTBOX);
        this.DATA_GROUP_BOX.Controls.Add(this.VALOARE_PRODUS_TEXTBOX);
        this.DATA_GROUP_BOX.Location = new System.Drawing.Point(34, 75);
        this.DATA_GROUP_BOX.Name = "data_groupbox";
        this.DATA_GROUP_BOX.Size = new System.Drawing.Size(279, 160);
        this.DATA_GROUP_BOX.TabIndex = 17;
        this.DATA_GROUP_BOX.TabStop = false;
        this.DATA_GROUP_BOX.Text = "Date Produse";

        // Group box
        this.BUTOATE_GROUP_BOX.Controls.Add(this.SALVARE_DATE_BUTTON);
        this.BUTOATE_GROUP_BOX.Controls.Add(this.UPDATE_DATE_BUTTON);
        this.BUTOATE_GROUP_BOX.Controls.Add(this.DELETE_DATA_BUTTON);
        this.BUTOATE_GROUP_BOX.Controls.Add(this.ARATA_DATE_BUTTON);
        this.BUTOATE_GROUP_BOX.Controls.Add(this.CALCULEAZA_TAXA);
        this.BUTOATE_GROUP_BOX.Controls.Add(this.CALCULEAZA_TAXE_TOATE);
        this.BUTOATE_GROUP_BOX.Location = new System.Drawing.Point(34, 270);
        this.BUTOATE_GROUP_BOX.Name = "butoane_groupbox";
        this.BUTOATE_GROUP_BOX.Size = new System.Drawing.Size(200, 120);
        this.BUTOATE_GROUP_BOX.TabIndex = 18;
        this.BUTOATE_GROUP_BOX.TabStop = false;
        this.BUTOATE_GROUP_BOX.Text = "Actiune";
    }

    private void createForm()
    {
        this.FormBorderStyle = FormBorderStyle.FixedSingle; // Face sa nu poata sa fie modificata marimea aplicatiei
        this.MaximizeBox = false; // Dezactiveaza butonul de marire
        this.MinimizeBox = true; // Tine activat butonul de micsorare in bara
        this.StartPosition = FormStartPosition.CenterScreen;
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
        this.ClientSize = new System.Drawing.Size(1000, 449);
        this.Controls.Add(this.BUTOATE_GROUP_BOX);
        this.Controls.Add(this.DATA_GROUP_BOX);
        this.Controls.Add(this.ADAUGARE_DATE);
        this.Controls.Add(this.INFORMATII);
        this.Controls.Add(this.VIZUALIZARE_DATE_PRODUSE);
        this.Name = "CalculatorTaxe";
        this.Text = "Calculator Taxe";
        ((System.ComponentModel.ISupportInitialize)(this.VIZUALIZARE_DATE_PRODUSE)).EndInit();
        this.DATA_GROUP_BOX.ResumeLayout(false);
        this.DATA_GROUP_BOX.PerformLayout();
        this.BUTOATE_GROUP_BOX.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    // Un EventHandler pentru a inchide winform-ul cand este apasat pe x. Trebe pus asta ca altfel ramane deschis in background. Folosind asta dureaza destul de mult sa se inchida dar este destul de buna momentan
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        this.Close();
    } 
}