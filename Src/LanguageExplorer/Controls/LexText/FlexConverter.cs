// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Sfm2Xml;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.LexText
{
	internal class FlexConverter : Converter
	{
		private LcmCache m_cache;
		private int m_wsEn;

		public FlexConverter(LcmCache cache)
			: base()
		{
			m_cache = cache;
			m_wsEn = m_cache.WritingSystemFactory.GetWsFromStr("en");
		}

		protected override string GetMorphTypeInfo(ref string sForm, out string sAlloClass, out string sMorphTypeWs)
		{
			var clsid = MoStemAllomorphTags.kClassId;
			var mmt = sForm.Length > 0 ? MorphServices.FindMorphType(m_cache, ref sForm, out clsid) : m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
			sAlloClass = m_cache.DomainDataByFlid.MetaDataCache.GetClassName(clsid);
			int ws;
			var tss = mmt.Name.GetAlternativeOrBestTss(m_wsEn, out ws);
			sMorphTypeWs = ws == m_wsEn ? "en" : m_cache.WritingSystemFactory.GetStrFromWs(ws);
			return tss.Text;

		}
	}
}