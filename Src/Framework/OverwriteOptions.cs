// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Options to indicate how to deal with user-modified styles.
	/// </summary>
	public enum OverwriteOptions
	{
		/// <summary>
		/// Do not overwrite any properties of the user-modified style
		/// </summary>
		Skip,
		/// <summary>
		/// Overwrite only the functional properties of the user-modified style,
		/// not the properties that merely affect appearance.
		/// </summary>
		FunctionalPropertiesOnly,
		/// <summary>
		/// Overwrite all the properties of the user-modified style
		/// </summary>
		All,
	}
}