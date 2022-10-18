<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/SuppressErrorBeepsAttribute.cs
// Copyright (c) 2017-2020 SIL International
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/SuppressErrorBeepsAttribute.cs
﻿// Copyright (c) 2017 SIL International
=======
// Copyright (c) 2017 SIL International
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/SuppressErrorBeepsAttribute.cs
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/SuppressErrorBeepsAttribute.cs
using SIL.FieldWorks.Common.FwUtils;
using NUnit.Framework.Interfaces;
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/SuppressErrorBeepsAttribute.cs
=======
using NUnit.Framework.Interfaces;
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/SuppressErrorBeepsAttribute.cs

namespace FieldWorks.TestUtilities.Attributes
{
	/// <summary>
	/// Suppresses error beeps.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
	public class SuppressErrorBeepsAttribute : TestActionAttribute
	{
<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/SuppressErrorBeepsAttribute.cs
		/// <inheritdoc />
		public override void BeforeTest(ITest testDetails)
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/SuppressErrorBeepsAttribute.cs
		/// <summary>
		/// Method called before each test
		/// </summary>
		public override void BeforeTest(TestDetails testDetails)
=======
		/// <summary>
		/// Method called before each test
		/// </summary>
		public override void BeforeTest(ITest test)
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/SuppressErrorBeepsAttribute.cs
		{
			base.BeforeTest(test);

			FwUtils.SuppressErrorBeep = true;
		}

<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/SuppressErrorBeepsAttribute.cs
		/// <inheritdoc />
		public override void AfterTest(ITest testDetails)
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/SuppressErrorBeepsAttribute.cs
		/// <summary>
		/// Method called after each test
		/// </summary>
		public override void AfterTest(TestDetails testDetails)
=======
		/// <summary>
		/// Method called after each test
		/// </summary>
		public override void AfterTest(ITest test)
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/SuppressErrorBeepsAttribute.cs
		{
			base.AfterTest(test);

			FwUtils.SuppressErrorBeep = false;
		}
	}
}
