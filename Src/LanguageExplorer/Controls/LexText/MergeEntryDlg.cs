// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.LexText
{
#if RANDYTODO
	// TODO: Move this class into Lexicon Area in Lang Exp.
#endif
	public class MergeEntryDlg : EntryGoDlg
	{
		#region Data members

		private PictureBox m_pictureBox;
		private XElement m_parametersElement;

		#endregion Data members

		#region Properties

		protected override WindowParams DefaultWindowParams
		{
			get
			{
				return new WindowParams
				{
					m_title = LexTextControls.ksMergeEntry,
					m_btnText = LexTextControls.ks_Merge
				};
			}
		}

		protected override string PersistenceLabel
		{
			get { return "MergeEntry"; }
		}

		#endregion Properties

		#region	Construction and Destruction

		public MergeEntryDlg()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
			ShowControlsBasedOnPanel1Position();	// used for sizing and display of some controls

			Icon infoIcon = SystemIcons.Information;
			m_pictureBox.Image = infoIcon.ToBitmap();
			m_pictureBox.Size = infoIcon.Size;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		public void SetDlgInfo(LcmCache cache, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, XElement parametersElement, ILexEntry startingEntry, string title, string formlabel, string okbuttonlabel)
		{
			CheckDisposed();

			Debug.Assert(startingEntry != null);
			m_startingEntry = startingEntry;
			m_parametersElement = parametersElement;

			SetDlgInfo(cache, null, propertyTable, publisher, subscriber);

			Text = title;
			m_formLabel.Text = formlabel;
			m_btnOK.Text = okbuttonlabel;

			// Relocate remaining three buttons.
			Point pt = m_btnHelp.Location;
			// Make the Help btn 20 off the right edge of the dlg
			pt.X = Width - m_btnHelp.Width - 20;
			m_btnHelp.Location = pt;
			// Make the Cancel btn 10 from the left of the Help btn
			pt.X -= (m_btnClose.Width + 10);
			m_btnClose.Location = pt;
			// Make the Merge Entry btn 10 from the left of the Cancel btn.
			pt.X -= (m_btnOK.Width + 10);
			m_btnOK.Location = pt;
			SetBottomMessage();

			SetHelpTopic("khtpMergeEntry");

			//LT-3017 Launch the dialog with the Lexeme that is currently selected.
			Form = m_startingEntry.HomographForm;
		}

		#endregion	Construction and Destruction

		#region	Other methods

		protected override void HandleMatchingSelectionChanged()
		{
			SetBottomMessage();
			base.HandleMatchingSelectionChanged();
		}

		protected override void SetBottomMessage()
		{
			int userWs = m_cache.WritingSystemFactory.UserWs;
			string sBase;
			if (m_selObject != null)
				sBase = LexTextControls.ksEntryXMergedIntoY;
			else
				sBase = LexTextControls.ksEntryXMergedIntoSel;
			ITsStrBldr tsb = TsStringUtils.MakeStrBldr();
			tsb.ReplaceTsString(0, tsb.Length, TsStringUtils.MakeString(sBase, userWs));
			// Replace every "{0}" with the headword we'll be merging, and make it bold.
			ITsString tssFrom = m_startingEntry.HeadWord;
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
			if (m_selObject != null)
			{
				// Replace every "{1}" with the headword we'll be merging into.
				ITsString tssTo = ((ILexEntry)m_selObject).HeadWord;
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
					ich = sTmp.IndexOf("{0}", StringComparison.Ordinal);
				}
				// Replace every "{2}" with a newline character.
				ich = sTmp.IndexOf("{2}", StringComparison.Ordinal);
				while (ich >= 0)
				{
					tsb.ReplaceTsString(ich, ich + 3, TsStringUtils.MakeString(StringUtils.kChHardLB.ToString(), userWs));
					sTmp = tsb.Text;
					ich = sTmp.IndexOf("{2}", StringComparison.Ordinal);
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
				}
			}
			m_fwTextBoxBottomMsg.Tss = tsb.GetString();
		}

		protected override void InitializeMatchingObjects(LcmCache cache)
		{
			var searchEngine = (MergeEntrySearchEngine)SearchEngine.Get(m_propertyTable, "MergeEntrySearchEngine", () => new MergeEntrySearchEngine(cache));
			searchEngine.CurrentEntryHvo = m_startingEntry.Hvo;

			m_matchingObjectsBrowser.Initialize(cache, FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable), m_propertyTable, m_publisher, m_subscriber, m_parametersElement,
				searchEngine);

			// start building index
			var selectedWs = (CoreWritingSystemDefinition) m_cbWritingSystems.SelectedItem;
			if(selectedWs != null)
				m_matchingObjectsBrowser.SearchAsync(GetFields(string.Empty, selectedWs.Handle));
		}

		/// <summary>
		/// A search engine that excludes the current entry (you can't merge an entry with its self
		/// </summary>
		private class MergeEntrySearchEngine : EntryGoSearchEngine
		{
			public int CurrentEntryHvo { private get; set; }

			public MergeEntrySearchEngine(LcmCache cache) : base(cache)
			{
			}

			protected override IEnumerable<int>  FilterResults(IEnumerable<int> results)
			{
				return results == null ? null : results.Where(hvo => hvo != CurrentEntryHvo);
			}
		}
		#endregion	Other methods

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MergeEntryDlg));
			this.m_pictureBox = new System.Windows.Forms.PictureBox();
			this.m_panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_pictureBox)).BeginInit();
			this.SuspendLayout();
			//
			// m_btnClose
			//
			resources.ApplyResources(this.m_btnClose, "m_btnClose");
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			//
			// m_btnInsert
			//
			resources.ApplyResources(this.m_btnInsert, "m_btnInsert");
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			//
			// m_matchingObjectsBrowser
			//
			resources.ApplyResources(this.m_matchingObjectsBrowser, "m_matchingObjectsBrowser");
			//
			// m_fwTextBoxBottomMsg
			//
			resources.ApplyResources(this.m_fwTextBoxBottomMsg, "m_fwTextBoxBottomMsg");
			//
			// m_pictureBox
			//
			resources.ApplyResources(this.m_pictureBox, "m_pictureBox");
			this.m_pictureBox.Name = "m_pictureBox";
			this.m_pictureBox.TabStop = false;
			//
			// MergeEntryDlg
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.m_pictureBox);
			this.m_helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "MergeEntryDlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.Controls.SetChildIndex(this.m_btnClose, 0);
			this.Controls.SetChildIndex(this.m_btnOK, 0);
			this.Controls.SetChildIndex(this.m_btnInsert, 0);
			this.Controls.SetChildIndex(this.m_btnHelp, 0);
			this.Controls.SetChildIndex(this.m_panel1, 0);
			this.Controls.SetChildIndex(this.m_matchingObjectsBrowser, 0);
			this.Controls.SetChildIndex(this.m_cbWritingSystems, 0);
			this.Controls.SetChildIndex(this.m_wsLabel, 0);
			this.Controls.SetChildIndex(this.m_fwTextBoxBottomMsg, 0);
			this.Controls.SetChildIndex(this.m_objectsLabel, 0);
			this.Controls.SetChildIndex(this.m_pictureBox, 0);
			this.m_panel1.ResumeLayout(false);
			this.m_panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_pictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
