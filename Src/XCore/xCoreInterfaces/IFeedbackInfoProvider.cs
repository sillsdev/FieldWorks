// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IFeedbackInfoProvider.cs
// ---------------------------------------------------------------------------------------------

namespace XCore
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
