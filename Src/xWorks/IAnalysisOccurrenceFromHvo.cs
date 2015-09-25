// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This interface is implemented by ConcDecorator in LanguageExplorer, which is configured to be the
	/// SDA that the Clerk's VirtualListPublisher decorates. This allows the Clerk to make available
	/// the selected analysis occurrence, without introducing a (circular) dependency between xWorks and LanguageExplorer.
	/// </summary>
	public interface IAnalysisOccurrenceFromHvo
	{
		IParaFragment OccurrenceFromHvo(int hvo);
	}
}