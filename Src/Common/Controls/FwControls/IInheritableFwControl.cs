// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// An interface which indicates that the control may display values inherited from a parent setting
	/// </summary>
	public interface IInheritableFwControl
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A value indicating whether this instance represents a property which is
		/// inherited.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsInherited
		{
			get;
			set;
		}
	}
}
