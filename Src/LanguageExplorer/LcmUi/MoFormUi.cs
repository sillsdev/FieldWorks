// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		/// <summary />
		public MoFormUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IMoForm);
		}

		internal MoFormUi() { }

		/// <summary>
		/// gives the hvo of the object to use in the URL we construct when doing a jump
		/// </summary>
		public override Guid GuidForJumping(object commandObject)
		{
#if RANDYTODO
			var cmd = (Command) commandObject;
			string className = XmlUtils.GetMandatoryAttributeValue(cmd.Parameters[0], "className");
			if (className == "LexEntry")
			{
				ICmObject cmo = GetSelfOrParentOfClass(Object, LexEntryTags.kClassId);
				return (cmo == null) ? Guid.Empty : cmo.Guid;
			}
#endif
			return MyCmObject.Guid;
		}

#if RANDYTODO
		protected override bool ShouldDisplayMenuForClass(int specifiedClsid, UIItemDisplayProperties display)
		{
			if (LexEntryTags.kClassId == specifiedClsid)
				return true;
			return DomainObjectServices.IsSameOrSubclassOf(m_cache.DomainDataByFlid.MetaDataCache, Object.ClassID, specifiedClsid);
		}
#endif

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