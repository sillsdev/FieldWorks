// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Special UI behaviors for the MoMorphSynAnalysis class.
	/// </summary>
	public class MoMorphSynAnalysisUi : CmObjectUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		public MoMorphSynAnalysisUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IMoMorphSynAnalysis);
		}

		internal MoMorphSynAnalysisUi()
		{
		}

		protected override bool ShouldDisplayMenuForClass(int specifiedClsid)
		{
			return (PartOfSpeechTags.kClassId == specifiedClsid) && GuidForJumping(null) != Guid.Empty;
		}

		/// <summary>
		/// Gets a special VC that knows to display the name or abbr of the PartOfSpeech.
		/// </summary>
		public override IVwViewConstructor Vc
		{
			get
			{
				CheckDisposed();

				if (m_vc == null)
				{
					m_vc = new MsaVc(m_cache);
				}
				return base.Vc;
			}
		}
	}
}