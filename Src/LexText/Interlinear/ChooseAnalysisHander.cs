using System;
using System.Windows.Forms;
using System.Drawing;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FdoUi;
using SIL.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.CoreImpl;


namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class handles the functions of the combo box that is used to choose a
	/// different existing analysis.
	/// </summary>
	internal class ChooseAnalysisHandler : IComboHandler, IFWDisposable
	{
		int m_hvoAnalysis; // The current 'analysis', may be wordform, analysis, gloss.
		int m_hvoSrc; // the object (CmAnnotation? or SbWordform) we're analyzing.
		bool m_fInitializing = false; // true to suppress AnalysisChosen while setting up combo.
		FdoCache m_cache;
		IComboList m_combo;
		SandboxBase m_owner;
		const string ksMissingString = "---";
		const string ksPartSeparator = "   ";

		/// <summary>
		/// this is something of a hack until we convert to using a style sheet
		/// </summary>
		static protected int s_baseFontSize = 12;

		/// <summary>
		/// This fires when the user selects an item in the menu.
		/// The client will typically read the Analysis of 'this' to find which object has been
		/// selected as the current analysis.
		/// As a special case, if Analysis is zero, the user has selected the special
		/// 'Use default analysis' item, and the Sandbox should re-initialize as if no analysis
		/// had been selected for this word.
		/// Another special case is when the analysis is the ID of the WfiWordform itself.
		/// In this case, we specifically want to create a new analysis (so show no defaults).
		/// </summary>
		public event EventHandler AnalysisChosen;

		/// <summary>
		/// The object that should be modified if the user selects an analysis
		/// </summary>
		public int Source
		{
			get
			{
				CheckDisposed();
				return m_hvoSrc;
			}
		}

		/// <summary>
		/// Initially, the analysis we based the list on. After the user makes a selection,
		/// the selection he made.
		/// </summary>
		public AnalysisTree GetAnalysisTree()
		{
				var analysisTree = new AnalysisTree();
				if (m_hvoAnalysis != 0)
				{
					var analysisObj = (IAnalysis)m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoAnalysis);
					analysisTree.Analysis = analysisObj;
				}
				return analysisTree;
		}

		/// <summary>
		/// The current selected item in the combo box.
		/// </summary>
		public HvoTssComboItem SelectedItem
		{
			get
			{
				CheckDisposed();

				if (m_combo == null)
					return null;
				return m_combo.SelectedItem as HvoTssComboItem;
			}
		}

		internal IVwStylesheet StyleSheet
		{
			get
			{
				CheckDisposed();

				if (m_combo != null)
					return m_combo.StyleSheet;
				return null;
			}
		}

		/// Create one, typically from the Sandbox, using an existing combo box or list. The caller is responsible
		/// to display it; the Show method should not be called, especially if comboList is actually not a combo box.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoSrc"></param>
		/// <param name="?"></param>
		/// <param name="comboList"></param>
		public ChooseAnalysisHandler(FdoCache cache, int hvoSrc, int hvoAnalysis, IComboList comboList)
		{
			m_combo = comboList;
			m_cache = cache;
			m_hvoSrc = hvoSrc;
			m_hvoAnalysis = hvoAnalysis;
			m_combo.SelectedIndexChanged += new EventHandler(m_combo_SelectedIndexChanged);
			m_combo.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~ChooseAnalysisHandler()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

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
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_combo != null)
				{
					m_combo.SelectedIndexChanged -= new EventHandler(m_combo_SelectedIndexChanged);
					FwComboBox combo = m_combo as FwComboBox;
					if (combo != null && combo.Parent == null)
						combo.Dispose();
					else
					{
						ComboListBox clb = (m_combo as ComboListBox);
						if (clb != null)
							clb.Dispose();
					}
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cache = null;
			m_owner = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Add an item to the combo box, but only if its text is non-empty.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="fPossibleCurrent">generally true; false for items like "new analysis" that
		/// can't possibly be the current item, though hvoAnalysis might match.</param>
		void AddItem(ICmObject co, ITsString text, bool fPossibleCurrent)
		{
			AddItem(co, text, fPossibleCurrent, 0);
		}

		/// <summary>
		/// Add an item to the combo box, but only if its text is non-empty.
		/// </summary>
		/// <param name="fPossibleCurrent">generally true; false for items like "new analysis" that
		/// can't possibly be the current item, though hvoAnalysis might match.</param>
		/// <param name="tag">tag to specify an otherwise ambigious item.</param>
		void AddItem(ICmObject co, ITsString text, bool fPossibleCurrent, int tag)
		{
			if (text.Length == 0)
				return;
			int hvoObj = co != null ? co.Hvo : 0;
			HvoTssComboItem newItem = new HvoTssComboItem(hvoObj, text, tag);
			m_combo.Items.Add(newItem);
			if (fPossibleCurrent && hvoObj == m_hvoAnalysis)
				m_combo.SelectedItem = newItem;
		}

		void AddSeparatorLine()
		{
			//review
			ITsStrBldr builder = TsStrBldrClass.Create();
			builder.Replace(0,0,"-------", null);
			builder.SetIntPropValues(0, builder.Length, (int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, m_cache.DefaultUserWs);
			HvoTssComboItem newItem = new HvoTssComboItem(-1, builder.GetString()); //hack todo
			m_combo.Items.Add(newItem);
		}

		/// <summary>
		/// Initialize the combo contents.
		/// </summary>
		public void SetupCombo()
		{
			CheckDisposed();

			// ITsStrBldr builder = TsStrBldrClass.Create(); // CS0219

			m_fInitializing = true;
			var wordform = m_owner.GetWordformOfAnalysis();

			// Add the analyses, and recursively the other items.
			foreach (var wa in wordform.AnalysesOC)
			{
				Opinions o = wa.GetAgentOpinion(
					m_cache.LangProject.DefaultUserAgent);
				// skip any analysis the user has disapproved.
				if (o != Opinions.disapproves)
				{
					AddAnalysisItems(wa);
					AddSeparatorLine();
				}
			}

			// Add option to clear the analysis altogeter.
			AddItem(wordform, MakeSimpleString(ITextStrings.ksNewAnalysis), false, WfiWordformTags.kClassId);
			// Add option to reset to the default
			AddItem(null, MakeSimpleString(ITextStrings.ksUseDefaultAnalysis), false);

			m_fInitializing = false;
		}

		/// <summary>
		/// Add the items for this WfiAnalysis (itself and ones it owns).
		/// </summary>
		void AddAnalysisItems(IWfiAnalysis wa)
		{
			AddItem(wa,
				MakeAnalysisStringRep(wa, m_cache, StyleSheet != null, (m_owner as SandboxBase).RawWordformWs), true);
			foreach (var gloss in wa.MeaningsOC)
			{
				AddItem(gloss, MakeGlossStringRep(gloss, m_cache, StyleSheet != null), true);
			}

			//add the "new word gloss" option

			AddItem(wa, MakeSimpleString(ITextStrings.ksNewWordGloss), false, WfiGlossTags.kClassId);
		}

		protected ITsString MakeSimpleString (String str)
		{
			ITsStrBldr builder = TsStrBldrClass.Create();
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, m_cache.DefaultUserWs);
			bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize ,
				(int)FwTextPropVar.ktpvMilliPoint, s_baseFontSize * 800);
			bldr.SetStrPropValue((int)FwTextStringProp.kstpFontFamily, "Arial");
			builder.Replace(0,0,str , bldr.GetTextProps());
			return builder.GetString();
		}

		// Generate a suitable string representation of a WfiGloss.
		// Todo: finish implementing (add the gloss!)
		static internal ITsString MakeGlossStringRep(IWfiGloss wg, FdoCache fdoCache, bool fUseStyleSheet)
		{
			ITsStrBldr tsb = TsStrBldrClass.Create();
			var wa = wg.Owner as IWfiAnalysis;

			var category = wa.CategoryRA;
			if (category != null)
			{
				ITsString tssPos = category.Abbreviation.get_String( fdoCache.DefaultAnalWs);
				tsb.Replace(0, 0, tssPos.Text,
					PartOfSpeechTextProperties(fdoCache,false, fUseStyleSheet));
			}
			else
			{
				tsb.Replace(0, 0, ksMissingString,
					PartOfSpeechTextProperties(fdoCache,false, fUseStyleSheet));
			}
			tsb.Replace(tsb.Length, tsb.Length, " ", null);
			tsb.Replace(tsb.Length, tsb.Length,
						wg.Form.get_String(fdoCache.DefaultAnalWs).Text,
				GlossTextProperties(fdoCache, false, fUseStyleSheet));

			//indent
			tsb.Replace(0,0, "    ", null);
			return tsb.GetString();
		}

		// Make a string representing a WfiAnalysis, suitable for use in a combo box item.
		static internal ITsString MakeAnalysisStringRep(IWfiAnalysis wa, FdoCache fdoCache, bool fUseStyleSheet, int wsVern)
		{
			//			ITsTextProps boldItalicAnalysis = BoldItalicAnalysis(fdoCache);
			//			ITsTextProps italicAnalysis = ItalicAnalysis(fdoCache, Sandbox.SandboxVc.krgbRed);
			ITsTextProps posTextProperties = PartOfSpeechTextProperties(fdoCache, true, fUseStyleSheet);
			ITsTextProps formTextProperties = FormTextProperties(fdoCache, fUseStyleSheet, wsVern);
			ITsTextProps glossTextProperties = GlossTextProperties(fdoCache, true, fUseStyleSheet);
			ITsStrBldr tsb = TsStrBldrClass.Create();
			ISilDataAccess sda = fdoCache.MainCacheAccessor;
			int cmorph = wa.MorphBundlesOS.Count;
			if (cmorph == 0)
				return TsStringUtils.MakeTss(ITextStrings.ksNoMorphemes, fdoCache.DefaultUserWs);
			bool fRtl = fdoCache.ServiceLocator.WritingSystemManager.Get(wsVern).RightToLeftScript;
			int start = 0;
			int lim = cmorph;
			int increment = 1;
			if (fRtl)
			{
				start = cmorph - 1;
				lim = -1;
				increment = -1;
			}
			for (int i = start; i != lim; i += increment)
			{
				var mb = wa.MorphBundlesOS[i];
				var mf = mb.MorphRA;
				ITsString tssForm = null;
				// Review: Appears to be similar to code in LexEntryVc.
				if (mf != null)
				{
					var entry = mf.Owner as ILexEntry;
					var lexemeForm = entry.LexemeFormOA;
					if (lexemeForm != null)
					{
						tssForm = lexemeForm.Form.get_String(wsVern);
					}
					if (tssForm == null || tssForm.Length == 0)
					{
						tssForm = entry.CitationForm.get_String(wsVern);
					}
					if (tssForm.Length == 0)
					{
						// If there isn't a lexeme form OR citation form use the form of the morph.
						tssForm = mf.Form.get_String(wsVern);
					}
				}
				else // no MoForm linked to this bundle, use its own form.
				{
					tssForm = mb.Form.get_String(wsVern);
				}
				int ichForm = tsb.Length;
				tsb.ReplaceTsString(ichForm, ichForm, tssForm);
				tsb.SetProperties(ichForm, tsb.Length,formTextProperties);

				// add category (part of speech)
				var msa = mb.MsaRA;
				tsb.Replace(tsb.Length, tsb.Length, " ", null);
				int ichMinMsa = tsb.Length;
				string interlinName = ksMissingString;
				if (msa != null)
					interlinName = msa.InterlinearAbbr;
				tsb.Replace(ichMinMsa, ichMinMsa, interlinName, posTextProperties);

				//add sense
				var sense = mb.SenseRA;
				tsb.Replace(tsb.Length, tsb.Length, " ", null);
				int ichMinSense = tsb.Length;
				if (sense != null)
				{
					ITsString tssGloss = sense.Gloss.get_String(fdoCache.DefaultAnalWs);
					tsb.Replace(ichMinSense, ichMinSense, tssGloss.Text, glossTextProperties);
				}
				else
					tsb.Replace(ichMinSense, ichMinSense, ksMissingString, glossTextProperties);

				// Enhance JohnT: use proper seps.
				tsb.Replace(tsb.Length, tsb.Length, ksPartSeparator, null);
			}
			// Delete the final separator. (Enhance JohnT: this needs to get smarter when we do
			// real seps.)
			int ichFrom = tsb.Length - ksPartSeparator.Length;
			if (ichFrom < 0)
				ichFrom = 0;
			tsb.Replace(ichFrom, tsb.Length, "", null);
			return tsb.GetString();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fdoCache"></param>
		/// <returns></returns>
		public static ITsTextProps FormTextProperties(FdoCache fdoCache, bool fUseStyleSheet, int wsVern)
		{
			int color =(int) CmObjectUi.RGB(Color.DarkBlue);
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			if (!fUseStyleSheet)
			{
				bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize ,
					(int)FwTextPropVar.ktpvMilliPoint, s_baseFontSize * 1000);
			}
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, wsVern);
			//			bldr.SetIntPropValues((int)FwTextPropType.ktptBold,
			//				(int)FwTextPropVar.ktpvEnum,
			//				(int)FwTextToggleVal.kttvInvert);
			//			bldr.SetIntPropValues((int)FwTextPropType.ktptItalic,
			//				(int)FwTextPropVar.ktpvEnum,
			//				(int)FwTextToggleVal.kttvInvert);
			bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault, color);
			return bldr.GetTextProps();
		}

		public static ITsTextProps GlossTextProperties(FdoCache fdoCache, bool inAnalysisLine, bool fUseStyleSheet)
		{
			int color =(int) CmObjectUi.RGB(Color.DarkRed);
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			if (!fUseStyleSheet)
			{
				bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize ,
					(int)FwTextPropVar.ktpvMilliPoint, s_baseFontSize * 1000);
			}
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, fdoCache.DefaultAnalWs);

			if (inAnalysisLine)
			{
				bldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwSuperscriptVal.kssvSuper);
			}

			bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault, color);
			return bldr.GetTextProps();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fdoCache"></param>
		/// <returns></returns>
		public static ITsTextProps PartOfSpeechTextProperties(FdoCache fdoCache, bool inAnalysisLine, bool fUseStyleSheet)
		{
			int color =(int) CmObjectUi.RGB(Color.Green);
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			if (!fUseStyleSheet)
			{
				bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize ,
					(int)FwTextPropVar.ktpvMilliPoint, (int)( s_baseFontSize * 1000* .8));
			}
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, fdoCache.DefaultAnalWs);
			//			bldr.SetIntPropValues((int)FwTextPropType.ktptItalic,
			//				(int)FwTextPropVar.ktpvEnum,
			//				(int)FwTextToggleVal.kttvInvert);

			if (inAnalysisLine)
			{
				bldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwSuperscriptVal.kssvSub);
			}



			bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault, color);
			return bldr.GetTextProps();
		}

		/// <summary>
		/// Get the site where the combo or combo list is displayed.
		/// </summary>
		public SandboxBase Owner
		{
			get
			{
				CheckDisposed();
				return m_owner;
			}
			set
			{
				CheckDisposed();
				m_owner = value;
			}
		}

		/// <summary>
		/// Display the combo box at the specified location, or the list box pulled down from the specified location.
		/// </summary>
		/// <param name="loc"></param>
		public void Activate(SIL.Utils.Rect loc)
		{
			CheckDisposed();

			FwComboBox combo = m_combo as FwComboBox;
			if (combo != null)
			{

				combo.Location = new System.Drawing.Point(loc.left, loc.top);
				// 21 is the default height of a combo, the smallest reasonable size.
				combo.Size = new System.Drawing.Size(Math.Max(loc.right - loc.left + 30, 200), Math.Max( loc.bottom - loc.top, 50));
				if (!m_owner.Controls.Contains(combo))
					m_owner.Controls.Add(combo);
			}
			else
			{
				ComboListBox c = (m_combo as ComboListBox);
				c.AdjustSize(500, 400); // these are maximums!
				c.Launch(m_owner.RectangleToScreen(loc), Screen.GetWorkingArea(m_owner));
			}
		}

		/// <summary>
		/// Required interface method. We don't have a particular morph selected so answer zero.
		/// </summary>
		public int SelectedMorphHvo
		{
			get
			{
				CheckDisposed();
				return 0;
			}
		}

		/// <summary>
		/// Required interface method, not relevant for this class.
		/// </summary>
		public void HandleSelectIfActive()
		{
			CheckDisposed();

		}

		/// <summary>
		/// Handle the user selecting something in the combo.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_combo_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_fInitializing)
				return;
			int x = ((HvoTssComboItem)m_combo.SelectedItem).Hvo;
			if(x>=0)//could be a separator
				m_hvoAnalysis = x;
			Hide();	// Moved here from the end as the 'AnalysisChosen' method can
					// cause the current object to be disposed of.  Not very nice...
					// if there are other calls yet to be invoked on the object.
					// LT-5775: this is no real fix, as a real fix would understand
					// why the RootBox.MakeTextSelection would cause this object to
					// be disposed.  This I don't know...
			if (AnalysisChosen != null)
				AnalysisChosen(this, new EventArgs());
			// Old behavior appropriate for main window.
			//m_cache.SetObjProperty(m_hvoSrc, m_tagSrc, ((CaComboItem)m_combo.SelectedItem).Analysis);

		}

		/// <summary>
		/// Implement required interface method; will never be called because editing not allowed.
		/// </summary>
		/// <returns></returns>
		public virtual bool HandleReturnKey()
		{
			CheckDisposed();

			return false;
		}

		/// <summary>
		/// Get rid of the combo, typically when the user clicks outside it.
		/// </summary>
		public void Hide()
		{
			CheckDisposed();

			FwComboBox combo = m_combo as FwComboBox;
			if (combo != null && m_owner.Controls.Contains(combo))
				m_owner.Controls.Remove(combo);
			ComboListBox clb = m_combo as ComboListBox;
			if (clb != null)
				clb.HideForm();
		}
	}
}
