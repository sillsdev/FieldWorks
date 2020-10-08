// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Special UI behaviors for the MoMorphSynAnalysis class.
	/// </summary>
	internal sealed class MoMorphSynAnalysisUi : CmObjectUi
	{
		/// <summary>
		/// Gets a special VC that knows to display the name or abbr of the PartOfSpeech.
		/// </summary>
		internal override IVwViewConstructor Vc
		{
			get
			{
				if (m_vc == null)
				{
					m_vc = new MsaVc(m_cache);
				}
				return base.Vc;
			}
		}
	}
}