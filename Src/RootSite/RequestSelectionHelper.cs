// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
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

		/// <summary>
		/// Make one and hook it up to be called at the appropriate time.
		/// </summary>
		public RequestSelectionHelper(IActionHandlerExtensions hookup, IVwRootBox rootb, int ihvoRoot, SelLevInfo[] rgvsli, int tagTextProp, int cpropPrevious, int ich, int wsAlt, bool fAssocPrev, ITsTextProps selProps)
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
					m_rootb.MakeTextSelection(m_ihvoRoot, m_rgvsli.Length, m_rgvsli, m_tagTextProp, m_cpropPrevious, m_ich, m_ich, m_wsAlt, m_fAssocPrev, -1, m_selProps, true);
				}
			}
			catch (COMException)
			{
				try
				{
					// Try again making the selection in a prompt
					m_rootb.MakeTextSelection(m_ihvoRoot, m_rgvsli.Length, m_rgvsli, SimpleRootSite.kTagUserPrompt, m_cpropPrevious, m_ich, m_ich, m_wsAlt, m_fAssocPrev, -1, m_selProps, true);
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
}