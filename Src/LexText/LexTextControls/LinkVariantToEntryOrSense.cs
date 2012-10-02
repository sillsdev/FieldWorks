using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class LinkVariantToEntryOrSense : SIL.FieldWorks.LexText.Controls.LinkEntryOrSenseDlg
	{
		PopupTreeManager m_tcManager = null;
		/// <summary>
		/// when calling the dialog from "Variant Of" context...without an existing variant used as
		/// m_startingEntry, we need to pass in the lexeme form of the variant to create the variant
		/// in order to link it to an entry. Mutually exclusive with m_startingEntry.
		/// </summary>
		ITsString m_tssVariantLexemeForm = null;
		/// <summary>
		/// TODO: refactor all related logic to InsertVariantDlg or GoDlg.
		/// when calling the dialog from an "Insert Variant" context this
		/// flag is used to indicate that m_startingEntry is a componentLexeme
		/// rather than the variant
		/// </summary>
		bool m_fBackRefToVariant = false;

		public LinkVariantToEntryOrSense()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_tcManager != null && !m_tcManager.IsDisposed)
					m_tcManager.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="tssForm">if startingEntry does not (yet) exist, then use the form so we
		/// can find or create one matching the form.</param>
		public void SetDlgInfo(FdoCache cache, Mediator mediator, ITsString tssVariantLexemeForm)
		{
			m_tssVariantLexemeForm = tssVariantLexemeForm;
			base.SetDlgInfo(cache, mediator, null);
		}


		/// <summary>
		/// when calling the dialog from an "Insert Variant" context this
		/// constructor is used to indicate that m_startingEntry is a componentLexeme
		/// rather than the variant
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="componentLexeme">the entry we wish to find or create a variant for.</param>
		protected void SetDlgInfoForComponentLexeme(FdoCache cache, Mediator mediator, IVariantComponentLexeme componentLexeme)
		{
			m_fBackRefToVariant = true;
			ILexEntry startingEntry = null;
			if (componentLexeme.ClassID == LexEntry.kclsidLexEntry)
			{
				startingEntry = componentLexeme as LexEntry;
			}
			else
			{
				int hvoEntry = cache.GetOwnerOfObjectOfClass(componentLexeme.Hvo, LexEntry.kclsidLexEntry);
				if (hvoEntry != 0)
					startingEntry = LexEntry.CreateFromDBObject(cache, hvoEntry);
			}
			base.SetDlgInfo(cache, mediator, startingEntry);
			// we are looking for an existing variant form
			// so hide the Entry/Sense radio group box.
			grplbl.Visible = false;
			// also hide variant type.
			tcVariantTypes.Visible = false;
			lblVariantType.Visible = false;
			m_fGetVariantEntryTypeFromTreeCombo = false;
			lblCreateEntry.Visible = false;

			// The dialog title and other labels need to reflect "Insert Variant" context.
			m_formLabel.Text = LexTextControls.ks_Variant;
			this.Text = LexTextControls.ksFindVariant;
			btnInsert.Text = LexTextControls.ks_Create;
			// We disable the "Create" button when we don't have text in the Find textbox.
			UpdateButtonCreateNew();
		}


		protected override void SetDlgInfo(FdoCache cache, WindowParams wp, XCore.Mediator mediator, int wsVern)
		{
			WritingSystemAndStylesheetHelper.SetupWritingSystemAndStylesheetInfo(tcVariantTypes,
				cache, mediator, cache.DefaultUserWs);
			base.SetDlgInfo(cache, wp, mediator, wsVern);
			// load the variant type possibilities.
			LoadVariantTypes();
		}

		/// <summary>
		/// return null to use visual studio designer's settings.
		/// </summary>
		protected override WindowParams DefaultWindowParams
		{
			get { return null; }
		}

		protected override string PersistenceLabel
		{
			get { return "LinkVariantToEntryOrSense"; }
		}

		private void LoadVariantTypes()
		{
			// by default, select the first variant type.
			int hvoTarget = m_cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS[0].Hvo;
			LoadVariantTypesAndSelectTarget(hvoTarget);
		}

		private void LoadVariantTypesAndSelectTarget(int hvoTarget)
		{
			if (m_tcManager == null)
			{
				m_tcManager = new PossibilityListPopupTreeManager(tcVariantTypes, m_cache,
						 m_cache.LangProject.LexDbOA.VariantEntryTypesOA, m_cache.DefaultUserWs, false, this);
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
					return 0;
				TreeNode selectedNode = tcVariantTypes.SelectedNode;
				if (selectedNode == null || !(selectedNode is HvoTreeNode))
					return 0;
				return (selectedNode as HvoTreeNode).Hvo;
			}
		}

		ILexEntryRef m_variantEntryRefResult = null;
		/// <summary>
		/// the variant LexEntryRef, that was created or found by the state of the dialog
		/// after user clicks the OK button.
		/// </summary>
		public ILexEntryRef VariantEntryRefResult
		{
			get { return m_variantEntryRefResult; }
		}

		bool m_fNewlyCreatedVariantEntryRef = false;
		/// <summary>
		/// indicates whether VariantEntryRefResult is new.
		/// </summary>
		public bool NewlyCreatedVariantEntryRefResult
		{
			get { return m_fNewlyCreatedVariantEntryRef; }
		}

		/// <summary>
		/// If we get here without passing through btnOK_Click, and we're not canceling the
		/// dialog, then we need to call btnOK_Click to set the return result.  See LT-9776.
		/// </summary>
		protected override void OnClosing(CancelEventArgs e)
		{
			if (this.DialogResult != DialogResult.Cancel && !e.Cancel && m_variantEntryRefResult == null)
				btnOK_Click(null, null);
			base.OnClosing(e);
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			if (SelectedID == 0)
				return; // odd. nothing more to do.

			ILexEntry variant;
			IVariantComponentLexeme componentLexeme;
			ILexEntryType selectedEntryType;
			GetVariantAndComponentAndSelectedEntryType(out variant, out componentLexeme, out selectedEntryType);

			ILexEntryRef matchingEntryRef = FindMatchingEntryRef(variant, componentLexeme, selectedEntryType);
			if (matchingEntryRef != null)
			{
				// we found a matching ComponentLexeme. See if we can find the selected type.
				// if the selected type does not yet exist, add it.
				if (selectedEntryType != null &&
					!matchingEntryRef.VariantEntryTypesRS.Contains(selectedEntryType))
				{
					matchingEntryRef.VariantEntryTypesRS.Append(selectedEntryType);
				}

				m_variantEntryRefResult = matchingEntryRef;
				m_fNewlyCreatedVariantEntryRef = false;
				return;
			}

			// otherwise we need to create a new LexEntryRef.
			m_fNewlyCreatedVariantEntryRef = true;
			if (variant != null)
			{
				m_variantEntryRefResult = variant.MakeVariantOf(componentLexeme, selectedEntryType);
			}
			else
			{
				m_variantEntryRefResult = componentLexeme.CreateVariantEntryAndBackRef(selectedEntryType,
					m_tssVariantLexemeForm);
			}
		}

		/// <summary>
		/// extracts the variant and component from the dialog, depending upon whether we're
		/// called from an "Insert Variant" or "Variant Of..." context.
		/// </summary>
		/// <param name="variant"></param>
		/// <param name="componentLexeme"></param>
		private void GetVariantAndComponentAndSelectedEntryType(out ILexEntry variant, out IVariantComponentLexeme componentLexeme, out ILexEntryType selectedEntryType)
		{
			variant = null;
			componentLexeme = null;
			if (m_fBackRefToVariant)
			{
				// in "Insert Variant" contexts,
				// we're calling the dialog from the component lexeme, so consider SelectedID as the variant.
				componentLexeme = m_startingEntry;
				variant = LexEntry.CreateFromDBObject(m_cache, SelectedID);
			}
			else
			{
				// in "Variant of..." contexts,
				// we're calling the dialog from the variant, so consider SelectedID the componentLexeme.
				variant = m_startingEntry;
				componentLexeme = CmObject.CreateFromDBObject(m_cache, SelectedID) as IVariantComponentLexeme;
			}
			if (m_fGetVariantEntryTypeFromTreeCombo)
				selectedEntryType = LexEntryType.CreateFromDBObject(m_cache, SelectedVariantEntryTypeHvo);
			else
				selectedEntryType = null;
		}

		private ILexEntryRef FindMatchingEntryRef(ILexEntry variant, IVariantComponentLexeme componentLexeme, ILexEntryType selectedEntryType)
		{
			ILexEntryRef matchingEntryRef = null;
			if (variant != null)
			{
				// see if the starting entry has the SelectedID already as a ComponentLexeme
				matchingEntryRef = (variant as LexEntry).FindMatchingVariantEntryRef(componentLexeme,
					selectedEntryType);
			}
			else
			{
				// determine whether the selected entry or sense is already
				// linked to an existing variant with the given lexeme form.
				matchingEntryRef = LexEntry.FindMatchingVariantEntryBackRef(componentLexeme,
					selectedEntryType, m_tssVariantLexemeForm);
			}
			return matchingEntryRef;
		}

		/// <summary>
		/// update the buttons when the state of the dialog changes.
		/// </summary>
		/// <param name="searchKey"></param>
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
			btnOK.Text = LexTextControls.ksAddVariant;
			if (SelectedID != 0)
			{
				// detect whether SelectedID already matches an existing EntryRef relationship.
				// If so, we want to "Use" it rather than say "Add" when clicking OK.

				ILexEntry variant;
				IVariantComponentLexeme componentLexeme;
				ILexEntryType selectedEntryType;
				GetVariantAndComponentAndSelectedEntryType(out variant, out componentLexeme, out selectedEntryType);
				ILexEntryRef matchingEntryRef = FindMatchingEntryRef(variant, componentLexeme, selectedEntryType);
				if (matchingEntryRef != null)
				{
					// Indicate to the user that the SelectedID matches an existing EntryRef relationship.
					if (m_fBackRefToVariant)
						btnOK.Text = LexTextControls.ks_OK;
					else
						btnOK.Text = LexTextControls.ksUseEntry;
					// if the VariantTypes combo is visible, select the last appended type of the matching relationship.
					if (tcVariantTypes.Visible && matchingEntryRef.VariantEntryTypesRS.Count > 0)
					{
						int hvoLastAppendedType = matchingEntryRef.VariantEntryTypesRS[matchingEntryRef.VariantEntryTypesRS.Count - 1].Hvo;
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
				ITsString tssNewVariantLexemeForm = CreateVariantTss();
				// enable the button if we didn't find an existing one.
				btnInsert.Enabled = (tssNewVariantLexemeForm != null);
			}
		}

		protected override void btnInsert_Click(object sender, EventArgs e)
		{
			if (!m_fBackRefToVariant)
				base.btnInsert_Click(sender, e);

			// the user wants to try to create a variant with a lexeme form
			// built from the current state of our Find text box and WritingSystem combo.
			ITsString tssNewVariantLexemeForm = CreateVariantTss();
			if (tssNewVariantLexemeForm == null)
				return;

			// we need to create the new LexEntryRef and its variant from the starting entry.
			m_variantEntryRefResult = m_startingEntry.CreateVariantEntryAndBackRef(null, tssNewVariantLexemeForm);
			m_fNewlyCreatedVariantEntryRef = true;
			m_selEntryID = m_variantEntryRefResult.OwnerHVO;
			m_fNewlyCreated = true;
			DialogResult = DialogResult.OK;
			Close();
		}

		public override int SelectedID
		{
			get
			{
				if (m_fBackRefToVariant && m_fNewlyCreated && m_variantEntryRefResult != null)
				{
					// we inserted a new variant and linked it to m_startingEntry,
					// return the owner of the variant ref to get the new variant.
					return m_variantEntryRefResult.OwnerHVO;
				}
				else
				{
					return base.SelectedID;
				}
			}
		}

		/// <summary>
		/// try to create a tss based upon the state of the Find text box and WritingSystems combo.
		/// </summary>
		/// <returns></returns>
		private ITsString CreateVariantTss()
		{
			// only create a variant tss when we're calling the dialog up from an entry
			// upon which we want to add a variant.
			if (!m_fBackRefToVariant || m_tbForm.Text == null)
				return null;
			ITsString tssNewVariantLexemeForm = null;
			string trimmed = m_tbForm.Text.Trim();
			if (trimmed.Length > 0 && m_cbWritingSystems.SelectedItem != null)
			{
				ILgWritingSystem ws = m_cbWritingSystems.SelectedItem as ILgWritingSystem;
				if (m_cache.LangProject.CurVernWssRS.Contains(ws))
					tssNewVariantLexemeForm = StringUtils.MakeTss(trimmed, ws.Hvo);
			}
			return tssNewVariantLexemeForm;
		}

	}

	/// <summary>
	/// (LT-9283)
	/// "Insert Variant" should look like the GoDlg layout, but we still want some
	/// of the extra logic in LinkVariantToEntryOrSense, (e.g. determine whether
	/// we've already inserted the selected variant.)
	///
	/// TODO: refactor with LinkVariantToEntryOrSense to put all m_fBackRefToVariant logic here,
	/// else allow GoDlg to support additional Variant matching logic.
	/// </summary>
	public class InsertVariantDlg : LinkVariantToEntryOrSense
	{
		public InsertVariantDlg()
		{
			// inherit some layout controls from GoDlg
			InitializeSomeComponentsLikeGoDlg();
		}

		private void InitializeSomeComponentsLikeGoDlg()
		{
			this.SuspendLayout();
			// first reapply some BaseGoDlg settings
			System.ComponentModel.ComponentResourceManager resources =
				new System.ComponentModel.ComponentResourceManager(typeof (BaseGoDlg));
			ApplySomeResources(resources);
			// then apply some GoDlg settings
			resources = new System.ComponentModel.ComponentResourceManager(typeof(GoDlg));
			ApplySomeResources(resources);
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		private void ApplySomeResources(ComponentResourceManager resources)
		{
			//
			// btnClose
			//
			resources.ApplyResources(this.btnClose, "btnClose");
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			//
			// btnInsert
			//
			resources.ApplyResources(this.btnInsert, "btnInsert");
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			//
			// matchingEntries
			//
			resources.ApplyResources(this.matchingEntries, "matchingEntries");
			////
			//// GoDlg
			////
			resources.ApplyResources(this, "$this");
		}

		/// <summary>
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="componentLexeme">the entry we wish to find or create a variant for.</param>
		public void SetDlgInfo(FdoCache cache, Mediator mediator, IVariantComponentLexeme componentLexeme)
		{
			this.SetDlgInfoForComponentLexeme(cache, mediator, componentLexeme);
		}
	}
}
