// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	internal class PossibilityVectorReferenceVc : VectorReferenceVc
	{
		public PossibilityVectorReferenceVc(LcmCache cache, int flid, string displayNameProperty, string displayWs)
			: base(cache, flid, displayNameProperty, displayWs)
		{
		}

		/// <summary>
		/// This is the basic method needed for the view constructor.
		/// </summary>
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case VectorReferenceView.kfragTargetVector:
					if (!string.IsNullOrEmpty(TextStyle))
					{
						vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, TextStyle);
					}
					vwenv.OpenParagraph();
					vwenv.AddObjVec(m_flid, this, frag);
					vwenv.CloseParagraph();
					break;
				case VectorReferenceView.kfragTargetObj:
					// Display one object by displaying the fake string property of that object which our special
					// private decorator stores for it.
					vwenv.AddStringProp(PossibilityVectorReferenceView.kflidFake, this);
					break;
				default:
					throw new ArgumentException("Don't know what to do with the given frag.", nameof(frag));
			}
		}

		/// <summary>
		/// Calling vwenv.AddObjVec() in Display() and implementing DisplayVec() seems to
		/// work better than calling vwenv.AddObjVecItems() in Display().  Theoretically
		/// this should not be case, but experience trumps theory every time.  :-) :-(
		/// </summary>
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			if (hvo == 0)
			{
				return;
			}
			var da = vwenv.DataAccess;
			var count = da.get_VecSize(hvo, tag);
			for (var i = 0; i < count; ++i)
			{
				vwenv.AddObj(da.get_VecItem(hvo, tag, i), this, VectorReferenceView.kfragTargetObj);
				vwenv.AddSeparatorBar();
			}
			vwenv.AddObj(PossibilityVectorReferenceView.khvoFake, this, VectorReferenceView.kfragTargetObj);
		}
	}
}