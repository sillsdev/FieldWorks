// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils.Attributes
{
	/// <summary>
	/// Suppresses error beeps.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method,
		AllowMultiple = true)]
	public class SuppressErrorBeepsAttribute : TestActionAttribute
	{
		/// <summary>
		/// Method called before each test
		/// </summary>
		public override void BeforeTest(TestDetails testDetails)
		{
			base.BeforeTest(testDetails);

			FwUtils.SuppressErrorBeep = true;
		}

		/// <summary>
		/// Method called after each test
		/// </summary>
		public override void AfterTest(TestDetails testDetails)
		{
			base.AfterTest(testDetails);

			FwUtils.SuppressErrorBeep = false;
		}
	}
}
