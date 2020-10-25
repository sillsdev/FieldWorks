// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Special VC for classes that should display in the default analysis writing system.
	/// </summary>
	internal class CmAnalObjectVc : FwBaseVc
	{
		internal CmAnalObjectVc(LcmCache cache)
			: base(cache.DefaultAnalWs)
		{
			Cache = cache;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			if (hvo == 0)
			{
				return;
			}
			var wsAnal = DefaultWs;
			ICmObject co;
			switch (frag)
			{
				case (int)VcFrags.kfragHeadWord:
					co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
					vwenv.AddString(co is ILexEntry lexEntry ? lexEntry.HeadWord : TsStringUtils.MakeString(co.ShortName, wsAnal));
					break;
				case (int)VcFrags.kfragShortName:
					co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
					vwenv.AddString(TsStringUtils.MakeString(co.ShortName, wsAnal));
					break;
				case (int)VcFrags.kfragPosAbbrAnalysis:
					vwenv.AddStringAltMember(CmPossibilityTags.kflidAbbreviation, wsAnal, this);
					break;
				default:
					co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
					vwenv.AddString(TsStringUtils.MakeString(co.ToString(), wsAnal));
					break;
			}
		}
	}
}