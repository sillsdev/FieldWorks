// Copyright (c) 2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;
using SIL.WritingSystems;
using SIL.WritingSystems.WindowsForms.Keyboarding;

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
		/// Initialize keyboard controller
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Keyboard.Controller gets disposed")]
		public override void BeforeTest(TestDetails testDetails)
		{
			base.BeforeTest(testDetails);
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
			Keyboard.Controller = new DefaultKeyboardController();
		}
	}
}
