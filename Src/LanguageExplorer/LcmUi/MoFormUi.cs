// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// UI functions for MoMorphSynAnalysis.
	/// </summary>
	public class MoFormUi : CmObjectUi
	{
		internal MoFormUi() { }

		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out XElement guiControlParameters, out string helpTopic)
		{
			wp.m_title = LcmUiStrings.ksMergeAllomorph;
			wp.m_label = LcmUiStrings.ksAlternateForms;
			var defVernWs = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			var le = (ILexEntry)MyCmObject.Owner;
			foreach (var allo in le.AlternateFormsOS)
			{
				if (allo.Hvo != MyCmObject.Hvo && allo.ClassID == MyCmObject.ClassID)
				{
					mergeCandidates.Add(new DummyCmObject(allo.Hvo, allo.Form.VernacularDefaultWritingSystem.Text, defVernWs));
				}
			}
			if (le.LexemeFormOA.ClassID == MyCmObject.ClassID)
			{
				// Add the lexeme form.
				mergeCandidates.Add(new DummyCmObject(le.LexemeFormOA.Hvo, le.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text, defVernWs));
			}
			guiControlParameters = XElement.Parse(LcmUiStrings.MergeAllomorphListParameters);
			helpTopic = "khtpMergeAllomorph";
			return new DummyCmObject(m_hvo, ((IMoForm)MyCmObject).Form.VernacularDefaultWritingSystem.Text, defVernWs);
		}
	}
}