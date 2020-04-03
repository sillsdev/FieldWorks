// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Application;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Another way to request a selection at the end of the UOW.
	/// </summary>
	internal static class RequestSelectionByHelper
	{
		private static SelectionHelper m_helper;
		private static IVwSelection m_sel;

		internal static void SetupForPropChangedCompletedCall(IActionHandlerExtensions hookup, SelectionHelper helper, IVwSelection sel = null)
		{
			m_helper = helper;
			m_sel = sel;
			if (helper != null)
			{
				hookup.DoAtEndOfPropChanged(m_hookup_PropChangedCompleted);
			}
		}

		/// <summary>
		/// Conditionally restores the selection and scroll position based on the selection
		/// helper.
		/// </summary>
		private static void m_hookup_PropChangedCompleted()
		{
			try
			{
				if (m_sel == null || !m_sel.IsValid)
				{
					m_helper.RestoreSelectionAndScrollPos();
				}
			}
			catch (COMException)
			{
				Debug.Assert(false);
				// Ignore any errors that happen when trying to set the selection. We really don't
				// want a program to crash if it fails.
			}
			finally
			{
				m_helper = null;
				m_sel = null;
			}
		}
	}
}