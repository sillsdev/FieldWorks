// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using LanguageExplorer.Controls.LexText;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// UI functions for WfiGloss.
	/// </summary>
	public class WfiGlossUi : CmObjectUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="obj"></param>
		public WfiGlossUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IWfiGloss);
		}

		internal WfiGlossUi()
		{ }

		protected override bool ShouldDisplayMenuForClass(int specifiedClsid)
		{
			return WfiGlossTags.kClassId == specifiedClsid;
		}

		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out string guiControl, out string helpTopic)
		{
			wp.m_title = LcmUiStrings.ksMergeWordGloss;
			wp.m_label = LcmUiStrings.ksGlosses;

			var anal = (IWfiAnalysis) Object.Owner;
			ITsString tss;
			int nVar;
			int ws;
			foreach (var gloss in anal.MeaningsOC)
			{
				if (gloss.Hvo != Object.Hvo)
				{
					tss = gloss.ShortNameTSS;
					ws = tss.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
					mergeCandidates.Add(
						new DummyCmObject(
							gloss.Hvo,
							tss.Text,
							ws));
				}
			}

			guiControl = "MergeWordGlossList";
			helpTopic = "khtpMergeWordGloss";

			var me = (IWfiGloss) Object;
			tss = me.ShortNameTSS;
			ws = tss.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
			return new DummyCmObject(m_hvo, tss.Text, ws);
		}
	}
}