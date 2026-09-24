// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// One authority over several: a menu id is answered by the first member that owns it.
	/// </summary>
	internal sealed class CompositeMenuAuthority : IDetailMenuAuthority
	{
		private readonly IDetailMenuAuthority[] _members;

		public CompositeMenuAuthority(params IDetailMenuAuthority[] members)
		{
			_members = members ?? throw new ArgumentNullException(nameof(members));
			if (Array.IndexOf(_members, null) >= 0)
				throw new ArgumentException("A member authority is null.", nameof(members));
		}

		public bool Owns(string menuId) => Owner(menuId) != null;

		public DetailMenuItem Build(string menuId, ChoiceBase leaf)
		{
			var owner = Owner(menuId);
			if (owner == null)
				throw new InvalidOperationException(string.Format("No authority owns menu '{0}'.", menuId));
			return owner.Build(menuId, leaf);
		}

		private IDetailMenuAuthority Owner(string menuId)
		{
			foreach (var member in _members)
			{
				if (member.Owns(menuId))
					return member;
			}
			return null;
		}
	}
}
