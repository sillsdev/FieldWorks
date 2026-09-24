// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The native authority for the per-object menu every detail row merges (Field Visibility,
	/// Move Field, Help) and for the empty Help menu most rows bind. Field Visibility and Move
	/// Field act on the row's node in the project override layer; Help opens the row's own
	/// topic. A row whose override target cannot be located keeps those items, disabled.
	/// </summary>
	internal sealed class ObjectMenuAuthority : IDetailMenuAuthority
	{
		internal const string MenuId = RecordEditView.ObjectMenuId;
		internal const string HelpMenuId = "mnuDataTree-Help";
		internal const string AlwaysVisibleCommandId = "CmdAlwaysVisible";
		internal const string IfDataCommandId = "CmdIfData";
		internal const string NormallyHiddenCommandId = "CmdNormallyHidden";
		internal const string MoveFieldUpCommandId = "CmdDataTree-MoveFieldUp";
		internal const string MoveFieldDownCommandId = "CmdDataTree-MoveFieldDown";
		internal const string HelpCommandId = "CmdDataTree-Help";

		private readonly Lazy<OverrideTarget> _target;
		private readonly Func<string, OverrideTarget, ViewVisibility, DetailMenuItem> _fieldVisibility;
		private readonly Func<string, OverrideTarget, bool, DetailMenuItem> _moveField;
		private readonly Lazy<string> _helpTopic;
		private readonly Action<string> _showHelp;

		/// <summary>Creates the authority for one row's menu.</summary>
		/// <param name="field">The row.</param>
		/// <param name="locateTarget">The row's override target, or null when it cannot be
		/// located; asked once, on the first Field Visibility or Move Field leaf.</param>
		/// <param name="fieldVisibility">Builds a Field Visibility item from (label, target,
		/// visibility).</param>
		/// <param name="moveField">Builds a Move Field item from (label, target, up).</param>
		/// <param name="helpTopic">The row's help topic when the help provider has it, else
		/// null; asked once.</param>
		/// <param name="showHelp">Opens a help topic.</param>
		public ObjectMenuAuthority(DetailField field, Func<DetailField, OverrideTarget> locateTarget,
			Func<string, OverrideTarget, ViewVisibility, DetailMenuItem> fieldVisibility,
			Func<string, OverrideTarget, bool, DetailMenuItem> moveField,
			Func<DetailField, string> helpTopic, Action<string> showHelp)
		{
			if (field == null)
				throw new ArgumentNullException(nameof(field));
			if (locateTarget == null)
				throw new ArgumentNullException(nameof(locateTarget));
			if (helpTopic == null)
				throw new ArgumentNullException(nameof(helpTopic));
			_fieldVisibility = fieldVisibility ?? throw new ArgumentNullException(nameof(fieldVisibility));
			_moveField = moveField ?? throw new ArgumentNullException(nameof(moveField));
			_showHelp = showHelp ?? throw new ArgumentNullException(nameof(showHelp));
			_target = new Lazy<OverrideTarget>(() => locateTarget(field));
			_helpTopic = new Lazy<string>(() => helpTopic(field));
		}

		public bool Owns(string menuId)
			=> string.Equals(menuId, MenuId, StringComparison.Ordinal)
				|| string.Equals(menuId, HelpMenuId, StringComparison.Ordinal);

		public DetailMenuItem Build(string menuId, ChoiceBase leaf)
		{
			if (leaf == null)
				throw new ArgumentNullException(nameof(leaf));
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			switch (leaf.HelpId)
			{
				case AlwaysVisibleCommandId:
					return VisibilityItem(label, ViewVisibility.Always);
				case IfDataCommandId:
					return VisibilityItem(label, ViewVisibility.IfData);
				case NormallyHiddenCommandId:
					return VisibilityItem(label, ViewVisibility.Never);
				case MoveFieldUpCommandId:
					return MoveItem(label, up: true);
				case MoveFieldDownCommandId:
					return MoveItem(label, up: false);
				case HelpCommandId:
					return HelpItem(label);
				default:
					throw new InvalidOperationException(string.Format(
						"Menu '{0}' has a leaf '{1}' this authority does not answer.", menuId, leaf.HelpId));
			}
		}

		// Offered on every row; without a located target it is disabled rather than guessed.
		private DetailMenuItem VisibilityItem(string label, ViewVisibility visibility)
			=> _target.Value == null ? Disabled(label) : _fieldVisibility(label, _target.Value, visibility);

		private DetailMenuItem MoveItem(string label, bool up)
			=> _target.Value == null ? Disabled(label) : _moveField(label, _target.Value, up);

		// Hidden when the help provider has no topic for the row, as WinForms hides it.
		private DetailMenuItem HelpItem(string label)
		{
			var topic = _helpTopic.Value;
			if (topic == null)
				return null;
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => _showHelp(topic));
		}

		private static DetailMenuItem Disabled(string label)
			=> new DetailMenuItem(label, isEnabled: false, isChecked: false, children: null, execute: null);
	}

	/// <summary>
	/// A row's node in its own compiled model with the current override applied: what Field
	/// Visibility reads its checkmark from and Move Field its enablement.
	/// </summary>
	internal sealed class OverrideTarget
	{
		public OverrideTarget(string templateId, ViewNodeLocation location)
		{
			TemplateId = templateId ?? throw new ArgumentNullException(nameof(templateId));
			Location = location ?? throw new ArgumentNullException(nameof(location));
		}

		/// <summary>The row's template id, without its runtime suffix.</summary>
		public string TemplateId { get; }

		/// <summary>The node's position and visibility among its siblings.</summary>
		public ViewNodeLocation Location { get; }
	}
}
