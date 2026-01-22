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
		public MultiStringSlice(ICmObject obj, int flid, int ws, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
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
		{
			var singlePropertySequenceValue = StringSliceUtils.GetVisibleWSSPropertyValue(PartRef(),
				((LabeledMultiStringView) Control).GetWritingSystemOptions(false));
			return StringSliceUtils.GetVisibleWritingSystems(singlePropertySequenceValue, WritingSystemOptionsForDisplay);
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
				dlg.Text = String.Format(DetailControlsStrings.ksSliceConfigureWssDlgTitle, Label);
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
			m_propertyTable.SetProperty(display.PropertyName, StringSliceUtils.GetVisibleWSSPropertyValue(PartRef(),
				((LabeledMultiStringView) Control).GetWritingSystemOptions(false)), false);
			AddWritingSystemListWithIcuLocales(display, WritingSystemOptionsForDisplay);
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// stores the list values in terms of icu locale
		/// </summary>
		/// <param name="display"></param>
		/// <param name="list"></param>
		private void AddWritingSystemListWithIcuLocales(UIListDisplayProperties display, IEnumerable<CoreWritingSystemDefinition> list)
		{
			var active = StringSliceUtils.GetVisibleWSSPropertyValue(PartRef(),
				((LabeledMultiStringView) Control).GetWritingSystemOptions(false)).Split(',');
			foreach (var ws in list)
			{
				// generally enable all items, but if only one is checked that one is disabled;
				// it can't be turned off.
				bool enabled = (active.Length != 1 || ws.Id != active[0]);
				display.List.Add(ws.DisplayLabel, ws.Id, null, null, enabled);
			}
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
		{
			if (newValues.Count() != m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Count
				|| !m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.SequenceEqual(newValues))
			{
				NonUndoableUnitOfWorkHelper.Do(m_cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Clear();
					foreach (CoreWritingSystemDefinition ws in newValues)
						m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Add(ws);
				});
			}
		}

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
