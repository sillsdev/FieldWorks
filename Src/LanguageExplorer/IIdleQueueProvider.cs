// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer
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