// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.LexText
{
	internal class FeatureTreeNodeInfo
	{
		public FeatureTreeNodeKind eKind;
		public int iHvo;
		public FeatureTreeNodeInfo(int hvo, FeatureTreeNodeKind kind)
		{
			iHvo = hvo;
			eKind = kind;
		}
	}
}