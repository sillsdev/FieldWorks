// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Win32;

namespace SIL.FieldWorks.Build.Tasks
{
	/// <summary>
	/// Helper class for COM DLL registration and type library loading.
	/// Note: Registry redirection has been removed - RegFree manifest generation now reads
	/// directly from HKEY_CLASSES_ROOT where COM classes are already registered.
	/// </summary>
	public class RegHelper : IDisposable
	{
		private TaskLoggingHelper m_Log;
		private bool IsDisposed { get; set; }

		/// <summary>
		/// Initializes a new instance of RegHelper.
		/// </summary>
		/// <param name="log">MSBuild logging helper</param>
		/// <param name="platform">Platform (unused, kept for compatibility)</param>
		public RegHelper(TaskLoggingHelper log, string platform)
		{
			m_Log = log;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="fDisposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Dispose(bool fDisposing)
		{
			IsDisposed = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads a type library from a file.
		/// </summary>
		/// <param name="szFile">Path to the file containing the type library</param>
		/// <param name="typeLib">Output parameter receiving the loaded type library</param>
		/// <returns>0 if successful, otherwise an error code</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("oleaut32.dll", CharSet = CharSet.Unicode)]
		public static extern int LoadTypeLib(string szFile, out ITypeLib typeLib);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Registers a type library in the system registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DllImport("oleaut32.dll")]
		private static extern int RegisterTypeLib(
			ITypeLib typeLib,
			string fullPath,
			string helpDir
		);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the long path name for a short path.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DllImport("kernel32.dll")]
		public static extern int GetLongPathName(
			string shortPath,
			StringBuilder longPath,
			int longPathLength
		);

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

		[return: MarshalAs(UnmanagedType.Error)]
		private delegate int DllRegisterServerFunction();

		[return: MarshalAs(UnmanagedType.Error)]
		private delegate int DllInstallFunction(
			bool fInstall,
			[MarshalAs(UnmanagedType.LPWStr)] string cmdLine
		);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dynamically invokes <paramref name="methodName "/> in the dll
		/// <paramref name="fileName"/>.
		/// </summary>
		/// <param name="log">Log helper.</param>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="methodName">Name of the method.</param>
		/// <returns><c>true</c> if successfully invoked method, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		internal static void ApiInvoke(TaskLoggingHelper log, string fileName, string methodName)
		{
			ApiInvoke(log, fileName, typeof(DllRegisterServerFunction), methodName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dynamically invokes the DllInstall method in the dll <paramref name="fileName"/>.
		/// </summary>
		/// <param name="log">Log helper.</param>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="fRegister"><c>true</c> to register, <c>false</c> to unregister.</param>
		/// <param name="inHklm"><c>true</c> to register in HKLM, otherwise in HKCU.</param>
		/// <returns><c>true</c> if successfully invoked method, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		internal static void ApiInvokeDllInstall(
			TaskLoggingHelper log,
			string fileName,
			bool fRegister,
			bool inHklm
		)
		{
			ApiInvoke(
				log,
				fileName,
				typeof(DllInstallFunction),
				"DllInstall",
				fRegister,
				inHklm ? null : "user"
			);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dynamically invokes <paramref name="methodName"/> in dll <paramref name="fileName"/>.
		/// </summary>
		/// <param name="log">Log helper</param>
		/// <param name="fileName">Name of the dll.</param>
		/// <param name="delegateSignatureType">Signature of the method.</param>
		/// <param name="methodName">Name of the method</param>
		/// <param name="args">Arguments to pass to <paramref name="methodName"/>.</param>
		/// ------------------------------------------------------------------------------------
		private static void ApiInvoke(
			TaskLoggingHelper log,
			string fileName,
			Type delegateSignatureType,
			string methodName,
			params object[] args
		)
		{
			if (!File.Exists(fileName))
				return;
			fileName = Path.GetFullPath(fileName);
			IntPtr hModule = LoadLibrary(fileName);
			if (hModule == IntPtr.Zero)
			{
				var errorCode = Marshal.GetLastWin32Error();
				log.LogError(
					"Failed to load library {0} for {1} with error code {2}",
					fileName,
					methodName,
					errorCode
				);
				return;
			}

			try
			{
				IntPtr method = GetProcAddress(hModule, methodName);
				if (method == IntPtr.Zero)
					return;

				Marshal
					.GetDelegateForFunctionPointer(method, delegateSignatureType)
					.DynamicInvoke(args);
			}
			catch (Exception e)
			{
				log.LogError(
					"RegHelper.ApiInvoke failed getting function pointer for {0}: {1}",
					methodName,
					e
				);
			}
			finally
			{
				FreeLibrary(hModule);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Temporarily registers the specified file.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="registerInHklm"><c>true</c> to register in HKLM, <c>false</c> to
		/// register in HKCU. Passing <c>false</c> has the same effect as calling
		/// regsvr32 with parameter /i:user.</param>
		/// <param name="registerTypeLib"><c>true</c> to also register tye type library.</param>
		/// ------------------------------------------------------------------------------------
		public bool Register(string fileName, bool registerInHklm, bool registerTypeLib)
		{
			SetDllDirectory(Path.GetDirectoryName(fileName));
			ApiInvokeDllInstall(m_Log, fileName, true, registerInHklm);
			try
			{
				if (registerInHklm && registerTypeLib)
				{
					ITypeLib typeLib;
					var loadResult = LoadTypeLib(fileName, out typeLib);
					if (loadResult == 0)
					{
						var registerResult = RegisterTypeLib(typeLib, fileName, null);
						if (registerResult == 0)
						{
							m_Log.LogMessage(
								MessageImportance.Low,
								"Registered type library {0} with result {1}",
								fileName,
								registerResult
							);
						}
						else
						{
							m_Log.LogWarning(
								"Registering type library {0} failed with result {1} (RegisterTypeLib)",
								fileName,
								registerResult
							);
						}
					}
					else
					{
						m_Log.LogWarning(
							"Registering type library {0} failed with result {1} (LoadTypeLib)",
							fileName,
							loadResult
						);
					}
				}
				else
					m_Log.LogMessage(MessageImportance.Low, "Registered {0}", fileName);
			}
			catch (Exception e)
			{
				m_Log.LogWarningFromException(e);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unregisters the specified file.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="inHklm"><c>true</c> to unregister from HKLM, <c>false</c> to
		/// unregister from HKCU. Passing <c>false</c> has the same effect as calling
		/// regsvr32 with parameter /i:user.</param>
		/// ------------------------------------------------------------------------------------
		public void Unregister(string fileName, bool inHklm)
		{
			ApiInvokeDllInstall(m_Log, fileName, false, inHklm);
		}
	}
}
