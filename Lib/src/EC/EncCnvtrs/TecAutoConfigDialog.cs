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
	public partial class TecAutoConfigDialog : SilEncConverters31.AutoConfigDialog
	{
		protected bool m_bInitialized = false;

		public TecAutoConfigDialog
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
			TecEncConverter.strHtmlFilename,
			strDisplayName,
			strFriendlyName,
			strConverterIdentifier,
			eConversionType,
			strLhsEncodingId,
			strRhsEncodingId,
			lProcessTypeFlags,
			bIsInRepository
			);

			// if we're editing a TECkit map, then set the Converter Spec and say it's unmodified
			if (m_bEditMode)
			{
				System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(ConverterIdentifier));
				textBoxFileSpec.Text = ConverterIdentifier;
				IsModified = false;
			}

			m_bInitialized = true;

			helpProvider.SetHelpString(textBoxFileSpec, Properties.Resources.ConverterFileSpecHelpString);
		}

		public TecAutoConfigDialog
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
			// for TECkit, get the converter identifier from the Setup tab controls.
			ConverterIdentifier = textBoxFileSpec.Text;

			// if we're actually on the setup tab, then do some further checking as well.
			if (tabControl.SelectedTab == tabPageSetup)
			{
				// only do these message boxes if we're on the Setup tab itself, because if this OnApply
				//  is being called as a result of the user switching to the Test tab, that code will
				//  already put up an error message and we don't need two error messages.
				if (String.IsNullOrEmpty(ConverterIdentifier))
				{
					MessageBox.Show(this, "Choose a TECkit map first!", EncConverters.cstrCaption);
					return false;
				}
				else if (!File.Exists(ConverterIdentifier))
				{
					MessageBox.Show(this, "TECkit map doesn't exist!", EncConverters.cstrCaption);
					return false;
				}
			}

			return base.OnApply();
		}

		protected override string ProgID
		{
			get
			{
				if (ConverterIdentifier == TecEncConverter.strEFCReq)
					return typeof(TecFormEncConverter).FullName;
				else
					return typeof(TecEncConverter).FullName;
			}
		}

		protected override string ImplType
		{
			get
			{
				if (ConverterIdentifier == TecEncConverter.strEFCReq)
					return EncConverters.strTypeSILtecForm;
				else if (ConverterIdentifier.Substring(ConverterIdentifier.Length - 4).ToLower() == ".map")
					return EncConverters.strTypeSILmap;
				else
					return EncConverters.strTypeSILtec;
			}
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
			if (m_bInitialized) // but only do this after we've already initialized (we might have set it during m_bEditMode)
				IsModified = (((TextBox)sender).Text.Length > 0);
		}
	}
}
