// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Areas.Grammar.Tools.PosEdit
{
	/// <summary>
	/// InflAffixTemplateMenuHandler provides context menus to the Inflectional Affix Template control.
	/// When the user (or test code) issues commands, this class also invokes the corresponding methods on the
	/// Inflectional Affix Template control.
	/// </summary>
	internal class InflAffixTemplateMenuHandler : IFlexComponent, IDisposable
	{
		// These variables are used for the popup menus.
		private ComboListBox m_clb;
		private bool m_fConstructingMenu;
		private System.Collections.Generic.List<FwMenuItem> m_rgfmi;

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion

		#region IDisposable & Co. implementation

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~InflAffixTemplateMenuHandler()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_clb != null)
				{
					m_clb.Dispose();
					m_clb = null;
				}
				m_rgfmi = null;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			InflAffixTemplate = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// factory method which creates the correct subclass based on the XML parameters
		/// </summary>
		internal static InflAffixTemplateMenuHandler Create(InflAffixTemplateControl inflAffixTemplateCtrl, XElement configuration)
		{
			InflAffixTemplateMenuHandler h = null;
			var node = configuration?.Element("menuHandler");
			if (node != null)
			{
				h = (InflAffixTemplateMenuHandler)DynamicLoader.CreateObject(node);
			}

			if (h == null) //no class specified, so just returned a generic InflAffixTemplateControl
			{
				h = new InflAffixTemplateMenuHandler();
			}
			h.InflAffixTemplate = inflAffixTemplateCtrl;
			return h;
		}

		private InflAffixTemplateControl InflAffixTemplate { get; set; }

		protected InflAffixTemplateMenuHandler()
		{
		}

		public bool OnInflTemplateInsertSlot(object cmd)
		{
#if RANDYTODO
			// TODO: "Later" was present at the time of the switch to git, so this method didn't do much, besides claiming to handle the command.
#if Later
			Command command = (Command) cmd;
			string field = command.GetParameter("field");
			string className = command.GetParameter("className");

			HandleInsertCommand(field, className);
#endif
#endif
			return true;	//we handled this.
		}

#if RANDYTODO
		/// <summary>
		/// decide whether to display this Menu Item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInflTemplateInsertSlot(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = true;
			return true;//we handled this, no need to ask anyone else.
		}
#endif

		protected LcmCache Cache => InflAffixTemplate.Cache;

		/// <summary>
		/// Invoked by a DataTree (which is in turn invoked by the slice)
		/// when the user does something to bring up a context menu
		/// </summary>
		public void ShowSliceContextMenu(object sender, InflAffixTemplateEventArgs e)
		{
			var configuration = e.ConfigurationNode;
			var menuId = XmlUtils.GetOptionalAttributeValue(configuration, "menu");

			//an empty menu attribute means no menu
			if (menuId != null && menuId.Length == 0)
			{
				return;
			}

			//a missing menu attribute means "figure out a default"
			if (menuId == null)
			{
				menuId="mnuInflAffixTemplate-Error"; // this is our default
			}
			if (menuId == string.Empty)
			{
				return;	//explicitly stated that there should not be a menu
			}

			m_rgfmi = BuildMenu(menuId);
			LaunchFwContextMenu(new Point(Cursor.Position.X, Cursor.Position.Y));
		}

		private void LaunchFwContextMenu(Point ptLoc)
		{
			if (m_rgfmi == null || m_rgfmi.Count == 0)
			{
				return;
			}
			m_fConstructingMenu = true;
			if (m_clb == null)
			{
				m_clb = new ComboListBox();
				m_clb.SelectedIndexChanged += HandleFwMenuSelection;
				m_clb.SameItemSelected += HandleFwMenuSelection;
				// Since we may initialize with TsStrings, need to set WSF.
				m_clb.WritingSystemFactory = Cache.LanguageWritingSystemFactoryAccessor;
				m_clb.DropDownStyle = ComboBoxStyle.DropDownList; // Prevents direct editing.
				m_clb.StyleSheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
			}
			m_clb.Items.Clear();
			foreach (var menuItem in m_rgfmi)
			{
				if (menuItem.Enabled)
				{
					m_clb.Items.Add(menuItem.Label);
				}
			}
			AdjustListBoxSize();
			m_clb.AdjustSize(500, 400); // these are maximums!
			m_clb.SelectedIndex = 0;
			var boundsLauncher = new Rectangle(ptLoc, new Size(10,10));
			var boundsScreen = Screen.GetWorkingArea(InflAffixTemplate);
			m_fConstructingMenu = false;
			m_clb.Launch(boundsLauncher, boundsScreen);
		}

		private void AdjustListBoxSize()
		{
			using (Graphics g = InflAffixTemplate.CreateGraphics())
			{
				var nMaxWidth = 0;
				var nHeight = 0;
				var ie = m_clb.Items.GetEnumerator();
				while (ie.MoveNext())
				{
					string s = null;
					if (ie.Current is ITsString)
					{
						var tss = (ITsString)ie.Current;
						s = tss.Text;
					}
					else if (ie.Current is string)
					{
						s = (string)ie.Current;
					}
					if (s != null)
					{
						var szf = g.MeasureString(s, m_clb.Font);
						var nWidth = (int)szf.Width + 2;
						if (nMaxWidth < nWidth)
						{
							// 2 is not quite enough for height if you have homograph
							// subscripts.
							nMaxWidth = nWidth;
						}
						nHeight += (int)szf.Height + 3;
					}
				}
				m_clb.Form.Width = Math.Max(m_clb.Form.Width, nMaxWidth);
				m_clb.Form.Height = Math.Max(m_clb.Form.Height, nHeight);
			}
		}

		internal virtual void HandleFwMenuSelection(object sender, EventArgs ea)
		{
			if (m_fConstructingMenu)
			{
				return;
			}
			var iSel = m_clb.SelectedIndex;
			m_clb.HideForm();
			var fmi = FindEnabledItem(iSel);
			if (fmi == null)
			{
				return;
			}
#if RANDYTODO
			var command = new Command(m_mediator, fmi.ConfigurationNode);
			m_mediator.SendMessage(fmi.Message, command);
#endif
		}

		private FwMenuItem FindEnabledItem(int iSel)
		{
			if (iSel < 0 || iSel >= m_rgfmi.Count)
			{
				return null;
			}
			foreach (var menuItem in m_rgfmi)
			{
				if (!menuItem.Enabled)
				{
					continue;
				}

				if (iSel == 0)
				{
					return menuItem;
				}
				--iSel;
			}
			return null;
		}

		/// <summary>
		/// We may need to display vernacular data within the menu: if so, the standard menu display
		/// won't work, and we must do something else...  Actually the analysis and user writing systems
		/// (or fonts) may differ as well.
		/// </summary>
		private List<FwMenuItem> BuildMenu(string menuId)
		{
			var rgfmi = new List<FwMenuItem>();
			var xnWindow = PropertyTable.GetValue<XElement>("WindowConfiguration");
			Debug.Assert(xnWindow != null);
			var xnMenu = xnWindow.XPathSelectElement("contextMenus/menu[@id=\"" + menuId + "\"]");
			Debug.Assert(xnMenu != null && xnMenu.HasElements && xnMenu.Elements().Any());
			foreach (var xnItem in xnMenu.Elements())
			{
				var sCmd = XmlUtils.GetOptionalAttributeValue(xnItem, "command");
				Debug.Assert(!string.IsNullOrEmpty(sCmd));
				if (string.IsNullOrEmpty(sCmd))
				{
					continue;
				}
				var xn = xnWindow.XPathSelectElement("commands/command[@id=\"" + sCmd + "\"]");
				Debug.Assert(xn != null);
				if (xn == null)
				{
					continue;
				}
				var sMsg = XmlUtils.GetOptionalAttributeValue(xn, "message");
				var sLabel = XmlUtils.GetOptionalAttributeValue(xn, "label");
				Debug.Assert(!string.IsNullOrEmpty(sMsg) && !string.IsNullOrEmpty(sLabel));
				if (string.IsNullOrEmpty(sMsg) || string.IsNullOrEmpty(sLabel))
				{
					continue;
				}
				ITsString tssLabel;
				var fEnabled = true;
				switch (sMsg)
				{
					case "InflTemplateAddInflAffixMsa":
						tssLabel = InflAffixTemplate.MenuLabelForInflTemplateAddInflAffixMsa(sLabel);
						break;
					case "InflTemplateInsertSlotAfter":
					case "InflTemplateInsertSlotBefore":
						tssLabel = InflAffixTemplate.DetermineSlotContextMenuItemLabel(sLabel);
						break;
					case "InflTemplateMoveSlotLeft":
						tssLabel = InflAffixTemplate.MenuLabelForInflTemplateMoveSlot(sLabel, true, out fEnabled);
						break;
					case "InflTemplateMoveSlotRight":
						tssLabel = InflAffixTemplate.MenuLabelForInflTemplateMoveSlot(sLabel, false, out fEnabled);
						break;
					case "InflTemplateToggleSlotOptionality":
					case "InflTemplateRemoveSlot":
						tssLabel = InflAffixTemplate.MenuLabelForInflTemplateAffixSlotOperation(sLabel, out fEnabled);
						break;
					case "InflTemplateRemoveInflAffixMsa":
						tssLabel = InflAffixTemplate.MenuLabelForInflTemplateRemoveInflAffixMsa(sLabel);
						break;
					case "JumpToTool":
						tssLabel = InflAffixTemplate.MenuLabelForJumpToTool(sLabel);
						break;
					case "InflAffixTemplateHelp":
						tssLabel = InflAffixTemplate.MenuLabelForInflAffixTemplateHelp(sLabel);
						break;
					default:
						Debug.Assert(sMsg == "InflTemplateAddInflAffixMsa");
						tssLabel = null;
						break;
				}
				if (tssLabel != null)
					rgfmi.Add(new FwMenuItem(tssLabel, xn, fEnabled));
			}
			return rgfmi;
		}
	}
}