// In total sunt 775 linii de cod(si da includ si comenturile ca uitai sa vad cate sunt inainte sa incep sa scriu :)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

public class Login : Form // Cream o clasa Login care implementeaza clasa Form. O data implementata clasa avem la disponibilitate toate functiie si caracteristicile clasei Form
{
    // Port: 3306
    // Datasrc: localhost
    // Nume si Parola este cea setata la sql
    // Definim toate componentele aplicatiei facandule  private
    // Groupbox
    private System.Windows.Forms.GroupBox groupBox;

    // Label
    private System.Windows.Forms.Label USERNAME;
    private System.Windows.Forms.Label PASSWORD;
    private System.Windows.Forms.Label DATASOURCE;
    private System.Windows.Forms.Label PORT;

    private System.Windows.Forms.Label NUME_PROIECT;
    private System.Windows.Forms.Label LOGIN_LABEL;

    // Textbox
    private System.Windows.Forms.TextBox USER_TEXT_BOX;
    private System.Windows.Forms.TextBox PASS_TEXT_BOX;
    private System.Windows.Forms.TextBox DATASRC_TEXT_BOX;
    private System.Windows.Forms.TextBox PORT_TEXT_BOX;

    // Button
    private System.Windows.Forms.Button LOGIN_BUTTON;

    // Astea sunt pentru a ne putea loga in MySql. Trebuie ne aparat sa fie publice si statice pentru a putea fi accesate din CalculatorTaxe.cs
    public static String username = "";
    public static String password = "";
    public static String datasrc  = "";
    public static String port     = "";

    public Login()
    {
        init();
        createLoginComponent();
        createForm();
        PASS_TEXT_BOX.PasswordChar = '•';
    }

    // Doar o functie pentru a putea fi folosita o data ce butonul de "Loggin" este apasat
    private void login_button_actiune(object sender, EventArgs e)
    {
        // Dam check daca toate casutele au scris in ele, daca nu dam errori respective casutelor
        if (this.USER_TEXT_BOX.Text != "" && this.PASS_TEXT_BOX.Text != "" && this.DATASOURCE.Text != "" && this.PORT.Text != "")
        {
            username = this.USER_TEXT_BOX.Text;
            password = this.PASS_TEXT_BOX.Text;
            datasrc  = this.DATASRC_TEXT_BOX.Text;
            port     = this.PORT_TEXT_BOX.Text;
            this.Hide();
            CalculatorTaxe calc = new CalculatorTaxe();
            calc.ShowDialog();
        } else if(this.USER_TEXT_BOX.Text == "" && this.PASS_TEXT_BOX.Text == "" && this.DATASOURCE.Text == "" && this.PORT.Text == "")
        {
            MessageBox.Show("Trebuie valori pentru a va putea loga!");
        } else if(this.USER_TEXT_BOX.Text == "")
        {
            MessageBox.Show("Username invalid!");
        } else if(this.PASS_TEXT_BOX.Text == "")
        {
            MessageBox.Show("Parola invalida!");
        } else if(this.DATASOURCE.Text == "")
        {
            MessageBox.Show("Datasource invalid!");
        } else if(this.PORT.Text == "")
        {
            MessageBox.Show("Port invalid!");
        }
    }

    // Functie in care initialiazam fiecare obiect din aplicatie
    private void init()
    {
         // Groupbox-uri
        this.groupBox = new System.Windows.Forms.GroupBox();
        this.groupBox.SuspendLayout();

        // Label-uri
        this.USERNAME = new System.Windows.Forms.Label();
        this.PASSWORD = new System.Windows.Forms.Label();
        this.DATASOURCE = new System.Windows.Forms.Label();
        this.PORT = new System.Windows.Forms.Label();
        this.NUME_PROIECT = new System.Windows.Forms.Label();
        this.LOGIN_LABEL = new System.Windows.Forms.Label();

        // Textbox-uri
        this.USER_TEXT_BOX = new System.Windows.Forms.TextBox();
        this.PASS_TEXT_BOX = new System.Windows.Forms.TextBox();
        this.DATASRC_TEXT_BOX = new System.Windows.Forms.TextBox();
        this.PORT_TEXT_BOX = new System.Windows.Forms.TextBox();

        // Button
        this.LOGIN_BUTTON = new System.Windows.Forms.Button();
    }

    // Functia care creaza obiectele
    private void createLoginComponent()
    {
        // Password
        this.USERNAME.AutoSize = true; // Setare pentru a face ca label-ul sa isi schimbe marimea in functie de dimensiunea fontului
        this.USERNAME.Location = new System.Drawing.Point(6, 28); // In C# winform trebuie creat un punct(Point) pentru a defini locatia obiectului. Point este doar u vector care stocheaza locatia 'x' si 'y'
        this.USERNAME.Name = "username"; // Setam numele invizibil pentru label
        this.USERNAME.Size = new System.Drawing.Size(55, 13); // Setam dimensiunea label-ului folosind functia Size
        this.USERNAME.Text = "Username"; // Setam textul pentru label
        this.USERNAME.TabIndex = 0; // Asta seteaza, dupa cum spune si numele, indexul la tab. Adica, atunci cand dai tab in ce ordine sa o ia(de la 0 la 1 la 2 la 3 la etc)
        
        // Username
        this.PASSWORD.AutoSize = true;
        this.PASSWORD.Location = new System.Drawing.Point(5, 65);
        this.PASSWORD.Name = "password";
        this.PASSWORD.Size = new System.Drawing.Size(53, 13);
        this.PASSWORD.Text = "Password";
        this.PASSWORD.TabIndex = 1;

        // Datasoruce
        this.DATASOURCE.AutoSize = true;
        this.DATASOURCE.Location = new System.Drawing.Point(6, 102);
        this.DATASOURCE.Name = "datasource";
        this.DATASOURCE.Size = new System.Drawing.Size(55, 13);
        this.DATASOURCE.Text = "Datasource";
        this.DATASOURCE.TabIndex = 2;
        
        // Port
        this.PORT.AutoSize = true;
        this.PORT.Location = new System.Drawing.Point(5, 139);
        this.PORT.Name = "port";
        this.PORT.Size = new System.Drawing.Size(53, 13);
        this.PORT.Text = "Port";
        this.PORT.TabIndex = 3;

        // NUME_PROIECT
        this.NUME_PROIECT.AutoSize = true;
        this.NUME_PROIECT.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold 
        | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0))); // Cream un font pentru acest label. Functia Font ia ca parametrii un font(orice font instalat in pc), dimensiunea fontului in float, un font style(FontStyle.BOLD si FontStyle.ITALIC le transformam ca fiind un singur FontStyle), un Point si inca o valoare in bytes care nush ce face :)
        this.NUME_PROIECT.ForeColor = System.Drawing.SystemColors.HotTrack; // Setam culoare(de notat, poate fi folosita orice culoare folosind "System.Drawing.Color.Culoare")
        this.NUME_PROIECT.Location = new System.Drawing.Point(111, 9);
        this.NUME_PROIECT.Name = "NUME_PROIECT";
        this.NUME_PROIECT.Size = new System.Drawing.Size(272, 31);
        this.NUME_PROIECT.Text = "Baza de date";
        this.NUME_PROIECT.TabIndex = 4;
        
        // Nume proiect
        this.LOGIN_LABEL.AutoSize = true;
        this.LOGIN_LABEL.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold 
        | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.LOGIN_LABEL.ForeColor = System.Drawing.SystemColors.HotTrack;
        this.LOGIN_LABEL.Location = new System.Drawing.Point(203, 50);
        this.LOGIN_LABEL.Name = "project_name";
        this.LOGIN_LABEL.Size = new System.Drawing.Size(112, 25);
        this.LOGIN_LABEL.Text = "Login";
        this.LOGIN_LABEL.TabIndex = 5;

        // Username textbox
        this.USER_TEXT_BOX.Location = new System.Drawing.Point(108, 25); // Setam locatia la textbox
        this.USER_TEXT_BOX.Name = "username_textbox"; // Numele invizibil la textbox
        this.USER_TEXT_BOX.Size = new System.Drawing.Size(100, 20); // Setam dimensiunea la textbox
        this.USER_TEXT_BOX.TabIndex = 6;

        // Password textbox
        this.PASS_TEXT_BOX.Location = new System.Drawing.Point(108, 62);
        this.PASS_TEXT_BOX.Name = "password_textbox";
        this.PASS_TEXT_BOX.Size = new System.Drawing.Size(100, 20);
        this.PASS_TEXT_BOX.TabIndex = 7;

        // Datasource textbox
        this.DATASRC_TEXT_BOX.Location = new System.Drawing.Point(108, 99);
        this.DATASRC_TEXT_BOX.Name = "datasource_textbox";
        this.DATASRC_TEXT_BOX.Size = new System.Drawing.Size(100, 20);
        this.DATASRC_TEXT_BOX.TabIndex = 8;

        // Port textbox
        this.PORT_TEXT_BOX.Location = new System.Drawing.Point(108, 136);
        this.PORT_TEXT_BOX.Name = "port_textbox";
        this.PORT_TEXT_BOX.Size = new System.Drawing.Size(100, 20);
        this.PORT_TEXT_BOX.TabIndex = 9;

        // Login button
        this.LOGIN_BUTTON.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.00F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold 
        | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0))); // De asemenea aici setam un font la textul de la buton
        this.LOGIN_BUTTON.ForeColor = System.Drawing.Color.Green; // Setam culoare verde
        this.LOGIN_BUTTON.Location = new System.Drawing.Point((this.groupBox.Width / 2) - 25, 171); // Locatia butonuli care este la jumatatea GroupBox-ului minus 25 pentru a fi exact in mijloc(nush de ce trebe da trebe)
        this.LOGIN_BUTTON.Name = "LOGIN_BUTTON"; // Numele invizibil la buton
        this.LOGIN_BUTTON.Size = new System.Drawing.Size(75, 34); // Dimensiunea la buton
        this.LOGIN_BUTTON.Text = "Login"; // Textul la buton
        this.LOGIN_BUTTON.UseVisualStyleBackColor = true; // Ia sau seteaza o valoare care determina daca background-ul este desenat folosind stiluri vizuale(nush exact ce face da trebe)
        this.LOGIN_BUTTON.Click += new System.EventHandler(this.login_button_actiune); // Seteaza o actiune pentru cand este butonul apasat(de notat, functia "login_button_actiune" nu are parantezele la final deoarece este un EventHandler si funcatia "EventHandler" are nevoie de ea nu ca functie, nush sa explic, mai pe scurt o executa si merge :)
        this.LOGIN_BUTTON.TabIndex = 10;

        this.groupBox.Controls.Add(this.USERNAME); // Adaugam toate label-urile, textbox-urile si butoanele la GroupBox
        this.groupBox.Controls.Add(this.PASSWORD);
        this.groupBox.Controls.Add(this.DATASOURCE);
        this.groupBox.Controls.Add(this.PORT);
        this.groupBox.Controls.Add(this.PASS_TEXT_BOX);
        this.groupBox.Controls.Add(this.USER_TEXT_BOX);
        this.groupBox.Controls.Add(this.DATASRC_TEXT_BOX);
        this.groupBox.Controls.Add(this.PORT_TEXT_BOX);
        this.groupBox.Controls.Add(this.LOGIN_BUTTON);
        this.groupBox.Location = new System.Drawing.Point(this.Width / 2, 100); // Setam locatia la GroupBox la mijlocul winform-ului(this.Width sau this.Height sunt direct construite atunci cand folosesti clasa Form)
        this.groupBox.Name = "groupbox"; // Numele invizibil la GroupBox
        this.groupBox.Size = new System.Drawing.Size(221, 220); // Dimensiunea la GroupBox
        this.groupBox.Text = "Login"; // Textul grupului
    }

    // Aici cream aplicatia in sine
    private void createForm()
    {
        this.FormBorderStyle = FormBorderStyle.FixedSingle; // Face sa nu poata sa fie modificata marimea aplicatiei
        this.MaximizeBox = false; // Dezactiveaza butonul de marire
        this.MinimizeBox = true; // Tine activat butonul de micsorare in bara
        this.StartPosition = FormStartPosition.CenterScreen;
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F); // N am idee ce face asta :)
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; // Specifica diferitele typuri de autoscalare suportate de winforms
        this.BackColor = System.Drawing.SystemColors.GradientActiveCaption; // Seteaza culoarea de background
        this.ClientSize = new System.Drawing.Size(511, 333); // Dimensiunea aplicatiei
        this.Controls.Add(this.groupBox); // Adauga GroupBox-ul si orice nu mai este adaugat intr-un GroupBox
        this.Controls.Add(this.NUME_PROIECT);
        this.Controls.Add(this.LOGIN_LABEL);
        this.Name = "Form1"; // Numele invizibil
        this.Text = "Login Database"; // Numele aplicatiei
        this.groupBox.ResumeLayout(false); // Ceva setari pentru GroupBox care nush ce fac dar stiu ca trebe :)
        this.groupBox.PerformLayout();
        this.ResumeLayout(false); // De asemenea inca doua setari pentru winform care nush ce face dar stiu ca trebe, poti cauta pe google :)
        this.PerformLayout();
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Login());
    }
}