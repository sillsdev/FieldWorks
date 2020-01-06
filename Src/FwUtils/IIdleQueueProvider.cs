// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Interface to return an instance of IdleQueue, which is a singleton per IFwMainWnd instance.
	/// </summary>
	public interface IIdleQueueProvider
	{
		/// <summary>
		/// Get the IdleQueue instance, which is a singleton per IFwMainWnd instance.
		/// </summary>
		IdleQueue IdleQueue { get; }
	}
}