// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IFeedbackInfoProvider.cs
// ---------------------------------------------------------------------------------------------

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for any developed component that has a distinct support/development group
	/// (typically an application, but can also be used for plug-in components).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFeedbackInfoProvider
	{
		/// <summary>E-mail address for bug reports, etc.</summary>
		string SupportEmailAddress { get; }
		/// <summary>E-mail address for feedback reports, kudos, etc.</summary>
		string FeedbackEmailAddress { get; }
	}
}
