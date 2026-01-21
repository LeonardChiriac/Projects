using System;

namespace WindowsFormsApp1
{
    public abstract class Cont
    {
        private string UserName;
        private string Email;
        private string Password;

        public void SetUserName(string userName) { this.UserName = userName; }
        public void SetEmail(string email) { this.Email = email; }
        public void SetPassword(string password) { this.Password = password; }

        public string GetUserName() { return this.UserName; }
        public string GetEmail() { return this.Email; }
        public string GetPassword() { return this.Password; }

        public Cont()
        {
            this.UserName = null;
            this.Email = null;
            this.Password = null;
        }
        public Cont(string UserName, string Email, string Password)
        {
            this.UserName = UserName;
            this.Email = Email;
            this.Password = Password;
        }

        public abstract string ToString();


        public virtual Cont DeepCopy()
        {

            return (Cont)MemberwiseClone();
        }
    }

    public class Utilizator : Cont
    {
        private string IdUtilizator;

        public void SetIdUtilizator(string IdUtilizator) { this.IdUtilizator = IdUtilizator; }
        public string GetIdUtilizator() { return this.IdUtilizator; }

        public Utilizator()
        {
            this.IdUtilizator = null;
        }

        public Utilizator(string UserName, string Email, string Password, string IdUtilizator) : base(UserName, Email, Password)
        {
            this.IdUtilizator = IdUtilizator;
        }

        public override string ToString()
        {
            return $"Email : {GetEmail()} \n UserName : {GetUserName()} \n IdUtilizator : {GetIdUtilizator()}";
        }

        public override Cont DeepCopy()
        {
            var copy = (Utilizator)base.DeepCopy();
            copy.IdUtilizator = this.IdUtilizator;
            return copy;
        }

    }

    public class Admin : Cont
    {
        private bool IsAdmin;

        public void SetIsAdmin(bool IsAdmin) { this.IsAdmin = IsAdmin; }
        public bool GetIsAdmin() { return this.IsAdmin; }

        public Admin()
        {
            IsAdmin = false;
        }

        public Admin(string UserName, string Email, string Password, bool IsAdmin) : base(UserName, Email, Password)
        {
            this.IsAdmin = IsAdmin;
        }

        public override string ToString()
        {
            return $"Email : {GetEmail()} \n UserName : {GetUserName()}";
        }

        public void SetAdminTrue()
        {
            if (IsAdmin == false)
            {
                IsAdmin = true;
            }
        }

        public override Cont DeepCopy()
        {
            var copy = (Admin)base.DeepCopy();
            copy.IsAdmin = this.IsAdmin;
            return copy;
        }
    }
}
