// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	/// <summary>
	/// This class defines an encapsulation of factories for ICmPossibility and its
	/// subclasses.  This allows some a sizable chunk of code to be written only once.
	/// </summary>
	public class CmPossibilityCreator
	{
		private ICmPossibilityFactory m_fact;
		public CmPossibilityCreator()
		{
		}
		public CmPossibilityCreator(ICmPossibilityFactory fact)
		{
			m_fact = fact;
		}
		public virtual ICmPossibility Create()
		{
			return m_fact.Create();
		}
	}
}