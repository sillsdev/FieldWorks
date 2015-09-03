// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using Paratext;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.XWorks;
using SIL.Utils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.IText
{
	public class InterlinearTextsRecordClerk : RecordClerk, IBookImporter
	{
		private FwStyleSheet m_stylesheet;

		// The following is used in the process of selecting the ws for a new text.  See LT-6692.
		private int m_wsPrevText;
		public int PrevTextWs
		{
			get { return m_wsPrevText; }
			set { m_wsPrevText = value; }
		}

		/// <summary>
		/// Get the list of currently selected Scripture section ids.
		/// </summary>
		/// <returns></returns>
		public List<int> GetScriptureIds()
		{
			return (from st in GetInterestingTextList().ScriptureTexts select st.Hvo).ToList();
		}

		protected override string FilterStatusContents(bool listIsFiltered)
		{
			var baseStatus = base.FilterStatusContents(listIsFiltered);
			var interestingTexts = GetInterestingTextList();
			if (interestingTexts.AllCoreTextsAreIncluded)
				return baseStatus;
			return string.Format(ITextStrings.ksSomeTexts, interestingTexts.CoreTexts.Count,
				interestingTexts.AllCoreTexts.Count()) + (string.IsNullOrEmpty(baseStatus) ? "" : "; " + baseStatus);
		}

		/// <summary>
		/// The current object in this view is either a WfiWordform or an StText, and if we can delete
		/// an StText at all, we want to delete its owning Text.
		/// </summary>
		/// <param name="currentObject"></param>
		/// <returns></returns>
		protected override ICmObject GetObjectToDelete(ICmObject currentObject)
		{
			if (currentObject is IWfiWordform)
				return currentObject;
			return currentObject.Owner;
		}

		/// <summary>
		/// We can only delete Texts in this view, not scripture sections.
		/// </summary>
		/// <returns></returns>
		protected override bool CanDelete()
		{
			if (CurrentObject is IWfiWordform)
				return true;
			return CurrentObject.Owner is FDO.IText;
		}

		public override void ReloadIfNeeded()
		{
			if (m_list as ConcordanceWordList != null)
			{
				((ConcordanceWordList)m_list).RequestRefresh();
			}
			base.ReloadIfNeeded();
		}

		public override bool OnRefresh(object sender)
		{
			if(m_list as ConcordanceWordList != null)
			{
				((ConcordanceWordList)m_list).RequestRefresh();
			}
			return base.OnRefresh(sender);
		}

		protected override void ReportCannotDelete()
		{
			if (CurrentObject is IWfiWordform)
				MessageBox.Show(Form.ActiveForm, ITextStrings.ksCannotDeleteWordform, ITextStrings.ksError,
								MessageBoxButtons.OK, MessageBoxIcon.Error);
			else
				MessageBox.Show(Form.ActiveForm, ITextStrings.ksCannotDeleteScripture, ITextStrings.ksError,
								MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		protected override bool AddItemToList(int hvoItem)
		{

			IStText stText;
			if (!Cache.ServiceLocator.GetInstance<IStTextRepository>().TryGetObject(hvoItem, out stText))
			{
				// Not an StText; we have no idea how to add it (possibly a WfiWordform?).
				return base.AddItemToList(hvoItem);
			}
			var interestingTexts = GetInterestingTextList();
			return interestingTexts.AddChapterToInterestingTexts(stText);
		}

#if RANDYTODO
		/// <summary>
		/// This toolbar option no longer applies only to Scripture.
		/// Any scripture related control function is handled in the IFilterTextsDialog implementation.
		/// Simply test that there is an active clerk before enabling.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayAddTexts(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			display.Enabled = IsActiveClerk;
			display.Visible = display.Enabled;
			return true;
		}
#endif

		/// <summary>
		/// This method should cause all paragraphs in interesting texts which do not have the ParseIsCurrent flag set
		/// to be Parsed. Created for use with ConcordanceWordList lists.
		/// </summary>
		public void ParseInterstingTextsIfNeeded()
		{
			//Optimize(JT): The reload is overkill, all we want to do is reparse those texts who are not up to date.
			if(m_list != null)
			{
				m_list.ForceReloadList();
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Gendarme is just too dumb to understand the try...finally pattern to ensure disposal of dlg")]
		protected internal bool OnAddTexts(object args)
		{
			CheckDisposed();
			// get saved scripture choices
			var interestingTextsList = GetInterestingTextList();
			var interestingTexts = interestingTextsList.InterestingTexts.ToArray();

			IFilterTextsDialog<IStText> dlg = null;
			try
			{
				if (Cache.ServiceLocator.GetInstance<IScrBookRepository>().AllInstances().Any())
				{
					dlg = new FilterTextsDialogTE(Cache, interestingTexts, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), this);
				}
				else
				{
					dlg = new FilterTextsDialog(Cache, interestingTexts, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"));
				}
				if (dlg.ShowDialog(PropertyTable.GetValue<IApp>("App").ActiveMainWindow) == DialogResult.OK)
				{
					interestingTextsList.SetInterestingTexts(dlg.GetListOfIncludedTexts());
					UpdateFilterStatusBarPanel();
				}
			}
			finally
			{
				if (dlg != null)
					((IDisposable)dlg).Dispose();
			}

			return true;
		}

		private InterestingTextList GetInterestingTextList()
		{
			return InterestingTextsDecorator.GetInterestingTextList(PropertyTable, Cache.ServiceLocator);
		}

#if RANDYTODO
		/// <summary>
		/// Always enable the 'InsertInterlinText' command by default for this class, but allow
		/// subclasses to override this behavior.
		/// </summary>
		public virtual bool OnDisplayInsertInterlinText(object commandObject,
														ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Visible = IsActiveClerk && InDesiredArea("textsWords");
			if (!display.Visible)
			{
				display.Enabled = false;
				return true; // or should we just say, we don't know? But this command definitely should only be possible when this IS active.
			}

			RecordClerk clrk = m_propertyTable.GetValue<RecordClerk>("ActiveClerk");
			if (clrk != null && clrk.Id == "interlinearTexts")
			{
				display.Enabled = true;
				return true;
			}
			display.Enabled = false;
			return true;
		}
#endif

		/// <summary>
		/// We use a unique method name for inserting a text, which could otherwise be handled simply
		/// by letting the Clerk handle InsertItemInVector, because after it is inserted we may
		/// want to switch tools.
		/// The argument should be the XmlNode for <parameters className="Text"/>.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnInsertInterlinText(object argument)
		{
			if (!IsActiveClerk || !InDesiredArea("textsWords"))
				return false;
#if RANDYTODO
			return AddNewText(argument as Command);
#else
			return false;
#endif
		}

		/// <summary>
		/// Add a new text (but don't make it undoable)
		/// </summary>
		/// <returns></returns>
		internal bool AddNewTextNonUndoable()
		{
#if RANDYTODO
			return AddNewText(null);
#else
			return false;
#endif
		}

#if RANDYTODO
		private bool AddNewText(Command command)
		{
			// Get the default writing system for the new text.  See LT-6692.
			m_wsPrevText = Cache.DefaultVernWs;
			if (CurrentObject != null && Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Count > 1)
			{
				m_wsPrevText = WritingSystemServices.ActualWs(Cache, WritingSystemServices.kwsVernInParagraph,
															  CurrentObject.Hvo, StTextTags.kflidParagraphs);
			}
			if (m_list.Filter != null)
			{
				// Tell the user we're turning off the filter, and then do it.
				MessageBox.Show(ITextStrings.ksTurningOffFilter, ITextStrings.ksNote, MessageBoxButtons.OK);
				m_mediator.SendMessage("RemoveFilters", this);
				m_activeMenuBarFilter = null;
			}
			SaveOnChangeRecord(); // commit any changes before we create a new text.
			RecordList.ICreateAndInsert<IStText> createAndInsertMethodObj;
			if (command != null)
				createAndInsertMethodObj = new UndoableCreateAndInsertStText(Cache, command, this);
			else
				createAndInsertMethodObj = new NonUndoableCreateAndInsertStText(Cache, this);
			var newText = m_list.DoCreateAndInsert(createAndInsertMethodObj);

			// Check to if a genre was assigned to this text
			// (when selected from the text list: ie a genre w/o a text was sellected)
			string property = GetCorrespondingPropertyName("DelayedGenreAssignment");
			var genreList = m_propertyTable.GetValue<List<TreeNode>>(property, null);
			var ownerText = newText.Owner as FDO.IText;
			if (genreList != null && genreList.Count > 0 && ownerText != null)
			{
				foreach (var node in genreList)
				{
					ownerText.GenresRC.Add((ICmPossibility)node.Tag);
				}
				m_propertyTable.RemoveProperty(property);
			}

			if (CurrentObject == null || CurrentObject.Hvo == 0)
				return false;
			if (!InDesiredTool("interlinearEdit"))
			{
				var commands = new List<string>
						{
							"AboutToFollowLink",
							"FollowLink"
						};
				var parms = new List<object>
						{
							null,
							new FwLinkArgs("interlinearEdit", CurrentObject.Guid)
						};
				Publisher.Publish(commands, parms);
			}
			// This is a workable alternative (where link is the one created above), but means this code has to know about the FwXApp class.
			//(FwXApp.App as FwXApp).OnIncomingLink(link);
			// This alternative does NOT work; it produces a deadlock...I think the remote code is waiting for the target app
			// to return to its message loop, but it never does, because it is the same app that is trying to send the link, so it is busy
			// waiting for 'Activate' to return!
			//link.Activate();
			return true;
		}
#endif

		internal abstract class CreateAndInsertStText : RecordList.ICreateAndInsert<IStText>
		{
			internal CreateAndInsertStText(FdoCache cache, InterlinearTextsRecordClerk clerk)
			{
				Cache = cache;
				Clerk = clerk;
			}

			protected InterlinearTextsRecordClerk Clerk;
			protected FdoCache Cache;
			protected IStText NewStText;

			#region ICreateAndInsert<IStText> Members

			public abstract IStText Create();

			/// <summary>
			/// updates NewStText
			/// </summary>
			protected void CreateNewTextWithEmptyParagraph(int wsText)
			{
				var newText =
					Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
				NewStText =
					Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				newText.ContentsOA = NewStText;
				Clerk.CreateFirstParagraph(NewStText, wsText);
				InterlinMaster.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(NewStText, false);
			}

			#endregion
		}

		internal class UndoableCreateAndInsertStText : CreateAndInsertStText
		{
			internal UndoableCreateAndInsertStText(FdoCache cache, InterlinearTextsRecordClerk clerk)
				: base(cache, clerk)
			{
#if RANDYTODO
				CommandArgs = command;
#endif
			}
#if RANDYTODO
			private Command CommandArgs;
#endif

			public override IStText Create()
			{
#if RANDYTODO
				// don't inline this, it launches a dialog and should be done BEFORE starting the UOW.
				int wsText = Clerk.GetWsForNewText();

				UndoableUnitOfWorkHelper.Do(CommandArgs.UndoText, CommandArgs.RedoText, Cache.ActionHandlerAccessor,
											()=> CreateNewTextWithEmptyParagraph(wsText));
#endif
				return NewStText;
			}
		}

		internal class NonUndoableCreateAndInsertStText : CreateAndInsertStText
		{
			internal NonUndoableCreateAndInsertStText(FdoCache cache, InterlinearTextsRecordClerk clerk)
				: base(cache, clerk)
			{
			}

			public override IStText Create()
			{
				// don't inline this, it launches a dialog and should be done BEFORE starting the UOW.
				int wsText = Clerk.GetWsForNewText();

				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor,
																   () => CreateNewTextWithEmptyParagraph(wsText));
				return NewStText;
			}
		}

		/// <summary>
		/// Establish the writing system of the new text by filling its first paragraph with
		/// an empty string in the proper writing system.
		/// </summary>
		internal void CreateFirstParagraph(IStText stText, int wsText)
		{
			var txtPara = stText.AddNewTextPara(null);
			txtPara.Contents = TsStringUtils.MakeTss(string.Empty, wsText);
		}

		private int GetWsForNewText()
		{
			int wsText = PrevTextWs;
			if (wsText != 0)
			{
				if (Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Count == 1)
				{
					wsText = Cache.DefaultVernWs;
				}
				else
				{
					using (var dlg = new ChooseTextWritingSystemDlg())
					{
						dlg.Initialize(Cache, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), wsText);
						dlg.ShowDialog(Form.ActiveForm);
						wsText = dlg.TextWs;
					}
				}
				PrevTextWs = 0;
			}
			else
			{
				wsText = Cache.DefaultVernWs;
			}
			return wsText;
		}

		/// <summary>
		/// This class creates text, it must delete it here when UNDO is commanded
		/// so it can update InterestingTexts.
		/// </summary>
/*		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (cvDel != 1)
				return;
			SaveOnChangeRecord();
			SuppressSaveOnChangeRecord = true;
			try
			{
				m_list.DeleteCurrentObject();
			}
			finally
			{
				SuppressSaveOnChangeRecord = false;
			}
			GetInterestingTextList().UpdateInterestingTexts();
		} */

		#region IBookImporter Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Imports the specified book.
		/// </summary>
		/// <param name="bookNum">The canonical book number.</param>
		/// <param name="owningForm">Form that can be used as the owner of progress dialogs and
		/// message boxes.</param>
		/// <param name="importBt">True to import only the back translation, false to import
		/// only the main translation</param>
		/// <returns>
		/// The ScrBook created to hold the imported data
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook Import(int bookNum, Form owningForm, bool importBt)
		{
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;
			bool haveSomethingToImport = NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					IScrImportSet importSettings = scr.FindOrCreateDefaultImportSettings(TypeOfImport.Paratext6);
					ScrText paratextProj = ParatextHelper.GetAssociatedProject(Cache.ProjectId);
					importSettings.ParatextScrProj = paratextProj.Name;
					importSettings.StartRef = new BCVRef(bookNum, 0, 0);
					int chapter = paratextProj.Versification.LastChapter(bookNum);
					importSettings.EndRef = new BCVRef(bookNum, chapter, paratextProj.Versification.LastVerse(bookNum, chapter));
					if (!importBt)
					{
						importSettings.ImportTranslation = true;
						importSettings.ImportBackTranslation = false;
					}
					else
					{
						List<ScrText> btProjects = ParatextHelper.GetBtsForProject(paratextProj).ToList();
						if (btProjects.Count > 0 && (string.IsNullOrEmpty(importSettings.ParatextBTProj) ||
													 !btProjects.Any(st => st.Name == importSettings.ParatextBTProj)))
						{
							importSettings.ParatextBTProj = btProjects[0].Name;
						}
						if (string.IsNullOrEmpty(importSettings.ParatextBTProj))
							return false;
						importSettings.ImportTranslation = false;
						importSettings.ImportBackTranslation = true;
					}
					ParatextHelper.LoadProjectMappings(importSettings);
					return true;
				});

			if (haveSomethingToImport && ReflectionHelper.GetBoolResult(ReflectionHelper.GetType("TeImportExport.dll",
				"SIL.FieldWorks.TE.TeImportManager"), "ImportParatext", owningForm, ScriptureStylesheet,
				PropertyTable.GetValue<IFlexApp>("App")))
			{
				return scr.FindBook(bookNum);
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Scripture stylesheet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private FwStyleSheet ScriptureStylesheet
		{
			get
			{
				if (m_stylesheet == null)
				{
					m_stylesheet = new FwStyleSheet();
					m_stylesheet.Init(Cache, Cache.LangProject.TranslatedScriptureOA.Hvo, ScriptureTags.kflidStyles);
				}
				return m_stylesheet;
			}
		}
		#endregion
	}
}