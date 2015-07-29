// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This interface MUST be implemented by any control which is configured with a defaultFocusControl.
	/// A multipane will not focus any control that does not implement this. This also allows a control
	/// to know if it can expect to be focused by default, or if it should avoid taking focus when it is
	/// not configured as the important control in the MultiPane.
	/// </summary>
	public interface IFocusablePanePortion
	{
		/// <summary>
		/// true if there is a parent multipane which has been configured with this child control as the focused one.
		/// This is expected to be set by the multipane based off the xml configuration settings.
		/// Some controls which would normally try and take focus (like DataTree) should not do so if they find this
		/// Property has been set to false.
		/// </summary>
		bool IsFocusedPane
		{
			get;set;
		}

		/// <summary>
		/// Most IFocusablePanePortion implementors will be a Control, so this method will not need to be implemented.
		/// It is included so that code working with objects cast to this interface can call the Focus method without
		/// extra effort. If an implementor is not a control it must still do something intelligent as MultiPane will
		/// call this method.
		/// </summary>
		/// <returns></returns>
		bool Focus();
	}
}
