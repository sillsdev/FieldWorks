// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Cheapo version of the LCM MoForm object.
	/// </summary>
	internal class LMsa : LObject
	{
		private readonly string m_name;

		public LMsa(IMoMorphSynAnalysis msa) : base(msa.Hvo)
		{
			m_name = msa.InterlinearName;
		}

		public override string ToString()
		{
			return m_name ?? HVO.ToString();
		}
	}
}