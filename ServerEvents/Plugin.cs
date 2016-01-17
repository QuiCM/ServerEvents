﻿using System;
using System.Threading;
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
		private EventThread _events;
		
		/// <summary>
		/// Event fired when the server hits its daily reset
		/// </summary>
		public event EventHandler OnServerReset;

		public delegate void PlayerLogin(TSPlayer player, int loginStreak);
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
			Start();
		}

		/// <summary>
		/// Starts all internal timing
		/// </summary>
		public void Start()
		{
			//If already started, don't start again
			if (_started)
			{
				return;
			}
			_started = true;

			//Create a new instance of EventThread and hook into the reset event
			_events = new EventThread();
			_events.OnReset += Events_OnReset;

			//Start the thread
			Thread t = new Thread(new ThreadStart(_events.Start));
			t.Start();
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
			
			PlayerLogin playerLogin = OnPlayerLogin;
			if (playerLogin != null)
			{
				//fire the OnPlayerLogin event
				playerLogin(e.Player, db.GetLoginStreak(e.Player.User.ID));
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
	}
}
