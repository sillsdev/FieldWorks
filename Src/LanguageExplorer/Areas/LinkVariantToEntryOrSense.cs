// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas
{
	public partial class LinkVariantToEntryOrSense : LinkEntryOrSenseDlg
	{
		private PossibilityListPopupTreeManager m_tcManager;
		/// <summary>
		/// when calling the dialog from "Variant Of" context...without an existing variant used as
		/// m_startingEntry, we need to pass in the lexeme form of the variant to create the variant
		/// in order to link it to an entry. Mutually exclusive with m_startingEntry.
		/// </summary>
		private ITsString m_tssVariantLexemeForm;
		/// <summary>
		/// TODO: refactor all related logic to InsertVariantDlg or GoDlg.
		/// when calling the dialog from an "Insert Variant" context this
		/// flag is used to indicate that m_startingEntry is a componentLexeme
		/// rather than the variant
		/// </summary>
		private bool m_fBackRefToVariant;

		public LinkVariantToEntryOrSense()
		{
			InitializeComponent();
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				m_tcManager?.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Sets the DLG info.
		/// </summary>
		public void SetDlgInfo(LcmCache cache, ITsString tssVariantLexemeForm)
		{
			m_tssVariantLexemeForm = tssVariantLexemeForm;
			base.SetDlgInfo(cache, null);
		}


		/// <summary>
		/// when calling the dialog from an "Insert Variant" context this
		/// constructor is used to indicate that m_startingEntry is a componentLexeme
		/// rather than the variant
		/// </summary>
		protected void SetDlgInfoForComponentLexeme(LcmCache cache, IVariantComponentLexeme componentLexeme)
		{
			m_fBackRefToVariant = true;
			ILexEntry startingEntry;
			if (componentLexeme.ClassID == LexEntryTags.kClassId)
			{
				startingEntry = componentLexeme as ILexEntry;
			}
			else
			{
				startingEntry = componentLexeme.Owner as ILexEntry;
			}
			SetDlgInfo(cache, startingEntry);
			// we are looking for an existing variant form
			// so hide the Entry/Sense radio group box.
			grplbl.Visible = false;
			// also hide variant type.
			tcVariantTypes.Visible = false;
			lblVariantType.Visible = false;
			m_fGetVariantEntryTypeFromTreeCombo = false;
			lblCreateEntry.Visible = false;
			// The dialog title and other labels need to reflect "Insert Variant" context.
			m_formLabel.Text = LanguageExplorerControls.ks_Variant;
			Text = LanguageExplorerControls.ksFindVariant;
			m_btnInsert.Text = LanguageExplorerControls.ks_Create;
			// We disable the "Create" button when we don't have text in the Find textbox.
			UpdateButtonCreateNew();
		}

		protected override void SetDlgInfo(LcmCache cache, WindowParams wp, int ws)
		{
			tcVariantTypes.WritingSystemFactory = cache.WritingSystemFactory;
			tcVariantTypes.WritingSystemCode = ws;
			tcVariantTypes.Font = new System.Drawing.Font(cache.ServiceLocator.WritingSystemManager.Get(ws).DefaultFontName, 10);
			tcVariantTypes.StyleSheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
			base.SetDlgInfo(cache, wp, ws);
			// load the variant type possibilities.
			LoadVariantTypes();
		}

		/// <summary>
		/// return null to use visual studio designer's settings.
		/// </summary>
		protected override WindowParams DefaultWindowParams => null;

		protected override string PersistenceLabel => "LinkVariantToEntryOrSense";

		private void LoadVariantTypes()
		{
			// by default, select the first variant type.
			var hvoTarget = m_cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS[0].Hvo;
			LoadVariantTypesAndSelectTarget(hvoTarget);
		}

		private void LoadVariantTypesAndSelectTarget(int hvoTarget)
		{
			if (m_tcManager == null)
			{
				m_tcManager = new PossibilityListPopupTreeManager(tcVariantTypes, m_cache, new FlexComponentParameters(PropertyTable, Publisher, Subscriber), m_cache.LangProject.LexDbOA.VariantEntryTypesOA, m_cache.DefaultUserWs, false, this);
			}
			m_tcManager.LoadPopupTree(hvoTarget);
		}

		/// <summary>
		/// in some contexts (e.g. "Insert Variant" in detail pane) we hide tcVariantTypes,
		/// so the user can't select them.
		/// </summary>
		bool m_fGetVariantEntryTypeFromTreeCombo = true;
		/// <summary>
		/// The VariantEntryType possibilty choice.
		/// </summary>
		public int SelectedVariantEntryTypeHvo
		{
			get
			{
				if (!m_fGetVariantEntryTypeFromTreeCombo)
				{
					return 0;
				}
				var selectedNode = tcVariantTypes.SelectedNode;
				return (selectedNode as HvoTreeNode)?.Hvo ?? 0;
			}
		}

		/// <summary>
		/// the variant LexEntryRef, that was created or found by the state of the dialog
		/// after user clicks the OK button.
		/// </summary>
		public ILexEntryRef VariantEntryRefResult { get; private set; }

		/// <summary>
		/// indicates whether VariantEntryRefResult is new.
		/// </summary>
		public bool NewlyCreatedVariantEntryRefResult { get; private set; }

		/// <summary>
		/// If we get here without passing through btnOK_Click, and we're not canceling the
		/// dialog, then we need to call btnOK_Click to set the return result.  See LT-9776.
		/// </summary>
		protected override void OnClosing(CancelEventArgs e)
		{
			if (DialogResult != DialogResult.Cancel && !e.Cancel && VariantEntryRefResult == null)
			{
				btnOK_Click(null, null);
			}
			base.OnClosing(e);
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			if (SelectedObject == null)
			{
				return; // odd. nothing more to do.
			}
			ILexEntry variant;
			IVariantComponentLexeme componentLexeme;
			ILexEntryType selectedEntryType;
			GetVariantAndComponentAndSelectedEntryType(out variant, out componentLexeme, out selectedEntryType);
			var matchingEntryRef = FindMatchingEntryRef(variant, componentLexeme, selectedEntryType);
			try
			{
				UndoableUnitOfWorkHelper.Do(LanguageExplorerControls.ksUndoAddVariant, LanguageExplorerControls.ksRedoAddVariant, m_cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					if (matchingEntryRef != null)
					{
						// we found a matching ComponentLexeme. See if we can find the selected type.
						// if the selected type does not yet exist, add it.
						if (selectedEntryType != null && !matchingEntryRef.VariantEntryTypesRS.Contains(selectedEntryType))
						{
							matchingEntryRef.VariantEntryTypesRS.Add(selectedEntryType);
						}

						VariantEntryRefResult = matchingEntryRef;
						NewlyCreatedVariantEntryRefResult = false;
					}
					else
					{
						// otherwise we need to create a new LexEntryRef.
						NewlyCreatedVariantEntryRefResult = true;
						VariantEntryRefResult = variant != null ? variant.MakeVariantOf(componentLexeme, selectedEntryType) : componentLexeme.CreateVariantEntryAndBackRef(selectedEntryType, m_tssVariantLexemeForm);
					}
				});
			}
			catch (ArgumentException)
			{
				MessageBoxes.ReportLexEntryCircularReference(componentLexeme, variant, false);
			}
		}

		/// <summary>
		/// extracts the variant and component from the dialog, depending upon whether we're
		/// called from an "Insert Variant" or "Variant Of..." context.
		/// </summary>
		private void GetVariantAndComponentAndSelectedEntryType(out ILexEntry variant, out IVariantComponentLexeme componentLexeme, out ILexEntryType selectedEntryType)
		{
			if (m_fBackRefToVariant)
			{
				// in "Insert Variant" contexts,
				// we're calling the dialog from the component lexeme, so consider SelectedID as the variant.
				componentLexeme = m_startingEntry;
				variant = SelectedObject as ILexEntry;
			}
			else
			{
				// in "Variant of..." contexts,
				// we're calling the dialog from the variant, so consider SelectedID the componentLexeme.
				variant = m_startingEntry;
				componentLexeme = SelectedObject as IVariantComponentLexeme;
			}
			selectedEntryType = m_fGetVariantEntryTypeFromTreeCombo ? m_cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(SelectedVariantEntryTypeHvo) : null;
		}

		private ILexEntryRef FindMatchingEntryRef(ILexEntry variant, IVariantComponentLexeme componentLexeme, ILexEntryType selectedEntryType)
		{
			return variant != null ? variant.FindMatchingVariantEntryRef(componentLexeme, selectedEntryType) : componentLexeme.FindMatchingVariantEntryBackRef(selectedEntryType, m_tssVariantLexemeForm);
		}

		/// <summary>
		/// update the buttons when the state of the dialog changes.
		/// </summary>
		protected override void ResetMatches(string searchKey)
		{
			base.ResetMatches(searchKey);
			// update CreateNew button when the Find textbox or WritingSystems combo changes.
			UpdateButtonCreateNew();
		}

		/// <summary>
		/// update the OK button and selected entry type according to whether or not SelectedID
		/// is already linked.
		/// </summary>
		protected override void HandleMatchingSelectionChanged()
		{
			base.HandleMatchingSelectionChanged();
			// by default, btnOK should be set to "Add Variant"
			m_btnOK.Text = LanguageExplorerControls.ksAddVariant;
			if (SelectedObject != null)
			{
				// detect whether SelectedID already matches an existing EntryRef relationship.
				// If so, we want to "Use" it rather than say "Add" when clicking OK.
				ILexEntry variant;
				IVariantComponentLexeme componentLexeme;
				ILexEntryType selectedEntryType;
				GetVariantAndComponentAndSelectedEntryType(out variant, out componentLexeme, out selectedEntryType);
				var matchingEntryRef = FindMatchingEntryRef(variant, componentLexeme, selectedEntryType);
				if (matchingEntryRef != null)
				{
					// Indicate to the user that the SelectedID matches an existing EntryRef relationship.
					m_btnOK.Text = m_fBackRefToVariant ? LanguageExplorerControls.ks_OK : LanguageExplorerControls.ksUseEntry;
					// if the VariantTypes combo is visible, select the last appended type of the matching relationship.
					if (tcVariantTypes.Visible && matchingEntryRef.VariantEntryTypesRS.Count > 0)
					{
						var hvoLastAppendedType = matchingEntryRef.VariantEntryTypesRS[matchingEntryRef.VariantEntryTypesRS.Count - 1].Hvo;
						LoadVariantTypesAndSelectTarget(hvoLastAppendedType);
					}
				}
			}
		}

		private void UpdateButtonCreateNew()
		{
			if (m_fBackRefToVariant)
			{
				// enable the "Create Entry" button if we can make a current vernacular string
				// from the Find text box and the Writing Systems combo box.
				var tssNewVariantLexemeForm = CreateVariantTss();
				// enable the button if we didn't find an existing one.
				m_btnInsert.Enabled = (tssNewVariantLexemeForm != null);
			}
		}

		protected override void m_btnInsert_Click(object sender, EventArgs e)
		{
			if (!m_fBackRefToVariant)
			{
				base.m_btnInsert_Click(sender, e);
			}
			// the user wants to try to create a variant with a lexeme form
			// built from the current state of our Find text box and WritingSystem combo.
			var tssNewVariantLexemeForm = CreateVariantTss();
			if (tssNewVariantLexemeForm == null)
			{
				return;
			}
			// we need to create the new LexEntryRef and its variant from the starting entry.
			UndoableUnitOfWorkHelper.Do(LanguageExplorerControls.ksUndoCreateVarEntry, LanguageExplorerControls.ksRedoCreateVarEntry, m_startingEntry, () =>
			{
				VariantEntryRefResult = m_startingEntry.CreateVariantEntryAndBackRef(null, tssNewVariantLexemeForm);
			});
			NewlyCreatedVariantEntryRefResult = true;
			m_selObject = VariantEntryRefResult.Owner as ILexEntry;
			m_fNewlyCreated = true;
			DialogResult = DialogResult.OK;
			Close();
		}

		public override ICmObject SelectedObject
		{
			get
			{
				if (m_fBackRefToVariant && m_fNewlyCreated && VariantEntryRefResult != null)
				{
					// we inserted a new variant and linked it to m_startingEntry,
					// return the owner of the variant ref to get the new variant.
					return VariantEntryRefResult.Owner;
				}
				return base.SelectedObject;
			}
		}

		/// <summary>
		/// try to create a tss based upon the state of the Find text box and WritingSystems combo.
		/// </summary>
		private ITsString CreateVariantTss()
		{
			// only create a variant tss when we're calling the dialog up from an entry
			// upon which we want to add a variant.
			if (!m_fBackRefToVariant || m_tbForm.Text == null)
			{
				return null;
			}
			ITsString tssNewVariantLexemeForm = null;
			var trimmed = m_tbForm.Text.Trim();
			if (trimmed.Length > 0 && m_cbWritingSystems.SelectedItem != null)
			{
				var ws = (CoreWritingSystemDefinition)m_cbWritingSystems.SelectedItem;
				if (m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Contains(ws))
				{
					tssNewVariantLexemeForm = TsStringUtils.MakeString(trimmed, ws.Handle);
				}
			}
			return tssNewVariantLexemeForm;
		}
	}
}