#region Copyright (c) 2003-2005, Luke T. Maxon

/********************************************************************************************************************
'
' Copyright (c) 2003-2005, Luke T. Maxon
' All rights reserved.
'
' Redistribution and use in source and binary forms, with or without modification, are permitted provided
' that the following conditions are met:
'
' * Redistributions of source code must retain the above copyright notice, this list of conditions and the
' 	following disclaimer.
'
' * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and
' 	the following disclaimer in the documentation and/or other materials provided with the distribution.
'
' * Neither the name of the author nor the names of its contributors may be used to endorse or
' 	promote products derived from this software without specific prior written permission.
'
' THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
' WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
' PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
' ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
' LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
' INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
' OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN
' IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
'
'*******************************************************************************************************************/

#endregion

using System;
using System.Reflection;
using System.Windows.Forms;

namespace NUnit.Extensions.Forms
{
	public class NUnitFormTest
	{
		protected bool verified;

		private static readonly FieldInfo isUserInteractive =
				typeof(SystemInformation).GetField("isUserInteractive", BindingFlags.Static | BindingFlags.NonPublic);

		/// <summary>
		/// This property controls whether a separate desktop is used at all.  I highly recommend that you
		/// leave this as returning true.  Tests on the separate desktop are faster and safer.  (There is
		/// no danger of keyboard or mouse input going to your own separate running applications.)  However
		/// I have heard report of operating systems or environments where the separate desktop does not work.
		/// In that case there are 2 options.  You can override this method from your test class to return false.
		/// Or you can set an environment variable called "UseHiddenDesktop" and set that to "false"  Either will
		/// cause the tests to run on your original, standard desktop.
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		/// <li>This method now defaults to <c>false</c>. When the problems with the separate desktop are solved, this
		/// method will again return <c>true</c>.</li>
		/// <li>An <c>else</c> branch to deal with <c>UseHiddenDesktop</c> is <c>TRUE</c>.</li>
		/// </list>
		/// </remarks>
		public virtual bool UseHidden
		{
			get
			{
				string useHiddenDesktop = Environment.GetEnvironmentVariable("UseHiddenDesktop");
				if (useHiddenDesktop != null && useHiddenDesktop.ToUpper().Equals("FALSE"))
				{
					return false;
				}
				if (useHiddenDesktop != null && useHiddenDesktop.ToUpper().Equals("TRUE"))
				{
					return true;
				}

				return false;
			}
		}

		/// <summary>
		/// This is the base classes setup method.  It will be called by NUnit before each test.
		/// You should not have anything to do with it.
		/// </summary>
		public void SetUp()
		{
			verified = false;
			if (!SystemInformation.UserInteractive)
			{
				isUserInteractive.SetValue(null, true);
			}

			modal = new ModalFormTester();
		}

		private ModalFormTester modal;

		/// <summary>
		/// This method is needed because the way the FileDialogs working are strange.
		/// It seems that both open/save dialogs initial title is "Open". The handler
		/// </summary>
		/// <param name="modalHandler"></param>
		public void ExpectFileDialog(object obj, string modalHandler)
		{
			ExpectModal(obj, "Open", modalHandler);
		}

		public void ExpectFileDialog(object obj, string modalHandler, bool expected)
		{
			ExpectModal(obj, "Open", modalHandler, expected);
		}

		public void ExpectFileDialog(ModalFormActivated handler)
		{
			modal.ExpectModal("Open", handler, true);
		}

		public void ExpectFileDialog(ModalFormActivated handler, bool expected)
		{
			modal.ExpectModal("Open", handler, true);
		}

		/// <summary>
		/// One of four overloaded methods to set up a modal dialog handler.  If you expect a modal
		/// dialog to appear and can handle it during the test, use this method to set up the handler.
		/// </summary>
		/// <param name="name">The caption on the dialog you expect.</param>
		/// <param name="handler">The method to call when that dialog appears.</param>
		public void ExpectModal(string name, ModalFormActivated handler)
		{
			modal.ExpectModal(name, handler, true);
		}

		/// <summary>
		/// One of four overloaded methods to set up a modal dialog handler.  If you expect a modal
		/// dialog to appear and can handle it during the test, use this method to set up the handler.
		/// Because "expected" is usually (always) true if you are calling this, I don't expect it will
		/// be used externally.
		/// </summary>
		/// <param name="name">The caption on the dialog you expect.</param>
		/// <param name="handler">The method to call when that dialog appears.</param>
		/// <param name="expected">A boolean to indicate whether you expect this modal dialog to appear.</param>
		public void ExpectModal(string name, ModalFormActivated handler, bool expected)
		{
			modal.ExpectModal(name, handler, expected);
		}

		/// <summary>
		/// One of four overloaded methods to set up a modal dialog handler.  If you expect a modal
		/// dialog to appear and can handle it during the test, use this method to set up the handler.
		/// Because "expected" is usually (always) true if you are calling this, I don't expect it will
		/// be used externally.
		/// </summary>
		/// <param name="name">The caption on the dialog you expect.</param>
		/// <param name="handlerName">The name of the method to call when that dialog appears.</param>
		/// <param name="expected">A boolean to indicate whether you expect this modal dialog to appear.</param>
		public void ExpectModal(object obj, string name, string handlerName, bool expected)
		{
			ExpectModal(name,
						(ModalFormActivated)Delegate.CreateDelegate(typeof(ModalFormActivated), obj, handlerName),
						expected);
		}

		/// <summary>
		/// One of four overloaded methods to set up a modal dialog handler.  If you are not sure which
		/// to use, use this one.  If you expect a modal dialog to appear and can handle it during the
		/// test, use this method to set up the handler. Because "expected" is usually (always) true
		/// if you are calling this, I don't expect it will be used externally.
		/// </summary>
		/// <param name="name">The caption on the dialog you expect.</param>
		/// <param name="handlerName">The name of the method to call when that dialog appears.</param>
		public void ExpectModal(object obj, string name, string handlerName)
		{
			ExpectModal(obj, name, handlerName, true);
		}

		/// <summary>
		/// This method should be called after each test runs.
		/// </summary>
		public void TearDown()
		{
			if (!verified)
			{
				verified = true;
				FormCollection allForms = new FormFinder().FindAll();

				foreach (Form form in allForms)
				{
					if (!KeepAlive(form))
					{
						form.Dispose();
						form.Hide();
					} //else branch not tested
				}

				bool modalVerify = modal.Verify();

				modal.Dispose();

				if (!modalVerify)
				{
					throw new FormsTestAssertionException("unexpected/expected modal was invoked/not invoked");
				}
			}
		}

		internal bool KeepAlive(Form form)
		{
			return form is IKeepAlive && (form as IKeepAlive).KeepAlive;
		}
	}

	public interface IKeepAlive
	{
		bool KeepAlive
		{
			get;
		}
	}
}