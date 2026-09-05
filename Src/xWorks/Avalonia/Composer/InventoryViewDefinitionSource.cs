// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	internal sealed class LayoutNotFoundException : InvalidOperationException
	{
		internal LayoutNotFoundException(string message) : base(message)
		{
		}
	}

	/// <summary>
	/// Creates immutable view-definition snapshots from an effective project layout inventory.
	/// </summary>
	public sealed class InventoryViewDefinitionSource
	{
		private readonly Inventory _layouts;
		private readonly string _partsXml;
		private readonly IFwMetaDataCache _metadataCache;
		private readonly LcmCache _cache;
		private readonly Dictionary<string, XmlNode> _callerNodes =
			new Dictionary<string, XmlNode>(StringComparer.Ordinal);

		/// <summary>
		/// Creates a source backed by the current effective layouts and immutable merged parts
		/// XML.
		/// </summary>
		/// <exception cref="ArgumentNullException">A constructor argument is null.</exception>
		public InventoryViewDefinitionSource(Inventory layouts, string partsXml,
			IFwMetaDataCache metadataCache, LcmCache cache = null)
		{
			_layouts = layouts ?? throw new ArgumentNullException(nameof(layouts));
			_partsXml = partsXml ?? throw new ArgumentNullException(nameof(partsXml));
			_metadataCache = metadataCache ?? throw new ArgumentNullException(nameof(metadataCache));
			_cache = cache;
		}

		/// <summary>
		/// Gets the effective detail layout snapshot.
		/// </summary>
		/// <exception cref="LayoutNotFoundException">Neither the requested layout nor the
		/// default layout exists in the class hierarchy.</exception>
		public ViewDefinitionSourceSnapshot GetSnapshot(ICmObject obj, string layoutName,
			string choiceGuid = null, string callerXml = null)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));
			if (string.Equals(obj.ClassName, "CmCustomItem", StringComparison.OrdinalIgnoreCase))
				layoutName = CustomItemLayoutName(layoutName, obj as ICmPossibility);
			return GetSnapshot(obj.ClassName, layoutName, choiceGuid, callerXml);
		}

		/// <summary>
		/// Resolves a snapshot by class name rather than a live object. The CmCustomItem
		/// writing-system layout mapping is not applied.
		/// </summary>
		public ViewDefinitionSourceSnapshot GetSnapshot(string className, string layoutName,
			string choiceGuid = null, string callerXml = null)
		{
			var originalClassId = _metadataCache.GetClassId(className);
			var classId = originalClassId;
			var baseClassMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var requestedName = layoutName ?? "default";
			var useName = requestedName;
			var isRnGenericRecord = IsSameOrSubclass(originalClassId,
				_metadataCache.GetClassId("RnGenericRec"));
			string resolvedClassName;
			XmlNode resolvedLayout;

			while (true)
			{
				resolvedClassName = _metadataCache.GetClassName(classId);
				var layout = _layouts.GetElement("layout",
					new[] { resolvedClassName, "detail", useName, choiceGuid });
				if (layout == null && isRnGenericRecord && choiceGuid != null)
				{
					var generic = _layouts.GetElement("layout",
						new[] { resolvedClassName, "detail", useName, null });
					if (generic != null)
					{
						var clone = generic.Clone();
						XmlUtils.AppendAttribute(clone, "choiceGuid", choiceGuid);
						_layouts.AddNodeToInventory(clone);
						_layouts.PersistOverrideElement(clone);
						layout = clone;
					}
				}

				if (layout != null)
				{
					resolvedLayout = layout;
					break;
				}

				if (classId == 0 && !string.Equals(useName, "default",
					StringComparison.Ordinal))
				{
					useName = "default";
					classId = originalClassId;
					resolvedClassName = _metadataCache.GetClassName(classId);
				}
				if (classId == 0)
				{
					throw new LayoutNotFoundException("No matching layout found for class "
						+ resolvedClassName + " detail layout " + requestedName + ".");
				}
				var baseId = _metadataCache.GetBaseClsId(classId);
				if (baseId == classId)
					throw new InvalidOperationException("The metadata class hierarchy contains a cycle.");
				baseClassMap[resolvedClassName] = _metadataCache.GetClassName(baseId);
				classId = baseId;
			}

			var ancestorClassId = classId;
			while (ancestorClassId != 0)
			{
				var baseId = _metadataCache.GetBaseClsId(ancestorClassId);
				if (baseId == ancestorClassId)
					break;
				baseClassMap[_metadataCache.GetClassName(ancestorClassId)] =
					_metadataCache.GetClassName(baseId);
				if (baseId == 0)
					break;
				ancestorClassId = baseId;
			}

			if (!string.IsNullOrEmpty(callerXml))
				resolvedLayout = _layouts.GetUnified(resolvedLayout, CallerNode(callerXml));

			var effectiveLayout = XElement.Parse(resolvedLayout.OuterXml,
				LoadOptions.PreserveWhitespace);
			DetailComposer.ExpandCustomFields(effectiveLayout, _cache, originalClassId,
				placeholder => PersistPlaceholderRef(resolvedLayout, effectiveLayout,
					placeholder));

			return new ViewDefinitionSourceSnapshot(className, "detail", effectiveLayout.ToString(),
				_partsXml, new ReadOnlyDictionary<string, string>(baseClassMap), requestedName,
				choiceGuid, _cache != null);
		}

		/// <summary>
		/// Sets <c>ref="_CustomFieldPlaceholder"</c> on the live inventory node behind a
		/// placeholder the snapshot copy just expanded, then persists that node's nearest
		/// <c>layout</c> or <c>part</c> ancestor through the inventory. Mutating the node the
		/// inventory handed out, rather than the snapshot copy, matches the legacy DataTree so
		/// every holder of that node and the project layout file agree on the ref.
		/// </summary>
		/// <exception cref="InvalidOperationException">The snapshot placeholder has no
		/// counterpart in the live layout, or that counterpart has no layout or part
		/// ancestor.</exception>
		private void PersistPlaceholderRef(XmlNode liveLayout, XElement snapshotLayout,
			XElement snapshotPlaceholder)
		{
			// The snapshot copy is parsed from the live node's XML and generated Custom parts
			// carry no customFields attribute, so the placeholders line up by document order.
			var index = snapshotLayout.Descendants("part")
				.Where(part => part.Attribute("customFields") != null)
				.ToList().IndexOf(snapshotPlaceholder);
			var livePlaceholders = liveLayout.SelectNodes(".//part[@customFields]");
			if (index < 0 || livePlaceholders == null || index >= livePlaceholders.Count)
			{
				throw new InvalidOperationException(
					"The custom-field placeholder has no counterpart in the live layout.");
			}
			var livePlaceholder = livePlaceholders[index];
			if (livePlaceholder.Attributes?["ref"] == null)
				XmlUtils.AppendAttribute(livePlaceholder, "ref", "_CustomFieldPlaceholder");
			_layouts.PersistOverrideElement(FindPersistableParent(livePlaceholder));
		}

		private static XmlNode FindPersistableParent(XmlNode placeholder)
		{
			for (var parent = placeholder.ParentNode; parent != null; parent = parent.ParentNode)
			{
				if (parent.Name == "part" || parent.Name == "layout")
					return parent;
			}
			throw new InvalidOperationException(
				"No layout or part parent exists for a custom-field placeholder.");
		}

		private XmlNode CallerNode(string callerXml)
		{
			lock (_callerNodes)
			{
				if (_callerNodes.TryGetValue(callerXml, out var existing))
					return existing;
				var document = new XmlDocument();
				document.LoadXml(callerXml);
				_callerNodes[callerXml] = document.DocumentElement;
				return document.DocumentElement;
			}
		}

		private bool IsSameOrSubclass(int classId, int possibleBaseId)
		{
			while (classId != 0)
			{
				if (classId == possibleBaseId)
					return true;
				var baseId = _metadataCache.GetBaseClsId(classId);
				if (baseId == classId)
					return false;
				classId = baseId;
			}
			return possibleBaseId == 0;
		}

		private static string CustomItemLayoutName(string requestedLayoutName, ICmPossibility item)
		{
			var owningList = item?.Owner == null ? null : item.OwningList;
			if (owningList == null)
				return "CmPossibilityA";

			var selector = owningList.WsSelector;
			switch (selector)
			{
				case WritingSystemServices.kwsVerns:
					return "CmPossibilityV";
				case WritingSystemServices.kwsAnals:
					return "CmPossibilityA";
				case WritingSystemServices.kwsAnalVerns:
					return "CmPossibilityAV";
				case WritingSystemServices.kwsVernAnals:
					return "CmPossibilityVA";
				default:
					return requestedLayoutName;
			}
		}
	}
}
