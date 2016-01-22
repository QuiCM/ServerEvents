using ServerEvents;
using System;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace SeverEvents.Example
{
	[ApiVersion(1, 22)]
	public class ServerEventsExamplePlugin : TerrariaPlugin
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
				return "Server Events Example";
			}
		}

		public override string Description
		{
			get
			{
				return "Example plugin for ServerEvents";
			}
		}

		public ServerEventsPlugin EventsPlugin;

		public ServerEventsExamplePlugin(Main game) : base(game)
		{
		}

		public override void Initialize()
		{
			EventsPlugin = (ServerEventsPlugin)ServerApi.Plugins.Select(p => p.Plugin).First(plugin => plugin.Name == "Server Events");
			EventsPlugin.AddTableToDb(
				new SqlTable("ExampleTable",
					new SqlColumn("ExampleColumn", MySql.Data.MySqlClient.MySqlDbType.Int32)));

			//Set reset time for 0:00 UTC
			EventsPlugin.SetServerResetTime(0, 0);

			//Hook into the Events Plugin's PlayerLogin event to receive login data
			EventsPlugin.OnPlayerLogin += EventsPlugin_OnPlayerLogin;
			//Hook into the Events Plugin's Reset event to handle resets
			EventsPlugin.OnServerReset += EventsPlugin_OnServerReset;

			//Run the TalkingClock method every 1 second
			int handle = EventsPlugin.RunEvery(0, 0, 1, TalkingClock);
			//Stop running the TalkingClock method
			EventsPlugin.StopRunningCallback(ref handle);
		}

		private void EventsPlugin_OnServerReset(object sender, EventArgs e)
		{
			Console.WriteLine("RESET");
		}

		private void EventsPlugin_OnPlayerLogin(TSPlayer player, LoginData loginData)
		{
			Console.WriteLine($"PLAYER {player.Name} HAS LOGIN STREAK: {loginData.LoginStreak}");

			if (loginData.LoginStreak > 5)
			{
				Console.WriteLine($"PLAYER {player.Name} has logged in 5 days in a row!");
				EventsPlugin.QueryDb("INSERT INTO ExampleTable (ExampleColumn) VALUES (@0)", loginData.UserID);
			}
		}

		private void TalkingClock()
		{
			TSPlayer.All.SendInfoMessage($"The time is {DateTime.UtcNow.ToString("T")}");
		}
	}
}
