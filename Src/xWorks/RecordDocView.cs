// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RecordDocView.cs
// Responsibility: John Thomson
// Last reviewed:
//
// <remarks>
// </remarks>
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.Xml;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// RecordDocView implements a RecordView (view showing one object at a time from a sequence)
	/// in which the single object is displayed using a FieldWorks view. This is an abstract class.
	/// The actual view is the responsibility of the subclass. Subclasses must:
	///
	/// 1. Implement a subclass of RootSite, including a MakeRoot method and suitable
	/// view constructor as needed. It must implement IChangeRootObject.
	/// 2. Override ConstructRoot, which should return a new instance of the SimpleRootSite subclass.
	///		(This class will take care of docking the root site and making it visible and setting
	///		its FdoCache, which will result in its MakeRoot being called.)
	/// </summary>
	public class RecordDocView : RecordView
	{
		#region Data members

		/// <summary>
		/// The RootSite that displays the current object.
		/// </summary>
		protected RootSite m_rootSite;

		#endregion // Data members

		#region Construction and Removal

		public RecordDocView(XElement configurationParametersElement, RecordClerk recordClerk)
			: base(configurationParametersElement, recordClerk)
		{
		}

		#region Overrides of XWorksViewBase

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			InitBase();

			m_fullyInitialized = true;
		}

		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_rootSite != null)
					m_rootSite.Dispose();
			}
			m_rootSite = null;
			// m_mediator = null; // Bad idea, since superclass still needs it.

			base.Dispose(disposing);
		}

		#endregion // Construction and Removal

		#region Message Handlers

		public bool OnConsideringClosing(object argument, System.ComponentModel.CancelEventArgs args)
		{
			CheckDisposed();

			args.Cancel = !PrepareToGoAway();
			return args.Cancel; // if we want to cancel, others don't need to be asked.
		}

		#endregion // Message Handlers

		#region Other methods

		protected override void OnHandleCreated(EventArgs e)
		{
			// Must do this BEFORE base.OnHandleCreated, which will otherwise create the root box
			// with no stylesheet.
			if (m_rootSite != null && m_rootSite.StyleSheet == null)
			{
				SetupStylesheet();
			}
			base.OnHandleCreated(e);
		}

		protected override void SetupStylesheet()
		{
			// If possible make it use the style sheet appropriate for its main window.
			m_rootSite.StyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable);
		}
		protected virtual RootSite ConstructRoot()
		{
			Debug.Assert(false); // subclass must implement.
			return null;
		}

		protected override void ShowRecord()
		{
#if RANDYTODO
			Debug.Assert(Clerk.CurrentObject != null);
			Debug.Assert(m_rootSite != null);
#else
			// TODO: I run release builds, so the above assert doesn't do anything.
			// TODO: Remove this approach, when I'm satisfied all callers are well-behaved.
			if (Clerk.CurrentObject == null)
				throw new InvalidOperationException("'ShowRecord' called too early.");
			if (m_rootSite == null)
				throw new InvalidOperationException("'ShowRecord' called too early.");
#endif

			//todo: add the document view name to the task label
			//todo: fast machine, this doesn't really seem to do any good. I think maybe the parts that
			//are taking a long time are not getting Breath().
			//todo: test on a machine that is slow enough to see if this is helpful or not!
			using (var progress = ProgressState.CreatePredictiveProgressState(PropertyTable, ((RecordList)Clerk.SortItemProvider).PropertyName))
			{

				progress.Breath();

				base.ShowRecord();

				Clerk.SaveOnChangeRecord();

				progress.Breath();

				try
				{
					progress.SetMilestone();

					if (!m_rootSite.Visible)
					{
						m_rootSite.Visible = true;
					}
					BringToFront();
					m_rootSite.BringToFront();
					using (new WaitCursor(this))
					{
						IChangeRootObject root = m_rootSite as IChangeRootObject;
						if (root != null && !Clerk.SuspendLoadingRecordUntilOnJumpToRecord)
							root.SetRoot(Clerk.CurrentObject.Hvo);
					}
				}
				catch (Exception error)
				{
					using (var appSettingsKey = PropertyTable.GetValue<IFlexApp>("App").SettingsKey)
					{
						//don't really need to make the program stop just because we could not show this record.
						ErrorReporter.ReportException(error, appSettingsKey,
							PropertyTable.GetValue<IFeedbackInfoProvider>("FeedbackInfoProvider").SupportEmailAddress, null, false);
					}
				}
			}
		}

		protected override void SetupDataContext()
		{
			Debug.Assert(m_configurationParametersElement != null);

			base.SetupDataContext();
			m_rootSite = ConstructRoot();
			m_rootSite.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber)); // Init it as Flex component.
			//m_rootSite.PersistenceProvder = new XCore.PersistenceProvider(m_mediator.PropertyTable);

			m_rootSite.Dock = System.Windows.Forms.DockStyle.Fill;
			m_rootSite.Cache = Cache;

			Controls.Add(m_rootSite);
			m_rootSite.BringToFront(); // Review JohnT: is this needed?
		}

		/// <summary>
		/// if the XML configuration does not specify the availability of the treebar
		/// (e.g. treeBarAvailibility="Required"), then use this.
		/// </summary>
		protected override TreebarAvailability DefaultTreeBarAvailability
		{
			get
			{
				return TreebarAvailability.NotAllowed;
			}
		}

		#endregion // Other methods
	}

	/// <summary>
	/// This is a class that can be used as the rootsite of a RecordDocView, to make it a
	/// RecordDocXmlView.
	/// </summary>
	public class XmlDocItemView : XmlView, IChangeRootObject
	{
		private string m_configObjectName;

		#region implemention of IChangeRootObject

		public void SetRoot(int hvo)
		{
			CheckDisposed();
			if (m_hvoRoot == hvo)
				return; // OnRecordNavigation is often called repeatedly wit the same HVO, we don't need to recompute every time.

			m_hvoRoot = hvo;
			if (RootBox != null)
				RootBox.SetRootObject(m_hvoRoot, m_xmlVc, 1, m_styleSheet);
			// If the root box doesn't exist yet, the right root will be used in MakeRoot.
		}
		#endregion

		public XmlDocItemView(int hvoRoot, XElement xnSpec, string sLayout) :
			base(hvoRoot, sLayout, XmlUtils.GetOptionalBooleanAttributeValue(xnSpec, "editable", true))
		{
			if (m_xnSpec == null)
				m_xnSpec = xnSpec;
		}

		public override void MakeRoot()
		{
			base.MakeRoot();
			m_xmlVc.IdentifySource = true; // We need this to know our context for the context menu!
		}

		// Context menu exists just for one invocation (until idle).
		private ContextMenuStrip m_contextMenu;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Provides a context menu so we can configure parts of the dictionary preview.
		/// </summary>
		/// <param name="sel"></param>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		protected override bool DoContextMenu(IVwSelection sel, Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			int hvo, tag, ihvo, cpropPrevious;
			IVwPropertyStore propStore;
			sel.PropInfo(false, 0, out hvo, out tag, out ihvo, out cpropPrevious, out propStore);
			string nodePath = null;
			if (propStore != null)
			{
				nodePath = propStore.get_StringProperty((int) FwTextPropType.ktptBulNumTxtBef);
			}
			if (string.IsNullOrEmpty(nodePath))
			{
				if (sel.SelType == VwSelType.kstPicture)
					return true;
				// may be a literal string, where we can get it from the string itself.
				ITsString tss;
				int ich, ws;
				bool fAssocPrev;
				sel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out tag, out ws);
				nodePath = tss.get_Properties(0).GetStrPropValue((int) FwTextPropType.ktptBulNumTxtBef);
			}
			if (m_configObjectName == null)
				m_configObjectName = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(m_xnSpec, "configureObjectName", null));
			string label;
			if (string.IsNullOrEmpty(nodePath))
				label = String.Format(xWorksStrings.ksConfigure, m_configObjectName);
			else
				label = String.Format(xWorksStrings.ksConfigureIn, nodePath.Split(':')[3], m_configObjectName);
			m_contextMenu = new ContextMenuStrip();
			var item = new ToolStripMenuItem(label);
			m_contextMenu.Items.Add(item);
			item.Click += RunConfigureDialogAt;
			item.Tag = nodePath;
			m_contextMenu.Show(this, pt);
			m_contextMenu.Closed += m_contextMenu_Closed;
			return true;
		}

		void RunConfigureDialogAt(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			var nodePath = (string)item.Tag;
			RunConfigureDialog(nodePath ?? "");
		}

		private void RunConfigureDialog(string nodePath)
		{
			using (var dlg = new XmlDocConfigureDlg())
			{
				// If this is optional and defaults to DictionaryPublicationLayout,
				// it messes up our Dictionary when we make something else configurable (like Classified Dictionary).
				var sProp = XmlUtils.GetOptionalAttributeValue(m_xnSpec, "layoutProperty");
				Debug.Assert(sProp != null, "When making a view configurable you need to put a 'layoutProperty' in the XML configuration.");
				dlg.SetConfigDlgInfo(m_xnSpec, Cache, (FwStyleSheet)StyleSheet,
					FindForm() as IFwMainWnd, PropertyTable, Publisher, sProp);
				if (nodePath != null)
					dlg.SetActiveNode(nodePath);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					// Configuration may well have changed. Reset XML tables and redraw.
					var sNewLayout = PropertyTable.GetValue<string>(sProp);
					ResetTables(sNewLayout);
				}
				if (dlg.MasterRefreshRequired)
				{
					Publisher.Publish("MasterRefresh", null);
				}
			}
		}

		void m_contextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			Application.Idle += DisposeContextMenu;
		}

		void DisposeContextMenu(object sender, EventArgs e)
		{
			Application.Idle -= DisposeContextMenu;
			if (m_contextMenu != null)
			{
				m_contextMenu.Dispose();
				m_contextMenu = null;
			}
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			// Must do this BEFORE base.OnHandleCreated, which will otherwise create the root box
			// with no stylesheet.
			if (StyleSheet == null)
			{
				SetupStylesheet();
			}
			base.OnHandleCreated (e);
		}

		private void SetupStylesheet()
		{
			StyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable);
		}
	}

	/// <summary>
	/// This is a RecordDocView in which the view of each object is specified by a jtview XML element that is the
	/// first child of the parameters node.
	/// </summary>
	public class RecordDocXmlView : RecordDocView
	{
		XElement m_jtSpecs; // node required by XmlView.
		protected string m_configObjectName; // name to display in Configure dialog.

		public RecordDocXmlView(XElement configurationParametersElement, RecordClerk recordClerk)
			: base(configurationParametersElement, recordClerk)
		{
		}

		#region IDisposable override

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
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}
			m_jtSpecs = null;

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		protected override RootSite ConstructRoot()
		{
			string sLayout = GetLayoutName(m_jtSpecs, PropertyTable);
			return new XmlDocItemView(0, m_jtSpecs, sLayout);
		}

		protected override TreebarAvailability DefaultTreeBarAvailability
		{
			get { return TreebarAvailability.NotMyBusiness; }
		}

		/// <summary>
		/// This routine encapsulates the process for looking at the spec node of a tool (specifically the
		/// parameters node controlling the tool for a RecordDocView) and determining the layout that should
		/// be used as the root of the view.
		/// Three attributes contribute to this. First, if "layoutProperty" is present, it gives the name of
		/// a property to be looked up in the mediator to get the desired layout name.
		/// If nothing is found in the mediator, we fall back to using the "layout" attribute, which is
		/// mandatory, to determine the view.
		/// Then, if layoutSuffix is specified, it will be appended to whatever we got from the process above,
		/// typically appended to the end, but if there is a # suffix already, it goes before that.
		/// For example: The main dictionary view and the dictionary preview both use views like "publishRoot"
		/// and "publishStem", and optionally user-defined views like publishRoot#root-612, and both use the
		/// mediator property DictionaryPublicationLayout to record which view the user has selected. However,
		/// the preview specifies layoutSuffix "Preview" to indicate that the layout it actually uses
		/// is (e.g.) publishStemPreview. (This view wraps publishStem with some conditional logic to ensure
		/// that we display things like "Not published" if the entry is excluded from all publications.)
		/// </summary>
		public static string GetLayoutName(XElement xnSpec, IPropertyTable propertyTable)
		{
			string sLayout = null;
			string sProp = XmlUtils.GetOptionalAttributeValue(xnSpec, "layoutProperty", null);
			if (!String.IsNullOrEmpty(sProp))
				sLayout = propertyTable.GetValue<string>(sProp);
			if (String.IsNullOrEmpty(sLayout))
				sLayout = XmlUtils.GetManditoryAttributeValue(xnSpec, "layout");
			var parts = sLayout.Split('#');
			parts[0] += XmlUtils.GetOptionalAttributeValue(xnSpec, "layoutSuffix", "");
			return string.Join("#", parts);
		}
		protected override void SetupDataContext()
		{
			// The base class uses these specs, so locate them first!
			m_jtSpecs = m_configurationParametersElement;
			base.SetupDataContext();
		}

		protected override void ReadParameters()
		{
			m_configObjectName = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "configureObjectName", null));
			base.ReadParameters();
		}

		internal void ReallyShowRecordNow()
		{
			ShowRecord();
		}

#if RANDYTODO
		/// <summary>
		/// The configure dialog may be launched any time this tool is active.
		/// Its name is derived from the name of the tool.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayConfigureXmlDocView(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (string.IsNullOrEmpty(m_configObjectName))
			{
				display.Enabled = display.Visible = false;
				return true;
			}
			display.Enabled = true;
			display.Visible = true;
			// Enhance JohnT: make this configurable. We'd like to use the 'label' attribute of the 'tool'
			// element, but we don't have it, only the two-level-down 'parameters' element
			// so use "configureObjectName" parameter for now.
			// REVIEW: SHOULD THE "..." BE LOCALIZABLE (BY MAKING IT PART OF THE SOURCE FOR display.Text)?
			display.Text = String.Format(display.Text, m_configObjectName + "...");
			return true; //we've handled this
		}
#endif

		/// <summary>
		/// Launch the configure dialog.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnConfigureXmlDocView(object commandObject)
		{
			CheckDisposed();
			string sProp = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "layoutProperty");
			if(String.IsNullOrEmpty(sProp))
				sProp = "DictionaryPublicationLayout";
			using(var dlg = new XmlDocConfigureDlg())
			{
				dlg.SetConfigDlgInfo(m_configurationParametersElement, Cache, FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable),
					FindForm() as IFwMainWnd, PropertyTable, Publisher, sProp);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					// LT-8767 When this dialog is launched from the Configure Dictionary View dialog
					// m_mediator != null && m_rootSite == null so we need to handle this to prevent a crash.
					if (PropertyTable != null && m_rootSite != null)
					{
						(m_rootSite as XmlDocItemView).ResetTables(GetLayoutName(m_configurationParametersElement, PropertyTable));
					}
				}
				if (dlg.MasterRefreshRequired)
				{
					Publisher.Publish("MasterRefresh", null);
				}
				return true; // we handled it
			}
		}

	}
}
