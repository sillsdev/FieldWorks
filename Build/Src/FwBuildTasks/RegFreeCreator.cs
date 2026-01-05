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

		// CLSIDs that are defined in native TypeLibs but implemented in managed code.
		// We must exclude them from the native manifest to avoid duplicate definitions
		// when the managed assembly also provides a manifest for them.
		private readonly HashSet<string> _excludedClsids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds CLSIDs to the exclusion list. These CLSIDs will be skipped when processing
		/// TypeLibs.
		/// </summary>
		/// <param name="clsids">The CLSIDs to exclude.</param>
		/// ------------------------------------------------------------------------------------
		public void AddExcludedClsids(IEnumerable<string> clsids)
		{
			if (clsids == null)
				return;

			foreach (var clsid in clsids)
			{
				if (!string.IsNullOrEmpty(clsid))
				{
					// Ensure consistent format (braces)
					string formatted = clsid.Trim();
					if (!formatted.StartsWith("{"))
						formatted = "{" + formatted + "}";

					_excludedClsids.Add(formatted);
				}
			}
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
			// elem.SetAttribute("xmlns:xsi", UrnSchema);
			// elem.SetAttribute("schemaLocation", UrnSchema, UrnAsmv1 + " assembly.adaptive.xsd");

			XmlNode oldChild = _doc.SelectSingleNode("asmv1:assembly", _nsManager);
			if (oldChild != null)
				_doc.ReplaceChild(elem, oldChild);
			else
				_doc.AppendChild(elem);

			if (!string.IsNullOrEmpty(assemblyName))
			{
				bool isMsil =
					"msil".Equals(Platform, StringComparison.OrdinalIgnoreCase)
					|| "anycpu".Equals(Platform, StringComparison.OrdinalIgnoreCase);
				bool isX64 = "x64".Equals(Platform, StringComparison.OrdinalIgnoreCase);
				bool isX86 = "x86".Equals(Platform, StringComparison.OrdinalIgnoreCase);

				string manifestType = isX64 ? "win64" : "win32";
				string processorArch = "x86";
				if (isX64)
					processorArch = "amd64";
				else if (isMsil)
					processorArch = "msil";
				else if (isX86)
					processorArch = "x86";

				if (isMsil)
					manifestType = "win32";

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
		/// <param name="serverImage">Name (and path) of the server file (DLL/EXE) if different from fileName.</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessTypeLibrary(XmlElement parent, string fileName, string serverImage = null)
		{
			_baseDirectory = Path.GetDirectoryName(fileName);
			_fileName = fileName.ToLower();
			string _serverName = serverImage != null ? serverImage.ToLower() : _fileName;

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
				var file = GetOrCreateFileNode(parent, _serverName);

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

					ProcessTypeInfo(parent, libAttr.guid, typeInfo, _serverName);
				}

				oldChild = parent.SelectSingleNode(
					string.Format("asmv1:file[asmv1:name='{0}']", Path.GetFileName(_serverName)),
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
		public bool ProcessManagedAssembly(XmlElement parent, string fileName)
		{
			_baseDirectory = Path.GetDirectoryName(fileName);
			_fileName = fileName.ToLower();
			bool foundClrClass = false;
			XmlElement fileNode = null;

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
						return false;

					var reader = peReader.GetMetadataReader();
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

						// Skip ComImport (these are wrappers, not implementations)
						if ((attributes & TypeAttributes.Import) != 0)
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

						if (fileNode == null)
						{
							fileNode = GetOrCreateFileNode(parent, fileName);
						}
						AddOrReplaceClrClass(
							fileNode,
							clsId,
							"Both",
							typeName,
							progId,
							runtimeVersion
						);
						foundClrClass = true;
						_excludedClsids.Add(clsId);

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

			if (!foundClrClass)
			{
				_log.LogMessage(
					MessageImportance.Low,
					"\tNo COM-visible classes found in {0}; manifest will be skipped.",
					Path.GetFileName(fileName)
				);
			}

			return foundClrClass;
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
		/// Processes the classes under CLSID. This reads from HKEY_CLASSES_ROOT directly
		/// (where FieldWorks COM classes are already registered). Fallback to defaults if not found.
		/// This is mainly done so that we get the proxy classes. The other classes are already
		/// processed through the type lib.
		/// </summary>
		/// <param name="parent">The parent node.</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessClasses(XmlElement parent)
		{
			// Registry lookups removed to ensure deterministic, hermetic builds.
			// All necessary information is now derived from the TypeLib in ProcessTypeInfo.
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
			// Registry lookups removed to ensure deterministic, hermetic builds.
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
		/// <param name="serverName">The name of the server file.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessTypeInfo(
			XmlNode parent,
			Guid tlbGuid,
			ITypeInfo typeInfo,
			string serverName
		)
		{
			try
			{
				IntPtr pTypeAttr;
				typeInfo.GetTypeAttr(out pTypeAttr);
				var typeAttr = (TYPEATTR)Marshal.PtrToStructure(pTypeAttr, typeof(TYPEATTR));
				typeInfo.ReleaseTypeAttr(pTypeAttr);

				// Assume the file containing the TypeLib is the server.
				// This avoids registry lookups and ensures deterministic builds.
				XmlElement file = GetOrCreateFileNode(parent, serverName);

				if (typeAttr.typekind == TYPEKIND.TKIND_COCLASS)
				{
					var clsId = typeAttr.guid.ToString("B");

					if (_excludedClsids.Contains(clsId))
					{
						_log.LogMessage(MessageImportance.Low, "\tSkipping excluded CoClass {0}", clsId);
						return;
					}

					if (!_coClasses.ContainsKey(clsId))
					{
						// Get name from TypeInfo for description
						string name, docString, helpFile;
						int helpContext;
						typeInfo.GetDocumentation(-1, out name, out docString, out helpContext, out helpFile);

						// Default to Apartment threading for FieldWorks native components.
						var threadingModel = "Apartment";
						string description = name;
						string progId = null;

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
								@"Coclass: clsid=""{0}"", threadingModel=""{1}"", tlbid=""{2}""",
								clsId,
								threadingModel,
								tlbGuid
							)
						);
					}
				}
				else if (typeAttr.typekind == TYPEKIND.TKIND_INTERFACE || typeAttr.typekind == TYPEKIND.TKIND_DISPATCH)
				{
					var iid = typeAttr.guid.ToString("B");

					string name, docString, helpFile;
					int helpContext;
					typeInfo.GetDocumentation(-1, out name, out docString, out helpContext, out helpFile);

					// Assume merged proxy/stub: ProxyStubClsid32 = IID
					// This is typical for ATL/merged proxy stubs used in FieldWorks.
					string proxyStubClsid = iid;

					AddOrReplaceInterface(file, iid, name, tlbGuid.ToString("B"), proxyStubClsid);

					_log.LogMessage(
						MessageImportance.Low,
						string.Format(
							@"Interface: iid=""{0}"", name=""{1}"", proxyStub=""{2}""",
							iid,
							name,
							proxyStubClsid
						)
					);
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
		/// Adds a comInterfaceProxyStub element.
		/// </summary>
		/// <param name="parent">The parent file node.</param>
		/// <param name="iid">The IID string.</param>
		/// <param name="name">The name of the interface.</param>
		/// <param name="tlbId">The type library id.</param>
		/// <param name="proxyStubClsid32">The proxy stub CLSID.</param>
		/// ------------------------------------------------------------------------------------
		private void AddOrReplaceInterface(
			XmlElement parent,
			string iid,
			string name,
			string tlbId,
			string proxyStubClsid32
		)
		{
			Debug.Assert(iid.StartsWith("{"));
			iid = iid.ToLower();
			if (proxyStubClsid32 != null) proxyStubClsid32 = proxyStubClsid32.ToLower();
			if (tlbId != null) tlbId = tlbId.ToLower();

			// <comInterfaceProxyStub iid="{...}" name="..." tlbid="{...}" proxyStubClsid32="{...}" />
			var elem = _doc.CreateElement("comInterfaceProxyStub", UrnAsmv1);
			elem.SetAttribute("iid", iid);
			elem.SetAttribute("name", name);
			if (!string.IsNullOrEmpty(tlbId))
				elem.SetAttribute("tlbid", tlbId);
			if (!string.IsNullOrEmpty(proxyStubClsid32))
				elem.SetAttribute("proxyStubClsid32", proxyStubClsid32);

			AppendOrReplaceNode(parent, elem, "iid", iid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a clrClass element.
		/// </summary>
		/// <param name="assemblyNode">The assembly element.</param>
		/// <param name="clsId">The CLSID string.</param>
		/// <param name="threadingModel">The threading model.</param>
		/// <param name="name">The full name of the class.</param>
		/// <param name="progId">The prog id (might be <c>null</c>).</param>
		/// <param name="runtimeVersion">The runtime version.</param>
		/// ------------------------------------------------------------------------------------
		private void AddOrReplaceClrClass(
			XmlElement assemblyNode,
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

			AppendOrReplaceNode(assemblyNode, elem, "clsid", clsId);
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
