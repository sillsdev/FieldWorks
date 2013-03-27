// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using NUnit.Framework;

namespace SIL.Utils.Attributes
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NUnit helper attribute that registers the unmanaged COM DLLs of FieldWorks before we
	/// start running tests and optionally unregisters them afterwards.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class |
		AttributeTargets.Interface)]
	public class RegisterComDllsAttribute: Attribute, ITestAction
	{
		private static readonly ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim();
		private static string m_pathFwKernel;
		private static string m_pathLanguage;
		private static string m_pathViews;
		private static string m_pathGraphite;

		private readonly bool m_fUninstall;

		public RegisterComDllsAttribute(): this(true)
		{
		}

		public RegisterComDllsAttribute(bool uninstall)
		{
			m_fUninstall = uninstall;
		}

		#region ITestAction Members

		/// <summary>
		/// Register the COM dlls if they aren't registered yet
		/// </summary>
		public void BeforeTest(TestDetails testDetails)
		{
#if !__MonoCS__
			// On Linux we use a different (file based) approach for accessing COM objects,
			// so we don't have to do this.
			m_lock.EnterWriteLock();
			try
			{
				var uriBase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
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
			}
			finally
			{
				m_lock.ExitWriteLock();
			}
#endif
		}

		/// <summary>
		/// Unregister the COM DLLs
		/// </summary>
		public void AfterTest(TestDetails testDetails)
		{
#if !__MonoCS__
			// On Linux we use a different (file based) approach for accessing COM objects,
			// so we don't have to do this.

			if (!m_fUninstall)
				return;

			m_lock.EnterWriteLock();
			try
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
			finally
			{
				m_lock.ExitWriteLock();
			}
#endif
		}

		/// <summary>
		/// Run on the fixture
		/// </summary>
		public ActionTargets Targets
		{
			get { return ActionTargets.Suite; }
		}

		#endregion

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
