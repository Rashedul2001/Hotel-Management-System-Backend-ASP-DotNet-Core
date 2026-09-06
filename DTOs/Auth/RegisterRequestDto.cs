namespace Hotel_Management_System_Backend_dotNet.DTOs.Auth
{
    public class RegisterRequestDto
    {
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string ConfirmPassword { get; set; }
        public required bool AcceptTerms { get; set; }


    }
}
