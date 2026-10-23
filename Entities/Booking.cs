namespace Hotel_Management_System_Backend_dotNet.Entities
{
    public enum BookingStatus
    {
        PendingPayment, // Locked while the user is typing credit card details
        Confirmed,      // Payment successful
        Cancelled,      // Cancelled by user or admin
        Completed       // Stay is over
    }

    public class Booking 
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public Room? Room { get; set; }

        public string UserId { get; set; } = string.Empty; // From IdentityUser

        public DateOnly CheckInDate { get; set; }  
        public DateOnly CheckOutDate { get; set; } 

        public BookingStatus Status { get; set; }

        // The exact time the temporary lock expires if payment isn't completed
        public DateTime? PaymentExpiresAt { get; set; } 

        public decimal TotalPrice { get; set; }
    }
}
