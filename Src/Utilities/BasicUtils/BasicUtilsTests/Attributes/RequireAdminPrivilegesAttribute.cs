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
