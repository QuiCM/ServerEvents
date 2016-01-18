using ServerEvents;
using System;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
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
			EventsPlugin.AddTable(
				new SqlTable("ExampleTable",
					new SqlColumn("ExampleColumn", MySql.Data.MySqlClient.MySqlDbType.Int32)));

			EventsPlugin.SetServerResetTime(0, 0);

			EventsPlugin.OnPlayerLogin += EventsPlugin_OnPlayerLogin;
			EventsPlugin.OnServerReset += EventsPlugin_OnServerReset;
		}

		private void EventsPlugin_OnServerReset(object sender, EventArgs e)
		{
			Console.WriteLine("RESET");
		}

		private void EventsPlugin_OnPlayerLogin(TShockAPI.TSPlayer player, int loginStreak)
		{
			Console.WriteLine($"PLAYER {player.Name} HAS LOGIN STREAK: {loginStreak}");
		}
	}
}
