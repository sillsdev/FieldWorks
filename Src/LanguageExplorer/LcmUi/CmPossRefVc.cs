// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Special VC for classes that have a reference to a CmPossibility whose name/abbr should be
	/// used as the name/abbr for this.
	/// </summary>
	public class CmPossRefVc : CmObjectVc
	{
		protected int m_flidRef; // flid that refers to the CmPossibility

		public CmPossRefVc(LcmCache cache, int flidRef)
			: base(cache)
		{
			m_flidRef = flidRef;
		}

		/// <summary>
		/// If the expected reference property is null, insert "??" and return false;
		/// otherwise return true.
		/// </summary>
		private bool HandleObjMissing(IVwEnv vwenv, int hvo)
		{
			if (m_cache.DomainDataByFlid.get_ObjectProp(hvo, m_flidRef) != 0)
			{
				return true;
			}
			var wsUi = vwenv.DataAccess.WritingSystemFactory.UserWs;
			vwenv.AddString(TsStringUtils.MakeString(LcmUiStrings.ksQuestions, wsUi));  // was "??", not "???"
			vwenv.NoteDependency(new[] { hvo }, new[] { m_flidRef }, 1);
			return false;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			const int kfragAbbr = 17; // arbitrary const from reserved range.
			const int kfragName = 18;
			switch (frag)
			{
				case (int)VcFrags.kfragInterlinearAbbr:
				case (int)VcFrags.kfragInterlinearName:  // abbr is probably more appropriate in interlinear.
				case (int)VcFrags.kfragShortName:
					if (HandleObjMissing(vwenv, hvo))
						vwenv.AddObjProp(m_flidRef, this, kfragAbbr);
					break;
				case kfragAbbr:
					vwenv.AddStringAltMember(CmPossibilityTags.kflidAbbreviation,
						m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle,
						this);
					break;
				case (int)VcFrags.kfragName:
					if (HandleObjMissing(vwenv, hvo))
						vwenv.AddObjProp(m_flidRef, this, kfragName);
					break;
				default:
					vwenv.AddStringAltMember(CmPossibilityTags.kflidName,
						m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle,
						this);
					break;
			}
		}
	}
}