// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;

namespace LanguageExplorer
{
	/// <summary>
	/// This interface (the only current implementation is XmlViews.LayoutMerger) is used when we find an old version
	/// of an inventory element while loading user overrides. It is used only when there is a current element with
	/// the same key. It is passed the current element, the one 'wanted' (the old version), and the destination
	/// document in which a merged element should be created and returned.
	/// Enhance JohnT: We could pass null for current if there is no current node with that key. We could allow
	/// returning null if no merge is possible.
	/// </summary>
	public interface IOldVersionMerger
	{
		/// <summary>
		/// Do the merge.
		/// </summary>
		XElement Merge(XElement newMaster, XElement oldConfigured, XDocument dest, string oldLayoutLevelSuffix);
	}
}