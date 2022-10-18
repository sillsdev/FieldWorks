// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using SIL.Keyboarding;
using SIL.Windows.Forms.Keyboarding;

namespace FieldWorks.TestUtilities.Attributes
{
	/// <summary>
	/// NUnit helper attribute that initializes the real keyboard controller before the tests and
	/// calls shutdown afterwards. If InitDummyAfterTests is <c>true</c> a dummy keyboard
	/// controller will be created after the test or suite has finished, i.e. in the AfterTest
	/// method.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
	public class InitializeRealKeyboardControllerAttribute: TestActionAttribute
	{
<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/InitializeRealKeyboardControllerAttribute.cs
		/// <inheritdoc />
		public override void BeforeTest(ITest testDetails)
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/InitializeRealKeyboardControllerAttribute.cs
		/// <summary>
		/// Initialize keyboard controller
		/// </summary>
		public override void BeforeTest(TestDetails testDetails)
=======
		/// <summary>
		/// Initialize keyboard controller
		/// </summary>
		public override void BeforeTest(ITest test)
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/InitializeRealKeyboardControllerAttribute.cs
		{
<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/InitializeRealKeyboardControllerAttribute.cs
			base.BeforeTest(testDetails);

			Keyboard.Controller?.Dispose();
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/InitializeRealKeyboardControllerAttribute.cs
			base.BeforeTest(testDetails);
			if (Keyboard.Controller != null)
				Keyboard.Controller.Dispose();
=======
			base.BeforeTest(test);
			if (Keyboard.Controller != null)
				Keyboard.Controller.Dispose();
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/InitializeRealKeyboardControllerAttribute.cs

			KeyboardController.Initialize();
			base.BeforeTest(test);
		}

<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/InitializeRealKeyboardControllerAttribute.cs
		/// <inheritdoc />
		public override void AfterTest(ITest testDetails)
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/InitializeRealKeyboardControllerAttribute.cs
		/// <summary>
		/// Shutdown keyboard controller
		/// </summary>
		public override void AfterTest(TestDetails testDetails)
=======
		/// <summary>
		/// Shutdown keyboard controller
		/// </summary>
		public override void AfterTest(ITest test)
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/InitializeRealKeyboardControllerAttribute.cs
		{
			base.AfterTest(test);
			KeyboardController.Shutdown();
			Keyboard.Controller = new DefaultKeyboardController();
		}
	}
}
