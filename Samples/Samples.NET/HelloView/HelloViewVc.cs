/// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002, SIL International. All Rights Reserved.
/// <copyright from='2002' to='2002' company='SIL International'>
///		Copyright (c) 2002, SIL International. All Rights Reserved.
///
///		Distributable under the terms of either the Common Public License or the
///		GNU Lesser General Public License, as specified in the LICENSING.txt file.
/// </copyright>
#endregion
///
/// File: HelloViewVc.cs
/// Responsibility: Eberhard Beilharz
/// Last reviewed:
///
/// <remarks>
/// Implementation of the view constructor
/// </remarks>
/// --------------------------------------------------------------------------------------------

using System;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Samples.HelloView
{
	/// <summary>
	/// Implementation of the ViewConstructor
	/// </summary>
	public class HvVc: VwBaseVc
	{
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				default:
					break;
				case HelloViewView.kfrText:
					vwenv.AddStringProp(HelloViewView.ktagProp, this);
					break;
			}
		}
	}
}
