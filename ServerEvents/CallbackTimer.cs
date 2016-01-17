using System;
using System.Timers;

namespace ServerEvents
{
	/// <summary>
	/// Timer with a <see cref="System.Action"/> callback
	/// </summary>
	public class CallbackTimer : Timer
	{
		public Action Callback { get; private set; }

		public CallbackTimer(double interval, Action callback) : base(interval)
		{
			Callback = callback;
		}
	}
}
