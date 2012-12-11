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
	public class RegHelper : IDisposable
	{
		private TaskLoggingHelper m_Log;
		private bool RedirectRegistryFailed { get; set; }
		private bool IsRedirected { get; set; }
		private bool IsDisposed { get; set; }
		private static readonly UIntPtr HKEY_CLASSES_ROOT = new UIntPtr(0x80000000);
		private static readonly UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001);

		/// <summary/>
		public RegHelper(TaskLoggingHelper log)
		{
			m_Log = log;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the a temporary registry key to register dlls. This registry key is process
		/// specific, so multiple instances can run at the same time without interfering with
		/// each other.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string TmpRegistryKey
		{
			get
			{
				return string.Format(@"Software\SIL\NAntBuild\tmp-{0}",
					Process.GetCurrentProcess().Id);
			}
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
			if (!IsDisposed)
			{
				if (IsRedirected && !RedirectRegistryFailed)
				{
					EndRedirection();
					m_Log.LogMessage(MessageImportance.Low, TmpRegistryKey);
					Registry.CurrentUser.DeleteSubKeyTree(TmpRegistryKey);
				}
			}

			IsDisposed = true;
		}

		[DllImport("oleaut32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
		public static extern ITypeLib LoadTypeLib(string szFile);

		[DllImport("oleaut32.dll")]
		private static extern int RegisterTypeLib(ITypeLib typeLib, string fullPath, string helpDir);

		[DllImport("kernel32.dll")]
		public static extern int GetLongPathName(string shortPath, StringBuilder longPath,
			int longPathLength);

		[DllImport("Advapi32.dll")]
		private static extern int RegOverridePredefKey(UIntPtr hKey, UIntPtr hNewKey);

		[DllImport("Advapi32.dll")]
		private static extern int RegCreateKey(UIntPtr hKey, string lpSubKey, out UIntPtr phkResult);

		[DllImport("Advapi32.dll")]
		private static extern int RegCloseKey(UIntPtr hKey);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Temporarily redirects access to HKCR to a subkey under HKCU.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RedirectRegistry()
		{
			try
			{
				IsRedirected = true;
				UIntPtr hKey;
				RegCreateKey(HKEY_CURRENT_USER, TmpRegistryKey, out hKey);
				RegOverridePredefKey(HKEY_CLASSES_ROOT, hKey);
				RegCloseKey(hKey);

				// We also have to create a CLSID subkey - some DLLs expect that it exists
				Registry.CurrentUser.CreateSubKey(TmpRegistryKey + @"\CLSID");
			}
			catch
			{
				m_Log.LogError("registry redirection failed.");
				RedirectRegistryFailed = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ends the redirection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void EndRedirection()
		{
			SetDllDirectory(null);
			RegOverridePredefKey(HKEY_CLASSES_ROOT, UIntPtr.Zero);
		}

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
		private delegate int DllInstallFunction(bool fInstall,
			[MarshalAs(UnmanagedType.LPWStr)] string cmdLine);

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
		internal static void ApiInvokeDllInstall(TaskLoggingHelper log, string fileName,
			bool fRegister, bool inHklm)
		{
			ApiInvoke(log, fileName, typeof(DllInstallFunction), "DllInstall", fRegister,
				inHklm ? null : "user");
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
		private static void ApiInvoke(TaskLoggingHelper log, string fileName,
			Type delegateSignatureType, string methodName, params object[] args)
		{
			if (!File.Exists(fileName))
				return;
			fileName = Path.GetFullPath(fileName);
			IntPtr hModule = LoadLibrary(fileName);
			if (hModule == IntPtr.Zero)
			{
				var errorCode = Marshal.GetLastWin32Error();
				log.LogError("Failed to load library {0} for {1} with error code {2}", fileName, methodName,
					errorCode);
				return;
			}

			try
			{
				IntPtr method = GetProcAddress(hModule, methodName);
				if (method == IntPtr.Zero)
					return;

				Marshal.GetDelegateForFunctionPointer(method, delegateSignatureType).DynamicInvoke(args);
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
		/// ------------------------------------------------------------------------------------
		public bool Register(string fileName, bool registerInHklm)
		{
			SetDllDirectory(Path.GetDirectoryName(fileName));
			ApiInvokeDllInstall(m_Log, fileName, true, registerInHklm);
			try
			{
				if (registerInHklm)
				{
					ITypeLib typeLib = LoadTypeLib(fileName);
					var registerResult = RegisterTypeLib(typeLib, fileName, null);
					if (registerResult == 0)
					{
						m_Log.LogMessage(MessageImportance.Low, "Registered {0} with result {1}",
							fileName, registerResult);
					}
					else
					{
						m_Log.LogWarning("Registering {0} failed with result {1}", fileName,
							registerResult);
					}
				}
				else
					m_Log.LogMessage(MessageImportance.Low, "Registered {0}", fileName);
			}
			catch(Exception e)
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