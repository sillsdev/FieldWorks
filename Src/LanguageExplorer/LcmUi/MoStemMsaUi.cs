// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Special UI behaviors for the MoStemMsa class.
	/// </summary>
	public class MoStemMsaUi : MoMorphSynAnalysisUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		public MoStemMsaUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IMoStemMsa);
		}

		internal MoStemMsaUi()
		{
		}

		/// <summary>
		/// gives the hvo of the object to use in the URL reconstruct when doing a jump
		/// </summary>
		public override Guid GuidForJumping(object commandObject)
		{
			return ((IMoStemMsa)Object).PartOfSpeechRA?.Guid ?? Guid.Empty;
		}
	}
}