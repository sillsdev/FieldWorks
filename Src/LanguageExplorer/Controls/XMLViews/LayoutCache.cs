// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using SIL.Code;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class caches the layout and part inventories and optimizes looking up a particular item.
	/// </summary>
	public class LayoutCache
	{
		IFwMetaDataCache m_mdc;
		readonly Inventory m_partInventory;
		readonly Dictionary<Tuple<int, string, bool>, XElement> m_map = new Dictionary<Tuple<int, string, bool>, XElement>();

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		/// <remarks>TESTS ONLY.</remarks>
		public LayoutCache(IFwMetaDataCache mdc, Inventory layouts, Inventory parts)
		{
			m_mdc = mdc;
			LayoutInventory = layouts;
			m_partInventory = parts;
		}

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		public LayoutCache(IFwMetaDataCache mdc, string sDatabase, string applicationName, string projectPath)
		{
			m_mdc = mdc;
			LayoutInventory = Inventory.GetInventory("layouts", sDatabase);
			m_partInventory = Inventory.GetInventory("parts", sDatabase);
			if (LayoutInventory != null && m_partInventory != null)
			{
				return;
			}
			InitializePartInventories(sDatabase, applicationName, projectPath);
			LayoutInventory = Inventory.GetInventory("layouts", sDatabase);
			m_partInventory = Inventory.GetInventory("parts", sDatabase);
		}

		/// <summary>
		/// Layout Version Number (last updated by GordonM, 10 June 2016, as part of Etymology cluster update).
		/// </summary>
		/// <remarks>Note: often we also want to update BrowseViewer.kBrowseViewVersion.</remarks>
		public static readonly int LayoutVersionNumber = 25;

		/// <summary>
		/// Initializes the part inventories.
		/// </summary>
		public static void InitializePartInventories(string sDatabase, string applicationName, string projectPath)
		{
			InitializePartInventories(sDatabase, applicationName, true, projectPath);
		}

		/// <summary>
		/// Initialize the part inventories.
		/// </summary>
		public static void InitializePartInventories(string sDatabase, string applicationName, bool fLoadUserOverrides, string projectPath)
		{
			Guard.AgainstNullOrEmptyString(applicationName, nameof(applicationName));

			var partDirectory = Path.Combine(FwDirectoryFinder.FlexFolder, Path.Combine("Configuration", "Parts"));
			var keyAttrs = new Dictionary<string, string[]>
			{
				["layout"] = new[] {"class", "type", "name", "choiceGuid"},
				["group"] = new[] {"label"},
				["part"] = new[] {"ref"}
			};

			var layoutInventory = new Inventory(new[] {partDirectory}, "*.fwlayout", "/LayoutInventory/*", keyAttrs, applicationName, projectPath)
			{
				Merger = new LayoutMerger()
			};
			// Holding shift key means don't use extant preference file, no matter what.
			// This includes user overrides of layouts.
			if (fLoadUserOverrides && System.Windows.Forms.Control.ModifierKeys != System.Windows.Forms.Keys.Shift)
			{
				layoutInventory.LoadUserOverrides(LayoutVersionNumber, sDatabase);
			}
			else
			{
				layoutInventory.DeleteUserOverrides(sDatabase);
				// LT-11193: The above may leave some user defined dictionary views to be loaded.
				layoutInventory.LoadUserOverrides(LayoutVersionNumber, sDatabase);
			}
			Inventory.SetInventory("layouts", sDatabase, layoutInventory);

			keyAttrs = new Dictionary<string, string[]>
			{
				["part"] = new[] {"id"}
			};

			Inventory.SetInventory("parts", sDatabase, new Inventory(new[] {partDirectory}, "*Parts.xml", "/PartInventory/bin/*", keyAttrs, applicationName, projectPath));
		}

		/// <summary>
		/// Displaying Reversal Indexes requires expanding a variable number of writing system
		/// specific layouts.  This method does that for a specific writing system and database.
		/// </summary>
		public static void InitializeLayoutsForWsTag(string sWsTag, string sDatabase)
		{
			var layouts = Inventory.GetInventory("layouts", sDatabase);
			layouts?.ExpandWsTaggedNodes(sWsTag);
		}

		static readonly char[] ktagMarkers = { '-', LayoutKeyUtils.kcMarkLayoutCopy, LayoutKeyUtils.kcMarkNodeCopy };

		/// <summary>
		/// Gets the node.
		/// </summary>
		public XElement GetNode(int clsid, string layoutName, bool fIncludeLayouts)
		{
			var key = Tuple.Create(clsid, layoutName, fIncludeLayouts);
			if (m_map.ContainsKey(key))
			{
				return m_map[key];
			}

			XElement node;
			var classId = clsid;
			var useName = layoutName ?? "default";
			var origName = useName;
			for( ; ; )
			{
				var classname = m_mdc.GetClassName(classId);
				if (fIncludeLayouts)
				{
					// Inventory of layouts has keys class, type, name
					node = LayoutInventory.GetElement("layout", new[] {classname, "jtview", useName, null});
					if (node != null)
					{
						break;
					}
				}
				// inventory of parts has key id.
				node = m_partInventory.GetElement("part", new[] {classname + "-Jt-" + useName});
				if (node != null)
				{
					break;
				}
				if (classId == 0 && useName == origName)
				{
					// This is somewhat by way of robustness. When we generate a modified layout name we should generate
					// a modified layout to match. If something slips through the cracks, use the unmodified original
					// view in preference to a default view of Object.
					var index = origName.IndexOfAny(ktagMarkers);
					if (index > 0)
					{
						useName = origName.Substring(0, index);
						classId = clsid;
						continue;
					}
				}
				if (classId == 0 && useName != "default")
				{
					// Nothing found all the way to CmObject...try default layout.
					useName = "default";
					classId = clsid;
					continue; // try again with the main class, don't go to its base class at once.
				}
				if (classId == 0)
				{
					if (fIncludeLayouts)
					{
						// Really surprising...default view not found on CmObject??
						throw new ApplicationException("No matching layout found for class " + classname + " jtview layout " + origName);
					}
					// okay to not find specific custom parts...we can generate them.
					return null;
				}
				// Otherwise try superclass.
				classId = m_mdc.GetBaseClsId(classId);
			}
			m_map[key] = node; // find faster next time!
			return node;
		}

		/// <summary>
		/// Gets the layout inventory.
		/// </summary>
		public Inventory LayoutInventory { get; }
	}
}