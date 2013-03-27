// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
using System;
using System.Security;
using NUnit.Framework;

namespace SIL.Utils.Attributes
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NUnit helper class that allows to create COM objects from a manifest file
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class |
		AttributeTargets.Interface)]
	[SuppressUnmanagedCodeSecurity]
	public class CreateComObjectsFromManifestAttribute : TestActionAttribute
	{
		/// <summary/>
		public override ActionTargets Targets
		{
			get { return ActionTargets.Suite; }
		}

		/// <summary/>
		public override void BeforeTest(TestDetails testDetails)
		{
			base.BeforeTest(testDetails);

#if !__MonoCS__
			ManifestHelper.CreateActivationContext();
#endif
		}

		/// <summary/>
		public override void AfterTest(TestDetails testDetails)
		{
#if !__MonoCS__
			ManifestHelper.DestroyActivationContext();
#endif

			base.AfterTest(testDetails);
		}
	}
}
