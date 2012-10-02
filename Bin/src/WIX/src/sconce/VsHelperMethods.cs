//-------------------------------------------------------------------------------------------------
// <copyright file="VsHelperMethods.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Helper methods for working with the Visual Studio environment.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using System.Globalization;
	using Microsoft.VisualStudio.Shell.Interop;

	public sealed class VsHelperMethods
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(VsHelperMethods);
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Prevent direct instantiation of this class.
		/// </summary>
		private VsHelperMethods()
		{
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Prints a line per document in the RDT (Running Document Table) to the trace log.
		/// </summary>
		[Conditional("TRACE")]
		public static void TraceRunningDocuments()
		{
			// Get the RDT (Running Document Table)
			IVsRunningDocumentTable rdt = Package.Instance.Context.ServiceProvider.GetService(typeof(IVsRunningDocumentTable)) as IVsRunningDocumentTable;
			if (rdt == null)
			{
				Tracer.WriteLineWarning(classType, "TraceRunningDocuments", "Cannot get an instance of IVsRunningDocumentTable to use for enumerating the running documents.");
				return;
			}

			// Get the enumerator for the currently running documents.
			IEnumRunningDocuments enumerator;
			int hr = rdt.GetRunningDocumentsEnum(out enumerator);
			if (NativeMethods.Failed(hr))
			{
				Tracer.WriteLineWarning(classType, "TraceRunningDocuments", "Cannot get an instance of IEnumRunningDocuments to use for enumerating the running documents.");
				return;
			}

			// Enumerate.
			StringCollection traceLines = new StringCollection();
			uint[] cookies = new uint[1];
			uint fetchCount;
			while (true)
			{
				hr = enumerator.Next(1, cookies, out fetchCount);
				if (NativeMethods.Failed(hr))
				{
					Tracer.WriteLineWarning(classType, "TraceRunningDocuments", "The enumeration failed for the running documents. Hr=0x{0:X}", hr);
					return;
				}

				if (fetchCount == 0)
				{
					break;
				}

				uint cookie = cookies[0];

				// We shouldn't be getting a nil cookie.
				if (cookie == DocumentInfo.NullCookie)
				{
					Tracer.WriteLineWarning(classType, "TraceRunningDocuments", "There is a null cookie value in the RDT, which shouldn't be happening.");
				}
				else
				{
					// Now we have a document cookie, so let's get some information about it.
					DocumentInfo docInfo = Package.Instance.Context.RunningDocumentTable.FindByCookie(cookie);
					string traceMessage;
					if (docInfo == null)
					{
						traceMessage = PackageUtility.SafeStringFormatInvariant("The document with cookie '{0}' could not be found in the RDT. There's something weird going on.", cookie);
					}
					else
					{
						// Here's where we actually do the trace finally.
						traceMessage = PackageUtility.SafeStringFormatInvariant("RDT document: Cookie={0} Path={1} IsOpen={2} IsDirty={3}", docInfo.Cookie, docInfo.AbsolutePath, docInfo.IsOpen, docInfo.IsDirty);
					}

					// We don't want to trace immediately because we want all of these lines to appear together. If we
					// trace immediately, then the messages will be split up.
					traceLines.Add(traceMessage);
				}
			}

			// Now trace all of the messages at once.
			foreach (string traceMessage in traceLines)
			{
				Tracer.WriteLine(classType, "TraceRunningDocuments", Tracer.Level.Information, traceMessage);
			}
		}
		#endregion
	}
}
