// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Interface that allows for suspending and renewing Application idle time event handling.
	/// </summary>
	public interface IApplicationIdleEventHandler
	{
		/// <summary>
		/// Call this for the duration of a block of code where we don't want idle events.
		/// (Note that various things outside our control may pump events and cause the
		/// timer that fires the idle events to be triggered when we are not idle, even in the
		/// middle of processing another event.) Call ResumeIdleProcessing when done.
		/// </summary>
		void SuspendIdleProcessing();

		/// <summary>
		/// See SuspendIdleProcessing.
		/// </summary>
		void ResumeIdleProcessing();
	}
}
