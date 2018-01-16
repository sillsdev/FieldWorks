// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	public enum TreeItemState : byte
	{
		ktisCollapsed,
		ktisExpanded,
		ktisFixed, // not able to expand or contract
		// Normally capable of expansion, this node has no current children, typically because it
		// expands to show a sequence and the sequence is empty. We treat it like 'collapsed'
		// in that, if an object is added to the sequence, we show it. But, it is drawn as an empty
		// box, and clicking has no effect.
		ktisCollapsedEmpty
	}
}