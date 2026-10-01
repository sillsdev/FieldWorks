// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for ViewPropertyItem.
	/// </summary>
	public class MultiStringSlice : ViewPropertySlice, IWritingSystemChooser
	{
		// The writing-system sets this slice was built for, kept so the Writing Systems menu can
		// be answered by the rule both detail views share.
		private int m_wsMagic;
		private int m_wsMagicOptional;
		private bool m_forceIncludeEnglish;

		public MultiStringSlice(ICmObject obj, int flid, int ws, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			m_wsMagic = ws;
			m_wsMagicOptional = wsOptional;
			m_forceIncludeEnglish = forceIncludeEnglish;
			var view = new LabeledMultiStringView(obj.Hvo, flid, ws, wsOptional, forceIncludeEnglish, editable, spellCheck);
			Control = view;
#if _DEBUG
			Control.CheckForIllegalCrossThreadCalls = true;
#endif
			InternalInitialize();
			Reuse(obj, flid);
			view.InnerView.Display += view_Display;
			view.InnerView.RightMouseClickedEvent += HandleRightMouseClickedEvent;
			view.InnerView.LostFocus += view_LostFocus;
		}

		/// <summary>
		/// Get the rootsite. It's important to use this method to get the rootsite, not to
		/// assume that the control is a rootsite, because some classes override and insert
		/// another layer of control, with the root site being a child.
		/// </summary>
		public override RootSite RootSite
		{
			get
			{
				CheckDisposed();
				var view = (LabeledMultiStringView)Control;
				return view.InnerView;
			}
		}

		/// <summary>
		/// Reset the slice to the state as if it had been constructed with these arguments. (It is going to be
		/// reused for a different record.)
		/// </summary>
		public void Reuse(ICmObject obj, int flid, int ws, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			m_wsMagic = ws;
			m_wsMagicOptional = wsOptional;
			m_forceIncludeEnglish = forceIncludeEnglish;
			var view = (LabeledMultiStringView)Control;
			Label = null; // new slice normally has this
			SetupWssToDisplay();
			view.Reuse(obj.Hvo, flid, ws, wsOptional, forceIncludeEnglish, editable, spellCheck);
		}

		public override void FinishInit()
		{
			base.FinishInit();
			((LabeledMultiStringView)Control).FinishInit(ConfigurationNode);
		}

		void view_LostFocus(object sender, EventArgs e)
		{
			DoSideEffects();
		}

		private void DoSideEffects()
		{
			string sideEffectMethod = XmlUtils.GetOptionalAttributeValue(m_configurationNode, "sideEffectMethod");
			if (string.IsNullOrEmpty(sideEffectMethod))
				return;
			ReflectionHelper.CallMethod(Object, sideEffectMethod, null);
		}

		private void view_Display(object sender, VwEnvEventArgs e)
		{
			XmlVc.ProcessProperties(ConfigurationNode, e.Environment);
		}

		void HandleRightMouseClickedEvent(SimpleRootSite sender, FwRightMouseClickEventArgs e)
		{
			string sMenu = XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "contextMenu");
			if (String.IsNullOrEmpty(sMenu))
				return;
			e.EventHandled = true;
			e.Selection.Install();
			var xwind = m_propertyTable.GetValue<XWindow>("window");
			xwind.ShowContextMenu(sMenu, new Point(Cursor.Position.X, Cursor.Position.Y), null, null);
		}

		/// <summary>
		/// Gets a list of the visible writing systems stored in our layout part ref override.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CoreWritingSystemDefinition> GetVisibleWritingSystems()
			=> FieldWritingSystemOptions.Shown(m_cache, WritingSystemSpec(), StoredWritingSystems());

		// The sets this field draws on, as the shared rule takes them.
		private WritingSystemFieldSpec WritingSystemSpec()
			=> WritingSystemFieldSpec.FromMagicIds(Object?.Hvo ?? 0, m_wsMagic,
				m_wsMagicOptional, m_forceIncludeEnglish);

		// The selection persisted on this field's part ref, or null when it has none and the
		// field shows its default set.
		private IReadOnlyList<string> StoredWritingSystems()
		{
			var stored = XmlUtils.GetOptionalAttributeValue(PartRef(), "visibleWritingSystems", null);
			return stored == null ? null : ChoiceGroup.DecodeSinglePropertySequenceValue(stored);
		}

		public override void Install(DataTree parent)
		{
			base.Install(parent);
			// setup the visible writing systems for our control
			// (We should have called MakeRoot on our control by now)
			SetupWssToDisplay();
		}

		/// <summary>
		/// Setup our view's Wss to display from our persisted layout/part ref override
		/// </summary>
		private void SetupWssToDisplay()
		{
			WritingSystemsSelectedForDisplay = GetVisibleWritingSystems();
		}

		/// <summary>
		/// Make a selection in the specified writing system at the specified character offset.
		/// Note: selecting other than the first writing system is not yet implemented.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="ich"></param>
		public void SelectAt(int ws, int ich)
		{
			CheckDisposed();
			((LabeledMultiStringView) Control).SelectAt(ws, ich);
		}

		/// <summary>
		/// Get the writing systems that are available for displaying on our slice.
		/// </summary>
		public IEnumerable<CoreWritingSystemDefinition> WritingSystemOptionsForDisplay
		{
			get { return ((LabeledMultiStringView) Control).WritingSystemOptions; }
		}

		/// <summary>
		/// Get/Set the writing systems selected to be displayed for this kind of slice.
		/// </summary>
		public IEnumerable<CoreWritingSystemDefinition> WritingSystemsSelectedForDisplay
		{
			get
			{
				// If we're not initialized enough to know what ones are being displayed,
				// get the default we expect to be initialized to.
				if (Control == null)
					return GetVisibleWritingSystems();
				var result = ((LabeledMultiStringView) Control).WritingSystemsToDisplay;
				if (result.Count == 0)
					return GetVisibleWritingSystems();
				return result;
			}
			set
			{
				var labeledMultiStringView = (LabeledMultiStringView) Control;
				if (labeledMultiStringView.WritingSystemsToDisplay?.SequenceEqual(value) ?? false)
					return; // no change.
				labeledMultiStringView.WritingSystemsToDisplay = value.ToList();
				labeledMultiStringView.RefreshDisplay();
			}
		}

		/// <summary>
		/// Show all the available writing system fields for this slice, while it is the "current" slice
		/// on the data tree. When it is no longer current, we'll reload/refresh the slice in SetCurrentState().
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public bool OnDataTreeWritingSystemsShowAll(object args)
		{
			CheckDisposed();
			SetWssToDisplayForPart(WritingSystemOptionsForDisplay);
			return true;
		}

		/// <summary>
		/// Show a dialog to allow the user to select/unselect multiple writing systems
		/// at a time, whether or not to display them (if they don't have data)
		/// If they do have data, we show the fields anyhow.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public bool OnDataTreeWritingSystemsConfigureDlg(object args)
		{
			CheckDisposed();

			ReloadWssToDisplayForPart();
			using (var dlg = new ConfigureWritingSystemsDlg(WritingSystemOptionsForDisplay, WritingSystemsSelectedForDisplay,
				m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				dlg.Text = ConfigureWritingSystemsDlg.TitleFor(Label);
				if (dlg.ShowDialog() == DialogResult.OK)
					PersistAndRedisplayWssToDisplayForPart(dlg.SelectedWritingSystems);
			}
			return true;
		}

		/// <summary>
		/// when our slice moves from being current to not being current,
		/// we want to redisplay the writing systems configured for that slice,
		/// since the user may have selected "Show all for now" which is only
		/// valid while the slice is current.
		/// </summary>
		/// <param name="isCurrent"></param>
		public override void SetCurrentState(bool isCurrent)
		{
			if (!isCurrent)
			{
				ReloadWssToDisplayForPart();
				DoSideEffects();
			}
			base.SetCurrentState(isCurrent);
		}

		/// <summary>
		/// reload the WssToDisplay if we haven't defined any, since
		/// OnDataTreeWritingSystemsShowAll may have temporary masked them.
		/// </summary>
		private void ReloadWssToDisplayForPart()
		{
			if (WritingSystemsSelectedForDisplay == null)
			{
				SetWssToDisplayForPart(GetVisibleWritingSystems());
			}
		}

		/// <summary>
		/// Populate the writing system options for the slice.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <param name="display">The display.</param>
		/// <returns></returns>
		public bool OnDisplayWritingSystemOptionsForSlice(object parameter, ref UIListDisplayProperties display)
		{
			CheckDisposed();
			display.List.Clear();
			// Options, checkmarks and the cannot-empty-the-set rule all come from the shared
			// rule, so this menu and the Avalonia one offer the same list.
			var options = FieldWritingSystemOptions.Menu(m_cache, WritingSystemSpec(),
				StoredWritingSystems());
			m_propertyTable.SetProperty(display.PropertyName,
				ChoiceGroup.EncodeSinglePropertySequenceValue(
					options.Where(o => o.IsChecked).Select(o => o.Id).ToArray()), false);
			foreach (var option in options)
				display.List.Add(option.Label, option.Id, null, null, option.CanUncheck);
			return true;//we handled this, no need to ask anyone else.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when property changed.
		/// </summary>
		/// <param name="name">The name.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void OnPropertyChanged(string name)
		{
			CheckDisposed();

			switch (name)
			{
				case "SelectedWritingSystemHvosForCurrentContextMenu":
					string singlePropertySequenceValue = m_propertyTable.GetStringProperty("SelectedWritingSystemHvosForCurrentContextMenu", null);
					PersistAndRedisplayWssToDisplayForPart(singlePropertySequenceValue);
					break;
				default:
					break;
			}
		}

		private void PersistAndRedisplayWssToDisplayForPart(IEnumerable<CoreWritingSystemDefinition> wssToDisplay)
		{
			PersistAndRedisplayWssToDisplayForPart(StringSliceUtils.EncodeWssToDisplayPropertyValue(wssToDisplay));
		}

		private void PersistAndRedisplayWssToDisplayForPart(string singlePropertySequenceValue)
		{
			ReplacePartWithNewAttribute("visibleWritingSystems", singlePropertySequenceValue);
			var wssToDisplay = StringSliceUtils.GetVisibleWritingSystems(singlePropertySequenceValue, WritingSystemOptionsForDisplay);
			if (Key.Length > 0)
			{
				XmlNode lastKey = Key[Key.Length - 1] as XmlNode;
				// This is a horrible kludge to implement LT-9620 and catch the fact that we are changing the list
				// of current pronunciation writing systems, and update the database.
				if (lastKey != null && XmlUtils.GetOptionalAttributeValue(lastKey, "menu") == "mnuDataTree-Pronunciation")
					UpdatePronunciationWritingSystems(wssToDisplay);
			}
			SetWssToDisplayForPart(wssToDisplay);
		}

		/// <summary>
		/// Get the language project's list of pronunciation writing systems into sync with the supplied list.
		/// </summary>
		private void UpdatePronunciationWritingSystems(IEnumerable<CoreWritingSystemDefinition> newValues)
			=> PronunciationWritingSystems.Sync(m_cache, newValues);

		/// <summary>
		/// go through all the data tree slices, finding the slices that refer to the same part as this slice
		/// setting them to the same writing systems to display
		/// and redisplaying their views.
		/// </summary>
		/// <param name="wssToDisplay"></param>
		private void SetWssToDisplayForPart(IEnumerable<CoreWritingSystemDefinition> wssToDisplay)
		{
			XmlNode ourPart = this.PartRef();
			var writingSystemsToDisplay = wssToDisplay == null ? null : wssToDisplay.ToList();
			foreach (Control c in ContainingDataTree.Slices)
			{
				var slice = (Slice) c;
				XmlNode part = slice.PartRef();
				if (part == ourPart)
				{
					((LabeledMultiStringView) slice.Control).WritingSystemsToDisplay = writingSystemsToDisplay;
					((LabeledMultiStringView) slice.Control).RefreshDisplay();
				}
			}
		}
	}
}
