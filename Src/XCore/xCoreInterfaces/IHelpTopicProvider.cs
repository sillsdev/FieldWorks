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
// File: IHelpTopicProvider.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------

namespace XCore
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