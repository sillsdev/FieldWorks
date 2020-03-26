// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary/>
	internal sealed class ScriptListItem : IEquatable<ScriptListItem>
	{
		private readonly ScriptSubtag _script;

		/// <summary/>
		internal ScriptListItem(ScriptSubtag script)
		{
			_script = script;
		}

		/// <summary/>
		internal string Name => _script?.Name;

		/// <summary/>
		internal bool IsPrivateUse => _script != null && _script.IsPrivateUse;

		/// <summary/>
		internal string Code => _script?.Code;

		/// <summary/>
		internal string Label => _script == null ? "None" : $"{_script.Name} ({_script.Code})";

		/// <summary/>
		public bool Equals(ScriptListItem other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}
			if (ReferenceEquals(this, other))
			{
				return true;
			}
			return _script == null == (other._script == null) &&
				   (_script == null && other._script == null ||
					_script.IsPrivateUse == other.IsPrivateUse &&
					_script.Code == other.Code &&
					_script.Name == other.Name);
		}

		/// <summary/>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}
			if (ReferenceEquals(this, obj))
			{
				return true;
			}
			return obj.GetType() == GetType() && Equals((ScriptListItem)obj);
		}

		/// <summary/>
		public override int GetHashCode()
		{
			return _script != null ? _script.GetHashCode() : 0;
		}

		/// <summary>Allow cast of a ScriptListItem to a ScriptSubtag</summary>
		public static implicit operator ScriptSubtag(ScriptListItem item)
		{
			return item?._script;
		}
	}
}