// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IncludeXml.cs
// History: John Hatton
// Last reviewed:
//
// <remarks>
//		Someday, the WWW3C XML inclusion standard will become available.  If that
//		has happened when you are reading this know that this class has nothing
//		to do with that, except that I would maybe have used that if it was available.
//
//		I named this IncludeXML rather than XMLInclude to reduce future confusion with
//		that standard.
// </remarks>

using System;
using System.Xml;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace XCore
{
	/// <summary>
	/// Summary description for XmlIncluder.
	/// </summary>
	public class XmlIncluder
	{
		protected IResolvePath m_resolver;
		private bool m_fSkipMissingFiles;

		/// <summary></summary>
		/// <param name="resolver">An object which can convert directory
		///		references into actual physical directory paths.</param>
		public XmlIncluder(IResolvePath resolver)
		{
			m_resolver = resolver;
		}

		/// True to allow missing include files to be ignored. This is useful with partial installations,
		/// for example, TE needs some of the FLEx config files to support change multiple spelling Dialog,
		/// but a lot of stuff can just be skipped (and therefore need not be installed).
		/// (Pathologically, a needed file may be missing and still ignored; can't help this for now.)
		public bool SkipMissingFiles
		{
			get { return m_fSkipMissingFiles; }
			set { m_fSkipMissingFiles = value; }
		}

		/// <summary>
		/// replace every <include/> node in the document with the nodes that it references
		/// </summary>
		/// <param name="dom"></param>=
		public void ProcessDom(string parentPath, XmlDocument dom)
		{
			Dictionary<string, XmlDocument> cachedDoms = new Dictionary<string, XmlDocument>();
			cachedDoms.Add(parentPath, dom);
			ProcessDom(cachedDoms, null, dom);
			cachedDoms.Clear();
		}

		/// <summary>
		/// replace every <include/> node in the document with the nodes that it references
		/// </summary>
		/// <param name="cachedDoms"></param>=
		/// <param name="parentPath"></param>=
		/// <param name="dom"></param>=
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		protected void ProcessDom(Dictionary<string, XmlDocument> cachedDoms, string parentPath, XmlDocument dom)
		{
			XmlNode nodeForError = null;
			string baseFile = "";
			XmlNode baseNode = dom.SelectSingleNode("//includeBase");
			if (baseNode != null)
			{
				baseFile = XmlUtils.GetManditoryAttributeValue(baseNode,"path");
				//now that we have read it, remove it, so that it does not violate the schema of
				//the output file.
				baseNode.ParentNode.RemoveChild(baseNode);
			}

			try
			{
#if !__MonoCS__
				foreach (XmlNode includeNode in dom.SelectNodes("//include"))
				{
#else
				// TODO-Linux: work around for mono bug https://bugzilla.novell.com/show_bug.cgi?id=495693
				XmlNodeList includeList = dom.SelectNodes("//include");
				for(int j = includeList.Count - 1; j >= 0; --j)
				{
					XmlNode includeNode = includeList[j];
					if (includeNode == null)
						continue;
#endif
					nodeForError = includeNode;
					ReplaceNode(cachedDoms, parentPath, includeNode, baseFile);
				}
			}
			catch (Exception error)
			{
				throw new ApplicationException("Error while processing <include> element:" + nodeForError.OuterXml, error);
			}

			Debug.Assert(dom.SelectSingleNode("//include") == null, "some <include> node was not handled");
		}

		/// <summary>
		/// "overrides" nodes can be contained in "include" nodes to make ammendments to the included nodes.
		/// An override node consists of an element to change, followed by the first attribute identifying the node.
		/// If the override node has an inner element, we'll replace the included node with the entire override node.
		/// Otherwise, we'll just use the attributes in the overrides node to specify which attributes to change or add.
		/// <example>
		///		<include path="doc" query="targetNodes">
		///			<overrides>
		///				<elementToChange keyAttributeToFind="keyValue" attributeToModify="newValue">
		///			</overrides>
		///     </include>
		/// </example>
		/// </summary>
		/// <param name="includeNode">include" node, possibly containing "overrides" nodes</param>
		/// <returns>true if we processed an "overrides" node.</returns>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		private static bool HandleIncludeOverrides(XmlNode includeNode)
		{
			XmlNode parentNode = includeNode.ParentNode;
			XmlNode overridesNode = null;
			// find any "overrides" node
			foreach (XmlNode childNode in includeNode.ChildNodes)
			{
				// first skip over any XmlComment nodes.
				// TODO-Linux: System.Boolean System.Type::op_Equality(System.Type,System.Type)
				// is marked with [MonoTODO] and might not work as expected in 4.0.
				if (childNode.GetType() == typeof(XmlComment))
					continue;
				if (childNode != null && childNode.Name == "overrides")
					overridesNode = childNode;
			}
			if (overridesNode == null)
				return false;
			// this is a group of overrides, so alter matched nodes accordingly.
			// treat the first three element parts (element and first attribute) as a node query key,
			// and subsequent attributes as subsitutions.
			foreach (XmlNode overrideNode in overridesNode.ChildNodes)
			{
				// TODO-Linux: System.Boolean System.Type::op_Equality(System.Type,System.Type)
				// is marked with [MonoTODO] and might not work as expected in 4.0.
				if (overrideNode.GetType() == typeof(XmlComment))
					continue;
				string elementKey = overrideNode.Name;
				string firstAttributeKey = overrideNode.Attributes[0].Name;
				string firstAttributeValue = overrideNode.Attributes[0].Value;
				string xPathToModifyElement = String.Format(".//{0}[@{1}='{2}']", elementKey, firstAttributeKey, firstAttributeValue);
				XmlNode elementToModify = parentNode.SelectSingleNode(xPathToModifyElement);
				Debug.Assert(elementToModify != null && elementToModify != overrideNode, "Could not find included node '" + xPathToModifyElement + "' to apply override");
				if (elementToModify != null && elementToModify != overrideNode)
				{
					if (overrideNode.ChildNodes.Count > 0)
					{
						// replace the elementToModify with this overrideNode.
						XmlNode parentToModify = elementToModify.ParentNode;
						parentToModify.ReplaceChild(overrideNode.Clone(), elementToModify);
					}
					else
					{
						// just modify existing attributes or add new ones.
						foreach (XmlAttribute xaOverride in overrideNode.Attributes)
						{
							// the keyAttribute will be identical, so it won't change.
							XmlAttribute xaToModify = elementToModify.Attributes[xaOverride.Name];
							// if the attribute exists on the node we're modifying, alter it
							// otherwise add the new attribute.
							if (xaToModify != null)
								xaToModify.Value = xaOverride.Value;
							else
								elementToModify.Attributes.Append(xaOverride.Clone() as XmlAttribute);
						}
					}
				}
			}
			return true;
		}

		/// <summary>
		/// replace the node with the node or nodes that it refers to
		/// </summary>
		/// <example>
		/// <include path='IncludeXmlTestSource.xml' query='food/fruit/name'/>
		/// </example>
		/// <param name="includeNode"></param>
		/// <remarks>This is public because a test needs to access the otherwise protected method.</remarks>
		public void ReplaceNode(Dictionary<string, XmlDocument> cachedDoms, XmlNode includeNode)
		{
			ReplaceNode(cachedDoms, null, includeNode, null);
		}

		/// <summary>
		/// replace the node with the node or nodes that it refers to
		/// </summary>
		/// <example>
		/// <include path='IncludeXmlTestSource.xml' query='food/fruit/name'/>
		/// </example>
		/// <param name="includeNode"></param>
		protected void ReplaceNode(Dictionary<string, XmlDocument> cachedDoms, string parentPath, XmlNode includeNode, string defaultPath)
		{
			string path = null;
			if (defaultPath != null && defaultPath.Length > 0)
				path = XmlUtils.GetOptionalAttributeValue(includeNode, "path", defaultPath);
			else
			{
				path = XmlUtils.GetOptionalAttributeValue(includeNode, "path");
				if (path == null || path.Trim().Length == 0)
					throw new ApplicationException(
						"The path attribute was missing and no default path was specified. " + Environment.NewLine
						+ includeNode.OuterXml);
			}
			XmlNode parentNode = includeNode.ParentNode;
			try
			{
				/* To support extensions, we need to see if 'path' starts with 'Extensions/* /'. (without the extra space following the '*'.)
				* If it does, then we will have to get any folders (the '*' wildcard)
				* and see if any of them have the specified file (at end of 'path'.
				*/
				StringCollection paths = new StringCollection();
				// The extension XML files should be stored in the data area, not in the code area.
				// This reduces the need for users to have administrative privileges.
				bool fExtension = false;
				string extensionBaseDir = null;
				if (path.StartsWith("Extensions") || path.StartsWith("extensions"))
				{
					// Extension <include> element,
					// which may have zero or more actual extensions.
					string extensionFileName = path.Substring(path.LastIndexOf("/") + 1);
					string pluginBaseDir = (parentPath == null) ? m_resolver.BaseDirectory : parentPath;
					extensionBaseDir = pluginBaseDir;
					string sBaseCode = FwDirectoryFinder.CodeDirectory;
					string sBaseData = FwDirectoryFinder.DataDirectory;
					if (extensionBaseDir.StartsWith(sBaseCode) && sBaseCode != sBaseData)
						extensionBaseDir = extensionBaseDir.Replace(sBaseCode, sBaseData);
					// JohnT: allow the Extensions directory not even to exist. Just means no extentions, as if empty.
					if (!Directory.Exists(extensionBaseDir + "/Extensions"))
						return;
					foreach (string extensionDir in Directory.GetDirectories(extensionBaseDir + "/Extensions"))
					{
						string extensionPathname = Path.Combine(extensionDir, extensionFileName);
						// Add to 'paths' collection, but only from 'Extensions' on.
						if (File.Exists(extensionPathname))
							paths.Add(extensionPathname.Substring(extensionPathname.IndexOf("Extensions")));
					}
					// Check for newer versions of the extension files in the
					// "Available Plugins" directory.  See LT-8051.
					UpdateExtensionFilesIfNeeded(paths, pluginBaseDir, extensionBaseDir);
					if (paths.Count == 0)
						return;
					fExtension = true;
				}
				else
				{
					// Standard, non-extension, <include> element.
					paths.Add(path);
				}

				/* Any fragments (extensions or standard) will be added before the <include>
				 * element. Aftwerwards, the <include> element will be removed.
				 */
				string query = XmlUtils.GetManditoryAttributeValue(includeNode, "query");
				foreach (string innerPath in paths)
				{
					XmlDocumentFragment fragment;
					if (innerPath == "$this")
					{
						fragment = CreateFragmentWithTargetNodes(query, includeNode.OwnerDocument);
					}
					else
					{
						fragment = GetTargetNodes(cachedDoms,
							fExtension ? extensionBaseDir : parentPath, innerPath, query);
					}
					if (fragment != null)
					{
						XmlNode node = includeNode.OwnerDocument.ImportNode(fragment, true);
						// Since we can't tell the index of includeNode,
						// always add the fluffed-up node before the include node to keep it/them in the original order.
						parentNode.InsertBefore(node, includeNode);
					}
				}
				// Handle any overrides.
				HandleIncludeOverrides(includeNode);
			}
			catch(Exception e)
			{
				// TODO-Linux: if you delete this exception block !check! flex still runs on linux.
				Console.WriteLine("Debug ReplaceNode error: {0}", e);
			}
			finally
			{
				// Don't want the original <include> element any more, no matter what.
				parentNode.RemoveChild(includeNode);
			}
		}

		/// <summary>
		/// Check for changed (or missing) files in the Extensions subdirectory (as
		/// compared to the corresponding "Available Plugins" subdirectory).
		/// </summary>
		/// <param name="paths"></param>
		/// <param name="pluginBaseDir"></param>
		/// <param name="extensionBaseDir"></param>
		private void UpdateExtensionFilesIfNeeded(StringCollection paths, string pluginBaseDir,
			string extensionBaseDir)
		{
			if (paths.Count == 0)
				return;
			List<string> obsoletePaths = new List<string>();
			foreach (string extensionPath in paths)
			{
				string pluginPathname = Path.Combine(pluginBaseDir, extensionPath);
				pluginPathname = pluginPathname.Replace("Extensions", "Available Plugins");
				if (File.Exists(pluginPathname))
				{
					string extensionPathname = Path.Combine(extensionBaseDir, extensionPath);
					Debug.Assert(File.Exists(extensionPathname));
					if (!FileUtils.AreFilesIdentical(pluginPathname, extensionPathname))
					{
						string extensionDir = Path.GetDirectoryName(extensionPathname);
						Directory.Delete(extensionDir, true);
						Directory.CreateDirectory(extensionDir);
						File.Copy(pluginPathname, extensionPathname);
						File.SetAttributes(extensionPathname, FileAttributes.Normal);
						// plug-ins usually have localization strings-XX.xml files.
						foreach (string pluginFile in Directory.GetFiles(Path.GetDirectoryName(pluginPathname), "strings-*.xml"))
						{
							string extensionFile = Path.Combine(extensionDir, Path.GetFileName(pluginFile));
							File.Copy(pluginFile, extensionFile);
							File.SetAttributes(extensionFile, FileAttributes.Normal);
						}
					}
				}
				else
				{
					obsoletePaths.Add(extensionPath);
				}
			}
			foreach (string badPath in obsoletePaths)
				paths.Remove(badPath);
		}

		/// <summary>
		/// get a group of nodes specified by and XPATH query and a file path
		/// </summary>
		/// <remarks> this is the "inner loop" where recursion happens so that files can include other files.</remarks>
		/// <param name="cachedDoms"></param>
		/// <param name="parentPath"></param>
		/// <param name="path"></param>
		/// <param name="query"></param>
		/// <returns></returns>
		protected XmlDocumentFragment GetTargetNodes(Dictionary<string, XmlDocument> cachedDoms, string parentPath, string path, string query)
		{
			path = (parentPath == null) ? m_resolver.Resolve(path, m_fSkipMissingFiles)
				: m_resolver.Resolve(parentPath, path, m_fSkipMissingFiles);
			if (path == null)
				return null; // Only possible if m_fSkipMissingFiles is true.
			//path = m_resolver.Resolve(parentPath,path);
			XmlDocument document = null;
			if (!cachedDoms.ContainsKey(path))
			{
				if (m_fSkipMissingFiles && !System.IO.File.Exists(path))
					return null;
				document = new XmlDocument();
				document.Load(path);
				cachedDoms.Add(path, document);
			}
			else
				document = cachedDoms[path];

			//enhance:protect against infinite recursion somehow
			//recurse so that that file itself can have <include/>s.
			ProcessDom(cachedDoms, System.IO.Path.GetDirectoryName(path), document);

			return CreateFragmentWithTargetNodes(query, document);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private static XmlDocumentFragment CreateFragmentWithTargetNodes(string query, XmlDocument document)
		{
			//find the nodes specified in the XML query
			XmlNodeList list = document.SelectNodes(query);
			XmlDocumentFragment fragment = document.CreateDocumentFragment();
			foreach (XmlNode node in list)
			{
				// We must clone the node, otherwise, AppendChild merely MOVES it,
				// modifying the document we have cached, and causing a repeat query for
				// the same element (or any of its children) to fail.
				fragment.AppendChild(node.Clone());
			}
			return fragment;
		}


		/// <summary>
		/// replace every <copyElement idref='foo'/> node in the document with
		/// the node that it references, from the same file. This is used by PNG branch report filter system.
		/// </summary>
		/// <param name="dom"></param>=
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void ProcessCopyElements(XmlDocument dom)
		{
			XmlNode nodeForError = null;
			try
			{
				foreach(XmlNode node in dom.SelectNodes("//copyElement"))
				{
					nodeForError = node;
					CopyElement (dom, node);
				}
			}
			catch (Exception error)
			{
				throw new ApplicationException("Error while rocessing <copyElement> element:" + nodeForError.OuterXml, error);
			}

			XmlNode copyElement =  dom.SelectSingleNode("//copyElement");
			Debug.Assert(copyElement== null, "some <copyElement> node was not handled");
		}

		/// <summary>
		/// replace the node with the node or nodes that it refers to
		/// </summary>
		/// <example>
		/// <include path='IncludeXmlTestSource.xml' query='food/fruit/name'/>
		/// </example>
		/// <param name="targetNode"></param>
		protected void CopyElement(XmlDocument dom, XmlNode copyInstructionNode)
		{
			string id = XmlUtils.GetManditoryAttributeValue(copyInstructionNode, "idref");
			XmlNode node =	copyInstructionNode.OwnerDocument.SelectSingleNode("//*[@id='" + id + "']");
			if (node == null)
				throw new ApplicationException ("Could not find an element in this file with the id of '" + id + "', in order to do the <copyElement> .");

			copyInstructionNode.ParentNode.ReplaceChild(node.CloneNode(true), copyInstructionNode);
		}
/* Not used yet.
		protected XmlNodeList GetNodes(string directoryReference, string query)
		{
			return null;
		}
*/
	}
}
