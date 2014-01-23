// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.Utils.Attributes
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NUnit helper attribute that releases all singletons at the end of all tests
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class |
		AttributeTargets.Interface)]
	public class CleanupSingletonsAttribute : TestActionAttribute
	{
		/// <summary/>
		public override ActionTargets Targets
		{
			get { return ActionTargets.Suite; }
		}

		/// <summary>
		/// Release all singletons
		/// </summary>
		public override void AfterTest(TestDetails testDetails)
		{
			base.AfterTest(testDetails);
			SingletonsContainer.Release();
		}
	}
}
