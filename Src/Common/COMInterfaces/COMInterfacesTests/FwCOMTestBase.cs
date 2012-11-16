// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwCOMTestBase.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using NUnit.Framework;
using SIL.Utils;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test base class that automatically registers/unregisters the COM DLLs.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwCOMTestBase
	{
		static int m_regs;
		static ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim();
		static string m_pathFwKernel;
		static string m_pathLanguage;
		static string m_pathViews;
		static string m_pathGraphite;

		/// <summary>
		/// Constructor: register the COM DLLs if they exist, and haven't yet been
		/// registered.
		/// </summary>
		public FwCOMTestBase()
		{
#if !__MonoCS__
			// On Linux we use a different (file based) approach for accessing COM objects,
			// so we don't have to do this.
			m_lock.EnterWriteLock();
			try
			{
				Uri uriBase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
				string sBaseDir = Path.GetDirectoryName(Uri.UnescapeDataString(uriBase.AbsolutePath));
				if (m_pathFwKernel == null)
				{
					string sDllFile = Path.Combine(sBaseDir, "FwKernel.dll");
					if (File.Exists(sDllFile))
					{
						m_pathFwKernel = sDllFile;
						Register(m_pathFwKernel);
					}
				}
				if (m_pathLanguage == null)
				{
					string sDllFile = Path.Combine(sBaseDir, "Language.dll");
					if (File.Exists(sDllFile))
					{
						m_pathLanguage = sDllFile;
						Register(m_pathLanguage);
					}
				}
				if (m_pathViews == null)
				{
					string sDllFile = Path.Combine(sBaseDir, "Views.dll");
					if (File.Exists(sDllFile))
					{
						m_pathViews = sDllFile;
						Register(m_pathViews);
					}
				}
				if (m_pathGraphite == null)
				{
					string sDllFile = Path.Combine(sBaseDir, "Graphite.dll");
					if (File.Exists(sDllFile))
					{
						m_pathGraphite = sDllFile;
						Register(m_pathGraphite);
					}
				}
				++m_regs;
			}
			finally
			{
				m_lock.ExitWriteLock();
			}
#endif
		}

#if !__MonoCS__
		/// <summary>
		/// Destructor: unregister the COM DLLs.
		/// </summary>
		~FwCOMTestBase()
		{
			// ENHANCE: doing this in the Finalizer is problematic since objects get finalized
			// in random order, i.e. m_lock might already be finalized here.
			// It would be better to add a SetupFixture class that registers/unregisters
			// the COM dlls. However, we would have to add this class to each test assembly.
			m_lock.EnterWriteLock();
			try
			{
				--m_regs;
				Debug.Assert(m_regs >= 0);
				if (m_regs <= 0)
				{
					if (m_pathFwKernel != null)
					{
						Unregister(m_pathFwKernel);
						m_pathFwKernel = null;
					}
					if (m_pathGraphite != null)
					{
						Unregister(m_pathGraphite);
						m_pathGraphite = null;
					}
					if (m_pathLanguage != null)
					{
						Unregister(m_pathLanguage);
						m_pathLanguage = null;
					}
					if (m_pathViews != null)
					{
						Unregister(m_pathViews);
						m_pathViews = null;
					}
				}
				if (m_regs < 0)
					m_regs = 0;
			}
			finally
			{
				m_lock.ExitWriteLock();
			}
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		/// <summary/>
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
			// Set stub for messagebox so that we don't pop up a message box when running tests.
			MessageBoxUtils.Manager.SetMessageBoxAdapter(new MessageBoxStub());
		}

		// The following code was copied from RegFreeCreator.cs (under $FW/Bin/nant/src/FwTasks).

		#region Imported methods to register dll
		[DllImport("oleaut32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
		private static extern ITypeLib LoadTypeLib(string szFile);

		[DllImport("oleaut32.dll")]
		private static extern int RegisterTypeLib(ITypeLib typeLib, string fullPath, string helpDir);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The LoadLibrary function maps the specified executable module into the address
		/// space of the calling process.
		/// </summary>
		/// <param name="fileName">The name of the executable module (either a .dll or .exe
		/// file).</param>
		/// <returns>If the function succeeds, the return value is a handle to the module. If
		/// the function fails, the return value is IntPtr.Zero.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr LoadLibrary(string fileName);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The FreeLibrary function decrements the reference count of the loaded dynamic-link
		/// library (DLL). When the reference count reaches zero, the module is unmapped from
		/// the address space of the calling process and the handle is no longer valid.
		/// </summary>
		/// <param name="hModule">The handle to the loaded DLL module.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("Kernel32.dll", SetLastError = true)]
		private static extern int FreeLibrary(IntPtr hModule);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The GetProcAddress function retrieves the address of an exported function or
		/// variable from the specified dynamic-link library (DLL).
		/// </summary>
		/// <param name="hModule">A handle to the DLL module that contains the function or
		/// variable.</param>
		/// <param name="lpProcName">The function or variable name, or the function's ordinal
		/// value.</param>
		/// <returns>If the function succeeds, the return value is the address of the exported
		/// function or variable. If the function fails, the return value is IntPtr.Zero.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("Kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a directory to the search path used to locate DLLs for the application.
		/// </summary>
		/// <param name="pathName">The directory to be added to the search path. If this
		/// parameter is an empty string (""), the call removes the current directory from the
		/// default DLL search order. If this parameter is <c>null</c>, the function restores
		/// the default search order.</param>
		/// <returns>If the function succeeds, the return value is nonzero. If the function
		/// fails, the return value is zero. To get extended error information, call
		/// GetLastError.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool SetDllDirectory(string pathName);

		// The delegate for DllRegisterServer.
		[return: MarshalAs(UnmanagedType.Error)]
		private delegate int DllRegisterServerFunction();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dynamically invokes a method in a dll.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="methodName">Name of the method.</param>
		/// <returns><c>true</c> if successfully invoked method, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private static void ApiInvoke(string fileName, string methodName)
		{
			if (!File.Exists(fileName))
				return;

			IntPtr hModule = LoadLibrary(fileName);
			if (hModule == IntPtr.Zero)
				return;

			try
			{
				IntPtr method = GetProcAddress(hModule, methodName);
				if (method == IntPtr.Zero)
					return;

				Marshal.GetDelegateForFunctionPointer(method,
					typeof(DllRegisterServerFunction)).DynamicInvoke();
			}
			finally
			{
				FreeLibrary(hModule);
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Temporarily registers the specified file.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// ------------------------------------------------------------------------------------
		internal static void Register(string fileName)
		{
			SetDllDirectory(Path.GetDirectoryName(fileName));
			ApiInvoke(fileName, "DllRegisterServer");
			try
			{
				ITypeLib typeLib = LoadTypeLib(fileName);
				RegisterTypeLib(typeLib, fileName, null);
			}
			catch
			{
				// just ignore any errors
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unregisters the specified file.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// ------------------------------------------------------------------------------------
		internal static void Unregister(string fileName)
		{
			ApiInvoke(fileName, "DllUnregisterServer");
		}
	}
}
