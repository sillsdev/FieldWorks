// Copyright (c) 2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using Palaso.UI.WindowsForms.Keyboarding;
using Palaso.WritingSystems;

namespace SIL.Utils.Attributes
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NUnit helper attribute that initializes the real keyboard controller before the tests and
	/// calls shutdown afterwards. If InitDummyAfterTests is <c>true</c> a dummy keyboard
	/// controller will be created after the test or suite has finished, i.e. in the AfterTest
	/// method.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class |
		AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
	public class InitializeRealKeyboardControllerAttribute: TestActionAttribute
	{
		/// <summary>
		/// Gets or sets a value indicating whether a dummy keyboard controller should be
		/// initialized after this test/suite run.
		/// </summary>
		public bool InitDummyAfterTests { get; set; }

		/// <summary>
		/// Initialize keyboard controller
		/// </summary>
		public override void BeforeTest(TestDetails testDetails)
		{
			if (Keyboard.Controller != null)
				Keyboard.Controller.Dispose();

			KeyboardController.Initialize();
			base.BeforeTest(testDetails);
		}

		/// <summary>
		/// Shutdown keyboard controller
		/// </summary>
		public override void AfterTest(TestDetails testDetails)
		{
			base.AfterTest(testDetails);
			KeyboardController.Shutdown();

			if (InitDummyAfterTests)
				Keyboard.Controller = new NoOpKeyboardController();
		}
	}
}
