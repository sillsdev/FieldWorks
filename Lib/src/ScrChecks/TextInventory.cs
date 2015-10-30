// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace SILUBS.ScriptureChecks
{
	/// <summary>
	/// We keep inventories of various kinds of items in a text, e.g. characters,
	/// repeated words, etc. For each item we store its textual form, the books
	/// it occurs in (so that we can reasonably quickly go refind it), and the
	/// number of times it occurs.
	/// </summary>
	public class TextInventoryItem
	{
		public enum ItemStatus { unknown, good, bad };

		int count = 0;

		// This is a list of book numbers in ascending order with no duplicates allowed.
		// We keep a list of book numbers in order to make it faster to go back and search
		// for occurrences  of this item in the project.
		List<int> references = new List<int>();

		public string Text;

		public ItemStatus Status = ItemStatus.unknown; // 'y', 'n', '?'

		public void AddReference(int bookNum)
		{
			count = count + 1;
			if (references.Count == 0 || references[references.Count - 1] != bookNum)
			{
				// make sure the list is in ascending order
				Debug.Assert(references.Count == 0 ||
					bookNum > references[references.Count - 1]);
				references.Add(bookNum);
			}
		}

		public void AddReference(int bookNum, int Count)
		{
			int i = references.BinarySearch(bookNum);
			if (i < 0)
				references.Insert(~i, bookNum);
			count = count + Count;
		}

		public int Count { get { return count; } }

		public List<int> References
		{
			get { return references; }
		}

		public string Books
		{
			get
			{
				string books = "";

				foreach (int bookNum in references)
				{
					while (books.Length < bookNum - 1)
						books += "0";
					books += "1";
				}

				return books;
			}
		}
	}

	/// <summary>
	/// A dictionary containing instances of tetual items indexed by their
	/// text. Example: the repeated key "the".
	/// </summary>
	public class TextInventory : Dictionary<string, TextInventoryItem>
	{
		public TextInventoryItem GetValue(string key)
		{
			TextInventoryItem item;

			if (!this.TryGetValue(key, out item))
			{
				item = new TextInventoryItem();
				item.Text = key;
				this[key] = item;
			}

			return item;
		}
	}

	//void InventoryBook(TextAnalyzer tkb)
	//{
	//    GetErrors(tkb);

	//    foreach (TextToken tok in repeatedWords)
	//    {
	//        inventory.AddReference(tok.ToString(), tkb.BookNumber);
	//    }
	//}

}
