using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The interesting texts list is responsible for maintaining a list of the texts that should be displayed
	/// in the Interlinear Texts tool and searched in the various concordance tools.
	/// Currently this is all the StTexts owned by Texts (that is, the main collection of interlinear texts
	/// in the language project), and selected sections of Scripture.
	/// The list is persisted in the mediator's property table, being thus specific to one user, and possibly
	/// a particular window.
	/// It implements IVwNotifyChange and updates the list when various relevant properties change.
	/// </summary>
	public class InterestingTextList : IVwNotifyChange
	{
		private readonly ITextRepository m_textRepository;
		private readonly IStTextRepository m_stTextRepository;
		private readonly PropertyTable m_propertyTable;
		public const string PersistPropertyName = "InterestingScriptureTexts";
		public const string ExcludeCoreTextPropertyName = "ExcludedCoreTexts";
		/// <summary>
		/// This is kept containing the same items as InterestingTexts, as a set so membership can be
		/// tested efficiently.
		/// </summary>
		private HashSet<IStText> m_interestingTests;
		/// <summary>
		/// These two RecordClerks both need to respond to InterestingTextList changes EVEN when not loaded.
		/// (LT-13217)
		/// So if one changes this list of texts, the other's sort sequence file will be deleted.
		/// </summary>
		private static string[] RelatedClerkIds = { "interlinearTexts", "concordanceWords" };

		/// <summary>
		/// Used by InvalidateRelatedSortSequences()
		/// </summary>
		public FdoCache Cache { get; set; }

		public InterestingTextList(PropertyTable propertyTable, ITextRepository repo, IStTextRepository stTextRepo)
			: this(propertyTable, repo, stTextRepo, true)
		{
		}

		public InterestingTextList(PropertyTable propertyTable, ITextRepository repo,
			IStTextRepository stTextRepo, bool includeScripture)
		{
			m_textRepository = repo;
			m_propertyTable = propertyTable;
			m_stTextRepository = stTextRepo;
			CoreTexts = GetCoreTexts();
			m_scriptureTexts = GetScriptureTexts();
			IncludeScripture = includeScripture;
			GetCache();
		}

		private void GetCache()
		{
			if (m_propertyTable == null)
				return;
			Cache = (FdoCache)m_propertyTable.GetValue("cache");
		}

		private List<IStText> m_coreTexts;
		public List<IStText> CoreTexts
		{

			get
			{
				if (m_coreTexts == null)
					m_coreTexts = GetCoreTexts();
				return m_coreTexts;
			}
			set { m_coreTexts = value; }
		}
		private List<IStText> m_scriptureTexts;

		public bool IncludeScripture { get; private set; }

		/// <summary>
		/// Get the "core" (non-scripture) texts that we want to display. This is all the ones not on the excluded list.
		/// </summary>
		/// <returns></returns>
		private List<IStText> GetCoreTexts()
		{
			var result = AllCoreTexts.ToList();
			if (m_propertyTable == null)
				return result;
			var excludedGuids = ExcludedCoreTextIdList();
			if (excludedGuids.Count == 0)
				return result;
			return (from obj in result where !excludedGuids.Contains(obj.Guid) select obj).ToList();
		}

		/// <summary>
		/// True if all (non-scripture) texts are published
		/// </summary>
		public bool AllCoreTextsAreIncluded
		{
			get { return ExcludedCoreTextIdList().Count == 0; }
		}

		private HashSet<Guid> ExcludedCoreTextIdList()
		{
			var idList = m_propertyTable.GetStringProperty(ExcludeCoreTextPropertyName, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			var excludedGuids = new HashSet<Guid>();
			foreach (string id in idList)
			{
				Guid guid;
				try
				{
					guid = new Guid(Convert.FromBase64String(id));
				}
				catch (FormatException)
				{
					// Just ignore this one. (I'd like to Assert, but a unit test verifies we can handle garbage).
					Debug.WriteLine(PersistPropertyName + "contains invalid guid " + id);
					continue;
				}
				excludedGuids.Add(guid);
			}
			return excludedGuids;
		}

		/// <summary>
		/// The core (non-Scripture) texts that might be selected to display and concord.
		/// </summary>
		public IEnumerable<IStText> AllCoreTexts
		{
			get
			{
				return from text in m_textRepository.AllInstances()
					where text.ContentsOA != null
					select text.ContentsOA;
			}
		}

		private List<IStText> GetScriptureTexts()
		{
			var result = new List<IStText>();
			if (m_propertyTable == null)
				return result;
			var idList = m_propertyTable.GetStringProperty(PersistPropertyName, "");
			foreach (string id in idList.Split(','))
			{
				if (id.Length == 0)
					continue; // we get one empty string even from splitting an empty one.
				Guid guid;
				try
				{
					guid = new Guid(Convert.FromBase64String(id));
				}
				catch (FormatException)
				{
					// Just ignore this one. (I'd like to Assert, but a unit test verifies we can handle garbage).
					Debug.WriteLine(PersistPropertyName + "contains invalid guid " + id);
					continue;
				}
				IStText item;
				if (m_stTextRepository.TryGetObject(guid, out item))
					result.Add(m_stTextRepository.GetObject(guid));
				// An invalid item is not an error, it may just have been deleted while the interesting
				// text list was not monitoring things.
			}
			return result;
		}

		public bool IsInterestingText(IStText text)
		{
			if (m_interestingTests == null)
				m_interestingTests = new HashSet<IStText>(InterestingTexts);
			return m_interestingTests.Contains(text);
		}

		/// <summary>
		/// Virtual for testing
		/// </summary>
		public virtual IEnumerable<IStText> InterestingTexts
		{
			get
			{
				foreach (var st in CoreTexts)
					yield return st;
				if (IncludeScripture)
				{
					foreach (var st in m_scriptureTexts)
						yield return st;
				}
			}
		}

		/// <summary>
		/// The subset of Scripture that we currently want to include (saved as part of project properties).
		/// </summary>
		public IEnumerable<IStText> ScriptureTexts
		{
			get { return m_scriptureTexts.ToArray(); }
		}

		public event EventHandler<InterestingTextsChangedArgs> InterestingTextsChanged;

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			switch (tag)
			{
				case TextTags.kflidContents:
					if (cvIns > 0 && cvDel == 0)
					{
						var text = m_textRepository.GetObject(hvo);
						CoreTexts.Add(text.ContentsOA);
						if (m_interestingTests != null)
							m_interestingTests.Add(text.ContentsOA);
						RaiseInterestingTextsChanged(CoreTexts.Count - 1, 1, 0);
					}
					else if (cvIns == 1 && cvDel == 1)
					{
						ClearInvalidObjects(CoreTexts, 0, false); // get rid of the old one but do NOT raise notification.
						var text = m_textRepository.GetObject(hvo);
						CoreTexts.Add(text.ContentsOA);
						if (m_interestingTests != null)
							m_interestingTests.Add(text.ContentsOA);
						// We don't know where the old one was removed, safest to treat as changing all.
						RaiseInterestingTextsChanged(0, CoreTexts.Count, CoreTexts.Count);
					}
					else
					{
						// We could try getting the text and removing its ContentsOA from the list,
						// but that assumes a lot about the implementation of deleting objects,
						// such as that when a Text is deleted, it is still present in its repository
						// at the moment we clear ContentsOA. I think it's safest to do something generic.
						ClearInvalidObjects(CoreTexts, 0, true);
					}
					break;
				case LangProjectTags.kflidTexts:
				case RnGenericRecTags.kflidText:
					if (cvIns > 0)
					{
						// Need to add the new text(s). Have to find which ones to add.
						var coreTextsSet = new HashSet<IStText>(CoreTexts);
						int count = 0;
						foreach (var newText in (from sttext in GetCoreTexts() where !coreTextsSet.Contains(sttext) select sttext))
						{
							count++;
							CoreTexts.Add(newText);
							if (m_interestingTests != null)
								m_interestingTests.Add(newText);
						}
						RaiseInterestingTextsChanged(CoreTexts.Count - count, count, 0);
					}
					ClearInvalidObjects(CoreTexts, 0, true);
					break;
				case ScrSectionTags.kflidHeading:
				case ScrSectionTags.kflidContent:
				case ScrBookTags.kflidSections:
				case ScriptureTags.kflidScriptureBooks:
				case ScrBookTags.kflidTitle:
				case ScrBookTags.kflidFootnotes:
					if (cvDel > 0)
					{
						if (ClearInvalidObjects(m_scriptureTexts, CoreTexts.Count, IncludeScripture))
							if (m_propertyTable != null && !m_propertyTable.IsDisposed)
								UpdatePropertyTable();
					}
					break;
			}
		}

		private void RaiseInterestingTextsChanged(int insertAt, int inserted, int deleted)
		{
			if (inserted == 0 && deleted == 0)
				return;
			InvalidateRelatedSortSequences();
			if (InterestingTextsChanged != null)
				InterestingTextsChanged(this, new InterestingTextsChangedArgs(insertAt, inserted, deleted));
		}

		private void InvalidateRelatedSortSequences()
		{
			if (Cache == null)
				return;

			// We won't keep track of the clerk between calls since it could change from time to time.
			var clerk = m_propertyTable.GetValue("ActiveClerk", null) as RecordClerk;
			if (clerk == null)
				return;

			if (!RelatedClerkIds.Contains(clerk.Id))
			{
				Debug.Fail("We may need to add a new RelatedClerkId.");
				return; // somehow we got in here with the wrong clerk?!
			}
			var otherRelatedClerkIds = GetRelatedClerkIds(clerk.Id);
			foreach (var clerkId in otherRelatedClerkIds)
			{
				RemoveSortSequenceFile(RecordView.GetSortFilePersistPathname(Cache, clerkId));
			}
		}

		private void RemoveSortSequenceFile(string filename)
		{
			FileUtils.Delete(filename);
		}

		private static IEnumerable<string> GetRelatedClerkIds(string id)
		{
			return RelatedClerkIds.Where(clerkId => clerkId != id);
		}

		//Remove invalid objects from the list. Return true if any were removed.
		private bool ClearInvalidObjects(List<IStText> listToSearch, int offset, bool raiseChangeNotification)
		{
			bool didRemoveAny = false;
			for (int i = listToSearch.Count - 1; i >= 0; i--)
			{
				if (!listToSearch[i].IsValidObject || listToSearch[i].OwnerOfClass(ScrDraftTags.kClassId) != null)
				{
					// Enhance JohnT: if several are removed, especially close together, we might want
					// to combine the change notifications. However I think this will be quite unusual.
					if (m_interestingTests != null)
						m_interestingTests.Remove(listToSearch[i]);
					listToSearch.RemoveAt(i);
					if (raiseChangeNotification)
						RaiseInterestingTextsChanged(i + offset, 0, 1);
					didRemoveAny = true;
				}
			}
			return didRemoveAny;
		}

		/// <summary>
		/// Make a string that corresponds to a list of objects.
		/// </summary>
		public static string MakeIdList(IEnumerable<ICmObject> objects)
		{
			return objects.ToString(",", obj => Convert.ToBase64String(obj.Guid.ToByteArray()));
		}
		/// <summary>
		/// Make a string that corresponds to a list of guids.
		/// </summary>
		public static string MakeIdList(IEnumerable<Guid> objects)
		{
			return objects.ToString(",", guid => Convert.ToBase64String(guid.ToByteArray()));
		}

		/// <summary>
		/// This routine is where InterlinMaster sends the results of running the IFilterTextsDialog.
		/// The list may include regular as well as scripture texts.
		/// We persist them separately because Scripture is excluded by default and regular texts are included by default.
		/// Thus, storing a list of the INCLUDED scripture means that originally, or if we clear all saved settings,
		/// NO scripture is included, and any newly created Scripture is also excluded until manually added.
		/// Storing a lis of EXCLUDED regular texts means that originally, or if we clear all saved settings,
		/// ALL regular texts are included, and if we add a new one, it is automatically included.
		/// </summary>
		/// <param name="stTexts"></param>
		public void SetInterestingTexts(IEnumerable<IStText> stTexts)
		{
			var oldTexts = InterestingTexts.ToArray();
			m_scriptureTexts = new List<IStText>();
			var excludedGuids = new HashSet<Guid>(from obj in AllCoreTexts select obj.Guid);
			foreach (var obj in stTexts)
			{
				if (obj.Owner is IText)
					excludedGuids.Remove(obj.Guid);
				else
					m_scriptureTexts.Add(obj);
			}
			UpdatePropertyTable();
			UpdateExcludedCoreTexts(excludedGuids);
			m_coreTexts = null;
			m_interestingTests = null; // regenerate when next needed. (Before we raise changed, which may use it...)
			var newTexts = InterestingTexts.ToArray();
			int firstChange = 0;
			int minLength = Math.Min(oldTexts.Length, newTexts.Length);
			while (firstChange < minLength && newTexts[firstChange] == oldTexts[firstChange])
				firstChange++;
			int endMatchCount = 0;
			while (endMatchCount < minLength - firstChange && newTexts[newTexts.Length - endMatchCount - 1] == oldTexts[oldTexts.Length - endMatchCount - 1])
				endMatchCount++;
			// Enhance JohnT: could look for unchanged items in the list. But this is fairly rare,
			// typically when someone runs the configure dialog and OKs it.
			RaiseInterestingTextsChanged(firstChange, newTexts.Length - firstChange - endMatchCount, oldTexts.Length - firstChange - endMatchCount);
		}

		private void UpdateExcludedCoreTexts(HashSet<Guid> excludedGuids)
		{
			m_propertyTable.SetProperty(ExcludeCoreTextPropertyName, MakeIdList(excludedGuids));
		}

		private void UpdatePropertyTable()
		{
			SetScriptureTextsInPropertyTable(m_propertyTable, m_scriptureTexts);
		}

		/// <summary>
		/// Store in the property table what needs to be there so that we will use the specified set of scripture
		/// texts as 'interesting'.
		/// </summary>
		/// <param name="propertyTable"></param>
		public static void SetScriptureTextsInPropertyTable(PropertyTable propertyTable, IEnumerable<IStText> texts)
		{
			propertyTable.SetProperty(PersistPropertyName, MakeIdList(texts.Cast<ICmObject>()));
		}

		/// <summary>
		/// This is invoked when TE (or some other program) invokes a link, typically to a Scripture Section text not in our filter.
		/// If possible, add it to the filter and return true. Also add any other sections in the same chapter.
		/// Also when jumping from Notebook to a text that is excluded.
		/// Todo JohnT: get it called from TE and test it; not currently used by TE (ported from parts of old InterlinearTextsVirtualHandler)
		/// </summary>
		public bool AddChapterToInterestingTexts(IStText newText)
		{
			int oldCount = m_scriptureTexts.Count;
			int targetPosition = TextPosition(newText);
			if (targetPosition < 0)
			{
				var excludedCoreTextIdList = ExcludedCoreTextIdList();
				if (newText.Owner is IText && excludedCoreTextIdList.Contains(newText.Guid))
				{
					CoreTexts.Add(newText);
					if (m_interestingTests != null)
						m_interestingTests.Add(newText);
					excludedCoreTextIdList.Remove(newText.Guid);
					UpdateExcludedCoreTexts(excludedCoreTextIdList);
					RaiseInterestingTextsChanged(CoreTexts.Count - 1, 1, 0);
					return true; // added sucessfully
				}
				return false; // not a text in current Scripture.
			}
			int index;
			for (index = 0; index < m_scriptureTexts.Count; index++)
			{
				if (TextPosition(m_scriptureTexts[index]) > targetPosition)
				{
					break;
				}
			}
			m_scriptureTexts.Insert(index, newText);
			// Also insert the other text in the same section
			var sec = newText.Owner as IScrSection;
			if (sec != null) // not a book title
			{
				if (newText == sec.ContentOA && sec.HeadingOA != null)
				{
					if (index == 0 || m_scriptureTexts[index - 1] != sec.HeadingOA)
						m_scriptureTexts.Insert(index, sec.HeadingOA);
					else
						index--; // move index to point at heading
				}
				else if (sec.ContentOA != null)
				{
					if (index >= m_scriptureTexts.Count - 1 || m_scriptureTexts[index + 1] != sec.ContentOA)
						m_scriptureTexts.Insert(index + 1, sec.ContentOA);
				}
				// At this point the heading and contents of the section for the inserted text
				// are at index. We look for adjacent sections in the same chapter and if necessary
				// add them too.
				int indexAfter = index + 1;
				if (sec.ContentOA != null && sec.HeadingOA != null)
					indexAfter++;
				// It would be nicer to use ScrReference, but not worth adding a whole project reference.
				int chapMax = sec.VerseRefMax / 1000;
				int chapMin = sec.VerseRefMin / 1000;
				var book = (IScrBook)sec.Owner;
				int csec = book.SectionsOS.Count;
				int isecCur = sec.IndexInOwner;
				for (int isec = isecCur + 1; isec < csec; isec++)
				{
					IScrSection secNext = book.SectionsOS[isec];
					if (secNext.VerseRefMin / 1000 != chapMax)
						break; // different chapter.
					indexAfter = AddAfter(indexAfter, secNext.HeadingOA);
					indexAfter = AddAfter(indexAfter, secNext.ContentOA);
				}
				for (int isec = isecCur - 1; isec >= 0; isec--)
				{
					IScrSection secPrev = book.SectionsOS[isec];
					if (secPrev.VerseRefMax / 1000 != chapMin)
						break;
					index = AddBefore(index, secPrev.ContentOA);
					index = AddBefore(index, secPrev.HeadingOA);
				}
			}
			// We could get fancy and try to figure the exact range that changed, but this is close enough.
			RaiseInterestingTextsChanged(CoreTexts.Count, m_scriptureTexts.Count, oldCount);
			return true;
		}

		private int AddBefore(int index, IStText item)
		{
			if (item == null)
				return index; // nothing to add
			if (index == 0 || m_scriptureTexts[index - 1] != item)
			{
				// Not present, add it.
				m_scriptureTexts.Insert(index, item);
				return index; // no change, things moved up.
			}
			return index - 1; // next earlier item goes before one already present.
		}

		private int AddAfter(int indexAfter, IStText item)
		{
			if (item == null)
				return indexAfter; // nothing to add
			if (indexAfter >= m_scriptureTexts.Count - 1 || m_scriptureTexts[indexAfter] != item)
			{
				// Not already present, add it.
				m_scriptureTexts.Insert(indexAfter, item);
			}
			return indexAfter + 1; // in either case next text goes after this one.
		}

		/// <summary>
		/// Return an index we can use to order StTexts in Scripture.
		/// Take the book index * 10,000.
		/// if not in the title, add (section index + 1)*2.
		/// If in contents add 1.
		/// </summary>
		/// <param name="hvoText"></param>
		/// <returns></returns>
		int TextPosition(IStText text)
		{
			ICmObject owner = text.Owner;
			int flid = text.OwningFlid;
			if (flid != ScrSectionTags.kflidContent &&
				flid != ScrSectionTags.kflidHeading
				&& flid != ScrBookTags.kflidTitle)
			{
				return -1;
			}
			if (flid == ScrBookTags.kflidTitle)
				return BookPosition((IScrBook)owner);
			var section = (IScrSection)owner;
			var book = (IScrBook)section.Owner;
			return BookPosition(book)
				   + section.IndexInOwner * 2 + 2
				   + (flid == ScrSectionTags.kflidContent ? 1 : 0);
		}

		private int BookPosition(IScrBook book)
		{
			return book.IndexInOwner * 10000;
		}
	}

	public class InterestingTextsChangedArgs : EventArgs
	{
		public InterestingTextsChangedArgs(int insertAt, int inserted, int deleted)
		{
			InsertedAt = insertAt;
			NumberInserted = inserted;
			NumberDeleted = deleted;
		}
		public int InsertedAt { get; private set; }
		public int NumberInserted { get; private set; }
		public int NumberDeleted { get; private set; }
	}
}
