namespace BlazorApp.Services;

    public class AuthService
    {
        public string? CurrentUserEmail { get; private set; }
        public string? CurrentUserName { get; private set; }


        public bool IsloggedIn => CurrentUserEmail != null;

        public bool Login(string email, string password)
        {
        
            // Dummy user for now
            if (email == "demo@weather.com" && password == "1234")
            {
                CurrentUserEmail = email;
                CurrentUserName = "Demo User";
                return true;
            }
            return false;
        }

        //logout
        public void Logout()
        {
            CurrentUserEmail = null;
            CurrentUserName = null;
        }
    }
