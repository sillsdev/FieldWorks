// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface that returns an IPublisher implementation.
	/// </summary>
	public interface IPublisherProvider
	{
		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		IPublisher Publisher { get; }
	}
}