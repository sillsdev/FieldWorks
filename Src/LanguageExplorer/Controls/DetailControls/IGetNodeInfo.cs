// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// The is the basic interface that must be implemented to configure a TwoLevelConc.
	/// Review JohnT: very possibly this method could just be part of IConcPolicy.
	/// However, clients are much less likely to use one of the default implementations
	/// for this method.
	/// </summary>
	internal interface IGetNodeInfo
	{
		/// <summary>
		/// Obtain an implementation of INodeInfo for the specified object at the specified
		/// position in the overall list of top-level objects.
		/// </summary>
		INodeInfo InfoFor(int ihvoRoot, int hvoRoot);
	}
}