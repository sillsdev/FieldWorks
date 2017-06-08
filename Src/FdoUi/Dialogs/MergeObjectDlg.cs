// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.CoreImpl.Text;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;
using SIL.Windows.Forms;

namespace SIL.FieldWorks.FdoUi.Dialogs
{
	/// <summary>
	/// Summary description for MergeObjectDlg.
	/// </summary>
	public class MergeObjectDlg : Form, IFlexComponent
	{
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_fwTextBoxBottomMsg;
		private FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private DummyCmObject m_mainObj = null;
		private DummyCmObject m_obj;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Panel m_bvPanel;
		private BrowseViewer m_bvMergeOptions = null;
		private System.Windows.Forms.ColumnHeader m_chItems;
		private System.Windows.Forms.Button buttonHelp;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private string m_helpTopic;
		private HelpProvider helpProvider;
		private Dictionary<int, DummyCmObject> m_candidates;

		public int Hvo
		{
			get
			{
				CheckDisposed();
				return m_obj.Hvo;
			}
		}

		private MergeObjectDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = "MergeObjectDlg";

			Icon infoIcon = System.Drawing.SystemIcons.Information;
			pictureBox1.Image = infoIcon.ToBitmap();
			pictureBox1.Size = infoIcon.Size;

			m_candidates = new Dictionary<int, DummyCmObject>();

			// Ensure form expands to fit all the controls
			AutoSize = true;
		}

		public MergeObjectDlg(IHelpTopicProvider helpTopicProvider) : this()
		{
			m_helpTopicProvider = helpTopicProvider;
		}

		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		/// <param name="cache">FDO cache.</param>
		/// <param name="wp">Strings used for various items in this dialog.</param>
		/// <param name="mainObj"></param>
		/// <param name="mergeCandidates"></param>
		/// <param name="guiControl"></param>
		/// <param name="helpTopic"></param>
		public void SetDlgInfo(FdoCache cache, WindowParams wp, DummyCmObject mainObj, List<DummyCmObject> mergeCandidates,
			string guiControl, string helpTopic)
		{
			CheckDisposed();

			Debug.Assert(cache != null);

			m_cache = cache;
			m_mainObj = mainObj;

			m_fwTextBoxBottomMsg.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_fwTextBoxBottomMsg.WritingSystemCode = m_cache.WritingSystemFactory.UserWs;

			InitBrowseView(guiControl, mergeCandidates);

			Text = wp.m_title;
			label2.Text = wp.m_label;

			m_helpTopic = helpTopic;

			if(m_helpTopic != null && m_helpTopicProvider != null) // m_helpTopicProvider could be null for testing
			{
				helpProvider = new HelpProvider();
				helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_helpTopic));
				helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}

			MoveWindowToPreviousPosition();
		}

		private void MoveWindowToPreviousPosition()
		{
			// Get location to the stored values, if any.
			object locWnd = PropertyTable.GetValue<object>("mergeDlgLocation");
			// JohnT: this dialog can't be resized. So it doesn't make sense to
			// remember a size. If we do, we need to override OnLoad (as in SimpleListChooser)
			// to prevent the dialog growing every time at 120 dpi. But such an override
			// makes it too small to show all the controls at the default size.
			// It's better just to use the default size until it's resizeable for some reason.
			//PropertyTable.GetValue("msaCreatorDlgSize");
			object szWnd = Size;
			if (locWnd != null)
			{
				Rectangle rect = new Rectangle((Point)locWnd, (Size)szWnd);
				ScreenHelper.EnsureVisibleRect(ref rect);
				DesktopBounds = rect;
				StartPosition = FormStartPosition.Manual;
			}
		}

		private void InitBrowseView(string guiControl, List<DummyCmObject> mergeCandidates)
		{
			var configurationParameters = PropertyTable.GetValue<XElement>("WindowConfiguration");
			var toolNode = configurationParameters.XPathSelectElement("controls/parameters/guicontrol[@id='" + guiControl + "']/parameters");

			const int kMadeUpFieldIdentifier = 8999958;
			ObjectListPublisher sda = new ObjectListPublisher((ISilDataAccessManaged)m_cache.DomainDataByFlid , kMadeUpFieldIdentifier);

			int[] hvos = (from obj in mergeCandidates select obj.Hvo).ToArray();
			for (int i = 0; i < mergeCandidates.Count; i++)
				m_candidates[mergeCandidates[i].Hvo] = mergeCandidates[i];
			sda.SetOwningPropInfo(WfiWordformTags.kClassId, "LangProject", "Options");
			sda.SetOwningPropValue(hvos);
#if RANDYTODO
			// TODO: Call FinishInitialization on m_bvMergeOptions and feed it ObjectListPublisher.OwningFlid for the 'madeUpFieldIdentifier' parameter.
#endif
			m_bvMergeOptions = new BrowseViewer(toolNode, m_cache.LangProject.Hvo, m_cache, null, sda)
			{
				StyleSheet = Common.Widgets.FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable)
			};
			m_bvMergeOptions.SelectedIndexChanged += m_bvMergeOptions_SelectedIndexChanged;
			m_bvMergeOptions.Dock = DockStyle.Fill;
			m_bvPanel.Controls.Add(m_bvMergeOptions);
		}

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
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			m_cache = null;
			m_fwTextBoxBottomMsg = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			base.Dispose( disposing );
		}

		#region	Other methods

		private void SetBottomMessage()
		{
			int userWs = m_cache.ServiceLocator.WritingSystemManager.UserWs;
			string sBase;
			if (m_obj != null && m_obj.Hvo > 0)
				sBase = FdoUiStrings.ksMergeXIntoY;
			else
				sBase = FdoUiStrings.ksMergeXIntoSelection;
			ITsStrBldr tsb = TsStringUtils.MakeStrBldr();
			tsb.ReplaceTsString(0, tsb.Length, TsStringUtils.MakeString(sBase, userWs));
			// Replace every "{0}" with the headword we'll be merging, and make it bold.
			ITsString tssFrom = TsStringUtils.MakeString(m_mainObj.ToString(), m_mainObj.WS);
			string sTmp = tsb.Text;
			int ich = sTmp.IndexOf("{0}", StringComparison.Ordinal);
			int cch = tssFrom.Length;
			while (ich >= 0 && cch > 0)
			{
				tsb.ReplaceTsString(ich, ich + 3, tssFrom);
				tsb.SetIntPropValues(ich, ich + cch,
					(int)FwTextPropType.ktptBold,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwTextToggleVal.kttvForceOn);
				sTmp = tsb.Text;
				ich = sTmp.IndexOf("{0}", StringComparison.Ordinal);	// in case localization needs more than one.
			}
			int cLines = 1;
			if (m_obj != null && m_obj.Hvo > 0)
			{
				// Replace every "{1}" with the headword we'll be merging into.
				ITsString tssTo = TsStringUtils.MakeString(m_obj.ToString(), m_obj.WS);
				ich = sTmp.IndexOf("{1}", StringComparison.Ordinal);
				cch = tssTo.Length;
				while (ich >= 0 && cch > 0)
				{
					tsb.ReplaceTsString(ich, ich + 3, tssTo);
					tsb.SetIntPropValues(ich, ich + cch,
						(int)FwTextPropType.ktptBold,
						(int)FwTextPropVar.ktpvEnum,
						(int)FwTextToggleVal.kttvForceOn);
					sTmp = tsb.Text;
					ich = sTmp.IndexOf("{1}", StringComparison.Ordinal);
				}
				// Replace every "{2}" with a newline character.
				ich = sTmp.IndexOf("{2}", StringComparison.Ordinal);
				while (ich >= 0)
				{
					tsb.ReplaceTsString(ich, ich + 3, TsStringUtils.MakeString(StringUtils.kChHardLB.ToString(), userWs));
					sTmp = tsb.Text;
					ich = sTmp.IndexOf("{2}", StringComparison.Ordinal);
					++cLines;
				}
			}
			else
			{
				// Replace every "{1}" with a newline character.
				ich = sTmp.IndexOf("{1}", StringComparison.Ordinal);
				while (ich >= 0)
				{
					tsb.ReplaceTsString(ich, ich + 3, TsStringUtils.MakeString(StringUtils.kChHardLB.ToString(), userWs));
					sTmp = tsb.Text;
					ich = sTmp.IndexOf("{1}", StringComparison.Ordinal);
					++cLines;
				}
			}
			m_fwTextBoxBottomMsg.Tss = tsb.GetString();
			int oldHeight = m_fwTextBoxBottomMsg.Height;
			int newHeight = m_fwTextBoxBottomMsg.PreferredHeight;
			// Having newlines in the middle of the string messes up the height calculation.
			// See FWR-2308.  The adjustment may not be perfect, but is better than just showing
			// the text before the first newline.
			if (newHeight < 30)
				newHeight *= cLines;
			if (newHeight != m_fwTextBoxBottomMsg.Height)
			{
				int delta = newHeight - oldHeight;
				Size = MinimumSize;
				FontHeightAdjuster.GrowDialogAndAdjustControls(this, delta, m_fwTextBoxBottomMsg);
				m_fwTextBoxBottomMsg.Height = newHeight;
			}
		}

		#endregion Other methods

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MergeObjectDlg));
			this.label2 = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnClose = new System.Windows.Forms.Button();
			this.m_bvPanel = new System.Windows.Forms.Panel();
			this.m_chItems = new System.Windows.Forms.ColumnHeader();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.m_fwTextBoxBottomMsg = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.SuspendLayout();
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// pictureBox1
			//
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			//
			// btnOK
			//
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.Name = "btnOK";
			//
			// btnClose
			//
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnClose, "btnClose");
			this.btnClose.Name = "btnClose";
			//
			// m_bvPanel
			//
			resources.ApplyResources(this.m_bvPanel, "m_bvPanel");
			this.m_bvPanel.Name = "m_bvPanel";
			//
			// m_chItems
			//
			resources.ApplyResources(this.m_chItems, "m_chItems");
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// m_fwTextBoxBottomMsg
			//
			this.m_fwTextBoxBottomMsg.AdjustStringHeight = true;
			this.m_fwTextBoxBottomMsg.WordWrap = true;
			this.m_fwTextBoxBottomMsg.BackColor = System.Drawing.SystemColors.Control;
			resources.ApplyResources(this.m_fwTextBoxBottomMsg, "m_fwTextBoxBottomMsg");
			this.m_fwTextBoxBottomMsg.HasBorder = false;
			this.m_fwTextBoxBottomMsg.Name = "m_fwTextBoxBottomMsg";
			this.m_fwTextBoxBottomMsg.TabStop = false;
			//
			// MergeObjectDlg
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnClose;
			this.CausesValidation = false;
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.m_bvPanel);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.m_fwTextBoxBottomMsg);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MergeObjectDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Closed += new System.EventHandler(this.MergeObjectDlg_Closed);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void m_bvMergeOptions_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetSelectedObject();
		}

		private void SetSelectedObject()
		{
			int hvo = m_bvMergeOptions.AllItems[m_bvMergeOptions.SelectedIndex];
			m_obj = m_candidates[hvo];
			SetBottomMessage();
			btnOK.Enabled = true;
		}

		private void MergeObjectDlg_Closed(object sender, EventArgs e)
		{
			if (PropertyTable != null)
			{
				PropertyTable.SetProperty("mergeDlgLocation", Location, true, true);
				PropertyTable.SetProperty("mergeDlgSize", Size, true, true);
			}
		}

		private void buttonHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopic);
		}

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
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion
	}
}
