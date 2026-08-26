// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel.Core.KernelInterfaces;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Creates immutable view-definition snapshots from an effective project layout inventory.
	/// </summary>
	public sealed class InventoryViewDefinitionSource
	{
		private readonly Inventory _layouts;
		private readonly string _partsXml;
		private readonly IFwMetaDataCache _metadataCache;

		/// <summary>
		/// Creates a source backed by the current effective layouts and immutable merged parts
		/// XML.
		/// </summary>
		/// <exception cref="ArgumentNullException">A constructor argument is null.</exception>
		public InventoryViewDefinitionSource(Inventory layouts, string partsXml,
			IFwMetaDataCache metadataCache)
		{
			_layouts = layouts ?? throw new ArgumentNullException(nameof(layouts));
			_partsXml = partsXml ?? throw new ArgumentNullException(nameof(partsXml));
			_metadataCache = metadataCache ?? throw new ArgumentNullException(nameof(metadataCache));
		}

		/// <summary>
		/// Gets the effective detail layout snapshot, or null when the class hierarchy has no
		/// match.
		/// </summary>
		public ViewDefinitionSourceSnapshot GetSnapshot(string className, string layoutName,
			string choiceGuid = null)
		{
			var classId = _metadataCache.GetClassId(className);
			var baseClassMap = new Dictionary<string, string>(StringComparer.Ordinal);
			string resolvedClassName;
			string layoutXml;

			while (true)
			{
				resolvedClassName = _metadataCache.GetClassName(classId);
				var layout = _layouts.GetElement("layout",
					new[] { resolvedClassName, "detail", layoutName, choiceGuid });
				if (layout == null)
				{
					layout = _layouts.GetElement("layout",
						new[] { resolvedClassName, "detail", layoutName, null });
				}

				if (layout != null)
				{
					layoutXml = layout.OuterXml;
					break;
				}

				if (classId == 0)
					return null;
				var baseId = _metadataCache.GetBaseClsId(classId);
				if (baseId == classId)
					return null;
				baseClassMap[resolvedClassName] = _metadataCache.GetClassName(baseId);
				classId = baseId;
			}

			var ancestorClassId = classId;
			while (ancestorClassId != 0)
			{
				var baseId = _metadataCache.GetBaseClsId(ancestorClassId);
				if (baseId == ancestorClassId || baseId == 0)
					break;
				baseClassMap[_metadataCache.GetClassName(ancestorClassId)] =
					_metadataCache.GetClassName(baseId);
				ancestorClassId = baseId;
			}

			return new ViewDefinitionSourceSnapshot(resolvedClassName, "detail", layoutXml,
				_partsXml, new ReadOnlyDictionary<string, string>(baseClassMap));
		}
	}
}
