// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.Code;

namespace LanguageExplorer
{
	/// <summary>
	/// ISeparatorToolStripBundle implementation for separator that has preceding tool bar UI widgets (may be to the start of the tool bar), and following (to the next separator, or to the end).
	/// </summary>
	internal sealed class SeparatorToolStripBundle : ISeparatorToolStripBundle
	{
		private readonly ToolStripSeparator _separator;
		private readonly ISeparatorToolStripBundle _precedingBundle;
		private readonly List<ToolStripItem> _followingItems;

		internal SeparatorToolStripBundle(ToolStripSeparator separator, ISeparatorToolStripBundle precedingBundle, List<ToolStripItem> followingItems)
		{
			Guard.AgainstNull(separator, nameof(separator));
			Guard.AgainstNull(followingItems, nameof(followingItems));
			Require.That(followingItems.Any());

			_separator = separator;
			_precedingBundle = precedingBundle;
			_followingItems = followingItems;
		}

		#region Implementation of ISeparatorToolStripBundle
		/// <inheritdoc />
		ToolStripSeparator ISeparatorToolStripBundle.Separator => _separator;

		/// <inheritdoc />
		List<ToolStripItem> ISeparatorToolStripBundle.PrecedingItems
		{
			get
			{
				var retVal = new List<ToolStripItem>();
				if (!_separator.Visible)
				{
					retVal.AddRange(_precedingBundle.PrecedingItems);
				}
				retVal.AddRange(_precedingBundle.FollowingItems);
				return retVal;
			}
		}

		/// <inheritdoc />
		List<ToolStripItem> ISeparatorToolStripBundle.FollowingItems => _followingItems;
		#endregion
	}
}