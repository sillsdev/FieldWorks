using System;
using System.ComponentModel;
using System.Collections;               // for Hashtable (ECAttributes)
using System.Windows.Forms;
using System.Drawing;                   // for Font
using Microsoft.Win32;                  // for RegistryKey
using System.Text;                      // for Encoding
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters40
{
	public partial class AutoConfigDialog : Form
	{
		public string FriendlyName;
		public string ConverterIdentifier;
		public string LhsEncodingId;
		public string RhsEncodingId;
		public ConvType ConversionType;
		public int ProcessType;
		public bool IsInRepository;
		public bool IsQueryToUseTempConverter;

		protected string m_strOriginalFriendlyName;
		protected ConvType m_eOrigConvType;
		protected IEncConverters m_aECs;
		protected bool m_bQueryForConvType = false;
		protected bool m_bQueryToUseTempConverter = true;
		protected bool m_bEditMode;
		protected bool m_bAdvancedTabVisited = false;
		protected int m_nLhsExpects;
		protected int m_nRhsReturns;
		protected IEncConverter m_aEC = null;

		public AutoConfigDialog()
		{
			InitializeComponent();

			helpProvider.SetHelpString(buttonSaveInRepository, Properties.Resources.SaveInRepositoryHelpString);
			helpProvider.SetHelpString(ecTextBoxInput, Properties.Resources.TestInputBoxHelpString);
			helpProvider.SetHelpString(ecTextBoxOutput, Properties.Resources.TestOutputBoxHelpString);
			helpProvider.SetHelpString(richTextBoxHexInput, Properties.Resources.TestHexDecOutputBoxesHelpString);
			helpProvider.SetHelpString(richTextBoxHexOutput, Properties.Resources.TestHexDecOutputBoxesHelpString);
		}

		public virtual void Initialize
			(
			IEncConverters aECs,
			string strHtmlFileName,
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
			m_strOriginalFriendlyName = FriendlyName = strFriendlyName;
			ConverterIdentifier = strConverterIdentifier;
			LhsEncodingId = strLhsEncodingId;
			RhsEncodingId = strRhsEncodingId;
			m_eOrigConvType = ConversionType = eConversionType;
			ProcessType = lProcessTypeFlags;
			IsInRepository = bIsInRepository;

			m_aECs = aECs;

			// if the identifier is given, then it means we're editing.
			// (which means our button says *Update* rather than "Save in system repository"
			//  and we should ask during OnOK whether they want to update or not)
			m_bEditMode = !String.IsNullOrEmpty(ConverterIdentifier);

			// if we're editing, then it starts out clean
			IsModified = !m_bEditMode;

			// if we're editing, then we already have this converter in the collection (even if it's
			//  temporary)
			if (m_bEditMode)
			{
				m_aEC = m_aECs[FriendlyName];
				if (FriendlyName.IndexOf(EncConverters.cstrTempConverterPrefix) == 0)
					FriendlyName = m_strOriginalFriendlyName = null;
			}

			// this parameter seems the most confusing and yet is a crucial part of EncConverters
			//  so if this is *given* to us, then just use those values rather than prompting the user
			//  for them (e.g. FW knows that the BulkEdits are Unicode_to_(from_)Unicode, so we don't
			//  have to bother the user with it.
			// If we're in 'edit' mode, it is often because the user has mis-configured these parameters
			//  so if edit mode, then query for the ConvType value.
			if ((ConversionType == ConvType.Unknown) || m_bEditMode)
			{
				m_bQueryForConvType = true;
				SetConvTypeControls();
			}

			this.Text = strDisplayName;

			// get the help for the about tab
			RegistryKey keyRoot = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\SIL\SilEncConverters40", false);
			if (keyRoot != null)
			{
				string strXmlFilePath = (string)keyRoot.GetValue("RootDir");
				if (strXmlFilePath[strXmlFilePath.Length - 1] != '\\')
					strXmlFilePath += '\\';
				strXmlFilePath += @"help\" + strHtmlFileName;
				System.Diagnostics.Debug.Assert(System.IO.File.Exists(strXmlFilePath), String.Format("Can find '{0}'. If this is a development machine, you need to add the following reg key to see the About help files: HLKM\\SOFTWARE\\SIL\\SilEncConverters40\\[RootDir] = '<parent folder where the 'help' sub-folder exists>' along with a trailing slash (e.g. \"C:\\fw\\lib\\release\\\")", strHtmlFileName));
				this.webBrowserHelp.Url = new Uri(strXmlFilePath);
			}
#if DEBUG
			else
				throw new ApplicationException(@"Can't read the HLKM\SOFTWARE\SIL\SilEncConverters40\[RootDir] registry key!");
#endif

			ecTextBoxInput.Text = "Test Data";
		}

		protected virtual void SetConvTypeControls()
		{
			// usually for giving sub-classes an opportunity to query for the ConvType
		}

		public virtual void Initialize
			(
			IEncConverters aECs,
			string strFriendlyName,
			string strConverterIdentifier,
			ConvType eConversionType,
			string strTestData
			)
		{
			FriendlyName = strFriendlyName;
			ConverterIdentifier = strConverterIdentifier;
			ConversionType = eConversionType;

			m_aECs = aECs;
			m_aEC = InitializeEncConverter;

			tabControl.Controls.Remove(tabPageAbout);
			tabControl.Controls.Remove(tabPageSetup);
			tabControl.Controls.Remove(tabPageAdvanced);

			// for 'test', it's possible that there may be some font mapping in the repository
			string strLhsName, strRhsName;
			if (m_aECs.GetFontMappingFromMapping(strFriendlyName, out strLhsName, out strRhsName))
			{
				ecTextBoxInput.Font = CreateFontSafe(strLhsName, ecTextBoxInput.Font);
				ecTextBoxOutput.Font = CreateFontSafe(strRhsName, ecTextBoxOutput.Font);
			}

			ecTextBoxInput.Text = strTestData;
			buttonOK.Visible = buttonApply.Visible = false;
			buttonCancel.Text = "Close";

			helpProvider.SetHelpString(buttonCancel, Properties.Resources.CloseButtonHelpString);

		}

		// the creation of a Font can throw an exception if, for example, you try to construct one with
		//  the default style 'Regular' when the font itself doesn't have a Regular style. So this method
		//  can be called to create one and it'll try different styles if it fails.
		protected int cnDefaultFontSize = 14;
		protected Font CreateFontSafe(string strFontName, Font fontDefault)
		{
			Font font = null;
			try
			{
				font = new Font(strFontName, cnDefaultFontSize);
			}
			catch
			{
				font = fontDefault;
			}
			return font;
		}

		protected void SetConvTypeFromRbControls
			(
			RadioButton rbExpectsUnicode,
			RadioButton rbExpectsLegacy,
			RadioButton rbReturnsUnicode,
			RadioButton rbReturnsLegacy
			)
		{
			if (rbExpectsUnicode.Checked)
			{
				if (rbReturnsUnicode.Checked)
				{
					ConversionType = ConvType.Unicode_to_Unicode;
				}
				else
				{
					ConversionType = ConvType.Unicode_to_Legacy;
				}
			}
			else
			{
				if (rbReturnsUnicode.Checked)
				{
					ConversionType = ConvType.Legacy_to_Unicode;
				}
				else
				{
					ConversionType = ConvType.Legacy_to_Legacy;
				}
			}
		}

		protected void SetRbValuesFromConvType
			(
			RadioButton rbExpectsUnicode,
			RadioButton rbExpectsLegacy,
			RadioButton rbReturnsUnicode,
			RadioButton rbReturnsLegacy
			)
		{
			switch (ConversionType)
			{
				case ConvType.Legacy_to_Unicode:
				case ConvType.Legacy_to_from_Unicode:
					{
						rbExpectsLegacy.Checked =
						rbReturnsUnicode.Checked = true;
						break;
					};
				case ConvType.Unicode_to_Legacy:
				case ConvType.Unicode_to_from_Legacy:
					{
						rbExpectsUnicode.Checked =
						rbReturnsLegacy.Checked = true;
						break;
					};
				case ConvType.Legacy_to_Legacy:
				case ConvType.Legacy_to_from_Legacy:
					{
						rbExpectsLegacy.Checked =
						rbReturnsLegacy.Checked = true;
						break;
					};
				case ConvType.Unicode_to_Unicode:
				case ConvType.Unicode_to_from_Unicode:
				default:
					{
						rbExpectsUnicode.Checked =
						rbReturnsUnicode.Checked = true;
						break;
					};
			};
		}

		protected bool m_bIsModified = true;  // start out 'dirty' (to force OnApply if we switch to another tab)

		public bool IsModified
		{
			get { return m_bIsModified; }
			set
			{
				buttonApply.Enabled = m_bIsModified = value;
			}
		}

		protected void UpdateLegacyCodes(string strInputString, int cp, RichTextBox lableUniCodes)
		{
			// to get the real byte values, we need to first convert it using the def code page
			byte[] aby = EncConverters.GetBytesFromEncoding(cp, strInputString, true);
			string strWhole = null;
			foreach (byte by in aby)
				strWhole += String.Format("{0:D3} ", (int)by);

			lableUniCodes.Text = strWhole;
		}

		protected void UpdateUniCodes(string strInputString, RichTextBox lableUniCodes)
		{
			string strWhole = null;
			foreach (char ch in strInputString)
				strWhole += String.Format("{0:X4} ", (int)ch);

			lableUniCodes.Text = strWhole;
		}

		protected bool IsLhsLegacy(IEncConverter aEC)
		{
			if (aEC.DirectionForward)
				return (EncConverter.NormalizeLhsConversionType(aEC.ConversionType) == NormConversionType.eLegacy);
			else
				return (EncConverter.NormalizeRhsConversionType(aEC.ConversionType) == NormConversionType.eLegacy);
		}

		private void buttonApply_Click(object sender, EventArgs e)
		{
			OnApply();
		}

		// this method is called either when the user clicks the "Apply" or "OK" buttons *OR* if she
		//  tries to switch to the Test or Advanced tab. It is usually called by one of the sub-classes
		//  which have overriden the method in order to confirm their own configuration information. So
		//  here is checking code that all possible sub-classes must adhere to (or totally re-write).
		protected virtual bool OnApply()
		{
			// every converter must have a Converter Identifier (I think)
			if (String.IsNullOrEmpty(ConverterIdentifier))
				return false;

			// if the user has switched to the Advanced tab...
			else if (m_bAdvancedTabVisited)
			{
				// means we're saving that extra information in the repository also (e.g. Encoding Names)
				// update the values from those the Advanced tab
				FriendlyName = textBoxFriendlyName.Text;
				LhsEncodingId = comboBoxEncodingNamesLhs.Text;
				RhsEncodingId = comboBoxEncodingNamesRhs.Text;
				ProcessType = ProcessTypesChecked;

				// finally, add it to the repository
				//  (again, some sub-classes do something different at this point, so call a virtual function)
				AddConverterMapping();
				IsModified = false;
			}
			else
			{
				m_aEC = InitializeEncConverter;
				IsModified = false;

				// finally, if we're in 'edit mode' and the converter is already in the repository
				//  then re-add it to save changes to the repository (i.e. make the default behavior
				//  to save to repository in this case, rather than requiring the user to explicitly
				//  click the Update... button)
				if ((m_aEC != null) && m_bEditMode && IsInRepository)
					AddConverterMapping();
			}

			return true;
		}

		protected virtual string ProgID
		{
			get
			{
				// must be overriden
				System.Diagnostics.Debug.Assert(false);
				return null;
			}
		}

		protected virtual string ImplType
		{
			get
			{
				// must be overriden
				System.Diagnostics.Debug.Assert(false);
				return null;
			}
		}

		protected virtual string DefaultFriendlyName
		{
			get
			{
				// must be overriden
				System.Diagnostics.Debug.Assert(false);
				return null;
			}
		}

		protected void ResetFields()
		{
			ConversionType = m_eOrigConvType;
			ConverterIdentifier = null;
			ProcessType = 0;
			m_aEC = null;
		}


		protected virtual IEncConverter InitializeEncConverter
		{
			get
			{
				// gotta have the converter identifier to call this property
				System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(ConverterIdentifier));

				// if it changed, then start over again
				if ((m_aEC != null) && (ConverterIdentifier != m_aEC.ConverterIdentifier))
					m_aEC = null;

				if (m_aEC == null)
				{
					EncConverters aECs = (EncConverters)m_aECs;
					m_aEC = aECs.InstantiateIEncConverter(ProgID);
					m_aEC.Initialize(
								EncConverters.cstrTempConverterPrefix,
								ConverterIdentifier,
								ref LhsEncodingId,
								ref RhsEncodingId,
								ref ConversionType,
								ref ProcessType,
								0,
								0,
								true
							);
				}

				return m_aEC;
			}
		}

		protected virtual void AddConverterMapping()
		{
			// remove any existing converter by the name we're about to give it (probably not necessary).
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(FriendlyName));
			if (ShouldRemoveBeforeAdd)
				m_aECs.Remove(FriendlyName);

			// if it was originally under a different name...
			if (!String.IsNullOrEmpty(m_strOriginalFriendlyName) && (m_strOriginalFriendlyName != FriendlyName))
			{
				// ... remove that too (this one probably *is* necessary to remove old stuff)
				m_aECs.Remove(m_strOriginalFriendlyName);
			}

			// have the sub-classes do their thing to add it
			AddConverterMappingSub();

			// if it worked, then ...
			// ... indicate that now this converter is in the repository
			IsInRepository = true;

			// and save the name so we can clear it out if need be later
			m_strOriginalFriendlyName = FriendlyName;
		}

		// allow subclasses to define whether we should remove records before adding them
		//  (e.g. SpellFixer is already added, so we don't want to remove it or we'll clobber the
		//  extra properties added by the SpellFixer assembly)
		protected virtual bool ShouldRemoveBeforeAdd
		{
			get { return true; }
		}

		protected virtual void AddConverterMappingSub()
		{
			m_aECs.AddConversionMap
					(
						FriendlyName,
						ConverterIdentifier,
						ConversionType,
						ImplType,            // get from sub-class
						LhsEncodingId,
						RhsEncodingId,
						(ProcessTypeFlags)ProcessType
					);
		}

		protected void buttonSaveInRepositoryEx()
		{
			m_bQueryToUseTempConverter = false;
			if (IsModified && !OnApply())
				return;

			// if the user has gone to the Advanced tab, then the text box has the default (or typed in) friendly name
			if (m_bAdvancedTabVisited)
				FriendlyName = textBoxFriendlyName.Text;

			if (tabControl.SelectedTab == tabPageAdvanced)
			{
				// if we're currently *on* the advanced tab, then put a more specific error message
				if (String.IsNullOrEmpty(FriendlyName))
					MessageBox.Show(this, "Enter a 'FriendlyName' for the converter above and click again", EncConverters.cstrCaption);
			}
			else
			{
				string strFriendlyName = (String.IsNullOrEmpty(FriendlyName)) ? DefaultFriendlyName : FriendlyName;
				QueryConverterNameForm dlg = new QueryConverterNameForm(strFriendlyName);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					// means we're saving in the repository
					// update the values from those the dialog box queried
					FriendlyName = dlg.FriendlyName;
				}
			}

			// if by now, we have a friendly name...
			if (!String.IsNullOrEmpty(FriendlyName))
			{
				// then, add it to the repository
				//  (again, some sub-classes do something different at this point, so call a virtual function)
				AddConverterMapping();
				IsModified = false;
			}
		}

		private void buttonSaveInRepository_Click(object sender, EventArgs e)
		{
			buttonSaveInRepositoryEx();
		}

		protected int ProcessTypesChecked
		{
			get
			{
				int lProcessTypes = 0;
				lProcessTypes |= (checkBoxUnicodeEncodingConversion.Checked) ? (int)ProcessTypeFlags.UnicodeEncodingConversion : 0;
				lProcessTypes |= (checkBoxTransliteration.Checked) ? (int)ProcessTypeFlags.Transliteration : 0;
				lProcessTypes |= (checkBoxICUTransliteration.Checked) ? (int)ProcessTypeFlags.ICUTransliteration : 0;
				lProcessTypes |= (checkBoxICUConverter.Checked) ? (int)ProcessTypeFlags.ICUConverter : 0;
				lProcessTypes |= (checkBoxCodePage.Checked) ? (int)ProcessTypeFlags.CodePageConversion : 0;
				lProcessTypes |= (checkBoxNonUnicodeEncodingConversion.Checked) ? (int)ProcessTypeFlags.NonUnicodeEncodingConversion : 0;
				lProcessTypes |= (checkBoxSpellingFixerProject.Checked) ? (int)ProcessTypeFlags.SpellingFixerProject : 0;
				lProcessTypes |= (checkBoxICURegularExpression.Checked) ? (int)ProcessTypeFlags.ICURegularExpression : 0;
				lProcessTypes |= (checkBoxPythonScript.Checked) ? (int)ProcessTypeFlags.PythonScript : 0;
				lProcessTypes |= (checkBoxPerlExpression.Checked) ? (int)ProcessTypeFlags.PerlExpression : 0;
				lProcessTypes |= (checkBoxSpare1.Checked) ? (int)ProcessTypeFlags.UserDefinedSpare1 : 0;
				lProcessTypes |= (checkBoxSpare2.Checked) ? (int)ProcessTypeFlags.UserDefinedSpare2 : 0;
				return lProcessTypes;
			}
		}

		protected int ProcessCodePage(string strCodePageValue)
		{
			int nCodePage = 0;
			try
			{
				nCodePage = Convert.ToInt32(strCodePageValue);
			}
			catch
			{
				MessageBox.Show(String.Format("'{0}' is not a valid code page number", strCodePageValue), EncConverters.cstrCaption);
			}
			return nCodePage;
		}

		protected virtual bool SetupTabSelected_MakeSaveInRepositoryVisible
		{
			get { return true; }
		}

		private void tabControl_Selected(object sender, TabControlEventArgs e)
		{
			if (e.TabPage == tabPageSetup)
			{
				buttonSaveInRepository.Visible = SetupTabSelected_MakeSaveInRepositoryVisible;
				SetupTabSelected(e);
			}
			// if it was modified, then we need to apply it or switch back to
			//  the setup tab (unless it was the about tab that was selected)
			else if (e.TabPage != tabPageAbout)
			{
				// Test or Advanced tab.
				// If the configuration was modified, then make the user go back
				if (IsModified && !OnApply())
				{
					MessageBox.Show("You must first configure the conversion process on the Setup tab", EncConverters.cstrCaption);
					tabControl.SelectTab(tabPageSetup);
				}
				else
				{
					System.Diagnostics.Debug.Assert(!IsModified);
					IEncConverter aEC = InitializeEncConverter;
					if (aEC != null)
					{
						if (e.TabPage == tabPageTestArea)
						{
							string strLhsFont, strRhsFont;
							if (GetFontMapping(aEC.Name, out strLhsFont, out strRhsFont))
							{
								if (!String.IsNullOrEmpty(strLhsFont))
									ecTextBoxInput.Font = new Font(strLhsFont, 14);

								if (!String.IsNullOrEmpty(strRhsFont))
									ecTextBoxOutput.Font = new Font(strRhsFont, 14);
							}

							TestTabInputChanged();  // doesn't happen automatically the first time for some reason
							checkBoxTestReverse.Visible = !EncConverters.IsUnidirectional(aEC.ConversionType);
						}
						else // if (e.TabPage == tabPageAdvanced)
						{
							m_bAdvancedTabVisited = true;
							dataGridViewProperties.Visible = labelProperties.Visible = false;   // pessimistic
							if (String.IsNullOrEmpty(FriendlyName))
							{
								textBoxFriendlyName.Text = DefaultFriendlyName;
								IsModified = true;
							}
							else
							{
								textBoxFriendlyName.Text = FriendlyName;

								// we shouldn't get here with a temporary converter...
								System.Diagnostics.Debug.Assert(FriendlyName.IndexOf(EncConverters.cstrTempConverterPrefix) != 0);

								// load the grid with any existing converter property keys and their values
								// (but only do this if this is already in the collection object (or there
								//  won't be any properties by definition) and if there *are* any properties
								//  (because many sub-classes don't have any properties)
								EncConverters aECs = (EncConverters)m_aECs;
								ECAttributes attrs = null;
								if (aECs.ContainsKey(FriendlyName)
									&& ((attrs = m_aECs.Attributes(FriendlyName, AttributeType.Converter)) != null)
									&& (attrs.Count > 0))
								{
									dataGridViewProperties.Visible = labelProperties.Visible = true;
									dataGridViewProperties.Rows.Clear();
									foreach (DictionaryEntry kvp in attrs)
									{
										object[] aos = new object[] { kvp.Key, kvp.Value };
										dataGridViewProperties.Rows.Add(aos);
									}
								}
							}

							// certain sub-classes don't allow the friendly name to be modified (e.g. spellfixer)
							textBoxFriendlyName.ReadOnly = ShouldFriendlyNameBeReadOnly;

							// load the combo boxes with the existing Encoding names
							comboBoxEncodingNamesLhs.Items.Clear();
							comboBoxEncodingNamesRhs.Items.Clear();
							foreach (string strEncodingName in m_aECs.Encodings)
							{
								comboBoxEncodingNamesLhs.Items.Add(strEncodingName);
								comboBoxEncodingNamesRhs.Items.Add(strEncodingName);
							}

							// if the left-hand side Encoding name is already configured, then select that
							if (!String.IsNullOrEmpty(LhsEncodingId))
							{
								if (!comboBoxEncodingNamesLhs.Items.Contains(LhsEncodingId))
									comboBoxEncodingNamesLhs.Items.Add(LhsEncodingId);
								comboBoxEncodingNamesLhs.SelectedItem = LhsEncodingId;
							}

							// if the right-hand side Encoding name is already configured, then select that
							if (!String.IsNullOrEmpty(RhsEncodingId))
							{
								if (!comboBoxEncodingNamesRhs.Items.Contains(RhsEncodingId))
									comboBoxEncodingNamesRhs.Items.Add(RhsEncodingId);
								comboBoxEncodingNamesRhs.SelectedItem = RhsEncodingId;
							}

							// initialize the check boxes for the Process Types
							checkBoxUnicodeEncodingConversion.Checked = ((ProcessType & (int)ProcessTypeFlags.UnicodeEncodingConversion) != 0);
							checkBoxTransliteration.Checked = ((ProcessType & (int)ProcessTypeFlags.Transliteration) != 0);
							checkBoxICUTransliteration.Checked = ((ProcessType & (int)ProcessTypeFlags.ICUTransliteration) != 0);
							checkBoxICUConverter.Checked = ((ProcessType & (int)ProcessTypeFlags.ICUConverter) != 0);
							checkBoxCodePage.Checked = ((ProcessType & (int)ProcessTypeFlags.CodePageConversion) != 0);
							checkBoxNonUnicodeEncodingConversion.Checked = ((ProcessType & (int)ProcessTypeFlags.NonUnicodeEncodingConversion) != 0);
							checkBoxSpellingFixerProject.Checked = ((ProcessType & (int)ProcessTypeFlags.SpellingFixerProject) != 0);
							checkBoxICURegularExpression.Checked = ((ProcessType & (int)ProcessTypeFlags.ICURegularExpression) != 0);
							checkBoxPythonScript.Checked = ((ProcessType & (int)ProcessTypeFlags.PythonScript) != 0);
							checkBoxPerlExpression.Checked = ((ProcessType & (int)ProcessTypeFlags.PerlExpression) != 0);
							checkBoxSpare1.Checked = ((ProcessType & (int)ProcessTypeFlags.UserDefinedSpare1) != 0);
							checkBoxSpare2.Checked = ((ProcessType & (int)ProcessTypeFlags.UserDefinedSpare2) != 0);
						}
					}
				}
			}
		}

		// allow the sub-classes to specify that the Friendly name is not editable (e.g. SpellFixer)
		protected virtual bool ShouldFriendlyNameBeReadOnly
		{
			get { return false; }
		}

		// provide a way for subclasses to get the font mapping (e.g. SpellFixer projects know this implicitly)
		protected virtual bool GetFontMapping(string strFriendlyName, out string strLhsFont, out string strRhsFont)
		{
			EncConverters aECs = (EncConverters)m_aECs;
			return aECs.GetFontMappingFromMapping(strFriendlyName, out strLhsFont, out strRhsFont);
		}

		protected virtual void SetupTabSelected(TabControlEventArgs e)
		{
			// to give the sub-classes an opportunity to set things up when in 'edit' mode
		}

		private void richTextBoxInput_TextChanged(object sender, EventArgs e)
		{
			TestTabInputChanged();
		}

		protected void TestTabInputChanged()
		{
			if (m_aEC == null)  // means it hasn't been set up yet.
				return;

			if (ecTextBoxInput.TextLength > 0)
			{
				buttonTest.Enabled = true;

				NormConversionType eType = (checkBoxTestReverse.Checked)
					? EncConverter.NormalizeRhsConversionType(ConversionType) : EncConverter.NormalizeLhsConversionType(ConversionType);

				if (eType == NormConversionType.eLegacy)
					UpdateLegacyCodes(ecTextBoxInput.Text, m_aEC.CodePageInput, richTextBoxHexInput);
				else
					UpdateUniCodes(ecTextBoxInput.Text, richTextBoxHexInput);
			}
			else
			{
				richTextBoxHexInput.Clear();
				buttonTest.Enabled = false;
			}

			// whenever the input changes, clear out the output
			ecTextBoxOutput.Text = null;
			richTextBoxHexOutput.Text = null;
		}

		private void buttonTest_Click(object sender, EventArgs e)
		{
			IEncConverter aEC = InitializeEncConverter;
			if (aEC != null)
			{
				try
				{
					aEC.DirectionForward = !checkBoxTestReverse.Checked;
					ecTextBoxOutput.Text = aEC.Convert(ecTextBoxInput.Text);
				}
				catch (Exception ex)
				{
					MessageBox.Show(String.Format("Test failed! Reason: {0}", ex.Message),
						EncConverters.cstrCaption);
				}

				NormConversionType eType = (checkBoxTestReverse.Checked)
					? EncConverter.NormalizeLhsConversionType(ConversionType) : EncConverter.NormalizeRhsConversionType(ConversionType);

				if (eType == NormConversionType.eLegacy)
					UpdateLegacyCodes(ecTextBoxOutput.Text, m_aEC.CodePageOutput, richTextBoxHexOutput);
				else
					UpdateUniCodes(ecTextBoxOutput.Text, richTextBoxHexOutput);
			}
		}

		private void SomethingChanged(object sender, EventArgs e)
		{
			IsModified = true;
		}

		private void checkBoxReverse_CheckedChanged(object sender, EventArgs e)
		{
			TestTabInputChanged();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			if (OnApply())
			{
				// see if the user has *not* added the converter to the repository (i.e. it's currently
				//  a "temporary converter").
				// since it is somewhat non-obvious what a temporary converter is, let's make sure that's
				//  what the user really wanted before continuing.
				// But only do it once per instantiation (by using m_bQueryToUseTempConverter to trigger it)
				if (!IsInRepository && m_bQueryToUseTempConverter)
				{
					m_bQueryToUseTempConverter = false; // stop the nagging
					if (MessageBox.Show("You are creating a temporary converter that will only be available to this one calling program and only this one time.\n\nDo you want to make it permanent instead?", EncConverters.cstrCaption, MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
						buttonSaveInRepositoryEx();
				}

				DialogResult = DialogResult.OK;
				this.Close();
			}
		}

		private void ecTextBoxInput_TextChanged(object sender, EventArgs e)
		{
			TestTabInputChanged();
		}

		protected TextBox m_tbLastClicked = null;
		private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			ContextMenuStrip aCMS = (ContextMenuStrip)sender;
			m_tbLastClicked = (TextBox)aCMS.SourceControl;
			right2LeftToolStripMenuItem.Checked = (m_tbLastClicked.RightToLeft == RightToLeft.Yes);
		}

		private void changeFontToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
			{
				fontDialog.Font = m_tbLastClicked.Font;
				try
				{
					if (fontDialog.ShowDialog() == DialogResult.OK)
						m_tbLastClicked.Font = fontDialog.Font;
				}
				catch (Exception ex)
				{
					if (ex.Message == "Only TrueType fonts are supported. This is not a TrueType font.")
						MessageBox.Show("This doesn't appear to be a TrueType font. If you just installed it, then you need to restart this application for it to be recognized properly");
					else
						MessageBox.Show(ex.Message);
				}
			}
		}

		private void undoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
				m_tbLastClicked.Undo();
		}

		private void cutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
			{
				if (m_tbLastClicked.SelectionLength == 0)
					m_tbLastClicked.SelectAll();
				m_tbLastClicked.Cut();
			}
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
			{
				if (m_tbLastClicked.SelectionLength == 0)
					m_tbLastClicked.SelectAll();
				m_tbLastClicked.Copy();
			}
		}

		private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
				m_tbLastClicked.Paste();
		}

		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
				m_tbLastClicked.Clear();
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
				m_tbLastClicked.SelectAll();
		}

		private void right2LeftToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
			{
				ToolStripMenuItem aMenuItem = (ToolStripMenuItem)sender;
				m_tbLastClicked.RightToLeft = (aMenuItem.Checked) ? RightToLeft.Yes : RightToLeft.No;
			}
		}
	}
}