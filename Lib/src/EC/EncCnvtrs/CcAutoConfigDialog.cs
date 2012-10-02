using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters31
{
	public partial class CcAutoConfigDialog : SilEncConverters31.AutoConfigDialog
	{
		protected bool m_bInitialized = false;  // set at the end of Initialize (to block certain events until we're ready for them)

		public CcAutoConfigDialog
			(
			IEncConverters aECs,
			string strDisplayName,
			string strFriendlyName,
			string strConverterIdentifier,
			ConvType eConversionType,
			string strLhsEncodingId,
			string strRhsEncodingId,
			int lProcessTypeFlags,
			bool bIsInRepository
			)
		{
			InitializeComponent();

			base.Initialize
			(
			aECs,
			CcEncConverter.strHtmlFilename,
			strDisplayName,
			strFriendlyName,
			strConverterIdentifier,
			eConversionType,
			strLhsEncodingId,
			strRhsEncodingId,
			lProcessTypeFlags,
			bIsInRepository
			);

			// if we're editing a CC table/spellfixer project, then set the Converter Spec and say it's unmodified
			if (m_bEditMode)
			{
				System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(ConverterIdentifier));
				textBoxFileSpec.Text = ConverterIdentifier;
				IsModified = false;
			}

			// if we're editing a SpellFixer project, then the converter is managed by the SpellFixer
			//  project dialog, so we don't need to have an "Save in Repository" button.
			UpdateUI(!IsSpellFixerProject);

			m_bInitialized = true;

			helpProvider.SetHelpString(textBoxFileSpec, Properties.Resources.ConverterFileSpecHelpString);
			helpProvider.SetHelpString(buttonBrowse, Properties.Resources.BrowseFileSpecHelpString);
			helpProvider.SetHelpString(groupBoxExpects, Properties.Resources.ConvTypeExpectsHelpString);
			helpProvider.SetHelpString(groupBoxReturns, Properties.Resources.ConvTypeReturnsHelpString);
		}

		protected bool IsSpellFixerProject
		{
			get { return ((ProcessType & (int)ProcessTypeFlags.SpellingFixerProject) != 0); }
		}

		protected void UpdateUI(bool bVisible)
		{
			buttonSaveInRepository.Visible =
				groupBoxExpects.Visible =
				groupBoxReturns.Visible = bVisible;

			// only make the SpellFixer button visible either if it is editing a spellfixer
			//  project or if we're if we're not editing a non-spellfixer project and spellfixer
			//  is actually installed.
			buttonAddSpellFixer.Visible = (!m_bEditMode || IsSpellFixerProject) && SpellFixerByReflection.IsSpellFixerAvailable;
			labelSpellFixerInstructions.Visible = (IsSpellFixerProject && SpellFixerByReflection.IsSpellFixerAvailable);
		}

		protected override void SetConvTypeControls()
		{
			SetRbValuesFromConvType(radioButtonExpectsUnicode, radioButtonExpectsLegacy, radioButtonReturnsUnicode,
				radioButtonReturnsLegacy);
		}

		public CcAutoConfigDialog
			(
			IEncConverters aECs,
			string strFriendlyName,
			string strConverterIdentifier,
			ConvType eConversionType,
			string strTestData
			)
		{
			InitializeComponent();

			base.Initialize
			(
			aECs,
			strFriendlyName,
			strConverterIdentifier,
			eConversionType,
			strTestData
			);
		}

		// this method is called either when the user clicks the "Apply" or "OK" buttons *OR* if she
		//  tries to switch to the Test or Advanced tab. This is the dialog's one opportunity
		//  to make sure that the user has correctly configured a legitimate converter.
		protected override bool OnApply()
		{
			// for CC, get the converter identifier and ConvType from the Setup tab controls.
			ConverterIdentifier = textBoxFileSpec.Text;
			SetConvTypeFromRbControls(radioButtonExpectsUnicode, radioButtonExpectsLegacy,
				radioButtonReturnsUnicode, radioButtonReturnsLegacy);

			// if we're actually on the setup tab, then do some further checking as well.
			if (tabControl.SelectedTab == tabPageSetup)
			{
				// only do these message boxes if we're on the Setup tab itself, because if this OnApply
				//  is being called as a result of the user switching to the Test tab, that code will
				//  already put up an error message and we don't need two error messages.
				if (String.IsNullOrEmpty(ConverterIdentifier))
				{
					MessageBox.Show(this, "Choose a CC table first!", EncConverters.cstrCaption);
					return false;
				}
				else if (!File.Exists(ConverterIdentifier))
				{
					MessageBox.Show(this, "CC table doesn't exist!", EncConverters.cstrCaption);
					return false;
				}
			}

			return base.OnApply();
		}

		protected override bool ShouldRemoveBeforeAdd
		{
			// if this is a spell fixer project, then we've already taken care of everything
			get { return !IsSpellFixerProject; }
		}

		// if this is a spellFixer project, then don't allow the user to edit the Friendly name on
		//  the Advanced tab (or we won't be able to find it)
		protected override bool ShouldFriendlyNameBeReadOnly
		{
			get { return IsSpellFixerProject; }
		}

		protected override bool GetFontMapping(string strFriendlyName, out string strLhsFont, out string strRhsFont)
		{
			bool bRet = base.GetFontMapping(strFriendlyName, out strLhsFont, out strRhsFont);

			// if it's a spellfixer project (and we don't already have a font mapping from the repository),
			//  then use the SpellFixer project's font
			if (!bRet && IsSpellFixerProject)
			{
				System.Diagnostics.Debug.Assert(((EncConverters)m_aECs).ContainsKey(strFriendlyName));
				ECAttributes aECAttrs = m_aECs.Attributes(strFriendlyName, AttributeType.Converter);
				if (aECAttrs != null)
				{
					strLhsFont = strRhsFont = aECAttrs[SpellFixerByReflection.cstrAttributeFontToUse];
					bRet = true;
				}
			}
			return bRet;
		}

		protected override string ProgID
		{
			get { return typeof(CcEncConverter).FullName; }
		}

		protected override string ImplType
		{
			get { return EncConverters.strTypeSILcc; }
		}

		protected override string DefaultFriendlyName
		{
			// as the default, make it the same as the table name (w/o extension)
			get { return Path.GetFileNameWithoutExtension(ConverterIdentifier); }
		}

		private void buttonBrowse_Click(object sender, EventArgs e)
		{
			if (!String.IsNullOrEmpty(ConverterIdentifier))
				openFileDialogBrowse.InitialDirectory = Path.GetDirectoryName(ConverterIdentifier);
			else
				openFileDialogBrowse.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + EncConverters.strDefMapsTablesPath;

			if (openFileDialogBrowse.ShowDialog() == DialogResult.OK)
			{
				ResetFields();
				textBoxFileSpec.Text = openFileDialogBrowse.FileName;
			}
		}

		private void textBoxFileSpec_TextChanged(object sender, EventArgs e)
		{
			if (m_bInitialized) // but only do this after we're already initialized
			{
				IsModified = (((TextBox)sender).Text.Length > 0);
				ProcessType &= ~(int)ProcessTypeFlags.SpellingFixerProject;
				UpdateUI(IsModified);
			}
		}

		protected override bool SetupTabSelected_MakeSaveInRepositoryVisible
		{
			get { return !IsSpellFixerProject; }
		}

		private void buttonAddSpellFixer_Click(object sender, EventArgs e)
		{
			try
			{
				SpellFixerByReflection aSF = new SpellFixerByReflection();
				aSF.LoginProject();
				((EncConverters)m_aECs).Reinitialize();
				FriendlyName = aSF.SpellFixerEncConverterName;
				m_aEC = m_aECs[FriendlyName];
				if (m_aEC != null)
				{
					textBoxFileSpec.Text = ConverterIdentifier = m_aEC.ConverterIdentifier;
					ConversionType = m_aEC.ConversionType;
					ProcessType = m_aEC.ProcessType;
					UpdateUI(false);
					aSF.QueryForSpellingCorrectionIfTableEmpty("incorect");
					aSF.EditSpellingFixes();
					IsInRepository = true;
				}
			}
			catch (Exception)
			{
				// usually just a "no project selected message, so .... ignoring it
				// MessageBox.Show(ex.Message, EncConverters.cstrCaption);
			}
		}

		private void radioButton_CheckedChanged(object sender, EventArgs e)
		{
			IsModified = true;
		}
	}
}
