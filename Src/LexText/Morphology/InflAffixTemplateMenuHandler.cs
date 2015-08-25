// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: InflAffixTemplateMenuHandler.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// </remarks>
using System;
using System.Xml;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using SIL.CoreImpl;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// InflAffixTemplateMenuHandler provides context menus to the Inflectional Affix Template control.
	/// When the user (or test code) issues commands, this class also invokes the corresponding methods on the
	/// Inflectional Affix Template control.
	/// </summary>
	public class InflAffixTemplateMenuHandler : IFlexComponent, IFWDisposable
	{
		/// <summary>
		/// Inflectiona Affix Template Control.
		/// </summary>
		protected InflAffixTemplateControl m_inflAffixTemplateCtrl;

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

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;
		}

		#endregion

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

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
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

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
			m_inflAffixTemplateCtrl = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// factory method which creates the correct subclass based on the XML parameters
		/// </summary>
		/// <param name="inflAffixTemplateCtrl"></param>
		/// <param name="configuration"></param>
		/// <returns></returns>
		public static InflAffixTemplateMenuHandler Create(InflAffixTemplateControl inflAffixTemplateCtrl, XmlNode configuration)
		{
			InflAffixTemplateMenuHandler h = null;
			if(configuration != null)
			{
				XmlNode node = configuration.SelectSingleNode("menuHandler");
				if (node != null)
				{
					h = (InflAffixTemplateMenuHandler) SIL.Utils.DynamicLoader.CreateObject(node);
				}
			}
			if (h == null)			//no class specified, so just returned a generic InflAffixTemplateControl
				h = new InflAffixTemplateMenuHandler();
			h.InflAffixTemplate = inflAffixTemplateCtrl;
			return h;
		}

		public InflAffixTemplateControl InflAffixTemplate
		{
			set
			{
				CheckDisposed();

				m_inflAffixTemplateCtrl = value;
			}
		}

		protected InflAffixTemplateMenuHandler()
		{
		}

		public bool OnInflTemplateInsertSlot(object cmd)
		{
			CheckDisposed();

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
			CheckDisposed();

			display.Enabled = true;
			return true;//we handled this, no need to ask anyone else.
		}
#endif

		protected FdoCache Cache
		{
			get
			{
				return m_inflAffixTemplateCtrl.Cache;
			}
		}

		/// <summary>
		/// Invoked by a DataTree (which is in turn invoked by the slice)
		/// when the user does something to bring up a context menu
		/// </summary>
		public void ShowSliceContextMenu(object sender, SIL.FieldWorks.XWorks.MorphologyEditor.InflAffixTemplateEventArgs e)
		{
			CheckDisposed();

			XmlNode configuration =e.ConfigurationNode;
			string menuId = XmlUtils.GetOptionalAttributeValue(configuration, "menu");

			//an empty menu attribute means no menu
			if (menuId != null && menuId.Length== 0)
				return;

			//a missing menu attribute means "figure out a default"
			if (menuId == null)
			{
				menuId="mnuInflAffixTemplate-Error"; // this is our default
			}
			if (menuId == "")
				return;	//explicitly stated that there should not be a menu

			m_rgfmi = BuildMenu(menuId);
			LaunchFwContextMenu(new Point(Cursor.Position.X, Cursor.Position.Y));
		}

		private void LaunchFwContextMenu(Point ptLoc)
		{
			if (m_rgfmi == null || m_rgfmi.Count == 0)
				return;
			m_fConstructingMenu = true;
			if (m_clb == null)
			{
				m_clb = new ComboListBox();
				m_clb.SelectedIndexChanged += new EventHandler(HandleFwMenuSelection);
				m_clb.SameItemSelected += new EventHandler(HandleFwMenuSelection);
				// Since we may initialize with TsStrings, need to set WSF.
				m_clb.WritingSystemFactory = Cache.LanguageWritingSystemFactoryAccessor;
				m_clb.DropDownStyle = ComboBoxStyle.DropDownList; // Prevents direct editing.
				m_clb.StyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable);
			}
			m_clb.Items.Clear();
			for (int i = 0; i < m_rgfmi.Count; ++i)
			{
				if (m_rgfmi[i].Enabled)
					m_clb.Items.Add(m_rgfmi[i].Label);
			}
			AdjustListBoxSize();
			m_clb.AdjustSize(500, 400); // these are maximums!
			m_clb.SelectedIndex = 0;
			Rectangle boundsLauncher = new Rectangle(ptLoc, new Size(10,10));
			Rectangle boundsScreen = Screen.GetWorkingArea(m_inflAffixTemplateCtrl);
			m_fConstructingMenu = false;
			m_clb.Launch(boundsLauncher, boundsScreen);
		}

		private void AdjustListBoxSize()
		{
			using (System.Drawing.Graphics g = m_inflAffixTemplateCtrl.CreateGraphics())
			{
				int nMaxWidth = 0;
				int nHeight = 0;
				System.Collections.IEnumerator ie = m_clb.Items.GetEnumerator();
				while (ie.MoveNext())
				{
					string s = null;
					if (ie.Current is ITsString)
					{
						ITsString tss = ie.Current as ITsString;
						s = tss.Text;
					}
					else if (ie.Current is String)
					{
						s = ie.Current as string;
					}
					if (s != null)
					{
						SizeF szf = g.MeasureString(s, m_clb.Font);
						int nWidth = (int)szf.Width + 2;
						if (nMaxWidth < nWidth)
							// 2 is not quite enough for height if you have homograph
							// subscripts.
							nMaxWidth = nWidth;
						nHeight += (int)szf.Height + 3;
					}
				}
				m_clb.Form.Width = Math.Max(m_clb.Form.Width, nMaxWidth);
				m_clb.Form.Height = Math.Max(m_clb.Form.Height, nHeight);
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification="Dispose not used elsewhere for creating XCore.Command objects")]
		internal virtual void HandleFwMenuSelection(object sender, EventArgs ea)
		{
			CheckDisposed();
			if (m_fConstructingMenu)
				return;
			int iSel = m_clb.SelectedIndex;
			m_clb.HideForm();
			FwMenuItem fmi = FindEnabledItem(iSel);
			if (fmi == null)
				return;
#if RANDYTODO
			var command = new Command(m_mediator, fmi.ConfigurationNode);
			m_mediator.SendMessage(fmi.Message, command);
#endif
		}

		private FwMenuItem FindEnabledItem(int iSel)
		{
			if (iSel < 0 || iSel >= m_rgfmi.Count)
				return null;
			for (int i = 0; i < m_rgfmi.Count; ++i)
			{
				if (m_rgfmi[i].Enabled)
				{
					if (iSel == 0)
						return m_rgfmi[i];
					else
						--iSel;
				}
			}
			return null;
		}

		/// <summary>
		/// We may need to display vernacular data within the menu: if so, the standard menu display
		/// won't work, and we must do something else...  Actually the analysis and user writing systems
		/// (or fonts) may differ as well.
		/// </summary>
		/// <param name="menuId"></param>
		private System.Collections.Generic.List<FwMenuItem> BuildMenu(string menuId)
		{
			System.Collections.Generic.List<FwMenuItem> rgfmi = new System.Collections.Generic.List<FwMenuItem>();
			XmlNode xnWindow = PropertyTable.GetValue<XmlNode>("WindowConfiguration");
			Debug.Assert(xnWindow != null);
			XmlNode xnMenu = xnWindow.SelectSingleNode("contextMenus/menu[@id=\"" + menuId + "\"]");
			Debug.Assert(xnMenu != null && xnMenu.ChildNodes != null && xnMenu.ChildNodes.Count > 0);
			foreach (XmlNode xnItem in xnMenu.ChildNodes)
			{
				string sCmd = XmlUtils.GetAttributeValue(xnItem, "command");
				Debug.Assert(!String.IsNullOrEmpty(sCmd));
				if (String.IsNullOrEmpty(sCmd))
					continue;
				XmlNode xn = xnWindow.SelectSingleNode("commands/command[@id=\"" + sCmd + "\"]");
				Debug.Assert(xn != null);
				if (xn == null)
					continue;
				string sMsg = XmlUtils.GetAttributeValue(xn, "message");
				string sLabel = XmlUtils.GetAttributeValue(xn, "label");
				Debug.Assert(!String.IsNullOrEmpty(sMsg) && !String.IsNullOrEmpty(sLabel));
				if (String.IsNullOrEmpty(sMsg) || String.IsNullOrEmpty(sLabel))
					continue;
				ITsString tssLabel;
				bool fEnabled = true;
				switch (sMsg)
				{
					case "InflTemplateAddInflAffixMsa":
						tssLabel = m_inflAffixTemplateCtrl.MenuLabelForInflTemplateAddInflAffixMsa(sLabel);
						break;
					case "InflTemplateInsertSlotAfter":
					case "InflTemplateInsertSlotBefore":
						tssLabel = m_inflAffixTemplateCtrl.DetermineSlotContextMenuItemLabel(sLabel);
						break;
					case "InflTemplateMoveSlotLeft":
						tssLabel = m_inflAffixTemplateCtrl.MenuLabelForInflTemplateMoveSlot(sLabel, true, out fEnabled);
						break;
					case "InflTemplateMoveSlotRight":
						tssLabel = m_inflAffixTemplateCtrl.MenuLabelForInflTemplateMoveSlot(sLabel, false, out fEnabled);
						break;
					case "InflTemplateToggleSlotOptionality":
					case "InflTemplateRemoveSlot":
						tssLabel = m_inflAffixTemplateCtrl.MenuLabelForInflTemplateAffixSlotOperation(sLabel, out fEnabled);
						break;
					case "InflTemplateRemoveInflAffixMsa":
						tssLabel = m_inflAffixTemplateCtrl.MenuLabelForInflTemplateRemoveInflAffixMsa(sLabel);
						break;
					case "JumpToTool":
						tssLabel = m_inflAffixTemplateCtrl.MenuLabelForJumpToTool(sLabel);
						break;
					case "InflAffixTemplateHelp":
						tssLabel = m_inflAffixTemplateCtrl.MenuLabelForInflAffixTemplateHelp(sLabel);
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

	/// <summary>
	/// This class stores the information needed for one menu item in a menu that must be displayed
	/// using views code (in order to handle multiple writing systems/fonts within each menu item).
	/// </summary>
	class FwMenuItem
	{
		ITsString m_tssItem;
		XmlNode m_xnConfig;
		bool m_fEnabled;

		public FwMenuItem(ITsString tssItem, XmlNode xnConfig, bool fEnabled)
		{
			m_tssItem = tssItem;
			m_xnConfig = xnConfig;
			m_fEnabled = fEnabled;
		}

		public ITsString Label
		{
			get { return m_tssItem; }
		}

		public string Message
		{
			get { return XmlUtils.GetAttributeValue(m_xnConfig, "message"); }
		}

		public bool Enabled
		{
			get { return m_fEnabled; }
		}

		public XmlNode ConfigurationNode
		{
			get { return m_xnConfig; }
		}
	}
}
