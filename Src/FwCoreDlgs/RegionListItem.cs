// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary/>
	internal sealed class RegionListItem : IEquatable<RegionListItem>
	{
		private readonly RegionSubtag _region;

		/// <summary/>
		internal RegionListItem(RegionSubtag region)
		{
			_region = region;
		}

		/// <summary/>
		internal string Name => _region?.Name;

		/// <summary/>
		internal bool IsPrivateUse => _region != null && _region.IsPrivateUse;

		/// <summary/>
		internal string Code => _region?.Code;

		/// <summary/>
		internal string Label => _region == null ? "None" : $"{_region.Name} ({_region.Code})";

		/// <summary>Allow cast of a RegionListItem to a RegionSubtag</summary>
		public static implicit operator RegionSubtag(RegionListItem item)
		{
			return item?._region;
		}

		/// <summary/>
		public bool Equals(RegionListItem other)
		{
			return !ReferenceEquals(null, other) && (ReferenceEquals(this, other) || Equals(_region, other._region));
		}

		/// <summary/>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((RegionListItem)obj);
		}

		/// <summary/>
		public override int GetHashCode()
		{
			return _region != null ? _region.GetHashCode() : 0;
		}
	}
}