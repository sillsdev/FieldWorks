// Copyright (c) 2003-2018 SIL International
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
	public class PossibilityAtomicReferenceVc : AtomicReferenceVc
	{
		private string m_textStyle;

		public PossibilityAtomicReferenceVc(LcmCache cache, int flid, string displayNameProperty)
			: base(cache, flid, displayNameProperty)
		{
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case AtomicReferenceView.kFragAtomicRef:
					// Display a paragraph with a single item.
					vwenv.OpenParagraph();		// vwenv.OpenMappedPara();
					if (!string.IsNullOrEmpty(TextStyle))
					{
						vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, TextStyle);
					}
					vwenv.AddStringProp(PossibilityAtomicReferenceView.kflidFake, this);
					vwenv.CloseParagraph();
					break;
				default:
					throw new ArgumentException("Don't know what to do with the given frag.", nameof(frag));
			}
		}
	}
}