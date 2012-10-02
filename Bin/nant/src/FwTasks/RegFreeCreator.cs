// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RegFreeCreator.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml;
using Microsoft.Win32;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Creates the necessary entries that allows later to use an assembly without registering
	/// it.
	/// </summary>
	/// <remarks>At the time this class gets called, the assembly needs to be registered!
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public class RegFreeCreator: IDisposable
	{
		#region Member variables
		[System.Runtime.InteropServices.DllImport("oleaut32.dll",
		   CharSet = System.Runtime.InteropServices.CharSet.Unicode, PreserveSig = false)]
		private static extern ITypeLib LoadTypeLib(string szFile);

		[System.Runtime.InteropServices.DllImport("oleaut32.dll")]
		private static extern int RegisterTypeLib(ITypeLib typeLib, string fullPath, string helpDir);

		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		private static extern int GetLongPathName(string shortPath, StringBuilder longPath,
			int longPathLength);

		private bool m_fRedirectRegistryFailed;
		/// <summary>The directory where the type library resides</summary>
		private string m_BaseDirectory;
		private string m_FileName;
		private readonly XmlDocument m_Doc;
		private readonly bool m_fDisplayWarnings = true;
		private readonly Dictionary<string, XmlElement> m_Files = new Dictionary<string, XmlElement>();
		private readonly Dictionary<string, XmlElement> m_CoClasses = new Dictionary<string, XmlElement>();
		private readonly Dictionary<Guid, string> m_TlbGuids = new Dictionary<Guid, string>();
		private readonly List<string> m_NonExistingServers = new List<string>();
		private bool m_fIsDisposed;

		private const string UrnSchema = "http://www.w3.org/2001/XMLSchema-instance";
		private const string UrnAsmv1 = "urn:schemas-microsoft-com:asm.v1";
		private const string UrnAsmv2 = "urn:schemas-microsoft-com:asm.v2";
		private const string UrnDsig = "http://www.w3.org/2000/09/xmldsig#";
		#endregion

		#region Constructors and Dispose
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RegFreeCreator"/> class.
		/// </summary>
		/// <param name="doc">The XML document.</param>
		/// ------------------------------------------------------------------------------------
		public RegFreeCreator(XmlDocument doc)
		{
			m_Doc = doc;
			RedirectRegistry();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RegFreeCreator"/> class.
		/// </summary>
		/// <param name="doc">The XML document.</param>
		/// <param name="fDisplayWarnings">set to <c>true</c> to display warnings, otherwise
		/// <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		public RegFreeCreator(XmlDocument doc, bool fDisplayWarnings): this(doc)
		{
			m_fDisplayWarnings = fDisplayWarnings;
		}

		#region IDisposable Members

#if DEBUG
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="RegFreeCreator"/> is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~RegFreeCreator()
		{
			Debug.Fail("Finalizer called - there is a call to Dispose() missing!");
		}
#endif

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
			if (!m_fIsDisposed)
			{
				if (!m_fRedirectRegistryFailed)
				{
					EndRedirection();
					Registry.CurrentUser.DeleteSubKeyTree(@"Software\SIL\NAntBuild\tmp");
				}
			}

			m_fIsDisposed = true;
		}

		#endregion

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the info for the executable. The info consist of:
		/// <list type="bullet">
		///		<item>name (from file name)</item>
		///		<item>version info (from assembly)</item>
		///		<item>type (hardcoded as "win32" for now)</item>
		/// </list>
		/// This method also adds the root element with all necessary namespaces.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// ------------------------------------------------------------------------------------
		public XmlElement CreateExeInfo(string fileName)
		{
			XmlElement elem = m_Doc.CreateElement("assembly", UrnAsmv1);
			elem.SetAttribute("manifestVersion", "1.0");
			elem.SetAttribute("xmlns:asmv1", UrnAsmv1);
			elem.SetAttribute("xmlns:asmv2", UrnAsmv2);
			elem.SetAttribute("xmlns:dsig", UrnDsig);
			elem.SetAttribute("xmlns:xsi", UrnSchema);
			elem.SetAttribute("schemaLocation", UrnSchema, UrnAsmv1 + " assembly.adaptive.xsd");

			XmlNode oldChild = m_Doc.SelectSingleNode("assembly");
			if (oldChild != null)
				m_Doc.ReplaceChild(elem, oldChild);
			else
				m_Doc.AppendChild(elem);

			// <assemblyIdentity name="TE.exe" version="1.4.1.39149" type="win32" />
			XmlElement assemblyIdentity = m_Doc.CreateElement("assemblyIdentity", UrnAsmv1);
			assemblyIdentity.SetAttribute("name", Path.GetFileName(fileName));
// ReSharper disable EmptyGeneralCatchClause
			try
			{
				FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(fileName);
				assemblyIdentity.SetAttribute("version", versionInfo.FileVersion);
			}
			catch
			{
				// just ignore
			}
// ReSharper restore EmptyGeneralCatchClause
			assemblyIdentity.SetAttribute("type", "win32");

			oldChild = elem.SelectSingleNode("assemblyIdentity");
			if (oldChild != null)
				elem.ReplaceChild(assemblyIdentity, oldChild);
			else
				elem.AppendChild(assemblyIdentity);

			return elem;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the entries for the type library and all classes defined in this type
		/// library. We can get the necessary information for the file element (file name and
		/// size) directly from the file. We get the information for the type library from
		/// the type library itself.
		/// </summary>
		/// <param name="parent">The parent node.</param>
		/// <param name="fileName">Name (and path) of the file.</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessTypeLibrary(XmlElement parent, string fileName)
		{
			m_BaseDirectory = Path.GetDirectoryName(fileName);
			m_FileName = fileName.ToLower();

			try
			{
				XmlNode oldChild;
				ITypeLib typeLib = LoadTypeLib(m_FileName);
				IntPtr pLibAttr;
				typeLib.GetLibAttr(out pLibAttr);
				var libAttr = (TYPELIBATTR)
					System.Runtime.InteropServices.Marshal.PtrToStructure(pLibAttr, typeof(TYPELIBATTR));
				typeLib.ReleaseTLibAttr(pLibAttr);

				string flags = string.Empty;
				if ((libAttr.wLibFlags & LIBFLAGS.LIBFLAG_FHASDISKIMAGE) == LIBFLAGS.LIBFLAG_FHASDISKIMAGE)
					flags = "HASDISKIMAGE";

				// <file name="FwKernel.dll" asmv2:size="1507328">
				XmlElement file = GetOrCreateFileNode(parent, fileName);

				// <typelib tlbid="{2f0fccc0-c160-11d3-8da2-005004defec4}" version="1.0" helpdir=""
				//		resourceid="0" flags="HASDISKIMAGE" />
				if (m_TlbGuids.ContainsKey(libAttr.guid))
				{
					Console.WriteLine("Warning: Type library with GUID {0} is defined in {1} and {2}",
						libAttr.guid, m_TlbGuids[libAttr.guid], Path.GetFileName(fileName));
				}
				else
				{
					m_TlbGuids.Add(libAttr.guid, Path.GetFileName(fileName));
					XmlElement elem = m_Doc.CreateElement("typelib", UrnAsmv1);
					elem.SetAttribute("tlbid", string.Format("{{{0}}}", libAttr.guid));
					elem.SetAttribute("version", string.Format("{0}.{1}", libAttr.wMajorVerNum,
						libAttr.wMinorVerNum));
					elem.SetAttribute("helpdir", string.Empty);
					elem.SetAttribute("resourceid", "0");
					elem.SetAttribute("flags", flags);
					oldChild = file.SelectSingleNode(string.Format("typelib[tlbid='{{{0}}}']",
						libAttr.guid));
					if (oldChild != null)
						file.ReplaceChild(elem, oldChild);
					else
						file.AppendChild(elem);
				}

				Debug.WriteLine(string.Format(@"typelib tlbid=""{0}"" version=""{1}.{2}"" helpdir="""" resourceid=""0"" flags=""{3}""",
					libAttr.guid, libAttr.wMajorVerNum, libAttr.wMinorVerNum, flags));

				int count = typeLib.GetTypeInfoCount();
				for (int i = 0; i < count; i++)
				{
					ITypeInfo typeInfo;
					typeLib.GetTypeInfo(i, out typeInfo);

					ProcessTypeInfo(parent, libAttr.guid, typeInfo);
				}

				oldChild = parent.SelectSingleNode(string.Format("file[name='{0}']",
					Path.GetFileName(fileName)));
				if (oldChild != null)
					parent.ReplaceChild(file, oldChild);
				else
					parent.AppendChild(file);
			}
			catch (System.Runtime.InteropServices.COMException)
			{
				// just ignore if this isn't a type library
				if (m_fDisplayWarnings)
					Console.WriteLine("Warning: Can't load type library {0}", fileName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the fragment to the manifest.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="elementName">Name of the element.</param>
		/// ------------------------------------------------------------------------------------
		private void AddFragmentInternal(XmlNode parent, string fileName, string elementName)
		{
			if (!File.Exists(fileName))
			{
				if (m_fDisplayWarnings)
					Console.WriteLine("Warning: Can't add fragment {0}", fileName);
				return;
			}

			var fragmentManifest = new XmlDocument { PreserveWhitespace = true };

			using (var reader = new XmlTextReader(fileName))
			{
				reader.WhitespaceHandling = WhitespaceHandling.Significant;
				reader.MoveToContent();
				fragmentManifest.Load(reader);
				XmlNode root = fragmentManifest.DocumentElement;
				if (root == null)
					return;

				// I couldn't get it to work with root.SelectNodes so I loop over all children
				// and copy the relevant ones
				//foreach (XmlNode fragment in root.SelectNodes("file"))
				//    parent.AppendChild(fragment);
				foreach (XmlNode fragment in root.ChildNodes)
				{
					if (elementName == null || fragment.Name == elementName)
						parent.AppendChild(parent.OwnerDocument.ImportNode(fragment, true));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the fragment.
		/// </summary>
		/// <param name="parent">The parent node.</param>
		/// <param name="fileName">Name (and path) of the fragment manifest file.</param>
		/// ------------------------------------------------------------------------------------
		public void AddFragment(XmlElement parent, string fileName)
		{
			AddFragmentInternal(parent, fileName, "file");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a fragment "as is".
		/// </summary>
		/// <param name="parent">The parent node.</param>
		/// <param name="fileName">Name (and path) of the fragment manifest file.</param>
		/// ------------------------------------------------------------------------------------
		public void AddAsIs(XmlElement parent, string fileName)
		{
			AddFragmentInternal(parent, fileName, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes one type info. We get the necessary information from the type library
		/// and also from the registry.
		/// </summary>
		/// <param name="parent">The parent element.</param>
		/// <param name="tlbGuid">The guid of the type library.</param>
		/// <param name="typeInfo">The type info.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessTypeInfo(XmlNode parent, Guid tlbGuid, ITypeInfo typeInfo)
		{
// ReSharper disable EmptyGeneralCatchClause
			try
			{
				IntPtr pTypeAttr;
				typeInfo.GetTypeAttr(out pTypeAttr);
				var typeAttr = (TYPEATTR)
					System.Runtime.InteropServices.Marshal.PtrToStructure(pTypeAttr, typeof(TYPEATTR));
				typeInfo.ReleaseTypeAttr(pTypeAttr);
				if (typeAttr.typekind == TYPEKIND.TKIND_COCLASS)
				{
					string keyString = string.Format(@"CLSID\{{{0}}}", typeAttr.guid);
					RegistryKey typeKey = Registry.ClassesRoot.OpenSubKey(keyString);
					if (typeKey == null)
						return;

					RegistryKey inprocServer = typeKey.OpenSubKey("InprocServer32");
					if (inprocServer == null)
						return;

					// Try to get the file element for the server
					var bldr = new StringBuilder(255);
					GetLongPathName((string)inprocServer.GetValue(null), bldr, 255);
					string serverFullPath = bldr.ToString();
					string server = Path.GetFileName(serverFullPath);
					if (!File.Exists(serverFullPath) &&
						!File.Exists(Path.Combine(m_BaseDirectory, server)))
					{
						if (!m_NonExistingServers.Contains(server))
						{
							Console.WriteLine("{0} is referenced in the TLB but is not in current directory", server);
							m_NonExistingServers.Add(server);
						}
						return;
					}

					XmlElement file = GetOrCreateFileNode(parent, server);
					//// Check to see that the DLL we're processing is really the DLL that can
					//// create this class. Otherwise we better not claim that we know how to do it!
					//if (keyString == null || keyString == string.Empty ||
					//    server.ToLower() != Path.GetFileName(m_FileName))
					//{
					//    return;
					//}

					var threadingModel = (string)inprocServer.GetValue("ThreadingModel");
					var progIdKey = typeKey.OpenSubKey("ProgID");
					if (progIdKey == null)
						return;
					var progId = (string)progIdKey.GetValue(null);

					XmlElement elem;
					if (!m_CoClasses.ContainsKey(progId))
					{
						// <comClass clsid="{2f0fccc2-c160-11d3-8da2-005004defec4}" threadingModel="Apartment"
						//		tlbid="{2f0fccc0-c160-11d3-8da2-005004defec4}" progid="FieldWorks.FwXmlData" />
						elem = m_Doc.CreateElement("comClass", UrnAsmv1);
						elem.SetAttribute("clsid", string.Format("{{{0}}}", typeAttr.guid));
						elem.SetAttribute("threadingModel", threadingModel);
						elem.SetAttribute("tlbid", string.Format("{{{0}}}", tlbGuid));
						elem.SetAttribute("progid", progId);
						m_CoClasses.Add(progId, elem);
						XmlNode oldChild = file.SelectSingleNode(string.Format("comClass[clsid='{{{0}}}']",
							typeAttr.guid));
						if (oldChild != null)
							file.ReplaceChild(elem, oldChild);
						else
							file.AppendChild(elem);
					}

					Debug.WriteLine(string.Format(@"Coclass: clsid=""{0}"", threadingModel=""{1}"", tlbid=""{2}"", progid=""{3}""",
						typeAttr.guid, threadingModel, tlbGuid, progId));

				}
			}
			catch
			{
				// just ignore any errors
			}
// ReSharper restore EmptyGeneralCatchClause
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the or create file node.
		/// </summary>
		/// <param name="parent">The parent node.</param>
		/// <param name="filePath">The file name with full path.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private XmlElement GetOrCreateFileNode(XmlNode parent, string filePath)
		{
			string fileName = Path.GetFileName(filePath);
			if (m_Files.ContainsKey(fileName))
				return m_Files[fileName];

			var fileInfo = new FileInfo(filePath);
			if (!fileInfo.Exists)
				fileInfo = new FileInfo(Path.Combine(m_BaseDirectory, fileName));
			XmlElement file = m_Doc.CreateElement("file", UrnAsmv1);
			file.SetAttribute("name", fileName);
			if (fileInfo.Exists)
			{
				parent.AppendChild(file);
				file.SetAttribute("size", "urn:schemas-microsoft-com:asm.v2", fileInfo.Length.ToString());
			}
			m_Files.Add(fileName, file);
			return file;
		}

		#region Registry redirection
// ReSharper disable InconsistentNaming
		private static readonly UIntPtr HKEY_CLASSES_ROOT = new UIntPtr(0x80000000);
		private static readonly UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001);
		//private static readonly UIntPtr HKEY_LOCAL_MACHINE = new UIntPtr(0x80000002);
// ReSharper restore InconsistentNaming

		[System.Runtime.InteropServices.DllImport("Advapi32.dll")]
		private extern static int RegOverridePredefKey(UIntPtr hKey, UIntPtr hNewKey);

		[System.Runtime.InteropServices.DllImport("Advapi32.dll")]
		private extern static int RegCreateKey(UIntPtr hKey, string lpSubKey, out UIntPtr phkResult);

		[System.Runtime.InteropServices.DllImport("Advapi32.dll")]
		private extern static int RegCloseKey(UIntPtr hKey);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Temporarily redirects access to HKCR to a subkey under HKCU.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RedirectRegistry()
		{
			try
			{
				UIntPtr hKey;
				RegCreateKey(HKEY_CURRENT_USER, @"Software\SIL\NAntBuild\tmp", out hKey);
				RegOverridePredefKey(HKEY_CLASSES_ROOT, hKey);
				RegCloseKey(hKey);

				// We also have to create a CLSID subkey - some DLLs expect that it exists
				Registry.CurrentUser.CreateSubKey(@"Software\SIL\NAntBuild\tmp\CLSID");
			}
			catch
			{
				m_fRedirectRegistryFailed = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ends the redirection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void EndRedirection()
		{
			RegOverridePredefKey(HKEY_CLASSES_ROOT, UIntPtr.Zero);
		}
		#endregion

		#region Imported methods to register dll

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
		[System.Runtime.InteropServices.DllImport("Kernel32.dll",
			CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
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
		[System.Runtime.InteropServices.DllImport("Kernel32.dll", SetLastError = true)]
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
		[System.Runtime.InteropServices.DllImport("Kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

		// The delegate for DllRegisterServer.
		[return: System.Runtime.InteropServices.MarshalAs(
			System.Runtime.InteropServices.UnmanagedType.Error)]
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

				System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(
					method, typeof(DllRegisterServerFunction)).DynamicInvoke();
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
		public void Register(string fileName)
		{
			ApiInvoke(fileName, "DllRegisterServer");
// ReSharper disable EmptyGeneralCatchClause
			try
			{
				ITypeLib typeLib = LoadTypeLib(fileName);
				RegisterTypeLib(typeLib, fileName, null);
			}
			catch
			{
				// just ignore any errors
			}
// ReSharper restore EmptyGeneralCatchClause
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unregisters the specified file.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// ------------------------------------------------------------------------------------
		public void Unregister(string fileName)
		{
			ApiInvoke(fileName, "DllUnregisterServer");
		}

	}
}
