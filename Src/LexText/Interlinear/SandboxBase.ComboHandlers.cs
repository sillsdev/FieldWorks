using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.LexText.Controls;
using Color=System.Drawing.Color;
using SIL.CoreImpl;
using XCore;

namespace SIL.FieldWorks.IText
{

	/// <summary>
	/// An interface common to classes that 'handle' combo boxes that appear when something in
	/// IText is clicked.
	/// </summary>
	internal interface IComboHandler
	{
		/// <summary>
		/// Initialize the combo contents.
		/// </summary>
		void SetupCombo();
		/// <summary>
		/// Get rid of the combo, typically when the user clicks outside it.
		/// </summary>
		void Hide();
		/// <summary>
		/// Handle a return key press in an editable combo.
		/// </summary>
		/// <returns></returns>
		bool HandleReturnKey();
		/// <summary>
		/// Activate the combo-handler's control.
		/// If the control is a combo make it visible at the indicated location.
		/// If it is a ComboListBox pop it up at the relevant place for the indicated location.
		/// </summary>
		/// <param name="loc"></param>
		void Activate(SIL.Utils.Rect loc);

		/// <summary>
		/// This one is a bit awkward in this interface, but it simplifies things. It's OK to
		/// just answer zero if the handler has no particular morpheme selected.
		/// </summary>
		int SelectedMorphHvo { get; }

		/// <summary>
		/// Act as if the user selected the current item.
		/// </summary>
		void HandleSelectIfActive();

	}

	partial class SandboxBase
	{
		/// <summary>
		/// This class and its subclasses handles the events that can happen in the course of
		/// the use of a combo box or popup list box in the Sandbox. Actually, a collection of
		/// subclasses, one for each kind of place the combo can be in the annotation hierarchy,
		/// handles the events.  For most of the primary events, the default here is to do
		/// nothing.
		/// </summary>
		public class InterlinComboHandler : FwDisposableBase, IComboHandler
		{
			// Main array of information retrieved from sel that made combo.
			protected SelLevInfo[] m_rgvsli;
			protected int m_hvoSbWord; // Hvo of the root word.
			protected int m_hvoSelObject; // lowest level object selected.
			// selected morph, if any...may be zero if not in morph, or equal to m_hvoSelObject.
			protected int m_hvoMorph;
			// int for all classes, except IhMissingEntry, which studds MorphItem data into it.
			// So, that ill-behaved class has to make its own m_items data member.
			protected List<int> m_items = new List<int>();
			private IComboList m_comboList;
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
				m_hvoSbWord = kSbWord;
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
				m_comboList = list;
			}

			internal void SetMorphForTesting(int imorph)
			{
				m_hvoMorph = m_sandbox.Caches.DataAccess.get_VecItem(kSbWord, ktagSbWordMorphs, imorph);
			}

			#region FwDisposableBase for IDisposable

			protected override void DisposeManagedResources()
			{
				// Dispose managed resources here.
				if (m_comboList != null && (m_comboList is IDisposable) && (m_comboList as Control).Parent == null)
					(m_comboList as IDisposable).Dispose();
				else if (m_comboList is ComboListBox)
				{
					// It typically has a parent, the special form used to display it, so will not
					// get disposed by the above, but we do want to dispose it.
					(m_comboList as IDisposable).Dispose();
				}
				if (m_items != null)
					m_items.Clear(); // I've seen it contain ints or MorphItems.

			}

			protected override void DisposeUnmanagedResources()
			{
				// Dispose unmanaged resources here, whether disposing is true or false.
				m_rgvsli = null;
				m_caches = null;
				m_sandbox = null;
				m_rootb = null;
				m_items = null;
				m_comboList = null;
			}

			#endregion FwDisposableBase for IDisposable

			/// <summary>
			/// encapsulates the common behavior of items in an InterlinComboHandler combo list.
			/// </summary>
			internal class InterlinComboHandlerActionComboItem : HvoTssComboItem
			{
				EventHandler OnSelect;

				/// <summary>
				///
				/// </summary>
				/// <param name="tssDisplay">the tss used to display the text of the combo item.</param>
				/// <param name="select">the event delegate to be executed when this item is selected. By default,
				/// we send "this" InterlinComboHandlerActionComboItem as the event sender.</param>
				internal InterlinComboHandlerActionComboItem(ITsString tssDisplay, EventHandler select)
					: this(tssDisplay, select, 0, 0)
				{
				}

				/// <summary>
				///
				/// </summary>
				/// <param name="tssDisplay">the tss to display in the combo box.</param>
				/// <param name="select">the event to fire when this is selected</param>
				/// <param name="hvoPrimary">the hvo most closely associated with this item, 0 if none.</param>
				/// <param name="tag">id to resolve any further ambiguity associated with this item's hvo.</param>
				internal InterlinComboHandlerActionComboItem(ITsString tssDisplay, EventHandler select, int hvoPrimary, int tag)
					: base(hvoPrimary, tssDisplay, tag)
				{
					OnSelect = select;
				}

				/// <summary>
				/// If enabled, will do something if clicked.
				/// </summary>
				internal bool IsEnabled
				{
					get { return OnSelect != null; }
				}

				/// <summary>
				/// Do OnSelect if defined, and this item is enabled.
				/// By default, we send "this" InterlinComboHandlerActionComboItem as the event sender.
				/// </summary>
				internal protected virtual void OnSelectItem()
				{
					if (OnSelect != null && IsEnabled)
						OnSelect(this, EventArgs.Empty);
				}
			}

			/// <summary>
			/// Setup the properties for combo items that should appear disabled.
			/// </summary>
			/// <returns></returns>
			protected static ITsTextProps DisabledItemProperties()
			{
				return HighlightProperty(Color.LightGray);
			}

			/// <summary>
			/// Setup a property for a specified color.
			/// </summary>
			/// <returns></returns>
			protected static ITsTextProps HighlightProperty(System.Drawing.Color highlightColor)
			{
				int color = (int)CmObjectUi.RGB(highlightColor);
				ITsPropsBldr bldr = TsPropsBldrClass.Create();
				bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor,
					(int)FwTextPropVar.ktpvDefault, color);
				return bldr.GetTextProps();
			}

			// Call this to create the appropriate subclass and set up the combo and return it.
			// May return null if no appropriate combo can be created at the current position.
			// Caller should hide all combos before calling, then
			// call Activate to add the combo to its controls (thus making it visible)
			// or display the ComboListBox if a non-null value
			// is returned.
			static internal IComboHandler MakeCombo(IHelpTopicProvider helpTopicProvider,
				IVwSelection vwselNew, SandboxBase sandbox, bool fMouseDown)
			{
				// Figure what property is selected and create a suitable class if appropriate.
				int cvsli = vwselNew.CLevels(false);
				// CLevels includes the string property itself, but AllTextSelInfo doesn't need
				// it.
				cvsli--;

				// Out variables for AllTextSelInfo.
				int ihvoRoot;
				int tagTextProp;
				int cpropPrevious;
				int ichAnchor;
				int ichEnd;
				int ws;
				bool fAssocPrev;
				int ihvoEnd;
				ITsTextProps ttpBogus;
				// Main array of information retrived from sel that made combo.
				SelLevInfo[] rgvsli;

				// Analysis can now be zero (e.g., displaying alterate case form for non-existent WfiWordform)
				// and I don't believe it's a problem for the code below (JohnT).
				//				if (sandbox.Analysis == 0)
				//				{
				//					// We aren't fully initialized yet, so don't do anything.
				//					return null;
				//				}
				if (cvsli < 0)
					return null;
				try
				{
					rgvsli = SelLevInfo.AllTextSelInfo(vwselNew, cvsli,
						out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
						out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);
				}
				catch
				{
					// If anything goes wrong just give up.
					return null;
				}

				int hvoMorph = 0;
				int hvoSelObject = 0;
				if (tagTextProp >= ktagMinIcon && tagTextProp < ktagLimIcon) // its an icon
				{
					// If we're just hovering don't launch the pull-down.
					if (!fMouseDown)
						return null;
					if (rgvsli.Length >= 1)
						hvoMorph = hvoSelObject = rgvsli[0].hvo;
					return MakeCombo(helpTopicProvider, tagTextProp, sandbox, hvoMorph, rgvsli, hvoSelObject);
				}
				return null;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// make a combo handler based upon the given comboIcon and morph
			/// </summary>
			/// <param name="helpTopicProvider">The help topic provider.</param>
			/// <param name="tagComboIcon">The tag combo icon.</param>
			/// <param name="sandbox">The sandbox.</param>
			/// <param name="imorph">The index of the morph.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			internal static IComboHandler MakeCombo(IHelpTopicProvider helpTopicProvider,
				int tagComboIcon, SandboxBase sandbox, int imorph)
			{
				int hvoSbMorph = sandbox.Caches.DataAccess.get_VecItem(kSbWord, ktagSbWordMorphs, imorph);
				return MakeCombo(helpTopicProvider, tagComboIcon, sandbox, hvoSbMorph, null, 0);
			}

			private static IComboHandler MakeCombo(IHelpTopicProvider helpTopicProvider,
				int tagComboIcon, SandboxBase sandbox, int hvoMorph, SelLevInfo[] rgvsli, int hvoSelObject)
			{
				IVwRootBox rootb = sandbox.RootBox;
				int hvoSbWord = sandbox.RootWordHvo;
				InterlinComboHandler handler = null;
				CachePair caches = sandbox.Caches;
				switch (tagComboIcon)
				{
					case ktagMorphFormIcon:
						handler = new IhMorphForm();
						break;
					case ktagMorphEntryIcon:
						handler = new IhMorphEntry(helpTopicProvider);
						break;
					case ktagWordPosIcon:
						handler = new IhWordPos();
						break;
					case ktagAnalysisIcon:
						ComboListBox clb2 = new ComboListBox();
						clb2.StyleSheet = sandbox.StyleSheet;
						ChooseAnalysisHandler caHandler = new ChooseAnalysisHandler(
							caches.MainCache, hvoSbWord, sandbox.Analysis, clb2);
						caHandler.Owner = sandbox;
						caHandler.AnalysisChosen += new EventHandler(
							sandbox.Handle_AnalysisChosen);
						caHandler.SetupCombo();
						return caHandler;
					case ktagWordGlossIcon: // line 6, word gloss.
						if (sandbox.ShouldAddWordGlossToLexicon)
						{
							if (hvoMorph == 0)
							{
								// setup the first hvoMorph
								hvoMorph = caches.DataAccess.get_VecItem(kSbWord, ktagSbWordMorphs, 0);
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
					ComboListBox clb = new ComboListBox();
					handler.m_comboList = clb;
					clb.SelectedIndexChanged += new EventHandler(
						handler.HandleComboSelChange);
					clb.SameItemSelected += new EventHandler(
						handler.HandleComboSelSame);
					// Since we may initialize with TsStrings, need to set WSF.
					handler.m_comboList.WritingSystemFactory =
						caches.MainCache.LanguageWritingSystemFactoryAccessor;
				}
				else
				{
					// REVIEW: Do we need to handle wsf for word POS combo?
				}
				handler.m_caches = caches;
				handler.m_hvoSelObject = hvoSelObject;
				handler.m_hvoSbWord = hvoSbWord;
				handler.m_hvoMorph = hvoMorph;
				handler.m_rgvsli = rgvsli;
				handler.m_rootb = rootb;
				handler.m_wsVern = sandbox.RawWordformWs;
				handler.m_wsAnal = caches.MainCache.DefaultAnalWs;
				handler.m_wsUser = caches.MainCache.DefaultUserWs;
				handler.m_sandbox = sandbox;
				handler.m_fUnderConstruction = true;
				handler.SetupCombo();
				if (handler.m_comboList != null)
					handler.m_comboList.StyleSheet = sandbox.StyleSheet;
				handler.m_fUnderConstruction = false;
				return handler;
			}

			/// <summary>
			/// Hide yourself.
			/// </summary>
			public void Hide()
			{
				CheckDisposed();

				HideCombo();
			}

			// If the handler is managing a combo box and it is visible hide it.
			// Likewise if it is a combo list.
			internal void HideCombo()
			{
				CheckDisposed();

				if (m_sandbox.ParentForm == Form.ActiveForm)
					m_sandbox.Focus();
				ComboListBox clb = m_comboList as ComboListBox;
				if (clb != null)
				{
					if (clb.IsDisposed)
					{
						// This can happen if the user tries hard enough.  See FWR-3577.
						// It seems to get reconstructed okay if we just clear it.
						m_comboList = null;
					}
					else
					{
						clb.HideForm();
					}
				}
			}

			// Activate the combo-handler's control.
			// If the control is a combo make it visible at the indicated location.
			// If it is a ComboListBox pop it up at the relevant place for the indicated
			// location.
			public virtual void Activate(SIL.Utils.Rect loc)
			{
				CheckDisposed();

				AdjustListBoxSize();
				ComboListBox c = (m_comboList as ComboListBox);
				c.AdjustSize(500, 400); // these are maximums!
				c.Launch(m_sandbox.RectangleToScreen(loc),
					Screen.GetWorkingArea(m_sandbox));
			}

			internal void AdjustListBoxSize()
			{
				CheckDisposed();

				if (m_comboList is ComboListBox)
				{
					ComboListBox clb = m_comboList as ComboListBox;
					using (var g = m_sandbox.CreateGraphics())
					{
						int nMaxWidth = 0;
						int nHeight = 0;
						IEnumerator ie = clb.Items.GetEnumerator();
						while (ie.MoveNext())
						{
							string s = null;
							if (ie.Current is ITsString)
							{
								ITsString tss = ie.Current as ITsString;
								s = tss.Text;
							}
							else if (ie.Current is String)
							{
								s = ie.Current as string;
							}
							if (s != null)
							{
								SizeF szf = g.MeasureString(s, clb.Font);
								int nWidth = (int)szf.Width + 2;
								if (nMaxWidth < nWidth)
									// 2 is not quite enough for height if you have homograph
									// subscripts.
									nMaxWidth = nWidth;
								nHeight += (int)szf.Height + 3;
							}
						}
						clb.Form.Width = Math.Max(clb.Form.Width, nMaxWidth);
						clb.Form.Height = Math.Max(clb.Form.Height, nHeight);
					}
				}
			}

			public int SelectedMorphHvo
			{
				get
				{
					CheckDisposed();
					return m_hvoMorph;
				}
			}

			// Return true if handled, otherwise, default behavior.
			public virtual bool HandleReturnKey()
			{
				CheckDisposed();

				return false;
			}

			// Handles a change in the item selected in the combo box.
			// Sub-classes can override where needed.
			internal virtual void HandleComboSelChange(object sender, EventArgs ea)
			{
				CheckDisposed();

				// Revisit (EricP): we could reimplement m_sandbox.HandleComboSelChange
				// here, but I suppose duplicating the logic here isn't necessary.
				// For now just use that one.
				// Assuming it's not disposed, which it might be apparently on switching tools.
				// (See LT-12350)
				if (!m_sandbox.IsDisposed)
					m_sandbox.HandleComboSelChange(sender, ea);
				// Alternative re-implementation:
				//	if (m_fUnderConstruction)
				//		return;
				//	this.HideCombo();
				//	HandleSelectIfActive();
			}

			// Handles an item in the combo box when it is the same.
			// Sub-classes can override where needed.
			internal virtual void HandleComboSelSame(object sender, EventArgs ea)
			{
				CheckDisposed();

				// by default, just do the same as when item selected has changed.
				this.HandleComboSelChange(sender, ea);
			}

			/// <summary>
			/// Handle the user selecting an item in the control.
			/// </summary>
			public virtual void HandleSelectIfActive()
			{
				CheckDisposed();

				if (!m_fUnderConstruction)
					HandleSelect(m_comboList.SelectedIndex);
				if (m_sandbox.ParentForm == Form.ActiveForm)
					m_sandbox.Focus();
			}
			// Handle the user selecting an item in the combo box.
			// Todo JohnT: many of the overrides should probably create a new selection.
			// The caller first hides the combo, so it can be manipulated in various
			// ways and possibly shown in a new place. Method should redisplay it if
			// appropriate.
			public virtual void HandleSelect(int index)
			{
				CheckDisposed();
			}

			/// <summary>
			/// select the combo list item matching the given string
			/// </summary>
			/// <param name="target"></param>
			public virtual void SelectComboItem(string target)
			{
				int index;
				object foundItem = GetComboItem(target, out index);
				if (foundItem != null)
				{
					HandleSelect(index);
				}
			}

			internal object GetComboItem(string target, out int index)
			{
				object foundItem = null;
				index = 0;
				if (m_comboList != null)
				{
					foreach (object item in m_comboList.Items)
					{
						if (((item is ITsString) && (item as ITsString).Text == target) ||
							(item is ITssValue) && (item as ITssValue).AsTss.Text == target)
						{
							foundItem = item;
							break;
						}
						else if (item.Equals(target))
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
					foreach (int hvo in Items)
					{
						ICmPossibility possibility =
							m_caches.MainCache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(hvo);
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
			/// <param name="hvoTarget"></param>
			public virtual void SelectComboItem(int hvoTarget)
			{
				int index = 0;
				foreach (int item in Items)
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
				CheckDisposed();

				m_items.Clear();
				m_comboList.Items.Clear();
				// Some SetupCombo methods alter this to DropDownList, which prevents editing,
				// but it's useful to have a set default. Note that this needs to be done each
				// time, because we reuse the combo, and changes in one location can affect others.
				m_comboList.DropDownStyle = ComboBoxStyle.DropDown;
			}

			/// <summary>
			/// Return the index of the currently selected item. Subclasses can override this
			/// method for finding the sandbox setting, so it can select and highlight
			/// that item in the list, rather than the default.
			/// </summary>
			/// <returns></returns>
			public virtual int IndexOfCurrentItem
			{
				get
				{
					if (m_comboList != null)
						return m_comboList.SelectedIndex;
					return -1;
				}
			}

			// Save extra information needed for other commands, and set the combo items.
			// Or, change m_comboList to a new ComboListBox. This will result in no combo box
			// being displayed.
			public virtual void SetupCombo()
			{
				CheckDisposed();

				InitCombo();
			}

			/// <summary>
			/// the hvos related to the parallel items in m_comboList.Items
			/// </summary>
			public virtual List<int> Items
			{
				get { return m_items; }
			}

			/// <summary>
			/// Handles the problem that an ITsString returns null (which works fine as a BSTR) when there
			/// are no characters. But in C#, null is not the same as an empty string.
			/// Also handles the possibility that the ITsString itself is null.
			/// </summary>
			/// <param name="tss"></param>
			/// <returns></returns>
			public static string StrFromTss(ITsString tss)
			{
				if (tss == null)
					return string.Empty;
				string result = tss.Text;
				if (result != null)
					return result;
				return string.Empty;
			}

			// Change the selection, keeping the higher levels of the current spec
			// from isliCopy onwards, and adding a new lowest level that has
			// cpropPrevious 0, and the specified tag and ihvo.
			// The selection made is an IP at the start of the property tagTextProp,
			// writing system ws, of the object thus specified.
			// (The selection is in the first and usually only root object.)
			internal void MakeNewSelection(int isliCopy, int tag, int ihvo, int tagTextProp, int ws)
			{
				CheckDisposed();

				SelLevInfo[] rgvsli = new SelLevInfo[m_rgvsli.Length - isliCopy + 1];
				for (int i = isliCopy; i < m_rgvsli.Length; i++)
					rgvsli[i - isliCopy + 1] = m_rgvsli[i];
				rgvsli[0].cpropPrevious = 0;
				rgvsli[0].ihvo = ihvo;
				rgvsli[0].tag = tag;

				// first and only root object; length and array of path to target object;
				// property, no previous occurrences, range 0 to 0, no ws, not assocPrev,
				// no other object for the other end,
				// no override text props, do make it the current active selection.
				m_rootb.MakeTextSelection(0, rgvsli.Length, rgvsli, tagTextProp,
					0, 0, 0, ws, false, -1, null, true);
			}

			/// <summary>
			///  Add to the combo list the items in property flidVec of object hvoOwner in the main cache.
			///  Add to m_comboList.items the ShortName of each item, and to m_items the hvo.
			/// </summary>
			/// <param name="hvoOwner"></param>
			/// <param name="flidVec"></param>
			internal void AddVectorToComboItems(int hvoOwner, int flidVec)
			{
				CheckDisposed();
				ISilDataAccess sda = m_caches.DataAccess;
				int citem = sda.get_VecSize(hvoOwner, flidVec);
				var coRepository = m_caches.MainCache.ServiceLocator.GetInstance<ICmObjectRepository>();

				for (int i = 0; i < citem; i++)
				{
					int hvoItem = sda.get_VecItem(hvoOwner, flidVec, i);
					m_items.Add(hvoItem);
					m_comboList.Items.Add(coRepository.GetObject(hvoItem).ShortName);
				}
			}

			internal void AddPartsOfSpeechToComboItems()
			{
				CheckDisposed();

				AddVectorToComboItems(m_caches.MainCache.LangProject.PartsOfSpeechOA.Hvo,
					CmPossibilityListTags.kflidPossibilities);
			}

			internal int MorphCount
			{
				get
				{
					CheckDisposed();
					return m_sandbox.MorphCount;
				}

			}

			/// <summary>
			/// Items that appear in the dropdown control for each interlinear line in the sandbox
			/// </summary>
			internal IComboList ComboList
			{
				get { return m_comboList; }
			}

			internal int MorphHvo(int i)
			{
				CheckDisposed();

				return m_caches.DataAccess.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, i);
			}

			internal ITsString NewAnalysisString(string str)
			{
				CheckDisposed();

				return TsStrFactoryClass.Create().
					MakeString(str, m_caches.MainCache.DefaultAnalWs);
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
				CheckDisposed();
				if (!fCopyToWordGloss && !fCopyToWordPos)
					return;

				ISilDataAccess sda = m_caches.DataAccess;
				int cmorphs = sda.get_VecSize(m_hvoSbWord, ktagSbWordMorphs);
				int hvoSbRootSense = 0;
				int hvoStemPos = 0; // ID in real database of part-of-speech of stem.
				bool fGiveUpOnPOS = false;
				int hvoDerivedPos = 0; // real ID of POS output of derivational MSA.
				for (int imorph = 0; imorph < cmorphs; imorph++)
				{
					int hvoMorph = sda.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, imorph);
					int hvoSbSense = sda.get_ObjectProp(hvoMorph, ktagSbMorphGloss);
					if (hvoSbSense == 0)
						continue; // Can't sync from morph sense to word if we don't have  morph sense.
					var sense = m_caches.RealObject(hvoSbSense) as ILexSense;
					IMoMorphSynAnalysis msa = sense.MorphoSyntaxAnalysisRA;

					//					ITsString prefix = sda.get_StringProp(hvoMorph, ktagSbMorphPrefix);
					//					ITsString suffix = sda.get_StringProp(hvoMorph, ktagSbMorphPostfix);
					//					bool fStem = prefix.Length == 0 && suffix.Length == 0;

					bool fStem = msa is IMoStemMsa;

					// If we have only one morpheme, treat it as the stem from which we will copy the gloss.
					// otherwise, use the first stem we find, if any.
					if ((fStem && hvoSbRootSense == 0) || cmorphs == 1)
						hvoSbRootSense = hvoSbSense;

					if (fStem)
					{
						int hvoPOS = (msa as IMoStemMsa).PartOfSpeechRA != null ? (msa as IMoStemMsa).PartOfSpeechRA.Hvo : 0;
						if (hvoPOS != hvoStemPos && hvoStemPos != 0)
						{
							// found conflicting stems
							fGiveUpOnPOS = true;
						}
						else
							hvoStemPos = hvoPOS;
					}
					else if (msa is IMoDerivAffMsa)
					{
						if (hvoDerivedPos != 0)
							fGiveUpOnPOS = true; // more than one DA
						else
							hvoDerivedPos = (msa as IMoDerivAffMsa).ToPartOfSpeechRA != null ? (msa as IMoDerivAffMsa).ToPartOfSpeechRA.Hvo : 0;
					}
				}

				// If we found a sense to copy from, do it.  Replace the word gloss even there already is
				// one, since users get confused/frustrated if we don't.  (See LT-6141.)  It's marked as a
				// guess after all!
				CopySenseToWordGloss(fCopyToWordGloss, hvoSbRootSense);

				// If we didn't find a stem, we don't have enough information to find a POS.
				if (hvoStemPos == 0)
					fGiveUpOnPOS = true;

				int hvoLexPos = 0;
				if (!fGiveUpOnPOS)
				{
					if (hvoDerivedPos != 0)
						hvoLexPos = hvoDerivedPos;
					else
						hvoLexPos = hvoStemPos;
				}
				CopyLexPosToWordPos(fCopyToWordPos, hvoLexPos);
			}

			protected virtual void CopySenseToWordGloss(bool fCopyWordGloss, int hvoSbRootSense)
			{
				if (hvoSbRootSense != 0 && fCopyWordGloss)
				{
					ISilDataAccess sda = m_caches.DataAccess;
					m_caches.DataAccess.SetInt(m_hvoSbWord, ktagSbWordGlossGuess, 1);
					int hvoRealSense = m_caches.RealHvo(hvoSbRootSense);
					foreach (int wsId in m_sandbox.m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
					{
						// Update the guess, by copying the glosses of the SbNamedObj representing the sense
						// to the word gloss property.
						//ITsString tssGloss = sda.get_MultiStringAlt(hvoSbRootSense, ktagSbNamedObjName, wsId);
						// No, it is safer to copy from the real sense. We may be displaying more WSS for the word than the sense.
						ITsString tssGloss = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(hvoRealSense, LexSenseTags.kflidGloss, wsId);
						sda.SetMultiStringAlt(m_hvoSbWord, ktagSbWordGloss, wsId, tssGloss);
						sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord, ktagSbWordGloss,
							wsId, 0, 0);
					}
				}
			}
			protected virtual int CopyLexPosToWordPos(bool fCopyToWordCat, int hvoMsaPos)
			{
				int hvoPos = 0;
				if (fCopyToWordCat && hvoMsaPos != 0)
				{
					// got the one we want, in the real database. Make a corresponding sandbox one
					// and install it as a guess
					hvoPos = m_sandbox.CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidWordPos, hvoMsaPos,
						CmPossibilityTags.kflidAbbreviation);
					int hvoSbWordPos = m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, ktagSbWordPos);
					m_caches.DataAccess.SetObjProp(m_hvoSbWord, ktagSbWordPos, hvoPos);
					m_caches.DataAccess.SetInt(hvoPos, ktagSbNamedObjGuess, 1);
					m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord,
						ktagSbWordPos, 0, 1, (hvoSbWordPos == 0 ? 0 : 1));
				}
				return hvoPos;
			}
		}


		/// <summary>
		/// The actual form of the word. Eventually we will offer a popup representing all the
		/// currently known possible analyses, and other options.
		/// </summary>
		internal class IhSbWordForm : InterlinComboHandler
		{
			public override void SetupCombo()
			{
				CheckDisposed();

				base.SetupCombo();
				ComboList.Items.Add(ITextStrings.ksAcceptEntireAnalysis);
				ComboList.Items.Add(ITextStrings.ksEditThisWordform);
				ComboList.Items.Add(ITextStrings.ksDeleteThisWordform);
				// These aren't likely to get implemented soon.
				//m_comboList.Items.Add("Change spelling of occurrences");
				//m_comboList.Items.Add("Concordance");
				//// following not valid, don't know how in .NET, maybe Add("-")?
				//m_comboList.Items.AddSeparator();
				//m_comboList.Add("Interlinear help");

				ComboList.DropDownStyle = ComboBoxStyle.DropDownList; // Prevents direct editing.
			}

			public override void HandleSelect(int index)
			{
				CheckDisposed();

				switch (index)
				{
					case 0: // Accept entire analysis
						// Todo: figure how to implement.
						break;
					case 1: // Edit this wordform.
						// Allows direct editing.
						ComboList.DropDownStyle = ComboBoxStyle.DropDown;
						// restore the combo to visibility so we can do the editing.
						m_sandbox.ShowCombo();
						break;
					case 2: // Delete this wordform.
						// Todo: figure implementation
						//					int ihvoTwfic = m_rgvsli[m_iRoot].ihvo;
						//					int [] itemsToInsert = new int[0];
						//					m_fdoCache.ReplaceReferenceProperty(m_hvoSbWord,
						//						StTxtParaTags.kflidAnalyzedTextObjects,
						//						ihvoTwfic, ihvoTwfic + 1, ref itemsToInsert);
						// Enhance JohnT: consider removing the WfiWordform, if there are no
						// analyses and no other references.
						// Comment: RandyR: Please don't delete it.
						break;
				}
			}

			public override bool HandleReturnKey()
			{
				CheckDisposed();

				// If it hasn't changed don't do anything.
				string newval = ComboList.Text;
				if (newval == StrFromTss(m_caches.DataAccess.get_MultiStringAlt(m_hvoSbWord, ktagSbWordForm, m_sandbox.RawWordformWs)))
				{
					return true;
				}
				ITsString tssWord = TsStrFactoryClass.Create().MakeString(newval,
					m_sandbox.RawWordformWs);
				// Todo JohnT: clean out old analysis, come up with new defaults.
				//SetAnalysisTo(DbOps.FindOrCreateWordform(m_fdoCache, tssWord));
				// Enhance JohnT: consider removing the old WfiWordform, if there are no
				// analyses and no other references.
				return true;
			}
		}

		internal class IhMorphForm : InterlinComboHandler
		{
			internal IhMorphForm()
				: base()
			{
			}
			internal IhMorphForm(SandboxBase sandbox)
				: base(sandbox)
			{
			}

			public override int IndexOfCurrentItem
			{
				get
				{
					return 0; // Treat the first item as the selected item.
				}
			}

			public override void SetupCombo()
			{
				CheckDisposed();

				base.SetupCombo();
				// Any time we pop this up, the text in the box is the text form of the current
				// analysis, as a starting point.
				ITsStrBldr builder = TsStrBldrClass.Create();
				int cmorphs = MorphCount;
				Debug.Assert(cmorphs != 0); // we're supposed to be building on one of them!

				var wordform = m_sandbox.GetWordformOfAnalysis();
				IWfiAnalysis wa = m_sandbox.GetWfiAnalysisInUse();

				// Find the actual original form of the current wordform
				ITsString tssForm = m_sandbox.FindAFullWordForm(wordform);
				string form = StrFromTss(tssForm);
				bool fBaseWordIsPhrase = SandboxBase.IsPhrase(form);

				// First, store the current morph breakdown if we have one,
				// Otherwise, if the user has deleted all the morphemes on the morpheme line
				// (per LT-1621) simply use the original wordform.
				// NOTE: Normally we would use Sandbox.IsMorphFormLineEmpty for this condition
				// but since we're already using the variable(s) needed for this check,
				// here we'll use those variables for economy/performance instead.
				string currentBreakdown = m_sandbox.SandboxEditMonitor.BuildCurrentMorphsString();
				if (currentBreakdown != string.Empty)
				{
					ComboList.Text = currentBreakdown;
					// The above and every other distinct morpheme breakdown from owned
					// WfiAnalyses are possible choices.
					ITsString tssText = TsStrFactoryClass.Create().
						MakeString(currentBreakdown, m_wsVern);
					ComboList.Items.Add(tssText);
				}
				else
				{
					ComboList.Text = form;
					ComboList.Items.Add(tssForm);
				}
				// if we added the fullWordform (or the current breakdown is somehow empty although we may have an analysis), then add the
				// wordform HVO; otherwise, add the analysis HVO.
				if (currentBreakdown == string.Empty || (wa == null && tssForm != null && tssForm.Equals(ComboList.Items[0] as ITsString)))
					m_items.Add(wordform != null ? wordform.Hvo : 0);
				else
					m_items.Add(wa != null ? wa.Hvo : 0);	// [wfi] hvoAnalysis may equal '0' (for annotations that are instances of Wordform).
				Debug.Assert(m_items.Count == ComboList.Items.Count,
					"combo list (m_comboList) should contain the same count as the m_items list (hvos)");
				AddAnalysesOf(wordform, fBaseWordIsPhrase);
				// Add the original wordform, if not already present.
				AddIfNotPresent(tssForm, wordform);
				ComboList.SelectedIndex = this.IndexOfCurrentItem;

				// Add any relevant 'other case' forms.
				int wsVern = m_sandbox.RawWordformWs;
				string locale = m_caches.MainCache.ServiceLocator.WritingSystemManager.Get(wsVern).IcuLocale;
				CaseFunctions cf = new CaseFunctions(locale);
				switch (m_sandbox.CaseStatus)
				{
					case StringCaseStatus.allLower:
						break; // no more to add
					case StringCaseStatus.title:
						AddOtherCase(cf.SwitchTitleAndLower(form));
						break;
					case StringCaseStatus.mixed:
						switch (cf.StringCase(form))
						{
							case StringCaseStatus.allLower:
								AddOtherCase(cf.ToTitle(form));
								AddOtherCase(m_sandbox.RawWordform.Text);
								break;
							case StringCaseStatus.title:
								AddOtherCase(cf.ToLower(form));
								AddOtherCase(m_sandbox.RawWordform.Text);
								break;
							case StringCaseStatus.mixed:
								AddOtherCase(cf.ToLower(form));
								AddOtherCase(cf.ToTitle(form));
								break;
						}
						break;
				}
				Debug.Assert(m_items.Count == ComboList.Items.Count,
					"combo list (m_comboList) should contain the same count as the m_items list (hvos)");
				ComboList.Items.Add(ITextStrings.ksEditMorphBreaks_);
			}

			/// <summary>
			/// Add to the combo the specified alternate-case form of the word.
			/// </summary>
			/// <param name="other"></param>
			void AddOtherCase(string other)
			{
				// 0 is a reserved value for other case wordform
				AddIfNotPresent(TsStringUtils.MakeTss(other, m_sandbox.RawWordformWs), null);
			}

			/// <summary>
			/// Add to the combo the analyses of the specified wordform (that don't already occur).
			/// REFACTOR : possibly could refactor with SandboxEditMonitor.BuildCurrentMorphsString
			/// </summary>
			private void AddAnalysesOf(IWfiWordform wordform, bool fBaseWordIsPhrase)
			{
				if (wordform == null)
					return; // no real wordform, can't have analyses.
				ITsStrBldr builder = TsStrBldrClass.Create();
				ITsString space = TsStrFactoryClass.Create().
					MakeString(fBaseWordIsPhrase ? "  " : " ", m_wsVern);
				foreach (IWfiAnalysis wa in wordform.AnalysesOC)
				{
					Opinions o = wa.GetAgentOpinion(
						m_caches.MainCache.LangProject.DefaultUserAgent);
					if (o == Opinions.disapproves)
						continue;	// skip any analysis the user has disapproved.
					int cmorphs = wa.MorphBundlesOS.Count;
					if (cmorphs == 0)
						continue;
					builder.Clear();
					for (int imorph = 0; imorph < cmorphs; ++imorph)
					{
						if (imorph != 0)
							builder.ReplaceTsString(builder.Length, builder.Length, space);
						IWfiMorphBundle mb = wa.MorphBundlesOS[imorph];
						IMoForm morph = mb.MorphRA;
						if (morph != null)
						{
							ITsString tss = morph.Form.get_String(m_sandbox.RawWordformWs);
							var morphType = morph.MorphTypeRA;
							string sPrefix = morphType.Prefix;
							string sPostfix = morphType.Postfix;
							int ich = builder.Length;
							builder.ReplaceTsString(ich, ich, tss);
							if (sPrefix != null && sPrefix.Length != 0)
								builder.Replace(ich, ich, sPrefix, null);
							if (sPostfix != null && sPostfix.Length != 0)
								builder.Replace(builder.Length, builder.Length,
									sPostfix, null);
						}
						else
						{
							// No MoMorph object?  must be the Form string.
							ITsString tss = mb.Form.get_String(m_sandbox.RawWordformWs);
							builder.ReplaceTsString(builder.Length, builder.Length, tss);
						}
					}
					ITsString tssAnal = builder.GetString();
					// Add only non-whitespace morpheme breakdowns.
					if (tssAnal.Length > 0 && tssAnal.Text.Trim().Length > 0)
						AddIfNotPresent(tssAnal, wa);
				}
			}

			/// <summary>
			/// Add an item to the combo unless it is already present.
			/// </summary>
			/// <param name="tssAnal"></param>
			void AddIfNotPresent(ITsString tssAnal, ICmObject analysisObj)
			{
				// Can't use m_comboList.Items.Contains() because it doesn't use our Equals
				// function and just notes that all the TsStrings are different objects.
				bool fFound = false;
				foreach (ITsString tss in ComboList.Items)
				{
					if (tss.Equals(tssAnal))
					{
						fFound = true;
						break;
					}
				}
				if (!fFound)
				{
					ComboList.Items.Add(tssAnal);
					m_items.Add(analysisObj != null ? analysisObj.Hvo : 0);
				}

			}

			public override bool HandleReturnKey()
			{
				CheckDisposed();

				IVwCacheDa cda = (IVwCacheDa)m_caches.DataAccess;
				ISilDataAccess sda = m_caches.DataAccess;
				int cmorphs = MorphCount;
				// JohnT: 0 is fine, that's what we see for a word which has no known analyses and
				// shows up as *** on the morphs line.
				//Debug.Assert(cmorphs != 0);
				for (int imorph = 0; imorph < cmorphs; ++imorph)
				{
					int hvoMbSec = MorphHvo(imorph);
					// Erase all the information.
					cda.CacheObjProp(hvoMbSec, ktagSbMorphForm, 0);
					cda.CacheObjProp(hvoMbSec, ktagSbMorphEntry, 0);
					cda.CacheObjProp(hvoMbSec, ktagSbMorphGloss, 0);
					cda.CacheObjProp(hvoMbSec, ktagSbMorphPos, 0);
					cda.CacheStringProp(hvoMbSec, ktagSbMorphPrefix, null);
					cda.CacheStringProp(hvoMbSec, ktagSbMorphPostfix, null);
					// Send notifiers for each of these deleted items.
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoMbSec, ktagSbMorphForm, 0, 1, 1);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoMbSec, ktagSbMorphEntry, 0, 0, 1);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoMbSec, ktagSbMorphGloss, 0, 0, 1);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoMbSec, ktagSbMorphPos, 0, 0, 1);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoMbSec, ktagSbMorphPrefix, 0, 0, 1);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoMbSec, ktagSbMorphPostfix, 0, 0, 1);
				}
				// Now erase the morph bundles themselves.
				cda.CacheVecProp(m_hvoSbWord, ktagSbWordMorphs, new int[0], 0);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_hvoSbWord, ktagSbWordMorphs, 0, 0, 1);

				MorphemeBreaker mb = new MorphemeBreaker(m_caches, ComboList.Text,
					m_hvoSbWord, m_wsVern, m_sandbox);
				mb.Run();
				m_rootb.Reconstruct(); // Everything changed, more or less.
				// Todo: having called reconstruct, selection is invalid, may have to do
				// something special about making a new one.
				return true;
			}

			public override void HandleSelect(int index)
			{
				CheckDisposed();

				string sMorphs = null;
				if (index >= m_items.Count)
				{
					// The user did not choose an existing set of morph breaks, which means that
					// he wants to bring up a dialog to edit the morph breaks manually.
					sMorphs = EditMorphBreaks();
				}
				else
				{
					// user selected an existing set of morph breaks.
					ITsString menuItemForm = (ComboList.Items[index]) as ITsString;
					Debug.Assert(menuItemForm != null, "menu item should be TsString");
					int hvoAnal = m_items[index];
					if (hvoAnal == 0)
					{
						// We're looking at an alternate case form of the whole word.
						// Switch the sandbox to the corresponding form.
						m_sandbox.SetWordform(menuItemForm, true);
						return;
					}
					else
					{
						// use the new morph break down.
						sMorphs = StrFromTss(menuItemForm);
					}
				}
				UpdateMorphBreaks(sMorphs);
				m_sandbox.SelectIconOfMorph(0, ktagMorphFormIcon);
			}

			internal void UpdateMorphBreaks(string sMorphs)
			{
				if (sMorphs != null && sMorphs.Trim().Length > 0)
					sMorphs = sMorphs.Trim();
				else
					return;

				ISilDataAccess sda = m_caches.DataAccess;
				IVwCacheDa cda = (IVwCacheDa)m_caches.DataAccess;

				// Compare to the actual original form of the sandbox wordform
				var wf = m_sandbox.GetWordformOfAnalysis();
				ITsString tssWordform = m_sandbox.FindAFullWordForm(wf);
				string wordform = StrFromTss(tssWordform);
				if (wordform == sMorphs)
				{
					// The only wordform choice in the list is the wordform of
					// some current analysis. We want to switch to that original wordform.
					// We do NOT want to look up the default, because that could well have an
					// existing morpheme breakdown, preventing us from getting back to the original
					// whole word.
					m_sandbox.SetWordform(tssWordform, false);
					return;
				}
				// We want to try to break this down into morphemes.
				// nb: use sda.Replace rather than cds.CachVecProp so that this registers as a change
				// in need of saving.
				int coldMorphs = sda.get_VecSize(m_hvoSbWord, ktagSbWordMorphs);
				sda.Replace(m_hvoSbWord, ktagSbWordMorphs, 0, coldMorphs, new int[0], 0);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_hvoSbWord, ktagSbWordMorphs, 0, 0, coldMorphs);
				MorphemeBreaker mb = new MorphemeBreaker(m_caches, sMorphs, m_hvoSbWord,
					m_wsVern, m_sandbox);
				mb.Run();
				m_rootb.Reconstruct(); // Everything changed, more or less.
				// We've changed properties that the morph manager cares about, but we don't want it
				// to fire when we fix the selection.
				m_sandbox.m_editMonitor.NeedMorphemeUpdate = false;
			}

			/// <summary>
			///
			/// </summary>
			/// <returns>string of new morph breaks</returns>
			internal string EditMorphBreaks()
			{
				string sMorphs = null;
				using (var dlg = new EditMorphBreaksDlg(((IxWindow)m_sandbox.FindForm()).Mediator.HelpTopicProvider))
				{
					ITsString tssWord = m_sandbox.SbWordForm(m_sandbox.RawWordformWs);
					sMorphs = m_sandbox.SandboxEditMonitor.BuildCurrentMorphsString();
					dlg.Initialize(tssWord, sMorphs, m_caches.MainCache.MainCacheAccessor.WritingSystemFactory,
						m_caches.MainCache, m_sandbox.Mediator.StringTbl, m_sandbox.StyleSheet);
					Form mainWnd = m_sandbox.FindForm();
					// Making the form active fixes problems like LT-2619.
					// I'm (RandyR) not sure what adverse impact might show up by doing this.
					mainWnd.Activate();
					if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
						sMorphs = dlg.GetMorphs();
					else
						sMorphs = null;
				}
				return sMorphs;
			}
		}

		/// <summary>
		/// This combo box appears in the same place in the view as the IhMorphForm one, but
		/// when an analysis is missing. Currently it has the same options
		/// as the IhMorphForm, but the process of building the combo is slightly different
		/// because the initial text is taken from the word form, not the morph forms. There
		/// may eventually be other differences, such as subtracting an item to delete the
		/// current analysis.
		/// </summary>
		internal class IhMissingMorphs : IhMorphForm
		{
			public override void SetupCombo()
			{
				CheckDisposed();

				InitCombo();
				ComboList.Text = StrFromTss(m_caches.DataAccess.get_MultiStringAlt(m_hvoSbWord,
					ktagSbWordForm, m_sandbox.RawWordformWs));
				ComboList.Items.Add(ITextStrings.ksEditMorphBreaks_);
			}
		}


		/// <summary>
		/// Handles the morpheme entry (LexEntry) line when none is known.
		/// </summary>
		internal class IhMissingEntry : InterlinComboHandler
		{
			private IHelpTopicProvider m_helpTopicProvider;
			ITsString m_tssMorphForm; // form of the morpheme when the combo was initialized.
			bool m_fHideCombo = true; // flag to HideCombo after HandleSelect.
			// int for all classes, except IhMissingEntry, which stuffs MorphItem data into it.
			// So, that ill-behaved class has to make its own m_items data member.
			List<MorphItem> m_morphItems = new List<MorphItem>();

			internal class MorphItemOptions
			{
				internal int HvoMoForm;
				internal int HvoEntry;
				internal int HvoSense;
				internal int HvoMsa;
				internal ILexEntryInflType InflType;
				internal ILexEntryRef EntryRef;
				internal ITsString TssName;
				internal string SenseName;
				internal string MsaName;
			}

			internal struct MorphItem : IComparable
			{
				/// <summary>
				/// hvo of the morph form of an entry
				/// </summary>
				public int m_hvoMorph;
				/// <summary>
				/// typically derived from the variant component lexeme
				/// </summary>
				public int m_hvoMainEntryOfVariant;
				public int m_hvoSense;
				public int m_hvoMsa;
				public ILexEntryInflType m_inflType;
				public ILexEntryRef m_entryRef;
				public ITsString m_name;
				public string m_nameSense;
				public string m_nameMsa;

				public MorphItem(MorphItemOptions options)
					: this(options.HvoMoForm, options.HvoEntry, options.TssName, options.HvoSense, options.SenseName, options.HvoMsa, options.MsaName)
				{
					m_inflType = options.InflType;
					m_entryRef = options.EntryRef;
					if (m_entryRef != null)
					{
						var entry = GetMainEntryOfVariant(m_entryRef);
						m_hvoMainEntryOfVariant = entry.Hvo;
					}
				}

				public MorphItem(int hvoMorph, ITsString tssName)
					: this(hvoMorph, 0, tssName)
				{
				}

				public MorphItem(int hvoMorph, int hvoMainEntryOfVariant, ITsString tssName)
					: this(hvoMorph, hvoMainEntryOfVariant, tssName, 0, null, 0, null)
				{
				}

				public MorphItem(int hvoMorph, ITsString tssName, int hvoSense, string nameSense, int hvoMsa, string nameMsa)
					: this(hvoMorph, 0, tssName, hvoSense, nameSense, hvoMsa, nameMsa)
				{
				}

				/// <summary>
				///
				/// </summary>
				/// <param name="hvoMorph">IMoForm (e.g. wmb.MorphRA)</param>
				/// <param name="hvoMainEntryOfVariant">for variant specs, this is hvoMorph's Entry.VariantEntryRef.ComponentLexeme target, 0 otherwise</param>
				/// <param name="tssName"></param>
				/// <param name="hvoSense">ILexSense (e.g. wmb.SensaRA)</param>
				/// <param name="nameSense"></param>
				/// <param name="hvoMsa">IMoMorphSynAnalysis (e.g. wmb.MsaRA)</param>
				/// <param name="nameMsa"></param>
				public MorphItem(int hvoMorph, int hvoMainEntryOfVariant, ITsString tssName, int hvoSense, string nameSense, int hvoMsa, string nameMsa)
				{
					m_hvoMorph = hvoMorph;
					m_hvoMainEntryOfVariant = hvoMainEntryOfVariant;
					m_name = tssName;
					m_hvoSense = hvoSense;
					m_nameSense = nameSense;
					m_hvoMsa = hvoMsa;
					m_nameMsa = nameMsa;
					m_inflType = null;
					m_entryRef = null;
				}

				/// <summary>
				/// for variant relationships, return the primary entry
				/// (of which this morph is a variant). Otherwise,
				/// return the owning entry of the morph.
				/// </summary>
				/// <param name="cache"></param>
				/// <returns></returns>
				public ILexEntry GetPrimaryOrOwningEntry(FdoCache cache)
				{
					var repository = cache.ServiceLocator.GetInstance<ICmObjectRepository>();
					ILexEntry morphEntryReal = null;
					if (m_hvoMainEntryOfVariant != 0)
					{
						// for variant relationships, we want to allow trying to create a
						// new sense on the entry of which we are a variant.
						morphEntryReal = repository.GetObject(m_hvoMainEntryOfVariant) as ILexEntry;
					}
					else
					{
						var morph = repository.GetObject(m_hvoMorph);
						morphEntryReal = morph.Owner as ILexEntry;
					}
					return morphEntryReal;
				}

				#region IComparer Members

				/// <summary>
				/// make sure SetupCombo groups morph items according to lex name, sense,
				/// and msa names in that order. (LT-5848).
				/// </summary>
				/// <param name="x"></param>
				/// <param name="y"></param>
				/// <returns></returns>
				public int Compare(object x, object y)
				{
					var miX = (MorphItem)x;
					var miY = (MorphItem)y;

					// first compare the lex and sense names.
					if (miX.m_name == null || miY.m_name == null) //handle sort under null conditions
					{
						if (miY.m_name != null)
						{
							return -1;
						}
						if (miX.m_name != null)
						{
							return 1;
						}
					}
					else
					{
						var compareLexNames = String.Compare(miX.m_name.Text, miY.m_name.Text);
						if (compareLexNames != 0)
							return compareLexNames;
					}

					// otherwise if the hvo's are the same, then we want the ones with senses to be higher.
					// when m_hvoSense equals '0' we want to insert "Add New Sense" for that lexEntry,
					// following all the other senses for that lexEntry.
					if (miX.m_hvoMorph == miY.m_hvoMorph)
					{
						if (miX.m_hvoSense == 0)
							return 1;
						else if (miY.m_hvoSense == 0)
							return -1;
					}
					// only compare sense names for the same morph
					if (miX.m_hvoMorph == miY.m_hvoMorph)
					{
						int compareSenseNames = String.Compare(miX.m_nameSense, miY.m_nameSense);
						if (compareSenseNames != 0)
						{
							// if we have inflectional affix information, order them according to their order in LexEntryRef.VariantEntryTypes.
							if (miX.m_entryRef != null && miY.m_entryRef != null &&
								miX.m_entryRef.Hvo == miY.m_entryRef.Hvo)
							{
								var commonVariantEntryTypesRs = miX.m_entryRef.VariantEntryTypesRS;
								if (miX.m_inflType == null || miY.m_inflType == null) //handle sort under null conditions
								{
									if (miY.m_inflType != null)
									{
										return -1;
									}
									if (miX.m_inflType != null)
									{
										return 1;
									}
								}
								else
								{
									var iX = commonVariantEntryTypesRs.IndexOf(miX.m_inflType);
									var iY = commonVariantEntryTypesRs.IndexOf(miY.m_inflType);
									if (iX > iY)
										return 1;
									if (iX < iY)
										return -1;
								}
							}
							return compareSenseNames;
						}

						var msaCompare = String.Compare(miX.m_nameMsa, miY.m_nameMsa);
						if (msaCompare != 0)
							return msaCompare;
					}
					// otherwise, try to regroup common lex morphs together.
					return miX.m_hvoMorph.CompareTo(miY.m_hvoMorph);
				}

				#endregion

				#region IComparable Members

				public int CompareTo(object obj)
				{
					return Compare(this, obj);
				}

				#endregion

			};

			/// <summary>
			/// Determines if the two MorphItems are based on the same objects, ignoring string values.
			/// </summary>
			/// <param name="x"></param>
			/// <param name="y"></param>
			/// <returns></returns>
			bool HaveSameObjs(MorphItem x, MorphItem y)
			{
				return x.m_hvoSense == y.m_hvoSense &&
					   x.m_hvoMainEntryOfVariant == y.m_hvoMainEntryOfVariant &&
					   x.m_hvoMorph == y.m_hvoMorph &&
					   x.m_hvoMsa == y.m_hvoMsa &&
					   x.m_inflType == y.m_inflType &&
					   x.m_entryRef == y.m_entryRef;
			}

			static int HvoOrZero(ICmObject co)
			{
				return co == null ? 0 : co.Hvo;
			}

			internal MorphItem CreateCoreMorphItemBasedOnSandboxCurrentState()
			{
				var hvoWmb = m_hvoMorph;

				int hvoMorphSense = m_caches.DataAccess.get_ObjectProp(hvoWmb, ktagSbMorphGloss);
				int hvoInflType = m_caches.DataAccess.get_ObjectProp(hvoWmb, ktagSbNamedObjInflType);
				ILexEntryInflType inflType = null;
				if (hvoInflType != 0)
					inflType = m_caches.MainCache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(m_caches.RealHvo(hvoInflType));
				int hvoMSA = m_caches.DataAccess.get_ObjectProp(hvoWmb, ktagSbMorphPos);
				int hvoMorphEntry = m_caches.DataAccess.get_ObjectProp(hvoWmb, ktagSbMorphEntry);
				ILexEntry realEntry = null;
				IMoForm mf = null;
				if (hvoMorphEntry != 0)
				{
					realEntry =
						m_caches.MainCache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(m_caches.RealHvo(hvoMorphEntry));
					mf = realEntry.LexemeFormOA;
				}
				ILexSense realSense = null;
				ILexEntryRef ler = null;
				if (hvoMorphSense != 0)
				{
					realSense = m_caches.MainCache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(m_caches.RealHvo(hvoMorphSense));
					if (realEntry != null)
						realEntry.IsVariantOfSenseOrOwnerEntry(realSense, out ler);
				}

				//var mi = new MorphItem(options);
				var mi = GetMorphItem(mf, null, realSense, null, ler, HvoOrZero(realEntry), inflType);
				return mi;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="IhMissingEntry"/> class.
			/// </summary>
			/// <param name="helpTopicProvider">The help topic provider.</param>
			/// --------------------------------------------------------------------------------
			internal IhMissingEntry(IHelpTopicProvider helpTopicProvider)
			{
				m_helpTopicProvider = helpTopicProvider;
			}

			#region IDisposable override

			/// <summary>
			/// Executes in two distinct scenarios.
			///
			/// 1. If disposing is true, the method has been called directly
			/// or indirectly by a user's code via the Dispose method.
			/// Both managed and unmanaged resources can be disposed.
			///
			/// 2. If disposing is false, the method has been called by the
			/// runtime from inside the finalizer and you should not reference (access)
			/// other managed objects, as they already have been garbage collected.
			/// Only unmanaged resources can be disposed.
			/// </summary>
			/// <param name="disposing"></param>
			/// <remarks>
			/// If any exceptions are thrown, that is fine.
			/// If the method is being done in a finalizer, it will be ignored.
			/// If it is thrown by client code calling Dispose,
			/// it needs to be handled by fixing the bug.
			///
			/// If subclasses override this method, they should call the base implementation.
			/// </remarks>
			protected override void Dispose(bool disposing)
			{
				//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
				// Must not be run more than once.
				if (IsDisposed)
					return;

				if (disposing)
				{
					// Dispose managed resources here.
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_tssMorphForm = null;

				base.Dispose(disposing);
			}

			#endregion IDisposable override

			public override List<int> Items
			{
				get
				{
					if (m_items.Count == 0)
						SyncItemsToMorphItems();
					return base.Items;
				}
			}

			internal List<MorphItem> MorphItems
			{
				get { return m_morphItems; }
			}

			private void SyncItemsToMorphItems()
			{
				// re-populate the items with the most specific levels of analysis.
				m_items.Clear();
				foreach (MorphItem mi in m_morphItems)
				{
					if (mi.m_hvoSense > 0)
						m_items.Add(mi.m_hvoSense);
					else if (mi.m_hvoMorph > 0)
						m_items.Add(mi.m_hvoMorph);	// should be owned by LexEntry
					else
						throw new ArgumentException("invalid morphItem");
				}
			}

			internal void LoadMorphItems()
			{
				ISilDataAccess sda = m_caches.DataAccess;
				int hvoForm = sda.get_ObjectProp(m_hvoMorph, ktagSbMorphForm);
				ITsString tssMorphForm = sda.get_MultiStringAlt(hvoForm, ktagSbNamedObjName, m_sandbox.RawWordformWs);
				string sPrefix = StrFromTss(sda.get_StringProp(m_hvoMorph, ktagSbMorphPrefix));
				string sPostfix = StrFromTss(sda.get_StringProp(m_hvoMorph, ktagSbMorphPostfix));
				IEnumerable<IMoForm> morphs = MorphServices.GetMatchingMorphs(m_caches.MainCache, sPrefix, tssMorphForm, sPostfix);
				m_tssMorphForm = tssMorphForm;
				m_morphItems.Clear();
				foreach (IMoForm mf in morphs)
				{
					ILexEntry parentEntry = mf.Owner as ILexEntry;
					BuildMorphItemsFromEntry(mf, parentEntry, null);

					Debug.Assert(parentEntry != null, "MoForm Owner shouldn't be null.");
					var variantRefs =	from entryRef in parentEntry.EntryRefsOS
										where entryRef.VariantEntryTypesRS != null && entryRef.VariantEntryTypesRS.Count > 0
										select entryRef;
					// for now, just build morph items for variant EntryRefs having only one component
					// otherwise, it's ambiguous which component to use to build a WfiAnalysis with.
					foreach (var ler in variantRefs.Where(ler=>ler.ComponentLexemesRS.Count == 1))
					{
						ILexEntry mainEntryOfVariant = GetMainEntryOfVariant(ler);
						BuildMorphItemsFromEntry(mf, mainEntryOfVariant, ler);
					}
				}
			}

			private static ILexEntry GetMainEntryOfVariant(ILexEntryRef ler)
			{
				IVariantComponentLexeme component = ler.ComponentLexemesRS[0] as IVariantComponentLexeme;
				ILexEntry mainEntryOfVariant = null;
				if (component.ClassID == LexEntryTags.kClassId)
					mainEntryOfVariant = component as ILexEntry;
				else if (component.ClassID == LexSenseTags.kClassId)
					mainEntryOfVariant = (component as ILexSense).Entry;
				return mainEntryOfVariant;
			}


			/// <summary>
			///
			/// </summary>
			/// <param name="mf"></param>
			/// <param name="le">the entry used in the morph bundle (for sense info). typically
			/// this is an owner of hvoMorph, but if not, it most likely has hvoMorph linked as its variant.</param>
			private void BuildMorphItemsFromEntry(IMoForm mf, ILexEntry le, ILexEntryRef ler)
			{
				int hvoLexEntry = 0;
				if (le != null)
					hvoLexEntry = le.Hvo;
				ITsString tssName = null;
				if (le != null)
				{
					tssName = LexEntryVc.GetLexEntryTss(m_caches.MainCache, le.Hvo, m_wsVern, ler);
				}
				else
				{
					// looks like we're not in a good state, so just use the form for the name.
					int wsActual;
					tssName = mf.Form.GetAlternativeOrBestTss(m_wsVern, out wsActual);
				}
				var wsAnalysis = m_caches.MainCache.ServiceLocator.WritingSystemManager.Get(m_caches.MainCache.DefaultAnalWs);

				// Populate morphItems with Sense/Msa level specifics
				if (le != null)
				{
					foreach (ILexSense sense in le.AllSenses)
					{
						var tssSense = sense.Gloss.get_String(wsAnalysis.Handle);
						if (ler != null)
						{
							MorphItem mi;
							var lexEntryInflTypes =
									ler.VariantEntryTypesRS.Where(let => let is ILexEntryInflType).Select(let => let as ILexEntryInflType);
							if (lexEntryInflTypes.Count() > 0)
							{
								foreach (var inflType in lexEntryInflTypes)
								{
									var glossAccessor = (tssSense.Length == 0 ? (IMultiStringAccessor) sense.Definition : sense.Gloss);
									tssSense = MorphServices.MakeGlossOptionWithInflVariantTypes(inflType, glossAccessor, wsAnalysis);
									mi = GetMorphItem(mf, tssName, sense, tssSense, ler, hvoLexEntry, inflType);
									m_morphItems.Add(mi);
								}
							}
							else
							{
								AddMorphItemToList(mf, ler, tssSense, sense, wsAnalysis, tssName, hvoLexEntry);
							}
						}
						else
						{
							AddMorphItemToList(mf, null, tssSense, sense, wsAnalysis, tssName, hvoLexEntry);
						}
					}
				}
				// Make a LexEntry level item
				m_morphItems.Add(new MorphItem(mf.Hvo, ler != null ? hvoLexEntry : 0, tssName));
			}

			private void AddMorphItemToList(IMoForm mf, ILexEntryRef ler, ITsString tssSense, ILexSense sense,
											CoreWritingSystemDefinition wsAnalysis, ITsString tssName, int hvoLexEntry)
			{
				MorphItem mi;
				if (tssSense.Length == 0)
				{
					// If it doesn't have a gloss (e.g., from Categorised Entry), use the definition.
					tssSense = sense.Definition.get_String(wsAnalysis.Handle);
				}
				mi = GetMorphItem(mf, tssName, sense, tssSense, ler, hvoLexEntry, null);
				m_morphItems.Add(mi);
			}

			private static MorphItem GetMorphItem(IMoForm mf, ITsString tssName, ILexSense sense, ITsString tssSense,
				ILexEntryRef ler, int hvoLexEntry, ILexEntryInflType inflType)
			{
				IMoMorphSynAnalysis msa = null;
				string msaText = null;
				if (sense != null)
				{
					msa = sense.MorphoSyntaxAnalysisRA;
					if (msa != null)
						msaText = msa.InterlinearName;
				}

				var options = new MorphItemOptions
				{
					HvoMoForm = HvoOrZero(mf),
					HvoEntry = ler != null ? hvoLexEntry : 0,
					TssName = tssName,
					HvoSense = HvoOrZero(sense),
					SenseName = tssSense != null ? tssSense.Text : null,
					HvoMsa = HvoOrZero(msa),
					MsaName = msaText,
					InflType = inflType,
					EntryRef = ler,
				};
				return new MorphItem(options);
			}

			internal class MorphComboItem : InterlinComboHandlerActionComboItem
			{
				MorphItem m_mi;
				internal MorphComboItem(MorphItem mi, ITsString tssDisplay, EventHandler handleMorphComboItem, int hvoPrimary)
					: base(tssDisplay, handleMorphComboItem, hvoPrimary, 0)
				{
					m_mi = mi;
				}

				/// <summary>
				///
				/// </summary>
				internal MorphItem MorphItem
				{
					get { return m_mi; }
				}

			}

			// m_morphItems is a list of MorphItems, which contain both the main-cache hvo of the
			// MoForm with the right text in the m_wsVern alternative of its MoForm_Form, and
			// the main-cache hvo of each sense of that MoForm.  A sense hvo of 0 is used to
			// flag the "Add New Sense" line which ends each MoForm's list of sense.
			//
			// Items in the menu are the shortnames of the owning LexEntries, followed by.
			//
			/// <summary>
			///
			/// </summary>
			public override void SetupCombo()
			{
				CheckDisposed();

				base.SetupCombo();

				LoadMorphItems();
				AddMorphItemsToComboList();
				// DON'T ADD ANY MORE MorphItem OBJECTS TO m_morphItems AT THIS POINT!
				// The order of items added to m_comboList.Items below must match exactly the
				// switch statement at the beginning of HandleSelect().
				AddUnknownLexEntryToComboList();
				// If morphemes line is empty then make the Create New Entry
				// appear disabled (cf. LT-6480). If user tries to select this index,
				// we prevent the selection in our HandleSelect override.
				ITsTextProps disabledItemProperties = null;
				if (m_sandbox.IsMorphFormLineEmpty)
					disabledItemProperties = DisabledItemProperties();
				AddItemToComboList(ITextStrings.ksCreateNewEntry_,
					new EventHandler(OnSelectCreateNewEntry),
					disabledItemProperties,
					disabledItemProperties == null);
				AddItemToComboList(ITextStrings.ksVariantOf_,
					new EventHandler(OnSelectVariantOf),
					disabledItemProperties,
					disabledItemProperties == null);

				// If morphemes line is empty then make the allomorph selection,
				// appear disabled (cf. LT-1621). If user tries to select this index,
				// we prevent the selection in our HandleComboSelChange override.
				AddItemToComboList(ITextStrings.ksAllomorphOf_,
					new EventHandler(OnSelectAllomorphOf),
					disabledItemProperties,
					disabledItemProperties == null);

				// If the morpheme line is hidden, give the user the option to edit morph breaks.
				if (m_sandbox.m_choices.IndexOf(InterlinLineChoices.kflidMorphemes) < 0)
				{
					AddItemToComboList("-------", null, null, false);
					AddItemToComboList(ITextStrings.ksEditMorphBreaks_,
						new EventHandler(OnSelectEditMorphBreaks),
						null,
						true);
				}

				// Set combo selection to current selection.
				ComboList.SelectedIndex = this.IndexOfCurrentItem;
			}

			private void AddMorphItemsToComboList()
			{
				var coRepository = m_caches.MainCache.ServiceLocator.GetInstance<ICmObjectRepository>();
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				MorphItem miPrev = new MorphItem();
				m_morphItems.Sort();
				foreach (MorphItem mi in m_morphItems)
				{
					ITsString tssToDisplay = null;
					int hvoPrimary = 0; // the key hvo associated with the combo item.
					tisb.Clear();

					var morph = coRepository.GetObject(mi.m_hvoMorph);
					var le = morph.Owner as ILexEntry;
					if (mi.m_hvoSense > 0)
					{
						int hvoSense = mi.m_hvoSense;
						tisb.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
							(int)FwTextPropVar.ktpvEnum,
							(int)FwSuperscriptVal.kssvOff);
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
							m_wsAnal);
						tisb.Append("  ");

						ITsString tssSense = TsStringUtils.MakeTss(mi.m_nameSense,
							m_caches.MainCache.DefaultAnalWs);

						tisb.AppendTsString(tssSense);
						tisb.Append(", ");

						string sPos = mi.m_nameMsa;
						if (sPos == null)
							sPos = ITextStrings.ksQuestions;	// was "??", not "???"
						tisb.Append(sPos);
						tisb.Append(", ");

						// append lex entry form info
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
							m_wsVern);
						tisb.AppendTsString(mi.m_name);

						tssToDisplay = tisb.GetString();
						hvoPrimary = mi.m_hvoSense;
						tisb.Clear();
					}
					else
					{
						hvoPrimary = mi.m_hvoMorph;
						// mi.m_hvoSense == 0
						// Make a comboList item for adding a new sense to the LexEntry
						if (miPrev.m_hvoMorph != 0 && mi.m_hvoMorph == miPrev.m_hvoMorph &&
							miPrev.m_hvoSense > 0)
						{
							// "Add New Sense..."
							// the comboList has already added selections for senses and lexEntry form
							// thus establishing the LexEntry the user may wish to "Add New Sense..." to.
							tisb.Clear();
							tisb.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
								(int)FwTextPropVar.ktpvEnum,
								(int)FwSuperscriptVal.kssvOff);
							tisb.SetIntPropValues((int)FwTextPropType.ktptBold,
								(int)FwTextPropVar.ktpvEnum,
								(int)FwTextToggleVal.kttvOff);
							tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
								m_wsUser);
							tisb.Append(ITextStrings.ksAddNewSense_);
							tssToDisplay = tisb.GetString();

						}
						else
						{
							// "Add New Sense for {0}"
							// (EricP) This path means the current form matches an entry that (strangely enough)
							// doesn't have any senses so we need to add the LexEntry form into the string,
							// so the user knows what Entry they'll be adding the new sense to.
							Debug.Assert(le.SensesOS.Count == 0, "Expected LexEntry to have no senses.");
							string sFmt = ITextStrings.ksAddNewSenseForX_;
							tisb.Clear();
							tisb.SetIntPropValues(
								(int)FwTextPropType.ktptSuperscript,
								(int)FwTextPropVar.ktpvEnum,
								(int)FwSuperscriptVal.kssvOff);
							tisb.SetIntPropValues((int)FwTextPropType.ktptBold,
								(int)FwTextPropVar.ktpvEnum,
								(int)FwTextToggleVal.kttvOff);
							tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
								m_wsUser);
							tisb.Append(sFmt);
							ITsString tss = tisb.GetString();
							int ich = sFmt.IndexOf("{0}");
							if (ich >= 0)
							{
								ITsStrBldr tsbT = tss.GetBldr();
								tsbT.ReplaceTsString(ich, ich + "{0}".Length, mi.m_name);
								tss = tsbT.GetString();
							}
							tssToDisplay = tss;
						}
					}
					// keep track of the previous MorphItem to track context.
					ComboList.Items.Add(new MorphComboItem(mi, tssToDisplay,
						new EventHandler(HandleSelectMorphComboItem), hvoPrimary));
					miPrev = mi;
				}
				SyncItemsToMorphItems();
			}

			private void AddUnknownLexEntryToComboList()
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.Clear();
				tisb.SetIntPropValues(
					(int)FwTextPropType.ktptSuperscript,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwSuperscriptVal.kssvOff);
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsUser);
				tisb.Append(ITextStrings.ksUnknown);
				ComboList.Items.Add(new InterlinComboHandlerActionComboItem(
					tisb.GetString(), new EventHandler(SetLexEntryToUnknown)));
			}

			private void AddItemToComboList(string itemName, EventHandler onSelect, ITsTextProps itemProperties, bool enableItem)
			{
				ITsStrBldr tsb = TsStrBldrClass.Create();
				tsb.Replace(tsb.Length, tsb.Length, itemName, itemProperties);
				tsb.SetIntPropValues(0, tsb.Length, (int)FwTextPropType.ktptWs, 0, m_wsUser);
				ComboList.Items.Add(new InterlinComboHandlerActionComboItem(
					tsb.GetString(),
					enableItem ? onSelect : null));
			}

			/// <summary>
			/// Return the index corresponding to the current LexEntry/Sense state of the Sandbox.
			/// </summary>
			public override int IndexOfCurrentItem
			{
				get
				{
					// See if we can find the real hvo corresponding to the LexEntry/Sense currently
					// selected in the Sandbox.
					int sbHvo = m_sandbox.CurrentLexEntriesAnalysis(m_hvoMorph);
					int realHvo = m_sandbox.Caches.RealHvo(sbHvo);
					if (realHvo <= 0)
						return base.IndexOfCurrentItem;
					//int index = ReturnIndexOfMorphItemMatchingCurrentAnalysisLevel(realHvo); // Debug only.
					var miCurrentSb = CreateCoreMorphItemBasedOnSandboxCurrentState();
					// Look through our relevant list items to see if we find a match.
					for (int i = 0; i < m_morphItems.Count; ++i)
					{
						MorphItem mi = m_morphItems[i];
						if (HaveSameObjs(mi, miCurrentSb))
							return i;
					}

					// save the class id
					//  return ReturnIndexOfMorphItemMatchingCurrentAnalysisLevel(realHvo);
					return base.IndexOfCurrentItem;
				}
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="realHvo">hvo of current analysis level</param>
			/// <returns></returns>
			private int ReturnIndexOfMorphItemMatchingCurrentAnalysisLevel(int realHvo)
			{
				var coRepository = m_caches.MainCache.ServiceLocator.GetInstance<ICmObjectRepository>();
				var co = coRepository.GetObject(realHvo);
				int classid = co.ClassID;
				IMoMorphSynAnalysis msa = co as IMoMorphSynAnalysis;

				// Look through our relevant list items to see if we find a match.
				for (int i = 0; i < m_morphItems.Count; ++i)
				{
					MorphItem mi = m_morphItems[i];
					switch (classid)
					{
						case LexSenseTags.kClassId:
							// See if we match the LexSense
							if (mi.m_hvoSense == realHvo)
							{
								return i;
							}

							break;
						case LexEntryTags.kClassId:
							// Otherwise, see if our LexEntry matches MoForm's owner (also a LexEntry)
							var morph = coRepository.GetObject(mi.m_hvoMorph);
							var entryReal = morph.Owner as ILexEntry;
							if (entryReal == co)
								return i;
							break;
						default:
							// See if we can match on the MSA
							if (msa != null && mi.m_hvoMsa == realHvo)
							{
								// verify the item sense is its owner
								var ls = coRepository.GetObject(mi.m_hvoSense) as ILexSense;
								if (msa == ls.MorphoSyntaxAnalysisRA)
									return i;
							}
							break;
					}
				}
				return base.IndexOfCurrentItem;
			}

			// This indicates there was a previous real LexEntry recorded. The 'real' subclass
			// overrides to answer 1. The value signifies the number of objects stored in the
			// ktagMorphEntry property before the user made a selection in the menu.
			internal virtual int WasReal()
			{
				CheckDisposed();

				return 0;
			}

			/// <summary>
			/// Run the dialog that allows the user to create a new LexEntry.
			/// </summary>
			private void RunCreateEntryDlg()
			{
				ILexEntry le;
				IMoForm allomorph;
				ILexSense sense;
				CreateNewEntry(false, out le, out allomorph, out sense);
			}

			internal void CreateNewEntry(bool fCreateNow, out ILexEntry le, out IMoForm allomorph, out ILexSense sense)
			{
				CheckDisposed();

				le = null;
				allomorph = null;
				sense = null;
				FdoCache cache = m_caches.MainCache;
				int hvoMorph = m_caches.DataAccess.get_ObjectProp(m_hvoMorph, ktagSbMorphForm);
				ITsString tssForm = m_caches.DataAccess.get_MultiStringAlt(hvoMorph,
																		   ktagSbNamedObjName, m_sandbox.RawWordformWs);
				// If we don't have a form or it isn't in a current vernacular writing system, give up.
				if (tssForm == null || tssForm.Length == 0 ||
					!WritingSystemServices.GetAllWritingSystems(m_caches.MainCache, "all vernacular", null, 0, 0).Contains(
						 TsStringUtils.GetWsOfRun(tssForm, 0)))
				{
					return;
				}
				var entryComponents = BuildEntryComponents();
				bool fCreateAllomorph = false;
				bool fCreatedEntry = false;
				if (fCreateNow)
				{
				   // just create a new entry based on the given information.
				   le  = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(entryComponents);
				}
				else
				{
					using (InsertEntryDlg dlg = InsertEntryNow.CreateInsertEntryDlg(fCreateNow))
					{
						dlg.SetDlgInfo(cache, m_sandbox.GetFullMorphForm(m_hvoMorph), m_sandbox.Mediator);
						dlg.TssGloss = entryComponents.GlossAlternatives.FirstOrDefault();
						foreach (ITsString tss in entryComponents.GlossAlternatives.Skip(1))
							dlg.SetInitialGloss(TsStringUtils.GetWsAtOffset(tss, 0), tss);
						dlg.ChangeUseSimilarToCreateAllomorph();

						if (fCreateNow)
						{
							// just create a new entry based on the given information.
							dlg.CreateNewEntry();
						}
						else
						{
							// bring up the dialog so the user can make further decisions.
							Form mainWnd = m_sandbox.FindForm();
							// Making the form active fixes LT-2344 & LT-2345.
							// I'm (RandyR) not sure what adverse impact might show up by doing this.
							mainWnd.Activate();
							// The combo should be automatically hidden by activating another window.
							// That works on Windows but not on Mono (reported as https://bugzilla.xamarin.com/show_bug.cgi?id=15848).
							// So to prevent the combo hanging around on Mono, we hide it explicitly here.
							HideCombo();
							dlg.SetHelpTopic("khtpInsertEntryFromInterlinear");
							if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
								fCreateAllomorph = true;
						}
						dlg.GetDialogInfo(out le, out fCreatedEntry);
						if (!fCreatedEntry && !fCreateAllomorph)
							return;
					}
				}
				if (fCreateAllomorph && le.SensesOS.Count > 0)
					sense = le.SensesOS[0];

				allomorph = MorphServices.FindMatchingAllomorph(le, tssForm);
				bool fCreatedAllomorph = false;
				if (allomorph == null)
				{
					using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
						cache.ServiceLocator.GetInstance<IActionHandler>(), ITextStrings.ksUndoAddAllomorphToSimilarEntry, ITextStrings.ksRedoAddAllomorphToSimilarEntry))
					{
						allomorph = MorphServices.MakeMorph(le, tssForm);
						fCreatedAllomorph = true;
						Debug.Assert(allomorph != null);
						undoHelper.RollBack = false;
					}
					if (fCreatedEntry)
					{
						// Making the entry and the allomorph should feel like one indivisible action to the end user.
						((IActionHandlerExtensions) cache.ActionHandlerAccessor).MergeLastTwoUnitsOfWork();
					}
				}
				var allomorph1 = allomorph;
				var le1 = le;
				var sense1 = sense;
				if (fCreatedEntry || fCreatedAllomorph)
				{
					// If we've created something, then updating the sandbox needs to be undone as a unit with it,
					// so the sandbox isn't left showing something uncreated.
					UndoableUnitOfWorkHelper.Do("join me up", "join me up", cache.ActionHandlerAccessor,
						() => UpdateMorphEntry(allomorph1, le1, sense1));
					((IActionHandlerExtensions)cache.ActionHandlerAccessor).MergeLastTwoUnitsOfWork();
				}
				else
				{
					// Updating the sandbox doesn't need to be undoable, no real data changes.
					UpdateMorphEntry(allomorph1, le1, sense1);
				}
			}

			private LexEntryComponents BuildEntryComponents()
			{
				var entryComponents = MorphServices.BuildEntryComponents(m_caches.MainCache,
					TsStringUtils.GetCleanSingleRunTsString(m_sandbox.GetFullMorphForm(m_hvoMorph)));
				int hvoMorph = m_caches.DataAccess.get_ObjectProp(m_hvoMorph, ktagSbMorphForm);
				var intermediateTssForm = m_caches.DataAccess.get_MultiStringAlt(hvoMorph,
											   ktagSbNamedObjName, m_sandbox.RawWordformWs);
				var tssForm = TsStringUtils.GetCleanSingleRunTsString(intermediateTssForm);
				if (entryComponents.LexemeFormAlternatives.Count > 0 &&
					!entryComponents.LexemeFormAlternatives[0].Equals(tssForm))
				{
					throw new ArgumentException("Expected entryComponents to already have " + tssForm.Text);
				}
				int cMorphs = m_caches.DataAccess.get_VecSize(m_hvoSbWord, ktagSbWordMorphs);
				if (cMorphs == 1)
				{
					// Make this string the gloss of the dlg.
					ITsString tssGloss = m_sandbox.Caches.DataAccess.get_MultiStringAlt(
						m_sandbox.RootWordHvo, ktagSbWordGloss,
						m_sandbox.Caches.MainCache.DefaultAnalWs);
					int hvoSbPos = m_sandbox.Caches.DataAccess.get_ObjectProp(m_sandbox.RootWordHvo,
						ktagSbWordPos);
					int hvoRealPos = m_sandbox.Caches.RealHvo(hvoSbPos);
					IPartOfSpeech realPos = null;
					if (hvoRealPos != 0)
						realPos = m_sandbox.Caches.MainCache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(hvoRealPos);
					entryComponents.MSA.MsaType = MsaType.kStem;
					entryComponents.MSA.MainPOS = realPos;
					entryComponents.GlossAlternatives.Add(tssGloss);
					// Also copy any other glosses we have.
					foreach (int ws in m_sandbox.Caches.MainCache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(wsObj => wsObj.Handle))
					{
						ITsString tss = m_sandbox.Caches.DataAccess.get_MultiStringAlt(m_sandbox.RootWordHvo, ktagSbWordGloss, ws);
						entryComponents.GlossAlternatives.Add(tss);
					}
				}
				return entryComponents;
			}

			/// <summary>
			/// this dialog, dumbs down InsertEntryDlg, to use its states and logic for
			/// creating a new entry immediately without trying to do matching Entries.
			/// </summary>
			class InsertEntryNow : InsertEntryDlg
			{
				static internal InsertEntryDlg CreateInsertEntryDlg(bool fCreateEntryNow)
				{
					if (fCreateEntryNow)
						return new InsertEntryNow();
					else
						return new InsertEntryDlg();
				}

				public InsertEntryNow()
				{
					m_matchingEntriesGroupBox.Visible = false;
				}

				/// <summary>
				/// skip updating matches, since this dialog is just for inserting a new entry.
				/// </summary>
				protected override void UpdateMatches()
				{
					// skip matchingEntries.ResetSearch
				}
			}

			internal void RunAddNewAllomorphDlg()
			{
				CheckDisposed();

				ITsString tssForm;
				ITsString tssFullForm;
				IMoMorphType morphType;
				GetMorphInfo(out tssForm, out tssFullForm, out morphType);

				using (AddAllomorphDlg dlg = new AddAllomorphDlg())
				{
					FdoCache cache = m_caches.MainCache;
					dlg.SetDlgInfo(cache, null, m_sandbox.Mediator, tssForm, morphType.Hvo);
					Form mainWnd = m_sandbox.FindForm();
					// Making the form active fixes LT-2619.
					// I'm (RandyR) not sure what adverse impact might show up by doing this.
					mainWnd.Activate();
					if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
					{
						// OK, they chose an entry, but does it have an appropriate MoForm?
						ILexEntry le = dlg.SelectedObject as ILexEntry;
						IActionHandler actionHandler;
						actionHandler = cache.ServiceLocator.GetInstance<IActionHandler>();
						if (dlg.InconsistentType && le.LexemeFormOA != null)
						{
							IMoForm morphLe = le.LexemeFormOA;
							IMoMorphType mmtLe = morphLe.MorphTypeRA;
							IMoMorphType mmtNew = null;
							if (morphType != null)
							{
								mmtNew = morphType;
							}
							string entryForm = null;
							ITsString tssHeadword = le.HeadWord;
							if (tssHeadword != null)
								entryForm = tssHeadword.Text;
							if (entryForm == null || entryForm == "")
								entryForm = ITextStrings.ksNoForm;
							string sNoMorphType = m_sandbox.Mediator.StringTbl.GetString(
								"NoMorphType", "DialogStrings");
							string sTypeLe;
							if (mmtLe != null)
								sTypeLe = mmtLe.Name.BestAnalysisAlternative.Text;
							else
								sTypeLe = sNoMorphType;
							string sTypeNew;
							if (mmtNew != null)
								sTypeNew = mmtNew.Name.BestAnalysisAlternative.Text;
							else
								sTypeNew = sNoMorphType;
							string msg1 = String.Format(ITextStrings.ksSelectedLexEntryXisaY,
								entryForm, sTypeLe);
							string msg2 = String.Format(ITextStrings.ksAreYouSureAddZtoX,
								sTypeNew, tssForm.Text);

							using (var warnDlg = new CreateAllomorphTypeMismatchDlg())
							{
								warnDlg.Warning = msg1;
								warnDlg.Question = msg2;
								switch (warnDlg.ShowDialog(mainWnd))
								{
									case DialogResult.No:
										return;
									// cancelled.
									case DialogResult.Yes:
										// Go ahead and create allomorph.
										// But first, we have to ensure an appropriate MSA exists.
										bool haveStemMSA = false;
										bool haveUnclassifiedMSA = false;
										foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
										{
											if (msa is IMoStemMsa)
												haveStemMSA = true;
											if (msa is IMoUnclassifiedAffixMsa)
												haveUnclassifiedMSA = true;
										}
										switch (mmtNew.Guid.ToString())
										{
											case MoMorphTypeTags.kMorphBoundRoot:
											case MoMorphTypeTags.kMorphBoundStem:
											case MoMorphTypeTags.kMorphClitic:
											case MoMorphTypeTags.kMorphEnclitic:
											case MoMorphTypeTags.kMorphProclitic:
											case MoMorphTypeTags.kMorphStem:
											case MoMorphTypeTags.kMorphRoot:
											case MoMorphTypeTags.kMorphParticle:
											case MoMorphTypeTags.kMorphPhrase:
											case MoMorphTypeTags.kMorphDiscontiguousPhrase:
												// Add a MoStemMsa, if needed.
												if (!haveStemMSA)
												{
													UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoAddAllomorph,
														ITextStrings.ksRedoAddAllomorph, actionHandler, () =>
													{
														le.MorphoSyntaxAnalysesOC.Add(
															cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create());
													});
												}
												break;
											default:
												// Add a MoUnclassifiedAffixMsa, if needed.
												if (!haveUnclassifiedMSA)
												{
													UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoAddAllomorph,
														ITextStrings.ksRedoAddAllomorph, actionHandler, () =>
													{
														le.MorphoSyntaxAnalysesOC.Add(
															cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>().Create());
													});
												}
												break;
										}
										break;
									case DialogResult.Retry:
										// Rather arbitrarily we use this dialog result for the
										// Create New option.
										this.RunCreateEntryDlg();
										return;
									default:
										// treat as cancelled
										return;
								}
							}
						}
						IMoForm allomorph = null;

						if (dlg.MatchingForm && !dlg.InconsistentType)
						{
							allomorph = MorphServices.FindMatchingAllomorph(le, tssForm);
							if (allomorph == null)
							{
								// We matched on the Lexeme Form, not on an alternate form.
								//allomorph = MoForm.MakeMorph(cache, le, tssFullForm);
								UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoAddAllomorph,ITextStrings.ksRedoAddAllomorph, actionHandler, () =>
								{
									allomorph = MorphServices.MakeMorph(le, tssFullForm);
									UpdateMorphEntry(allomorph, le, null);
								});
							}
						}
						else
						{
							UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoAddAllomorph, ITextStrings.ksRedoAddAllomorph, actionHandler, () =>
							{
								allomorph = MorphServices.MakeMorph(le, tssFullForm);
								UpdateMorphEntry(allomorph, le, null);
							});
						}
						Debug.Assert(allomorph != null);
					}
				}
			}


			private void GetMorphInfo(out ITsString tssForm, out ITsString tssFullForm, out IMoMorphType morphType)
			{
				IMoForm morphReal;
				GetMorphInfo(out tssForm, out tssFullForm, out morphReal, out morphType);
			}
			private void GetMorphInfo(out ITsString tssForm, out ITsString tssFullForm, out IMoForm morphReal, out IMoMorphType morphType)
			{
				morphReal = null;
				int hvoMorph = m_caches.DataAccess.get_ObjectProp(m_hvoMorph, ktagSbMorphForm);
				var hvoMorphReal = m_caches.RealHvo(hvoMorph);
				if (hvoMorphReal != 0)
					morphReal = m_caches.MainCache.ServiceLocator.GetInstance<IMoFormRepository>().GetObject(hvoMorphReal);
				ISilDataAccess sda = m_caches.DataAccess;
				tssForm = m_caches.DataAccess.get_MultiStringAlt(hvoMorph, ktagSbNamedObjName, m_sandbox.RawWordformWs);
				tssFullForm = m_sandbox.GetFullMorphForm(m_hvoMorph);
				string fullForm = tssFullForm.Text;
				morphType = null;
				if (morphReal != null)
				{
					morphType = morphReal.MorphTypeRA;
				}
				else
				{
					// if we don't have a form then we can't derive a type. (cf. LT-1621)
					if (string.IsNullOrEmpty(fullForm))
					{
						morphType = null;
					}
					else
					{
						// Find the type for this morpheme
						int clsidForm;
						string fullFormTmp = fullForm;
						morphType = MorphServices.FindMorphType(m_caches.MainCache, ref fullFormTmp, out clsidForm);
					}
				}
			}

			internal int RunAddNewSenseDlg(ITsString tssForm, ILexEntry le)
			{
				CheckDisposed();

				if (tssForm == null)
				{
					int hvoForm = m_caches.DataAccess.get_ObjectProp(m_hvoMorph,
						ktagSbMorphForm);
					tssForm = m_caches.DataAccess.get_MultiStringAlt(hvoForm, ktagSbNamedObjName, m_sandbox.RawWordformWs);
				}
				int newSenseID = 0;
				// This 'using' system is important,
				// because it calls Dispose on the dlg,
				// when it goes out of scope.
				// Otherwise, it gets disposed when the GC gets around to it,
				// and that may not happen until the app closes,
				// which causes bad problems.
				using (AddNewSenseDlg dlg = new AddNewSenseDlg(m_helpTopicProvider))
				{
					dlg.SetDlgInfo(tssForm, le, m_sandbox.Mediator);
					Form mainWnd = m_sandbox.FindForm();
					// Making the form active fixes problems like LT-2619.
					// I'm (RandyR) not sure what adverse impact might show up by doing this.
					mainWnd.Activate();
					if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
						dlg.GetDlgInfo(out newSenseID);
				}
				return newSenseID;
			}

			// Handles a change in the item selected in the combo box.
			// Some combo items (like "Allomorph of...") can be disabled under
			// certain conditions (cf. LT-1621), but still appear in the dropdown.
			// In those cases, we don't want to hide the combo, so let the HandleSelect
			// set m_fHideCombo (to false) for those items that user tried to select.
			internal override void HandleComboSelChange(object sender, EventArgs ea)
			{
				CheckDisposed();

				if (m_fUnderConstruction)
					return;
				this.HandleSelect(ComboList.SelectedIndex);
				if (m_fHideCombo)
				{
					this.HideCombo();
				}
				else
				{
					// After skipping HideCombo, reset the flag
					m_fHideCombo = true;
				}
			}

			/// <summary>
			/// Return true if it is necessary to call HandleSelect even though we selected the
			/// current item.
			/// </summary>
			/// <returns></returns>
			internal bool NeedSelectSame()
			{
				if (ComboList.SelectedIndex >= m_morphItems.Count || ComboList.SelectedIndex < 0)
				{
					// This happens, for a reason I (JohnT) don't understand, when we launch a dialog from
					// one of these menu options using Enter, and then the dialog is also closed using enter.
					// If we return true the dialog can be launched twice.
					return false;
				}
				var sbHvo = m_sandbox.CurrentLexEntriesAnalysis(m_hvoMorph);
				var realObject = m_sandbox.Caches.RealObject(sbHvo);
				if (realObject == null)
					return true; // nothing currently set, set whatever is current.
				var classid = realObject.ClassID;
				var mi = m_morphItems[ComboList.SelectedIndex];
				if (classid != LexSenseTags.kClassId && mi.m_hvoSense != 0)
					return true; // item is a sense, and current value is not!
				if (mi.m_hvoSense == 0)
					return true; // Add New Sense...
				if (m_sandbox.CurrentPos(m_hvoMorph) == 0)
					return true; // sense MSA has been set since analysis created, need to update it (LT-14574)
				// Review JohnT: are there any other cases where we should do it anyway?
				return false;
			}

			internal override void HandleComboSelSame(object sender, EventArgs ea)
			{
				CheckDisposed();

				// Just close the ComboBox, since nothing changed...unless we selected a sense item and all we
				// had was an entry or msa, or some similar special case.
				if (NeedSelectSame())
					this.HandleSelect(ComboList.SelectedIndex);
				this.HideCombo();
			}

			public override void HandleSelect(int index)
			{
				CheckDisposed();
				if (index < 0 || index >= ComboList.Items.Count)
					return;

				int morphIndex = GetMorphIndex();
				// NOTE: m_comboList.SelectedItem does not get automatically set in (some) tests.
				// so we use index here.
				InterlinComboHandlerActionComboItem comboItem = ComboList.Items[index] as InterlinComboHandlerActionComboItem;
				if (comboItem != null)
				{
					if (!comboItem.IsEnabled)
					{
						m_fHideCombo = false;
						return;
					}
					comboItem.OnSelectItem();
					if (!(comboItem is MorphComboItem))
						CopyLexEntryInfoToMonomorphemicWordGlossAndPos();
					SelectEntryIcon(morphIndex);
				}
			}

			private int GetMorphIndex()
			{
				int morphIndex = 0;
				ISilDataAccess sda = m_caches.DataAccess;
				int cmorphs = sda.get_VecSize(m_hvoSbWord, ktagSbWordMorphs);
				for (; morphIndex < cmorphs; morphIndex++)
					if (sda.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, morphIndex) == m_hvoMorph)
						break;
				Debug.Assert(morphIndex < cmorphs);
				return morphIndex;
			}

			private void OnSelectCreateNewEntry(object sender, EventArgs args)
			{
				try
				{
					RunCreateEntryDlg();
				}
				catch (Exception exc)
				{
					MessageBox.Show(exc.Message, ITextStrings.ksCannotCreateNewEntry);
				}
			}

			private void OnSelectAllomorphOf(object sender, EventArgs args)
			{
				try
				{
					RunAddNewAllomorphDlg();
				}
				catch (Exception exc)
				{
					MessageBox.Show(exc.Message, ITextStrings.ksCannotAddAllomorph);
				}
			}

			private void OnSelectEditMorphBreaks(object sender, EventArgs args)
			{
				using (IhMorphForm handler = new IhMorphForm(m_sandbox))
				{
					handler.UpdateMorphBreaks(handler.EditMorphBreaks()); // this should launch the dialog.
				}
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="sender">should be the selected combo item.</param>
			/// <param name="args"></param>
			private void HandleSelectMorphComboItem(object sender, EventArgs args)
			{
				MorphComboItem mci = (MorphComboItem)sender;
				MorphItem mi = mci.MorphItem;
				var morphReal = m_caches.MainCache.ServiceLocator.GetInstance<IMoFormRepository>().GetObject(mi.m_hvoMorph);
				ILexEntry morphEntryReal = mi.GetPrimaryOrOwningEntry(m_caches.MainCache);
				ITsString tss = mi.m_name;
				bool fUpdateMorphEntry = true;
				bool fCreatedSense = false;
				if (mi.m_hvoSense == 0)
				{
					mi.m_hvoSense = RunAddNewSenseDlg(tss, morphEntryReal);
					if (mi.m_hvoSense == 0)
					{
						// must have canceled from the dlg.
						fUpdateMorphEntry = false;
					}
					else
					{
						fCreatedSense = true;
					}
				}
				ILexSense senseReal = null;
				ILexEntryInflType inflType = null;
				if (mi.m_hvoSense != 0)
					senseReal = m_caches.MainCache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(mi.m_hvoSense);
				if (mi.m_inflType != null)
					inflType = mi.m_inflType;
				if (fUpdateMorphEntry)
				{
					// If we've created something, then updating the sandbox needs to be undone as a unit with it,
					// so the sandbox isn't left showing something uncreated.
					// If we are already in a UOW, we can just add the focus box to it.
					// If we didn't create a sense, we can just let the focus box Undo be discarded.
					if (m_caches.MainCache.ActionHandlerAccessor.CurrentDepth > 0)
					{
						UpdateMorphEntry(morphReal, morphEntryReal, senseReal, inflType); // already in UOW, join it
					}
					else
					{
						// But if we created something in a separate UOW that is now over, we need to make the
						// focus box action in a new UOW, then merge the two.
						UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoAddSense, ITextStrings.ksRedoAddSense, m_caches.MainCache.ActionHandlerAccessor,
							() => UpdateMorphEntry(morphReal, morphEntryReal, senseReal, inflType));
						if (fCreatedSense)
							((IActionHandlerExtensions)m_caches.MainCache.ActionHandlerAccessor).MergeLastTwoUnitsOfWork();
					}
				}
			}

			private void SetLexEntryToUnknown(object sender, EventArgs args)
			{
				ISilDataAccess sda = m_caches.DataAccess;
				IVwCacheDa cda = (IVwCacheDa)m_caches.DataAccess;
				cda.CacheObjProp(m_hvoMorph, ktagSbMorphEntry, 0);
				cda.CacheObjProp(m_hvoMorph, ktagSbMorphGloss, 0);
				cda.CacheObjProp(m_hvoMorph, ktagSbMorphPos, 0);
				// Forget we had an existing wordform; otherwise, the program considers
				// all changes to be editing the wordform, and since it belongs to the
				// old analysis, the old analysis gets resurrected.
				m_sandbox.m_hvoWordGloss = 0;
				// The current ktagSbMorphForm property is for an SbNamedObject that
				// is associated with an MoForm belonging to the LexEntry that we are
				// trying to dissociate from. If we leave it that way, it will resurrect
				// the LexEntry connection when we update the real cache.
				// Instead make a new named object for the form.
				ITsString tssForm = sda.get_MultiStringAlt(sda.get_ObjectProp(m_hvoMorph, ktagSbMorphForm),
					ktagSbNamedObjName, m_sandbox.RawWordformWs);
				int hvoNewForm = sda.MakeNewObject(kclsidSbNamedObj, m_hvoMorph, ktagSbMorphForm, -2);
				sda.SetMultiStringAlt(hvoNewForm, ktagSbNamedObjName,
					m_sandbox.RawWordformWs, tssForm);
				//cda.CacheStringProp(m_hvoMorph, ktagSbMorphPrefix, null);
				//cda.CacheStringProp(m_hvoMorph, ktagSbMorphPostfix, null);
				// Send notifiers for each of these deleted items.
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_hvoMorph, ktagSbMorphEntry, 0, 0, 1);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_hvoMorph, ktagSbMorphGloss, 0, 0, 1);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_hvoMorph, ktagSbMorphPos, 0, 0, 1);
				//sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
				//	m_hvoMorph, ktagSbMorphPrefix, 0, 0, 1);
				//sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
				//	m_hvoMorph, ktagSbMorphPostfix, 0, 0, 1);
			}

			private void OnSelectVariantOf(object sender, EventArgs args)
			{
				try
				{
					using (LinkVariantToEntryOrSense dlg = new LinkVariantToEntryOrSense())
					{
						ILexEntry variantEntry = null;
						// if no previous variant relationship has been defined,
						// then don't try to fill initial information,
						ITsString tssForm;
						ITsString tssFullForm;
						IMoMorphType morphType;
						IMoForm morphReal;
						GetMorphInfo(out tssForm, out tssFullForm, out morphReal, out morphType);
						if (morphReal != null && morphReal.IsValidObject)
						{
							variantEntry = morphReal.Owner as ILexEntry;
							dlg.SetDlgInfo(m_sandbox.Cache, m_sandbox.Mediator, variantEntry);
						}
						else
						{
							// since we didn't start with an entry,
							// set up the dialog using the form of the variant
							dlg.SetDlgInfo(m_sandbox.Cache, m_sandbox.Mediator, tssForm);
						}
						dlg.SetHelpTopic("khtpAddVariantFromInterlinear");
						Form mainWnd = m_sandbox.FindForm();
						// Making the form active fixes problems like LT-2619.
						// I'm (RandyR) not sure what adverse impact might show up by doing this.
						mainWnd.Activate();
						if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
						{
							if (dlg.SelectedObject == null)
								return; // odd. nothing more to do.

							ILexEntryRef variantEntryRef = dlg.VariantEntryRefResult;
							// if we didn't have a starting entry, create one now.
							ILexEntry variantResult = variantEntryRef.Owner as ILexEntry;
							int classOfSelectedId = dlg.SelectedObject.ClassID;
							int hvoVariantType = dlg.SelectedVariantEntryTypeHvo;
							ILexEntryInflType inflType = null;
							m_caches.MainCache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().
									TryGetObject(hvoVariantType, out inflType);

							// we need to create a new LexEntryRef.
							ILexEntry morphBundleEntry = dlg.SelectedObject as ILexEntry;
							ILexSense morphBundleSense = dlg.SelectedObject as ILexSense;
							if (morphBundleSense != null)
								morphBundleEntry = morphBundleSense.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
							UpdateMorphEntry(variantResult.LexemeFormOA, morphBundleEntry, morphBundleSense, inflType);
						}
					}

				}
				catch (Exception exc)
				{
					MessageBox.Show(exc.Message, ITextStrings.ksCannotAddVariant);
				}
			}

			protected virtual void SelectEntryIcon(int morphIndex)
			{
				m_sandbox.SelectEntryIconOfMorph(morphIndex);
			}

			/// <summary>
			/// Update the sandbox cache to reflect a choice of the real MoForm and the
			/// entry indicated by the FdoCache hvos passed.
			/// </summary>
			internal void UpdateMorphEntry(IMoForm moFormReal, ILexEntry entryReal, ILexSense senseReal,
				ILexEntryInflType inflType = null)
			{
				CheckDisposed();

				bool fDirty = m_sandbox.Caches.DataAccess.IsDirty();
				bool fApproved = !m_sandbox.UsingGuess;
				bool fHasApprovedWordGloss = m_sandbox.HasWordGloss() && (fDirty || fApproved);
				bool fHasApprovedWordCat = m_sandbox.HasWordCat() && (fDirty || fApproved);

				var undoAction = new UpdateMorphEntryAction(m_sandbox, m_hvoMorph); // before changes.

				// Make a new morph, if one does not already exist, corresponding to the
				// selected item.  Its form must match what is already displayed.  Store it as
				// the new value.
				int hvoMorph = m_sandbox.CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidMorphemes, moFormReal.Hvo,
					MoFormTags.kflidForm);
				m_caches.DataAccess.SetObjProp(m_hvoMorph, ktagSbMorphForm, hvoMorph);
				m_caches.DataAccess.PropChanged(m_rootb,
					(int)PropChangeType.kpctNotifyAll, m_hvoMorph, ktagSbMorphForm, 0,
					1, 1);

				// Try to establish the sense.  Call this before SetSelectedEntry and LoadSecDataForEntry.
				// reset cached gloss, since we should establish the sense according to the real sense or real entry.
				m_caches.DataAccess.SetObjProp(m_hvoMorph, ktagSbMorphGloss, 0);
				var morphEntry = moFormReal.Owner as ILexEntry;
				ILexSense realDefaultSense = m_sandbox.EstablishDefaultSense(m_hvoMorph, morphEntry, senseReal, inflType);
				// Make and install a secondary object to correspond to the real LexEntry.
				// (The zero says we are not guessing any more, since the user selected this entry.)

				m_sandbox.LoadSecDataForEntry(morphEntry, senseReal != null ? senseReal : realDefaultSense,
					m_hvoSbWord, m_caches.DataAccess as IVwCacheDa,
					m_wsVern, m_hvoMorph, 0, m_caches.MainCache.MainCacheAccessor, null);
				m_caches.DataAccess.PropChanged(m_rootb,
					(int)PropChangeType.kpctNotifyAll, m_hvoMorph, ktagSbMorphEntry, 0,
					1, WasReal());

				// Notify any delegates that the selected Entry changed.
				m_sandbox.SetSelectedEntry(entryReal);
				// fHasApprovedWordGloss: if an approved word gloss already exists -- don't replace it
				// fHasApprovedWordCat: if an approved word category already exists -- don't replace it
				CopyLexEntryInfoToMonomorphemicWordGlossAndPos(!fHasApprovedWordGloss, !fHasApprovedWordCat);
				undoAction.GetNewVals();
				// If we're doing this as part of something undoable, and then undo it, we should undo this also,
				// especially so the Sandbox isn't left displaying something whose creation has been undone. (FWR-3547)
				if (m_caches.MainCache.ActionHandlerAccessor.CurrentDepth > 0)
				{
					m_caches.MainCache.ActionHandlerAccessor.AddAction(undoAction);
				}
				return;
			}
			protected virtual void CopyLexEntryInfoToMonomorphemicWordGlossAndPos()
			{
				// do nothing in general.
			}

			protected virtual void CopyLexEntryInfoToMonomorphemicWordGlossAndPos(bool fCopyToWordGloss, bool fCopyToWordPos)
			{
				// conditionally set up the word gloss and POS to correspond to monomorphemic lex morph entry info.
				SyncMonomorphemicGlossAndPos(fCopyToWordGloss, fCopyToWordPos);
				// Forget we had an existing wordform; otherwise, the program considers
				// all changes to be editing the wordform, and since it belongs to the
				// old analysis, the old analysis gets resurrected.
				m_sandbox.m_hvoWordGloss = 0;
			}
		}

		[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
			Justification="m_sandbox is a reference")]
		class UpdateMorphEntryAction: UndoActionBase
		{
			private SandboxBase m_sandbox;
			private int m_hvoMorph;
			private ISilDataAccess m_sda;
			private int m_morphFormNew;
			private int m_morphFormOld;
			private readonly int[] m_tags = { ktagSbMorphForm, ktagSbMorphGloss, ktagSbNamedObjGuess, ktagSbMorphPos};
			private readonly int[] m_oldVals;
			private readonly int[] m_newVals;
			private int m_originalAnalysis;

			public UpdateMorphEntryAction(SandboxBase sandbox, int hvoMorph)
			{
				m_oldVals = new int[m_tags.Length];
				m_newVals = new int[m_tags.Length];
				m_sandbox = sandbox;
				m_sda = m_sandbox.Caches.DataAccess;
				m_originalAnalysis = m_sandbox.Analysis;
				m_hvoMorph = hvoMorph;
				for (int i = 0; i < m_tags.Length; i++ )
					m_oldVals[i] = m_sda.get_ObjectProp(m_hvoMorph, m_tags[i]);
			}

			public void GetNewVals()
			{
				for (int i = 0; i < m_tags.Length; i++)
					m_newVals[i] = m_sda.get_ObjectProp(m_hvoMorph, m_tags[i]);
			}
			/// <summary>
			/// Reverses (or "undoes") an action.
			/// </summary>
			public override bool Undo()
			{
				SetVals(m_oldVals);
				return true;
			}

			private void SetVals(int[] vals)
			{
				// If things have moved on, don't mess with it. These changes are no longer relevant.
				if (m_sandbox.IsDisposed || m_sandbox.Analysis != m_originalAnalysis)
					return;
				for (int i = 0; i < m_tags.Length; i++)
				{
					if (m_oldVals[i] == m_newVals[i])
						continue;
					m_sda.SetObjProp(m_hvoMorph, m_tags[i], vals[i]);
					m_sda.PropChanged(m_sandbox.m_rootb,
									  (int)PropChangeType.kpctNotifyAll, m_hvoMorph, m_tags[i], 0,
									  1, 1);
				}
			}

			/// <summary>
			/// Reapplies (or "redoes") an action.
			/// </summary>
			public override bool Redo()
			{
				SetVals(m_newVals);
				return true;
			}

			/// <summary>
			/// This is rather dubious. This change is NOT in itself a change to FieldWorks data.
			/// However, currently (Mar 17 2011) the whole undo bundle is discarded if none of the
			/// changes is a data change, and we need it to be put into the stack long enough so
			/// that it can get merged with the change it modifies. If we allow no-data-change UOWs
			/// to get put in the stack normally, we can remove this override.
			/// </summary>
			public override bool IsDataChange
			{
				get
				{
					return true;
				}
			}
		}



		/// <summary>
		/// This class handles the MorphEntry line when there is a current entry. Currently it
		/// is very nearly the same.
		/// </summary>
		internal class IhMorphEntry : IhMissingEntry
		{
			internal IhMorphEntry(IHelpTopicProvider helpTopicProvider) : base(helpTopicProvider)
			{
			}

			internal override int WasReal()
			{
				return 1;
			}
		}

		internal class IhLexWordGloss : IhMorphEntry
		{
			internal IhLexWordGloss(IHelpTopicProvider helpTopicProvider) : base(helpTopicProvider)
			{
			}

			protected override void SelectEntryIcon(int morphIndex)
			{
				m_sandbox.SelectIcon(ktagWordGlossIcon);
			}

			protected override void CopyLexEntryInfoToMonomorphemicWordGlossAndPos()
			{
				CopyLexEntryInfoToMonomorphemicWordGlossAndPos(true, true);
			}

			/// <summary>
			/// In the context of a LexWordGloss handler, the user is making a selection in the word combo list
			/// that should fill in the Word Gloss. So, make sure we copy the selected lex information.
			/// </summary>
			/// <param name="fCopyToWordGloss"></param>
			/// <param name="fCopyToWordPos"></param>
			protected override void CopyLexEntryInfoToMonomorphemicWordGlossAndPos(bool fCopyToWordGloss, bool fCopyToWordPos)
			{
				base.CopyLexEntryInfoToMonomorphemicWordGlossAndPos(true, true);
			}

			protected override void CopySenseToWordGloss(bool fCopyWordGloss, int hvoSbRootSense)
			{
				if (hvoSbRootSense == 0 && fCopyWordGloss)
				{
					// clear out the WordGloss line(s).
					ISilDataAccess sda = m_caches.DataAccess;
					foreach (int wsId in m_sandbox.m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
					{
						ITsString tssGloss = TsStringUtils.MakeTss("", wsId);
						sda.SetMultiStringAlt(m_hvoSbWord, ktagSbWordGloss, wsId, tssGloss);
						sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord, ktagSbWordGloss,
							wsId, 0, 0);
					}
				}
				else
				{
					base.CopySenseToWordGloss(fCopyWordGloss, hvoSbRootSense);
				}
				// treat as a deliberate user selection, not a guess.
				if (fCopyWordGloss)
					m_caches.DataAccess.SetInt(m_hvoSbWord, ktagSbWordGlossGuess, 0);
			}

			protected override int CopyLexPosToWordPos(bool fCopyToWordCat, int hvoLexPos)
			{

				int hvoPos = 0;
				if (fCopyToWordCat && hvoLexPos == 0)
				{
					// clear out the existing POS
					hvoPos = m_sandbox.CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidWordPos, hvoLexPos,
						CmPossibilityTags.kflidAbbreviation);
					int hvoSbWordPos = m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, ktagSbWordPos);
					m_caches.DataAccess.SetObjProp(m_hvoSbWord, ktagSbWordPos, hvoPos);
					m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord,
						ktagSbWordPos, 0, 1, (hvoSbWordPos == 0 ? 0 : 1));
				}
				else
				{
					hvoPos = base.CopyLexPosToWordPos(fCopyToWordCat, hvoLexPos);
				}
				// treat as a deliberate user selection, not a guess.
				if (fCopyToWordCat)
					m_caches.DataAccess.SetInt(hvoPos, ktagSbNamedObjGuess, 0);
				return hvoPos;

			}
		}

		// The WordGloss has no interesting menu for now. Just allow the text to be edited.
		internal class IhWordGloss : InterlinComboHandler
		{
			public IhWordGloss()
				: base()
			{
			}

			public override void HandleSelect(int index)
			{
				CheckDisposed();

				int fGuessingOld = m_caches.DataAccess.get_IntProp(m_hvoSbWord,
					ktagSbWordGlossGuess);

				HvoTssComboItem item = ComboList.SelectedItem as HvoTssComboItem;
				if (item == null)
					return;
				m_sandbox.WordGlossHvo = item.Hvo;
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				foreach (int ws in m_sandbox.m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
				{
					ITsString tss;
					if (item.Hvo == 0)
					{
						// Make an empty string in the specified ws.
						tss = tsf.MakeString("", ws);
					}
					else
					{
						tss = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(item.Hvo,
							WfiGlossTags.kflidForm, ws);
					}
					m_caches.DataAccess.SetMultiStringAlt(m_hvoSbWord, ktagSbWordGloss, ws, tss);
					// Regenerate the string regardless.  (See LT-10456)
					m_caches.DataAccess.PropChanged(m_rootb,
						(int)PropChangeType.kpctNotifyAll, m_hvoSbWord, ktagSbWordGloss,
						ws, tss.Length, tss.Length);
					// If it used to be a guess, mark it as no longer a guess.
					if (fGuessingOld != 0)
					{
						m_caches.DataAccess.SetInt(m_hvoSbWord, ktagSbWordGlossGuess, 0);
						m_caches.DataAccess.PropChanged(m_rootb,
							(int)PropChangeType.kpctNotifyAll, m_hvoSbWord,
							ktagSbWordGlossGuess, 0, 1, 1);
					}
				}

				m_sandbox.SelectAtEndOfWordGloss(-1);
				return;
			}

			// Not much to do except to initialize the edit box embedded in the combobox with
			// the proper writing system factory, writing system, and TsString.
			public override void SetupCombo()
			{
				CheckDisposed();

				base.SetupCombo();
				int hvoEmptyGloss = 0;
				ITsStrBldr tsb = TsStrBldrClass.Create();

				ComboList.WritingSystemFactory =
					m_caches.MainCache.LanguageWritingSystemFactoryAccessor;
				// Find the WfiAnalysis (from existing analysis or guess) to provide its word glosses as options (cf. LT-1428)
				IWfiAnalysis wa = m_sandbox.GetWfiAnalysisInUse();
				if (wa != null)
				{
					AddComboItems(ref hvoEmptyGloss, tsb, wa);
				}
				// TODO: Maybe this should merge invisibly with the current top of the undo stack? or start an
				// invisible top?
				using (var helper = new NonUndoableUnitOfWorkHelper(m_caches.MainCache.ActionHandlerAccessor))
				{
					var analMethod = m_sandbox.CreateRealWfiAnalysisMethod();
					IAnalysis anal = analMethod.Run();

					helper.RollBack = false;
					if (anal is IWfiAnalysis && anal.Guid != wa.Guid)
					{
						AddComboItems(ref hvoEmptyGloss, tsb, anal as IWfiAnalysis);
					}
					Debug.Assert(analMethod.ObsoleteAnalysis == null);
					//if (analMethod.ObsoleteAnalysis != null)
					//{
					//	// Should we worry about fixing up a reference in a Segment.AnalysesOC?
					//	analMethod.ObsoleteAnalysis.Delete();
					//}
				}
				ComboList.Items.Add(new HvoTssComboItem(hvoEmptyGloss,
					TsStringUtils.MakeTss(ITextStrings.ksNewWordGloss2, m_caches.MainCache.DefaultUserWs)));
				// Set combo selection to current selection.
				ComboList.SelectedIndex = this.IndexOfCurrentItem;

				// Enhance JohnT: if the analysts decide so, here we add all the other glosses from other analyses.

			}

			private void AddComboItems(ref int hvoEmptyGloss, ITsStrBldr tsb, IWfiAnalysis wa)
			{
				IList<int> wsids = m_sandbox.m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss);
				foreach (IWfiGloss gloss in wa.MeaningsOC)
				{
					int glossCount = 0;

					foreach (int ws in wsids)
					{
						ITsString nextWsGloss = gloss.Form.get_String(ws);
						if (nextWsGloss.Length > 0)
						{
							// Append a comma if there are more glosses.
							if (glossCount > 0)
								tsb.Replace(tsb.Length, tsb.Length, ", ", null);

							// Append a Ws label if there are more than one Ws.
							if (wsids.Count > 1)
							{
								tsb.ReplaceTsString(tsb.Length, tsb.Length, WsListManager.WsLabel(m_caches.MainCache, ws));
								tsb.Replace(tsb.Length, tsb.Length, " ", null);
							}
							int oldLen = tsb.Length;
							tsb.ReplaceTsString(oldLen, oldLen, nextWsGloss);
							int color = (int)CmObjectUi.RGB(Color.Blue);
							tsb.SetIntPropValues(oldLen, tsb.Length, (int)FwTextPropType.ktptForeColor,
								(int)FwTextPropVar.ktpvDefault, color);
							glossCount++;
						}
					}
					// (LT-1428) If we find an empty gloss, use this hvo for "New word gloss" instead of 0.
					if (glossCount == 0 && wsids.Count > 0)
					{
						hvoEmptyGloss = gloss.Hvo;
						ITsPropsBldr tpbUserWs = TsPropsBldrClass.Create();
						tpbUserWs.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsUser);
						tsb.Replace(tsb.Length, tsb.Length, ITextStrings.ksEmpty, tpbUserWs.GetTextProps());
					}

					ComboList.Items.Add(new HvoTssComboItem(gloss.Hvo, tsb.GetString()));
					tsb.Clear();
				}
			}

			/// <summary>
			/// Return the index corresponding to the current WordGloss state of the Sandbox.
			/// </summary>
			public override int IndexOfCurrentItem
			{
				get
				{
					for (int i = 0; i < ComboList.Items.Count; ++i)
					{
						HvoTssComboItem item = ComboList.Items[i] as HvoTssComboItem;
						if (item.Hvo == m_sandbox.WordGlossHvo)
							return i;
					}
					return -1;
				}
			}
		}

		/// <summary>
		/// The SbWord object has no Pos set.
		/// </summary>
		internal class IhMissingWordPos : InterlinComboHandler
		{
			POSPopupTreeManager m_pOSPopupTreeManager;
			PopupTree m_tree;

			internal PopupTree Tree
			{
				get
				{
					CheckDisposed();
					return m_tree;
				}
			}

			#region IDisposable override

			/// <summary>
			/// Executes in two distinct scenarios.
			///
			/// 1. If disposing is true, the method has been called directly
			/// or indirectly by a user's code via the Dispose method.
			/// Both managed and unmanaged resources can be disposed.
			///
			/// 2. If disposing is false, the method has been called by the
			/// runtime from inside the finalizer and you should not reference (access)
			/// other managed objects, as they already have been garbage collected.
			/// Only unmanaged resources can be disposed.
			/// </summary>
			/// <param name="disposing"></param>
			/// <remarks>
			/// If any exceptions are thrown, that is fine.
			/// If the method is being done in a finalizer, it will be ignored.
			/// If it is thrown by client code calling Dispose,
			/// it needs to be handled by fixing the bug.
			///
			/// If subclasses override this method, they should call the base implementation.
			/// </remarks>
			protected override void Dispose(bool disposing)
			{
				//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
				// Must not be run more than once.
				if (IsDisposed)
					return;

				if (disposing)
				{
					// Dispose managed resources here.
					if (m_pOSPopupTreeManager != null)
					{
						m_pOSPopupTreeManager.AfterSelect -= new TreeViewEventHandler(m_pOSPopupTreeManager_AfterSelect);
						m_pOSPopupTreeManager.Dispose();
					}
					if (m_tree != null)
					{
						m_tree.Load -= new EventHandler(m_tree_Load);
						m_tree.Dispose();
					}
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_pOSPopupTreeManager = null;
				m_tree = null;

				base.Dispose(disposing);
			}

			#endregion IDisposable override

			public override void SetupCombo()
			{
				CheckDisposed();

				m_tree = new PopupTree();
				// Try a bigger size here only for Sandbox POS editing (GordonM) [LT-7529]
				// Enhance: It would be better to know what size we need for the data,
				// but it gets displayed before we know what data goes in it!
				// PopupTree.DefaultSize was (120, 200)
				m_tree.Size = new Size(180, 220);
				m_tree.Load += new EventHandler(m_tree_Load);
				// Handle AfterSelect events through POSPopupTreeManager in m_tree_Load().
			}
			public override void Activate(SIL.Utils.Rect loc)
			{
				CheckDisposed();

				if (m_tree == null)
				{
					base.Activate(loc);
				}
				else
				{
					m_tree.Launch(m_sandbox.RectangleToScreen(loc),
						Screen.GetWorkingArea(m_sandbox));
				}
			}

			// This indicates there was not a previous real word POS recorded. The 'real' subclass
			// overrides to answer 1. The value signifies the number of objects stored in the
			// ktagSbWordPos property before the user made a selection in the menu.
			internal virtual int WasReal()
			{
				CheckDisposed();

				return 0;
			}

			public override List<int> Items
			{
				get
				{
					LoadItemsIfNeeded();
					return base.Items;
				}
			}

			private List<int> LoadItemsIfNeeded()
			{
				List<int> items = new List<int>();
				if (m_pOSPopupTreeManager == null || !m_pOSPopupTreeManager.IsTreeLoaded)
				{
					m_tree_Load(null, null);
					m_items = null;
					// not sure if this is guarranteed to be in the same order each time, but worth a try.
					foreach (ICmPossibility possibility in m_caches.MainCache.LangProject.PartsOfSpeechOA.ReallyReallyAllPossibilities)
					{
						items.Add(possibility.Hvo);
					}
					m_items = items;
				}
				return items;
			}

			public override int IndexOfCurrentItem
			{
				get
				{
					LoadItemsIfNeeded();
					// get currently selected item.
					int hvoLastCategory = m_caches.RealHvo(m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, ktagSbWordPos));
					// look it up in the items.
					return Items.IndexOf(hvoLastCategory);
				}
			}

			public override void HandleSelect(int index)
			{
				CheckDisposed();
				int hvoPos = Items[index];
				var possibility = m_caches.MainCache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(hvoPos);

				// Called only if it's a combo box.
				SelectItem(Items[index], possibility.Name.BestVernacularAnalysisAlternative.Text);
			}

			// We can't add the items until the form loads, or we get a spurious horizontal scroll bar.
			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "cache is a reference")]
			private void m_tree_Load(object sender, EventArgs e)
			{
				if (m_pOSPopupTreeManager == null)
				{
					FdoCache cache = m_caches.MainCache;
					m_pOSPopupTreeManager = new POSPopupTreeManager(m_tree, cache, cache.LangProject.PartsOfSpeechOA, cache.DefaultAnalWs, false, m_sandbox.Mediator, m_sandbox.FindForm());
					m_pOSPopupTreeManager.AfterSelect += new TreeViewEventHandler(m_pOSPopupTreeManager_AfterSelect);
				}
				m_pOSPopupTreeManager.LoadPopupTree(m_caches.RealHvo(m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, ktagSbWordPos)));
			}

			private void m_pOSPopupTreeManager_AfterSelect(object sender, TreeViewEventArgs e)
			{
				// we only want to actually select the item if we have clicked on it
				// or if we are simulating a click (e.g. by pressing Enter).
				if (!m_fUnderConstruction && e.Action == TreeViewAction.ByMouse)
				{
					SelectItem((e.Node as HvoTreeNode).Hvo, e.Node.Text);
				}
			}

			internal void SelectItem(int hvo, string label)
			{
				CheckDisposed();

				// if we haven't changed the selection, we don't need to change anything in the cache.
				int hvoLastCategory = m_caches.RealHvo(m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, ktagSbWordPos));
				if (hvoLastCategory != hvo)
				{
					int hvoPos = 0;
					if (hvo > 0)
					{
						ITsString tssAbbr = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(hvo,
							CmPossibilityTags.kflidName, m_caches.MainCache.DefaultAnalWs);
						hvoPos = m_caches.FindOrCreateSec(hvo, kclsidSbNamedObj,
							m_hvoSbWord, ktagSbWordDummy, ktagSbNamedObjName, tssAbbr);
						hvoPos = m_sandbox.CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidWordPos, hvo,
							CmPossibilityTags.kflidAbbreviation);
						m_caches.DataAccess.SetInt(hvoPos, ktagSbNamedObjGuess, 0);
					}
					m_caches.DataAccess.SetObjProp(m_hvoSbWord, ktagSbWordPos, hvoPos);
					m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord,
						ktagSbWordPos, 0, 1, WasReal());
					m_sandbox.SelectIcon(ktagWordPosIcon);
				}
			}
		}

		internal class IhWordPos : IhMissingWordPos
		{
			internal override int WasReal()
			{
				CheckDisposed();

				return 1;
			}
		}

	}
}
