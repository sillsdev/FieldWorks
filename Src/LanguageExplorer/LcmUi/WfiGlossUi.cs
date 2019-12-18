// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// UI functions for WfiGloss.
	/// </summary>
	public class WfiGlossUi : CmObjectUi
	{
		/// <summary />
		public WfiGlossUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IWfiGloss);
		}

		internal WfiGlossUi()
		{ }

		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out XElement guiControlParameters, out string helpTopic)
		{
			wp.m_title = LcmUiStrings.ksMergeWordGloss;
			wp.m_label = LcmUiStrings.ksGlosses;
			var anal = (IWfiAnalysis)MyCmObject.Owner;
			ITsString tss;
			int nVar;
			int ws;
			foreach (var gloss in anal.MeaningsOC)
			{
				if (gloss.Hvo == MyCmObject.Hvo)
				{
					continue;
				}
				tss = gloss.ShortNameTSS;
				ws = tss.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
				mergeCandidates.Add(new DummyCmObject(gloss.Hvo, tss.Text, ws));
			}
			guiControlParameters = XElement.Parse(LcmUiStrings.MergeWordGlossListParameters);
			helpTopic = "khtpMergeWordGloss";
			var me = (IWfiGloss)MyCmObject;
			tss = me.ShortNameTSS;
			ws = tss.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
			return new DummyCmObject(m_hvo, tss.Text, ws);
		}
	}
}