// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// a data record class for an occurrence of a spelling change
	/// </summary>
	internal class RespellOccurrence
	{
#pragma warning disable 0414
		int m_hvoBa; // Hvo of CmBaseAnnotation representing occurrence
		// position in the UNMODIFIED input string. (Note that it may not start at this position
		// in the output string, since there may be prior occurrences.) Initially equal to
		// m_hvoBa.BeginOffset; but that may get changed if we are undone.
		int m_ich;
#pragma warning restore 0414

		public RespellOccurrence(int hvoBa, int ich)
		{
			m_hvoBa = hvoBa;
			m_ich = ich;
		}
	}
}