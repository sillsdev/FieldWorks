// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.Utils.Attributes
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NUnit helper attribute that ignores the test if not running with admin privileges.
	/// Attribute can be applied to test or test fixture.
	/// </summary>
	/// <remarks>Requires NUnit >= 2.6</remarks>
	/// ----------------------------------------------------------------------------------------
	public class RequireAdminPrivilegesAttribute: TestActionAttribute
	{
		/// <summary/>
		public override ActionTargets Targets
		{
			get { return ActionTargets.Test | ActionTargets.Suite; }
		}

		/// <summary/>
		public override void BeforeTest(TestDetails testDetails)
		{
			if (!MiscUtils.IsUserAdmin)
				Assert.Ignore("Requires administrator privileges");
		}
	}
}
