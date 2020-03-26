// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Enum representing the anthropology list options presented to the user
	/// </summary>
	/// <remarks>This has to be public, because some test has a required public test that uses this class in "TestCase" mode.</remarks>
	public enum ListChoice
	{
		/// <summary>
		/// Empty list, user defines later
		/// </summary>
		UserDef,
		/// <summary>
		/// Standard OCM list
		/// </summary>
		OCM,
		/// <summary>
		/// Enhanced OCM list ("FRAME")
		/// </summary>
		FRAME
	}
}