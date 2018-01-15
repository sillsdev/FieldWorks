// Copyright (c) 201502018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Subclass for LexDb.AllPossibleAllomorphs.
	/// </summary>
	internal class GphAllPossibleAllomorphs : GhostParentHelper
	{
		internal GphAllPossibleAllomorphs(ILcmServiceLocator services, int parentClsid, int flidOwning)
			: base(services, parentClsid, flidOwning)
		{
		}

		/// <summary>
		/// In the case of AllPossibleAllomorphs, the class to create is determined by the owning entry.
		/// </summary>
		internal override int ClassToCreate(int hvoItem, int flidBasicProp)
		{
			var entry = m_services.GetObject(hvoItem) as ILexEntry;
			return entry.GetDefaultClassForNewAllomorph();
		}
	}
}