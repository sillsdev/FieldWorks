// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Interface that returns an ISubscriber implementation.
	/// </summary>
	public interface ISubscriberProvider
	{
		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		ISubscriber Subscriber { get; }
	}
}