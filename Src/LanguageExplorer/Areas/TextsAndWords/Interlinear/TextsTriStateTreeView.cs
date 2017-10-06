// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.FieldWorks.Language;
using SIL.FieldWorks.Resources;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// TriStateTreeView that knows how to load scripture from the cache.
	/// </summary>
	public class TextsTriStateTreeView : TriStateTreeView
	{
		private LcmCache m_cache;
		private LcmStyleSheet m_scriptureStylesheet;
		private IScripture m_scr;
		private IScrText m_associatedPtText;
		internal const string ksDummyName = "dummy"; // used for Name of dummy nodes.

		/// <summary>
		/// Make one.
		/// </summary>
		public TextsTriStateTreeView()
		{
			BeforeExpand += TriStateTreeView_BeforeExpand;
		}

		/// <summary>
		/// Set the cache.
		/// </summary>
		public LcmCache Cache
		{
			set
			{
				m_cache = value;
				m_associatedPtText = ParatextHelper.GetAssociatedProject(m_cache.ProjectId);
				m_scr = m_cache.LanguageProject.TranslatedScriptureOA;
				if (m_scr == null && m_associatedPtText != null)
				{
					m_scr = m_cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
				}
				if (m_scr == null)
				{
					return;
				}
				m_scriptureStylesheet = new LcmStyleSheet();
				m_scriptureStylesheet.Init(m_cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			}
		}

		/// <summary>
		/// Get/Set the FW application.
		/// </summary>
		public IApp App { private get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the non-Scripture texts.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadGeneralTexts()
		{
			var tnTexts = LoadTextsByGenreAndWithoutGenre();
			if (tnTexts == null || tnTexts.Nodes.Count == 0)
			{
				return;
			}
			foreach (TreeNode textCat in tnTexts.Nodes)
			{
				Nodes.Add(textCat);
			}
		}

		/// <summary>
		/// Loads the texts for each Scripture book title, section, footnote, etc.
		/// </summary>
		private void LoadScriptureTexts()
		{
			if (!m_cache.ServiceLocator.GetInstance<IScrBookRepository>().AllInstances().Any())
			{
				return; // Nobody home, so skip them.
			}
			var otBooks = new List<TreeNode>();
			var ntBooks = new List<TreeNode>();
			for (var bookNum = 1; bookNum <= BCVRef.LastBook; bookNum++)
			{
				var bookName = m_cache.ServiceLocator.GetInstance<IScrRefSystemRepository>().Singleton.BooksOS[bookNum - 1].UIBookName;
				object book = m_scr.FindBook(bookNum);
				if (book == null)
				{
					if (m_associatedPtText != null && m_associatedPtText.BookPresent(bookNum))
					{
						book = bookNum;
					}
					else
					{
						continue;
					}
				}

				var node = new TreeNode(bookName)
				{
					Tag = book,
					Name = "Book"
				};
				// help us query for books.
				if (bookNum < ScriptureTags.kiNtMin)
				{
					otBooks.Add(node);
				}
				else
				{
					ntBooks.Add(node);
				}
			}

			var bibleNode = new TreeNode(ITextStrings.kstidBibleNode)
			{
				Name = "Bible"
			};
			if (otBooks.Count > 0)
			{
				var testamentNode = new TreeNode(ITextStrings.kstidOtNode, otBooks.ToArray())
				{
					Name = "Testament"
				};
				// help us query for Testaments
				bibleNode.Nodes.Add(testamentNode);
			}

			if (ntBooks.Count > 0)
			{
				var testamentNode = new TreeNode(ITextStrings.kstidNtNode, ntBooks.ToArray())
				{
					Name = "Testament"
				};
				// help us query for Testaments
				bibleNode.Nodes.Add(testamentNode);
			}
			Nodes.Add(bibleNode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load texts by Genre into the texts tree view.
		/// </summary>
		/// <returns>A control tree of the Texts in the project</returns>
		/// ------------------------------------------------------------------------------------
		private TreeNode LoadTextsByGenreAndWithoutGenre()
		{
			if (m_cache.LanguageProject.GenreListOA == null) return null;
			var genreList = m_cache.LanguageProject.GenreListOA.PossibilitiesOS;
			Debug.Assert(genreList != null);
			var allTexts = m_cache.ServiceLocator.GetInstance<ITextRepository>().AllInstances();
			if (allTexts == null)
			{
				return null;
			}

			// Title node for all texts, Biblical and otherwise
			var textsNode = new TreeNode("All Texts in Genres and not in Genres")
			{
				Name = "Texts"
			};

			// For each genre, find the texts that claim it
			LoadTextsFromGenres(textsNode, genreList, allTexts);

			var textsWithNoGenre = new List<TreeNode>(); // and get the ones with no genre
			// LT-12179: Create a List for collecting selected tree nodes which we will later sort
			// before actually adding them to the tree:
			var foundFirstText = false;
			// Create a collator ready for sorting:
			var collator = new ManagedLgIcuCollator();

			foreach (var tex in allTexts)
			{
				if (tex.GenresRC.Any())
				{
					continue;
				}
				var texItem = new TreeNode(tex.ChooserNameTS.Text)
				{
					Tag = tex.ContentsOA,
					Name = "Text"
				};
				textsWithNoGenre.Add(texItem);

				// LT-12179: If this is the first tex we've added, establish the collator's details
				// according to the writing system at the start of the tex:
				if (foundFirstText)
				{
					continue;
				}
				foundFirstText = true;
				var ws1 = tex.ChooserNameTS.get_WritingSystemAt(0);
				var wsEngine = m_cache.WritingSystemFactory.get_EngineOrNull(ws1);
				collator.Open(wsEngine.Id);
			}

			if (!textsWithNoGenre.Any())
			{
				return textsNode;
			}
			// LT-12179: Order the TreeNodes alphabetically:
			textsWithNoGenre.Sort((x, y) => collator.Compare(x.Text, y.Text, LgCollatingOptions.fcoIgnoreCase));
			// Make a TreeNode for the texts with no known genre
			var woGenreTreeNode = new TreeNode("No Genre", textsWithNoGenre.ToArray())
				{
					Name = "TextsWoGenre"
				};
			textsNode.Nodes.Add(woGenreTreeNode);
			return textsNode;
		}

		/// <summary>
		/// Creates a TreeNode tree along genres populated by the texts that claim them.
		/// The list of textsWithNoGenre is also populated.
		/// Recursively descend the genre tree and duplicate parts that have corresponding texts.
		/// </summary>
		/// <param name="parent">The parent to attach the genres to. If null, nothing is done.</param>
		/// <param name="genreList">The owning sequence of genres - its a tree.</param>
		/// <param name="allTexts">The flat list of all texts in the project.</param>
		private static void LoadTextsFromGenres(TreeNode parent, ILcmOwningSequence<ICmPossibility> genreList, IEnumerable<IText> allTexts)
		{
			if (parent == null)
			{
				return;
			}
			var sortedGenreList = new List<ICmPossibility>(genreList);
			var sorter = new CmPossibilitySorter();
			sortedGenreList.Sort(sorter);
			foreach (var gen in sortedGenreList)
			{
				// This tree node is added to genreTreeNodes if there are texts or children
				var genItem = new TreeNode(gen.ChooserNameTS.Text);

				// LT-12179: Create a List for collecting selected tree nodes which we will later sort
				// before actually adding them to the tree:
				var sortedNodes = new List<TreeNode>();
				var foundFirstText = false;
				// Create a collator ready for sorting:
				var collator = new ManagedLgIcuCollator();

				foreach (var tex in allTexts)
				{   // This tex may not have a genre or it may claim to be in more than one
					if (!Enumerable.Contains(tex.GenresRC, gen))
					{
						continue;
					}
					var texItem = new TreeNode(tex.ChooserNameTS.Text)
					{
						Tag = tex.ContentsOA,
						Name = "Text"
					};

					// LT-12179: Add the new TreeNode to the (not-yet-)sorted list:
					sortedNodes.Add(texItem);

					// LT-12179: If this is the first tex we've added, establish the collator's details
					// according to the writing system at the start of the tex:
					if (foundFirstText)
					{
						continue;
					}
					foundFirstText = true;
					var ws1 = tex.ChooserNameTS.get_WritingSystemAt(0);
					var wsEngine = gen.Cache.WritingSystemFactory.get_EngineOrNull(ws1);
					collator.Open(wsEngine.Id);
				}

				// LT-12179:
				if (foundFirstText)
				{
					// Order the TreeNodes alphabetically:
					sortedNodes.Sort((x, y) => collator.Compare(x.Text, y.Text, LgCollatingOptions.fcoIgnoreCase));
					// Add the TreeNodes to the tree:
					genItem.Nodes.AddRange(sortedNodes.ToArray());
				}

				if (gen.SubPossibilitiesOS.Count > 0)
				{   // Descend to the child genres regardless if there were texts assigned to this genre
					LoadTextsFromGenres(genItem, gen.SubPossibilitiesOS, allTexts);
				}

				//Add the node even if there are no texts that point to this genre.
				genItem.Tag = gen;  // ICmPossibility
				genItem.Name = "Genre";
				parent.Nodes.Add(genItem);
			}
		}

		// Class to sort the Genre's before they are displayed.
		private class CmPossibilitySorter : IComparer<ICmPossibility>
		{
			internal CmPossibilitySorter()
			{
			}

			#region IComparer<T> Members

			public int Compare(ICmPossibility x, ICmPossibility y)
			{
				var xString = x.ChooserNameTS.Text;
				var yString = y.ChooserNameTS.Text;
				return xString.CompareTo(yString);
			}

			#endregion
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Since we're being lazy about filling in sections and footnotes, we need to do it for
		/// checked books when obtaining their children.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void FillInMissingChildren(TreeNode bookNode)
		{
			FillInChildren(bookNode, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If using lazy initialization, fill this in to make sure the relevant part of the
		/// tree is built so that the specified tag can be checked.
		/// </summary>
		/// <param name="tag">The object that better be the Tag of some node </param>
		/// ------------------------------------------------------------------------------------
		protected override void FillInIfHidden(object tag)
		{
			if (!(tag is ICmObject))
			{
				return;
			}
			var book = ((ICmObject)tag).OwnerOfClass<IScrBook>();
			if (book != null)
			{
				FillInChildren(FindNode(Nodes, book), false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the node for the specified book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static TreeNode FindNode(IEnumerable nodes, IScrBook book)
		{
			foreach (TreeNode node in nodes)
			{
				if (node.Tag != null && (node.Tag.Equals(book.CanonicalNum) || node.Tag.Equals(book)))
				{
					return node;
				}
				var result = FindNode(node.Nodes, book);
				if (result != null)
				{
					return result;
				}
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the in children.
		/// </summary>
		/// <param name="bookNode">The book node. This can (rarely) be null, in which case, this
		/// method won't do anything. Caller needs to be able to deal with that possibility.
		/// See FWR-3498 for details.</param>
		/// <param name="checkChildren">if set to <c>true</c> all child nodes will be selected.</param>
		/// <returns><c>true</c> if the given node has real child node(s) (after filling them in).</returns>
		/// ------------------------------------------------------------------------------------
		private bool FillInChildren(TreeNode bookNode, bool checkChildren)
		{
			if (bookNode == null)
			{
				return false;
			}
			if ((!(bookNode.Tag is IScrBook) && !(bookNode.Tag is int)) || bookNode.Nodes.Count != 1 || bookNode.Nodes[0].Name != ksDummyName)
			{
				return true;
			}
			BeforeExpand -= TriStateTreeView_BeforeExpand; // prevent recursion
			var retval = FillInBookChildren(bookNode);
			if (retval && checkChildren)
			{
				CheckAllChildren(bookNode);
			}
			BeforeExpand += TriStateTreeView_BeforeExpand;
			return retval;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects all the children of the given node (recursively).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckAllChildren(TreeNode node)
		{
			foreach (TreeNode child in node.Nodes)
			{
				SetChecked(child, CheckState.Checked);
				CheckAllChildren(child);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the BeforeExpand event of the TextsTriStateTreeView control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.TreeViewCancelEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void TriStateTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			e.Cancel = !FillInChildren(e.Node, false);
		}

		/// <summary>
		/// Load all texts, including Scripture texts, if present.
		/// </summary>
		public void LoadAllTexts()
		{
			// first load the book ids.
			Nodes.Clear();
			LoadGeneralTexts();
			LoadScriptureTexts();

			if (Nodes.Count == 0)
			{
				return;
			}

			//This requires the Bible node be loaded last.
			var bibleNode = Nodes[Nodes.Count-1];
			//If there was no Bible node loaded then just return. This might be the case for the SE version of FLEx.
			if (bibleNode.Name != "Bible")
			{
				return;
			}

			if (bibleNode.Nodes.Count == 0)
			{
				// There are no Bible book nodes, but we don't need to crash or show the Bible node.
				bibleNode.Remove();
				return;
			}

			foreach (TreeNode testament in bibleNode.Nodes)
			{
				foreach (TreeNode bookNode in testament.Nodes)
				{
					// Put a dummy node into each book so the computer thinks it can be expanded.
					// When it is, we will replace this with the real children.
					var node = new TreeNode(ksDummyName)
					{
						Name = ksDummyName
					};
					bookNode.Nodes.Add(node);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the children for a particular book. This is typically done when it gets
		/// expanded or when we need a list including the children. Separating it from the
		/// initial load saves a lot of time when we have a long list of books.
		/// </summary>
		/// <param name="bookNode">The book node.</param>
		/// <returns><c>true</c> if the dummy node was replaced by real child node(s)</returns>
		/// <remarks>protected virtual for unit tests</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual bool FillInBookChildren(TreeNode bookNode)
		{
			var book = bookNode.Tag as IScrBook;
			var bookNum = book?.CanonicalNum ?? (int)bookNode.Tag;
			var owningForm = FindForm();
			while (owningForm != null && !owningForm.Visible)
			{
				owningForm = owningForm.Owner;
			}
			if (owningForm == null)
			{
				owningForm = Form.ActiveForm ?? Application.OpenForms[0];
			}

			if (m_associatedPtText != null)
			{
				// Update main text, if possible/needed.
				if (m_associatedPtText.BookPresent(bookNum))
				{
					// PT has it.
					// If we don't have it, OR if our copy is stale, then get updated copy.
					if (book == null || !m_associatedPtText.IsCheckSumCurrent(bookNum, book.ImportedCheckSum))
					{
						// Get new/fresh version from PT.
						var importedBook = ImportBook(owningForm, bookNum);
						if (importedBook != null)
						{
							book = importedBook;
							bookNode.Tag = importedBook;
						}
					}
				}
				if (book == null)
				{
					// No book, so don't fret about a back translation
					return false;
				}

				// Update back translation
				IScrText btProject = ParatextHelper.GetBtsForProject(m_associatedPtText).FirstOrDefault();
				if (btProject != null && btProject.BookPresent(book.CanonicalNum) && !btProject.IsCheckSumCurrent(book.CanonicalNum, book.ImportedBtCheckSum.get_String(book.Cache.DefaultAnalWs).Text))
				{
					// The BT for this book node is out-of-date with the Paratext BT data
					ImportBackTranslation(owningForm, bookNum, btProject);
				}
			}

			bookNode.Nodes.Clear(); // Gets rid of dummy.
			// Add Title node.
			if (book.TitleOA != null)
			{
				var titleNode = new TreeNode(ResourceHelper.GetResourceString("kstidScriptureTitle"))
				{
					Name = book.TitleOA.ToString(),
					Tag = book.TitleOA
				};
				bookNode.Nodes.Add(titleNode);
			}

			// Add Sections.
			foreach (var section in book.SectionsOS)
			{
				var chapterVerseBridge = m_scr.ChapterVerseBridgeAsString(section);
				// Include the heading text if it's not empty.  See LT-8764.
				var cTotal = section.HeadingOA?.ParagraphsOS.Cast<IScrTxtPara>().Sum(para => para.Contents.Length);
				if (cTotal > 0)
				{
					var sFmt = ResourceHelper.GetResourceString("kstidSectionHeading");
					var node = new TreeNode(string.Format(sFmt, chapterVerseBridge))
					{
						Name = string.Format(sFmt, section),
						Tag = section.HeadingOA // expect an StText
					};
					bookNode.Nodes.Add(node);
				}
				var sectionNode = new TreeNode(chapterVerseBridge)
				{
					Name = section.ToString(),
					Tag = section.ContentOA // expect an StText
				};
				bookNode.Nodes.Add(sectionNode);
			}

			// Add Footnotes in reverse order, so we can insert them in the proper order.
			var footnotes = new List<IScrFootnote>(book.FootnotesOS);
			footnotes.Reverse();
			foreach (var scrFootnote in footnotes)
			{
				// insert under the relevant section, if any (LTB-408)
				IScrSection containingSection;
				IStText containingTitle = null;
				if (!scrFootnote.TryGetContainingSection(out containingSection) && !scrFootnote.TryGetContainingTitle(out containingTitle))
				{
					continue;
				}
				var nodeName = m_scr.ContainingRefAsString(scrFootnote);
				var footnoteNode = new TreeNode(nodeName)
				{
					Tag = scrFootnote,
					Name = "Footnote"
				};

				// see if we can lookup the node of this section.
				var nodeIndex = bookNode.Nodes.IndexOfKey(containingSection?.ToString() ?? containingTitle.ToString());
				if (nodeIndex >= 0)
				{
					bookNode.Nodes.Insert(nodeIndex + 1, footnoteNode);
				}
				else
				{
					bookNode.Nodes.Add(footnoteNode);	// insert at end.
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expand nodes to book level
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ExpandToBooks()
		{
			BeforeExpand -= TriStateTreeView_BeforeExpand; // Prevent loading Books before the user expands them
			ExpandAll();
			BeforeExpand += TriStateTreeView_BeforeExpand;
			// Collapse anything below book level
			foreach (var node in Nodes.Find("Book", true))
			{
				node.Collapse();
			}
			foreach (var node in Nodes.Find("Genre", true))
			{
				node.Collapse();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Imports the specified book.
		/// </summary>
		/// <param name="owningForm">Form that can be used as the owner of progress dialogs and
		/// message boxes.</param>
		/// <param name="bookNum">The canonical book number.</param>
		/// <returns>
		/// The ScrBook created to hold the imported data
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private IScrBook ImportBook(Form owningForm, int bookNum)
		{
			IScrImportSet importSettings = null;
			var haveSomethingToImport = NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				importSettings = m_scr.FindOrCreateDefaultImportSettings(TypeOfImport.Paratext6, m_scriptureStylesheet, FwDirectoryFinder.TeStylesPath);
				importSettings.ParatextScrProj = m_associatedPtText.Name;
				importSettings.StartRef = new BCVRef(bookNum, 0, 0);
				var chapter = m_associatedPtText.Versification.LastChapter(bookNum);
				importSettings.EndRef = new BCVRef(bookNum, chapter, m_associatedPtText.Versification.LastVerse(bookNum, chapter));
				importSettings.ImportTranslation = true;
				importSettings.ImportBackTranslation = false;
				ParatextHelper.LoadProjectMappings(importSettings);
				var importMap = importSettings.GetMappingListForDomain(ImportDomain.Main);
				var figureInfo = importMap[@"\fig"];
				if (figureInfo != null)
				{
					figureInfo.IsExcluded = true;
				}
				importSettings.SaveSettings();
				return true;
			});

			if (haveSomethingToImport && ReflectionHelper.GetBoolResult(ReflectionHelper.GetType("ParatextImport.dll", "ParatextImport.ParatextImportManager"), "ImportParatext", owningForm, m_cache, importSettings, m_scriptureStylesheet, App))
			{
				return m_scr.FindBook(bookNum);
			}
			return null;
		}

		/// <summary>
		/// Imports the specified book's back translation.
		/// </summary>
		/// <param name="owningForm">Form that can be used as the owner of progress dialogs and
		/// message boxes.</param>
		/// <param name="bookNum">The canonical book number.</param>
		/// <param name="btProject">The BT project to import</param>
		private void ImportBackTranslation(Form owningForm, int bookNum, IScrText btProject)
		{
			if (string.IsNullOrEmpty(btProject.Name))
			{
				return;
			}
			IScrImportSet importSettings = null;
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				importSettings = m_scr.FindOrCreateDefaultImportSettings(TypeOfImport.Paratext6, m_scriptureStylesheet, FwDirectoryFinder.TeStylesPath);
				importSettings.ParatextScrProj = m_associatedPtText.Name;
				importSettings.StartRef = new BCVRef(bookNum, 0, 0);
				importSettings.ParatextBTProj = btProject.Name;
				importSettings.ImportTranslation = false;
				importSettings.ImportBackTranslation = true;

				ParatextHelper.LoadProjectMappings(importSettings);
				var importMap = importSettings.GetMappingListForDomain(ImportDomain.Main);
				var figureInfo = importMap[@"\fig"];
				if (figureInfo != null)
				{
					figureInfo.IsExcluded = true;
				}
				importSettings.SaveSettings();
			});

			ReflectionHelper.GetBoolResult(ReflectionHelper.GetType("ParatextImport.dll", "ParatextImport.ParatextImportManager"), "ImportParatext", owningForm, m_cache, importSettings, m_scriptureStylesheet, App);
		}
	}
}
