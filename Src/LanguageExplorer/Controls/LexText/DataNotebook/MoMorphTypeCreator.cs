// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	/// <summary>
	/// Creator wrapping IMoMorphTypeFactory
	/// </summary>
	public class MoMorphTypeCreator : CmPossibilityCreator
	{
		private IMoMorphTypeFactory m_fact;
		public MoMorphTypeCreator(IMoMorphTypeFactory fact)
		{
			m_fact = fact;
		}
		public override ICmPossibility Create()
		{
			return m_fact.Create();
		}
	}
}