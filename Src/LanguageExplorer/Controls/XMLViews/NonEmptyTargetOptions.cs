// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Options for dealing with non-empty targets.
	/// </summary>
	public enum NonEmptyTargetOptions
	{
		/// <summary>
		/// Leave the non-empty value alone.
		/// </summary>
		DoNothing,
		/// <summary>
		/// Overwrite the non-empty target with the computed/copied value
		/// </summary>
		Overwrite,
		/// <summary>
		/// Append the computed/copied value to the non-empty target.
		/// </summary>
		Append
	}
}