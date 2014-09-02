// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: NotebookExportDialog.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Palaso.WritingSystems;
using XCore;
using System.Text;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;
using System.Xml.Xsl;

namespace SIL.FieldWorks.XWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Overrides various aspects of ExportDialog to support exporting from the Data Notebook
	/// section of Language Explorer.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class NotebookExportDialog : ExportDialog
	{
		List<int> m_customFlids = new List<int>();
		IFwMetaDataCacheManaged m_mdc;
		bool m_fRightToLeft = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public NotebookExportDialog(Mediator mediator)
			: base(mediator)
		{
			m_helpTopic = "khtpExportNotebook";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the relative path to the export configuration files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string ConfigurationFilePath
		{
			get { return String.Format("Language Explorer{0}Export Templates{0}Notebook", Path.DirectorySeparatorChar); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store any additional attributes of the configuration nodes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ConfigureItem(XmlDocument document, ListViewItem item, XmlNode ddNode)
		{
			base.ConfigureItem(document, item, ddNode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check whether the given export should be disabled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool ItemDisabled(string tag)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allow custom final preparation before asking for file.  See LT-8403.
		/// </summary>
		/// <returns>true iff export can proceed further</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool PrepareForExport()
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the actual export.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void DoExport(string outPath)
		{
			string fxtPath = (string) m_exportList.SelectedItems[0].Tag;
			FxtType ft = m_rgFxtTypes[FxtIndex(fxtPath)];
			using (new WaitCursor(this))
			{
				using (var progressDlg = new ProgressDialogWithTask(this))
				{
					try
					{
						progressDlg.Title = String.Format(xWorksStrings.Exporting0,
							m_exportList.SelectedItems[0].Text);
						progressDlg.Message = xWorksStrings.Exporting_;
						switch (ft.m_ft)
						{
							case FxtTypes.kftFxt:
								progressDlg.RunTask(true, ExportNotebook, outPath, fxtPath, ft);
								break;
							case FxtTypes.kftConfigured:
								progressDlg.Minimum = 0;
								progressDlg.Maximum = m_seqView.ObjectCount;
								progressDlg.AllowCancel = true;

								IVwStylesheet vss = m_seqView.RootBox == null ? null : m_seqView.RootBox.Stylesheet;
								progressDlg.RunTask(true, ExportConfiguredDocView,
									outPath, fxtPath, ft, vss);
								break;
						}
					}
					catch (WorkerThreadException e)
					{
						if (e.InnerException is CancelException)
						{
							MessageBox.Show(this, e.InnerException.Message);
							m_ce = null;
						}
						else
						{
							string msg = xWorksStrings.ErrorExporting_ProbablyBug + Environment.NewLine + e.InnerException.Message;
							MessageBox.Show(this, msg);
						}
					}
					finally
					{
						m_progressDlg = null;
						Close();
					}
				}
			}
		}

		object ExportNotebook(IProgress progress, object[] args)
		{
			string outPath = (string)args[0];
			string fxtPath = (string)args[1];
			FxtType ft = (FxtType)args[2];
			progress.Minimum = 0;
			progress.Maximum = m_cache.LangProject.ResearchNotebookOA.RecordsOC.Count + 6;
			using (var writer = new StreamWriter(outPath)) // defaults to UTF-8
			{
				writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
				writer.WriteLine("<Notebook exportVersion=\"2.0\" project=\"{0}\" dateExported=\"{1}\">",
					m_cache.ProjectId.UiName, DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss"));
				progress.Message = "Exporting data records...";
				ExportRecords(writer, progress);
				progress.Message = "Exporting writing systems...";
				ExportLanguages(writer);
				progress.Step(3);
				progress.Message = "Exporting styles...";
				ExportStyles(writer);
				progress.Step(3);
				writer.WriteLine("</Notebook>");
			}
			if (!String.IsNullOrEmpty(ft.m_sXsltFiles))
			{
				string[] rgsXslts = ft.m_sXsltFiles.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				int cXslts = rgsXslts.Length;
				if (cXslts > 0)
				{
					progress.Position = 0;
					progress.Minimum = 0;
					progress.Maximum = cXslts;
					progress.Message = xWorksStrings.ProcessingIntoFinalForm;
					string basePath = Path.GetDirectoryName(fxtPath);
					for (int ix = 0; ix < cXslts; ++ix)
					{
						string sXsltPath = Path.Combine(basePath, rgsXslts[ix]);
						// Apply XSLT to the output file, first renaming it so that the user sees
						// the expected final output file.
						CollectorEnv.ProcessXsltForPass(sXsltPath, outPath, ix + 1);
						progress.Step(1);
					}
				}
			}
			return null;
		}


		private void ExportLanguages(TextWriter writer)
		{
			string sAnal = AnalWsTag;
			string sVern = m_cache.WritingSystemFactory.GetStrFromWs(m_cache.DefaultVernWs);
			writer.WriteLine("<Languages defaultAnal=\"{0}\" defaultVern=\"{1}\">",
				sAnal, sVern);
			IWritingSystemManager manager = m_cache.ServiceLocator.GetInstance<IWritingSystemManager>();
			foreach (var wsLocal in manager.LocalWritingSystems)
			{
				string tag = LangTagUtils.ToLangTag(wsLocal.LanguageSubtag,
					wsLocal.ScriptSubtag, wsLocal.RegionSubtag, wsLocal.VariantSubtag);
				ILgWritingSystem lgws = null;
				int ws = m_cache.WritingSystemFactory.GetWsFromStr(tag);
				if (ws <= 0)
					continue;
				lgws = m_cache.WritingSystemFactory.get_EngineOrNull(ws);
				string code = wsLocal.LanguageSubtag.Code;
				string type = code.Length == 2 ? "ISO-639-1" : "ISO-639-3";
				writer.WriteLine("<WritingSystem id=\"{0}\" language=\"{1}\" type=\"{2}\">",
					tag, code, type);
				writer.WriteLine("<Name><Uni>{0}</Uni></Name>",
					XmlUtils.MakeSafeXml(wsLocal.LanguageName));
				writer.WriteLine("<Abbreviation><Uni>{0}</Uni></Abbreviation>",
					XmlUtils.MakeSafeXml(wsLocal.Abbreviation));
				// We previously wrote out the LCID, but this is obsolete. It would be unreliable to output the WindowsLcid, which only
				// old writing systems will have. If something needs this, we need to output something new in its place. But I'm pretty sure
				// nothing does...Locale is not used in any of the notebook output transforms.
				//writer.WriteLine("<Locale><Integer val=\"{0}\"/></Locale>", ((ILegacyWritingSystemDefinition)wsLocal).WindowsLcid);
				writer.WriteLine("<RightToLeft><Boolean val=\"{0}\"/></RightToLeft>",
					wsLocal.RightToLeftScript ? "true" : "false");
				if (ws == m_cache.DefaultAnalWs)
					m_fRightToLeft = wsLocal.RightToLeftScript;
				writer.WriteLine("<DefaultFont><Uni>{0}</Uni></DefaultFont>",
					XmlUtils.MakeSafeXml(wsLocal.DefaultFontName));
				if (!String.IsNullOrEmpty(wsLocal.DefaultFontFeatures))
					writer.WriteLine("<DefaultFontFeatures><Uni>{0}</Uni></DefaultFontFeatures>",
						XmlUtils.MakeSafeXml(wsLocal.DefaultFontFeatures));
				// The following commented out data are probably never needed.
				//if (!String.IsNullOrEmpty(wsLocal.ValidChars))
				//    writer.WriteLine("<ValidChars><Uni>{0}</Uni></ValidChars>",
				//        XmlUtils.MakeSafeXml(wsLocal.ValidChars));
				//writer.WriteLine("<ICULocale><Uni>{0}</Uni></ICULocale>",
				//    XmlUtils.MakeSafeXml(wsLocal.IcuLocale));
				writer.WriteLine("<SortUsing><Uni>{0}</Uni></SortUsing>",
					XmlUtils.MakeSafeXml(wsLocal.SortUsing.ToString()));
				writer.WriteLine("<SortRules><Uni>{0}</Uni></SortRules>",
					XmlUtils.MakeSafeXml(wsLocal.SortRules));
				writer.WriteLine("</WritingSystem>");
			}
			writer.WriteLine("</Languages>");
		}

		private void ExportStyles(TextWriter writer)
		{
			writer.WriteLine("<Styles>");
			foreach (var style in m_cache.LangProject.StylesOC)
			{
				writer.WriteLine("<StStyle>");
				writer.WriteLine("<Name><Uni>{0}</Uni></Name>", XmlUtils.MakeSafeXml(style.Name));
				writer.WriteLine("<Type><Integer val=\"{0}\"/></Type>", (int)style.Type);
				writer.WriteLine("<BasedOn><Uni>{0}</Uni></BasedOn>",
					style.BasedOnRA == null ? String.Empty : XmlUtils.MakeSafeXml(style.BasedOnRA.Name));
				writer.WriteLine("<Next><Uni>{0}</Uni></Next>",
					style.NextRA == null ? String.Empty : XmlUtils.MakeSafeXml(style.NextRA.Name));
				writer.WriteLine("<Rules>");
				writer.Write(TsStringUtils.GetXmlRep(style.Rules, m_cache.WritingSystemFactory));
				writer.WriteLine("</Rules>");
				writer.WriteLine("</StStyle>");
			}
			writer.WriteLine("</Styles>");
		}

		private void ExportRecords(TextWriter writer, IProgress progress)
		{
			m_mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			foreach (int flid in m_mdc.GetFields(RnGenericRecTags.kClassId, true,
				(int)CellarPropertyTypeFilter.All))
			{
				if (m_mdc.IsCustom(flid))
					m_customFlids.Add(flid);
			}


			writer.WriteLine("<Entries docRightToLeft=\"{0}\">",
				m_fRightToLeft ? "true" : "false");
			foreach (var record in m_cache.LangProject.ResearchNotebookOA.RecordsOC)
			{
				ExportRecord(writer, record, 0);
				progress.Step(1);
			}
			writer.WriteLine("</Entries>");
		}

		ITsString m_tssSpace = null;
		ITsString TssSpace
		{
			get
			{
				if (m_tssSpace == null)
				{
					ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultAnalWs);
					tisb.Append(" ");
					m_tssSpace = tisb.GetString();
				}
				return m_tssSpace;
			}
		}

		ITsString m_tssCommaSpace = null;
		ITsString TssCommaSpace
		{
			get
			{
				if (m_tssCommaSpace == null)
				{
					ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultAnalWs);
					tisb.Append(", ");
					m_tssCommaSpace = tisb.GetString();
				}
				return m_tssCommaSpace;
			}
		}

		string m_wsUserTag = null;
		string UserWsTag
		{
			get
			{
				if (m_wsUserTag == null)
					m_wsUserTag = m_cache.WritingSystemFactory.GetStrFromWs(m_cache.DefaultUserWs);
				return m_wsUserTag;
			}
		}

		string m_wsAnalTag = null;
		string AnalWsTag
		{
			get
			{
				if (m_wsAnalTag == null)
					m_wsAnalTag = m_cache.WritingSystemFactory.GetStrFromWs(m_cache.DefaultAnalWs);
				return m_wsAnalTag;
			}
		}

		private void ExportRecord(TextWriter writer, IRnGenericRec record, int level)
		{
			writer.WriteLine(
				"<Entry level=\"{0}\" dateCreated=\"{1}\" dateModified=\"{2}\" guid=\"{3}\">",
				level,
				record.DateCreated.ToString("yyyy-MM-ddThh:mm:ss"),
				record.DateModified.ToString("yyyy-MM-ddThh:mm:ss"),
				record.Guid);

			ExportString(writer, record.Title, "Title");

			ExportAtomicReference(writer, record.TypeRA, "Type", "CmPossibility");

			List<ICmPossibility> collection = new List<ICmPossibility>();
			collection.AddRange(record.RestrictionsRC);
			ExportReferenceList(writer, collection, "Restrictions", "CmPossibility",
				CellarPropertyType.ReferenceCollection);
			collection.Clear();

			if (!record.DateOfEvent.IsEmpty)
			{
				writer.WriteLine("<Field name=\"DateOfEvent\" type=\"GenDate\" card=\"atomic\">");
				writer.WriteLine("<Item ws=\"{0}\">{1}</Item>",
					UserWsTag, XmlUtils.MakeSafeXml(record.DateOfEvent.ToXMLExportShortString()));
				writer.WriteLine("</Field>");
			}

			collection.AddRange(record.TimeOfEventRC);
			ExportReferenceList(writer, collection, "TimeOfEvent", "CmPossibility",
				CellarPropertyType.ReferenceCollection);
			collection.Clear();

			collection.AddRange(record.ResearchersRC.ToArray());
			ExportReferenceList(writer, collection, "Researchers", "CmPerson",
				CellarPropertyType.ReferenceCollection);
			collection.Clear();

			collection.AddRange(record.SourcesRC.ToArray());
			ExportReferenceList(writer, collection, "Sources", "CmPerson",
				CellarPropertyType.ReferenceCollection);
			collection.Clear();

			ExportAtomicReference(writer, record.ConfidenceRA, "Confidence", "CmPossibility");

			if (record.ParticipantsOC != null && record.ParticipantsOC.Count > 0)
			{
				foreach (var part in record.ParticipantsOC)
				{
					collection.AddRange(part.ParticipantsRC.ToArray());
					if (part.RoleRA != null)
					{
						int wsRole;
						ITsString tssRole = part.RoleRA.Name.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out wsRole);
						ExportReferenceList(writer, collection, tssRole.Text, "RnRoledPartic",
							CellarPropertyType.ReferenceCollection);
					}
					else
					{
						ExportReferenceList(writer, collection, "Participants", "RnRoledPartic",
							CellarPropertyType.ReferenceCollection);
					}
					collection.Clear();
				}
			}

			if (record.LocationsRC != null && record.LocationsRC.Count > 0)
			{
				collection.AddRange(record.LocationsRC.ToArray());
				ExportReferenceList(writer, collection, "Locations", "CmLocation",
					CellarPropertyType.ReferenceCollection);
				collection.Clear();
			}

			ExportStText(writer, record.DescriptionOA, "Description");

			ExportStText(writer, record.HypothesisOA, "Hypothesis");

			ExportAtomicReference(writer, record.StatusRA, "Status", "CmPossibility");

			ExportStText(writer, record.DiscussionOA, "Discussion");

			if (record.AnthroCodesRC != null && record.AnthroCodesRC.Count > 0)
			{
				writer.WriteLine("<Field name=\"AnthroCodes\" type=\"CmAnthroItem\" card=\"collection\">");
				foreach (var item in record.AnthroCodesRC)
					writer.WriteLine("<Item ws=\"{0}\">{1}</Item>", AnalWsTag, XmlUtils.MakeSafeXml(item.AbbrAndName));
				writer.WriteLine("</Field>");
			}

			ExportStText(writer, record.ConclusionsOA, "Conclusions");

			if (record.SupportingEvidenceRS != null && record.SupportingEvidenceRS.Count > 0)
			{
				writer.WriteLine("<Field name=\"SupportingEvidence\" type=\"RnGenericRec\" card=\"sequence\">");
				foreach (var item in record.SupportingEvidenceRS)
				{
					writer.WriteLine("<Item guid=\"{0}\" ws=\"{1}\">{2}</Item>",
						item.Guid, AnalWsTag, GetLinkLabelForRecord(item));
				}
				writer.WriteLine("</Field>");
			}
			if (record.CounterEvidenceRS != null && record.CounterEvidenceRS.Count > 0)
			{
				writer.WriteLine("<Field name=\"CounterEvidence\" type=\"RnGenericRec\" card=\"sequence\">");
				foreach (var item in record.CounterEvidenceRS)
				{
					writer.WriteLine("<Item guid=\"{0}\" ws=\"{1}\">{2}</Item>",
						item.Guid, AnalWsTag, GetLinkLabelForRecord(item));
				}
				writer.WriteLine("</Field>");
			}
			if (record.SupersededByRC != null && record.SupersededByRC.Count > 0)
			{
				writer.WriteLine("<Field name=\"SupersededBy\" type=\"RnGenericRec\" card=\"collection\">");
				foreach (var item in record.SupersededByRC)
				{
					writer.WriteLine("<Item guid=\"{0}\" ws=\"{1}\">{2}</Item>",
						item.Guid, AnalWsTag, GetLinkLabelForRecord(item));
				}
				writer.WriteLine("</Field>");
			}
			if (record.SeeAlsoRC != null && record.SeeAlsoRC.Count > 0)
			{
				writer.WriteLine("<Field name=\"SeeAlso\" type=\"RnGenericRec\" card=\"collection\">");
				foreach (var item in record.SeeAlsoRC)
				{
					writer.WriteLine("<Item guid=\"{0}\" ws=\"{1}\">{2}</Item>",
						item.Guid, AnalWsTag, GetLinkLabelForRecord(item));
				}
				writer.WriteLine("</Field>");
			}
			ExportStText(writer, record.ExternalMaterialsOA, "ExternalMaterials");
			ExportStText(writer, record.FurtherQuestionsOA, "FurtherQuestions");
			ExportStText(writer, record.ResearchPlanOA, "ResearchPlan");
			ExportStText(writer, record.PersonalNotesOA, "PersonalNotes");

			// The following are in the model, but not used in practice.
			//ExportStText(writer, record.VersionHistoryOA, "VersionHistory");
			//if (record.RemindersRC != null && record.RemindersRC.Count > 0)
			//    MessageBox.Show("Cannot export Reminders from RnGenericRec", "Not yet implemented");
			//if (record.CrossReferencesRC != null && record.CrossReferencesRC.Count > 0)
			//    MessageBox.Show("Cannot export CrossReferences from RnGenericRec", "Not yet implemented");

			ExportCustomFields(writer, record);

			if (record.SubRecordsOS != null && record.SubRecordsOS.Count > 0)
			{
				writer.WriteLine("<Field name=\"Subentries\" type=\"RnGenericRec\" card=\"sequence\">");
				foreach (var subrec in record.SubRecordsOS)
					ExportRecord(writer, subrec, level + 1);
				writer.WriteLine("</Field>");
			}
			writer.WriteLine("</Entry>");
		}

		ICmPossibilityRepository m_repoPoss = null;
		IStTextRepository m_repoText = null;

		ICmPossibilityRepository PossibilityRepository
		{
			get
			{
				if (m_repoPoss == null)
					m_repoPoss = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
				return m_repoPoss;
			}
		}

		IStTextRepository StTextRepository
		{
			get
			{
				if (m_repoText == null)
					m_repoText = m_cache.ServiceLocator.GetInstance<IStTextRepository>();
				return m_repoText;
			}
		}

		private void ExportCustomFields(TextWriter writer, IRnGenericRec record)
		{
			ISilDataAccessManaged sda = m_cache.DomainDataByFlid as ISilDataAccessManaged;
			Debug.Assert(sda != null);
			foreach (int flid in m_customFlids)
			{
				string fieldName = m_mdc.GetFieldName(flid);
				bool fHandled = false;
				ITsString tss;
				string s;
				CellarPropertyType cpt = (CellarPropertyType)m_mdc.GetFieldType(flid);
				switch (cpt)
				{
					case CellarPropertyType.Boolean:
						break;
					case CellarPropertyType.Integer:
						break;
					case CellarPropertyType.Numeric:
						break;
					case CellarPropertyType.Float:
						break;
					case CellarPropertyType.Time:
						break;
					case CellarPropertyType.Guid:
						break;
					case CellarPropertyType.Image:
					case CellarPropertyType.Binary:
						break;
					case CellarPropertyType.GenDate:
						break;
					case CellarPropertyType.String:
						tss = sda.get_StringProp(record.Hvo, flid);
						if (tss != null && tss.Text != null)
							ExportString(writer, tss, fieldName);
						fHandled = true;
						break;
					case CellarPropertyType.MultiString:
					case CellarPropertyType.MultiUnicode:
						ITsMultiString tms = sda.get_MultiStringProp(record.Hvo, flid);
						int cch = 0;
						for (int i = 0; i < tms.StringCount; ++i)
						{
							int ws;
							tss = tms.GetStringFromIndex(i, out ws);
							cch += tss.Length;
							if (cch > 0)
								break;
						}
						if (cch > 0)
						{
							writer.WriteLine("<Field name=\"{0}\" type=\"MultiString\">", fieldName);
							for (int i = 0; i < tms.StringCount; ++i)
							{
								int ws;
								tss = tms.GetStringFromIndex(i, out ws);
								if (tss != null && tss.Length > 0)
								{
									if (cpt == CellarPropertyType.MultiString)
									{
										writer.WriteLine(TsStringUtils.GetXmlRep(tss,
											m_cache.WritingSystemFactory, ws, true));
									}
									else
									{
										writer.WriteLine("<AUni ws=\"{0}\">{1}</AUni>",
											m_cache.WritingSystemFactory.GetStrFromWs(ws),
											XmlUtils.MakeSafeXml(tss.Text));
									}
								}
							}
							writer.WriteLine("</Field>");
						}
						fHandled = true;
						break;
					case CellarPropertyType.Unicode:
						break;
					case CellarPropertyType.ReferenceAtomic:
					case CellarPropertyType.ReferenceCollection:
					case CellarPropertyType.ReferenceSequence:
						{
							int destClid = m_mdc.GetDstClsId(flid);
							List<int> rghvoDest = new List<int>();
							if (cpt == CellarPropertyType.ReferenceAtomic)
							{
								int hvo = sda.get_ObjectProp(record.Hvo, flid);
								if (hvo != 0)
								{
									if (destClid == CmPossibilityTags.kClassId)
									{
										ICmPossibility poss = PossibilityRepository.GetObject(hvo);
										ExportAtomicReference(writer, poss, fieldName, "CmPossibility");
										fHandled = true;
									}
									else
									{
										rghvoDest.Add(hvo);
									}
								}
								else
								{
									fHandled = true;
								}
							}
							else
							{
								int[] hvos = sda.VecProp(record.Hvo, flid);
								if (hvos.Length > 0)
								{
									if (destClid == CmPossibilityTags.kClassId)
									{
										List<ICmPossibility> collection = new List<ICmPossibility>();
										foreach (int hvo in hvos)
											collection.Add(PossibilityRepository.GetObject(hvo));
										ExportReferenceList(writer, collection, fieldName, "CmPossibility", cpt);
										fHandled = true;
									}
									else
									{
										rghvoDest.AddRange(hvos);
									}
								}
								else
								{
									fHandled = true;
								}
							}
							if (rghvoDest.Count > 0)
							{
							}
						}
						break;
					case CellarPropertyType.OwningAtomic:
					case CellarPropertyType.OwningCollection:
					case CellarPropertyType.OwningSequence:
						{
							int destClid = m_mdc.GetDstClsId(flid);
							List<int> rghvoDest = new List<int>();
							if (cpt == CellarPropertyType.OwningAtomic)
							{
								int hvo = sda.get_ObjectProp(record.Hvo, flid);
								if (hvo != 0)
								{
									if (destClid == StTextTags.kClassId)
									{
										IStText text = StTextRepository.GetObject(hvo);
										ExportStText(writer, text, fieldName);
										fHandled = true;
									}
									else
									{
										rghvoDest.Add(hvo);
									}
								}
								else
								{
									fHandled = true;
								}
							}
							else
							{
							}
						}
						break;
				}
				if (!fHandled)
				{
				}
			}
		}

		private void ExportMultiString(TextWriter writer, ITsString tss, int ws,
			string fieldName, string p, string fieldName_6)
		{
			writer.WriteLine("<!-- ExportMultiString not yet written... -->");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a label for a cross-referenced record.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string GetLinkLabelForRecord(IRnGenericRec rec)
		{
			StringBuilder bldr = new StringBuilder();
			if (rec.TypeRA != null && rec.TypeRA.Name != null)
			{
				int ws;
				ITsString tss = rec.TypeRA.Name.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out ws);
				if (tss.Length > 0)
					bldr.AppendFormat("{0} - ", tss.Text);
			}
			if (rec.Title != null && rec.Title.Length > 0)
				bldr.Append(TsStringUtils.GetXmlRep(rec.Title, m_cache.WritingSystemFactory, 0, true));
			if (!rec.DateOfEvent.IsEmpty)
				bldr.AppendFormat(" - {0}", rec.DateOfEvent.ToXMLExportShortString());
			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export an atomic list reference field.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportAtomicReference(TextWriter writer, ICmPossibility poss,
			string fieldName, string targetType)
		{
			if (poss == null)
				return;
			int ws;
			ITsString tss = poss.Name.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out ws);
			writer.WriteLine("<Field name=\"{0}\" type=\"{1}\" card=\"atomic\">", fieldName, targetType);
			writer.WriteLine("<Item ws=\"{0}\">{1}</Item>",
				m_cache.WritingSystemFactory.GetStrFromWs(ws), XmlUtils.MakeSafeXml(tss.Text));
			writer.WriteLine("</Field>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a collection/sequence list reference field.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportReferenceList(TextWriter writer, List<ICmPossibility> collection,
			string fieldName, string targetType, CellarPropertyType cpt)
		{
			if (collection == null || collection.Count == 0)
				return;
			if (cpt == CellarPropertyType.ReferenceCollection)
				writer.WriteLine("<Field name=\"{0}\" type=\"{1}\" card=\"collection\">", fieldName, targetType);
			else
				writer.WriteLine("<Field name=\"{0}\" type=\"{1}\" card=\"sequence\">", fieldName, targetType);
			int ws;
			foreach (var item in collection)
			{
				ITsString tss = item.Name.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out ws);
				writer.WriteLine("<Item ws=\"{0}\">{1}</Item>",
					m_cache.WritingSystemFactory.GetStrFromWs(ws), XmlUtils.MakeSafeXml(tss.Text));
			}
			writer.WriteLine("</Field>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a simple string field.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportString(TextWriter writer, ITsString tss, string fieldName)
		{
			if (tss == null || tss.Length == 0)
				return;
			writer.WriteLine("<Field name=\"{0}\" type=\"TsString\">", fieldName);
			writer.Write(TsStringUtils.GetXmlRep(tss, m_cache.WritingSystemFactory, 0, true));
			writer.WriteLine("</Field>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a structured text field.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportStText(TextWriter writer, IStText text, string fieldName)
		{
			// Don't export an empty text.
			if (text == null || text.ParagraphsOS == null)
				return;
			// There may be paragraphs, but do they have any content?
			int cch = 0;
			foreach (var para in text.ParagraphsOS)
			{
				IStTxtPara stp = para as IStTxtPara;
				if (stp != null && stp.Contents != null)
					cch += stp.Contents.Length;
				if (cch > 0)
					break;
			}
			if (cch == 0)
				return;
			writer.WriteLine("<Field name=\"{0}\" type=\"StText\">", fieldName);
			foreach (var para in text.ParagraphsOS)
			{
				IStTxtPara stp = para as IStTxtPara;
				if (stp == null)
					continue;
				writer.WriteLine("<StTxtPara>");
				if (stp.StyleRules != null)
				{
					writer.WriteLine("<StyleRules>{0}</StyleRules>",
									 TsStringUtils.GetXmlRep(stp.StyleRules, m_cache.WritingSystemFactory));
				}
				writer.WriteLine("<Contents>");
				writer.Write(TsStringUtils.GetXmlRep(stp.Contents, m_cache.WritingSystemFactory, 0, true));
				writer.WriteLine("</Contents>");
				writer.WriteLine("</StTxtPara>");
			}
			writer.WriteLine("</Field>");
		}

		/// <summary>
		/// Allows process to find an appropriate root hvo and change the current root.
		/// Subclasses (like this one) can override.
		/// </summary>
		/// <param name="cmo"></param>
		/// <param name="clidRoot"></param>
		/// <returns>Returns -1 if root hvo doesn't need changing.</returns>
		protected override int SetRoot(ICmObject cmo, out int clidRoot)
		{
			if (cmo is IRnGenericRec) // this ought to be the case
			{
				var hvoRoot = -1;
				// Need to find the main notebook object.
				var notebk = m_cache.LanguageProject.ResearchNotebookOA;
				clidRoot = notebk.ClassID;
				return notebk.Hvo;
			}
			// just for saftey
			return base.SetRoot(cmo, out clidRoot);
		}
	}
}
