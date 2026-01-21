using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public class FirmaCurierat
    {

        private string Nume;
        private List<Cont> Conturi;
        private List<Comanda> Comenzi;
        private List<SuperBox> SuperBoxes;
        private List<Admin> ListaAsteptare;

        public void SetNume(string Nume) { this.Nume = Nume; }
        public void SetConturi(List<Cont> Conturi) { this.Conturi = Conturi; }
        public void SetComenzi(List<Comanda> Comenzi) { this.Comenzi = Comenzi; }
        public void SetSuperBox(List<SuperBox> Superbox) { this.SuperBoxes = Superbox; }
        public void SetListaAsteptare(List<Admin> ListaAsteptare) { this.ListaAsteptare = ListaAsteptare; }
        public string GetNume() { return Nume; }
        public List<Cont> GetConturi() { return Conturi; }
        public List<Comanda> GetComenzi() { return Comenzi; }
        public List<SuperBox> GetSuperBox() { return SuperBoxes; }
        public List<Admin> GetListaAsteptare() { return ListaAsteptare; }


        public FirmaCurierat()
        {
            Conturi = new List<Cont>();
            Comenzi = new List<Comanda>();
            SuperBoxes = new List<SuperBox>();
            ListaAsteptare = new List<Admin>();
        }
        public FirmaCurierat(string Nume, List<Cont> Conturi, List<Comanda> Comenzi, List<SuperBox> superBoxes, List<Admin> ListaAsteptare)
        {
            this.Nume = Nume;
            this.Conturi = Conturi;
            this.Comenzi = Comenzi;
            this.SuperBoxes = superBoxes;
            this.ListaAsteptare = ListaAsteptare;
        }


        public void AdaugaCont(Cont cont)
        {
            this.Conturi.Add(cont);
        }

        public void AdaugaComanda(Comanda comanda)
        {
            this.Comenzi.Add(comanda);
        }
        public void AdaugaSuperBox(SuperBox superBox)
        {
            this.SuperBoxes.Add(superBox);
        }
        public void AdaugaAdminListaAsteptare(Admin admin)
        {
            this.ListaAsteptare.Add(admin);
        }

        public void RemoveCont(Cont cont)
        {
            this.Conturi.Remove(cont);
        }

        public void RemoveComanda(Comanda comanda)
        {
            this.Comenzi.Remove(comanda);
        }

        public void RemoveSuperBox(SuperBox superBox)
        {
            this.SuperBoxes.Remove(superBox);
        }

        public void RemoveAdminListaAsteptare(Admin admin)
        {
            this.ListaAsteptare.Remove(admin);
        }





    }
}
