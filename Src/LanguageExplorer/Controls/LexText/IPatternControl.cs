// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;

namespace LanguageExplorer.Controls.LexText
{
	public interface IPatternControl
	{
		/// <summary>
		/// Gets the currently selected context.
		/// </summary>
		object GetContext(SelectionHelper sel);

		/// <summary>
		/// Gets the currently selected context.
		/// </summary>
		object GetContext(SelectionHelper sel, SelectionHelper.SelLimitType limit);

		/// <summary>
		/// Gets the currently selected item.
		/// </summary>
		object GetItem(SelectionHelper sel, SelectionHelper.SelLimitType limit);

		/// <summary>
		/// Gets the index of an item in the specified context.
		/// </summary>
		int GetItemContextIndex(object ctxt, object obj);

		/// <summary>
		/// Gets the level information for selection purposes of the item at the specified
		/// index in the specified context.
		/// </summary>
		SelLevInfo[] GetLevelInfo(object ctxt, int index);

		/// <summary>
		/// Gets the number of items in the specified context.
		/// </summary>
		int GetContextCount(object ctxt);

		/// <summary>
		/// Gets the next context from the specified context.
		/// </summary>
		object GetNextContext(object ctxt);

		/// <summary>
		/// Gets the previous context from the specified context.
		/// </summary>
		object GetPrevContext(object ctxt);

		/// <summary>
		/// Gets the flid associated with the specified context. It is used for selection purposes.
		/// </summary>
		int GetFlid(object ctxt);
	}
}
