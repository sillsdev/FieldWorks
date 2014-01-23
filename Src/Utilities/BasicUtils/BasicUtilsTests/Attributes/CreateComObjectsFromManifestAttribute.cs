// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
			ManifestHelper.CreateActivationContext("FieldWorks.Tests.manifest");
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
