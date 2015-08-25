// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IHelpTopicProvider.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Controls that present UI can use this interface to request application-specific help
	/// file and topics. For example, a dialog can request a Help file URL to use when the user
	/// presses the dialog's Help button. Any app which has a Help file should implement this
	/// directly or own an object dedicated to implementing this.
	///</summary>
	/// ----------------------------------------------------------------------------------------
	public interface IHelpTopicProvider
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>Gets a URL identifying a Help topic.</summary>
		/// <param name='ksPropName'>A constant identifier for the desired Help topic</param>
		/// ------------------------------------------------------------------------------------
		string GetHelpString(string ksPropName);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The HTML Help file (.chm) for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string HelpFile { get; }
	}
}