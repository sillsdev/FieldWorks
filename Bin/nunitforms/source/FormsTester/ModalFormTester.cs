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
using System.Collections;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace NUnit.Extensions.Forms
{
	/// <summary>
	/// Used to specify a handler for a Modal form that is displayed during testing.
	/// </summary>
	public delegate void ModalFormActivated();

	internal delegate void ModalFormActivatedHwnd(IntPtr hWnd);

	public class ModalFormTester : IDisposable
	{
		private class Handler
		{
			public Handler(Delegate handler, bool expected)
			{
				this.handler = handler;
				this.expected = expected;
			}

			public bool Verify()
			{
				return expected == invoked;
			}

			public void Invoke(IntPtr hWnd)
			{
				invoked = true;
				try
				{
					if (handler is ModalFormActivated)
					{
						handler.DynamicInvoke(new object[] { });
					}
					else if (handler is ModalFormActivatedHwnd)
					{
						handler.DynamicInvoke(new object[] { hWnd });
					}
				}
				catch (TargetInvocationException ex)
				{
					if (ex.InnerException != null)
					{
						throw ex.InnerException;
					}
				}
			}

			private bool invoked = false;

			private bool expected = false;

			private Delegate handler = null;
		}

		private Hashtable handlers = new Hashtable();

		public string ANY = Guid.NewGuid().ToString();

		public ModalFormTester()
		{
			Add(ANY,
				(ModalFormActivatedHwnd)
				Delegate.CreateDelegate(typeof(ModalFormActivatedHwnd), this, "UnexpectedModal"), false);
		}

		public void UnexpectedModal(IntPtr hWnd)
		{
			//MessageBoxTester messageBox = new MessageBoxTester(hWnd);
			//messageBox.ClickOk();
		}

		public void ExpectModal(string name, ModalFormActivated handler)
		{
			ExpectModal(name, handler, true);
		}

		public void ExpectModal(string name, ModalFormActivated handler, bool expected)
		{
			BeginListening(); //can be called multiple times.
			handlers[name] = new Handler(handler, expected);
		}

		internal void Add(string name, ModalFormActivatedHwnd handler, bool expected)
		{
			BeginListening(); //can be called multiple times.
			handlers[name] = new Handler(handler, expected);
		}

		~ModalFormTester()
		{
			Dispose();
		}

		public bool Verify()
		{
			foreach (string name in handlers.Keys)
			{
				Handler h = handlers[name] as Handler;
				if (!h.Verify())
				{
					return false;
				}
			}
			return true;
		}

		private Win32.CBTCallback callback = null;

		private IntPtr handleToHook = IntPtr.Zero;

		private const int CbtHookType = 5;
		private const int HCBT_ACTIVATE = 5;

		private bool listening = false;

		private void BeginListening()
		{
			if (!listening)
			{
				listening = true;
				callback = new Win32.CBTCallback(ModalListener);
				handleToHook = Win32.SetWindowsHookEx(CbtHookType, callback, IntPtr.Zero, Win32.GetCurrentThreadId());
			}
		}

		public void Dispose()
		{
			if (handleToHook != IntPtr.Zero)
			{
				Win32.UnhookWindowsHookEx(handleToHook);
				handleToHook = IntPtr.Zero;
			}
			GC.SuppressFinalize(this);
		}

		private void Invoke(string name, IntPtr hWnd)
		{
			if (name != null)
			{
				Handler h = handlers[name] as Handler;
				if (h != null)
				{
					h.Invoke(hWnd);
					return;
				}

				Handler h2 = handlers[ANY] as Handler;
				if (h2 != null)
				{
					h2.Invoke(hWnd);
				}
			}
		}

		private IntPtr ModalListener(int code, IntPtr wParam, IntPtr lParam)
		{
			if (code == HCBT_ACTIVATE)
			{
				Form form = Form.FromHandle(wParam) as Form;

				string name = null;

				if (form != null && form.Modal)
				{
					name = form.Name;
				}
				else if (IsDialog(wParam))
				{
					System.Diagnostics.Debug.Assert(false);
					//name = MessageBoxTester.GetCaption(wParam);
					if (name == null)
					{
						name = string.Empty;
					}
				}

				Invoke(name, wParam);
			}

			return Win32.CallNextHookEx(handleToHook, code, wParam, lParam);
		}

		protected bool IsDialog(IntPtr wParam)
		{
			StringBuilder className = new StringBuilder();
			className.Capacity = 255;
			Win32.GetClassName(wParam, className, 255);

			return ("#32770" == className.ToString());
		}
	}
}