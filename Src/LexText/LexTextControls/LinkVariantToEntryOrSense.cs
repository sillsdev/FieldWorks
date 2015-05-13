// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.Utils;
using XCore;
using SIL.CoreImpl;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class LinkVariantToEntryOrSense : LinkEntryOrSenseDlg
	{
		PopupTreeManager m_tcManager;
		/// <summary>
		/// when calling the dialog from "Variant Of" context...without an existing variant used as
		/// m_startingEntry, we need to pass in the lexeme form of the variant to create the variant
		/// in order to link it to an entry. Mutually exclusive with m_startingEntry.
		/// </summary>
		ITsString m_tssVariantLexemeForm;
		/// <summary>
		/// TODO: refactor all related logic to InsertVariantDlg or GoDlg.
		/// when calling the dialog from an "Insert Variant" context this
		/// flag is used to indicate that m_startingEntry is a componentLexeme
		/// rather than the variant
		/// </summary>
		bool m_fBackRefToVariant;

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
		/// Sets the DLG info.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="propertyTable"></param>
		/// <param name="tssVariantLexemeForm">The variant lexeme form.</param>
		public void SetDlgInfo(FdoCache cache, Mediator mediator, PropertyTable propertyTable, ITsString tssVariantLexemeForm)
		{
			m_tssVariantLexemeForm = tssVariantLexemeForm;
			base.SetDlgInfo(cache, mediator, propertyTable, null);
		}


		/// <summary>
		/// when calling the dialog from an "Insert Variant" context this
		/// constructor is used to indicate that m_startingEntry is a componentLexeme
		/// rather than the variant
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="componentLexeme">the entry we wish to find or create a variant for.</param>
		protected void SetDlgInfoForComponentLexeme(FdoCache cache, Mediator mediator, PropertyTable propertyTable, IVariantComponentLexeme componentLexeme)
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
			SetDlgInfo(cache, mediator, propertyTable, startingEntry);
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
			Text = LexTextControls.ksFindVariant;
			m_btnInsert.Text = LexTextControls.ks_Create;
			// We disable the "Create" button when we don't have text in the Find textbox.
			UpdateButtonCreateNew();
		}


		protected override void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator, PropertyTable propertyTable, int ws)
		{
			WritingSystemAndStylesheetHelper.SetupWritingSystemAndStylesheetInfo(propertyTable, tcVariantTypes,
				cache, cache.DefaultUserWs);
			base.SetDlgInfo(cache, wp, mediator, propertyTable, ws);
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
					m_mediator, m_propertyTable, m_cache.LangProject.LexDbOA.VariantEntryTypesOA, m_cache.DefaultUserWs,
					false, this);
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

		ILexEntryRef m_variantEntryRefResult;
		/// <summary>
		/// the variant LexEntryRef, that was created or found by the state of the dialog
		/// after user clicks the OK button.
		/// </summary>
		public ILexEntryRef VariantEntryRefResult
		{
			get { return m_variantEntryRefResult; }
		}

		bool m_fNewlyCreatedVariantEntryRef;
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
			if (DialogResult != DialogResult.Cancel && !e.Cancel && m_variantEntryRefResult == null)
				btnOK_Click(null, null);
			base.OnClosing(e);
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			if (SelectedObject == null)
				return; // odd. nothing more to do.

			ILexEntry variant;
			IVariantComponentLexeme componentLexeme;
			ILexEntryType selectedEntryType;
			GetVariantAndComponentAndSelectedEntryType(out variant, out componentLexeme, out selectedEntryType);

			ILexEntryRef matchingEntryRef = FindMatchingEntryRef(variant, componentLexeme, selectedEntryType);
			try
			{
				UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoAddVariant, LexTextControls.ksRedoAddVariant,
					m_cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					if (matchingEntryRef != null)
					{
						// we found a matching ComponentLexeme. See if we can find the selected type.
						// if the selected type does not yet exist, add it.
						if (selectedEntryType != null &&
							!matchingEntryRef.VariantEntryTypesRS.Contains(selectedEntryType))
						{
							matchingEntryRef.VariantEntryTypesRS.Add(selectedEntryType);
						}

						m_variantEntryRefResult = matchingEntryRef;
						m_fNewlyCreatedVariantEntryRef = false;
					}
					else
					{
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
				});
			}
			catch (ArgumentException)
			{
				MessageBoxes.ReportLexEntryCircularReference((ILexEntry)componentLexeme, variant, false);
			}
		}

		/// <summary>
		/// extracts the variant and component from the dialog, depending upon whether we're
		/// called from an "Insert Variant" or "Variant Of..." context.
		/// </summary>
		/// <param name="variant">The variant.</param>
		/// <param name="componentLexeme">The component lexeme.</param>
		/// <param name="selectedEntryType">Type of the selected entry.</param>
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
			selectedEntryType = m_fGetVariantEntryTypeFromTreeCombo
				? m_cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(SelectedVariantEntryTypeHvo) : null;
		}

		private ILexEntryRef FindMatchingEntryRef(ILexEntry variant, IVariantComponentLexeme componentLexeme, ILexEntryType selectedEntryType)
		{
			ILexEntryRef matchingEntryRef;
			if (variant != null)
			{
				// see if the starting entry has the SelectedID already as a ComponentLexeme
				matchingEntryRef = variant.FindMatchingVariantEntryRef(componentLexeme,
					selectedEntryType);
			}
			else
			{
				// determine whether the selected entry or sense is already
				// linked to an existing variant with the given lexeme form.
				matchingEntryRef = componentLexeme.FindMatchingVariantEntryBackRef(selectedEntryType, m_tssVariantLexemeForm);
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
			m_btnOK.Text = LexTextControls.ksAddVariant;
			if (SelectedObject != null)
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
					m_btnOK.Text = m_fBackRefToVariant ? LexTextControls.ks_OK : LexTextControls.ksUseEntry;
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
				m_btnInsert.Enabled = (tssNewVariantLexemeForm != null);
			}
		}

		protected override void m_btnInsert_Click(object sender, EventArgs e)
		{
			if (!m_fBackRefToVariant)
				base.m_btnInsert_Click(sender, e);

			// the user wants to try to create a variant with a lexeme form
			// built from the current state of our Find text box and WritingSystem combo.
			ITsString tssNewVariantLexemeForm = CreateVariantTss();
			if (tssNewVariantLexemeForm == null)
				return;

			// we need to create the new LexEntryRef and its variant from the starting entry.
			UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoCreateVarEntry, LexTextControls.ksRedoCreateVarEntry, m_startingEntry, () =>
			{
				m_variantEntryRefResult = m_startingEntry.CreateVariantEntryAndBackRef(null, tssNewVariantLexemeForm);
			});
			m_fNewlyCreatedVariantEntryRef = true;
			m_selObject = m_variantEntryRefResult.Owner as ILexEntry;
			m_fNewlyCreated = true;
			DialogResult = DialogResult.OK;
			Close();
		}

		public override ICmObject SelectedObject
		{
			get
			{
				if (m_fBackRefToVariant && m_fNewlyCreated && m_variantEntryRefResult != null)
				{
					// we inserted a new variant and linked it to m_startingEntry,
					// return the owner of the variant ref to get the new variant.
					return m_variantEntryRefResult.Owner;
				}

				return base.SelectedObject;
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
				var ws = (IWritingSystem) m_cbWritingSystems.SelectedItem;
				if (m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Contains(ws))
					tssNewVariantLexemeForm = TsStringUtils.MakeTss(trimmed, ws.Handle);
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
			SuspendLayout();
			// first reapply some BaseGoDlg settings
			var resources = new ComponentResourceManager(typeof(BaseGoDlg));
			ApplySomeResources(resources);
			ResumeLayout(false);
			PerformLayout();
		}

	   private void ApplySomeResources(ComponentResourceManager resources)
		{
			//
			// m_btnClose
			//
			resources.ApplyResources(m_btnClose, "m_btnClose");
			//
			// m_btnOK
			//
			resources.ApplyResources(m_btnOK, "m_btnOK");
			//
			// m_btnInsert
			//
			resources.ApplyResources(m_btnInsert, "m_btnInsert");
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			//
			// m_matchingObjectsBrowser
			//
			resources.ApplyResources(m_matchingObjectsBrowser, "m_matchingObjectsBrowser");
			////
			//// GoDlg
			////
			resources.ApplyResources(this, "$this");

			if (MiscUtils.IsUnix)
			{
				// Mono doesn't handle anchoring coming in through these resources for adjusting
				// initial locations and sizes, so let's set those manually.  See FWNX-546.
				var bounds = this.ClientSize;
				var deltaX = bounds.Width - (m_matchingObjectsBrowser.Location.X + m_matchingObjectsBrowser.Width + 12);
				FixButtonLocation(m_btnClose, bounds, deltaX);
				FixButtonLocation(m_btnOK, bounds, deltaX);
				FixButtonLocation(m_btnInsert, bounds, deltaX);
				FixButtonLocation(m_btnHelp, bounds, deltaX);
				if (deltaX > 0)
					m_matchingObjectsBrowser.Width = m_matchingObjectsBrowser.Width + deltaX;
				var desiredBottom = Math.Min(m_btnClose.Location.Y, m_btnOK.Location.Y);
				desiredBottom = Math.Min(desiredBottom, m_btnInsert.Location.Y);
				desiredBottom = Math.Min(desiredBottom, m_btnHelp.Location.Y);
				desiredBottom -= 30;
				var deltaY = desiredBottom - (m_matchingObjectsBrowser.Location.Y + m_matchingObjectsBrowser.Height);
				if (deltaY > 0)
					m_matchingObjectsBrowser.Height = m_matchingObjectsBrowser.Height + deltaY;
			}
		}

		private static void FixButtonLocation(Button button, Size bounds, int deltaX)
		{
			var xloc = button.Location.X;
			if (deltaX > 0)
				xloc += deltaX;
			var yloc = button.Location.Y;
			var desiredY = bounds.Height - (button.Height + 12);
			var deltaY = desiredY - button.Location.Y;
			if (deltaY > 0)
				yloc = desiredY;
			if (xloc != button.Location.X || yloc != button.Location.Y)
				button.Location = new Point(xloc, yloc);;
		}

		/// <summary>
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="componentLexeme">the entry we wish to find or create a variant for.</param>
		public void SetDlgInfo(FdoCache cache, Mediator mediator, PropertyTable propertyTable, IVariantComponentLexeme componentLexeme)
		{
			SetDlgInfoForComponentLexeme(cache, mediator, propertyTable, componentLexeme);
		}
	}
}
