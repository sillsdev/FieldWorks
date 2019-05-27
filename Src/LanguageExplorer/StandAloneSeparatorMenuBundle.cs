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
	/// ISeparatorMenuBundle implementation for first (or only) separator in a menu.
	/// </summary>
	internal sealed class StandAloneSeparatorMenuBundle : ISeparatorMenuBundle
	{
		private readonly ToolStripSeparator _separator;
		private readonly List<ToolStripMenuItem> _precedingItems;
		private readonly List<ToolStripMenuItem> _followingItems;

		internal StandAloneSeparatorMenuBundle(ToolStripSeparator separator, List<ToolStripMenuItem> precedingItems, List<ToolStripMenuItem> followingItems)
		{
			Guard.AgainstNull(separator, nameof(separator));
			Guard.AgainstNull(precedingItems, nameof(precedingItems));
			Guard.AgainstNull(followingItems, nameof(followingItems));
			Require.That(followingItems.Any());

			_separator = separator;
			_precedingItems = precedingItems;
			_followingItems = followingItems;
		}

		#region Implementation of ISeparatorMenuBundle
		/// <inheritdoc />
		ToolStripSeparator ISeparatorMenuBundle.Separator => _separator;

		/// <inheritdoc />
		List<ToolStripMenuItem> ISeparatorMenuBundle.PrecedingItems => _precedingItems;

		/// <inheritdoc />
		List<ToolStripMenuItem> ISeparatorMenuBundle.FollowingItems => _followingItems;
		#endregion
	}
}