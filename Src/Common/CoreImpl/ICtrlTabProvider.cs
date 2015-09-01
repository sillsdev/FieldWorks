// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Implement this interface on controls that can provide targets to Cntrl-(Shift-)Tab.
	/// </summary>
	public interface ICtrlTabProvider
	{
		/// <summary>
		/// Gather up suitable targets to Ctrl(+Shift)+Tab into.
		/// </summary>
		/// <param name="targetCandidates">List of places to move to.</param>
		/// <returns>A suitable target for moving to in Ctrl(+Shift)+Tab.
		/// This returned value should also have been added to the main list.</returns>
		Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates);
	}
}