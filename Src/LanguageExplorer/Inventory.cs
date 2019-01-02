// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.Xml;

namespace LanguageExplorer
{
	/// <summary>
	/// Constructs a composite Xml document by gathering elements from files.
	/// An XPath may be used to identify the desired elements from each file.
	/// They all become top-level elements in the composite document, which may
	/// then be searched for desired elements.
	/// A list of key attributes may be supplied; if a node with the same values
	/// for all key attributes (or absence of a key attribute) is found in more
	/// than one file, the last encountered 'wins'.
	/// For example (square brackets used for angle ones)
	/// [layout class="LexSense" type="Detail" id="Advanced"]
	/// (here class, type, and id are listed as key attributes).
	///
	/// An element may also have an attribute 'base' which is used to create
	/// derived elements by a 'unification' process. For example, we might have
	/// [layout class="LexSense" type="Detail" id="MyAdvanced" base="Advanced"].
	///
	/// One of the key attributes is identified (by being placed last in the list of key
	/// attributes) as the one that 'base' must match; so here, we would list the
	/// key attributes as "class, type, id". The base element that a derived one
	/// modifies is found by matching all attributes except the last (and the element name)
	/// exactly as found in the derived node; the last key attribute of the base
	/// element must match the 'base' attribute of the derived element.
	///
	/// It is also possible for a derived node to 'override' the one it is based on:
	/// [layout class="LexSense" type="Detail" id="Advanced" base="Advanced"].
	/// This means that the new Advanced layout is unified with the existing one and
	/// then 'replaces' it, becoming the layout that will actually be used if the 'Advanced'
	/// one is requested.
	///
	/// If both occur (a derived layout called Advanced based on Advanced, and a layout
	/// with a different name based on Advanced), the layout with the different name
	/// is based on the overridden version of 'Advanced', no matter what order the
	/// elements were loaded.
	///
	/// It is an error to load an element (base or derived) whose key matches a
	/// previously-loaded override. (Among other things, this means that a base
	/// element must be loaded before its override.)
	///
	/// It is possible to override a derived element. However, all information needed
	/// to compute the effect of the derivation must be loaded before the override.
	///
	/// When derivation occurs, it must be possible to retrieve from the collection
	/// the base element, the derived element's original information, and the
	/// result of unifying the base with the derived information. The last of these
	/// (the 'unified' element) is the most important, and is stored in the main document
	/// and retrieved using GetElement. The original derived element (with the 'base' attribute)
	/// is stored separately and can be retrieved using GetDerived; similarly, the base
	/// is stored separately and may be retrieved using GetBase.
	///
	/// The current client has two instances of inventory, accessed by Inventories["parts"]
	/// and Inventories["layouts"].
	/// </summary>
	public sealed class Inventory
	{
		#region Member data
		/// <summary>
		/// This does not represent any physical file on the disk,
		/// it is an in memory only document.
		/// </summary>
		private XDocument m_mainDoc;
		/// <summary>doc used to store replaced base elements</summary>
		private XDocument m_baseDoc;
		/// <summary>
		/// doc used to store alteration elements. (This is not the OUTPUT of the derivation/
		/// unification process: that is stored in m_mainDoc. This is the node we read from
		/// the file containing the specifications for the alteration.
		/// </summary>
		private XDocument m_alterationsDoc;
		/// <summary>
		/// Set of template paths.
		/// </summary>
		private HashSet<string> m_inventoryPaths;
		/// <summary>
		/// The pattern used to find files to load into the inventory in a directory.
		/// PersistOverrideElement assumes that it can strip off one character to get a useful
		/// suffix for a file name (e.g., '*.fwlayout' is a typical pattern).
		/// </summary>
		private string m_filePattern;
		/// <summary />
		private string m_projectPath;
		/// <summary>
		/// An xpath defining the elements we want to load from the files.
		/// PersistOverrideElement assumes that it follows the pattern /element1/element2/.../elementn/*
		/// for example, '/LayoutInventory/*' or '/Parts/bin/*'.
		/// </summary>
		private string m_xpathElementsWanted;
		/// <summary>
		/// This table is used to implement the GetUnified method, which finds or creates
		/// an element produced by unifying the children of two nodes. The key is a 'KeyValuePair'
		/// in which the items are the two source XML nodes. The value is the unified XML node.
		/// </summary>
		private Dictionary<Tuple<XElement, XElement>, XElement> m_unifiedNodes = new Dictionary<Tuple<XElement, XElement>, XElement>();
		private readonly Dictionary<GetElementKey, XElement> m_getElementTable = new Dictionary<GetElementKey, XElement>();
		private static readonly Dictionary<string, Inventory> s_inventories = new Dictionary<string, Inventory>();
		private List<KeyValuePair<string, DateTime>> m_fileInfo;
		/// <summary>Version number passed to LoadUserOverrides.</summary>
		private int m_version;
		/// <summary>
		/// This is used to store layout nodes that have an attribute tagForWs="true".  These are
		/// used in displaying Reversal Indexes, and need have separate versions generated for each
		/// reversal index writing system.
		/// </summary>
		private List<XElement> m_wsTaggedNodes = new List<XElement>();
		// Tracing variable - used to control when and what is output to the debug and trace listeners
		private TraceSwitch xmlInventorySwitch = new TraceSwitch("XML_Inventory", "", "Off");
		private string m_appName;
		#endregion

		/// <summary>
		/// This marks the beginning of a tag added to layout names (and param values) when an
		/// entire top-level layout type is copied.
		/// </summary>
		internal const char kcMarkLayoutCopy = '#';

		/// <summary>
		/// Layout filename divider. In the case of user-created dictionary views there will
		/// be two of these; otherwise only one.
		/// </summary>
		private const string ksUnderscore = "_";

		/// <summary />
		/// <param name="customInventoryPaths">A list of paths to custom inventory files,
		/// leave empty if defaults are fine.</param>
		/// <param name="filePattern">The pattern to select files from each directory</param>
		/// <param name="keyAttrs">Initialize the KeyAttributes property</param>
		/// <param name="xpath">Identifies the elements we want to load.</param>
		/// <param name="appName"></param>
		/// <param name="projectPath"></param>
		internal Inventory(string[] customInventoryPaths, string filePattern, string xpath, Dictionary<string, string[]> keyAttrs, string appName, string projectPath)
			: this(filePattern, xpath, keyAttrs, appName, projectPath)
		{
			var msStart = Environment.TickCount;
			m_inventoryPaths.Add(FwDirectoryFinder.GetCodeSubDirectory(@"Parts"));
			if (customInventoryPaths != null)
			{
				foreach (var customInventoryPath in customInventoryPaths)
				{
					m_inventoryPaths.Add(FwDirectoryFinder.GetCodeSubDirectory(customInventoryPath));
				}
			}
			LoadElements();
			if (xmlInventorySwitch.TraceInfo)
			{
				var msEnd = Environment.TickCount;
				var sInventory = (customInventoryPaths == null || customInventoryPaths.Length > 1) ? filePattern : customInventoryPaths[0];
				Debug.WriteLine("Initializing part inventory " + sInventory + " took " + (msEnd - msStart) + " ms", xmlInventorySwitch.DisplayName);
			}
		}

		/// <summary>
		/// Caller should call LoadElements(input) to initialize. This is used for tests.
		/// </summary>
		/// <param name="filePattern">The pattern to select files from each directory</param>
		/// <param name="keyAttrs">Initialize the KeyAttributes property</param>
		/// <param name="xpath">Identifies the elements we want to load.</param>
		/// <param name="appName"></param>
		/// <param name="projectPath"></param>
		internal Inventory(string filePattern, string xpath, Dictionary<string, string[]> keyAttrs, string appName, string projectPath)
		{
			m_inventoryPaths = new HashSet<string>();
			m_filePattern = filePattern;
			KeyAttributes = keyAttrs;
			m_xpathElementsWanted = xpath;
			m_appName = appName;
			m_projectPath = projectPath;
		}

		/// <summary>
		/// Provides a merge tool for safely creating valid current versions of user overrides
		/// belonging to an older version.
		/// </summary>
		internal IOldVersionMerger Merger { get; set; }

		/// <summary>
		/// Load overrides from the user's private directory. If version number does not match, ignore.
		/// </summary>
		internal void LoadUserOverrides(int version, string sDatabase)
		{
			Debug.Assert(m_version == version || m_version == 0);
			m_version = version; // remember for reloads; currently we only support one version.
			Debug.Assert(DatabaseName == null || DatabaseName == sDatabase);
			DatabaseName = sDatabase;
			var path = UserOverrideConfigurationSettingsPath;
			m_inventoryPaths.Add(path);
			if (Directory.Exists(path))
			{
				var sPattern = string.IsNullOrEmpty(sDatabase) ? $"default$${m_filePattern}" : m_filePattern;
				AddElementsFromFiles(DirectoryUtils.GetOrderedFiles(path, sPattern), version, true);
			}
		}

		/// <summary>
		/// Delete override files from the user's private directory.  Ignore version numbers.
		/// </summary>
		internal void DeleteUserOverrides(string sDatabase)
		{
			Debug.Assert(DatabaseName == null || DatabaseName == sDatabase);
			DatabaseName = sDatabase;
			var path = UserOverrideConfigurationSettingsPath;
			if (!Directory.Exists(path))
			{
				return;
			}
			var sPattern = string.IsNullOrEmpty(sDatabase) ? $"default$${m_filePattern}" : m_filePattern;
			var rgsFiles = DirectoryUtils.GetOrderedFiles(path, sPattern);
			foreach (var sFilename in rgsFiles)
			{
				// LT-11193 Don't delete user overrides if they are new dictionary views
				// (from the Manage Views dialog)
				if (sFilename.Split(new[] { ksUnderscore }, StringSplitOptions.RemoveEmptyEntries).Length > 1)
				{
					continue;
				}
				File.Delete(sFilename);
			}
		}

		/// <summary>
		/// Get/set the name of the database associated with this Inventory.
		/// </summary>
		internal string DatabaseName { get; set; }

		/// <summary>
		/// Persist an override element by creating a file which LoadUserOverides will load.
		/// We may need to generalize this somewhat; currently it is only designed to
		/// handle the complexities of saving custom layouts.
		///
		/// 1. Assuming that m_xpathElementsWanted is something like /LayoutInventory/*,
		///    Make an XmlDocument that contains a LayoutInventory node containing element.
		///
		/// 2. Make a unique file name based on the name of the element and its identifying
		///    attributes. For example, if we have
		/// 			keyAttrs["layout"] = new string[] {"class", "type", "name" };
		///    and an element layout class="LexEntry" type="Detail" name="Full"
		///    and our m_filePattern is ".fwlayout"
		///    we want to make a file LexEntry.fwlayout.
		///   If this file already exists, we replace or append the element that matches
		///   the one we're given according to our key attributes.
		///
		///   However, if the name looks like "Full#Foo", the filename comes from the
		///   corresponding layoutType node with a layout name that ends with "#Foo".
		/// </summary>
		internal void PersistOverrideElement(XElement element)
		{
			var keyAttrs = KeyAttributes[element.Name.LocalName];
			if (element.Name == "layout")
			{
				var sVersion = XmlUtils.GetOptionalAttributeValue(element, "version");
				if (sVersion == null && m_version != 0)
				{
					XmlUtils.SetAttribute(element, "version", m_version.ToString());
				}
			}
			string name = null;
			var layoutName = XmlUtils.GetOptionalAttributeValue(element, "name", "");
			var idxTag = layoutName.IndexOf(kcMarkLayoutCopy);
			XElement layoutType = null;
			if (idxTag > 0)
			{
				var tag = layoutName.Substring(idxTag);
				var idx = tag.IndexOf(LayoutKeyUtils.kcMarkNodeCopy);
				if (idx > 0)
				{
					tag = tag.Remove(idx);
				}
				Debug.Assert(m_mainDoc != null);
				var root = m_mainDoc.Root;
				Debug.Assert(root != null);
				var nodes = root.Elements("layoutType");
				if (nodes != null)
				{
					foreach (var node in nodes.OfType<XElement>())
					{
						var layoutNode = XmlUtils.GetMandatoryAttributeValue(node, "layout");
						if (layoutNode.EndsWith(tag))
						{
							layoutType = node;
							break;
						}
					}
					if (layoutType != null)
					{
						var label = XmlUtils.GetMandatoryAttributeValue(layoutType, "label");
						var className = XmlUtils.GetOptionalAttributeValue(layoutType.Elements().First(), "class");
						name = $"{label}_{className}";
					}
				}
			}
			if (string.IsNullOrEmpty(name))
			{
				name = XmlUtils.GetMandatoryAttributeValue(element, keyAttrs[0]);
			}
			var sDatabase = string.IsNullOrEmpty(DatabaseName) ? "default$$" : "";
			var fileName = sDatabase + name + m_filePattern.Substring(1); // strip off leading *
			var path = Path.Combine(UserOverrideConfigurationSettingsPath, fileName);
			CreateDirectoryIfNonexistant(UserOverrideConfigurationSettingsPath);
			XDocument doc = null;
			var parentEltNames = GetParentElementNames();
			XElement parent = null; // where we will put new element
			// We expect to find or create a document that has one element
			if (File.Exists(path))
			{
				doc = XDocument.Load(path);
				parent = doc.Root;
				var current = parent;
				foreach (var eltName in parentEltNames)
				{
					if (current.Name != eltName)
					{
						parent = null;
						break;
					}
					parent = current;
					current = XmlUtils.GetFirstNonCommentChild(current);
				}
				if (parent != null)
				{
					// Remove any matching child
					foreach (var child in parent.Elements())
					{
						if (child.Name != element.Name)
						{
							continue;
						}
						var match = true;
						foreach (var attrName in keyAttrs)
						{
							if (XmlUtils.GetOptionalAttributeValue(child, attrName) != XmlUtils.GetOptionalAttributeValue(element, attrName))
							{
								match = false;
								break;
							}
						}
						if (match)
						{
							child.Remove();
							break;
						}
					}
					// Remove any matching layoutType
					if (layoutType != null)
					{
						var layout = XmlUtils.GetMandatoryAttributeValue(layoutType, "layout");
						foreach (var child in parent.Elements())
						{
							if (child.Name != layoutType.Name)
							{
								continue;
							}
							if (XmlUtils.GetOptionalAttributeValue(child, "layout") != layout)
							{
								continue;
							}
							child.Remove();
							break;
						}
					}
				}
			}
			if (parent == null)
			{
				// File does not exist, or has unexpected contents; overwrite.
				doc = new XDocument();
				doc.Add(new XElement(parentEltNames[0]));
				parent = doc.Root;
				for (var i = 1; i < parentEltNames.Length; i++)
				{
					var child = new XElement(parentEltNames[i]);
					parent.Add(child);
					parent = child;
				}
			}
			// One way or another we now have a parent node that doesn't contain an element
			// that conflicts with the new one.
			parent.Add(element);
			if (layoutType != null)
			{
				parent.Add(layoutType);
			}
			// OK, the document now contains the desired content. Write it out.
			doc.Save(path);
			// Finally actually add it to the inventory, replacing any existing node of the same key.
			AddNodeToInventory(element);
		}

		private void CreateDirectoryIfNonexistant(string directory)
		{
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}

		/// <summary>
		/// Add (or replace) the given layout type in the inventory.
		/// </summary>
		internal void AddLayoutTypeToInventory(XElement layoutType)
		{
			Debug.Assert(m_mainDoc != null);
			var root = m_mainDoc.Root;
			Debug.Assert(root != null);
			var layoutName = XmlUtils.GetMandatoryAttributeValue(layoutType, "layout");
			var nodes = root.Elements("layoutType");
			foreach (var xn in nodes)
			{
				var layoutOld = XmlUtils.GetMandatoryAttributeValue(xn, "layout");
				if (layoutOld == layoutName)
				{
					xn.ReplaceWith(layoutType);
					return;
				}
			}
			root.Add(layoutType);
		}

		/// <summary>
		/// Return the list of layout types that the inventory knows about.
		/// </summary>
		internal List<XElement> GetLayoutTypes()
		{
			Debug.Assert(m_mainDoc != null);
			var root = m_mainDoc.Root;
			Debug.Assert(root != null);
			return root.Elements("layoutType").ToList();
		}

		/// <summary>
		/// Take the given node and add it to the parts/layouts inventory
		/// </summary>
		internal void AddNodeToInventory(XElement element)
		{
			AddNode(element, m_mainDoc.Root);
		}

		private string[] GetParentElementNames()
		{
			Debug.Assert(m_xpathElementsWanted[0] == '/');
			Debug.Assert(m_xpathElementsWanted.Substring(m_xpathElementsWanted.Length - 2, 2) == "/*");
			var result = m_xpathElementsWanted.Substring(1, m_xpathElementsWanted.Length - 3).Split('/');
			Debug.Assert(result.Length > 0);
			Debug.Assert(result[0].Length > 0);
			return result;
		}

		private string UserOverrideConfigurationSettingsPath => LcmFileHelper.GetConfigSettingsDir(m_projectPath);

		/// <summary>
		/// Used to retrieve a shared inventory by name.
		/// </summary>
		internal static Inventory GetInventory(string key, string sDatabase)
		{
			var sKey = !string.IsNullOrEmpty(sDatabase) ? $"{key}${sDatabase}" : key;
			if (s_inventories.ContainsKey(sKey))
			{
				var val = s_inventories[sKey];
				Debug.Assert(val.DatabaseName == sDatabase);
				return val;
			}
			return null;
		}

		/// <summary>
		/// Used to remove a shared inventory by name.
		/// </summary>
		internal static void RemoveInventory(string key, string sDatabase)
		{
			var sKey = !string.IsNullOrEmpty(sDatabase) ? $"{key}${sDatabase}" : key;
			if (s_inventories.ContainsKey(sKey))
			{
				s_inventories.Remove(sKey);
			}
		}

		/// <summary>
		/// Used to set up a shared inventory.
		/// </summary>
		internal static void SetInventory(string key, string sDatabase, Inventory val)
		{
			Guard.AgainstNullOrEmptyString(key, nameof(key));
			Guard.AgainstNull(val, nameof(val));

			Debug.Assert(val.DatabaseName == null || val.DatabaseName == sDatabase);
			if (val.DatabaseName == null)
			{
				val.DatabaseName = sDatabase;
			}
			var sKey = !string.IsNullOrEmpty(sDatabase) ? $"{key}${sDatabase}" : key;
			s_inventories[sKey] = val;
		}

		/// <summary>
		/// Get the root node that has the collected elements as direct children.
		/// </summary>
		internal XElement Root
		{
			get
			{
				Debug.Assert(m_mainDoc != null);
				return m_mainDoc.Root;
			}
		}

		/// <summary>
		/// List of attribute names that must match for a node to be considered a replacement
		/// of an existing node.
		/// </summary>
		internal Dictionary<string, string[]> KeyAttributes { get; }

		/// <summary>
		/// Get the node (if any) that has the specified element name and the specified value
		/// for each of the attributes listed in KeyAttributes. If an item in attrvals is null,
		/// the specified attribute must not occur at all. If it's an empty string, it must
		/// occur with an empty value.
		///
		/// This is the main access point for clients wanting to USE a (possibly derived)
		/// element with a particular key.
		/// </summary>
		internal XElement GetElement(string elementName, string[] attrvals)
		{
			var node = GetEltFromDoc(elementName, attrvals, m_mainDoc);
			if (node != null)
			{
				return node;
			}
			// If not found, there might be an alteration node for which we have not yet computed the
			// meaning. Do it now, and return the result.
			// We postponed computing the effect of alterations for two reasons:
			// 1. Saves memory and time for alterations that are never used.
			// 2. Ensures that an alteration based on an override gets based on the override,
			// even if it is loaded before the override.
			var alteration = GetEltFromDoc(elementName, attrvals, m_alterationsDoc);
			return alteration == null ? null : ApplyAlteration(elementName, attrvals, alteration);
		}

		/// <summary>
		/// Compute and save in m_mainDoc a node which represents the actual meaning of the given
		/// alteration node, which has the specified element name and keys.
		/// </summary>
		private XElement ApplyAlteration(string elementName, string[] attrvals, XElement alteration)
		{
			var baseName = XmlUtils.GetMandatoryAttributeValue(alteration, "base");
			var baseKey = (string[])attrvals.Clone();
			var cKeys = baseKey.Length;
			baseKey[cKeys - 1] = baseName;
			var baseNode = GetEltFromDoc(elementName, baseKey, m_mainDoc);
			var result = Unify(alteration, baseNode);
			m_mainDoc.Root.Add(result);
			// we may already have a 'miss' for this element cached, so update the table
			// 'result' may be null;
			var key = new GetElementKey(elementName, attrvals, m_mainDoc);
			m_getElementTable[key] = result;
			return result;
		}

		/// <summary>
		/// Unify two nodes.
		/// This creates a new node in m_mainDoc with the same element name as alteration
		/// (and baseNode, unless it is null).
		/// It is expected that baseNode matches alteration for all the key attributes
		/// specified for this element name.
		/// The unified node has all the same attributes as alteration, plus any attributes
		/// of baseNode that are not present in alteration.
		/// It also has children, created as follows:
		/// For each child of alteration, if there is a child of baseNode that has the
		/// same element name and the same key attributes appropriate to that element,
		/// the new child is produced by unifying those two nodes. Otherwise the node is
		/// simply copied.
		/// Also, each child of baseNode that does not get unified with a child of
		/// alteration is copied to the unified node.
		/// The order of child nodes is, by default, the order in which nodes appear in
		/// baseNode, followed by any nodes in alteration which don't unify.
		/// if alteration has the attribute/value reorder='true', then the order is the
		/// order in alteration, followed by base nodes that don't unify.
		/// </summary>
		private XElement Unify(XElement alteration, XElement baseNode)
		{
			// If we don't have a base node, make an exact copy of alteration.
			if (baseNode == null)
			{
				return alteration;
			}
			// And, likewise, if we don't have an alteration, make an exact copy of base.
			if (alteration == null)
			{
				return baseNode;
			}
			var unified = new XElement(alteration.Name, alteration.Attributes());
			// Not done with child elements of alteration yet.
			CopyAttributes(baseNode, unified, true);
			UnifyChildren(alteration, baseNode, unified);
			return unified;
		}

		private void UnifyChildren(XElement alteration, XElement baseNode, XElement unified)
		{
			var reorder = XmlUtils.GetOptionalBooleanAttributeValue(alteration, "reorder", false);
			IEnumerable<XElement> orderBy;
			IEnumerable<XElement> others;
			if (reorder)
			{
				orderBy = alteration.Elements();
				others = baseNode.Elements();
			}
			else
			{
				orderBy = baseNode.Elements();
				others = alteration.Elements();
			}
			var remainingOthers = new HashSet<XElement>();
			foreach (var node in others)
			{
				remainingOthers.Add(node);
			}
			foreach (var item in orderBy)
			{
				var other = MatchAndRemove(remainingOthers, item);
				var newChild = reorder ? Unify(item, other) : Unify(other, item);
				unified.Add(newChild);
			}
			foreach (var item in remainingOthers)
			{
				unified.Add(item);
			}
		}

		private static void CopyAttributes(XElement source, XElement dest, bool fIfNotPresent)
		{
			foreach (var attr in source.Attributes())
			{
				if (fIfNotPresent && dest.Attribute(attr.Name) != null)
				{
					continue;
				}
				dest.Add(attr);
			}
		}

		/// <summary>
		/// If there is a node in remainingOthers which 'matches' item (in name and
		/// specified keys), remove and return it; otherwise return null.
		/// </summary>
		private XElement MatchAndRemove(ICollection<XElement> remainingOthers, XElement target)
		{
			var elementName = target.Name.LocalName;
			string[] keyAttrs = null;
			if (KeyAttributes.ContainsKey(elementName))
			{
				keyAttrs = KeyAttributes[elementName];
			}
			var ckeys = keyAttrs?.Length ?? 0;
			var keyVals = new string[ckeys]; // keys to try to match for each item
			for (var i = 0; i < ckeys; i++)
			{
				keyVals[i] = XmlUtils.GetOptionalAttributeValue(target, keyAttrs[i]);
			}
			foreach (var item in remainingOthers)
			{
				if (item.Name != elementName)
				{
					continue;
				}
				// See if all the keys match
				var cMatchingAttrs = 0;
				for (; cMatchingAttrs < ckeys; cMatchingAttrs++)
				{
					if (XmlUtils.GetOptionalAttributeValue(item, keyAttrs[cMatchingAttrs]) != keyVals[cMatchingAttrs])
					{
						break;
					}
				}
				if (cMatchingAttrs == ckeys)
				{
					// Full match!
					remainingOthers.Remove(item);
					return item;
				}
			}
			return null;
		}

		/// <summary>
		/// This routine just takes an xpath, and returns the matching elements.
		/// </summary>
		internal IEnumerable<XElement> GetElements(string xpath)
		{
			return m_mainDoc.Root.XPathSelectElements(xpath);
		}

		/// <summary>
		/// This routine takes a set of arguments, typically with a less complete set of attr vals
		/// than GetElement, and returns all matching elements. Currently it is not guaranteed to
		/// find alterations.
		/// </summary>
		internal IEnumerable<XElement> GetElements(string elementName, string[] attrvals)
		{
			// Create an xpath that will select a node with the same name and key attributes
			// (if any) as newNode. If some attributes are missing from newNode, they
			// must be missing from the matched node as well.
			string[] keyAttrs = null;
			if (KeyAttributes.ContainsKey(elementName))
			{
				keyAttrs = KeyAttributes[elementName];
			}
			if (keyAttrs == null)
			{
				keyAttrs = new string[0];
			}
			var pathBldr = new StringBuilder(elementName);
			var numAttrs = Math.Min(keyAttrs.Length, attrvals.Length);
			if (numAttrs <= 0)
			{
				return m_mainDoc.Root.XPathSelectElements(pathBldr.ToString());
			}
			pathBldr.Append("[");
			for (var i = 0; i < numAttrs; i++)
			{
				var attr = keyAttrs[i];
				if (i != 0)
				{
					pathBldr.Append(" and ");
				}
				var val = attrvals[i];
				if (val == null)
				{
					pathBldr.Append("not(@");
					pathBldr.Append(attr);
					pathBldr.Append(")");
				}
				else
				{
					pathBldr.Append("@");
					pathBldr.Append(attr);
					pathBldr.Append("='");
					pathBldr.Append(val);
					pathBldr.Append("'");
				}
			}
			pathBldr.Append("]");
			return m_mainDoc.Root.XPathSelectElements(pathBldr.ToString());
		}

		/// <summary />
		private XElement GetEltFromDoc(string elementName, string[] attrvals, XDocument doc)
		{
			var key = new GetElementKey(elementName, attrvals, doc);
			XElement result = null;
			var hasKey = m_getElementTable.ContainsKey(key);
			if (hasKey)
			{
				result = m_getElementTable[key]; // May be null, even with the key.
			}
			if (result == null && !hasKey)
			{
				result = GetEltFromDoc1(elementName, attrvals, doc);
				m_getElementTable[key] = result; // May still be null.
			}
			return result;
		}

		/// <summary />
		private XElement GetEltFromDoc1(string elementName, string[] attrvals, XDocument doc)
		{
			// Create an xpath that will select a node with the same name and key attributes
			// (if any) as newNode. If some attributes are missing from newNode, they
			// must be missing from the matched node as well.
			string[] keyAttrs = null;
			if (KeyAttributes.ContainsKey(elementName))
			{
				keyAttrs = KeyAttributes[elementName];
			}
			if (keyAttrs == null)
			{
				keyAttrs = new string[0];
			}
			var pathBldr = new StringBuilder(elementName);
			if (keyAttrs.Length == 0)
			{
				return doc.Root.XPathSelectElement(pathBldr.ToString());
			}
			pathBldr.Append("[");
			for (var i = 0; i < keyAttrs.Length; i++)
			{
				var attr = keyAttrs[i];
				if (i != 0)
				{
					pathBldr.Append(" and ");
				}
				var val = attrvals[i];
				if (val == null)
				{
					pathBldr.Append("not(@");
					pathBldr.Append(attr);
					pathBldr.Append(")");
				}
				else
				{
					pathBldr.Append("@");
					pathBldr.Append(attr);
					if (val.Contains("'"))
					{
						pathBldr.Append("=\"");
						pathBldr.Append(val);
						pathBldr.Append("\"");
					}
					else
					{
						pathBldr.Append("='");
						pathBldr.Append(val);
						pathBldr.Append("'");
					}
				}
			}
			pathBldr.Append("]");
			return doc.Root.XPathSelectElement(pathBldr.ToString());
		}

		/// <summary>
		/// Get the node (if any) that has the specified element name and the specified value
		/// for each of the attributes listed in KeyAttributes and has the 'base' attribute.
		/// That is, this is the node that originally defined the specified element,
		/// typically containing overrides but not the complete definition of the element
		/// that will be looked up by GetElement.
		///
		/// Note that a key may produce a result in GetElement, but produce null here, if it
		/// was fully defined directly and not by derivation.
		///
		/// Review: possibly only things from the last directory loaded show up here?
		/// </summary>
		internal XElement GetAlteration(string elementName, string[] attrvals)
		{
			return GetEltFromDoc(elementName, attrvals, m_alterationsDoc);
		}

		/// <summary>
		/// Get the node (if any) that was used as the base for the specified derived element.
		///
		/// Note that a key may produce a result in GetElement, but produce null here, if it
		/// was fully defined directly and not by derivation.
		///
		/// Exactly the keys that produce a value in GetDerived will produce one here.
		/// </summary>
		internal XElement GetBase(string elementName, string[] attrvals)
		{
			var alteration = GetAlteration(elementName, attrvals);
			if (alteration == null)
			{
				return null;
			}
			var keyBase = (string[])attrvals.Clone();
			keyBase[keyBase.Length - 1] = XmlUtils.GetMandatoryAttributeValue(alteration, "base");
			// if the alteration is an override (key = id), the base node is saved in m_baseDoc,
			// otherwise it is just in the normal main document.
			var keyAttrs = KeyAttributes[elementName];
			var id = XmlUtils.GetOptionalAttributeValue(alteration, keyAttrs[keyAttrs.Length - 1]);
			if (id == keyBase[keyBase.Length - 1])
			{
				// An override, the base should already be saved in m_baseDoc
				return GetEltFromDoc(elementName, keyBase, m_baseDoc);
			}
			// not an override, just an ordinary derived node.
			// Possibly the base is another derived node, not yet computed, so we need
			// to use the full GetElement to ensure that it gets created if needed.
			return GetElement(elementName, keyBase);
		}

		/// <summary>
		/// Load the templates again. Useful when you are working on template writing,
		/// and don't want to have to restart the application to test something you have done.
		/// </summary>
		internal void Reload()
		{
			LoadElements();
		}

		/// <summary>
		/// Reload if any of the files we depend on changed or new ones were added.
		/// </summary>
		public void ReloadIfChanges()
		{
			if (NoFilesChanged())
			{
				return;
			}
			Reload();
		}

		/// <summary>
		/// Get all of the elements in the given file that match our path.
		/// </summary>
		/// <param name="inventoryFilePath">Path to one inventory file.</param>
		/// <returns>A list of elements which should be inventory elements.</returns>
		private IEnumerable<XElement> LoadOneInventoryFile(string inventoryFilePath)
		{
			m_fileInfo.Add(new KeyValuePair<string, DateTime>(inventoryFilePath, File.GetLastWriteTime(inventoryFilePath)));
			XDocument xdoc;
			try
			{
				xdoc = XDocument.Load(inventoryFilePath);
			}
			catch (Exception error)
			{
				throw new ApplicationException(string.Format(LanguageExplorerResources.ErrorReadingXMLFile0, inventoryFilePath), error);

			}
			return xdoc.XPathSelectElements(m_xpathElementsWanted);
		}

		/// <summary>
		/// This method will save the given Nodes into the requested path, first it will remove all the existing nodes which match
		/// the type that this inventory is representing. Then it will add the new data to the parent node of the first match.
		/// </summary>
		private void RefreshOneInventoryFile(string inventoryFilePath, List<XElement> newData)
		{
			XDocument xdoc;
			try
			{
				xdoc = XDocument.Load(inventoryFilePath);
			}
			catch (Exception error)
			{
				throw new ApplicationException(string.Format(LanguageExplorerResources.ErrorReadingXMLFile0, inventoryFilePath), error);

			}
			var oldAndBusted = xdoc.XPathSelectElements(m_xpathElementsWanted).ToList();
			if (!oldAndBusted.Any())
			{
				return;
			}
			//get the parent of the nodes in the path
			var root = oldAndBusted[0].Parent;
			// In Mono, changing the root element invalidates the XmlNodeList iterator
			// so that it quits the loop immediately after the first node is removed.
			// See FWNX-1057.  This results in ever growing fwlayout files stored with
			// the project!  So we copy the list first into a form that won't be
			// invalidated.
			foreach (var match in new List<XElement>(oldAndBusted))
			{
				match.Remove();
			}
			foreach (var newItem in newData)
			{
				root.Add(newItem);
			}
			xdoc.Save(inventoryFilePath);
		}

		/// <summary>
		/// Add custom files after the main loading.
		/// </summary>
		internal void AddCustomFiles(string[] filePaths)
		{
			for (var i = 0; i < filePaths.Length; ++i)
			{
				var path = filePaths[i];
				if (m_inventoryPaths.Contains(path))
				{
					// Already in the path collection.
					filePaths[i] = null;
					continue;
				}
				if (Directory.Exists(path))
				{
					m_inventoryPaths.Add(path);
					AddElementsFromFiles(DirectoryUtils.GetOrderedFiles(path, m_filePattern));
				}
			}
		}

		/// <summary />
		private void AddElementsFromFiles(string[] filePaths)
		{
			AddElementsFromFiles(filePaths, m_version, false);
		}

		/// <summary>
		/// Collect all of the elements up from an array of files.
		/// </summary>
		/// <param name="filePaths">Collection of pathnames to individual XDE template files.</param>
		/// <param name="version"></param>
		/// <param name="loadUserOverRides">set to true if the version attribute needs to be added to elements in the configuration file.</param>
		/// <remarks>
		/// tests over in XMLVIews need access to this method.
		/// </remarks>
		internal void AddElementsFromFiles(IEnumerable<string> filePaths, int version, bool loadUserOverRides)
		{
			Debug.Assert(filePaths != null);
			Debug.Assert(m_mainDoc != null);
			var root = m_mainDoc.Root;
			foreach (var path in filePaths)
			{
				if (path == null)
				{
					continue;
				}
				var nodeList = LoadOneInventoryFile(path);
				bool wasMerged;
				var cleanedNodes = MergeAndUpdateNodes(nodeList, version, out wasMerged, loadUserOverRides);
				LoadElementList(cleanedNodes, version, root);
				if (wasMerged)
				{
					RefreshOneInventoryFile(path, cleanedNodes);
				}
			}
		}

		/// <summary>
		/// Take the collection of merged or retained nodes and insert them into our in memory document.
		/// </summary>
		private void LoadElementList(IEnumerable<XElement> elementList, int version, XElement root)
		{
			foreach (var element in elementList)
			{
				NoteIfNodeWsTagged(element);
				//"layoutType" nodes are handled differently in the Inventory.
				//Don't ask me why, the reason seems to have been lost to history.
				//It is important to follow through on that here though. naylor 6/7/2012
				if (element.Name == "layoutType")
				{
					AddLayoutTypeToInventory(element);
					continue;
				}
				//if the version is the same then we want to use the basic AddNode to get it into the inventory
				if (version == int.Parse(XmlUtils.GetOptionalAttributeValue(element, "version", version.ToString())))
				{
					AddNode(element, root);
					continue;
				}
				//set up the values for other options
				string[] keyAttrs;
				var key = GetKeyMain(element, out keyAttrs);
				XElement current;
				//if the element table already has a match then we want to insert replacing the current value
				if (m_getElementTable.TryGetValue(key, out current))
				{
					InsertNodeInDoc(element, current, m_mainDoc, key);
				}
				else //otherwise we are not going to attempt and replace any existing configurations
				{
					// We do NOT want this one to replace 'current', since it has a different name.
					// We already know there is no matching node to replace.
					InsertNodeInDoc(element, null, m_mainDoc, key);
				}
			}
		}

		/// <summary>
		/// This method will return a list of merged and unchanged nodes from the given list.
		/// </summary>
		/// <param name="nodeList"></param>
		/// <param name="version"></param>
		/// <param name="wasMerged">This parameter will be set to true if any nodes were merged to the latest version</param>
		/// <param name="loadUserOverRides">set to true if the version attribute needs to be added to elements in the configuration file.</param>
		/// <returns></returns>
		private List<XElement> MergeAndUpdateNodes(IEnumerable<XElement> nodeList, int version, out bool wasMerged, bool loadUserOverRides)
		{
			wasMerged = false;
			// Past bugs may have left duplication in the layout file.  (See FWNX-1057.)  Use a simple
			// check to weed out any duplicates, which will be exactly identical.
			var comparer = new XElementCompare();
			var survivors = new HashSet<XElement>(comparer);
			foreach (var node in nodeList)
			{
				// Load only nodes that either have matching version number or none.
				// JohnT says that all user files will have a version number, and have had one from the beginning.
				// None of our installer provided files have a version number.
				// That is why we check for an optional version attribute.
				//
				// LT-12778 revealed a defect related to version number. In previous versions of FieldWorks the version attribute
				// was not being added to the user configuration files. Therefore many layout elements were being processed
				// as if they were in the default configuration files.  The loadUserOverRides boolean was added to ensure
				// version number is added to the elements in the user configuration files.
				var fileVersion = int.Parse(XmlUtils.GetOptionalAttributeValue(node, "version", loadUserOverRides ? "0" : version.ToString()));
				//The layoutType element in user config files (i.e. a copy of the root dictionary view)
				//failed to include a version number, even though all the layout elements had one.
				if (fileVersion == 0)
				{
					//So to make sure that copies of the dictionary views get reloaded we will look for
					//the version number of a sibling node.
					if (node.Parent != null)
					{
						var versionedSibling = node.XPathSelectElement("../*[@version]");
						// ReSharper disable PossibleNullReferenceException
						// if we found a node, it has a version attribute.
						fileVersion = int.Parse(versionedSibling?.Attribute("version").Value ?? "0");
						// ReSharper restore PossibleNullReferenceException
					}
				}
				if (fileVersion != version && Merger != null && XmlUtils.GetOptionalAttributeValue(node, "base") == null)
				{
					if (node.Name == "layoutType")
					{
						AddLayoutTypeToInventory(node);
						if (loadUserOverRides)
						{
							XmlUtils.SetAttribute(node, "version", version.ToString(CultureInfo.InvariantCulture));
						}
						survivors.Add(node);
						wasMerged = true;
					}
					else
					{
						string[] keyAttrs;
						var key = GetKeyMain(node, out keyAttrs);
						XElement current;
						if (m_getElementTable.TryGetValue(key, out current))
						{
							var merged = Merger.Merge(current, node, m_mainDoc, string.Empty);
							if (loadUserOverRides)
							{
								XmlUtils.SetAttribute(merged, "version", version.ToString());
							}
							survivors.Add(merged);
							wasMerged = true;
						}
						else
						{
							// May be part of a named view or a duplicated node. Look for the unmodified one to merge with.
							string[] standardKeyVals;
							var oldLayoutSuffix = LayoutKeyUtils.GetSuffixedPartOfNamedViewOrDuplicateNode(keyAttrs, key.KeyVals, out standardKeyVals);
							if (string.IsNullOrEmpty(oldLayoutSuffix) || !m_getElementTable.TryGetValue(new GetElementKey(key.ElementName, standardKeyVals, m_mainDoc), out current))
							{
								continue;
							}
							var merged = Merger.Merge(current, node, m_mainDoc, oldLayoutSuffix);
							// We'll do the below and a bunch of other mods inside of LayoutMerger from now on.
							//XmlUtils.SetAttribute(merged, "name", originalKey[2]); // give it the name from before
							if (loadUserOverRides)
							{
								XmlUtils.SetAttribute(merged, "version", version.ToString(CultureInfo.InvariantCulture));
							}
							survivors.Add(merged);
							wasMerged = true;
						}
					}
				}
				else if (fileVersion == version)
				{
					survivors.Add(node);
				}
				// Otherwise it's an old-version node and we can't merge it, so ignore it.
				// we should remove obsolete nodes to indicate the merge so don't add it to survivors
			}
			return survivors.ToList();
		}

		private void NoteIfNodeWsTagged(XElement node)
		{
			if (node.Name == "layout" && XmlUtils.GetMandatoryAttributeValue(node, "type") == "jtview" && XmlUtils.GetOptionalBooleanAttributeValue(node, "tagForWs", false))
			{
				m_wsTaggedNodes.Add(node);
			}
		}

		/// <summary>
		/// Displaying Reversal Indexes requires expanding a variable number of writing
		/// system specific layouts.  This method does that.
		/// </summary>
		internal void ExpandWsTaggedNodes(string sWsTag)
		{
			Debug.Assert(!string.IsNullOrEmpty(sWsTag));
			Debug.Assert(m_mainDoc != null);
			var root = m_mainDoc.Root;
			foreach (var xn in m_wsTaggedNodes)
			{
				var sName = XmlUtils.GetMandatoryAttributeValue(xn, "name");
				var sWsName = $"{sName}-{sWsTag}";
				var sClass = XmlUtils.GetMandatoryAttributeValue(xn, "class");
				var sType = XmlUtils.GetMandatoryAttributeValue(xn, "type");
				Debug.Assert(xn.Name == "layout" && sType == "jtview" && XmlUtils.GetOptionalBooleanAttributeValue(xn, "tagForWs", false));
				var layout = GetElement("layout", new[] { sClass, sType, sWsName, null });
				if (layout != null)
				{
					continue;       // node has already been added.
				}
				var xnWs = xn.Clone();
				xnWs.Attribute("name").Value = sWsName;
				foreach (var xnChild in xnWs.Elements())
				{
					if (xnChild.Name == "sublayout")
					{
						var sSubName = XmlUtils.GetMandatoryAttributeValue(xnChild, "name");
						xnChild.Attribute("name").Value = $"{sSubName}-{sWsTag}";
					}
					else if (xnChild.Name == "part")
					{
						var sParam = XmlUtils.GetOptionalAttributeValue(xnChild, "param", null);
						if (!string.IsNullOrEmpty(sParam))
						{
							xnChild.Attribute("param").Value = $"{sParam}-{sWsTag}";
						}
					}
				}
				AddNode(xnWs, root);
			}
		}

		private void AddNode(XElement node, XElement root)
		{
			string[] keyAttrs;
			var keyMain = GetKeyMain(node, out keyAttrs);
			var keyVals = keyMain.KeyVals;
			var elementName = keyMain.ElementName;
			XElement extantNode;
			// Value may be null in the Dictionary, even if key is present.
			m_getElementTable.TryGetValue(keyMain, out extantNode);
			// Is the current node a derived node?
			var baseName = XmlUtils.GetOptionalAttributeValue(node, "base");
			if (baseName != null)
			{
				var id = XmlUtils.GetMandatoryAttributeValue(node, keyAttrs[keyAttrs.Length - 1]);
				if (id == baseName)
				{
					// it is an override.
					if (extantNode == null)
					{
						// Possibly trying to override a derived element?
						extantNode = GetElement(elementName, keyVals);
						if (extantNode == null)
						{
							throw new Exception($"no base found to override {baseName}");
						}
					}
					var keyBase = new GetElementKey(elementName, keyVals, m_baseDoc);
					if (m_getElementTable.ContainsKey(keyBase))
					{
						throw new Exception($"only one level of override is allowed {baseName}");
					}
					// Save the base node for future use.
					m_baseDoc.Root.Add(extantNode);
					m_getElementTable[keyBase] = extantNode;
					// Immediately compute the effect of the override and save it, replacing the base.
					var unified = Unify(node, extantNode);
					extantNode.ReplaceWith(unified);
					// and update the cache, which is loaded with the old element
					m_getElementTable[keyMain] = unified;
				}
				else
				{
					// it is a normal alteration node
					// derived node displaces non-derived one.
					extantNode?.Remove();
				}
				// alteration node goes into alterations doc (displacing any previous alteration
				// with the same key).
				var keyAlterations = new GetElementKey(elementName, keyVals, m_alterationsDoc);
				extantNode = null;
				if (m_getElementTable.ContainsKey(keyAlterations))
				{
					extantNode = m_getElementTable[keyAlterations]; // May still be null.
				}
				CopyNodeToDoc(node, extantNode, m_alterationsDoc, keyAlterations);
			}
			else // not an override, just save it, replacing existing node if needed
			{
				CopyNodeToDoc(node, extantNode, m_mainDoc, keyMain);
			}
		}

		// Get the key we use to look up the specified element, and also the array of attribute names
		// appropriate to the element type.
		private GetElementKey GetKeyMain(XElement node, out string[] keyAttrs)
		{
			var elementName = node.Name.LocalName;
			keyAttrs = null;
			if (KeyAttributes.ContainsKey(elementName))
			{
				keyAttrs = KeyAttributes[elementName];
			}
			if (keyAttrs == null)
			{
				keyAttrs = new string[0];
			}
			var keyVals = new string[keyAttrs.Length];
			var i = 0;
			foreach (var attr in keyAttrs)
			{
				var val = XmlUtils.GetOptionalAttributeValue(node, attr);
				keyVals[i++] = val;
			}
			return new GetElementKey(elementName, keyVals, m_mainDoc);
		}

		private void CopyNodeToDoc(XElement node, XElement extantNode, XDocument doc, GetElementKey key)
		{
			InsertNodeInDoc(node, extantNode, doc, key);
		}

		private void InsertNodeInDoc(XElement newNode, XElement extantNode, XDocument doc, GetElementKey key)
		{
			var root = doc.Root;
			if (extantNode == null)
			{
				root.Add(newNode);
			}
			else
			{
				extantNode.ReplaceWith(newNode);
			}
			if (newNode.Name != "layoutType")
			{
				m_getElementTable[key] = newNode;
			}
		}

		/// <summary>
		/// Collect all of the elements from wherever they come from.
		/// </summary>
		private void LoadElements()
		{
			BasicInit();
			foreach (var inventoryPath in m_inventoryPaths)
			{
				//jdh added dec 2003
				var p = FwDirectoryFinder.GetCodeSubDirectory(inventoryPath);
				if (Directory.Exists(p))
				{
					AddElementsFromFiles(DirectoryUtils.GetOrderedFiles(p, m_filePattern));
				}
			}
		}

		/// <summary>
		/// Collect all of the elements from a specific input string (only). Used in tests.
		/// </summary>
		internal void LoadElements(string input, int version)
		{
			BasicInit();
			Debug.Assert(m_mainDoc != null);
			var root = m_mainDoc.Root;
			var xdoc = XDocument.Parse(input);
			var elementList = xdoc.XPathSelectElements(m_xpathElementsWanted);
			bool dummy;
			var cleanedNodes = MergeAndUpdateNodes(elementList, version, out dummy, false);
			LoadElementList(cleanedNodes, version, root);
		}

		private void BasicInit()
		{
			// If this is called more than once,
			// it will throw away the old xml document here.
			m_fileInfo = new List<KeyValuePair<string, DateTime>>();
			m_mainDoc = new XDocument(new XElement("Main"));
			m_alterationsDoc = new XDocument(new XElement("Main"));
			m_baseDoc = new XDocument(new XElement("Main"));
			m_getElementTable.Clear();
		}

		/// <summary>
		/// Answer true if no files we load changed since last load.
		/// </summary>
		/// <returns></returns>
		private bool NoFilesChanged()
		{
			var ifile = 0;
			foreach (var inventoryPath in m_inventoryPaths)
			{
				//jdh added dec 2003
				var p = FwDirectoryFinder.GetCodeSubDirectory(inventoryPath);
				if (Directory.Exists(p))
				{
					foreach (var path in DirectoryUtils.GetOrderedFiles(p, m_filePattern))
					{
						if (ifile >= m_fileInfo.Count || m_fileInfo[ifile].Key != path || m_fileInfo[ifile].Value != File.GetLastWriteTime(path))
						{
							return false;
						}
						ifile++;
					}
				}
			}
			return ifile == m_fileInfo.Count;
		}

		/// <summary>
		/// This routine combines two elements by copying the attributes of main,
		/// and unifying the children of the two nodes in the way we do for
		/// overriding. It saves the result and will reuse it if a unification
		/// of the same two elements is requested again.
		/// </summary>
		internal XElement GetUnified(XElement main, XElement alteration)
		{
			if (main.XPathSelectElements("part[@ref='AsLexemeForm' and @shouldNotMerge='true']").Any())
			{
				return main;
			}
			XElement result;
			var key = new Tuple<XElement, XElement>(main, alteration);
			if (m_unifiedNodes.TryGetValue(key, out result))
			{
				return result; // It will not be null.
			}
			result = new XElement(main.Name);
			CopyAttributes(main, result, false);
			UnifyChildren(alteration, main, result);
			m_unifiedNodes[key] = result;
			return result; // It will not be null.
		}

		internal ISet<string> ExistingDuplicateKeys()
		{
			var set = new HashSet<string>();
			foreach (var dupKey in m_getElementTable.Keys.Select(key => key.ElementName).Select(elemName => new { elemName, idx = elemName.IndexOf('%') })
				.Where(@t => @t.idx >= 0).Select(@t => @t.elemName.Substring(@t.idx + 1)).Where(dupKey => !string.IsNullOrEmpty(dupKey) && !set.Contains(dupKey)))
			{
				set.Add(dupKey);
			}
			return set;
		}

		/// <summary>
		/// We want to override an attribute of the last part ref in the part, for the
		/// layout that is the first element.
		/// To do so, we build a modified version of that layout, which (for each part ref in path)
		/// has a child node of that name. The final part ref should have the modified attribute.
		/// </summary>
		/// <returns></returns>
		internal static XElement MakeOverride(object[] path, string attrName, string value, int version, out XElement newPartRef)
		{
			// if we are overriding an attribute in a part ref that is in a sublayout, we want to treat the sublayout as the
			// root layout, so we search starting from the end of the path for the last sublayout and if it exists we make it
			// the root layout
			int i;
			for (i = path.Length - 1; i > 0; i--)
			{
				var node = path[i] as XElement;
				if (node == null || node.Name != "sublayout")
				{
					continue;
				}

				i++;
				break;
			}
			var original = (XElement)path[i++];
			var result = original.Clone();
			XmlUtils.SetAttribute(result, "version", version.ToString());
			XElement finalPartRef = null;
			var currentParent = result;
			for (; i < path.Length; i++)
			{
				var node = path[i] as XElement;
				if (node == null || node.Name != "part")
				{
					continue;
				}
				var partId = XmlUtils.GetOptionalAttributeValue(node, "ref");
				if (partId == null)
				{
					continue; // a part node, but not a part ref.
				}
				// Handle the possibility that the parent of the part ref we want to override
				// is not directly the containing part ref. For now we just handle the special case,
				// <indent>. If the current part ref we are trying to add has such a parent,
				// try to reuse a matching one from the currentParent; failing that, make
				// a suitable parent node and make it current.
				var parent = node.Parent;
				if (parent != null && parent.Name == "indent")
				{
					var adjustParent = currentParent.Elements().FirstOrDefault(child => child.Name == parent.Name);
					if (adjustParent == null)
					{
						adjustParent = parent;
						currentParent.Add(adjustParent);
					}
					currentParent = adjustParent;
				}
				XElement currentChild = null;
				foreach (var child in currentParent.Elements())
				{
					var partIdChild = XmlUtils.GetOptionalAttributeValue(child, "ref");
					// For most children, one with the right part ID is enough, but for custom ones
					// it must have the right param, as well.
					if (partIdChild == partId && (partIdChild != "Custom" || XmlUtils.GetOptionalAttributeValue(node, "param") == XmlUtils.GetOptionalAttributeValue(child, "param")))
					{
						currentChild = child;
						break;
					}
				}
				if (currentChild == null)
				{
					// It's not one of the children of the current parent, presumably, it's a parent
					// of a layout invoked by that parent. We create a direct child to override the
					// behavior of that layout.
					currentChild = new XElement("part");
					currentParent.Add(currentChild);
					XmlUtils.SetAttribute(currentChild, "ref", partId);
					if (XmlUtils.GetOptionalAttributeValue(node, "ref") == "Custom")
					{
						// In this case (and possibly this case only, at least, we weren't doing it
						// before), we need to copy the param attribute.
						var param = XmlUtils.GetOptionalAttributeValue(node, "param");
						if (!string.IsNullOrEmpty(param))
						{
							XmlUtils.SetAttribute(currentChild, "param", param);
						}
					}
				}
				finalPartRef = currentChild;
				currentParent = currentChild; // if we continue we want a child of this next.
			}
			Debug.Assert(finalPartRef != null);
			XmlUtils.SetAttribute(finalPartRef, attrName, value);
			newPartRef = finalPartRef;
			return result;
		}

		/// <summary>
		/// Key used in Dictionary to optimize GetElementFromDoc.
		/// </summary>
		private sealed class GetElementKey
		{
			private readonly XDocument m_doc;

			internal GetElementKey(string elementName, string[] attrvals, XDocument doc)
			{
				ElementName = elementName;
				KeyVals = attrvals.Select(attrval => attrval?.ToLowerInvariant()).ToArray();
				m_doc = doc;
			}

			/// <summary />
			public override bool Equals(object obj)
			{
				var other = obj as GetElementKey;
				if (other == null)
				{
					return false;
				}
				if (other.ElementName != ElementName)
				{
					return false;
				}
				if (other.m_doc != m_doc)
				{
					return false;
				}
				if (other.KeyVals.Length != KeyVals.Length)
				{
					return false;
				}
				return !KeyVals.Where((t, i) => other.KeyVals[i] != t).Any();
			}

			/// <summary />
			public string ElementName { get; }

			/// <summary />
			public string[] KeyVals { get; }

			private static int HashZeroForNull(object obj)
			{
				return obj?.GetHashCode() ?? 0;
			}

			/// <summary />
			public override int GetHashCode()
			{
				var result = HashZeroForNull(m_doc) + HashZeroForNull(ElementName);
				foreach (var s in KeyVals)
				{
					result += HashZeroForNull(s);
				}
				return result;
			}

			/// <summary />
			public override string ToString()
			{
				var bldr = new StringBuilder();
				if (!string.IsNullOrEmpty(ElementName))
				{
					bldr.AppendFormat("{0}: ", ElementName);
				}
				if (KeyVals != null)
				{
					bldr.Append(string.Join("-", KeyVals));
				}
				return bldr.Length > 0 ? bldr.ToString() : base.ToString();
			}
		}

		/// <summary>
		/// This comparison class allows us to simplify getting only unique elements
		/// in the MergeAndUpdateNodes method below.
		/// </summary>
		private sealed class XElementCompare : IEqualityComparer<XElement>
		{
			/// <summary />
			public bool Equals(XElement first, XElement second)
			{
				return first.ToString() == second.ToString();
			}

			/// <summary />
			public int GetHashCode(XElement x)
			{
				return x.ToString().GetHashCode();
			}
		}
	}
}