namespace ServerEvents
{
	/// <summary>
	/// Represents a user's database record
	/// </summary>
	public struct LoginData
	{
		/// <summary>
		/// ID of user this data belongs to
		/// </summary>
		public int UserID;
		/// <summary>
		/// String representation of the last time this user logged in
		/// </summary>
		public string LastLoginDateString;
		/// <summary>
		/// Number of days in a row this user has logged in
		/// </summary>
		public int LoginStreak;
		/// <summary>
		/// Whether or not the user has logged in today
		/// </summary>
		public bool HasLoggedIn;

		internal LoginData(int userID, string loginDate, int streak, bool loggedIn)
		{
			UserID = userID;
			LastLoginDateString = loginDate;
			LoginStreak = streak;
			HasLoggedIn = loggedIn;
		}
	}
}
