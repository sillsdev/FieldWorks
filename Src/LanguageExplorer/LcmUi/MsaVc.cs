// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Special VC for MSAs. These have the InterlinearName method.
	/// Enhance JohnT: it would be better to actually build a view that shows what we want,
	/// so that all the proper dependencies could be noted.  But the algorithms are complex
	/// and involve backreferences.
	/// Todo: Finish reworking this into MsaVc; clean up stuff related to interlinearName
	/// above.
	/// </summary>
	public class MsaVc : CmAnalObjectVc
	{
		public MsaVc(LcmCache cache)
			: base(cache)
		{
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			var wsAnal = DefaultWs;
			var msa = m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().GetObject(hvo);
			switch (frag)
			{
				case (int)VcFrags.kfragFullMSAInterlinearname:
					// not editable
					vwenv.OpenParagraph();
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
					vwenv.AddString(msa.LongNameTs);
					vwenv.CloseParagraph();
					break;
				case (int)VcFrags.kfragInterlinearName:
					// not editable
					break;
				case (int)VcFrags.kfragInterlinearAbbr:
					// not editable
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
					vwenv.AddString(msa.InterlinAbbrTSS(wsAnal));
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}
	}
}