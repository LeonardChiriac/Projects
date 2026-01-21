using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{

    public class Comanda
    {
        private string Id;
        private SuperBox SuperBoxOrigine;
        private SuperBox SuperBoxDestinatie;
        private Cont ContTrimitere;
        private Cont ContPrimire;
        private DateTime dataTrimitere;
        private DateTime dataLivrare;
        private bool IsUrgent;
        private bool Ridicata;

        public void SetId (string id) {  this.Id = id; }
        public void SetSuperBoxOrigine(SuperBox SuperBoxOrigine) { this.SuperBoxOrigine = SuperBoxOrigine; }
        public void SetSuperBoxDestinatie(SuperBox SuperBoxDestinatie) { this.SuperBoxDestinatie = SuperBoxDestinatie; }
        public void SetContTrimitere(Cont Cont) {this.ContTrimitere = Cont;}
        public void SetContPrimire(Cont Cont) { this.ContPrimire = Cont; }
        public void SetIsUrgent(bool IsUrgent) { this.IsUrgent = IsUrgent;}
        public void SetRidicata(bool Ridicata) { this.Ridicata = Ridicata;}
        public void SetdataTrimitere (DateTime data) {  this.dataTrimitere = data; }
        public void SetdataLivrare (DateTime data) { this.dataLivrare = data;}

        public string GetId() { return this.Id; }  
        public SuperBox GetSuperBoxOrigine() { return this.SuperBoxOrigine; }
        public SuperBox GetSuperBoxDestinatie() { return this.SuperBoxDestinatie; }
        public Cont GetContTrimitere() { return this.ContTrimitere; }
        public Cont GetContPrimire() { return this.ContPrimire; }
        public bool GetIsUrgent() {  return this.IsUrgent; }
        public bool GetRidicata() {  return this.Ridicata; }
        public DateTime GetdataTrimitere() { return this.dataTrimitere;}
        public DateTime GetdataLivrare() { return this.dataLivrare;}


       

        public Comanda(string Id, SuperBox SuperBoxOrigine, SuperBox SuperBoxDestinatie, Cont ContTrimitere, Cont ContPrimire, bool IsUrgent, bool Ridicata , DateTime datatrmitere , DateTime datalivrare)
        {
            this.Id = Id;
            this.SuperBoxOrigine = SuperBoxOrigine;
            this.SuperBoxDestinatie = SuperBoxDestinatie;
            this.ContTrimitere = ContTrimitere;
            this.ContPrimire = ContPrimire;
            this.IsUrgent = IsUrgent;
            this.Ridicata = Ridicata;
            this.dataLivrare = datalivrare;
            this.dataTrimitere = datatrmitere;
        }




        public string ToString()
        {
            string doarDataT = GetdataTrimitere().ToString("dd-MM-yyyy");
            string doarDataL = GetdataLivrare().ToString("dd-MM-yyyy");


            if (dataLivrare <= DateTime.Today)
            {

                if (IsUrgent == true)
                {
                    if (this.Ridicata == true)
                    {
                        return $"Id : {Id}\nContTrimitere : {ContTrimitere.GetUserName()}\nContPrimire : {ContPrimire.GetUserName()}\nSuperBoxOrigine : {SuperBoxOrigine.GetLocatie()}\nSuperBoxDestinatie : {SuperBoxDestinatie.GetLocatie()} \nDataTrimitere : {doarDataT} \nDataLivrare : {doarDataL} \nUrgenta : Da \nStatus : Livrata\nRidicata : Da";
                    }
                    else
                    {
                        return $"Id : {Id}\nContTrimitere : {ContTrimitere.GetUserName()}\nContPrimire : {ContPrimire.GetUserName()}\nSuperBoxOrigine : {SuperBoxOrigine.GetLocatie()}\nSuperBoxDestinatie : {SuperBoxDestinatie.GetLocatie()}\nDataTrimitere : {doarDataT} \nDataLivrare : {doarDataL} \nUrgenta : Da \nStatus : Livrata\nRidicata : Nu";
                    }




                }
                else
                {
                    if (this.Ridicata == true)
                    {
                        return $"Id : {Id}\nContTrimitere : {ContTrimitere.GetUserName()}\nContPrimire : {ContPrimire.GetUserName()}\n  SuperBoxOrigine : {SuperBoxOrigine.GetLocatie()}\nSuperBoxDestinatie : {SuperBoxDestinatie.GetLocatie()}\nDataTrimitere : {doarDataT} \nDataLivrare : {doarDataL} \nUrgenta : Nu \nStatus : Livrata\nRidicata : Da";
                    }
                    else
                    {
                        return $"Id : {Id}\nContTrimitere : {ContTrimitere.GetUserName()}\nContPrimire : {ContPrimire.GetUserName()}\n  SuperBoxOrigine : {SuperBoxOrigine.GetLocatie()}\nSuperBoxDestinatie : {SuperBoxDestinatie.GetLocatie()}\nDataTrimitere : {doarDataT}  \nDataLivrare :  {doarDataL} \nUrgenta : Nu \nStatus : Livrata\nRidicata : Nu";
                    }
                }
            }
            else
            {
                if (IsUrgent == true)
                {
                    if (this.Ridicata == true)
                    {
                        return $"Id : {Id}\nContTrimitere : {ContTrimitere.GetUserName()}\nContPrimire : {ContPrimire.GetUserName()}\nSuperBoxOrigine : {SuperBoxOrigine.GetLocatie()}\nSuperBoxDestinatie : {SuperBoxDestinatie.GetLocatie()} \nDataTrimitere : {doarDataT} \nDataLivrare : {doarDataL} \nUrgenta : Da \nStatus : In curs de livrare\nRidicata : Da";
                    }
                    else
                    {
                        return $"Id : {Id}\nContTrimitere : {ContTrimitere.GetUserName()}\nContPrimire : {ContPrimire.GetUserName()}\nSuperBoxOrigine : {SuperBoxOrigine.GetLocatie()}\nSuperBoxDestinatie : {SuperBoxDestinatie.GetLocatie()}\nDataTrimitere : {doarDataT} \nDataLivrare : {doarDataL} \nUrgenta : Da \nStatus : In curs de livrare\nRidicata : Nu";
                    }




                }
                else
                {
                    if (this.Ridicata == true)
                    {
                        return $"Id : {Id}\nContTrimitere : {ContTrimitere.GetUserName()}\nContPrimire : {ContPrimire.GetUserName()}\n  SuperBoxOrigine : {SuperBoxOrigine.GetLocatie()}\nSuperBoxDestinatie : {SuperBoxDestinatie.GetLocatie()}\nDataTrimitere : {doarDataT} \nDataLivrare : {doarDataL} \nUrgenta : Nu \nStatus : In curs de livrare\nRidicata : Da";
                    }
                    else
                    {
                        return $"Id : {Id}\nContTrimitere : {ContTrimitere.GetUserName()}\nContPrimire : {ContPrimire.GetUserName()}\n  SuperBoxOrigine : {SuperBoxOrigine.GetLocatie()}\nSuperBoxDestinatie : {SuperBoxDestinatie.GetLocatie()}\nDataTrimitere : {doarDataT}  \nDataLivrare :  {doarDataL} \nUrgenta : Nu \nStatus : In curs de livrare\nRidicata : Nu";
                    }
                }
            }
        }


        





    }


}
