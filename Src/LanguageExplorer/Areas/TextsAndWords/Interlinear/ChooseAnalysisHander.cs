// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using Rect = SIL.FieldWorks.Common.ViewsInterfaces.Rect;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This class handles the functions of the combo box that is used to choose a
	/// different existing analysis.
	/// </summary>
	internal class ChooseAnalysisHandler : IComboHandler
	{
		int m_hvoAnalysis; // The current 'analysis', may be wordform, analysis, gloss.
		bool m_fInitializing = false; // true to suppress AnalysisChosen while setting up combo.
		LcmCache m_cache;
		IComboList m_combo;
		const string ksMissingString = "---";
		const string ksPartSeparator = "   ";

		/// <summary>
		/// this is something of a hack until we convert to using a style sheet
		/// </summary>
		protected static int s_baseFontSize = 12;

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
		public int Source { get; }

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
		public HvoTssComboItem SelectedItem => m_combo?.SelectedItem as HvoTssComboItem;

		internal IVwStylesheet StyleSheet => m_combo?.StyleSheet;

		/// <summary>
		/// Create one, typically from the Sandbox, using an existing combo box or list. The caller is responsible
		/// to display it; the Show method should not be called, especially if comboList is actually not a combo box.
		/// </summary>
		public ChooseAnalysisHandler(LcmCache cache, int hvoSrc, int hvoAnalysis, IComboList comboList)
		{
			m_combo = comboList;
			m_cache = cache;
			Source = hvoSrc;
			m_hvoAnalysis = hvoAnalysis;
			m_combo.SelectedIndexChanged += m_combo_SelectedIndexChanged;
			m_combo.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
		}

		#region IDisposable & Co. implementation

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;

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
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_combo != null)
				{
					m_combo.SelectedIndexChanged -= m_combo_SelectedIndexChanged;
					var combo = m_combo as FwComboBox;
					if (combo != null && combo.Parent == null)
					{
						combo.Dispose();
					}
					else
					{
						var clb = (m_combo as ComboListBox);
						clb?.Dispose();
					}
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cache = null;
			Owner = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Add an item to the combo box, but only if its text is non-empty.
		/// </summary>
		/// <param name="co"></param>
		/// <param name="text"></param>
		/// <param name="fPossibleCurrent">generally true; false for items like "new analysis" that
		/// can't possibly be the current item, though hvoAnalysis might match.</param>
		/// <param name="tag">tag to specify an otherwise ambigious item.</param>
		private void AddItem(ICmObject co, ITsString text, bool fPossibleCurrent, int tag = 0)
		{
			if (text.Length == 0)
			{
				return;
			}
			var hvoObj = co?.Hvo ?? 0;
			var newItem = new HvoTssComboItem(hvoObj, text, tag);
			m_combo.Items.Add(newItem);
			if (fPossibleCurrent && hvoObj == m_hvoAnalysis)
			{
				m_combo.SelectedItem = newItem;
			}
		}

		private void AddSeparatorLine()
		{
			//review
			var builder = TsStringUtils.MakeStrBldr();
			builder.Replace(0,0,"-------", null);
			builder.SetIntPropValues(0, builder.Length, (int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, m_cache.DefaultUserWs);
			var newItem = new HvoTssComboItem(-1, builder.GetString()); //hack todo
			m_combo.Items.Add(newItem);
		}

		/// <summary>
		/// Initialize the combo contents.
		/// </summary>
		public void SetupCombo()
		{
			m_fInitializing = true;
			var wordform = Owner.GetWordformOfAnalysis();

			// Add the analyses, and recursively the other items.
			foreach (var wa in wordform.AnalysesOC)
			{
				var o = wa.GetAgentOpinion(m_cache.LangProject.DefaultUserAgent);
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
		private void AddAnalysisItems(IWfiAnalysis wa)
		{
			AddItem(wa, MakeAnalysisStringRep(wa, m_cache, StyleSheet != null, (Owner as SandboxBase).RawWordformWs), true);
			foreach (var gloss in wa.MeaningsOC)
			{
				AddItem(gloss, MakeGlossStringRep(gloss, m_cache, StyleSheet != null), true);
			}

			//add the "new word gloss" option
			AddItem(wa, MakeSimpleString(ITextStrings.ksNewWordGloss), false, WfiGlossTags.kClassId);
		}

		protected ITsString MakeSimpleString(string str)
		{
			var builder = TsStringUtils.MakeStrBldr();
			var bldr = TsStringUtils.MakePropsBldr();
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, m_cache.DefaultUserWs);
			bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, s_baseFontSize * 800);
			bldr.SetStrPropValue((int)FwTextStringProp.kstpFontFamily, MiscUtils.StandardSansSerif);
			builder.Replace(0,0,str , bldr.GetTextProps());
			return builder.GetString();
		}

		// Generate a suitable string representation of a WfiGloss.
		// Todo: finish implementing (add the gloss!)
		internal static ITsString MakeGlossStringRep(IWfiGloss wg, LcmCache lcmCache, bool fUseStyleSheet)
		{
			var tsb = TsStringUtils.MakeStrBldr();
			var wa = wg.Owner as IWfiAnalysis;
			var category = wa.CategoryRA;
			if (category != null)
			{
				var tssPos = category.Abbreviation.get_String( lcmCache.DefaultAnalWs);
				tsb.Replace(0, 0, tssPos.Text, PartOfSpeechTextProperties(lcmCache,false, fUseStyleSheet));
			}
			else
			{
				tsb.Replace(0, 0, ksMissingString, PartOfSpeechTextProperties(lcmCache,false, fUseStyleSheet));
			}
			tsb.Replace(tsb.Length, tsb.Length, " ", null);
			tsb.Replace(tsb.Length, tsb.Length, wg.Form.get_String(lcmCache.DefaultAnalWs).Text, GlossTextProperties(lcmCache, false, fUseStyleSheet));

			//indent
			tsb.Replace(0,0, "    ", null);
			return tsb.GetString();
		}

		// Make a string representing a WfiAnalysis, suitable for use in a combo box item.
		internal static ITsString MakeAnalysisStringRep(IWfiAnalysis wa, LcmCache lcmCache, bool fUseStyleSheet, int wsVern)
		{
			var posTextProperties = PartOfSpeechTextProperties(lcmCache, true, fUseStyleSheet);
			var formTextProperties = FormTextProperties(lcmCache, fUseStyleSheet, wsVern);
			var glossTextProperties = GlossTextProperties(lcmCache, true, fUseStyleSheet);
			var tsb = TsStringUtils.MakeStrBldr();
			var cmorph = wa.MorphBundlesOS.Count;
			if (cmorph == 0)
			{
				return TsStringUtils.MakeString(ITextStrings.ksNoMorphemes, lcmCache.DefaultUserWs);
			}
			var fRtl = lcmCache.ServiceLocator.WritingSystemManager.Get(wsVern).RightToLeftScript;
			var start = 0;
			var lim = cmorph;
			var increment = 1;
			if (fRtl)
			{
				start = cmorph - 1;
				lim = -1;
				increment = -1;
			}
			for (var i = start; i != lim; i += increment)
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
				var ichForm = tsb.Length;
				tsb.ReplaceTsString(ichForm, ichForm, tssForm);
				tsb.SetProperties(ichForm, tsb.Length,formTextProperties);

				// add category (part of speech)
				var msa = mb.MsaRA;
				tsb.Replace(tsb.Length, tsb.Length, " ", null);
				var ichMinMsa = tsb.Length;
				var interlinName = ksMissingString;
				if (msa != null)
				{
					interlinName = msa.InterlinearAbbr;
				}
				tsb.Replace(ichMinMsa, ichMinMsa, interlinName, posTextProperties);

				//add sense
				var sense = mb.SenseRA;
				tsb.Replace(tsb.Length, tsb.Length, " ", null);
				var ichMinSense = tsb.Length;
				if (sense != null)
				{
					var tssGloss = sense.Gloss.get_String(lcmCache.DefaultAnalWs);
					tsb.Replace(ichMinSense, ichMinSense, tssGloss.Text, glossTextProperties);
				}
				else
					tsb.Replace(ichMinSense, ichMinSense, ksMissingString, glossTextProperties);

				// Enhance JohnT: use proper seps.
				tsb.Replace(tsb.Length, tsb.Length, ksPartSeparator, null);
			}
			// Delete the final separator. (Enhance JohnT: this needs to get smarter when we do
			// real seps.)
			var ichFrom = tsb.Length - ksPartSeparator.Length;
			if (ichFrom < 0)
			{
				ichFrom = 0;
			}
			tsb.Replace(ichFrom, tsb.Length, "", null);
			return tsb.GetString();
		}

		/// <summary />
		public static ITsTextProps FormTextProperties(LcmCache lcmCache, bool fUseStyleSheet, int wsVern)
		{
			var color =(int) CmObjectUi.RGB(Color.DarkBlue);
			var bldr = TsStringUtils.MakePropsBldr();
			if (!fUseStyleSheet)
			{
				bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, s_baseFontSize * 1000);
			}
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, wsVern);
			bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, color);
			return bldr.GetTextProps();
		}

		public static ITsTextProps GlossTextProperties(LcmCache lcmCache, bool inAnalysisLine, bool fUseStyleSheet)
		{
			var color =(int) CmObjectUi.RGB(Color.DarkRed);
			var bldr = TsStringUtils.MakePropsBldr();
			if (!fUseStyleSheet)
			{
				bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, s_baseFontSize * 1000);
			}
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, lcmCache.DefaultAnalWs);

			if (inAnalysisLine)
			{
				bldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum, (int)FwSuperscriptVal.kssvSuper);
			}

			bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, color);
			return bldr.GetTextProps();
		}

		/// <summary />
		public static ITsTextProps PartOfSpeechTextProperties(LcmCache lcmCache, bool inAnalysisLine, bool fUseStyleSheet)
		{
			var color =(int) CmObjectUi.RGB(Color.Green);
			var bldr = TsStringUtils.MakePropsBldr();
			if (!fUseStyleSheet)
			{
				bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, (int)( s_baseFontSize * 1000* .8));
			}
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, lcmCache.DefaultAnalWs);

			if (inAnalysisLine)
			{
				bldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum, (int)FwSuperscriptVal.kssvSub);
			}



			bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, color);
			return bldr.GetTextProps();
		}

		/// <summary>
		/// Get the site where the combo or combo list is displayed.
		/// </summary>
		public SandboxBase Owner { get; set; }

		/// <summary>
		/// Display the combo box at the specified location, or the list box pulled down from the specified location.
		/// </summary>
		public void Activate(Rect loc)
		{
			var combo = m_combo as FwComboBox;
			if (combo != null)
			{

				combo.Location = new Point(loc.left, loc.top);
				// 21 is the default height of a combo, the smallest reasonable size.
				combo.Size = new Size(Math.Max(loc.right - loc.left + 30, 200), Math.Max( loc.bottom - loc.top, 50));
				if (!Owner.Controls.Contains(combo))
				{
					Owner.Controls.Add(combo);
				}
			}
			else
			{
				var c = (m_combo as ComboListBox);
				c.AdjustSize(500, 400); // these are maximums!
				c.Launch(Owner.RectangleToScreen(loc), Screen.GetWorkingArea(Owner));
			}
		}

		/// <summary>
		/// Required interface method. We don't have a particular morph selected so answer zero.
		/// </summary>
		public int SelectedMorphHvo => 0;

		/// <summary>
		/// Required interface method, not relevant for this class.
		/// </summary>
		public void HandleSelectIfActive()
		{
		}

		/// <summary>
		/// Handle the user selecting something in the combo.
		/// </summary>
		private void m_combo_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_fInitializing)
			{
				return;
			}
			var x = ((HvoTssComboItem)m_combo.SelectedItem).Hvo;
			if (x >= 0) // could be a separator
			{
				m_hvoAnalysis = x;
			}
			Hide();	// Moved here from the end as the 'AnalysisChosen' method can
					// cause the current object to be disposed of.  Not very nice...
					// if there are other calls yet to be invoked on the object.
					// LT-5775: this is no real fix, as a real fix would understand
					// why the RootBox.MakeTextSelection would cause this object to
					// be disposed.  This I don't know...
			AnalysisChosen?.Invoke(this, new EventArgs());

		}

		/// <summary>
		/// Implement required interface method; will never be called because editing not allowed.
		/// </summary>
		public virtual bool HandleReturnKey()
		{
			return false;
		}

		/// <summary>
		/// Get rid of the combo, typically when the user clicks outside it.
		/// </summary>
		public void Hide()
		{
			var combo = m_combo as FwComboBox;
			if (combo != null && Owner.Controls.Contains(combo))
			{
				Owner.Controls.Remove(combo);
			}
			var clb = m_combo as ComboListBox;
			clb?.HideForm();
		}
	}
}