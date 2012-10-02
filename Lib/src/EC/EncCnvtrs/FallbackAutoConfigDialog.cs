using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters31
{
	public partial class FallbackAutoConfigDialog : SilEncConverters31.MetaCmpdAutoConfigDialog
	{
		enum PrimaryFallbackEnum
		{
			ePrimary = 0,
			eFallback = 1
		};

		protected string m_strPrimaryConverter = null;
		protected string m_strFallbackConverter = null;
		private bool m_bIgnoreSelItemChgWhileLoading = true;

		public FallbackAutoConfigDialog
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
			if (aECs.Count == 0)
			{
				MessageBox.Show("Unable to find any existing converters! You must add some before attempting to chain them together.");
				return;
			}

			InitializeComponent();

			base.Initialize
			(
			aECs,
			FallbackEncConverter.strHtmlFilename,
			strDisplayName,
			strFriendlyName,
			strConverterIdentifier,
			eConversionType,
			strLhsEncodingId,
			strRhsEncodingId,
			lProcessTypeFlags,
			bIsInRepository
			);

			// if we're editing ...
			if (m_bEditMode)
			{
				System.Diagnostics.Debug.Assert(m_aEC != null);
				System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(ConverterIdentifier));

				labelCompoundConverterName.Text = FriendlyName;

				if (m_aEC != null)
				{
					QueryStepData();    // get the data about the steps

					if (    ((m_astrStepFriendlyNames != null) && (m_astrStepFriendlyNames.Length == 2))
						&&  ((m_abDirectionForwards != null) && (m_abDirectionForwards.Length == 2)))
					{
						m_strPrimaryConverter = m_astrStepFriendlyNames[(int)PrimaryFallbackEnum.ePrimary];
						m_strFallbackConverter = m_astrStepFriendlyNames[(int)PrimaryFallbackEnum.eFallback];
						checkBoxReversePrimary.Checked = !m_abDirectionForwards[(int)PrimaryFallbackEnum.ePrimary];
						checkBoxReverseFallback.Checked = !m_abDirectionForwards[(int)PrimaryFallbackEnum.eFallback];
					}
				}

				IsModified = false;
			}

			// in any case, populate the combo boxes with the full list of available converters
			foreach (IEncConverter aEC in aECs.Values)
			{
				// don't let the primary-fallback converter be put into the lists
				if (strFriendlyName != aEC.Name)
				{
					comboBoxPrimary.Items.Add(aEC.Name);
					comboBoxFallback.Items.Add(aEC.Name);

					// if this is either the primary or the fallback, then also get the bidi status
					if (m_strPrimaryConverter == aEC.Name)
					{
						comboBoxPrimary.SelectedItem = m_strPrimaryConverter;   // select it in the combo box

						if (EncConverters.IsUnidirectional(aEC.ConversionType))
							checkBoxReversePrimary.Enabled = false;
					}
					else if (m_strFallbackConverter == aEC.Name)
					{
						comboBoxFallback.SelectedItem = m_strFallbackConverter; // select it in the combo box

						if (EncConverters.IsUnidirectional(aEC.ConversionType))
							checkBoxReverseFallback.Enabled = false;
					}
				}
			}

			// in case we set teh selected item in that last code, we don't want to treat that as the steady state situation
			//  (i.e. ignore the resultant SelectedItemChange event until after we're finished.
			m_bIgnoreSelItemChgWhileLoading = false;
		}

		public FallbackAutoConfigDialog
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
			// for Primary-Fallback, just make sure that one of the converters has been selected for the
			//  primary and fallback.
			m_strPrimaryConverter = comboBoxPrimary.Text;
			m_strFallbackConverter = comboBoxFallback.Text;

			if (String.IsNullOrEmpty(m_strPrimaryConverter) || String.IsNullOrEmpty(m_strFallbackConverter))
				return false;

			// if we're actually on the setup tab, then give the exact error.
			if (tabControl.SelectedTab == tabPageSetup)
			{
				// only do these message boxes if we're on the Setup tab itself, because if this OnApply
				//  is being called as a result of the user switching to the Test tab, that code will
				//  already put up an error message and we don't need two error messages.
				if (String.IsNullOrEmpty(m_strPrimaryConverter))
				{
					MessageBox.Show(this, "You must choose a primary converter!", EncConverters.cstrCaption);
					return false;
				}
				else if (String.IsNullOrEmpty(m_strFallbackConverter))
				{
					MessageBox.Show(this, "You must choose a fallback converter!", EncConverters.cstrCaption);
					return false;
				}
			}

			try
			{
				return base.OnApply();
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, String.Format("Failed to add fallback converter! {0}{0}{1}", Environment.NewLine, ex.Message), EncConverters.cstrCaption);
			}

			return false;
		}

		protected override string ProgID
		{
			get { return typeof(FallbackEncConverter).FullName; }
		}

		protected override string ImplType
		{
			get { return EncConverters.strTypeSILfallback; }
		}

		protected override string DefaultFriendlyName
		{
			// as the default, make it the same as the table name (w/o extension)
			get
			{
				string strFriendlyName = null;
				if (!String.IsNullOrEmpty(m_strOriginalFriendlyName))
					strFriendlyName = m_strOriginalFriendlyName;

				// otherwise, make it the primary and fallback step names together with ', with fallback to, ' in the middle
				else if (!String.IsNullOrEmpty(m_strPrimaryConverter) && !String.IsNullOrEmpty(m_strFallbackConverter))
					strFriendlyName = String.Format("{0}, with fallback to, {1}", m_strPrimaryConverter, m_strFallbackConverter);

				return strFriendlyName;
			}
		}

		protected override void AddConverterMappingSub()
		{
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(m_strPrimaryConverter) && !String.IsNullOrEmpty(m_strFallbackConverter));

			// 'forward' gets passed as 'true' only if the checkbox is not enabled (which implies unidirectional; so only forward makes sense) or
			//  if the checkbox is not checked (which means reverse)
			m_aECs.AddFallbackConverter
					(
						FriendlyName,
						m_strPrimaryConverter,
						!checkBoxReversePrimary.Enabled || !checkBoxReversePrimary.Checked,
						m_strFallbackConverter,
						!checkBoxReverseFallback.Enabled || !checkBoxReverseFallback.Checked
					);
			m_aEC = null;   // so it get's requeried (since, for example, the ConvType might have changed if we changed the direction)
		}

		private void comboBoxPrimary_SelectedIndexChanged(object sender, EventArgs e)
		{
			comboBox_SelectedIndexChanged((ComboBox)sender, checkBoxReversePrimary);
		}

		private void comboBoxFallback_SelectedIndexChanged(object sender, EventArgs e)
		{
			comboBox_SelectedIndexChanged((ComboBox)sender, checkBoxReverseFallback);
		}

		private void comboBox_SelectedIndexChanged(ComboBox sender, CheckBox chk)
		{
			if (!m_bIgnoreSelItemChgWhileLoading)
			{
				string strConverter = sender.Text;
				IEncConverter aEC = m_aECs[strConverter];
				if (aEC != null)
				{
					chk.Enabled = !EncConverters.IsUnidirectional(aEC.ConversionType);
				}

				// to enable the name to be changed
				IsModified = true;
			}
		}

		protected override void UpdateCompoundConverterNameLabel(string strFriendlyName)
		{
			labelCompoundConverterName.Text = strFriendlyName;
		}

		private void checkBoxReverse_CheckedChanged(object sender, EventArgs e)
		{
			IsModified = true;
		}
	}
}
