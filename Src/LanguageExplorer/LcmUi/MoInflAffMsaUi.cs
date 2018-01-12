// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// UI functions for MoMorphSynAnalysis.
	/// </summary>
	public class MoInflAffMsaUi : MoMorphSynAnalysisUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="obj"></param>
		public MoInflAffMsaUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IMoInflAffMsa);
		}

		internal MoInflAffMsaUi()
		{
		}

		/// <summary>
		/// gives the hvo of the object to use in the URL we construct when doing a jump
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public override Guid GuidForJumping(object commandObject)
		{
			var msa = (IMoInflAffMsa) Object;
			if (msa.PartOfSpeechRA == null)
				return Guid.Empty;
			return msa.PartOfSpeechRA.Guid;
		}
	}
}