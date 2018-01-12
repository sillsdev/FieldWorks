// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	/// <summary>
	/// This class encapsulates an ICmPersonFactory to look like it's creating
	/// ICmPossibility objects.  This allows some a sizable chunk of code to be written
	/// only once.
	/// </summary>
	public class CmPersonCreator : CmPossibilityCreator
	{
		private ICmPersonFactory m_fact;
		public CmPersonCreator(ICmPersonFactory fact)
		{
			m_fact = fact;
		}
		public override ICmPossibility Create()
		{
			return m_fact.Create();
		}
	}
}