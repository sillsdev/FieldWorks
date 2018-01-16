// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	internal class PhoneEnvReferenceVc : FwBaseVc
	{
		internal PhoneEnvReferenceVc(LcmCache cache)
		{
			Debug.Assert(cache != null);
			m_cache = cache;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case PhoneEnvReferenceView.kFragEnvironmentObj:
					vwenv.AddStringProp(PhoneEnvReferenceView.kEnvStringRep, this);
					break;
				case PhoneEnvReferenceView.kFragEnvironments:
					vwenv.OpenParagraph();
					vwenv.AddObjVec(PhoneEnvReferenceView.kMainObjEnvironments, this, frag);
					vwenv.CloseParagraph();
					break;
				default:
					throw new ArgumentException(@"Don't know what to do with the given frag.", nameof(frag));
			}
		}

		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			var da = vwenv.DataAccess;
			var count = da.get_VecSize(hvo, tag);
			for (var i = 0; i < count; ++i)
			{
				if (i != 0)
				{
					vwenv.AddSeparatorBar();
				}
				vwenv.AddObj(da.get_VecItem(hvo, tag, i), this, PhoneEnvReferenceView.kFragEnvironmentObj);
			}
		}
	}
}