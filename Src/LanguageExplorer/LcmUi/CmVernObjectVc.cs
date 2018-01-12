// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Special VC for classes that should display in the default vernacular writing system.
	/// </summary>
	public class CmVernObjectVc : FwBaseVc
	{
		public CmVernObjectVc(LcmCache cache)
		{
			Cache = cache;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			int wsVern = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			var co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			switch (frag)
			{
				case (int)VcFrags.kfragHeadWord:
					var le = co as ILexEntry;
					vwenv.AddString(le != null ? le.HeadWord : TsStringUtils.MakeString(co.ShortName, wsVern));
					break;
				case (int)VcFrags.kfragShortName:
					vwenv.AddString(TsStringUtils.MakeString(co.ShortName, wsVern));
					break;
				default:
					vwenv.AddString(TsStringUtils.MakeString(co.ToString(), wsVern));
					break;
			}
		}
	}
}