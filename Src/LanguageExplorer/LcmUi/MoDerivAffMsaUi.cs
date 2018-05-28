// Copyright (c) 2004-2018 SIL International
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
	public class MoDerivAffMsaUi : MoMorphSynAnalysisUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		public MoDerivAffMsaUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IMoDerivAffMsa);
		}

		internal MoDerivAffMsaUi()
		{
		}

		/// <summary>
		/// gives the hvo of the object to use in the URL we construct when doing a jump
		/// </summary>
		public override Guid GuidForJumping(object commandObject)
		{
			//todo: for now, this will just always send us to the "from" part of speech
			//	we could get at the "to" part of speech using a separate menu command
			//	or else, if this ends up being drawn by a view constructor rather than a string which combines both are from and the to,
			// then we will know which item of the user clicked on and can open the appropriate one.
			var msa = (IMoDerivAffMsa)MyCmObject;
			return msa.FromPartOfSpeechRA?.Guid ?? Guid.Empty;
		}
	}
}