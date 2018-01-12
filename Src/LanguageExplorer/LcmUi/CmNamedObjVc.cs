// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Special VC for classes that have name flid. It is a MultiString property, and the default user
	/// WS should be used to display it.
	/// </summary>
	public class CmNamedObjVc : CmObjectVc
	{
		protected int m_flidName;

		public CmNamedObjVc(LcmCache cache, int flidName)
			: base(cache)
		{
			m_flidName = flidName;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			int wsUi = vwenv.DataAccess.WritingSystemFactory.UserWs;
			vwenv.AddStringAltMember(m_flidName, wsUi, this);
		}
	}
}