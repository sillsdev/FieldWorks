using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for ViewPropertyItem.
	/// </summary>
	public class MultiStringSlice : ViewPropertySlice
	{
		public MultiStringSlice(int hvoObj, int flid, int ws, bool forceIncludeEnglish, bool editable, bool spellCheck)
			: base(new LabeledMultiStringView(hvoObj, flid, ws, forceIncludeEnglish, editable, spellCheck), hvoObj, flid)
		{
			LabeledMultiStringView view = Control as LabeledMultiStringView;
			view.Display += new VwEnvEventHandler(view_Display);
			view.RightMouseClickedEvent += new FwRightMouseClickEventHandler(HandleRightMouseClickedEvent);
			view.LostFocus += new EventHandler(view_LostFocus);
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
			ReflectionHelper.CallMethod(this.Object, sideEffectMethod, null);
		}

		private void view_Display(object sender, VwEnvEventArgs e)
		{
			XmlVc.ProcessProperties(this.ConfigurationNode, e.Environment);
		}

		void HandleRightMouseClickedEvent(SimpleRootSite sender, FwRightMouseClickEventArgs e)
		{
			string sMenu = XmlUtils.GetOptionalAttributeValue(this.ConfigurationNode, "contextMenu");
			if (String.IsNullOrEmpty(sMenu))
				return;
			e.EventHandled = true;
			e.Selection.Install();
			XWindow xwind = (XWindow)this.Mediator.PropertyTable.GetValue("window");
			xwind.ShowContextMenu(sMenu, new Point(Cursor.Position.X, Cursor.Position.Y), null, null);
		}

		/// <summary>
		/// Gets a list of the visible writing systems stored in our layout part ref override.
		/// </summary>
		/// <returns></returns>
		ILgWritingSystem[] GetVisibleWritingSystems()
		{
			string singlePropertySequenceValue = GetVisibleWSSPropertyValue();
			return GetVisibleWritingSystems(singlePropertySequenceValue);
		}

		/// <summary>
		/// Get the visible writing systems list in terms of a singlePropertySequenceValue string.
		/// if it hasn't been defined yet, we'll use the WritingSystemOptions for default.
		/// </summary>
		/// <returns></returns>
		public string GetVisibleWSSPropertyValue()
		{
			XmlNode partRef = PartRef();
			string singlePropertySequenceValue = XmlUtils.GetOptionalAttributeValue(partRef, "visibleWritingSystems", null);
			if (singlePropertySequenceValue == null)
			{
				// Encode a sinqlePropertySequenceValue property value using only current WritingSystemOptions.
				ILgWritingSystem[] wssOptions = (Control as LabeledMultiStringView).GetWritingSystemOptions(false);
				singlePropertySequenceValue = EncodeWssToDisplayPropertyValue(wssOptions);
			}
			return singlePropertySequenceValue;
		}

		/// <summary>
		/// convert the given writing systems into a property containing comma-delimited icuLocales.
		/// </summary>
		/// <param name="wss"></param>
		/// <returns></returns>
		private static string EncodeWssToDisplayPropertyValue(ILgWritingSystem[] wss)
		{
			List<string> icuLocaleList = new List<string>();
			foreach (ILgWritingSystem lws in wss)
			{
				icuLocaleList.Add(lws.ICULocale);
			}
			return ChoiceGroup.EncodeSinglePropertySequenceValue(icuLocaleList.ToArray());
		}

		private static string EncodeWssToDisplayPropertyValue(List<int> hvos, FdoCache cache)
		{
			return EncodeWssToDisplayPropertyValue(LabeledMultiStringView.WssFromHvos(hvos.ToArray(), cache));
		}

		private static List<int> HvosFromWss(ILgWritingSystem[] wss)
		{
			List<int> hvos = new List<int>();
			foreach (ILgWritingSystem lws in wss)
				hvos.Add(lws.Hvo);
			return hvos;
		}

		private ILgWritingSystem[] GetVisibleWritingSystems(string singlePropertySequenceValue)
		{
			string[] icuLocales = ChoiceGroup.DecodeSinglePropertySequenceValue(singlePropertySequenceValue);
			Set<string> icuLocaleSet = new Set<string>(icuLocales);
			List<int> wsList = new List<int>();
			// convert the icu locale ids into hvo ids.
			foreach (ILgWritingSystem lws in WritingSystemOptionsForDisplay)
			{
				if (icuLocaleSet.Contains(lws.ICULocale))
					wsList.Add(lws.Hvo);
			}
			// convert the ws hvos into ws object array.
			return LabeledMultiStringView.WssFromHvos(wsList.ToArray(), Cache);
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
			(Control as LabeledMultiStringView).SelectAt(ws, ich);
		}

		/// <summary>
		/// Get the writing systems that are available for displaying on our slice.
		/// </summary>
		public ILgWritingSystem[] WritingSystemOptionsForDisplay
		{
			get { return (Control as LabeledMultiStringView).WritingSystemOptions; }
		}

		/// <summary>
		/// Get/Set the writing systems selected to be displayed for this kind of slice.
		/// </summary>
		private ILgWritingSystem[] WritingSystemsSelectedForDisplay
		{
			get { return (Control as LabeledMultiStringView).WritingSystemsToDisplay; }
			set { (Control as LabeledMultiStringView).WritingSystemsToDisplay = value; }
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
			// NOTE: setting to 'null' causes all the available writing system fields to show.
			SetWssToDisplayForPart(null);
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

			Set<int> wsSet = new Set<int>(HvosFromWss(WritingSystemOptionsForDisplay));
			ObjectLabelCollection labels = new ObjectLabelCollection(m_cache, wsSet);
			ReloadWssToDisplayForPart();
			List<int> wssToDisplay = HvosFromWss(WritingSystemsSelectedForDisplay);
			using (ReallySimpleListChooser chooser = new ReallySimpleListChooser(null, labels, "DataTreeWritingSystems", m_cache, wssToDisplay.ToArray(), false))
			{
				chooser.ForbidNoItemChecked = true;
				IVwStylesheet stylesheet = (Control as LabeledMultiStringView).StyleSheet;
				chooser.SetFontForDialog(new int[] { Cache.DefaultVernWs, Cache.DefaultAnalWs }, stylesheet, Cache.LanguageWritingSystemFactoryAccessor);
				chooser.InitializeExtras(ConfigurationNode, Mediator);
				chooser.Text = String.Format(DetailControlsStrings.ksSliceConfigureWssDlgTitle, this.Label);
				chooser.InstructionalText = DetailControlsStrings.ksSliceConfigureWssDlgInstructionalText;
				if (chooser.ShowDialog() == DialogResult.OK)
				{
					PersistAndRedisplayWssToDisplayForPart(chooser.ChosenHvos);
				}
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
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayWritingSystemOptionsForSlice(object parameter, ref UIListDisplayProperties display)
		{
			CheckDisposed();
			display.List.Clear();
			Mediator.PropertyTable.SetProperty(display.PropertyName, this.GetVisibleWSSPropertyValue(), false);
			AddWritingSystemListWithIcuLocales(display, this.WritingSystemOptionsForDisplay);
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// stores the list values in terms of icu locale
		/// </summary>
		/// <param name="display"></param>
		/// <param name="list"></param>
		private void AddWritingSystemListWithIcuLocales(UIListDisplayProperties display,
			ILgWritingSystem[] list)
		{
			string[] active = GetVisibleWSSPropertyValue().Split(',');
			foreach (ILgWritingSystem ws in list)
			{
				// generally enable all items, but if only one is checked that one is disabled;
				// it can't be turned off.
				bool enabled = (active.Length != 1 || ws.ICULocale != active[0]);
				display.List.Add(ws.ShortName, ws.ICULocale, null, null, enabled);
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
					string singlePropertySequenceValue = Mediator.PropertyTable.GetStringProperty("SelectedWritingSystemHvosForCurrentContextMenu", null);
					PersistAndRedisplayWssToDisplayForPart(singlePropertySequenceValue);
					break;
				default:
					break;
			}
		}

		private void PersistAndRedisplayWssToDisplayForPart(List<int> hvosWssToDisplay)
		{
			PersistAndRedisplayWssToDisplayForPart(EncodeWssToDisplayPropertyValue(hvosWssToDisplay, Cache));
		}

		private void PersistAndRedisplayWssToDisplayForPart(string singlePropertySequenceValue)
		{
			ReplacePartWithNewAttribute("visibleWritingSystems", singlePropertySequenceValue);
			ILgWritingSystem[] wssToDisplay = GetVisibleWritingSystems(singlePropertySequenceValue);
			if (Key.Length > 0)
			{
				XmlNode lastKey = Key[Key.Length - 1] as XmlNode;
				// This is a horrible kludge to implement LT-9620 and catch the fact that we are changing the list
				// of current pronunciation writing systems, and update the database.
				if (lastKey != null && XmlUtils.GetOptionalAttributeValue(lastKey, "menu") == "mnuDataTree-Pronunciation")
				{
					int[] wss = new int[wssToDisplay.Length];
					for (int i = 0; i < wss.Length; i++)
						wss[i] = wssToDisplay[i].Hvo;
					m_cache.LangProject.UpdatePronunciationWritingSystems(wss);
				}
			}
			SetWssToDisplayForPart(wssToDisplay);
		}

		/// <summary>
		/// go through all the data tree slices, finding the slices that refer to the same part as this slice
		/// setting them to the same writing systems to display
		/// and redisplaying their views.
		/// </summary>
		/// <param name="wssToDisplay"></param>
		private void SetWssToDisplayForPart(ILgWritingSystem[] wssToDisplay)
		{
			XmlNode ourPart = this.PartRef();
			foreach (Control c in ContainingDataTree.Controls)
			{
				Slice slice = c as Slice;
				XmlNode part = slice.PartRef();
				if (part == ourPart)
				{
					(slice.Control as LabeledMultiStringView).WritingSystemsToDisplay = wssToDisplay;
					(slice.Control as LabeledMultiStringView).RefreshDisplay();
				}
			}
		}
	}
}
