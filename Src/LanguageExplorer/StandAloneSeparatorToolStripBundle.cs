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
	/// ISeparatorToolStripBundle implementation for first (or only) separator in a tool bar.
	/// </summary>
	internal sealed class StandAloneSeparatorToolStripBundle : ISeparatorToolStripBundle
	{
		private readonly ToolStripSeparator _separator;
		private readonly List<ToolStripItem> _precedingItems;
		private readonly List<ToolStripItem> _followingItems;

		internal StandAloneSeparatorToolStripBundle(ToolStripSeparator separator, List<ToolStripItem> precedingItems, List<ToolStripItem> followingItems)
		{
			Guard.AgainstNull(separator, nameof(separator));
			Guard.AgainstNull(precedingItems, nameof(precedingItems));
			Guard.AgainstNull(followingItems, nameof(followingItems));
			Require.That(followingItems.Any());

			_separator = separator;
			_precedingItems = precedingItems;
			_followingItems = followingItems;
		}

		#region Implementation of ISeparatorToolStripBundle
		/// <inheritdoc />
		ToolStripSeparator ISeparatorToolStripBundle.Separator => _separator;

		/// <inheritdoc />
		List<ToolStripItem> ISeparatorToolStripBundle.PrecedingItems => _precedingItems;

		/// <inheritdoc />
		List<ToolStripItem> ISeparatorToolStripBundle.FollowingItems => _followingItems;
		#endregion
	}
}