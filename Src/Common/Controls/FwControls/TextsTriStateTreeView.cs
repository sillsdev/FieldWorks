// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TextsTriStateTreeView.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using Paratext;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Language;
using SIL.FieldWorks.Resources;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// TriStateTreeView that knows how to load scripture from the cache.
	/// </summary>
	public class TextsTriStateTreeView : TriStateTreeView
	{
		private IScripture m_scr;
		private IBookImporter m_bookImporter;
		private ScrText m_associatedPtText;
		private const string ksDummmyName = "dummy"; // used for Name of dummy nodes.

		/// <summary>
		/// Make one.
		/// </summary>
		public TextsTriStateTreeView()
		{
			BeforeExpand += ScriptureTriStateTreeView_BeforeExpand;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load Texts and ScrBooks into the tree view.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="bookImporter">The delegate/class that knows how to import a
		/// (Paratext) book on demand.</param>
		/// ------------------------------------------------------------------------------------
		private void LoadTextsAndBooks(FdoCache cache, IBookImporter bookImporter)
		{
			Nodes.Clear();
			LoadGeneralTexts(cache);

			if (FwUtils.FwUtils.IsOkToDisplayScriptureIfPresent)
				LoadScriptureTexts(cache, bookImporter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the non-Scripture texts.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void LoadGeneralTexts(FdoCache cache)
		{
			TreeNode tnTexts = LoadTextsByGenreAndWithoutGenre(cache);
			if (tnTexts != null && tnTexts.Nodes.Count > 0)
			{
				foreach (TreeNode textCat in tnTexts.Nodes)
				{
					Nodes.Add(textCat);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the texts for each Scripture book title, section, footnote, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void LoadScriptureTexts(FdoCache cache, IBookImporter bookImporter)
		{
			m_bookImporter = bookImporter;
			m_associatedPtText = bookImporter != null ? ParatextHelper.GetAssociatedProject(cache.ProjectId) : null;

			m_scr = cache.LanguageProject.TranslatedScriptureOA;
			if (m_scr == null)
				return;
			List<TreeNode> otBooks = new List<TreeNode>();
			List<TreeNode> ntBooks = new List<TreeNode>();
			for (int bookNum = 1; bookNum <= BCVRef.LastBook; bookNum++)
			{
				string bookName =
				cache.ServiceLocator.GetInstance<IScrRefSystemRepository>().Singleton.BooksOS[bookNum - 1].UIBookName;
				object book = m_scr.FindBook(bookNum);
				if (book == null)
				{
					if (m_associatedPtText != null && m_associatedPtText.BookPresent(bookNum))
						book = bookNum;
					else
						continue;
				}

				TreeNode node = new TreeNode(bookName);
				node.Tag = book;
				node.Name = "Book"; // help us query for books.
				if (bookNum < ScriptureTags.kiNtMin)
					otBooks.Add(node);
				else
					ntBooks.Add(node);
			}

			TreeNode bibleNode = new TreeNode(FwControls.kstidBibleNode);
			bibleNode.Name = "Bible";
			if (otBooks.Count > 0)
			{
				TreeNode testamentNode = new TreeNode(FwControls.kstidOtNode, otBooks.ToArray());
				testamentNode.Name = "Testament"; // help us query for Testaments
				bibleNode.Nodes.Add(testamentNode);
			}

			if (ntBooks.Count > 0)
			{
				TreeNode testamentNode = new TreeNode(FwControls.kstidNtNode, ntBooks.ToArray());
				testamentNode.Name = "Testament"; // help us query for Testaments
				bibleNode.Nodes.Add(testamentNode);
			}
			Nodes.Add(bibleNode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load texts by Genre into the texts tree view.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <returns>A control tree of the Texts in the project</returns>
		/// ------------------------------------------------------------------------------------
		public TreeNode LoadTextsByGenreAndWithoutGenre(FdoCache cache)
		{
			if (cache.LanguageProject.GenreListOA == null) return null;
			var genreList = cache.LanguageProject.GenreListOA.PossibilitiesOS;
			Debug.Assert(genreList != null);
			var allTexts = cache.ServiceLocator.GetInstance<ITextRepository>().AllInstances();
			if (allTexts == null)
				return null;

			// Title node for all texts, Biblical and otherwise
			var textsNode = new TreeNode("All Texts in Genres and not in Genres");
			textsNode.Name = "Texts";

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
				if (tex.GenresRC.Count == 0)
				{
					var texItem = new TreeNode(tex.ChooserNameTS.Text);
					texItem.Tag = tex.ContentsOA;
					texItem.Name = "Text";
					textsWithNoGenre.Add(texItem);

					// LT-12179: If this is the first tex we've added, establish the collator's details
					// according to the writing system at the start of the tex:
					if (!foundFirstText)
					{
						foundFirstText = true;
						var ws1 = tex.ChooserNameTS.get_WritingSystemAt(0);
						var wsEngine = cache.WritingSystemFactory.get_EngineOrNull(ws1);
						collator.Open(wsEngine.Id);
					}
				}
			}

			if (textsWithNoGenre.Count > 0)
			{
				// LT-12179: Order the TreeNodes alphabetically:
				textsWithNoGenre.Sort((x, y) => collator.Compare(x.Text, y.Text, LgCollatingOptions.fcoIgnoreCase));

				// Make a TreeNode for the texts with no known genre
				var woGenreTreeNode = new TreeNode("No Genre", textsWithNoGenre.ToArray());
				woGenreTreeNode.Name = "TextsWoGenre";
				textsNode.Nodes.Add(woGenreTreeNode);
			}
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
		private void LoadTextsFromGenres(TreeNode parent, IFdoOwningSequence<ICmPossibility> genreList, IEnumerable<FDO.IText> allTexts)
		{
			if (parent == null) return;
			var sortedGenreList = new List<ICmPossibility>();
			foreach (var gen in genreList)
			{
				sortedGenreList.Add(gen);
			}
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

				foreach (IText tex in allTexts)
				{   // This tex may not have a genre or it may claim to be in more than one
					foreach (var tgen in tex.GenresRC)
					{
						if (tgen.Equals(gen))
						{
							// The current tex is valid, so create a TreeNode with its details:
							var texItem = new TreeNode(tex.ChooserNameTS.Text);
							texItem.Tag = tex.ContentsOA;
							texItem.Name = "Text";

							// LT-12179: Add the new TreeNode to the (not-yet-)sorted list:
							sortedNodes.Add(texItem);

							// LT-12179: If this is the first tex we've added, establish the collator's details
							// according to the writing system at the start of the tex:
							if (!foundFirstText)
							{
								foundFirstText = true;
								var ws1 = tex.ChooserNameTS.get_WritingSystemAt(0);
								var wsEngine = gen.Cache.WritingSystemFactory.get_EngineOrNull(ws1);
								collator.Open(wsEngine.Id);
							}
							break;
						}
					}
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
			if (tag is ICmObject)
			{
				IScrBook book = ((ICmObject)tag).OwnerOfClass<IScrBook>();
				if (book != null)
					FillInChildren(FindNode(Nodes, book), false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the node for the specified book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static TreeNode FindNode(TreeNodeCollection nodes, IScrBook book)
		{
			foreach (TreeNode node in nodes)
			{
				if (node.Tag != null && (node.Tag.Equals(book.CanonicalNum) || node.Tag.Equals(book)))
					return node;
				var result = FindNode(node.Nodes, book);
				if (result != null)
					return result;
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
		/// <returns><c>true</c> if the given node has real child node(s) (after filling them
		/// in).</returns>
		/// ------------------------------------------------------------------------------------
		private bool FillInChildren(TreeNode bookNode, bool checkChildren)
		{
			if (bookNode == null)
				return false;
			bool retval = true;
			if ((bookNode.Tag is IScrBook || bookNode.Tag is int) &&
				bookNode.Nodes.Count == 1 && bookNode.Nodes[0].Name == ksDummmyName)
			{
				BeforeExpand -= ScriptureTriStateTreeView_BeforeExpand; // prevent recursion
				retval = FillInBookChildren(bookNode);
				if (retval && checkChildren)
					CheckAllChildren(bookNode);
				BeforeExpand += ScriptureTriStateTreeView_BeforeExpand;
			}
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
		void ScriptureTriStateTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			e.Cancel = !FillInChildren(e.Node, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load sections into the books of a Scripture tree view optionally including the
		/// heading as well as the content of each section.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="paratextBookImporter">The delegate/class that knows how to import a
		/// Paratext book on demand.</param>
		/// ------------------------------------------------------------------------------------
		public void LoadScriptureAndOtherTexts(FdoCache cache, IBookImporter paratextBookImporter)
		{
			// first load the book ids.
			LoadTextsAndBooks(cache, paratextBookImporter);

			if (Nodes.Count == 0)
				return;

			//This requires the Bible node be loaded last.
			TreeNode bibleNode = Nodes[Nodes.Count-1];
			//If there was no Bible node loaded then just return. This might be the case for the SE version of FLEx.
			if (bibleNode.Name != "Bible")
				return;

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
					TreeNode node = new TreeNode(ksDummmyName);
					node.Name = ksDummmyName;
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
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		private bool FillInBookChildren(TreeNode bookNode)
		{
			IScrBook book = bookNode.Tag as IScrBook;
			int bookNum = (book == null) ? (int)bookNode.Tag : book.CanonicalNum;
			Form owner = FindForm();
			while (owner != null && !owner.Visible)
				owner = owner.Owner;
			if (owner == null)
				owner = Form.ActiveForm ?? Application.OpenForms[0];

			if (book == null || (m_associatedPtText != null && m_associatedPtText.BookPresent(book.CanonicalNum) &&
				!m_associatedPtText.IsCheckSumCurrent(book.CanonicalNum, book.ImportedCheckSum)))
			{
				// The book for this node is out-of-date with the Paratext book data
				IScrBook importedBook = m_bookImporter.Import(bookNum, owner, false);
				if (importedBook != null)
					bookNode.Tag = book = importedBook;
				if (book == null)
					return false;
			}

			if (m_associatedPtText != null)
			{
				ScrText btProject = ParatextHelper.GetBtsForProject(m_associatedPtText).FirstOrDefault();
				if (btProject != null && !btProject.IsCheckSumCurrent(book.CanonicalNum,
					book.ImportedBtCheckSum.get_String(book.Cache.DefaultAnalWs).Text))
				{
					// The BT for this book node is out-of-date with the Paratext BT data
					m_bookImporter.Import(bookNum, owner, true);
				}
			}

			bookNode.Nodes.Clear(); // Gets rid of dummy.
			//IScrBook book = (IScrBook)bookNode.Tag;
			// Add Title node.
			if (book.TitleOA != null)
			{
				TreeNode titleNode =
					new TreeNode(ResourceHelper.GetResourceString("kstidScriptureTitle"));
				titleNode.Name = book.TitleOA.ToString();
				titleNode.Tag = book.TitleOA;
				bookNode.Nodes.Add(titleNode);
			}

			// Add Sections.
			foreach (IScrSection section in book.SectionsOS)
			{
				string chapterVerseBridge = m_scr.ChapterVerseBridgeAsString(section);
				if (section.HeadingOA != null)
				{
					// Include the heading text if it's not empty.  See LT-8764.
					int cTotal = 0;
					foreach (IScrTxtPara para in section.HeadingOA.ParagraphsOS)
						cTotal += para.Contents.Length;
					if (cTotal > 0)
					{
						string sFmt = ResourceHelper.GetResourceString("kstidSectionHeading");
						TreeNode node = new TreeNode(String.Format(sFmt, chapterVerseBridge));
						node.Name = String.Format(sFmt, section);
						node.Tag = section.HeadingOA; // expect an StText
						bookNode.Nodes.Add(node);
					}
				}
				TreeNode sectionNode = new TreeNode(chapterVerseBridge);
				sectionNode.Name = section.ToString();
				sectionNode.Tag = section.ContentOA; // expect an StText
				bookNode.Nodes.Add(sectionNode);
			}

			// Add Footnotes in reverse order, so we can insert them in the proper order.
			List<IScrFootnote> footnotes = new List<IScrFootnote>(book.FootnotesOS);
			footnotes.Reverse();
			foreach (IScrFootnote scrFootnote in footnotes)
			{
				// insert under the relevant section, if any (LTB-408)
				IScrSection containingSection;
				IStText containingTitle = null;
				if (scrFootnote.TryGetContainingSection(out containingSection) ||
					scrFootnote.TryGetContainingTitle(out containingTitle))
				{
					string nodeName = m_scr.ContainingRefAsString(scrFootnote);
					TreeNode footnoteNode = new TreeNode(nodeName);
					footnoteNode.Tag = scrFootnote;
					footnoteNode.Name = "Footnote";

					// see if we can lookup the node of this section.
					int nodeIndex = bookNode.Nodes.IndexOfKey(
						containingSection != null ? containingSection.ToString() : containingTitle.ToString());
					//TreeNode[] sectionNodes = bookNode.Nodes.Find(hvoSection.ToString(), false);
					//if (sectionNodes != null && sectionNodes.Length > 0)
					if (nodeIndex >= 0)
						bookNode.Nodes.Insert(nodeIndex + 1, footnoteNode);
					else
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
			ExpandAll();
			// Collapse anything below book level
			foreach (TreeNode node in Nodes.Find("Book", true))
				node.Collapse();
			foreach (TreeNode node in Nodes.Find("Genre", true))
				node.Collapse();
		}
	}

	#region IParatextBookImporter interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implement this interface to represent something that can import a book from an external
	/// source.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IBookImporter
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Imports the specified book.
		/// </summary>
		/// <param name="bookNum">The canonical book number.</param>
		/// <param name="owningForm">Form that can be used as the owner of progress dialogs and
		/// message boxes.</param>
		/// <param name="importBt">True to import only the back translation, false to import
		/// only the main translation</param>
		/// <returns>The ScrBook created to hold the imported data</returns>
		/// ------------------------------------------------------------------------------------
		IScrBook Import(int bookNum, Form owningForm, bool importBt);
	}
	#endregion
}
