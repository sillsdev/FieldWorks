// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.LexText
{
	internal class FeatureTreeNodeInfo
	{
		public enum NodeKind
		{
			Complex = 0,
			Closed,
			SymFeatValue,
			Other
		}
		public NodeKind eKind;
		public int iHvo;
		public FeatureTreeNodeInfo(int hvo, NodeKind kind)
		{
			iHvo = hvo;
			eKind = kind;
		}
	}
}