// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Store information needed for knowing how to handle a particular type of something.
	/// </summary>
	public class ItemTypeInfo
	{
		/// <summary>Guid of the type object.  (This is probably a CmPossibility or subclass thereof.)</summary>
		public Guid ItemGuid { get; protected set; }

		/// <summary>Flag whether the given LexRefType is enabled for display.</summary>
		public bool Enabled { get; set; }

		/// <summary>Display name of the type object.</summary>
		public string Name { get; set; }

		/// <summary>Index of this item in the list of enabled items.</summary>
		public int Index { get; set; }

		/// <summary>
		/// Override for use in list view.
		/// </summary>
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Constructor needed for subclass.
		/// </summary>
		public ItemTypeInfo()
		{
		}

		/// <summary />
		public ItemTypeInfo(bool fEnabled, Guid guid)
		{
			Enabled = fEnabled;
			ItemGuid = guid;
		}

		/// <summary />
		public ItemTypeInfo(string s)
		{
			Enabled = s.StartsWith("+");
			ItemGuid = new Guid(s.Substring(1));
		}

		/// <summary>
		/// Get the string representation used in the part ref attributes.
		/// </summary>
		public virtual string StorageString => $"{(Enabled ? "+" : "-")}{ItemGuid}";

		/// <summary>
		/// Create a list of these objects from a string representation.
		/// </summary>
		public static List<ItemTypeInfo> CreateListFromStorageString(string sTypeseq)
		{
			var list = new List<ItemTypeInfo>();
			if (!string.IsNullOrEmpty(sTypeseq))
			{
				var rgsTypes = sTypeseq.Split(',');
				list.AddRange(rgsTypes.Select(t => new ItemTypeInfo(t)));
			}
			return list;
		}
	}
}