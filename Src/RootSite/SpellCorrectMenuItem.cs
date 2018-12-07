// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Menu item subclass containing the information needed to correct a spelling error.
	/// </summary>
	public class SpellCorrectMenuItem : ToolStripMenuItem
	{
		private readonly IVwRootBox m_rootb;
		private readonly int m_hvoObj;
		private readonly int m_tag;
		private readonly int m_wsAlt; // 0 if not multilingual--not yet implemented.
		private readonly int m_ichMin; // where to make the change.
		private readonly int m_ichLim; // end of string to replace
		private readonly ITsString m_tssReplacement;

		/// <summary />
		public SpellCorrectMenuItem(IVwRootBox rootb, int hvoObj, int tag, int wsAlt, int ichMin, int ichLim, string text, ITsString tss)
			: base(text)
		{
			m_rootb = rootb;
			m_hvoObj = hvoObj;
			m_tag = tag;
			m_wsAlt = wsAlt;
			m_ichMin = ichMin;
			m_ichLim = ichLim;
			m_tssReplacement = tss;
		}

		/// <summary />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			base.Dispose(disposing);
		}

		/// <summary />
		public void DoIt()
		{
			m_rootb.DataAccess.BeginUndoTask(RootSiteStrings.ksUndoCorrectSpelling, RootSiteStrings.ksRedoSpellingChange);
			var tssInput = m_wsAlt == 0 ? m_rootb.DataAccess.get_StringProp(m_hvoObj, m_tag) : m_rootb.DataAccess.get_MultiStringAlt(m_hvoObj, m_tag, m_wsAlt);
			var bldr = tssInput.GetBldr();
			bldr.ReplaceTsString(m_ichMin, m_ichLim, m_tssReplacement);
			if (m_wsAlt == 0)
			{
				m_rootb.DataAccess.SetString(m_hvoObj, m_tag, bldr.GetString());
			}
			else
			{
				m_rootb.DataAccess.SetMultiStringAlt(m_hvoObj, m_tag, m_wsAlt, bldr.GetString());
			}
			m_rootb.PropChanged(m_hvoObj, m_tag, m_wsAlt, 1, 1);
			m_rootb.DataAccess.EndUndoTask();
		}
	}
}