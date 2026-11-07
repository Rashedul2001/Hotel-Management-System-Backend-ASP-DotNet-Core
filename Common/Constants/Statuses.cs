namespace Hotel_Management_System_Backend_dotNet.Common.Constants
{
	public static class Statuses
	{
		public const string Available ="Available";
		public const string Occupied = "Occupied";
		public const string Cleaning = "Cleaning";
		public const string Maintenance = "Maintenance";
		public const string Blocked = "Blocked";
		public static readonly string[] AllStatuses = { Available, Occupied, Cleaning, Maintenance, Blocked };
	}
}
