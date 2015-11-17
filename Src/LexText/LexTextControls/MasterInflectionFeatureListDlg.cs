// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MasterPhonologicalFeatureListDlg.cs
// Responsibility:
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls.MGA;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for MasterInflectionFeatureListDlg.
	/// </summary>
	public class MasterInflectionFeatureListDlg : MasterListDlg
	{

		public MasterInflectionFeatureListDlg()
		{
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "GlossListTreeView gets disposed in base class")]
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

		protected override void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			using (var undoHelper = new UndoableUnitOfWorkHelper(
				m_cache.ServiceLocator.GetInstance<IActionHandler>(),
				LexTextControls.ksUndoInsertInflectionFeature,
				LexTextControls.ksRedoInsertInflectionFeature))
			{
				IFsFeatDefn fd;
				if (m_sClassName == "FsComplexFeature")
					fd = m_cache.ServiceLocator.GetInstance<IFsComplexFeatureFactory>().Create();
				else
					fd = m_cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
				m_cache.LanguageProject.MsFeatureSystemOA.FeaturesOC.Add(fd);
				IFsFeatStrucType type = m_cache.LanguageProject.MsFeatureSystemOA.GetFeatureType("Infl");
				if (type == null)
				{
					type = m_cache.ServiceLocator.GetInstance<IFsFeatStrucTypeFactory>().Create();
					m_cache.LanguageProject.MsFeatureSystemOA.TypesOC.Add(type);
					type.CatalogSourceId = "Infl";
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.AnalysisWritingSystems)
					{
						var tss = m_cache.TsStrFactory.MakeString("Infl", ws.Handle);
						type.Abbreviation.set_String(ws.Handle, tss);
						type.Name.set_String(ws.Handle, tss);
					}
				}
				type.FeaturesRS.Add(fd);
				m_selFeatDefn = fd;

				undoHelper.RollBack = false;
			}

			DialogResult = DialogResult.Yes;
			Close();
		}
	}
}
