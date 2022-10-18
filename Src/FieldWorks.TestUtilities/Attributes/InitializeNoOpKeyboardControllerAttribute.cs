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
	/// NUnit helper attribute that creates a dummy keyboard controller. This is suitable for
	/// unit tests that don't test any keyboarding functions. A test or suite that requires
	/// feedback from the real keyboard controller should use the
	/// InitializeRealKeyboardControllerAttribute instead.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
	public class InitializeNoOpKeyboardControllerAttribute: TestActionAttribute
	{
<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/InitializeNoOpKeyboardControllerAttribute.cs
		/// <inheritdoc />
		public override void BeforeTest(ITest testDetails)
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/InitializeNoOpKeyboardControllerAttribute.cs
		/// <summary>
		/// Create a dummy keyboard controller
		/// </summary>
		public override void BeforeTest(TestDetails testDetails)
=======
		/// <summary>
		/// Create a dummy keyboard controller
		/// </summary>
		public override void BeforeTest(ITest test)
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/InitializeNoOpKeyboardControllerAttribute.cs
		{
			base.BeforeTest(test);
			// If we already have a keyboard controller we'd better dispose it or we'll end up with missing dispose calls.
			if (Keyboard.Controller != null)
			{
				Keyboard.Controller.Dispose();
			}
			KeyboardController.Initialize(new DummyKeyboardAdaptor());

		}

<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/InitializeNoOpKeyboardControllerAttribute.cs
		/// <inheritdoc />
		public override void AfterTest(ITest testDetails)
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/InitializeNoOpKeyboardControllerAttribute.cs
		/// <summary>
		/// Unset keyboard controller
		/// </summary>
		public override void AfterTest(TestDetails testDetails)
=======
		/// <summary>
		/// Unset keyboard controller
		/// </summary>
		public override void AfterTest(ITest test)
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/InitializeNoOpKeyboardControllerAttribute.cs
		{
			// Shut down (and implicitly dispose) the keyboard controller we created.
<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/InitializeNoOpKeyboardControllerAttribute.cs
			base.AfterTest(testDetails);
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/InitializeNoOpKeyboardControllerAttribute.cs

			base.AfterTest(testDetails);
=======

			base.AfterTest(test);
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/InitializeNoOpKeyboardControllerAttribute.cs
			KeyboardController.Shutdown();
			Keyboard.Controller = new DefaultKeyboardController();
		}
	}
}
