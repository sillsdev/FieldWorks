// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.InteropServices;
using System.Security;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.PlatformUtilities;

namespace FieldWorks.TestUtilities.Attributes
{
	/// <summary>
	/// NUnit helper class that allows to create COM objects from a manifest file
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface)]
	[SuppressUnmanagedCodeSecurity]
	public class CreateComObjectsFromManifestAttribute : TestActionAttribute
	{
		private ActivationContextHelper m_activationContext;
		private IDisposable m_currentActivation;

		/// <inheritdoc />
		public override ActionTargets Targets => ActionTargets.Suite;

		/// <inheritdoc />
		public override void BeforeTest(TestDetails testDetails)
		{
			base.BeforeTest(testDetails);

			m_activationContext = new ActivationContextHelper("FieldWorks.Tests.manifest");
			m_currentActivation = m_activationContext.Activate();

			if (!Platform.IsWindows)
			{
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
				catch (Exception)
				{
					// just ignore any errors we get
				}
			}
		}

		/// <inheritdoc />
		public override void AfterTest(TestDetails testDetails)
		{
			CoFreeUnusedLibraries();

			m_currentActivation.Dispose();
			m_activationContext.Dispose();

			base.AfterTest(testDetails);
		}

		/// <summary>
		/// Unloads any DLLs that are no longer in use and that, when loaded, were specified to
		/// be freed automatically
		/// </summary>
		[DllImport("ole32.dll")]
		private static extern void CoFreeUnusedLibraries();

		[DllImport("libc")] // Linux
		private static extern int prctl(int option, IntPtr arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);

		private const int PR_SET_PTRACER = 0x59616d61;
	}
}