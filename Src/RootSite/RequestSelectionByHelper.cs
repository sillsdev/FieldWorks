// Copyright (c) 2009-2018 SIL International
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
	public class RequestSelectionByHelper
	{
		private readonly IActionHandlerExtensions m_hookup;
		private readonly SelectionHelper m_helper;
		private readonly IVwSelection m_sel;

		/// <summary>
		/// Make one to establish the selection indicated by the helper when the current UOW on
		/// the action handler completes.
		/// </summary>
		public RequestSelectionByHelper(IActionHandlerExtensions hookup, SelectionHelper helper) :
			this(hookup, null, helper)
		{
		}

		/// <summary>
		/// Make one to establish the selection indicated by the helper when the current UOW on
		/// the action handler completes, if the selection has been made invalid.
		/// </summary>
		public RequestSelectionByHelper(IActionHandlerExtensions hookup, IVwSelection sel, SelectionHelper helper)
		{
			m_hookup = hookup;
			m_helper = helper;
			m_sel = sel;
			if (helper != null)
			{
				m_hookup.DoAtEndOfPropChanged(m_hookup_PropChangedCompleted);
			}
		}

		/// <summary>
		/// Conditionally restores the selection and scroll position based on the selection
		/// helper.
		/// </summary>
		private void m_hookup_PropChangedCompleted()
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
		}
	}
}