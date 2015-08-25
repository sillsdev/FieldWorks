// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.CoreImpl
{
	/// <summary>
	/// Use to get accessability information.
	/// Implementors of this interface MUST derive, directly, or indirectly,
	/// from Control, as it will be cast to Control.
	/// </summary>
	/// <remarks>
	/// The only real reason this interface has been defined is so MultiPane is happy.
	/// </remarks>
	public interface IMainUserControl
	{
		/// <summary>
		/// Get or set the name to be used by the accessibility object.
		/// </summary>
		string AccName { get; set; }

		/// <summary>
		/// Get/set string that will trigger a message box to show.
		/// </summary>
		/// <remarks>Set to null or string.Empty to not show the message box.</remarks>
		string MessageBoxTrigger { get; set; }
	}
}