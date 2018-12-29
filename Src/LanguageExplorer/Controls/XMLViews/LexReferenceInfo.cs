// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Store information needed for knowing how to handle a particular type of LexReference.
	/// </summary>
	public class LexReferenceInfo : ItemTypeInfo
	{
		/// <summary>Flag how the LexRefType is used.</summary>
		public TypeSubClass SubClass { get; set; }

		/// <summary />
		public LexReferenceInfo(bool fEnabled, Guid guid)
			: base(fEnabled, guid)
		{
		}

		/// <summary />
		public LexReferenceInfo(string s)
		{
			Enabled = s.StartsWith("+");
			if (s.EndsWith(":f"))
			{
				SubClass = TypeSubClass.Forward;
				s = s.Remove(s.Length - 2);
			}
			else if (s.EndsWith(":r"))
			{
				SubClass = TypeSubClass.Reverse;
				s = s.Remove(s.Length - 2);
			}
			else
			{
				SubClass = TypeSubClass.Normal;
			}
			ItemGuid = new Guid(s.Substring(1));
		}

		/// <summary>
		/// Get the string representation used in the part ref attributes.
		/// </summary>
		// REVIEW (Hasso) 2014.05: The only places StorageString is used are in the Configuration Dialogs
		// (DictionaryDetailsController and XmlDocConfigureDlg). Since the newer dialog stores whether an item is enabled as its own property,
		// we may soon no longer need the leading + or -. (At which point we may no longer need this class)
		public override string StorageString => $"{(Enabled ? "+" : "-")}{ItemGuid}{SubClassAsString}";

		private string SubClassAsString
		{
			get
			{
				switch (SubClass)
				{
					case TypeSubClass.Forward: return ":f";
					case TypeSubClass.Reverse: return ":r";
					default: return string.Empty;
				}
			}
		}

		/// <summary>
		/// Create a list of these objects from a string representation.
		/// </summary>
		public new static List<LexReferenceInfo> CreateListFromStorageString(string sTypeseq)
		{
			var list = new List<LexReferenceInfo>();
			if (!string.IsNullOrEmpty(sTypeseq))
			{
				var rgsGuids = sTypeseq.Split(',');
				list.AddRange(rgsGuids.Select(t => new LexReferenceInfo(t)));
			}
			return list;
		}
	}
}