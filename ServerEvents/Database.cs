using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.IO;
using TShockAPI;
using TShockAPI.DB;

namespace ServerEvents
{
	public class Database
	{
		private IDbConnection _db;
		private SqlTableCreator _tableCreator;
		private string timeStr { get { return _db is MySqlConnection ? "UTC_TIMESTAMP()" : "datetime('now')"; } }

		private Database(IDbConnection db)
		{
			_db = db;

			//Define a table creator that will be responsible for ensuring the database table exists
			_tableCreator = new SqlTableCreator(_db,
				_db.GetSqlType() == SqlType.Sqlite
					? (IQueryBuilder)new SqliteQueryCreator()
					: new MysqlQueryCreator());

			//Define the table
			var table = new SqlTable("ServerEvents",
				new SqlColumn("UserID", MySqlDbType.Int32) { AutoIncrement = true, Primary = true },
				new SqlColumn("LastLoginTime", MySqlDbType.VarChar),
				new SqlColumn("DailyLogIn", MySqlDbType.Int32) { DefaultValue = "0" },
				new SqlColumn("DailyLogInStreak", MySqlDbType.Int32));

			_tableCreator.EnsureTableStructure(table);
		}

		/// <summary>
		/// Creates and returns an instance of <see cref="Database"/>
		/// </summary>
		/// <param name="name">File name (without .sqlite) if using SQLite, database name if using MySQL</param>
		/// <returns>Instance of <see cref="Database"/></returns>
		public static Database InitDb(string name)
		{
			IDbConnection db;
			if (TShock.Config.StorageType.ToLower() == "sqlite")
			{
				//Creates the database connection
				db = new SqliteConnection(string.Format("uri=file://{0},Version=3",
						  Path.Combine(TShock.SavePath, name + ".sqlite")));
			}
			else if (TShock.Config.StorageType.ToLower() == "mysql")
			{
				try
				{
					var host = TShock.Config.MySqlHost.Split(':');
					db = new MySqlConnection
					{
						ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4}",
							host[0],
							host.Length == 1 ? "3306" : host[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword
							)
					};
				}
				catch (MySqlException x)
				{
					TShock.Log.Error(x.ToString());
					throw new Exception("MySQL not setup correctly.");
				}
			}
			else
				throw new Exception("Invalid storage type.");

			return new Database(db);
		}

		public QueryResult QueryReader(string query, params object[] args)
		{
			return _db.QueryReader(query, args);
		}

		public int Query(string query, params object[] args)
		{
			return _db.Query(query, args);
		}

		internal void AddTable(SqlTable table)
		{
			if (!_tableCreator.EnsureTableStructure(table))
			{
				throw new Exception($"Failed to create table '{table.Name}'");
			}
		}

		internal bool Contains(int userID)
		{
			using (QueryResult res = _db.QueryReader("SELECT DailyLogIn FROM ServerEvents WHERE UserID = @0", userID))
			{
				return res.Read();
			}
		}

		internal void RegisterNewUser(int userID)
		{
			_db.Query($"INSERT INTO ServerEvents (UserID, LastLoginTime, DailyLogIn, DailyLogInStreak) VALUES (@0, {timeStr}, 1, 1)",
				userID);
		}

		internal void UpdateLogin(int userID)
		{
            _db.Query($"UPDATE ServerEvents SET DailyLogIn = 1, DailyLogInStreak = DailyLogInStreak + 1, LastLoginTime = {timeStr} WHERE UserID = @0",
				userID);
		}

		internal LoginData GetLoginData(int userID)
		{
			using (QueryResult res = _db.QueryReader("SELECT * FROM ServerEvents WHERE UserID = @0", userID))
			{
				if (res.Read())
				{
					string time = res.Get<string>("LastLoginTime");
					int streak = res.Get<int>("DailyLogInStreak");
					bool loggedIn = res.Get<int>("DailyLogIn") == 1;

					return new LoginData(userID, time, streak, loggedIn);
				}

				return default(LoginData);
			}
		}

		internal string GetLastLoginTime(int userID)
		{
			using (QueryResult res = _db.QueryReader("SELECT LastLoginTime FROM ServerEvents WHERE UserID = @0", userID))
			{
				if (res.Read())
				{
					return res.Get<string>("LastLoginTime");
				}
				return string.Empty;
			}
		}

		internal bool HasLoggedIn(int userID)
		{
			using (QueryResult res = _db.QueryReader("SELECT DailyLogIn FROM ServerEvents WHERE UserID = @0", userID))
			{
				if (res.Read())
				{
					return res.Get<int>("DailyLogIn") == 1;
				}
				return false;
			}
		}

		internal int GetLoginStreak(int userID)
		{
			using (QueryResult res = _db.QueryReader("SELECT DailyLogInStreak FROM ServerEvents WHERE UserID = @0", userID))
			{
				if (res.Read())
				{
					return res.Get<int>("DailyLogInStreak");
				}
				return 0;
			}
		}

		internal void ResetDailyLogins()
		{
			_db.Query("UPDATE ServerEvents SET DailyLogInStreak = 0 WHERE DailyLogIn = 0");
            _db.Query("UPDATE ServerEvents SET DailyLogIn = 0");
		}
	}
}
