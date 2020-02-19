// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary/>
	public class SpellingDictionaryItem : Tuple<string, string>, IEquatable<SpellingDictionaryItem>
	{
		/// <summary/>
		public SpellingDictionaryItem(string item1, string item2) : base(item1, item2)
		{
		}

		/// <summary/>
		public string Name => Item1;

		/// <summary/>
		public string Id => Item2;

		/// <summary/>
		public override string ToString()
		{
			return Name;
		}

		/// <summary/>
		public bool Equals(SpellingDictionaryItem other)
		{
			return Id.Equals(other?.Id);
		}

		/// <summary/>
		public override bool Equals(object obj)
		{
			return !ReferenceEquals(null, obj) &&
				   (ReferenceEquals(this, obj) || obj.GetType() == GetType() &&
					Equals((SpellingDictionaryItem)obj));
		}

		/// <summary/>
		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}
}