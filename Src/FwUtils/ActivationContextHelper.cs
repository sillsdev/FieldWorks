// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using SIL.ObjectModel;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Used to create an activation context
	/// </summary>
	[SuppressUnmanagedCodeSecurity]
	public class ActivationContextHelper : DisposableBase
	{
		#region Unmanaged structs and methods
		[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
		private struct ActCtx
		{
			public int cbSize;
			public uint dwFlags;
			public string lpSource;
			public ushort wProcessorArchitecture;
			public short wLangId;
			public string lpAssemblyDirectory;
			public string lpResourceName;
			public string lpApplicationName;
			public IntPtr hModule;
		}

		[DllImport("Kernel32.dll", SetLastError = true, EntryPoint = "CreateActCtxW")]
		private static extern IntPtr CreateActCtx(ref ActCtx actCtx);

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ActivateActCtx(IntPtr hActCtx, out IntPtr lpCookie);

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeactivateActCtx(int dwFlags, IntPtr lpCookie);

		[DllImport("Kernel32.dll", SetLastError = true)]
		private static extern void ReleaseActCtx(IntPtr hActCtx);
		#endregion // Unmanaged structs and methods

		private IntPtr m_activationContext;

		/// <summary />
		public ActivationContextHelper(string manifestFile)
		{
			if (!Platform.IsWindows)
			{
				return;
			}
			// Specifying a full path to the manifest file like this allows our unit tests to work even with a
			// test runner like Resharper 8 which does not set the current directory to the one containing the DLLs.
			// Note that we have to use CodeBase here because NUnit runs the tests from a shadow copy directory
			// that doesn't contain the manifest file.
			var path = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
			var location = Path.GetDirectoryName(path);
			var context = new ActCtx
			{
				cbSize = Marshal.SizeOf(typeof(ActCtx)),
				lpSource = Path.Combine(location, manifestFile)
			};

			var handle = CreateActCtx(ref context);
			if (handle == (IntPtr)(-1))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Error creating activation context");
			}
			m_activationContext = handle;
		}

		/// <summary>
		/// Override to dispose unmanaged resources.
		/// </summary>
		protected override void DisposeUnmanagedResources()
		{
			if (!Platform.IsWindows)
			{
				return;
			}
			// dispose unmanaged objects
			if (m_activationContext != IntPtr.Zero)
			{
				ReleaseActCtx(m_activationContext);
			}
			m_activationContext = IntPtr.Zero;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			base.Dispose(disposing);
		}

		/// <summary>
		/// Activates this instance.
		/// </summary>
		/// <exception cref="Win32Exception">Error activating context</exception>
		public IDisposable Activate()
		{
			var cookie = IntPtr.Zero;
			if (Platform.IsWindows)
			{
				if (!ActivateActCtx(m_activationContext, out cookie))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Error activating context");
				}
			}

			return new Activation(cookie);
		}

		private sealed class Activation : DisposableBase
		{
			private IntPtr m_cookie;

			public Activation(IntPtr cookie)
			{
				m_cookie = cookie;
			}

			protected override void DisposeUnmanagedResources()
			{
				if (!Platform.IsWindows)
				{
					return;
				}
				if (m_cookie != IntPtr.Zero)
				{
					DeactivateActCtx(0, m_cookie);
				}
				m_cookie = IntPtr.Zero;
			}

			/// <inheritdoc />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
				base.Dispose(disposing);
			}
		}
	}
}