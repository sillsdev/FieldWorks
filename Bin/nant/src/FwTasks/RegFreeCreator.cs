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
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml;
using Microsoft.Win32;
using LIBFLAGS=System.Runtime.InteropServices.ComTypes.LIBFLAGS;
using TYPEATTR=System.Runtime.InteropServices.ComTypes.TYPEATTR;
using TYPEKIND=System.Runtime.InteropServices.ComTypes.TYPEKIND;
using TYPELIBATTR=System.Runtime.InteropServices.ComTypes.TYPELIBATTR;

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
		[DllImport("oleaut32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
		private static extern ITypeLib LoadTypeLib(string szFile);

		[DllImport("oleaut32.dll")]
		private static extern int RegisterTypeLib(ITypeLib typeLib, string fullPath, string helpDir);

		[DllImport("kernel32.dll")]
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
		private readonly Dictionary<string, XmlElement> m_InterfaceProxies = new Dictionary<string, XmlElement>();
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
					Registry.CurrentUser.DeleteSubKeyTree(TmpRegistryKey);
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
		///		<item>type (hard coded as "win32" for now)</item>
		/// </list>
		/// This method also adds the root element with all necessary namespaces.
		/// </summary>
		/// <param name="pathName">pathname of the file.</param>
		/// ------------------------------------------------------------------------------------
		public XmlElement CreateExeInfo(string pathName)
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

			// The C++ test programs won't run if an assemblyIdentity element exists.
			string fileName = Path.GetFileName(pathName);
			if (!fileName.StartsWith("test"))
			{
				// <assemblyIdentity name="TE.exe" version="1.4.1.39149" type="win32" />
				XmlElement assemblyIdentity = m_Doc.CreateElement("assemblyIdentity", UrnAsmv1);
				assemblyIdentity.SetAttribute("name", fileName);
				// ReSharper disable EmptyGeneralCatchClause
				try
				{
					FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(pathName);
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
			}
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
					Marshal.PtrToStructure(pLibAttr, typeof(TYPELIBATTR));
				typeLib.ReleaseTLibAttr(pLibAttr);

				string flags = string.Empty;
				if ((libAttr.wLibFlags & LIBFLAGS.LIBFLAG_FHASDISKIMAGE) == LIBFLAGS.LIBFLAG_FHASDISKIMAGE)
					flags = "HASDISKIMAGE";

				// <file name="FwKernel.dll" asmv2:size="1507328">
				var file = GetOrCreateFileNode(parent, fileName);

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
					elem.SetAttribute("tlbid", libAttr.guid.ToString("B"));
					elem.SetAttribute("version", string.Format("{0}.{1}", libAttr.wMajorVerNum,
						libAttr.wMinorVerNum));
					elem.SetAttribute("helpdir", string.Empty);
					elem.SetAttribute("resourceid", "0");
					elem.SetAttribute("flags", flags);
					oldChild = file.SelectSingleNode(string.Format("typelib[tlbid='{0}']",
						libAttr.guid.ToString("B")));
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
			catch (COMException)
			{
				// just ignore if this isn't a type library
				if (m_fDisplayWarnings)
					Console.WriteLine("Warning: Can't load type library {0}", fileName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default value for a registry key.
		/// </summary>
		/// <param name="parentKey">The parent key.</param>
		/// <param name="keyName">Name of the child key.</param>
		/// <returns>The default value of the child key, or empty string if child key doesn't
		/// exist.</returns>
		/// ------------------------------------------------------------------------------------
		private static string GetDefaultValueForKey(RegistryKey parentKey, string keyName)
		{
			string retVal = string.Empty;
			using (var childKey = parentKey.OpenSubKey(keyName))
			{
				if (childKey != null)
					retVal = (string)childKey.GetValue(string.Empty);
			}
			return retVal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the classes under CLSID. This is mainly done so that we get the proxy
		/// classes. The other classes are already processed through the type lib.
		/// </summary>
		/// <param name="parent">The parent node.</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessClasses(XmlElement parent)
		{
			using (var regKeyClsid = Registry.ClassesRoot.OpenSubKey("CLSID"))
			{
				if (regKeyClsid == null)
					return;

				foreach (var clsId in regKeyClsid.GetSubKeyNames())
				{
					if (m_CoClasses.ContainsKey(clsId.ToLower()))
						continue;

					using (var regKeyClass = regKeyClsid.OpenSubKey(clsId))
					{
						var className = (string)regKeyClass.GetValue(string.Empty, string.Empty);
						using (var regKeyInProcServer = regKeyClass.OpenSubKey("InProcServer32"))
						{
							if (regKeyInProcServer == null)
								continue;
							var serverPath = (string)regKeyInProcServer.GetValue(string.Empty, string.Empty);
							var threadingModel = (string)regKeyInProcServer.GetValue("ThreadingModel", string.Empty);

							// <file name="FwKernel.dll" asmv2:size="1507328">
							XmlElement file = GetOrCreateFileNode(parent, serverPath);
							AddOrReplaceCoClass(file, clsId, threadingModel, className, null, null);
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the interfaces found under our temporary registry key.
		/// </summary>
		/// <param name="root">The parent node.</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessInterfaces(XmlElement root)
		{
			using (var regKeyInterfaces = Registry.ClassesRoot.OpenSubKey("Interface"))
			{
				if (regKeyInterfaces == null)
					return;

				foreach (var iid in regKeyInterfaces.GetSubKeyNames())
				{
					var interfaceIid = iid.ToLower();
					using (var regKeyInterface = regKeyInterfaces.OpenSubKey(iid))
					{
						var interfaceName = (string)regKeyInterface.GetValue(string.Empty, string.Empty);
						var numMethods = GetDefaultValueForKey(regKeyInterface, "NumMethods");
						var proxyStubClsId = GetDefaultValueForKey(regKeyInterface, "ProxyStubClsId32").ToLower();
						if (string.IsNullOrEmpty(proxyStubClsId))
						{
							Console.WriteLine("Error: no proxyStubClsid32 set for interface with iid {0}", interfaceIid);
							continue;
						}
						Debug.WriteLine(string.Format("Interface {0} is {1}: {2} methods, proxy: {3}", interfaceIid,
							interfaceName, numMethods, proxyStubClsId));

						if (!m_CoClasses.ContainsKey(proxyStubClsId))
						{
							Console.WriteLine("Warning: can't find coclass specified as proxy for interface with iid {0}; manifest might not work",
								interfaceIid);
						}

						if (m_InterfaceProxies.ContainsKey(interfaceIid))
						{
							Console.WriteLine("Internal error: encountered interface with iid {0} before", interfaceIid);
							continue;
						}

						// The MSDN documentation isn't very clear here, but we have to add a
						// comInterfaceExternalProxyStub even when the proxy is merged into
						// the implementing assembly, otherwise we won't be able to start the
						// application.
						// <comInterfaceExternalProxyStub name="IVwPrintContext" iid="{FF2E1DC2-95A8-41C6-85F4-FFCA3A64216A}"
						//		numMethods="24" proxyStubClsid32="{EFEBBD00-D418-4157-A730-C648BFFF3D8D}"/>
						var elem = m_Doc.CreateElement("comInterfaceExternalProxyStub", UrnAsmv1);
						elem.SetAttribute("iid", interfaceIid);
						elem.SetAttribute("proxyStubClsid32", proxyStubClsId);
						if (!string.IsNullOrEmpty(interfaceName))
							elem.SetAttribute("name", interfaceName);
						if (!string.IsNullOrEmpty(numMethods))
							elem.SetAttribute("numMethods", numMethods);

						AppendOrReplaceNode(root, elem, "iid", interfaceIid);

						m_InterfaceProxies.Add(interfaceIid, elem);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the child node. This is similar to XmlNode.SelectSingleNode which has the draw-
		/// back that it doesn't work "live".
		/// </summary>
		/// <param name="parentNode">The parent node.</param>
		/// <param name="childName">Name of the child.</param>
		/// <returns>The child node, or <c>null</c> if no child with name
		/// <paramref name="childName"/> exists.</returns>
		/// ------------------------------------------------------------------------------------
		private static XmlNode GetChildNode(XmlNode parentNode, string childName)
		{
			return parentNode.ChildNodes.Cast<XmlNode>().FirstOrDefault(child => child.Name == childName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the child node with name <paramref name="childName"/> which has an
		/// <paramref name="attribute"/> with value <paramref name="attrValue"/>.
		/// This is similar to XmlNode.SelectSingleNode which has the draw-back that it doesn't
		/// work "live".
		/// </summary>
		/// <param name="parentNode">The parent node.</param>
		/// <param name="childName">Name of the child.</param>
		/// <param name="attribute">The attribute.</param>
		/// <param name="attrValue">The attribute value.</param>
		/// <returns>
		/// The child node, or <c>null</c> if no child with name
		/// <paramref name="childName"/> exists.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private static XmlNode GetChildNode(XmlNode parentNode, string childName,
			string attribute, string attrValue)
		{
			return parentNode.ChildNodes.Cast<XmlNode>().FirstOrDefault(
				child => child.Name == childName && child.Attributes != null &&
					child.Attributes[attribute].Value == attrValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends or replaces a node.
		/// </summary>
		/// <param name="parentNode">The parent node.</param>
		/// <param name="childElement">The child element.</param>
		/// <param name="attribute">The attribute.</param>
		/// <param name="attrValue">The attribute value.</param>
		/// ------------------------------------------------------------------------------------
		private static void AppendOrReplaceNode(XmlNode parentNode, XmlNode childElement,
			string attribute, string attrValue)
		{
			var oldChild = GetChildNode(parentNode, childElement.Name, attribute, attrValue);
			if (oldChild != null)
				parentNode.ReplaceChild(childElement, oldChild);
			else
				parentNode.AppendChild(childElement);
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
				var typeAttr = (TYPEATTR)Marshal.PtrToStructure(pTypeAttr, typeof(TYPEATTR));
				typeInfo.ReleaseTypeAttr(pTypeAttr);
				if (typeAttr.typekind == TYPEKIND.TKIND_COCLASS)
				{
					var clsId = typeAttr.guid.ToString("B");
					string keyString = string.Format(@"CLSID\{0}", clsId);
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

					if (!m_CoClasses.ContainsKey(clsId))
					{
						var description = (string)typeKey.GetValue(string.Empty);
						var threadingModel = (string)inprocServer.GetValue("ThreadingModel");
						var progId = GetDefaultValueForKey(typeKey, "ProgID");
						AddOrReplaceCoClass(file, clsId, threadingModel, description, tlbGuid.ToString("B"), progId);
						Debug.WriteLine(string.Format(@"Coclass: clsid=""{0}"", threadingModel=""{1}"", tlbid=""{2}"", progid=""{3}""",
							clsId, threadingModel, tlbGuid, progId));
					}
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
		/// Adds a comClass element.
		/// </summary>
		/// <param name="parent">The parent file node.</param>
		/// <param name="clsId">The CLSID string.</param>
		/// <param name="threadingModel">The threading model.</param>
		/// <param name="description">The description (name) of the class (might be <c>null</c>).</param>
		/// <param name="tlbId">The type library id (might be <c>null</c>).</param>
		/// <param name="progId">The prog id (might be <c>null</c>).</param>
		/// ------------------------------------------------------------------------------------
		private void AddOrReplaceCoClass(XmlElement parent, string clsId, string threadingModel,
			string description, string tlbId, string progId)
		{
			Debug.Assert(clsId.StartsWith("{"));
			Debug.Assert(string.IsNullOrEmpty(tlbId) || tlbId.StartsWith("{"));

			clsId = clsId.ToLower();

			// <comClass clsid="{2f0fccc2-c160-11d3-8da2-005004defec4}" threadingModel="Apartment"
			//		tlbid="{2f0fccc0-c160-11d3-8da2-005004defec4}" progid="FieldWorks.FwXmlData" />
			var elem = m_Doc.CreateElement("comClass", UrnAsmv1);
			elem.SetAttribute("clsid", clsId);
			elem.SetAttribute("threadingModel", threadingModel);
			if (!string.IsNullOrEmpty(description))
				elem.SetAttribute("description", description);
			if (!string.IsNullOrEmpty(tlbId))
				elem.SetAttribute("tlbid", tlbId.ToLower());
			if (!string.IsNullOrEmpty(progId))
				elem.SetAttribute("progid", progId);
			m_CoClasses[clsId] = elem;
			AppendOrReplaceNode(parent, elem, "clsid", clsId);
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

		[DllImport("Advapi32.dll")]
		private extern static int RegOverridePredefKey(UIntPtr hKey, UIntPtr hNewKey);

		[DllImport("Advapi32.dll")]
		private extern static int RegCreateKey(UIntPtr hKey, string lpSubKey, out UIntPtr phkResult);

		[DllImport("Advapi32.dll")]
		private extern static int RegCloseKey(UIntPtr hKey);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the a temporary registry key to register dlls. This registry key is process
		/// specific, so multiple instances can run at the same time without interfering with
		/// each other.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string TmpRegistryKey
		{
			get
			{
				return string.Format(@"Software\SIL\NAntBuild\tmp-{0}",
					Process.GetCurrentProcess().Id);
			}
		}
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
				RegCreateKey(HKEY_CURRENT_USER, TmpRegistryKey, out hKey);
				RegOverridePredefKey(HKEY_CLASSES_ROOT, hKey);
				RegCloseKey(hKey);

				// We also have to create a CLSID subkey - some DLLs expect that it exists
				Registry.CurrentUser.CreateSubKey(TmpRegistryKey + @"\CLSID");
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
			SetDllDirectory(null);
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
		public void Register(string fileName)
		{
			SetDllDirectory(Path.GetDirectoryName(fileName));
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
