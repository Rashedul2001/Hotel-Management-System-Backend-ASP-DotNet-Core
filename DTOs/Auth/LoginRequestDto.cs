namespace Hotel_Management_System_Backend_dotNet.DTOs.Auth
{
    public class LoginRequestDto
    {
        public required string EmailOrUserName { get; set; }

        public required string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}