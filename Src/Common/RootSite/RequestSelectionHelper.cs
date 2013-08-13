// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2009' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RequestSelectionHelper.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using System.Runtime.InteropServices;

namespace SIL.FieldWorks.Common.RootSites
{
	#region RequestSelectionHelper
	/// <summary>
	/// This class is a helper for implementing the RequestSelectionAtEndOfUow method.
	/// </summary>
	public class RequestSelectionHelper
	{
		private readonly IActionHandlerExtensions m_hookup;
		private readonly IVwRootBox m_rootb;
		private readonly int m_ihvoRoot;
		private readonly SelLevInfo[] m_rgvsli;
		private readonly int m_tagTextProp;
		private readonly int m_cpropPrevious;
		private readonly int m_ich;
		private readonly int m_wsAlt;
		private readonly bool m_fAssocPrev;
		private readonly ITsTextProps m_selProps;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make one and hook it up to be called at the appropriate time.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public RequestSelectionHelper(IActionHandlerExtensions hookup, IVwRootBox rootb, int ihvoRoot,
			SelLevInfo[] rgvsli, int tagTextProp, int cpropPrevious, int ich, int wsAlt, bool fAssocPrev,
			ITsTextProps selProps)
		{
			m_hookup = hookup;
			m_rootb = rootb;
			m_ihvoRoot = ihvoRoot;
			m_rgvsli = rgvsli;
			m_tagTextProp = tagTextProp;
			m_cpropPrevious = cpropPrevious;
			m_ich = ich;
			m_wsAlt = wsAlt;
			m_fAssocPrev = fAssocPrev;
			m_selProps = selProps;
			m_hookup.DoAtEndOfPropChanged(m_hookup_PropChangedCompleted);
		}

		void m_hookup_PropChangedCompleted()
		{
			try
			{
				if (m_rootb.Site != null)
				{
					m_rootb.MakeTextSelection(m_ihvoRoot, m_rgvsli.Length, m_rgvsli, m_tagTextProp,
						m_cpropPrevious, m_ich, m_ich, m_wsAlt, m_fAssocPrev, -1, m_selProps, true);
				}
			}
			catch (COMException)
			{
				try
				{
					// Try again making the selection in a prompt
					m_rootb.MakeTextSelection(m_ihvoRoot, m_rgvsli.Length, m_rgvsli, SimpleRootSite.kTagUserPrompt,
						m_cpropPrevious, m_ich, m_ich, m_wsAlt, m_fAssocPrev, -1, m_selProps, true);
				}
				catch (COMException)
				{
					Debug.WriteLine("Failed to make a selection at end of UOW.");
					// Ignore any errors that happen when trying to set the selection. We really don't
					// want a program to crash if it fails.
				}
			}
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Another way to request a selection at the end of the UOW.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RequestSelectionByHelper
	{
		private readonly IActionHandlerExtensions m_hookup;
		private readonly SelectionHelper m_helper;
		private readonly IVwSelection m_sel;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make one to establish the selection indicated by the helper when the current UOW on
		/// the action handler completes.
		/// </summary>
		/// <param name="hookup">The hookup.</param>
		/// <param name="helper">The selection helper to use.</param>
		/// ------------------------------------------------------------------------------------
		public RequestSelectionByHelper(IActionHandlerExtensions hookup, SelectionHelper helper) :
			this(hookup, null, helper)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make one to establish the selection indicated by the helper when the current UOW on
		/// the action handler completes, if the selection has been made invalid.
		/// </summary>
		/// <param name="hookup">The hookup.</param>
		/// <param name="sel">The selection.</param>
		/// <param name="helper">The selection helper to use if the selection has been
		/// invalidated.</param>
		/// ------------------------------------------------------------------------------------
		public RequestSelectionByHelper(IActionHandlerExtensions hookup, IVwSelection sel, SelectionHelper helper)
		{
			m_hookup = hookup;
			m_helper = helper;
			m_sel = sel;
			if (helper != null)
				m_hookup.DoAtEndOfPropChanged(m_hookup_PropChangedCompleted);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Conditionally restores the selection and scroll position based on the selection
		/// helper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_hookup_PropChangedCompleted()
		{
			try
			{
				if (m_sel == null || !m_sel.IsValid)
					m_helper.RestoreSelectionAndScrollPos();
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
