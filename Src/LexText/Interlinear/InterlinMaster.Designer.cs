using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.IText
{
	public partial class InterlinMaster
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				//SuspendLayout();	// don't want do trigger OnLayout() when removing controls!
				//DestroyTitleContentsPane();
				//if (m_tabCtrl != null)
				//    m_tabCtrl.SelectedIndexChanged -= new System.EventHandler(m_tabCtrl_SelectedIndexChanged);
				//DisposeInterlinDocPanes();
				//DisposeIfParentNull(m_panelInterlin);

				//DisposeIfParentNull(m_rtPane);
				//DisposeIfParentNull(m_infoPane);
				if (components != null)
				{
					components.Dispose();
				}
				// LT-5702
				// The Find / Replace dlg can currently only exist in this view, so
				// remove it when the view changes.  This will have to be expanded
				// when the dlg can search and operate on more than one view in Flex
				// as it does in TE.
				IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
				if (app != null)
				   app.RemoveFindReplaceDialog();
			}

			m_tcPane = null;
			m_rtPane = null;
			m_infoPane = null;
			m_idcGloss = null;
			m_idcAnalyze = null;
			m_taggingPane = null;
			m_printViewPane = null;
			m_constChartPane = null;
			m_panelAnalyzeView = null;
			m_panelGloss = null;
			m_panelTagging = null;
			m_panelPrintView = null;

			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InterlinMaster));
			this.m_tcPane = new SIL.FieldWorks.IText.TitleContentsPane();
			this.m_tabCtrl = new System.Windows.Forms.TabControl();
			this.m_tpInfo = new System.Windows.Forms.TabPage();
			this.m_infoPane = new SIL.FieldWorks.IText.InfoPane();
			this.m_tpRawText = new System.Windows.Forms.TabPage();
			this.m_rtPane = new SIL.FieldWorks.IText.RawTextPane();
			this.m_tpGloss = new System.Windows.Forms.TabPage();
			this.m_panelGloss = new System.Windows.Forms.Panel();
			this.m_idcGloss = new SIL.FieldWorks.IText.InterlinDocForAnalysis();
			this.m_tpInterlinear = new System.Windows.Forms.TabPage();
			this.m_panelAnalyzeView = new System.Windows.Forms.Panel();
			this.m_idcAnalyze = new SIL.FieldWorks.IText.InterlinDocForAnalysis();
			this.m_tpTagging = new System.Windows.Forms.TabPage();
			this.m_panelTagging = new System.Windows.Forms.Panel();
			this.m_taggingPane = new SIL.FieldWorks.IText.InterlinTaggingChild();
			this.m_tpPrintView = new System.Windows.Forms.TabPage();
			this.m_panelPrintView = new System.Windows.Forms.Panel();
			this.m_printViewPane = new SIL.FieldWorks.IText.InterlinPrintChild();
			this.m_tpCChart = new System.Windows.Forms.TabPage();
			this.m_tabCtrl.SuspendLayout();
			this.m_tpInfo.SuspendLayout();
			this.m_tpRawText.SuspendLayout();
			this.m_tpGloss.SuspendLayout();
			this.m_panelGloss.SuspendLayout();
			this.m_tpInterlinear.SuspendLayout();
			this.m_panelAnalyzeView.SuspendLayout();
			this.m_tpTagging.SuspendLayout();
			this.m_panelTagging.SuspendLayout();
			this.m_tpPrintView.SuspendLayout();
			this.m_panelPrintView.SuspendLayout();
			this.m_tpCChart.SuspendLayout();
			this.SuspendLayout();
			//
			// m_tcPane
			//
			resources.ApplyResources(this.m_tcPane, "m_tcPane");
			this.m_tcPane.BackColor = System.Drawing.SystemColors.ControlLight;
			this.m_tcPane.DoSpellCheck = false;
			this.m_tcPane.Group = null;
			this.m_tcPane.IsTextBox = false;
			this.m_tcPane.Mediator = null;
			this.m_tcPane.MinimumSize = new System.Drawing.Size(0, 0); // LT-12233
			this.m_tcPane.Name = "m_tcPane";
			this.m_tcPane.ReadOnlyView = false;
			this.m_tcPane.ScrollMinSize = new System.Drawing.Size(0, 0);
			this.m_tcPane.ScrollPosition = new System.Drawing.Point(0, 0);
			this.m_tcPane.ShowRangeSelAfterLostFocus = false;
			this.m_tcPane.SizeChangedSuppression = false;
			this.m_tcPane.WritingSystemFactory = null;
			this.m_tcPane.WsPending = -1;
			this.m_tcPane.Zoom = 1F;
			//
			// m_tabCtrl
			//
			resources.ApplyResources(this.m_tabCtrl, "m_tabCtrl");
			this.m_tabCtrl.Controls.Add(this.m_tpInfo);
			this.m_tabCtrl.Controls.Add(this.m_tpRawText);
			this.m_tabCtrl.Controls.Add(this.m_tpGloss);
			this.m_tabCtrl.Controls.Add(this.m_tpInterlinear);
			this.m_tabCtrl.Controls.Add(this.m_tpTagging);
			this.m_tabCtrl.Controls.Add(this.m_tpPrintView);
			this.m_tabCtrl.Controls.Add(this.m_tpCChart);
			this.m_tabCtrl.Name = "m_tabCtrl";
			this.m_tabCtrl.SelectedIndex = 0;
			this.m_tabCtrl.Selected += new System.Windows.Forms.TabControlEventHandler(this.m_tabCtrl_Selected);
			this.m_tabCtrl.GotFocus += new System.EventHandler(this.m_tabCtrl_GotFocus);
			this.m_tabCtrl.Deselecting += new System.Windows.Forms.TabControlCancelEventHandler(this.m_tabCtrl_Deselecting);
			//
			// m_tpInfo
			//
			resources.ApplyResources(this.m_tpInfo, "m_tpInfo");
			this.m_tpInfo.Controls.Add(this.m_infoPane);
			this.m_tpInfo.Name = "m_tpInfo";
			this.m_tpInfo.UseVisualStyleBackColor = true;
			//
			// m_infoPane
			//
			this.m_infoPane.Cache = null;
			resources.ApplyResources(this.m_infoPane, "m_infoPane");
			this.m_infoPane.Name = "m_infoPane";
			//
			// m_tpRawText
			//
			resources.ApplyResources(this.m_tpRawText, "m_tpRawText");
			this.m_tpRawText.Controls.Add(this.m_rtPane);
			this.m_tpRawText.Name = "m_tpRawText";
			this.m_tpRawText.UseVisualStyleBackColor = true;
			//
			// m_rtPane
			//
			resources.ApplyResources(this.m_rtPane, "m_rtPane");
			this.m_rtPane.BackColor = System.Drawing.SystemColors.Window;
			this.m_rtPane.DoSpellCheck = true;
			this.m_rtPane.Group = null;
			this.m_rtPane.IsTextBox = false;
			this.m_rtPane.Mediator = null;
			this.m_rtPane.Name = "m_rtPane";
			this.m_rtPane.ReadOnlyView = false;
			this.m_rtPane.ScrollMinSize = new System.Drawing.Size(0, 0);
			this.m_rtPane.ScrollPosition = new System.Drawing.Point(0, 0);
			this.m_rtPane.ShowRangeSelAfterLostFocus = false;
			this.m_rtPane.SizeChangedSuppression = false;
			this.m_rtPane.WritingSystemFactory = null;
			this.m_rtPane.WsPending = -1;
			this.m_rtPane.Zoom = 1F;
			//
			// m_tpGloss
			//
			resources.ApplyResources(this.m_tpGloss, "m_tpGloss");
			this.m_tpGloss.Controls.Add(this.m_panelGloss);
			this.m_tpGloss.Name = "m_tpGloss";
			this.m_tpGloss.UseVisualStyleBackColor = true;
			//
			// m_panelGloss
			//
			this.m_panelGloss.Controls.Add(this.m_idcGloss);
			resources.ApplyResources(this.m_panelGloss, "m_panelGloss");
			this.m_panelGloss.Name = "m_panelGloss";
			//
			// m_idcGloss
			//
			resources.ApplyResources(this.m_idcGloss, "m_idcGloss");
			this.m_idcGloss.BackColor = System.Drawing.SystemColors.Window;
			this.m_idcGloss.DoSpellCheck = true;
			this.m_idcGloss.ForEditing = true;
			this.m_idcGloss.Group = null;
			this.m_idcGloss.IsTextBox = false;
			this.m_idcGloss.Mediator = null;
			this.m_idcGloss.Name = "m_idcGloss";
			this.m_idcGloss.ReadOnlyView = false;
			this.m_idcGloss.ScrollMinSize = new System.Drawing.Size(0, 0);
			this.m_idcGloss.ScrollPosition = new System.Drawing.Point(0, 0);
			this.m_idcGloss.ShowRangeSelAfterLostFocus = false;
			this.m_idcGloss.SizeChangedSuppression = false;
			this.m_idcGloss.WritingSystemFactory = null;
			this.m_idcGloss.WsPending = -1;
			this.m_idcGloss.Zoom = 1F;
			//
			// m_tpInterlinear
			//
			resources.ApplyResources(this.m_tpInterlinear, "m_tpInterlinear");
			this.m_tpInterlinear.Controls.Add(this.m_panelAnalyzeView);
			this.m_tpInterlinear.Name = "m_tpInterlinear";
			this.m_tpInterlinear.UseVisualStyleBackColor = true;
			//
			// m_panelAnalyzeView
			//
			resources.ApplyResources(this.m_panelAnalyzeView, "m_panelAnalyzeView");
			this.m_panelAnalyzeView.Controls.Add(this.m_idcAnalyze);
			this.m_panelAnalyzeView.Name = "m_panelAnalyzeView";
			//
			// m_idcAnalyze
			//
			resources.ApplyResources(this.m_idcAnalyze, "m_idcAnalyze");
			this.m_idcAnalyze.BackColor = System.Drawing.SystemColors.Window;
			this.m_idcAnalyze.DoSpellCheck = true;
			this.m_idcAnalyze.ForEditing = true;
			this.m_idcAnalyze.Group = null;
			this.m_idcAnalyze.IsTextBox = false;
			this.m_idcAnalyze.Mediator = null;
			this.m_idcAnalyze.Name = "m_idcAnalyze";
			this.m_idcAnalyze.ReadOnlyView = false;
			this.m_idcAnalyze.ScrollMinSize = new System.Drawing.Size(0, 0);
			this.m_idcAnalyze.ScrollPosition = new System.Drawing.Point(0, 0);
			this.m_idcAnalyze.ShowRangeSelAfterLostFocus = false;
			this.m_idcAnalyze.SizeChangedSuppression = false;
			this.m_idcAnalyze.WritingSystemFactory = null;
			this.m_idcAnalyze.WsPending = -1;
			this.m_idcAnalyze.Zoom = 1F;
			//
			// m_tpTagging
			//
			resources.ApplyResources(this.m_tpTagging, "m_tpTagging");
			this.m_tpTagging.Controls.Add(this.m_panelTagging);
			this.m_tpTagging.Name = "m_tpTagging";
			this.m_tpTagging.UseVisualStyleBackColor = true;
			//
			// m_panelTagging
			//
			resources.ApplyResources(this.m_panelTagging, "m_panelTagging");
			this.m_panelTagging.Controls.Add(this.m_taggingPane);
			this.m_panelTagging.Name = "m_panelTagging";
			//
			// m_taggingPane
			//
			resources.ApplyResources(this.m_taggingPane, "m_taggingPane");
			this.m_taggingPane.BackColor = System.Drawing.SystemColors.Window;
			this.m_taggingPane.DoSpellCheck = false;
			this.m_taggingPane.ForEditing = false;
			this.m_taggingPane.Group = null;
			this.m_taggingPane.IsTextBox = false;
			this.m_taggingPane.Mediator = null;
			this.m_taggingPane.Name = "m_taggingPane";
			this.m_taggingPane.ReadOnlyView = true;
			this.m_taggingPane.ScrollMinSize = new System.Drawing.Size(0, 0);
			this.m_taggingPane.ScrollPosition = new System.Drawing.Point(0, 0);
			this.m_taggingPane.ShowRangeSelAfterLostFocus = false;
			this.m_taggingPane.SizeChangedSuppression = false;
			this.m_taggingPane.WritingSystemFactory = null;
			this.m_taggingPane.WsPending = -1;
			this.m_taggingPane.Zoom = 1F;
			//
			// m_tpPrintView
			//
			resources.ApplyResources(this.m_tpPrintView, "m_tpPrintView");
			this.m_tpPrintView.Controls.Add(this.m_panelPrintView);
			this.m_tpPrintView.Name = "m_tpPrintView";
			this.m_tpPrintView.UseVisualStyleBackColor = true;
			//
			// m_panelPrintView
			//
			resources.ApplyResources(this.m_panelPrintView, "m_panelPrintView");
			this.m_panelPrintView.Controls.Add(this.m_printViewPane);
			this.m_panelPrintView.Name = "m_panelPrintView";
			//
			// m_printViewPane
			//
			resources.ApplyResources(this.m_printViewPane, "m_printViewPane");
			this.m_printViewPane.BackColor = System.Drawing.SystemColors.Window;
			this.m_printViewPane.DoSpellCheck = false;
			this.m_printViewPane.ForEditing = false;
			this.m_printViewPane.Group = null;
			this.m_printViewPane.IsTextBox = false;
			this.m_printViewPane.Mediator = null;
			this.m_printViewPane.Name = "m_printViewPane";
			this.m_printViewPane.ReadOnlyView = true;
			this.m_printViewPane.ScrollMinSize = new System.Drawing.Size(0, 0);
			this.m_printViewPane.ScrollPosition = new System.Drawing.Point(0, 0);
			this.m_printViewPane.ShowRangeSelAfterLostFocus = false;
			this.m_printViewPane.SizeChangedSuppression = false;
			this.m_printViewPane.WritingSystemFactory = null;
			this.m_printViewPane.WsPending = -1;
			this.m_printViewPane.Zoom = 1F;
			//
			// m_tpCChart
			//
			resources.ApplyResources(this.m_tpCChart, "m_tpCChart");
			this.m_tpCChart.Name = "m_tpCChart";
			this.m_tpCChart.UseVisualStyleBackColor = true;
			//
			// m_constChartPane is only 'known' as an InterlinDocChart and is still
			// null at this point!
			//
			//resources.ApplyResources(this.m_constChartPane, "m_constChartPane");
			//this.m_constChartPane.BackColor = System.Drawing.SystemColors.Window;
			//this.m_constChartPane.Name = "m_constChartPane";
			//
			// InterlinMaster
			//
			this.Controls.Add(this.m_tabCtrl);
			this.Controls.Add(this.m_tcPane);
			this.Name = "InterlinMaster";
			this.m_tabCtrl.ResumeLayout(false);
			this.m_tpInfo.ResumeLayout(false);
			this.m_tpRawText.ResumeLayout(false);
			this.m_tpGloss.ResumeLayout(false);
			this.m_panelGloss.ResumeLayout(false);
			this.m_tpInterlinear.ResumeLayout(false);
			this.m_panelAnalyzeView.ResumeLayout(false);
			this.m_tpTagging.ResumeLayout(false);
			this.m_panelTagging.ResumeLayout(false);
			this.m_tpPrintView.ResumeLayout(false);
			this.m_panelPrintView.ResumeLayout(false);
			this.m_tpCChart.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		protected TitleContentsPane m_tcPane;
		protected TabControl m_tabCtrl;
		private TabPage m_tpInfo;
		private TabPage m_tpRawText;
		private RawTextPane m_rtPane;
		private TabPage m_tpGloss;
		private TabPage m_tpInterlinear;
		private TabPage m_tpTagging;
		private TabPage m_tpPrintView;
		private TabPage m_tpCChart;
		private Panel m_panelPrintView;
		private InterlinPrintChild m_printViewPane;
		private Panel m_panelAnalyzeView;
		private InterlinDocForAnalysis m_idcAnalyze;
		private Panel m_panelTagging;
		private InterlinTaggingChild m_taggingPane;
		private Panel m_panelGloss;
		private InterlinDocForAnalysis m_idcGloss;

		/// <summary>
		/// This variable is the main constituent chart pane, SIL.FieldWorks.Discourse.ConstituentChart.
		/// Because of problems with circular references, we define it just as a InterlinDocChart,
		/// a dummy subclass of UserControl, and create it by reflection).
		/// This subclass is probably not really needed.
		/// Parent is m_tpCChart.
		/// </summary>
		internal InterlinDocChart m_constChartPane;
	}
}
