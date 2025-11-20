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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Win32;
using LIBFLAGS = System.Runtime.InteropServices.ComTypes.LIBFLAGS;
using TYPEATTR = System.Runtime.InteropServices.ComTypes.TYPEATTR;
using TYPEKIND = System.Runtime.InteropServices.ComTypes.TYPEKIND;
using TYPELIBATTR = System.Runtime.InteropServices.ComTypes.TYPELIBATTR;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Creates the necessary entries that allows later to use an assembly without registering
	/// it.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RegFreeCreator
	{
		#region Member variables
		/// <summary>The directory where the type library resides</summary>
		private string _baseDirectory;
		private string _fileName;
		private readonly XmlDocument _doc;
		public TaskLoggingHelper _log;
		private readonly Dictionary<string, XmlElement> _files =
			new Dictionary<string, XmlElement>();
		private readonly Dictionary<string, XmlElement> _coClasses =
			new Dictionary<string, XmlElement>();
		private readonly Dictionary<string, XmlElement> _interfaceProxies =
			new Dictionary<string, XmlElement>();
		private readonly Dictionary<Guid, string> _tlbGuids = new Dictionary<Guid, string>();
		private readonly List<string> _nonExistingServers = new List<string>();
		private readonly XmlNamespaceManager _nsManager;

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
			_doc = doc;
			_nsManager = CreateNamespaceManager(_doc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RegFreeCreator"/> class.
		/// </summary>
		/// <param name="doc">The XML document.</param>
		/// <param name="log"></param>
		/// ------------------------------------------------------------------------------------
		public RegFreeCreator(XmlDocument doc, TaskLoggingHelper log)
			: this(doc)
		{
			_log = log;
		}

		#endregion

		private static XmlNamespaceManager CreateNamespaceManager(XmlDocument doc)
		{
			var namespaceManager = new XmlNamespaceManager(doc.NameTable);
			namespaceManager.AddNamespace("asmv1", UrnAsmv1);
			namespaceManager.AddNamespace("asmv2", UrnAsmv2);
			namespaceManager.AddNamespace("dsig", UrnDsig);
			namespaceManager.AddNamespace("xsi", UrnSchema);
			return namespaceManager;
		}

		///  ------------------------------------------------------------------------------------
		///  <summary>
		///  Creates the info for the executable. The info consist of:
		///  <list type="bullet">
		/// 		<item>name (from file name)</item>
		/// 		<item>version info (from assembly)</item>
		/// 		<item>type (win64 for x64, win32 for x86)</item>
		/// 		<item>processorArchitecture (amd64 for x64, x86 for x86)</item>
		///  </list>
		///  This method also adds the root element with all necessary namespaces.
		///  </summary>
		/// ------------------------------------------------------------------------------------
		public XmlElement CreateExeInfo(
			string assemblyName,
			string assemblyVersion,
			string Platform
		)
		{
			XmlElement elem = _doc.CreateElement("assembly", UrnAsmv1);
			elem.SetAttribute("manifestVersion", "1.0");
			elem.SetAttribute("xmlns:asmv1", UrnAsmv1);
			elem.SetAttribute("xmlns:asmv2", UrnAsmv2);
			elem.SetAttribute("xmlns:dsig", UrnDsig);
			elem.SetAttribute("xmlns:xsi", UrnSchema);
			elem.SetAttribute("schemaLocation", UrnSchema, UrnAsmv1 + " assembly.adaptive.xsd");

			XmlNode oldChild = _doc.SelectSingleNode("asmv1:assembly", _nsManager);
			if (oldChild != null)
				_doc.ReplaceChild(elem, oldChild);
			else
				_doc.AppendChild(elem);

			if (!string.IsNullOrEmpty(assemblyName))
			{
				// Determine proper manifest type and processor architecture for 64-bit builds
				string manifestType = "x64".Equals(Platform, StringComparison.OrdinalIgnoreCase)
					? "win64"
					: "win32";
				string processorArch = "x64".Equals(Platform, StringComparison.OrdinalIgnoreCase)
					? "amd64"
					: "x86";

				// <assemblyIdentity name="TE.exe" version="1.4.1.39149" type="win64" processorArchitecture="amd64" />
				XmlElement assemblyIdentity = _doc.CreateElement("assemblyIdentity", UrnAsmv1);
				assemblyIdentity.SetAttribute("name", assemblyName);
				assemblyIdentity.SetAttribute("version", assemblyVersion);
				assemblyIdentity.SetAttribute("type", manifestType);
				assemblyIdentity.SetAttribute("processorArchitecture", processorArch);

				oldChild = elem.SelectSingleNode("asmv1:assemblyIdentity", _nsManager);
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
			_baseDirectory = Path.GetDirectoryName(fileName);
			_fileName = fileName.ToLower();

			try
			{
				_log.LogMessage(MessageImportance.Low, "\tProcessing type library {0}", fileName);
				XmlNode oldChild;
				ITypeLib typeLib;
				RegHelper.LoadTypeLib(_fileName, out typeLib);
				IntPtr pLibAttr;
				typeLib.GetLibAttr(out pLibAttr);
				var libAttr = (TYPELIBATTR)Marshal.PtrToStructure(pLibAttr, typeof(TYPELIBATTR));
				typeLib.ReleaseTLibAttr(pLibAttr);

				string flags = string.Empty;
				if (
					(libAttr.wLibFlags & LIBFLAGS.LIBFLAG_FHASDISKIMAGE)
					== LIBFLAGS.LIBFLAG_FHASDISKIMAGE
				)
					flags = "HASDISKIMAGE";

				// <file name="FwKernel.dll" asmv2:size="1507328">
				var file = GetOrCreateFileNode(parent, fileName);

				// <typelib tlbid="{2f0fccc0-c160-11d3-8da2-005004defec4}" version="1.0" helpdir=""
				//		resourceid="0" flags="HASDISKIMAGE" />
				if (_tlbGuids.ContainsKey(libAttr.guid))
				{
					_log.LogWarning(
						"Type library with GUID {0} is defined in {1} and {2}",
						libAttr.guid,
						_tlbGuids[libAttr.guid],
						Path.GetFileName(fileName)
					);
				}
				else
				{
					_tlbGuids.Add(libAttr.guid, Path.GetFileName(fileName));
					XmlElement elem = _doc.CreateElement("typelib", UrnAsmv1);
					elem.SetAttribute("tlbid", libAttr.guid.ToString("B"));
					elem.SetAttribute(
						"version",
						string.Format("{0}.{1}", libAttr.wMajorVerNum, libAttr.wMinorVerNum)
					);
					elem.SetAttribute("helpdir", string.Empty);
					elem.SetAttribute("resourceid", "0");
					elem.SetAttribute("flags", flags);
					oldChild = file.SelectSingleNode(
						string.Format(
							"asmv1:typelib[asmv1:tlbid='{0}']",
							libAttr.guid.ToString("B")
						),
						_nsManager
					);
					if (oldChild != null)
						file.ReplaceChild(elem, oldChild);
					else
						file.AppendChild(elem);
				}

				Debug.WriteLine(
					@"typelib tlbid=""{0}"" version=""{1}.{2}"" helpdir="""" resourceid=""0"" flags=""{3}""",
					libAttr.guid,
					libAttr.wMajorVerNum,
					libAttr.wMinorVerNum,
					flags
				);

				int count = typeLib.GetTypeInfoCount();
				_log.LogMessage(MessageImportance.Low, "\t\tTypelib has {0} types", count);
				for (int i = 0; i < count; i++)
				{
					ITypeInfo typeInfo;
					typeLib.GetTypeInfo(i, out typeInfo);

					ProcessTypeInfo(parent, libAttr.guid, typeInfo);
				}

				oldChild = parent.SelectSingleNode(
					string.Format("asmv1:file[asmv1:name='{0}']", Path.GetFileName(fileName)),
					_nsManager
				);
				if (oldChild != null)
					parent.ReplaceChild(file, oldChild);
				else
					parent.AppendChild(file);
			}
			catch (Exception)
			{
				// just ignore if this isn't a type library
				_log.LogMessage(
					MessageImportance.Normal,
					"Can't load type library {0}",
					fileName
				);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes a managed assembly to find COM-visible classes and add clrClass elements.
		/// </summary>
		/// <param name="parent">The parent node.</param>
		/// <param name="fileName">Name (and path) of the file.</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessManagedAssembly(XmlElement parent, string fileName)
		{
			_baseDirectory = Path.GetDirectoryName(fileName);
			_fileName = fileName.ToLower();

			try
			{
				_log.LogMessage(
					MessageImportance.Low,
					"\tProcessing managed assembly {0}",
					fileName
				);

				// Use System.Reflection.Metadata to avoid locking the file and to handle 32/64 bit mismatches
				using (
					var fs = new FileStream(
						fileName,
						FileMode.Open,
						FileAccess.Read,
						FileShare.ReadWrite
					)
				)
				using (var peReader = new PEReader(fs))
				{
					if (!peReader.HasMetadata)
						return;

					var reader = peReader.GetMetadataReader();
					var file = GetOrCreateFileNode(parent, fileName);
					string runtimeVersion = reader.MetadataVersion;

					// Check Assembly-level ComVisible
					bool asmComVisible = true;
					// Default is true, but check if [assembly: ComVisible(false)] is present
					if (
						TryGetComVisible(
							reader,
							reader.GetAssemblyDefinition().GetCustomAttributes(),
							out bool val
						)
					)
					{
						asmComVisible = val;
					}

					foreach (var typeHandle in reader.TypeDefinitions)
					{
						var typeDef = reader.GetTypeDefinition(typeHandle);
						var attributes = typeDef.Attributes;

						// Skip if not a class (Class is 0, so check it's not Interface)
						if ((attributes & TypeAttributes.Interface) != 0)
							continue;

						// Skip abstract
						if ((attributes & TypeAttributes.Abstract) != 0)
							continue;

						// Check visibility (Public only for now, skipping NestedPublic to avoid complexity)
						var visibility = attributes & TypeAttributes.VisibilityMask;
						if (visibility != TypeAttributes.Public)
							continue;

						// Check ComVisible
						bool isComVisible = asmComVisible;
						if (
							TryGetComVisible(
								reader,
								typeDef.GetCustomAttributes(),
								out bool typeVal
							)
						)
						{
							isComVisible = typeVal;
						}

						if (!isComVisible)
							continue;

						// Check for Guid
						string clsId = GetAttributeStringValue(
							reader,
							typeDef.GetCustomAttributes(),
							"System.Runtime.InteropServices.GuidAttribute"
						);
						if (string.IsNullOrEmpty(clsId))
							continue;

						clsId = "{" + clsId + "}";

						// Check for ProgId
						string progId = GetAttributeStringValue(
							reader,
							typeDef.GetCustomAttributes(),
							"System.Runtime.InteropServices.ProgIdAttribute"
						);

						string typeName = GetFullTypeName(reader, typeDef);

						AddOrReplaceClrClass(
							file,
							clsId,
							"Both",
							typeName,
							progId,
							runtimeVersion
						);

						_log.LogMessage(
							MessageImportance.Low,
							string.Format(
								@"ClrClass: clsid=""{0}"", name=""{1}"", progid=""{2}""",
								clsId,
								typeName,
								progId
							)
						);
					}
				}
			}
			catch (Exception ex)
			{
				_log.LogWarning(
					"Failed to process managed assembly {0}: {1}",
					fileName,
					ex.Message
				);
			}
		}

		private bool TryGetComVisible(
			MetadataReader reader,
			CustomAttributeHandleCollection attributes,
			out bool value
		)
		{
			value = true;
			foreach (var handle in attributes)
			{
				var attr = reader.GetCustomAttribute(handle);
				if (
					IsAttribute(
						reader,
						attr,
						"System.Runtime.InteropServices.ComVisibleAttribute"
					)
				)
				{
					var blobReader = reader.GetBlobReader(attr.Value);
					if (blobReader.Length >= 5) // Prolog (2) + bool (1) + NamedArgs (2)
					{
						blobReader.ReadUInt16(); // Prolog 0x0001
						value = blobReader.ReadBoolean();
						return true;
					}
				}
			}
			return false;
		}

		private string GetAttributeStringValue(
			MetadataReader reader,
			CustomAttributeHandleCollection attributes,
			string attrName
		)
		{
			foreach (var handle in attributes)
			{
				var attr = reader.GetCustomAttribute(handle);
				if (IsAttribute(reader, attr, attrName))
				{
					var blobReader = reader.GetBlobReader(attr.Value);
					if (blobReader.Length > 4)
					{
						blobReader.ReadUInt16(); // Prolog
						return blobReader.ReadSerializedString();
					}
				}
			}
			return null;
		}

		private bool IsAttribute(MetadataReader reader, CustomAttribute attr, string fullName)
		{
			if (attr.Constructor.Kind == HandleKind.MemberReference)
			{
				var memberRef = reader.GetMemberReference(
					(MemberReferenceHandle)attr.Constructor
				);
				if (memberRef.Parent.Kind == HandleKind.TypeReference)
				{
					var typeRef = reader.GetTypeReference((TypeReferenceHandle)memberRef.Parent);
					return GetFullTypeName(reader, typeRef) == fullName;
				}
			}
			else if (attr.Constructor.Kind == HandleKind.MethodDefinition)
			{
				var methodDef = reader.GetMethodDefinition(
					(MethodDefinitionHandle)attr.Constructor
				);
				var typeDef = reader.GetTypeDefinition(methodDef.GetDeclaringType());
				return GetFullTypeName(reader, typeDef) == fullName;
			}
			return false;
		}

		private string GetFullTypeName(MetadataReader reader, TypeDefinition typeDef)
		{
			string ns = reader.GetString(typeDef.Namespace);
			string name = reader.GetString(typeDef.Name);
			return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
		}

		private string GetFullTypeName(MetadataReader reader, TypeReference typeRef)
		{
			string ns = reader.GetString(typeRef.Namespace);
			string name = reader.GetString(typeRef.Name);
			return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
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
		/// Processes the classes under CLSID. This reads from HKEY_CLASSES_ROOT directly
		/// (where FieldWorks COM classes are already registered). Fallback to defaults if not found.
		/// This is mainly done so that we get the proxy classes. The other classes are already
		/// processed through the type lib.
		/// </summary>
		/// <param name="parent">The parent node.</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessClasses(XmlElement parent)
		{
			// Process classes that were already found in type libraries
			// Read additional metadata from HKCR if available
			foreach (var kvp in _coClasses.ToList())
			{
				var clsId = kvp.Key;
				try
				{
					using (var regKeyClass = Registry.ClassesRoot.OpenSubKey($"CLSID\\{clsId}"))
					{
						if (regKeyClass != null)
						{
							// Try to get ProgID from registry
							var progId = GetDefaultValueForKey(regKeyClass, "ProgID");

							// Try to get threading model from InprocServer32 subkey
							using (
								var regKeyInProcServer = regKeyClass.OpenSubKey("InprocServer32")
							)
							{
								if (regKeyInProcServer != null)
								{
									var threadingModel = (string)
										regKeyInProcServer.GetValue(
											"ThreadingModel",
											"Apartment"
										);

									_log.LogMessage(
										MessageImportance.Low,
										"Updated CLSID {0} from HKCR: ProgID={1}, ThreadingModel={2}",
										clsId,
										progId ?? "(none)",
										threadingModel
									);

									// Note: The comClass element was already added by ProcessTypeLibrary
									// We're just logging that we could read from HKCR successfully
								}
							}
						}
						else
						{
							_log.LogMessage(
								MessageImportance.Low,
								"CLSID {0} not found in HKCR, using type library defaults",
								clsId
							);
						}
					}
				}
				catch (Exception ex)
				{
					_log.LogMessage(
						MessageImportance.Low,
						"Cannot read CLSID {0} from registry: {1}",
						clsId,
						ex.Message
					);
					// Continue with defaults from type library
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the interfaces from HKEY_CLASSES_ROOT. Reads proxy/stub information
		/// with fallback to defaults if not found.
		/// </summary>
		/// <param name="root">The parent node.</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessInterfaces(XmlElement root)
		{
			// Process interfaces that were found in type libraries
			// Try to read proxy/stub information from HKCR
			foreach (var kvp in _interfaceProxies.ToList())
			{
				var interfaceIid = kvp.Key;
				try
				{
					using (
						var regKeyInterface = Registry.ClassesRoot.OpenSubKey(
							$"Interface\\{interfaceIid}"
						)
					)
					{
						if (regKeyInterface != null)
						{
							var interfaceName = (string)
								regKeyInterface.GetValue(string.Empty, string.Empty);
							var numMethods = GetDefaultValueForKey(regKeyInterface, "NumMethods");
							var proxyStubClsId = GetDefaultValueForKey(
								regKeyInterface,
								"ProxyStubClsid32"
							);

							if (!string.IsNullOrEmpty(proxyStubClsId))
							{
								proxyStubClsId = proxyStubClsId.ToLower();
								_log.LogMessage(
									MessageImportance.Low,
									"Updated interface {0} from HKCR: ProxyStub={1}, NumMethods={2}",
									interfaceIid,
									proxyStubClsId,
									numMethods ?? "(none)"
								);

								// Note: The comInterfaceExternalProxyStub element was already added
								// by ProcessTypeLibrary. We're just logging that we could read from HKCR.
							}
							else
							{
								_log.LogMessage(
									MessageImportance.Low,
									"No ProxyStubClsid32 in HKCR for interface {0}, using IID as proxy (merged proxy/stub)",
									interfaceIid
								);
							}
						}
						else
						{
							_log.LogMessage(
								MessageImportance.Low,
								"Interface {0} not found in HKCR, using type library defaults",
								interfaceIid
							);
						}
					}
				}
				catch (Exception ex)
				{
					_log.LogMessage(
						MessageImportance.Low,
						"Cannot read interface {0} from registry: {1}",
						interfaceIid,
						ex.Message
					);
					// Continue with defaults from type library
				}
			}
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
		private static XmlNode GetChildNode(
			XmlNode parentNode,
			string childName,
			string attribute,
			string attrValue
		)
		{
			return parentNode
				.ChildNodes.Cast<XmlNode>()
				.FirstOrDefault(child =>
					child.Name == childName
					&& child.Attributes != null
					&& child.Attributes[attribute].Value == attrValue
				);
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
		private static void AppendOrReplaceNode(
			XmlNode parentNode,
			XmlNode childElement,
			string attribute,
			string attrValue
		)
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
				_log.LogWarning(" Can't add fragment {0}", fileName);
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

		public void AddDependentAssembly(XmlElement parent, string fileName)
		{
			var depAsmElem = (XmlElement)
				parent.SelectSingleNode(
					string.Format(
						"asmv1:dependency/asmv1:dependentAssembly[@asmv2:codebase = '{0}']",
						Path.GetFileName(fileName)
					),
					_nsManager
				);
			if (depAsmElem == null)
			{
				var depElem = _doc.CreateElement("dependency", UrnAsmv1);
				parent.AppendChild(depElem);
				depAsmElem = _doc.CreateElement("dependentAssembly", UrnAsmv1);
				depElem.AppendChild(depAsmElem);
				depAsmElem.SetAttribute("codebase", UrnAsmv2, Path.GetFileName(fileName));
			}
			var asmIdElem = (XmlElement)
				depAsmElem.SelectSingleNode("asmv1:assemblyIdentity", _nsManager);
			if (asmIdElem == null)
			{
				asmIdElem = _doc.CreateElement("assemblyIdentity", UrnAsmv1);
				depAsmElem.AppendChild(asmIdElem);
			}

			var depAsmManifestDoc = new XmlDocument();
			depAsmManifestDoc.Load(fileName);
			var depAsmNsManager = CreateNamespaceManager(depAsmManifestDoc);
			var manifestAsmIdElem = (XmlElement)
				depAsmManifestDoc.SelectSingleNode(
					"/asmv1:assembly/asmv1:assemblyIdentity",
					depAsmNsManager
				);
			Debug.Assert(manifestAsmIdElem != null);
			asmIdElem.SetAttribute("name", manifestAsmIdElem.GetAttribute("name"));
			asmIdElem.SetAttribute("version", manifestAsmIdElem.GetAttribute("version"));
			asmIdElem.SetAttribute("type", manifestAsmIdElem.GetAttribute("type"));
			// Copy processorArchitecture if present (required for 64-bit manifests)
			string procArch = manifestAsmIdElem.GetAttribute("processorArchitecture");
			if (!string.IsNullOrEmpty(procArch))
			{
				asmIdElem.SetAttribute("processorArchitecture", procArch);
			}
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
					RegHelper.GetLongPathName((string)inprocServer.GetValue(null), bldr, 255);
					string serverFullPath = bldr.ToString();
					string server = Path.GetFileName(serverFullPath);
					if (
						!File.Exists(serverFullPath)
						&& !File.Exists(Path.Combine(_baseDirectory, server))
					)
					{
						if (!_nonExistingServers.Contains(server))
						{
							_log.LogMessage(
								MessageImportance.Low,
								"{0} is referenced in the TLB but is not in current directory",
								server
							);
							_nonExistingServers.Add(server);
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

					if (!_coClasses.ContainsKey(clsId))
					{
						var description = (string)typeKey.GetValue(string.Empty);
						var threadingModel = (string)inprocServer.GetValue("ThreadingModel");
						var progId = GetDefaultValueForKey(typeKey, "ProgID");
						AddOrReplaceCoClass(
							file,
							clsId,
							threadingModel,
							description,
							tlbGuid.ToString("B"),
							progId
						);
						_log.LogMessage(
							MessageImportance.Low,
							string.Format(
								@"Coclass: clsid=""{0}"", threadingModel=""{1}"", tlbid=""{2}"", progid=""{3}""",
								clsId,
								threadingModel,
								tlbGuid,
								progId
							)
						);
					}
				}
			}
			catch (Exception e)
			{
				_log.LogMessage(
					MessageImportance.High,
					"Failed to process the type info for {0}",
					tlbGuid
				);
				_log.LogMessage(MessageImportance.High, e.StackTrace);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a clrClass element.
		/// </summary>
		/// <param name="parent">The parent file node.</param>
		/// <param name="clsId">The CLSID string.</param>
		/// <param name="threadingModel">The threading model.</param>
		/// <param name="name">The full name of the class.</param>
		/// <param name="progId">The prog id (might be <c>null</c>).</param>
		/// <param name="runtimeVersion">The runtime version.</param>
		/// ------------------------------------------------------------------------------------
		private void AddOrReplaceClrClass(
			XmlElement parent,
			string clsId,
			string threadingModel,
			string name,
			string progId,
			string runtimeVersion
		)
		{
			Debug.Assert(clsId.StartsWith("{"));

			clsId = clsId.ToLower();

			// <clrClass clsid="{...}" threadingModel="Both" name="..." runtimeVersion="..." progid="..." />
			var elem = _doc.CreateElement("clrClass", UrnAsmv1);
			elem.SetAttribute("clsid", clsId);
			elem.SetAttribute("threadingModel", threadingModel);
			elem.SetAttribute("name", name);
			elem.SetAttribute("runtimeVersion", runtimeVersion);

			if (!string.IsNullOrEmpty(progId))
				elem.SetAttribute("progid", progId);

			AppendOrReplaceNode(parent, elem, "clsid", clsId);
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
		private void AddOrReplaceCoClass(
			XmlElement parent,
			string clsId,
			string threadingModel,
			string description,
			string tlbId,
			string progId
		)
		{
			Debug.Assert(clsId.StartsWith("{"));
			Debug.Assert(string.IsNullOrEmpty(tlbId) || tlbId.StartsWith("{"));

			clsId = clsId.ToLower();

			// <comClass clsid="{2f0fccc2-c160-11d3-8da2-005004defec4}" threadingModel="Apartment"
			//		tlbid="{2f0fccc0-c160-11d3-8da2-005004defec4}" progid="FieldWorks.FwXmlData" />
			var elem = _doc.CreateElement("comClass", UrnAsmv1);
			elem.SetAttribute("clsid", clsId);
			elem.SetAttribute("threadingModel", threadingModel);
			if (!string.IsNullOrEmpty(description))
				elem.SetAttribute("description", description);
			if (!string.IsNullOrEmpty(tlbId))
				elem.SetAttribute("tlbid", tlbId.ToLower());
			if (!string.IsNullOrEmpty(progId))
				elem.SetAttribute("progid", progId);
			_coClasses[clsId] = elem;
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
			Debug.Assert(fileName != null);
			if (_files.ContainsKey(fileName))
				return _files[fileName];

			var fileInfo = new FileInfo(filePath);
			if (!fileInfo.Exists)
				fileInfo = new FileInfo(Path.Combine(_baseDirectory, fileName));
			XmlElement file = _doc.CreateElement("file", UrnAsmv1);
			file.SetAttribute("name", fileName);
			if (fileInfo.Exists)
			{
				parent.AppendChild(file);
				file.SetAttribute(
					"size",
					"urn:schemas-microsoft-com:asm.v2",
					fileInfo.Length.ToString(CultureInfo.InvariantCulture)
				);
			}
			_files.Add(fileName, file);
			return file;
		}
	}
}
