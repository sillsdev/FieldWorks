// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2003' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BaseTest.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for tests that deals with debug output in unmanaged code
	/// </summary>
	/// <remarks>
	/// Use this class as base class for your tests if you use unmanaged objects
	/// and want to get the debug output of them, or if you want to disable popping up of
	/// message boxes due to assertions in C++ code.
	///
	/// If you do use this base class, then be sure to also use the class's system for Fisture
	/// Setup and TearDown, and than call the base methods.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public class BaseTest
	{
		/// <summary></summary>
		protected DebugProcs m_debugProcs;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BaseTest"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BaseTest()
		{
			Application.ThreadException += new ThreadExceptionEventHandler(OnThreadException);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when an unhandled exception is thrown.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.Threading.ThreadExceptionEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnThreadException(object sender, ThreadExceptionEventArgs e)
		{
			// this looks really strange - but if we don't catch this exception then .NET brings
			// up an unhandled exception dialog when running with nunit-console. This is not what
			// we want since that dialog waits for user input which stops an unattended build,
			// and besides if the users presses Continue it makes the tests pass.
			// Rethrowing the exception doesn't bring up this dialog and makes the tests fail.
			throw new ApplicationException(e.Exception.Message, e.Exception);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unloads any DLLs that are no longer in use and that, when loaded, were specified to
		/// be freed automatically
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DllImport("ole32.dll")]
		private extern static void CoFreeUnusedLibraries();

#if __MonoCS__
		[DllImport ("libc")] // Linux
		private static extern int prctl(int option, IntPtr arg2, IntPtr arg3, IntPtr arg4,
			IntPtr arg5);

		private const int PR_SET_PTRACER = 0x59616d61;
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
			m_debugProcs = new DebugProcs();
			try
			{
				Icu.InitIcuDataDir();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}

#if __MonoCS__
			try
			{
				using (var process = System.Diagnostics.Process.GetCurrentProcess())
				{
					// try to change PTRACE option so that unmanaged call stacks show more useful
					// information. Since Ubuntu 10.10 a normal user is no longer allowed to use
					// PTRACE. This prevents call stacks and assertions from working properly.
					// However, we can set a flag on the currently running process to allow
					// it. See also the similar code in Generic/ModuleEntry.cpp
					prctl(PR_SET_PTRACER, (IntPtr)process.Id, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				}
			}
			catch (Exception e)
			{
				// just ignore any errors we get
			}
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans up some resources that were used during the test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public virtual void FixtureTeardown()
		{
			KeyboardHelper.Release();

			// FWC-16: we have to call CoFreeUnusedLibraries. This causes sqlnclir.dll to get
			// unloaded. If we don't do this we get a deadlock after the fixture teardown
			// because we're running STA.
			CoFreeUnusedLibraries();

			m_debugProcs.Dispose();
			m_debugProcs = null;
		}

		#region Reflection methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string value returned from a call to a private method.
		/// </summary>
		/// <param name="binding">This is either the Type of the object on which the method
		/// is called or an instance of that type of object. When the method being called
		/// is static then binding should be a type.</param>
		/// <param name="methodName">Name of the method to call.</param>
		/// <param name="args">An array of arguments to pass to the method call.</param>
		/// ------------------------------------------------------------------------------------
		protected string GetStrResult(object binding, string methodName, params object[] args)
		{
			return (GetResult(binding, methodName, args) as string);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a integer value returned from a call to a private method.
		/// </summary>
		/// <param name="binding">This is either the Type of the object on which the method
		/// is called or an instance of that type of object. When the method being called
		/// is static then binding should be a type.</param>
		/// <param name="methodName">Name of the method to call.</param>
		/// <param name="args">An array of arguments to pass to the method call.</param>
		/// ------------------------------------------------------------------------------------
		protected int GetIntResult(object binding, string methodName, params object[] args)
		{
			return ((int)GetResult(binding, methodName, args));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a float value returned from a call to a private method.
		/// </summary>
		/// <param name="binding">This is either the Type of the object on which the method
		/// is called or an instance of that type of object. When the method being called
		/// is static then binding should be a type.</param>
		/// <param name="methodName">Name of the method to call.</param>
		/// <param name="args">An array of arguments to pass to the method call.</param>
		/// ------------------------------------------------------------------------------------
		protected float GetFloatResult(object binding, string methodName, params object[] args)
		{
			return ((float)GetResult(binding, methodName, args));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a boolean value returned from a call to a private method.
		/// </summary>
		/// <param name="binding">This is either the Type of the object on which the method
		/// is called or an instance of that type of object. When the method being called
		/// is static then binding should be a type.</param>
		/// <param name="methodName">Name of the method to call.</param>
		/// <param name="args">An array of arguments to pass to the method call.</param>
		/// ------------------------------------------------------------------------------------
		protected bool GetBoolResult(object binding, string methodName, params object[] args)
		{
			return ((bool)GetResult(binding, methodName, args));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls a method specified on the specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallMethod(object binding, string methodName, params object[] args)
		{
			GetResult(binding, methodName, args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the result of calling a method on the specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public object GetResult(object binding, string methodName, params object[] args)
		{
			return Invoke(binding, methodName, args, BindingFlags.InvokeMethod);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the specified property on the specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SetProperty(object binding, string propertyName, object args)
		{
			Invoke(binding, propertyName, new object[] { args }, BindingFlags.SetProperty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the specified field (i.e. member variable) on the specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SetField(object binding, string fieldName, object args)
		{
			Invoke(binding, fieldName, new object[] { args }, BindingFlags.SetField);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the specified property on the specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected object GetProperty(object binding, string propertyName)
		{
			return Invoke(binding, propertyName, null, BindingFlags.GetProperty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the specified field (i.e. member variable) on the specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected object GetField(object binding, string fieldName)
		{
			return Invoke(binding, fieldName, null, BindingFlags.GetField);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the specified member variable or property (specified by name) on the
		/// specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private object Invoke(object binding, string name, object[] args, BindingFlags flags)
		{
			flags |= BindingFlags.NonPublic;

			// If binding is a Type then assume invoke on a static method, property or field.
			// Otherwise invoke on an instance method, property or field.
			if (binding is Type)
			{
				return ((binding as Type).InvokeMember(name,
					flags | BindingFlags.Static, null, binding, args));
			}
			else
			{
				return binding.GetType().InvokeMember(name,
					flags | BindingFlags.Instance, null, binding, args);
			}
		}

		#endregion
	}
}
