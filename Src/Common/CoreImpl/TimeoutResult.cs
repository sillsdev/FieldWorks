//From:http://www.codeproject.com/cs/miscctrl/MessageBoxEx.asp

using System;

namespace Utils.MessageBoxExLib
{
	/// <summary>
	/// Enumerates the kind of results that can be returned when a
	/// message box times out
	/// </summary>
	public enum TimeoutResult
	{
		/// <summary>
		/// On timeout the value associated with the default button is set as the result.
		/// This is the default action on timeout.
		/// </summary>
		Default,

		/// <summary>
		/// On timeout the value associated with the cancel button is set as the result. If
		/// the messagebox does not have a cancel button then the value associated with
		/// the default button is set as the result.
		/// </summary>
		Cancel,

		/// <summary>
		/// On timeout MessageBoxExResult.Timeout is set as the result.
		/// </summary>
		Timeout
	}
}
