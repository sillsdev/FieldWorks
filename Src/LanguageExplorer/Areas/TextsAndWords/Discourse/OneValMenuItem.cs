// Copyright (c) 2008-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	internal class OneValMenuItem : DisposableToolStripMenuItem
	{
		public OneValMenuItem(string label, int colSrc)
			: base(label)
		{
			Source = colSrc;
		}

		/// <summary>
		/// The source (other) column.
		/// </summary>
		public int Source { get; }
	}
}