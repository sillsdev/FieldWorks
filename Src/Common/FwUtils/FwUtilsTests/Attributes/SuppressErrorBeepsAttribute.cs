// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

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
		public override void BeforeTest(ITest test)
		{
			base.BeforeTest(test);

			FwUtils.SuppressErrorBeep = true;
		}

		/// <summary>
		/// Method called after each test
		/// </summary>
		public override void AfterTest(ITest test)
		{
			base.AfterTest(test);

			FwUtils.SuppressErrorBeep = false;
		}
	}
}
