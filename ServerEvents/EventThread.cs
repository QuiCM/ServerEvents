using System;
using System.Collections.Generic;
using System.Timers;

namespace ServerEvents
{
	internal class EventThread
	{
		/// <summary>
		/// Time at which the server will reset
		/// </summary>
		private DateTime ResetTime;
		/// <summary>
		/// Reset timer ticks once every reset
		/// </summary>
		private Timer ResetTimer;

		internal event EventHandler OnReset;
		private List<CallbackTimer> Timers = new List<CallbackTimer>();

		internal void Start()
		{
			ResetTimer = new Timer();
			ResetTimer.Elapsed += ResetTimer_Elapsed;
		}

		private void ResetTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			//reset timers
			foreach (Timer t in Timers)
			{
				t.Stop();
				t.Start();
			}
			
			//Trigger the reset event
			EventHandler onReset = OnReset;
			if (onReset != null)
			{
				onReset(null, EventArgs.Empty);
			}

			//Add 24 hours to the ResetTime
			ResetTime += new TimeSpan(24, 0, 0);
			//Set the timer to tick again in 24 hours
			ResetTimer.Stop();
			ResetTimer.Interval = (24 * 60 * 60) * 1000;
			ResetTimer.Start();
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (sender == null)
			{
				return;
			}

			CallbackTimer timer = (CallbackTimer)sender;

			if (timer == null || !timer.Enabled)
			{
				return;
			}

			timer.Callback.Invoke();
		}

		internal void SetResetTime(string time)
		{
			//Parse time string into a UTC DateTime
			DateTime d = DateTime.Parse(time).ToUniversalTime();
			//if the time string was earlier today, make it the same time tomorrow instead
			if (d < DateTime.UtcNow)
			{
				d += new TimeSpan(24, 0, 0);
			}
			ResetTime = d;

			TimeSpan diff = d - DateTime.UtcNow;

			int seconds = (diff.Hours * 60 * 60) + (diff.Minutes * 60) + (diff.Seconds);
			if (ResetTimer != null)
			{
				ResetTimer.Stop();
			}
			ResetTimer.Interval = seconds * 1000;
			ResetTimer.Start();
		}

		internal void RegisterTimedCallback(Tuple<int, int, int> time, Action callback, out int handle)
		{
			int seconds = (time.Item1 * 60 * 60) + (time.Item2 * 60) + time.Item3;
			CallbackTimer t = new CallbackTimer(seconds * 1000, callback);
			t.Elapsed += Timer_Elapsed;

			//Get the first null index, or append to the list if there isn't one
			handle = Timers.IndexOf(null);
			if (handle == -1)
			{
				handle = Timers.Count;
				Timers.Add(t);
			}
			else
			{
				Timers[handle] = t;
			}

			t.Start();
		}

		internal void DeregisterTimedCallback(ref int handle)
		{
			Timers[handle].Stop();
			Timers[handle].Dispose();
			Timers[handle] = null;
			handle = -1;
		}
	}
}
