// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using NUnit.Framework.Interfaces;

namespace FieldWorks.TestUtilities.Attributes
{
	/// <summary>
	/// Suppresses error beeps.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
	public class SuppressErrorBeepsAttribute : TestActionAttribute
	{
		/// <inheritdoc />
		public override void BeforeTest(ITest testDetails)
		{
			base.BeforeTest(testDetails);

			FwUtils.SuppressErrorBeep = true;
		}

		/// <inheritdoc />
		public override void AfterTest(ITest testDetails)
		{
			base.AfterTest(testDetails);

			FwUtils.SuppressErrorBeep = false;
		}
	}
}
