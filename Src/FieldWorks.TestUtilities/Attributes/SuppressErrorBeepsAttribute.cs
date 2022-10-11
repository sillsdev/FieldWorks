// Copyright (c) 2017-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace FieldWorks.TestUtilities.Attributes
{
	/// <summary>
	/// Suppresses error beeps.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
	public class SuppressErrorBeepsAttribute : TestActionAttribute
	{
		/// <inheritdoc />
		public override void BeforeTest(ITest test)
		{
			base.BeforeTest(test);

			FwUtils.SuppressErrorBeep = true;
		}

		/// <inheritdoc />
		public override void AfterTest(ITest test)
		{
			base.AfterTest(test);

			FwUtils.SuppressErrorBeep = false;
		}
	}
}
