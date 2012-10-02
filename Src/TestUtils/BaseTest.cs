// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BaseTest.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;

[assembly:SecurityPermissionAttribute(SecurityAction.RequestMinimum, UnmanagedCode=true)]
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
	public class BaseTest : IFWDisposable
	{
		/// <summary></summary>
		protected DebugProcs m_DebugProcs;

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
		[DllImport("Ole32.dll")]
		private extern static void CoFreeUnusedLibraries();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
#if false
			CheckDisposed();
#else

			/* We need to document cases where this needs to be worried about.
			 * I (RandyR) don't understand how this can happen, if the nunit framework
			 * creates a test class instance, calls this method, runs all its tests,
			 * calls the TestFictureTeardown code (Dispose),
			 * and then turns loose of the test class instance.
			 *
			 * If the above is what happens, then how can it be called twice?
			 * If the class can indeed be reused, and this method called more than once,
			 * in what contexts does it happen?
			 *
			 * Until we can nail down that context,
			 * let's see if we can live with the more rigid approach.
			 *
			 * Followup observation:
			 * I (RandyR) saw in some acceptance tests (UndoRedoTests) that a test TearDown
			 * method had called this method, as well as the Dispose method, as a way
			 * to be sure the next was set up correcly. That is not good!
			 * I fixed that class to not override this method or the Dispose method,
			 * but to just do all the setup/teardown stuff between each test.
			 */
			// Answer (TimS/EberhardB): NUnit doesn't create a new instance each time
			// a test fixture is run. Therefore we have to resurrect the instance, otherwise
			// we can't run tests twice in NUnit-GUI.
			// ENHANCE: We think our use of doing a Dispose in the TestFixtureTearDown is wrong,
			// since FixtureSetUp and FixtureTearDown go together, so FixtureTearDown should
			// clean the things that FixtureSetUp created (or that might be left over from
			// running the tests).
			if (m_isDisposed)
			{
				// in case we're running the same test twice we have to reset the m_isDisposed
				// flag, otherwise in FixtureTearDown we think that Dispose was already called.
				m_isDisposed = false;
				GC.ReRegisterForFinalize(this);
			}

#if false
			// This should already be disposed if we run this test fixture twice.
			if (m_DebugProcs != null)
				m_DebugProcs.Dispose();
#endif
#endif

			m_DebugProcs = new DebugProcs();
			try
			{
				StringUtils.InitIcuDataDir();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		#region IDisposable & Co. implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		~BaseTest()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Subclasses should override the Dispose method with the 'bool disposing' parameter
		/// and call the base method to tear down a test fixture class.
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// FWC-16: we have to call CoFreeUnusedLibraries. This causes sqlnclir.dll to get
			// unloaded. If we don't do this we get a deadlock after the fixture teardown
			// because we're running STA.
			CoFreeUnusedLibraries();

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_DebugProcs != null)
					m_DebugProcs.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_DebugProcs = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

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
