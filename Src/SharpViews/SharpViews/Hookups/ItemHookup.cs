// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// An ItemHookup groups together the hookups for the various properties of an individual object,
	/// typically an item in an IndependentSequenceHookup. It may also track one or more boxes which
	/// belong entirely to this item.
	/// For example, a LexEntry might have one paragraph which holds most of its content (the text part),
	/// and another which holds its pictures. The ItemHookup would track the two paragraphs as well as
	/// the hookups which hold the various properties of the LexEntry. If the senses of the entry are
	/// displayed as a sequence of strings within the one paragraph, the ItemHookups for the senses would
	/// not track any box, just the individual string hookups (or possibly another layer of
	/// IndependentSequenceHookup holding example sentences, etc.).
	/// </summary>
	public class ItemHookup : GroupHookup, IItemsHookup
	{
		// tracks the first box entirely part of this item (may be null, if item consists of paragraph runs)
		public Box FirstBox { get; private set; }
		// tracks the last box entirely part of this item (may be null, if item consists of paragraph runs)
		public Box LastBox { get; private set; }

		/// <summary>
		/// In the parent sequence, an ItemHookup stands for the one item that is its target.
		/// </summary>
		public object[] ItemGroup
		{
			get { return new object[] { Target }; }
		}

		public ItemHookup(object target, GroupBox containingBox)
			: base(target, containingBox)
		{
		}

		internal void AddBox(Box box)
		{
			if (FirstBox == null)
				FirstBox = box;
			LastBox = box;
		}
	}
}
