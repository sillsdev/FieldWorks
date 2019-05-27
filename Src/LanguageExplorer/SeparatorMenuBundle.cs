// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.Code;

namespace LanguageExplorer
{
	/// <summary>
	/// ISeparatorMenuBundle implementation for separator that has preceding menus (may be to the start of the main menu), and following (to the next separator, or to the end).
	/// </summary>
	internal sealed class SeparatorMenuBundle : ISeparatorMenuBundle
	{
		private readonly ToolStripSeparator _separator;
		private readonly ISeparatorMenuBundle _precedingBundle;
		private readonly List<ToolStripMenuItem> _followingItems;

		internal SeparatorMenuBundle(ToolStripSeparator separator, ISeparatorMenuBundle precedingBundle, List<ToolStripMenuItem> followingItems)
		{
			Guard.AgainstNull(separator, nameof(separator));
			Guard.AgainstNull(followingItems, nameof(followingItems));
			Require.That(followingItems.Any());

			_separator = separator;
			_precedingBundle = precedingBundle;
			_followingItems = followingItems;
		}

		#region Implementation of ISeparatorMenuBundle
		/// <inheritdoc />
		ToolStripSeparator ISeparatorMenuBundle.Separator => _separator;

		/// <inheritdoc />
		List<ToolStripMenuItem> ISeparatorMenuBundle.PrecedingItems
		{
			get
			{
				var retVal = new List<ToolStripMenuItem>();
				if (!_separator.Visible)
				{
					retVal.AddRange(_precedingBundle.PrecedingItems);
				}
				retVal.AddRange(_precedingBundle.FollowingItems);
				return retVal;
			}
		}

		/// <inheritdoc />
		List<ToolStripMenuItem> ISeparatorMenuBundle.FollowingItems => _followingItems;
		#endregion
	}
}