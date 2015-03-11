// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MappingDetailsCtrl.cs
// Responsibility: TomB
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for MappingDetailsCtrl.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class MappingDetailsCtrl : UserControl, IFWDisposable
	{
		#region Custom Events
		/// <summary>Handles ValidStateChanged events.</summary>
		public delegate void ValidStateChangedHandler(object sender, bool valid);

		/// <summary>Event which occurs whenever the valid state changes.</summary>
		public event ValidStateChangedHandler ValidStateChanged;
		#endregion

		#region Data members
		private ImportMappingInfo m_mapping;
		private FdoCache m_cache;
		private IScripture m_scr;
		private bool m_fParatextMapping;
		private bool m_isAnnotationMapping;
		private bool m_fBackTransDomainLocked;

		/// <summary></summary>
		protected FwStyleSheet m_StyleSheet;
		/// <summary></summary>
		public StyleListBoxHelper m_styleListHelper;

		private FwOverrideComboBox cboList;
		/// <summary></summary>
		public CaseSensitiveListBox lbStyles;
		/// <summary></summary>
		public System.Windows.Forms.CheckBox chkExclude;
		/// <summary></summary>
		public FwOverrideComboBox cboWritingSys;
		/// <summary></summary>
		public System.Windows.Forms.CheckBox chkBackTranslation;
		/// <summary></summary>
		public System.Windows.Forms.RadioButton rbtnScripture;
		/// <summary></summary>
		public System.Windows.Forms.RadioButton rbtnFootnotes;
		/// <summary></summary>
		public System.Windows.Forms.RadioButton rbtnNotes;
		private GroupBox domainBox;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		static string[] s_allPseudoStyles = new string[] {
					ScrImportComponents.kstidChapterLabelStyle,
					ScrImportComponents.kstidTitleShortStyle,
					ScrImportComponents.kstidFigureStyle,
					ScrImportComponents.kstidFigureCaptionStyle,
					ScrImportComponents.kstidFigureCopyrightStyle,
					ScrImportComponents.kstidFigureDescriptionStyle,
					ScrImportComponents.kstidFigureFilenameStyle,
					ScrImportComponents.kstidFigureLayoutPositionStyle,
					ScrImportComponents.kstidFigureRefRangeStyle,
					ScrImportComponents.kstidFigureScaleStyle
				};

		static string[] s_btPseudoStyles = new string[] {
					ScrImportComponents.kstidChapterLabelStyle,
					ScrImportComponents.kstidTitleShortStyle,
					ScrImportComponents.kstidFigureStyle,
					ScrImportComponents.kstidFigureCaptionStyle,
					ScrImportComponents.kstidFigureCopyrightStyle,
				};

		static Dictionary<string, MappingTargetType> s_PsuedoStyleNamesToTargetType =
			new Dictionary<string, MappingTargetType>();
		#endregion

		#region Construction/initialization/destruction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="MappingDetailsCtrl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MappingDetailsCtrl()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			m_styleListHelper = new StyleListBoxHelper(lbStyles);
		}

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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_styleListHelper != null)
					m_styleListHelper.Dispose();
			}
			m_styleListHelper = null;
			m_StyleSheet = null;
			m_mapping = null;
			m_cache = null;
			m_scr = null;

			base.Dispose( disposing );
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize this dialog
		/// </summary>
		/// <param name="fParatextMapping"><c>true</c> if a Paratext mapping is being modified;
		/// <c>false</c> otherwise</param>
		/// <param name="mapping">Mapping object being modified</param>
		/// <param name="styleSheet">Stylesheet containing styles that will appear in the list
		/// </param>
		/// <param name="cache">The cache representing the DB connection</param>
		/// <param name="fBackTransDomainLocked">If <c>true</c>, won't allow the user to
		/// check or clear the BT checkbox. If the incoming mapping is for the back translation
		/// and has a domain of either Scripture or Footnote, these two domains remain
		/// enabled so the user can switch between them, but the Notes domain will be
		/// disabled. If the incoming mapping is not for the back translation, then
		/// this only affects the BT checkbox, not the domain options.
		/// </param>
		/// <param name="isAnnotationMapping">If <c>true</c>, forces this mapping to be in the
		/// Annotation domain.</param>
		/// <remarks>We separated this from the constructor so that we can create a mock object
		/// for testing purposes.</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void Initialize(bool fParatextMapping, ImportMappingInfo mapping,
			FwStyleSheet styleSheet, FdoCache cache, bool isAnnotationMapping,
			bool fBackTransDomainLocked)
		{
			CheckDisposed();

			m_fParatextMapping = fParatextMapping;
			m_cache = cache;
			m_scr = cache.LangProject.TranslatedScriptureOA;
			m_mapping = mapping;
			m_StyleSheet = styleSheet;
			m_isAnnotationMapping = isAnnotationMapping;
			m_fBackTransDomainLocked = fBackTransDomainLocked;
			m_styleListHelper.StyleSheet = styleSheet as FwStyleSheet;

			//			// if there are items in the styles list and there is not one selected then
			//			// set the first item to be the selected item.
			//			if (lbStyles.SelectedIndex == -1 && lbStyles.Items.Count > 0)
			//				lbStyles.SelectedIndex = 0;

			// Fill in the list selector combo box with the filters for the style list
			// Also set the maximum style level for the style list helper before the
			// style list gets filled in.
			cboList.Items.Clear();
			cboList.Items.Add(TeResourceHelper.GetResourceString("kstidStyleFilterBasic"));
			cboList.Items.Add(TeResourceHelper.GetResourceString("kstidStyleFilterAllStyles"));
			cboList.Items.Add(TeResourceHelper.GetResourceString("kstidStyleFilterCustomList"));

			cboList.SelectedIndex = 1;
			// This code was not completely removed for the highly likely case that
			// it gets put back :)
//			switch (Options.ShowTheseStylesSetting)
//			{
//				case Options.ShowTheseStyles.Basic:
//					cboList.SelectedIndex = 0;
//					m_styleListHelper.MaxStyleLevel = 0;
//					break;
//				case Options.ShowTheseStyles.All:
//					cboList.SelectedIndex = 1;
//					m_styleListHelper.MaxStyleLevel = int.MaxValue;
//					break;
//				case Options.ShowTheseStyles.Custom:
//					cboList.SelectedIndex = 2;
//					m_styleListHelper.MaxStyleLevel = ToolsOptionsDialog.MaxStyleLevel;
//					break;
//			}

			chkBackTranslation.Checked = (mapping.Domain & MarkerDomain.BackTrans) != 0;

			// Check the appropriate button for the domain.  This will cause the
			// style list to be loaded correctly for the domain.
			switch (mapping.Domain & ~MarkerDomain.BackTrans)
			{
				case MarkerDomain.Footnote:
					rbtnFootnotes.Checked = true;
					break;
				case MarkerDomain.Note:
					rbtnNotes.Checked = true;
					break;
				default:
					rbtnScripture.Checked = true;
					break;
			}

			// select the style name and add the handler for style changes.  This needs
			// to be done after setting the domain since that causes the style list to
			// be loaded the first time.
			m_styleListHelper.SelectedStyleName = MappingToUiStylename(mapping);
			m_styleListHelper.StyleChosen += new StyleChosenHandler(StyleChosen);

			// if the selected mapping is excluded, then check the box
			if (chkExclude.Checked != mapping.IsExcluded)
				chkExclude.Checked = mapping.IsExcluded;
			else
				SetControlStatesBasedOnExcludeCheckBox(null, null);

			cboWritingSys.Items.Clear();
			// Create a fake WS for the "Based on Context" item
			cboWritingSys.Items.Add(TeResourceHelper.GetResourceString("kstidBasedOnContext"));
			string initialWritingSystem = string.Empty;
			// Iterate through the available writing systems and add them to the writing systems
			// combo box.
			foreach (CoreWritingSystemDefinition wsObj in cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				cboWritingSys.Items.Add(wsObj);

				// If the mapping's ICULocale matches the current writing system's ICULocale,
				// save the string just added to the combo box so we can initialize the
				// combo box's value with it.
				if (mapping.WsId == wsObj.Id)
					initialWritingSystem = wsObj.ToString();
			}

			// Initialize the combo's value.
			cboWritingSys.SelectedIndex = (initialWritingSystem == string.Empty ?
				0 : cboWritingSys.FindString(initialWritingSystem));
		}
		#endregion

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MappingDetailsCtrl));
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label5;
			System.Windows.Forms.Label label2;
			this.cboList = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.lbStyles = new CaseSensitiveListBox();
			this.chkExclude = new System.Windows.Forms.CheckBox();
			this.cboWritingSys = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.domainBox = new System.Windows.Forms.GroupBox();
			this.rbtnFootnotes = new System.Windows.Forms.RadioButton();
			this.rbtnScripture = new System.Windows.Forms.RadioButton();
			this.rbtnNotes = new System.Windows.Forms.RadioButton();
			this.chkBackTranslation = new System.Windows.Forms.CheckBox();
			label3 = new System.Windows.Forms.Label();
			label5 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			this.domainBox.SuspendLayout();
			this.SuspendLayout();
			//
			// cboList
			//
			resources.ApplyResources(this.cboList, "cboList");
			this.cboList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboList.Name = "cboList";
			this.cboList.SelectedIndexChanged += new System.EventHandler(this.cboList_SelectedIndexChanged);
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// label5
			//
			resources.ApplyResources(label5, "label5");
			label5.Name = "label5";
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// lbStyles
			//
			resources.ApplyResources(this.lbStyles, "lbStyles");
			this.lbStyles.Name = "lbStyles";
			//
			// chkExclude
			//
			resources.ApplyResources(this.chkExclude, "chkExclude");
			this.chkExclude.Name = "chkExclude";
			this.chkExclude.CheckedChanged += new System.EventHandler(this.SetControlStatesBasedOnExcludeCheckBox);
			//
			// cboWritingSys
			//
			this.cboWritingSys.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.cboWritingSys, "cboWritingSys");
			this.cboWritingSys.Name = "cboWritingSys";
			this.cboWritingSys.Sorted = true;
			//
			// domainBox
			//
			this.domainBox.Controls.Add(this.rbtnFootnotes);
			this.domainBox.Controls.Add(this.rbtnScripture);
			this.domainBox.Controls.Add(this.rbtnNotes);
			this.domainBox.Controls.Add(this.chkBackTranslation);
			resources.ApplyResources(this.domainBox, "domainBox");
			this.domainBox.Name = "domainBox";
			this.domainBox.TabStop = false;
			//
			// rbtnFootnotes
			//
			resources.ApplyResources(this.rbtnFootnotes, "rbtnFootnotes");
			this.rbtnFootnotes.Name = "rbtnFootnotes";
			this.rbtnFootnotes.CheckedChanged += new System.EventHandler(this.DomainChanged);
			//
			// rbtnScripture
			//
			resources.ApplyResources(this.rbtnScripture, "rbtnScripture");
			this.rbtnScripture.Name = "rbtnScripture";
			this.rbtnScripture.CheckedChanged += new System.EventHandler(this.DomainChanged);
			//
			// rbtnNotes
			//
			resources.ApplyResources(this.rbtnNotes, "rbtnNotes");
			this.rbtnNotes.Name = "rbtnNotes";
			this.rbtnNotes.CheckedChanged += new System.EventHandler(this.DomainChanged);
			//
			// chkBackTranslation
			//
			resources.ApplyResources(this.chkBackTranslation, "chkBackTranslation");
			this.chkBackTranslation.Name = "chkBackTranslation";
			this.chkBackTranslation.CheckedChanged += new System.EventHandler(this.DomainChanged);
			//
			// MappingDetailsCtrl
			//
			this.Controls.Add(this.cboList);
			this.Controls.Add(label2);
			this.Controls.Add(this.lbStyles);
			this.Controls.Add(this.cboWritingSys);
			this.Controls.Add(this.domainBox);
			this.Controls.Add(this.chkExclude);
			this.Controls.Add(label3);
			this.Controls.Add(label5);
			this.Name = "MappingDetailsCtrl";
			resources.ApplyResources(this, "$this");
			this.domainBox.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the selected writing system's ICU Locale.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string WritingSystem
		{
			get
			{
				CheckDisposed();
				var ws = cboWritingSys.SelectedItem as CoreWritingSystemDefinition;
				return ws == null ? null : ws.Id;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether OK button should be enabled
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool Valid
		{
			get
			{
				return (m_styleListHelper.SelectedStyleName != null &&
					m_styleListHelper.SelectedStyleName != string.Empty) || chkExclude.Checked;
			}
		}
		#endregion

		#region public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save mapping details
		/// </summary>
		/// <remarks>Caller is responsible for saving the mapping somewhere useful. This
		/// just updates the mapping info.</remarks>
		/// ------------------------------------------------------------------------------------
		public void Save()
		{
			CheckDisposed();

			m_mapping.IsExcluded = chkExclude.Checked;
			if (!chkExclude.Checked)
			{
				m_mapping.Domain = MarkerDomain.Default;

				UpdateMapping(m_mapping, m_styleListHelper.SelectedStyleName);

				StyleListItem styleItem = (StyleListItem)lbStyles.SelectedItem;
				if (styleItem.Type != StyleType.kstCharacter ||
					m_mapping.StyleName == StyleUtils.DefaultParaCharsStyleName)
				{
					if (rbtnFootnotes.Checked)
						m_mapping.Domain = MarkerDomain.Footnote;
					else if (rbtnNotes.Checked)
						m_mapping.Domain = MarkerDomain.Note;
					else
						m_mapping.Domain = MarkerDomain.Default;

					if (chkBackTranslation.Checked)
						m_mapping.Domain |= MarkerDomain.BackTrans;
				}
				else
				{
					ContextValues context =
						(ContextValues)m_StyleSheet.GetContext(m_mapping.StyleName);
					FunctionValues function = m_StyleSheet.GetFunction(m_mapping.StyleName);
					if (context == ContextValues.BackTranslation ||
						(context == ContextValues.Title && chkBackTranslation.Checked))
					{
						m_mapping.Domain = MarkerDomain.BackTrans;
					}
					else if (context == ContextValues.Note || function == FunctionValues.Footnote)
					{
						m_mapping.Domain = MarkerDomain.Footnote;
					}
				}

				m_mapping.WsId = WritingSystem;
			}
		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enable/disable controls depending on whether this is an excluded mapping.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void SetControlStatesBasedOnExcludeCheckBox(object sender, System.EventArgs e)
		{
			lbStyles.Enabled = !chkExclude.Checked;
			cboWritingSys.Enabled = !chkExclude.Checked;
			DetermineDomainControlsState();
			if (ValidStateChanged != null)
				ValidStateChanged(this, Valid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enable/disable "Domain" controls depending on whether this is an excluded mapping
		/// and whether certain domains are "locked" (which is typically the case for P6).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DetermineDomainControlsState()
		{
			if (m_isAnnotationMapping)
			{
				domainBox.Enabled = false;
				rbtnNotes.Checked = true;
			}
			else
			{
				domainBox.Enabled = !chkExclude.Checked;
				chkBackTranslation.Enabled = (!rbtnNotes.Checked && !m_fBackTransDomainLocked);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the style changes, raise the ValidStateChanged event
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StyleChosen(StyleListItem prevStyle, StyleListItem newStyle)
		{
			if (ValidStateChanged != null)
				ValidStateChanged(this, Valid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This event handles a change for any of the radio buttons that specify the domain
		/// of the marker (scripture, footnotes, or consultant notes).  When a change
		/// is made, refill the styles list based on what is valid for the domain.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void DomainChanged(object sender, System.EventArgs e)
		{
			RadioButton rbtn = sender as RadioButton;
			chkBackTranslation.Enabled = !rbtnNotes.Checked && !m_fBackTransDomainLocked;
			if (!chkBackTranslation.Enabled)
				chkBackTranslation.Checked = false;

			if (rbtn == null || rbtn.Checked)
			{
				using (new WaitCursor(this))
				{
					// Decide on what to include or exclude.  We will either allow everything
					// except annotation and internal styles, or only allow annotation and
					// general styles.
					List<ContextValues> contextList = new List<ContextValues>();
					string[] pseudoStyles = null;
					m_styleListHelper.ExcludeStylesWithFunction.Clear();
					if (rbtnScripture.Checked)
					{
						contextList.Add(ContextValues.Annotation);
						contextList.Add(ContextValues.Internal);
						contextList.Add(ContextValues.Note);
						if (chkBackTranslation.Checked)
							pseudoStyles = BtPseudoStyles;
						else
						{
							contextList.Add(ContextValues.BackTranslation);
							m_styleListHelper.ExcludeStylesWithFunction.Add(FunctionValues.Verse);
							pseudoStyles = AllPseudoStyles;
						}
						m_styleListHelper.ExcludeStylesWithContext = contextList;
						m_styleListHelper.ExcludeStylesWithFunction.Add(FunctionValues.Footnote);
						m_styleListHelper.ExcludeStylesWithFunction.Add(FunctionValues.Chapter);
					}
					else
					{
						contextList.Add(ContextValues.General);
						if (rbtnFootnotes.Checked)
						{
							contextList.Add(ContextValues.Note);
							contextList.Add(ContextValues.InternalMappable);
							if (chkBackTranslation.Checked)
								contextList.Add(ContextValues.BackTranslation);
						}
						else
							contextList.Add(ContextValues.Annotation);
						m_styleListHelper.IncludeStylesWithContext = contextList;
					}

					// Build the list of styles for the control
					string styleName = m_styleListHelper.SelectedStyleName;
					m_styleListHelper.AddStyles(m_StyleSheet as FwStyleSheet, pseudoStyles);
					m_styleListHelper.SelectedStyleName = styleName;

					if (ValidStateChanged != null)
					{
						ValidStateChanged(this, m_styleListHelper.SelectedStyleName != null &&
							m_styleListHelper.SelectedStyleName != string.Empty);
					}
				}
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a selection change in the list combo box
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void cboList_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			switch(cboList.SelectedIndex)
			{
				case 0:	// basic
					m_styleListHelper.MaxStyleLevel = 0;
					break;
				case 1:	// all
					m_styleListHelper.MaxStyleLevel = int.MaxValue;
					break;
				case 2:	// custom
					m_styleListHelper.MaxStyleLevel = ToolsOptionsDialog.MaxStyleLevel;
					break;
			}
			m_styleListHelper.Refresh();

			if (ValidStateChanged != null)
				ValidStateChanged(this, Valid);
		}
		#endregion

		#region Static internal methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the mapping depending on the selectedStyleName's text.
		/// </summary>
		/// <param name="mapping">Mapping to update</param>
		/// <param name="selectedStyleName">the style selected in calling function</param>
		/// ------------------------------------------------------------------------------------
		internal static void UpdateMapping(ImportMappingInfo mapping, string selectedStyleName)
		{
			if (s_PsuedoStyleNamesToTargetType.Count == 0)
			{
				// Populate it for first-time use.
				s_PsuedoStyleNamesToTargetType[ScrImportComponents.kstidFigureStyle] = MappingTargetType.Figure;
				s_PsuedoStyleNamesToTargetType[ScrImportComponents.kstidFigureCaptionStyle] = MappingTargetType.FigureCaption;
				s_PsuedoStyleNamesToTargetType[ScrImportComponents.kstidFigureCopyrightStyle] = MappingTargetType.FigureCopyright;
				s_PsuedoStyleNamesToTargetType[ScrImportComponents.kstidFigureDescriptionStyle] = MappingTargetType.FigureDescription;
				s_PsuedoStyleNamesToTargetType[ScrImportComponents.kstidFigureFilenameStyle] = MappingTargetType.FigureFilename;
				s_PsuedoStyleNamesToTargetType[ScrImportComponents.kstidFigureLayoutPositionStyle] = MappingTargetType.FigureLayoutPosition;
				s_PsuedoStyleNamesToTargetType[ScrImportComponents.kstidFigureRefRangeStyle] = MappingTargetType.FigureRefRange;
				s_PsuedoStyleNamesToTargetType[ScrImportComponents.kstidFigureScaleStyle] = MappingTargetType.FigureScale;
				s_PsuedoStyleNamesToTargetType[ScrImportComponents.kstidTitleShortStyle] = MappingTargetType.TitleShort;
				s_PsuedoStyleNamesToTargetType[ScrImportComponents.kstidChapterLabelStyle] = MappingTargetType.ChapterLabel;
			}

			MappingTargetType targetType;
			if (s_PsuedoStyleNamesToTargetType.TryGetValue(selectedStyleName, out targetType))
				mapping.MappingTarget = targetType;
			else
				mapping.MappingTarget = MappingTargetType.TEStyle;

			mapping.StyleName = selectedStyleName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style-name of the mapping, or if this is one of our special internal
		/// pseudo-styles, returns a UI-friendly version.
		/// </summary>
		/// <param name="mapping">The mapping.</param>
		/// ------------------------------------------------------------------------------------
		internal static string MappingToUiStylename(ImportMappingInfo mapping)
		{
			switch (mapping.MappingTarget)
			{
				case MappingTargetType.ChapterLabel:
					return ScrImportComponents.kstidChapterLabelStyle;
				case MappingTargetType.TitleShort:
					return ScrImportComponents.kstidTitleShortStyle;
				case MappingTargetType.Figure:
					return ScrImportComponents.kstidFigureStyle;
				case MappingTargetType.FigureCaption:
					return ScrImportComponents.kstidFigureCaptionStyle;
				case MappingTargetType.FigureCopyright:
					return ScrImportComponents.kstidFigureCopyrightStyle;
				case MappingTargetType.FigureDescription:
					return ScrImportComponents.kstidFigureDescriptionStyle;
				case MappingTargetType.FigureFilename:
					return ScrImportComponents.kstidFigureFilenameStyle;
				case MappingTargetType.FigureLayoutPosition:
					return ScrImportComponents.kstidFigureLayoutPositionStyle;
				case MappingTargetType.FigureRefRange:
					return ScrImportComponents.kstidFigureRefRangeStyle;
				case MappingTargetType.FigureScale:
					return ScrImportComponents.kstidFigureScaleStyle;
				case MappingTargetType.TEStyle:
				case MappingTargetType.DefaultParaChars:
					return mapping.StyleName;
				default:
					Debug.Fail("Unexpected Mapping Target Type");
					return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all the pseudo styles that the user can map markers to, dude.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static string[] AllPseudoStyles
		{
			get { return s_allPseudoStyles; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all the pseudo styles that the user can map markers to in an interleaved BT.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static string[] BtPseudoStyles
		{
			get { return s_btPseudoStyles; }
		}
		#endregion
	}
}
