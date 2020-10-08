// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// UI functions for WfiGloss.
	/// </summary>
	internal sealed class WfiGlossUi : CmObjectUi
	{
		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out XElement guiControlParameters, out string helpTopic)
		{
			wp.m_title = LcmUiResources.ksMergeWordGloss;
			wp.m_label = LcmUiResources.ksGlosses;
			var anal = (IWfiAnalysis)MyCmObject.Owner;
			ITsString tss;
			int ws;
			foreach (var gloss in anal.MeaningsOC)
			{
				if (gloss.Hvo == MyCmObject.Hvo)
				{
					continue;
				}
				tss = gloss.ShortNameTSS;
				ws = tss.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out _);
				mergeCandidates.Add(new DummyCmObject(gloss.Hvo, tss.Text, ws));
			}
			guiControlParameters = XElement.Parse(LcmUiResources.MergeWordGlossListParameters);
			helpTopic = "khtpMergeWordGloss";
			var me = (IWfiGloss)MyCmObject;
			tss = me.ShortNameTSS;
			ws = tss.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out _);
			return new DummyCmObject(m_hvo, tss.Text, ws);
		}
	}
}