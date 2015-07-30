// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Samples.InterlinearSample
{
	/// <summary>
	/// View constructor for the interlinear view.
	/// </summary>
	public class InterlinearVc: VwBaseVc
	{
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				default:
					break;
				case InterlinearView.kfrText:
					vwenv.OpenParagraph();
					vwenv.AddObjVecItems(InterlinearView.ktagText_Words, this, InterlinearView.kfrWord);
					vwenv.CloseParagraph();
					break;
				case InterlinearView.kfrWord:
					vwenv.set_IntProperty((int)FwKernelLib.FwTextPropType.ktptMarginTrailing,
												(int)FwKernelLib.FwTextPropVar.ktpvMilliPoint, 10000);
					vwenv.OpenInnerPile();
					vwenv.AddStringProp(InterlinearView.ktagWord_Form, this);
					vwenv.AddStringProp(InterlinearView.ktagWord_Type, this);
					vwenv.CloseInnerPile();
					break;
			}
		}
	}
}
