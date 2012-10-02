using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;

namespace SilEncConverters40
{
	public partial class TechHindiSiteAutoConfigDialog : AutoConfigDialog
	{
		protected bool m_bInitialized = false;  // set at the end of Initialize (to block certain events until we're ready for them)

		public TechHindiSiteAutoConfigDialog
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
			TechHindiSiteEncConverter.CstrHtmlFilename,
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

				string strConverterPageUri, strInputHtmlElementId, strOutputHtmlElementId,
					strConvertFunctionName, strConvertReverseFunctionName;
				TechHindiSiteEncConverter.ParseConverterIdentifier(ConverterIdentifier, out strConverterPageUri,
					out strInputHtmlElementId, out strOutputHtmlElementId, out strConvertFunctionName, out
					strConvertReverseFunctionName);
				textBoxFileSpec.Text = strConverterPageUri;
				textBoxInputId.Text = strInputHtmlElementId;
				textBoxOutputId.Text = strOutputHtmlElementId;
				textBoxConvertFunctionForward.Text = strConvertFunctionName;
				textBoxConvertFunctionReverse.Text = strConvertReverseFunctionName;

				switch (ConversionType)
				{
					case ConvType.Legacy_to_from_Unicode:
					case ConvType.Unicode_to_from_Legacy:
						{
							radioButtonLegacyToUnicode.Checked = true;
							checkBoxBidirectional.Checked = true;
							break;
						};
					case ConvType.Legacy_to_Legacy:
						{
							radioButtonLegacyToLegacy.Checked = true;
							break;
						};
					case ConvType.Legacy_to_from_Legacy:
						{
							radioButtonLegacyToLegacy.Checked = true;
							checkBoxBidirectional.Checked = true;
							break;
						};
					case ConvType.Unicode_to_Unicode:
						{
							radioButtonUnicodeToUnicode.Checked = true;
							break;
						};
					case ConvType.Unicode_to_from_Unicode:
						{
							radioButtonUnicodeToUnicode.Checked = true;
							checkBoxBidirectional.Checked = true;
							break;
						};
					default:
						{
							radioButtonLegacyToUnicode.Checked = true;
							break;
						};
				};
				IsModified = false;
			}

			UpdateUI();

			m_bInitialized = true;
		}

		public TechHindiSiteAutoConfigDialog
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

		private void checkBoxBidirectional_CheckedChanged(object sender, EventArgs e)
		{
			UpdateUI();
			IsModified = true;
		}

		protected void UpdateUI()
		{
			labelConvertFunctionReverse.Visible =
				textBoxConvertFunctionReverse.Visible =
				buttonConvertFunctionReverse.Visible = checkBoxBidirectional.Checked;
		}

		private void buttonBrowse_Click(object sender, EventArgs e)
		{
			if (!String.IsNullOrEmpty(textBoxFileSpec.Text))
				openFileDialogBrowse.InitialDirectory = Path.GetDirectoryName(textBoxFileSpec.Text);
			else
				openFileDialogBrowse.InitialDirectory = Util.GetSpecialFolderPath(Environment.SpecialFolder.CommonApplicationData) + EncConverters.strDefMapsTablesPath;

			if (openFileDialogBrowse.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				ResetFields();
				textBoxFileSpec.Text = openFileDialogBrowse.FileName;
				IsModified = true;
			}
		}

		private void radioButton_CheckedChanged(object sender, EventArgs e)
		{
			IsModified = true;
		}

		protected override string DefaultFriendlyName
		{
			get
			{
				if (!String.IsNullOrEmpty(textBoxFileSpec.Text))
					return Path.GetFileNameWithoutExtension(textBoxFileSpec.Text); ;
				return null;
			}
		}

		protected override string ImplType
		{
			get
			{
				return TechHindiSiteEncConverter.CstrImplementationType;
			}
		}

		protected override string ProgID
		{
			get
			{
				return typeof(TechHindiSiteEncConverter).FullName;
			}
		}

		// this method is called either when the user clicks the "Apply" or "OK" buttons *OR* if she
		//  tries to switch to the Test or Advanced tab. This is the dialog's one opportunity
		//  to make sure that the user has correctly configured a legitimate converter.
		protected override bool OnApply()
		{
			// if we're actually on the setup tab, do some further checking as well.
			if (tabControl.SelectedTab == tabPageSetup)
			{
				// only do these message boxes if we're on the Setup tab itself, because if this OnApply
				//  is being called as a result of the user switching to the Test tab, that code will
				//  already put up an error message and we don't need two error messages.
				if (String.IsNullOrEmpty(textBoxFileSpec.Text))
				{
					MessageBox.Show(this, "Choose the html converter file first!", EncConverters.cstrCaption);
					return false;
				}
				if (String.IsNullOrEmpty(textBoxInputId.Text))
				{
					MessageBox.Show(this, "Indicate the html element for the input data (e.g. legacy_text) first.", EncConverters.cstrCaption);
					return false;
				}
				if (String.IsNullOrEmpty(textBoxOutputId.Text))
				{
					MessageBox.Show(this, "Indicate the html element for the output data (e.g. unicode_text) first.", EncConverters.cstrCaption);
					return false;
				}
				if (String.IsNullOrEmpty(textBoxConvertFunctionForward.Text))
				{
					MessageBox.Show(this, "Indicate the script function for converting data (e.g. convert_to_unicode) first.", EncConverters.cstrCaption);
					return false;
				}
				if (checkBoxBidirectional.Checked && String.IsNullOrEmpty(textBoxConvertFunctionReverse.Text))
				{
					MessageBox.Show(this, "Indicate the script function for converting data in the reverse direction (e.g. convert_to_Shusha) first.", EncConverters.cstrCaption);
					return false;
				}
			}

			// for this converter, the converter identifier is made up of:
			//     <uri to file>;
			//     <id of input (legacy) textarea>;
			//     <id of output (unicode) textarea>;
			//     <name of function to do conversion>;
			//     (<name of function to do reverse conversion>)
			ConverterIdentifier = String.Format("{0};{1};{2};{3}",
				textBoxFileSpec.Text,
				textBoxInputId.Text,
				textBoxOutputId.Text,
				textBoxConvertFunctionForward.Text);

			if (checkBoxBidirectional.Checked && !String.IsNullOrEmpty(textBoxConvertFunctionReverse.Text))
				ConverterIdentifier += String.Format(";{0}", textBoxConvertFunctionReverse.Text);

			if (radioButtonLegacyToLegacy.Checked)
			{
				if (checkBoxBidirectional.Checked)
					ConversionType = ConvType.Legacy_to_from_Legacy;
				else
					ConversionType = ConvType.Legacy_to_Legacy;
			}
			else if (radioButtonLegacyToUnicode.Checked)
			{
				if (checkBoxBidirectional.Checked)
					ConversionType = ConvType.Legacy_to_from_Unicode;
				else
					ConversionType = ConvType.Legacy_to_Unicode;
			}
			else if (radioButtonUnicodeToUnicode.Checked)
			{
				if (checkBoxBidirectional.Checked)
					ConversionType = ConvType.Unicode_to_from_Unicode;
				else
					ConversionType = ConvType.Unicode_to_Unicode;
			}
			else
				System.Diagnostics.Debug.Assert(false);

			try
			{
				return base.OnApply();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, EncConverters.cstrCaption);
				return false;
			}
		}

		private void buttonConvertFunctionForward_Click(object sender, EventArgs e)
		{
			HarvestConvertFunctionName(textBoxConvertFunctionForward, "convert_to_unicode");
		}

		private void buttonConvertFunctionReverse_Click(object sender, EventArgs e)
		{
			HarvestConvertFunctionName(textBoxConvertFunctionReverse, null);
		}

		protected void HarvestConvertFunctionName(TextBox textBoxConvertFunction, string strDefaultValue)
		{
			string strHtmlSource = GetHtmlSource(textBoxFileSpec.Text);
			if (String.IsNullOrEmpty(strHtmlSource))
				return;

			ElementPicker dlg = new ElementPicker(strHtmlSource, @"on[cC]lick=""(.*)\(", strDefaultValue);
			if (dlg.ShowDialog() == DialogResult.OK)
				textBoxConvertFunction.Text = dlg.SelectedElement;
		}

		private void buttonBrowseInputId_Click(object sender, EventArgs e)
		{
			HarvestElementId(textBoxInputId, "legacy_text");
		}

		private void buttonOutputId_Click(object sender, EventArgs e)
		{
			HarvestElementId(textBoxOutputId, "unicode_text");
		}

		private void HarvestElementId(TextBox tb, string strDefaultValue)
		{
			string strHtmlSource = GetHtmlSource(textBoxFileSpec.Text);
			if (String.IsNullOrEmpty(strHtmlSource))
				return;

			ElementPicker dlg = new ElementPicker(strHtmlSource, @"textarea .* id=""(.*?)""", strDefaultValue);

			if (dlg.ShowDialog() == DialogResult.OK)
				tb.Text = dlg.SelectedElement;
		}

		private void textBoxFileSpec_TextChanged(object sender, EventArgs e)
		{
			if (m_bInitialized) // but only do this after we're already initialized
			{
				IsModified = (((TextBox)sender).Text.Length > 0);
				UpdateUI();
			}
		}

		protected string GetHtmlSource(string strUri)
		{
			Uri uriConverter = new Uri(strUri);
			if (uriConverter.IsFile)
			{
				if (!File.Exists(strUri))
				{
					MessageBox.Show(String.Format("The html converter file '{0}' doesn't exist!", strUri), EncConverters.cstrCaption);
					return null;
				}

				return File.ReadAllText(strUri);
			}

			MessageBox.Show("The html converter must be a file stored on the local computer!", EncConverters.cstrCaption);
			return null;

			/*
			// otherwise, assume it's a webpage (code from: http://www.csharp-station.com/howto/httpwebfetch.aspx)
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uriConverter);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream resStream = response.GetResponseStream();

			int count = 0;
			byte[] buf = new byte[8192];
			StringBuilder sb = new StringBuilder();
			do
			{
				// fill the buffer with data
				count = resStream.Read(buf, 0, buf.Length);

				// make sure we read some data
				if (count != 0)
				{
					// translate from bytes to ASCII text
					string tempString = Encoding.UTF8.GetString(buf, 0, count);

					// continue building the string
					sb.Append(tempString);
				}
			}
			while (count > 0); // any more data to read?

			// print out page source
			return sb.ToString();
			*/
		}
	}
}
