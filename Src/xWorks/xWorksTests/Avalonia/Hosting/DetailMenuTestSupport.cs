// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// What the detail-menu fixtures share: loading a tool's <see cref="RecordEditView"/>,
	/// reading the rows it shows, and building, describing and running a row's menus.
	/// </summary>
	internal static class DetailMenuTestSupport
	{
		/// <summary>Makes <paramref name="toolValue"/> the current tool, so the window
		/// loads its detail view.</summary>
		public static void LoadRecordEditView(PropertyTable propertyTable, string toolValue)
		{
			var windowConfiguration = propertyTable.GetValue<XmlNode>("WindowConfiguration");
			Assert.That(windowConfiguration, Is.Not.Null);
			var controlNode = windowConfiguration.SelectSingleNode(string.Format(
				"//tool[@value='{0}']/control//control[dynamicloaderinfo/@class='SIL.FieldWorks.XWorks.RecordEditView']",
				toolValue));
			Assert.That(controlNode, Is.Not.Null, "Expected the RecordEditView configuration node for tool '{0}'.", toolValue);

			propertyTable.SetProperty("currentContentControlParameters", controlNode, true);
			propertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			propertyTable.SetProperty("currentContentControl", toolValue, true);
			propertyTable.SetPropertyPersistence("currentContentControl", false);
		}

		/// <summary>The rows of the model the host shows now, the instances its menus are
		/// asked about.</summary>
		public static List<DetailField> ShownRows(RecordEditView view)
		{
			var hostField = typeof(RecordEditView).GetField("m_avaloniaEntryForm", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(hostField, Is.Not.Null, "Missing private field: m_avaloniaEntryForm");
			var host = (DetailHostControl)hostField.GetValue(view);
			return host.ShownDetail.Fields.ToList();
		}

		/// <summary>The menus a row binds, each with the ids the host builds it from.</summary>
		public static IEnumerable<(DetailMenuKind Kind, string[] Ids)> MenusOf(DetailField row)
		{
			var label = RecordEditView.ComposeSliceMenuIds(row.MenuId, row.IsMultiStringRow).ToArray();
			if (label.Length > 0)
				yield return (DetailMenuKind.SliceMenu, label);
			var inString = RecordEditView.ComposeInStringMenuIds(row.ContextMenuId).ToArray();
			if (inString.Length > 0)
				yield return (DetailMenuKind.ContextMenu, inString);
			if (!string.IsNullOrEmpty(row.HotlinksId))
				yield return (DetailMenuKind.Hotlinks, new[] { row.HotlinksId });
		}

		/// <summary>The row's label menu as the host builds it, which no hidden tree may
		/// answer.</summary>
		public static IReadOnlyList<DetailMenuItem> MenuItems(RecordEditView view, PropertyTable propertyTable,
			DetailField row)
		{
			var window = propertyTable.GetValue<XWindow>("window");
			var ids = RecordEditView.ComposeSliceMenuIds(row.MenuId, row.IsMultiStringRow).ToArray();
			var authority = view.CreateMenuAuthority(DetailMenuRequest.FromAnchor(null, row, DetailMenuKind.SliceMenu));
			Assert.That(XCoreMenuBridge.OwnsAll(authority, ids), Is.True, "the menu is answered natively");
			return XCoreMenuBridge.CreateMenuItems(window, ids, null, null, authority);
		}

		/// <summary>The item with that label, which the menu must offer.</summary>
		public static DetailMenuItem Item(IReadOnlyList<DetailMenuItem> items, string label)
		{
			var item = items.FirstOrDefault(i => i.Label == label);
			Assert.That(item, Is.Not.Null, "the menu offers '{0}'", label);
			return item;
		}

		/// <summary>Runs the item with that label, which must be enabled.</summary>
		public static void Run(IReadOnlyList<DetailMenuItem> items, string label)
		{
			var item = Item(items, label);
			Assert.That(item.Execute, Is.Not.Null, "'{0}' is enabled", label);
			item.Execute();
		}

		public static Command CommandOf(PropertyTable propertyTable, string id)
			=> (Command)propertyTable.GetValue<XWindow>("window").Mediator.CommandSet[id];

		public static UIItemDisplayProperties Display() => new UIItemDisplayProperties(null, "item", false, null, true);

		/// <summary>One line per item with its state, submenus indented, for comparing two
		/// builds of a menu.</summary>
		public static string Describe(IReadOnlyList<DetailMenuItem> items, string indent = "")
		{
			var lines = new List<string>();
			foreach (var item in items)
			{
				if (item.IsSeparator)
				{
					lines.Add(indent + "---");
					continue;
				}
				lines.Add(string.Format("{0}{1} [enabled={2} checked={3} executes={4}]", indent, item.Label,
					item.IsEnabled, item.IsChecked, item.Execute != null));
				if (item.Children.Count > 0)
					lines.Add(Describe(item.Children, indent + "  "));
			}
			return string.Join(Environment.NewLine, lines);
		}

		/// <summary>Points the hidden tree at a row, for the menu the adapter path
		/// renders.</summary>
		public static void EnsureAdapter(RecordEditView view, int targetHvo, string fieldName)
		{
			var method = typeof(RecordEditView).GetMethod("EnsureMenuCommandAdapter",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "EnsureMenuCommandAdapter must exist (adapter targeting seam)");
			method.Invoke(view, new object[] { targetHvo, fieldName });
		}
	}

	/// <summary>Answers no display query of the detail menus' messages, only records that
	/// one arrived.</summary>
	internal sealed class MenuDisplaySpyColleague : IxCoreColleague
	{
		public bool Asked { get; private set; }
		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters) { }
		public IxCoreColleague[] GetMessageTargets() => new IxCoreColleague[] { this };
		public bool ShouldNotCall => false;
		public int Priority => (int)ColleaguePriority.High;

		public bool OnDisplayDataTreeDelete(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayShowFieldAlwaysVisible(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplaySwapLexemeWithAllomorph(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayConvertLexemeForm(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayDataTreeInsert(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayLaunchGuiControl(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayMoveUpObjectInSequence(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayMoveDownObjectInSequence(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayInsertMediaFile(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayPictureProperties(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayDataTreeMerge(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayDataTreeDeleteReference(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayDataTreeAddReference(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayDataTreeEdit(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplaySwapAllomorphWithLexeme(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayConvertAllomorph(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayDataTreeCopy(object commandObject, ref UIItemDisplayProperties display) => Record();
		public bool OnDisplayDataTreeMove(object commandObject, ref UIItemDisplayProperties display) => Record();

		private bool Record()
		{
			Asked = true;
			return false;
		}
	}
}
