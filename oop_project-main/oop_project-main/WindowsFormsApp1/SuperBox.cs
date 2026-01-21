using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public class SuperBox
    {
        private string Id;
        private string Locatie;


        public void SetId(string id) { this.Id = id; }
        public void SetLocatie(string locatie) { this.Locatie = locatie; }
        public string GetId() { return this.Id; }
        public string GetLocatie() { return this.Locatie; }

        public SuperBox()
        {
            this.Id = null;
            this.Locatie = null;
        }

        public SuperBox(string Id, string Locatie)
        {
            this.Id = Id;
            this.Locatie = Locatie;
        }





    }
}
