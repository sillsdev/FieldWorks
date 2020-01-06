// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace FieldWorks.TestUtilities.Attributes
{
	/// <summary>
	/// Handles unhandled exceptions that occur in Windows Forms threads. This avoids the display of unhandled exception dialogs when
	/// running nunit-console. Avoiding the dialogs is preferable, because it can cause an unattended build to pause, while it waits for
	/// input from the user. In addition, if the user presses "Continue", it makes the test pass. Rethrowing the exception doesn't bring
	/// up the dialog and correctly makes the test fail.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
	public class HandleApplicationThreadExceptionAttribute : TestActionAttribute
	{
		/// <inheritdoc />
		public override void BeforeTest(TestDetails testDetails)
		{
			base.BeforeTest(testDetails);

			Application.ThreadException += OnThreadException;
		}

		/// <inheritdoc />
		public override void AfterTest(TestDetails testDetails)
		{
			base.AfterTest(testDetails);

			Application.ThreadException -= OnThreadException;
		}

		private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
		{
			throw new ApplicationException(e.Exception.Message, e.Exception);
		}
	}
}