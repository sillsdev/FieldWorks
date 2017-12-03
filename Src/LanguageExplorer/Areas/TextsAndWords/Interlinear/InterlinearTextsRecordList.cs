// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Works;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class InterlinearTextsRecordList : RecordList
	{
		private LcmStyleSheet _stylesheet;

		// The following is used in the process of selecting the ws for a new text.  See LT-6692.
		internal int PrevTextWs { get; set; }

		internal InterlinearTextsRecordList(string id, StatusBar statusBar, RecordSorter defaultSorter, string defaultSortLabel, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion, ISilDataAccessManaged decorator, bool usingAnalysisWs, int flid, ICmObject owner, string propertyName)
			: base(id, statusBar, defaultSorter, defaultSortLabel, defaultFilter, allowDeletions, shouldHandleDeletion, decorator, usingAnalysisWs, flid, owner, propertyName)
		{
		}

		#region Overrides of RecordList

		protected override string FilterStatusContents(bool listIsFiltered)
		{
			var baseStatus = base.FilterStatusContents(listIsFiltered);
			var interestingTexts = GetInterestingTextList();
			if (interestingTexts.AllCoreTextsAreIncluded)
			{
				return baseStatus;
			}
			return string.Format(ITextStrings.ksSomeTexts, interestingTexts.CoreTexts.Count, interestingTexts.AllCoreTexts.Count()) + (string.IsNullOrEmpty(baseStatus) ? string.Empty : "; " + baseStatus);
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
			{
				return currentObject;
			}
			return currentObject.Owner;
		}

		/// <summary>
		/// We can only delete Texts in this view, not scripture sections.
		/// </summary>
		/// <returns></returns>
		protected override bool CanDelete()
		{
			if (CurrentObject is IWfiWordform)
			{
				return true;
			}
			return CurrentObject.Owner is IText;
		}

		public override void ReloadIfNeeded()
		{
			//// Push down to an overload of ConcordanceWordList
			//if (this is ConcordanceWordList)
			//{
			//	((ConcordanceWordList)this).RequestRefresh();
			//}
			base.ReloadIfNeeded();
		}

		public override bool OnRefresh(object sender)
		{
			//// Push down to an overload of ConcordanceWordList
			//if (this is ConcordanceWordList)
			//{
			//	((ConcordanceWordList)this).RequestRefresh();
			//}
			return base.OnRefresh(sender);
		}

		protected override void ReportCannotDelete()
		{
			var text = CurrentObject is IWfiWordform ? ITextStrings.ksCannotDeleteWordform : ITextStrings.ksCannotDeleteScripture;
			MessageBox.Show(PropertyTable.GetValue<Form>("window"), text, ITextStrings.ksError, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

		#endregion

		// TODO: Push down to ConcordanceWordList, since I believe that comment, below. It is called by RespellerDlg.
		/// <summary>
		/// This method should cause all paragraphs in interesting texts which do not have the ParseIsCurrent flag set
		/// to be Parsed. Created for use with ConcordanceWordList lists.
		/// </summary>
		internal void ParseInterstingTextsIfNeeded()
		{
			ForceReloadList();
		}

		internal bool AddTexts()
		{
			CheckDisposed();
			// get saved scripture choices
			var interestingTextsList = GetInterestingTextList();
			var interestingTexts = interestingTextsList.InterestingTexts.ToArray();

			using (var dlg = new FilterTextsDialog(PropertyTable.GetValue<IApp>("App"), Cache, interestingTexts, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				if (dlg.ShowDialog(PropertyTable.GetValue<Form>("window")) == DialogResult.OK)
				{
					interestingTextsList.SetInterestingTexts(dlg.GetListOfIncludedTexts());
					UpdateFilterStatusBarPanel();
				}
			}

			return true;
		}

		/// <summary>
		/// Get the list of currently selected Scripture section ids.
		/// </summary>
		/// <returns></returns>
		internal List<int> GetScriptureIds()
		{
			return (from st in GetInterestingTextList().ScriptureTexts select st.Hvo).ToList();
		}

		private InterestingTextList GetInterestingTextList()
		{
			return InterestingTextsDecorator.GetInterestingTextList(PropertyTable, Cache.ServiceLocator);
		}

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
			if (!IsActiveInGui)
			{
				return false;
			}
			return AddNewText(new UndoableCreateAndInsertStText(Cache, this, ITextStrings.UndoInsertText, ITextStrings.RedoInsertText));
		}

		/// <summary>
		/// Add a new text (but don't make it undoable)
		/// </summary>
		/// <returns></returns>
		internal bool AddNewTextNonUndoable()
		{
			return AddNewText(new NonUndoableCreateAndInsertStText(Cache, this));
		}

		private bool AddNewText(ICreateAndInsert<IStText> createAndInsertMethodObj)
		{
			// Get the default writing system for the new text.  See LT-6692.
			PrevTextWs = Cache.DefaultVernWs;
			if (CurrentObject != null && Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Count > 1)
			{
				PrevTextWs = WritingSystemServices.ActualWs(Cache, WritingSystemServices.kwsVernInParagraph, CurrentObject.Hvo, StTextTags.kflidParagraphs);
			}
			if (Filter != null)
			{
				// Tell the user we're turning off the filter, and then do it.
				MessageBox.Show(ITextStrings.ksTurningOffFilter, ITextStrings.ksNote, MessageBoxButtons.OK);
				Publisher.Publish("RemoveFilters", this);
				_activeMenuBarFilter = null;
			}
			SaveOnChangeRecord(); // commit any changes before we create a new text.
			var newText = DoCreateAndInsert(createAndInsertMethodObj);

			// Check to if a genre was assigned to this text
			// (when selected from the text list: ie a genre w/o a text was sellected)
			var property = GetCorrespondingPropertyName("DelayedGenreAssignment");
			var genreList = PropertyTable.GetValue<List<TreeNode>>(property, null);
			var ownerText = newText.Owner as IText;
			if (genreList != null && genreList.Count > 0 && ownerText != null)
			{
				foreach (var node in genreList)
				{
					ownerText.GenresRC.Add((ICmPossibility)node.Tag);
				}
				PropertyTable.RemoveProperty(property);
			}

			if (CurrentObject == null || CurrentObject.Hvo == 0)
			{
				return false;
			}
			var commands = new List<string>
			{
				"AboutToFollowLink",
				"FollowLink"
			};
			var parms = new List<object>
			{
				null,
				new FwLinkArgs(AreaServices.InterlinearEditMachineName, CurrentObject.Guid)
			};
			Publisher.Publish(commands, parms);
			// This is a workable alternative (where link is the one created above), but means this code has to know about the FwXApp class.
			//(FwXApp.App as FwXApp).OnIncomingLink(link);
			// This alternative does NOT work; it produces a deadlock...I think the remote code is waiting for the target app
			// to return to its message loop, but it never does, because it is the same app that is trying to send the link, so it is busy
			// waiting for 'Activate' to return!
			//link.Activate();
			return true;
		}

		/// <summary>
		/// Establish the writing system of the new text by filling its first paragraph with
		/// an empty string in the proper writing system.
		/// </summary>
		internal void CreateFirstParagraph(IStText stText, int wsText)
		{
			var txtPara = stText.AddNewTextPara(null);
			txtPara.Contents = TsStringUtils.MakeString(string.Empty, wsText);
		}

		internal int GetWsForNewText()
		{
			var wsText = PrevTextWs;
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
	}
}