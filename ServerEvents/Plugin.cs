using Rests;
using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace ServerEvents
{
	[ApiVersion(1, 22)]
	public class ServerEventsPlugin : TerrariaPlugin
	{
		public override string Author
		{
			get
			{
				return "White";
			}
		}

		public override string Name
		{
			get
			{
				return "Server Events";
			}
		}

		public override string Description
		{
			get
			{
				return "Creates a 24/hr event cycle";
			}
		}

		/// <summary>
		/// Whether or not the event thread has been started
		/// </summary>
		private bool _started;
		/// <summary>
		/// Event thread - handles the server reset as well as de/registering methods to be run on timers
		/// </summary>
		private EventTimers _events;

		/// <summary>
		/// Event fired when the server hits its daily reset
		/// </summary>
		public event EventHandler OnServerReset;

		public delegate void PlayerLogin(TSPlayer player, LoginData loginData);
		/// <summary>
		/// Event fired when a player logs in. Use this event to retrieve login streak information
		/// </summary>
		public event PlayerLogin OnPlayerLogin;

		internal Database db;

		public ServerEventsPlugin(Main game) : base(game)
		{
			//Load before other plugins
			Order = -1000;
		}

		public override void Initialize()
		{
			db = Database.InitDb("ServerEvents");
			
			TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += PlayerPostLogin;
			TShock.Initialized += TShock_Initialized;
			Start();
		}

		private void TShock_Initialized()
		{
			TShock.RestApi.Register(new RestCommand("/ServerEvents/logindata", GetLoginData));
			TShock.Initialized -= TShock_Initialized;
		}

		/// <summary>
		/// Starts all internal timing
		/// </summary>
		internal void Start()
		{
			//If already started, don't start again
			if (_started)
			{
				return;
			}
			_started = true;

			//Create a new instance of EventThread and hook into the reset event
			_events = new EventTimers();
			_events.OnReset += Events_OnReset;
			_events.Start();
			SetServerResetTime(0, 0);
		}

		/// <summary>
		/// Sets the hour and minute at which the server should reset each day (UTC time).
		/// This must be called for the <see cref="OnServerReset"/> event to be fired.
		/// </summary>
		/// <param name="hour">UTC hour</param>
		/// <param name="minute">UTC minute</param>
		public void SetServerResetTime(int hour, int minute)
		{
			_events.SetResetTime($"{FormatHours(hour)}:{FormatMinutes(minute)}:00");
		}

		/// <summary>
		/// Run a method at the specified interval.
		///	Returns an integer used with <see cref="StopRunningCallback"/> to stop the method from running.
		/// </summary>
		/// <param name="hour">Hours between subsequent runs</param>
		/// <param name="minute">Minutes between subsequent runs</param>
		/// <param name="second">Seconds between subsequent runs</param>
		/// <param name="callback">Method to run</param>
		/// <returns>Handle used to stop running the method</returns>
		public int RunEvery(int hour, int minute, int second, Action callback)
		{
			int handle;
			_events.RegisterTimedCallback(Tuple.Create(hour, minute, second), callback, out handle);
			return handle;
		}

		/// <summary>
		/// Stops running a queued method
		/// </summary>
		/// <param name="handle">Handle provided by <see cref="RunEvery"/></param>
		public void StopRunningCallback(ref int handle)
		{
			_events.DeregisterTimedCallback(ref handle);
		}

		/// <summary>
		/// Returns the <see cref="LoginData"/> assosciated with the given user
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public LoginData GetLoginData(User user)
		{
			return GetLoginData(user.ID);
		}

		/// <summary>
		/// Returns the <see cref="LoginData"/> assosciated with the given user ID
		/// </summary>
		/// <param name="userID"></param>
		/// <returns></returns>
		public LoginData GetLoginData(int userID)
		{
			return db.GetLoginData(userID);
		}

		/// <summary>
		/// Returns true if the user has logged in during the current day. Otherwise returns false.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public bool HasLoggedIn(User user)
		{
			return HasLoggedIn(user.ID);
		}

		/// <summary>
		/// Returns true if the user has logged in during the current day. Otherwise returns false.
		/// </summary>
		/// <param name="userID"></param>
		/// <returns></returns>
		public bool HasLoggedIn(int userID)
		{
			return db.HasLoggedIn(userID);
		}

		/// <summary>
		/// Returns the number of consecutive days the user has logged in
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public int GetLoginStreak(User user)
		{
			return GetLoginStreak(user.ID);
		}

		/// <summary>
		/// Returns the number of consecutive days the user has logged in
		/// </summary>
		/// <param name="userID"></param>
		/// <returns></returns>
		public int GetLoginStreak(int userID)
		{
			return db.GetLoginStreak(userID);
		}

		/// <summary>
		/// Adds a new table into the database
		/// </summary>
		/// <param name="table"></param>
		public void AddTableToDb(SqlTable table)
		{
			db.AddTable(table);
		}

		/// <summary>
		/// Query the database, returning the number of rows altered
		/// </summary>
		/// <param name="query"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public int QueryDb(string query, params object[] args)
		{
			return db.Query(query, args);
		}

		/// <summary>
		/// Query the database, returning a <see cref="QueryResult"/> object containing result data
		/// </summary>
		/// <param name="query"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public QueryResult ReadFromDb(string query, params object[] args)
		{
			return db.QueryReader(query, args);
		}

		public RestObject GetLoginData(RestRequestArgs args)
		{
			string idStr = args.Parameters["id"];
			if (string.IsNullOrWhiteSpace(idStr))
			{
				return new RestObject("400") { Error = "Missing or empty parameter 'id'" };
			}

			int id;
			if (!int.TryParse(idStr, out id))
			{
				return new RestObject("400") { Error = "Invalid id supplied. ID must be a valid user ID" };
			}

			LoginData data = db.GetLoginData(id);

			return new RestObject() { { "response", data } };
		}

		private void Events_OnReset(object sender, EventArgs e)
		{
			//Reset daily login metrics
			db.ResetDailyLogins();

			//Fire the OnServerReset event when EventThread tells us that the server is ready to reset
			EventHandler onReset = OnServerReset;
			if (onReset != null)
			{
				onReset(null, EventArgs.Empty);
			}
		}

		private void PlayerPostLogin(TShockAPI.Hooks.PlayerPostLoginEventArgs e)
		{
			//if the player is not in the db, add them
			if (!db.Contains(e.Player.User.ID))
			{
				db.RegisterNewUser(e.Player.User.ID);
			}
			
			//if the player is in db but has not logged in yet
			else if (!db.HasLoggedIn(e.Player.User.ID))
			{
				//update their login streak and daily login
				db.UpdateLogin(e.Player.User.ID);
			}
			else
			{
				db.UpdateTime(e.Player.User.ID);
			}
			
			PlayerLogin playerLogin = OnPlayerLogin;
			if (playerLogin != null)
			{
				//fire the OnPlayerLogin event
				playerLogin(e.Player, db.GetLoginData(e.Player.User.ID));
			}
		}

		private string FormatMinutes(int minutes)
		{
			if (minutes >= 60)
			{
				minutes = 0;
			}
			if (minutes >= 10)
			{
				return minutes.ToString();
			}
			return $"0{minutes.ToString()}";
		}

		private string FormatHours(int hours)
		{
			if (hours >= 24)
			{
				hours = 0;
			}
			if (hours >= 10)
			{
				return hours.ToString();
			}
			return $"0{hours}";
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				TShockAPI.Hooks.PlayerHooks.PlayerPostLogin -= PlayerPostLogin;
				_events.Dispose();
				db.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
