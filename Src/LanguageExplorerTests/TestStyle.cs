// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests
{
	internal class TestStyle : BaseStyleInfo
	{
		public TestStyle(FontInfo defaultFontInfo, LcmCache cache)
			: base(cache)
		{
			m_defaultFontInfo = defaultFontInfo;
		}

		public TestStyle(InheritableStyleProp<BulletInfo> defaultBulletFontInfo, LcmCache cache)
			: base(cache)
		{
			m_bulletInfo = defaultBulletFontInfo;
		}

		public void SetWsStyle(FontInfo fontInfo, int wsId)
		{
			m_fontInfoOverrides[wsId] = fontInfo;
		}

		public void SetDefaultFontInfo(FontInfo info)
		{
			m_defaultFontInfo = info;
		}

		/// <summary>
		/// Sets the based on style and resets all properties to inherited.
		/// </summary>
		public void SetBasedOnStyle(BaseStyleInfo parent)
		{
			m_basedOnStyle = parent;
			SetAllPropertiesToInherited();
		}
	}
}