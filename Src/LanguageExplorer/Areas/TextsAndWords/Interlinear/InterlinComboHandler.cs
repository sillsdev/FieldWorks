// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.ObjectModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This class and its subclasses handles the events that can happen in the course of
	/// the use of a combo box or popup list box in the Sandbox. Actually, a collection of
	/// subclasses, one for each kind of place the combo can be in the annotation hierarchy,
	/// handles the events.  For most of the primary events, the default here is to do
	/// nothing.
	/// </summary>
	internal class InterlinComboHandler : DisposableBase, IComboHandler
	{
		// Main array of information retrieved from sel that made combo.
		protected SelLevInfo[] m_rgvsli;
		protected int m_hvoSbWord; // Hvo of the root word.
		protected int m_hvoSelObject; // lowest level object selected.
		// selected morph, if any...may be zero if not in morph, or equal to m_hvoSelObject.
		// int for all classes, except IhMissingEntry, which studds MorphItem data into it.
		// So, that ill-behaved class has to make its own m_items data member.
		protected List<int> m_items = new List<int>();

		// More parallel data for the comboList items.
		protected IVwRootBox m_rootb;
		protected int m_wsVern;  // HVO of default vernacular writing system.
		protected int m_wsAnal;
		protected int m_wsUser;
		protected CachePair m_caches;
		protected bool m_fUnderConstruction; // True during SetupCombo.
		protected SandboxBase m_sandbox; // the sandbox we're manipulating.

		public InterlinComboHandler()
			: base()
		{
		}

		internal InterlinComboHandler(SandboxBase sandbox)
			: this()
		{
			m_sandbox = sandbox;
			m_caches = sandbox.Caches;
			m_wsVern = m_sandbox.RawWordformWs;
			m_wsAnal = m_caches.MainCache.DefaultAnalWs;
			m_wsUser = m_caches.MainCache.DefaultUserWs;
			m_hvoSbWord = SandboxBase.kSbWord;
			m_rootb = sandbox.RootBox;
		}

		// only for testing
		internal void SetSandboxForTesting(SandboxBase sandbox)
		{
			m_sandbox = sandbox;
			m_caches = sandbox.Caches;
			m_wsVern = m_caches.MainCache.DefaultVernWs;
		}

		internal void SetComboListForTesting(IComboList list)
		{
			ComboList = list;
		}

		internal void SetMorphForTesting(int imorph)
		{
			SelectedMorphHvo = m_sandbox.Caches.DataAccess.get_VecItem(SandboxBase.kSbWord, SandboxBase.ktagSbWordMorphs, imorph);
		}

		#region DisposableBase for IDisposable

		protected override void DisposeManagedResources()
		{
			// Dispose managed resources here.
			if (ComboList is IDisposable)
			{
				if ((ComboList as Control).Parent == null)
				{
					((IDisposable)ComboList).Dispose();
				}
				else if (ComboList is ComboListBox)
				{
					// It typically has a parent, the special form used to display it, so will not
					// get disposed by the above, but we do want to dispose it.
					((IDisposable)ComboList).Dispose();
				}
			}

			m_items?.Clear(); // I've seen it contain ints or MorphItems.

		}

		protected override void DisposeUnmanagedResources()
		{
			// Dispose unmanaged resources here, whether disposing is true or false.
			m_rgvsli = null;
			m_caches = null;
			m_sandbox = null;
			m_rootb = null;
			m_items = null;
			ComboList = null;
		}

		#endregion DisposableBase for IDisposable

		/// <summary>
		/// Setup the properties for combo items that should appear disabled.
		/// </summary>
		protected static ITsTextProps DisabledItemProperties()
		{
			return HighlightProperty(Color.LightGray);
		}

		/// <summary>
		/// Setup a property for a specified color.
		/// </summary>
		/// <returns></returns>
		protected static ITsTextProps HighlightProperty(Color highlightColor)
		{
			var color = (int)CmObjectUi.RGB(highlightColor);
			var bldr = TsStringUtils.MakePropsBldr();
			bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, color);
			return bldr.GetTextProps();
		}

		// Call this to create the appropriate subclass and set up the combo and return it.
		// May return null if no appropriate combo can be created at the current position.
		// Caller should hide all combos before calling, then
		// call Activate to add the combo to its controls (thus making it visible)
		// or display the ComboListBox if a non-null value
		// is returned.
		internal static IComboHandler MakeCombo(IHelpTopicProvider helpTopicProvider, IVwSelection vwselNew, SandboxBase sandbox, bool fMouseDown)
		{
			if (!vwselNew.IsValid)
			{
				throw new ArgumentException(@"The selection is invalid.", nameof(vwselNew));
			}
			// Figure what property is selected and create a suitable class if appropriate.
			var cvsli = vwselNew.CLevels(false);
			// CLevels includes the string property itself, but AllTextSelInfo doesn't need
			// it.
			cvsli--;
			// Out variable for AllTextSelInfo.
			int tagTextProp;
			// Main array of information retrived from sel that made combo.
			SelLevInfo[] rgvsli;
			if (cvsli < 0)
			{
				return null;
			}
			try
			{
				// More out variables for AllTextSelInfo.
				int ihvoRoot;
				int cpropPrevious;
				int ichAnchor;
				int ichEnd;
				int ws;
				bool fAssocPrev;
				int ihvoEnd;
				ITsTextProps ttpBogus;
				rgvsli = SelLevInfo.AllTextSelInfo(vwselNew, cvsli,
					out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
					out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);
			}
			catch
			{
				// If anything goes wrong just give up.
				return null;
			}

			var hvoMorph = 0;
			var hvoSelObject = 0;
			if (tagTextProp < SandboxBase.ktagMinIcon || tagTextProp >= SandboxBase.ktagLimIcon)
			{
				return null;
			}
			// If we're just hovering don't launch the pull-down.
			if (!fMouseDown)
			{
				return null;
			}

			if (rgvsli.Length >= 1)
			{
				hvoMorph = hvoSelObject = rgvsli[0].hvo;
			}
			return MakeCombo(helpTopicProvider, tagTextProp, sandbox, hvoMorph, rgvsli, hvoSelObject);
		}

		/// <summary>
		/// make a combo handler based upon the given comboIcon and morph
		/// </summary>
		internal static IComboHandler MakeCombo(IHelpTopicProvider helpTopicProvider, int tagComboIcon, SandboxBase sandbox, int imorph)
		{
			return MakeCombo(helpTopicProvider, tagComboIcon, sandbox, sandbox.Caches.DataAccess.get_VecItem(SandboxBase.kSbWord, SandboxBase.ktagSbWordMorphs, imorph), null, 0);
		}

		private static IComboHandler MakeCombo(IHelpTopicProvider helpTopicProvider, int tagComboIcon, SandboxBase sandbox, int hvoMorph, SelLevInfo[] rgvsli, int hvoSelObject)
		{
			var rootb = sandbox.RootBox;
			var hvoSbWord = sandbox.RootWordHvo;
			InterlinComboHandler handler;
			var caches = sandbox.Caches;
			switch (tagComboIcon)
			{
				case SandboxBase.ktagMorphFormIcon:
					handler = new IhMorphForm();
					break;
				case SandboxBase.ktagMorphEntryIcon:
					handler = new IhMorphEntry(helpTopicProvider);
					break;
				case SandboxBase.ktagWordPosIcon:
					handler = new IhWordPos();
					break;
				case SandboxBase.ktagAnalysisIcon:
					var clb2 = new ComboListBox();
					clb2.StyleSheet = sandbox.StyleSheet;
					var caHandler = new ChooseAnalysisHandler(caches.MainCache, hvoSbWord, sandbox.Analysis, clb2);
					caHandler.Owner = sandbox;
					caHandler.AnalysisChosen += sandbox.Handle_AnalysisChosen;
					caHandler.SetupCombo();
					return caHandler;
				case SandboxBase.ktagWordGlossIcon: // line 6, word gloss.
					if (sandbox.ShouldAddWordGlossToLexicon)
					{
						if (hvoMorph == 0)
						{
							// setup the first hvoMorph
							hvoMorph = caches.DataAccess.get_VecItem(SandboxBase.kSbWord, SandboxBase.ktagSbWordMorphs, 0);
						}
						handler = new IhLexWordGloss(helpTopicProvider);
					}
					else
					{
						handler = new IhWordGloss();
					}
					break;
				default:
					return null;
			}
			// Use the base class handler for most handlers. Override where needed.
			if (!(handler is IhWordPos))
			{
				var clb = new ComboListBox();
				handler.ComboList = clb;
				clb.SelectedIndexChanged += handler.HandleComboSelChange;
				clb.SameItemSelected += handler.HandleComboSelSame;
				// Since we may initialize with TsStrings, need to set WSF.
				handler.ComboList.WritingSystemFactory = caches.MainCache.LanguageWritingSystemFactoryAccessor;
			}
			else
			{
				// REVIEW: Do we need to handle wsf for word POS combo?
			}
			handler.m_caches = caches;
			handler.m_hvoSelObject = hvoSelObject;
			handler.m_hvoSbWord = hvoSbWord;
			handler.SelectedMorphHvo = hvoMorph;
			handler.m_rgvsli = rgvsli;
			handler.m_rootb = rootb;
			handler.m_wsVern = sandbox.RawWordformWs;
			handler.m_wsAnal = caches.MainCache.DefaultAnalWs;
			handler.m_wsUser = caches.MainCache.DefaultUserWs;
			handler.m_sandbox = sandbox;
			handler.m_fUnderConstruction = true;
			handler.SetupCombo();
			if (handler.ComboList != null)
			{
				handler.ComboList.StyleSheet = sandbox.StyleSheet;
			}
			handler.m_fUnderConstruction = false;
			return handler;
		}

		/// <summary>
		/// Hide yourself.
		/// </summary>
		public void Hide()
		{
			HideCombo();
		}

		// If the handler is managing a combo box and it is visible hide it.
		// Likewise if it is a combo list.
		internal void HideCombo()
		{
			if (m_sandbox.ParentForm == Form.ActiveForm)
			{
				m_sandbox.Focus();
			}
			var clb = ComboList as ComboListBox;
			if (clb == null)
			{
				return;
			}
			if (clb.IsDisposed)
			{
				// This can happen if the user tries hard enough.  See FWR-3577.
				// It seems to get reconstructed okay if we just clear it.
				ComboList = null;
			}
			else
			{
				clb.HideForm();
			}
		}

		// Activate the combo-handler's control.
		// If the control is a combo make it visible at the indicated location.
		// If it is a ComboListBox pop it up at the relevant place for the indicated
		// location.
		public virtual void Activate(Rect loc)
		{
			AdjustListBoxSize();
			var c = ((ComboListBox)ComboList);
			c.AdjustSize(500, 400); // these are maximums!
			c.Launch(m_sandbox.RectangleToScreen(loc), Screen.GetWorkingArea(m_sandbox));
		}

		internal void AdjustListBoxSize()
		{
			if (!(ComboList is ComboListBox))
			{
				return;
			}
			var clb = (ComboListBox)ComboList;
			using (var g = m_sandbox.CreateGraphics())
			{
				var nMaxWidth = 0;
				var nHeight = 0;
				var ie = clb.Items.GetEnumerator();
				while (ie.MoveNext())
				{
					string s = null;
					if (ie.Current is ITsString)
					{
						var tss = (ITsString)ie.Current;
						s = tss.Text;
					}
					else if (ie.Current is string)
					{
						s = (string)ie.Current;
					}

					if (s == null)
					{
						continue;
					}
					var szf = g.MeasureString(s, clb.Font);
					var nWidth = (int)szf.Width + 2;
					if (nMaxWidth < nWidth)
					{
						// 2 is not quite enough for height if you have homograph
						// subscripts.
						nMaxWidth = nWidth;
					}
					nHeight += (int)szf.Height + 3;
				}
				clb.Form.Width = Math.Max(clb.Form.Width, nMaxWidth);
				clb.Form.Height = Math.Max(clb.Form.Height, nHeight);
			}
		}

		public int SelectedMorphHvo { get; protected set; }

		// Return true if handled, otherwise, default behavior.
		public virtual bool HandleReturnKey()
		{
			return false;
		}

		// Handles a change in the item selected in the combo box.
		// Sub-classes can override where needed.
		internal virtual void HandleComboSelChange(object sender, EventArgs ea)
		{
			// Revisit (EricP): we could reimplement m_sandbox.HandleComboSelChange
			// here, but I suppose duplicating the logic here isn't necessary.
			// For now just use that one.
			// Assuming it's not disposed, which it might be apparently on switching tools.
			// (See LT-12350)
			if (!m_sandbox.IsDisposed)
			{
				m_sandbox.HandleComboSelChange(sender, ea);
			}
		}

		// Handles an item in the combo box when it is the same.
		// Sub-classes can override where needed.
		internal virtual void HandleComboSelSame(object sender, EventArgs ea)
		{
			// by default, just do the same as when item selected has changed.
			HandleComboSelChange(sender, ea);
		}

		/// <summary>
		/// Handle the user selecting an item in the control.
		/// </summary>
		public virtual void HandleSelectIfActive()
		{
			if (!m_fUnderConstruction)
			{
				HandleSelect(ComboList.SelectedIndex);
			}

			if (m_sandbox.ParentForm == Form.ActiveForm)
			{
				m_sandbox.Focus();
			}
		}
		// Handle the user selecting an item in the combo box.
		// Todo JohnT: many of the overrides should probably create a new selection.
		// The caller first hides the combo, so it can be manipulated in various
		// ways and possibly shown in a new place. Method should redisplay it if
		// appropriate.
		public virtual void HandleSelect(int index)
		{
		}

		/// <summary>
		/// select the combo list item matching the given string
		/// </summary>
		public virtual void SelectComboItem(string target)
		{
			int index;
			var foundItem = GetComboItem(target, out index);
			if (foundItem != null)
			{
				HandleSelect(index);
			}
		}

		internal object GetComboItem(string target, out int index)
		{
			object foundItem = null;
			index = 0;
			if (ComboList != null)
			{
				foreach (var item in ComboList.Items)
				{
					if (item is ITsString && (item as ITsString).Text == target || item is ITssValue && (item as ITssValue).AsTss.Text == target)
					{
						foundItem = item;
						break;
					}
					if (item.Equals(target))
					{
						foundItem = item;
						break;
					}
					index++;
				}
			}
			else if (Items != null)
			{
				// if Items is a list of Possibility hvos, you can check against names.
				foreach (var hvo in Items)
				{
					var possibility = m_caches.MainCache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(hvo);
					if (possibility != null && possibility.Name.BestAnalysisVernacularAlternative.Text == target)
					{
						foundItem = hvo;
						break;
					}
					index++;
				}
			}
			return foundItem;
		}

		/// <summary>
		/// select the combo item matching the given hvoTarget
		/// </summary>
		public virtual void SelectComboItem(int hvoTarget)
		{
			var index = 0;
			foreach (var item in Items)
			{
				if (item == hvoTarget)
				{
					HandleSelect(index);
					break;
				}
				index++;
			}
		}


		// This method contains the default SetupCombo functions, for the benefit of
		// classes that need to override without calling the immediate superclass,
		// but do want the general default behavior.
		internal void InitCombo()
		{
			m_items.Clear();
			ComboList.Items.Clear();
			// Some SetupCombo methods alter this to DropDownList, which prevents editing,
			// but it's useful to have a set default. Note that this needs to be done each
			// time, because we reuse the combo, and changes in one location can affect others.
			ComboList.DropDownStyle = ComboBoxStyle.DropDown;
		}

		/// <summary>
		/// Return the index of the currently selected item. Subclasses can override this
		/// method for finding the sandbox setting, so it can select and highlight
		/// that item in the list, rather than the default.
		/// </summary>
		public virtual int IndexOfCurrentItem => ComboList?.SelectedIndex ?? -1;

		// Save extra information needed for other commands, and set the combo items.
		// Or, change m_comboList to a new ComboListBox. This will result in no combo box
		// being displayed.
		public virtual void SetupCombo()
		{
			InitCombo();
		}

		/// <summary>
		/// the hvos related to the parallel items in m_comboList.Items
		/// </summary>
		public virtual List<int> Items => m_items;

		/// <summary>
		/// Handles the problem that an ITsString returns null (which works fine as a BSTR) when there
		/// are no characters. But in C#, null is not the same as an empty string.
		/// Also handles the possibility that the ITsString itself is null.
		/// </summary>
		public static string StrFromTss(ITsString tss)
		{
			return tss?.Text ?? string.Empty;
		}

		// Change the selection, keeping the higher levels of the current spec
		// from isliCopy onwards, and adding a new lowest level that has
		// cpropPrevious 0, and the specified tag and ihvo.
		// The selection made is an IP at the start of the property tagTextProp,
		// writing system ws, of the object thus specified.
		// (The selection is in the first and usually only root object.)
		internal void MakeNewSelection(int isliCopy, int tag, int ihvo, int tagTextProp, int ws)
		{
			var rgvsli = new SelLevInfo[m_rgvsli.Length - isliCopy + 1];
			for (var i = isliCopy; i < m_rgvsli.Length; i++)
			{
				rgvsli[i - isliCopy + 1] = m_rgvsli[i];
			}
			rgvsli[0].cpropPrevious = 0;
			rgvsli[0].ihvo = ihvo;
			rgvsli[0].tag = tag;

			// first and only root object; length and array of path to target object;
			// property, no previous occurrences, range 0 to 0, no ws, not assocPrev,
			// no other object for the other end,
			// no override text props, do make it the current active selection.
			m_rootb.MakeTextSelection(0, rgvsli.Length, rgvsli, tagTextProp, 0, 0, 0, ws, false, -1, null, true);
		}

		/// <summary>
		///  Add to the combo list the items in property flidVec of object hvoOwner in the main cache.
		///  Add to m_comboList.items the ShortName of each item, and to m_items the hvo.
		/// </summary>
		internal void AddVectorToComboItems(int hvoOwner, int flidVec)
		{
			var sda = m_caches.DataAccess;
			var citem = sda.get_VecSize(hvoOwner, flidVec);
			var coRepository = m_caches.MainCache.ServiceLocator.GetInstance<ICmObjectRepository>();

			for (var i = 0; i < citem; i++)
			{
				var hvoItem = sda.get_VecItem(hvoOwner, flidVec, i);
				m_items.Add(hvoItem);
				ComboList.Items.Add(coRepository.GetObject(hvoItem).ShortName);
			}
		}

		internal void AddPartsOfSpeechToComboItems()
		{
			AddVectorToComboItems(m_caches.MainCache.LangProject.PartsOfSpeechOA.Hvo, CmPossibilityListTags.kflidPossibilities);
		}

		internal int MorphCount => m_sandbox.MorphCount;

		/// <summary>
		/// Items that appear in the dropdown control for each interlinear line in the sandbox
		/// </summary>
		internal IComboList ComboList { get; private set; }

		internal int MorphHvo(int i)
		{
			return m_caches.DataAccess.get_VecItem(m_hvoSbWord, SandboxBase.ktagSbWordMorphs, i);
		}

		internal ITsString NewAnalysisString(string str)
		{
			return TsStringUtils.MakeString(str, m_caches.MainCache.DefaultAnalWs);
		}
		/// <summary>
		/// Synchronize the word gloss and POS with the morpheme gloss and MSA info, to the extent possible.
		/// Currently works FROM the morpheme TO the Word, but going the other way may be useful, too.
		///
		/// for the word gloss:
		///		- if only one morpheme, copy sense gloss to word gloss
		///		- if multiple morphemes, copy first stem gloss to word gloss, but only if word gloss is empty.
		///	for the POS:
		///		- if there is more than one stem and they have different parts of speech, do nothing.
		///		- if there is more than one derivational affix (DA), do nothing.
		///		- otherwise, if there is no DA, use the POS of the stem.
		///		- if there is no stem, do nothing.
		///		- if there is a DA, use its 'to' POS.
		///			(currently we don't insist that the 'from' POS matches the stem)
		/// </summary>
		internal void SyncMonomorphemicGlossAndPos(bool fCopyToWordGloss, bool fCopyToWordPos)
		{
			if (!fCopyToWordGloss && !fCopyToWordPos)
			{
				return;
			}

			var sda = m_caches.DataAccess;
			var cmorphs = sda.get_VecSize(m_hvoSbWord, SandboxBase.ktagSbWordMorphs);
			var hvoSbRootSense = 0;
			var hvoStemPos = 0; // ID in real database of part-of-speech of stem.
			var fGiveUpOnPOS = false;
			var hvoDerivedPos = 0; // real ID of POS output of derivational MSA.
			for (var imorph = 0; imorph < cmorphs; imorph++)
			{
				var hvoMorph = sda.get_VecItem(m_hvoSbWord, SandboxBase.ktagSbWordMorphs, imorph);
				var hvoSbSense = sda.get_ObjectProp(hvoMorph, SandboxBase.ktagSbMorphGloss);
				if (hvoSbSense == 0)
				{
					continue; // Can't sync from morph sense to word if we don't have  morph sense.
				}
				var sense = m_caches.RealObject(hvoSbSense) as ILexSense;
				var msa = sense.MorphoSyntaxAnalysisRA;
				var fStem = msa is IMoStemMsa;

				// If we have only one morpheme, treat it as the stem from which we will copy the gloss.
				// otherwise, use the first stem we find, if any.
				if (fStem && hvoSbRootSense == 0 || cmorphs == 1)
				{
					hvoSbRootSense = hvoSbSense;
				}

				if (fStem)
				{
					var hvoPOS = (msa as IMoStemMsa).PartOfSpeechRA?.Hvo ?? 0;
					if (hvoPOS != hvoStemPos && hvoStemPos != 0)
					{
						// found conflicting stems
						fGiveUpOnPOS = true;
					}
					else
					{
						hvoStemPos = hvoPOS;
					}
				}
				else if (msa is IMoDerivAffMsa)
				{
					if (hvoDerivedPos != 0)
					{
						fGiveUpOnPOS = true; // more than one DA
					}
					else
					{
						hvoDerivedPos = (msa as IMoDerivAffMsa).ToPartOfSpeechRA?.Hvo ?? 0;
					}
				}
			}

			// If we found a sense to copy from, do it.  Replace the word gloss even there already is
			// one, since users get confused/frustrated if we don't.  (See LT-6141.)  It's marked as a
			// guess after all!
			CopySenseToWordGloss(fCopyToWordGloss, hvoSbRootSense);

			// If we didn't find a stem, we don't have enough information to find a POS.
			if (hvoStemPos == 0)
			{
				fGiveUpOnPOS = true;
			}

			var hvoLexPos = 0;
			if (!fGiveUpOnPOS)
			{
				hvoLexPos = hvoDerivedPos != 0 ? hvoDerivedPos : hvoStemPos;
			}
			CopyLexPosToWordPos(fCopyToWordPos, hvoLexPos);
		}

		protected virtual void CopySenseToWordGloss(bool fCopyWordGloss, int hvoSbRootSense)
		{
			if (hvoSbRootSense == 0 || !fCopyWordGloss)
			{
				return;
			}
			var sda = m_caches.DataAccess;
			m_caches.DataAccess.SetInt(m_hvoSbWord, SandboxBase.ktagSbWordGlossGuess, 1);
			var hvoRealSense = m_caches.RealHvo(hvoSbRootSense);
			foreach (var wsId in m_sandbox.InterlinLineChoices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
			{
				// Update the guess, by copying the glosses of the SbNamedObj representing the sense
				// to the word gloss property.
				//ITsString tssGloss = sda.get_MultiStringAlt(hvoSbRootSense, ktagSbNamedObjName, wsId);
				// No, it is safer to copy from the real sense. We may be displaying more WSS for the word than the sense.
				var tssGloss = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(hvoRealSense, LexSenseTags.kflidGloss, wsId);
				sda.SetMultiStringAlt(m_hvoSbWord, SandboxBase.ktagSbWordGloss, wsId, tssGloss);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord, SandboxBase.ktagSbWordGloss, wsId, 0, 0);
			}
		}
		protected virtual int CopyLexPosToWordPos(bool fCopyToWordCat, int hvoMsaPos)
		{
			var hvoPos = 0;
			if (!fCopyToWordCat || hvoMsaPos == 0)
			{
				return hvoPos;
			}
			// got the one we want, in the real database. Make a corresponding sandbox one
			// and install it as a guess
			hvoPos = m_sandbox.CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidWordPos, hvoMsaPos, CmPossibilityTags.kflidAbbreviation);
			var hvoSbWordPos = m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, SandboxBase.ktagSbWordPos);
			m_caches.DataAccess.SetObjProp(m_hvoSbWord, SandboxBase.ktagSbWordPos, hvoPos);
			m_caches.DataAccess.SetInt(hvoPos, SandboxBase.ktagSbNamedObjGuess, 1);
			m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord, SandboxBase.ktagSbWordPos, 0, 1, (hvoSbWordPos == 0 ? 0 : 1));
			return hvoPos;
		}
	}
}