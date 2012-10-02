// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MasterPhonologicalFeatureListDlg.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.LexText.Controls.MGA;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for MasterInflectionFeatureListDlg.
	/// </summary>
	public class MasterInflectionFeatureListDlg : MasterListDlg
	{

		public MasterInflectionFeatureListDlg() : base()
		{
		}
		public MasterInflectionFeatureListDlg(string className) : base(className, new GlossListTreeView())
		{
		}

		protected override void DoExtraInit()
		{
			Text = LexTextControls.ksInflectionFeatureCatalogTitle;
			label1.Text = LexTextControls.ksInflectionFeatureCatalogPrompt;
			label2.Text = LexTextControls.ksInflectionFeatureCatalogTreeLabel;
			label3.Text = LexTextControls.ksInflectionFeatureCatalogDescriptionLabel;
			SetLinkLabel();
			s_helpTopic = "khtpInsertInflectionFeature";
		}

		private void SetLinkLabel()
		{
			string sInflFeature = LexTextControls.ksInflectionFeature;
			string sFeatureKind = sInflFeature;
			if (m_sClassName == "FsComplexFeature")
				sFeatureKind = LexTextControls.ksComplexFeature;
			linkLabel1.Text = String.Format(LexTextControls.ksLinkText, sInflFeature, sFeatureKind);
		}

		protected override void DoFinalActions(MasterItem mi)
		{
			PartOfSpeech.TryToAddInflectableFeature(m_cache, mi.Node, m_selFeatDefn);
		}

		protected override void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			m_cache.BeginUndoTask(LexTextControls.ksUndoInsertInflectionFeature,
				LexTextControls.ksRedoInsertInflectionFeature);
			int clsid;
			if (m_sClassName == "FsComplexFeature")
			{
				// NO: Since this fires a PropChanged, and we do it ourselves later on.
				// m_selFeatDefn = (FsComplexFeature)m_featureList.Add(new FsComplexFeature());
				clsid = FsComplexFeature.kClassId;
			}
			else
			{
				// NO: Since this fires a PropChanged, and we do it ourselves later on.
				// m_selFeatDefn = (FsClosedFeature)m_featureList.Add(new FsClosedFeature());
				clsid = FsClosedFeature.kClassId;
			}
			// CreateObject creates the entry without a PropChanged.
			int fsysHvo = m_cache.LangProject.MsFeatureSystemOAHvo;
			int flid = (int)FsFeatureSystem.FsFeatureSystemTags.kflidFeatures;
			int featureHvo = m_cache.CreateObject(clsid, fsysHvo, flid, 0); // 0 is fine, since the owning prop is not a sequence.
			m_selFeatDefn = (FsFeatDefn)CmObject.CreateFromDBObject(m_cache, featureHvo, true);
			m_cache.EndUndoTask();

			ForceRecordClerkToReload(fsysHvo, flid, featureHvo);

			DialogResult = DialogResult.Yes;
			Close();
		}

	}
}
