// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	internal sealed class LexReferenceTreeBranchesVc : VectorReferenceVc
	{
		/// <summary>
		/// Constructor for the Vector Reference View Constructor Class.
		/// </summary>
		public LexReferenceTreeBranchesVc(LcmCache cache, int flid, string displayNameProperty, string displayWs)
			: base (cache, flid, displayNameProperty, displayWs)
		{
		}

		/// <summary>
		/// Calling vwenv.AddObjVec() in Display() and implementing DisplayVec() seems to
		/// work better than calling vwenv.AddObjVecItems() in Display().  Theoretically
		/// this should not be case, but experience trumps theory every time.  :-) :-(
		/// </summary>
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			var da = vwenv.DataAccess;
			// Tree Branches consist of everything FOLLOWING the first element which is the Tree root.
			for (var i = 1; i < da.get_VecSize(hvo, tag); ++i)
			{
				vwenv.AddObj(da.get_VecItem(hvo, tag, i), this, VectorReferenceView.kfragTargetObj);
				vwenv.AddSeparatorBar();
			}
		}
	}
}