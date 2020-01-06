// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;
using SilEncConverters40;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Allows the user to try out the converters.
	/// </summary>
	internal sealed class ConverterTrial : UserControl
	{
		private FwOverrideComboBox outputFontCombo;
		private OpenFileDialogAdapter ofDlg;
		private string m_mapname; // name of the conversion to apply when convert is pressed.
		private StringBuilder m_savedOutput; // saves the converted data for saving to a file
		private SampleView m_svOutput;
		private bool m_fHasOutput;
		private Panel OutputPanel;
		private SaveFileDialogAdapter saveFileDialog;
		private TextBox txtInputFile;
		private ToolTip toolTipInputFile;
		private Button convertButton;
		private Button saveFileButton;
		private System.ComponentModel.IContainer components;

		/// <summary />
		internal EncConverters Converters { get; set; }

		/// <summary />
		internal ConverterTrial()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			ofDlg = new OpenFileDialogAdapter
			{
				DefaultExt = "txt",
				Filter = FileUtils.FileDialogFilterCaseInsensitiveCombinations(FwCoreDlgs.ofDlg_Filter)
			};

			saveFileDialog = new SaveFileDialogAdapter
			{
				DefaultExt = "txt",
				RestoreDirectory = true,
				Filter = ofDlg.Filter
			};

			if (DesignMode)
			{
				return;
			}
			InputArgsChanged(); // set the initial state of the Convert button

			// Set view properties.
			m_fHasOutput = false;
			m_svOutput = new SampleView
			{
				WritingSystemFactory = FwUtils.CreateWritingSystemManager(),
				Dock = DockStyle.Fill,
				Visible = true,
				Enabled = false,
				BackColor = OutputPanel.BackColor,
				TabIndex = 1,
				TabStop = true
			};
			OutputPanel.Controls.Add(m_svOutput);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();

				saveFileDialog.Dispose();
				ofDlg.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Button selectFileButton;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConverterTrial));
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label3;
			System.Windows.Forms.HelpProvider helpProvider1;
			this.outputFontCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.convertButton = new System.Windows.Forms.Button();
			this.OutputPanel = new System.Windows.Forms.Panel();
			this.saveFileButton = new System.Windows.Forms.Button();
			this.txtInputFile = new System.Windows.Forms.TextBox();
			this.toolTipInputFile = new System.Windows.Forms.ToolTip(this.components);
			selectFileButton = new System.Windows.Forms.Button();
			label2 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			helpProvider1 = new HelpProvider();
			this.SuspendLayout();
			//
			// selectFileButton
			//
			helpProvider1.SetHelpString(selectFileButton, resources.GetString("selectFileButton.HelpString"));
			resources.ApplyResources(selectFileButton, "selectFileButton");
			selectFileButton.Name = "selectFileButton";
			helpProvider1.SetShowHelp(selectFileButton, ((bool)(resources.GetObject("selectFileButton.ShowHelp"))));
			selectFileButton.Click += new System.EventHandler(this.selectFileButton_Click);
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			helpProvider1.SetHelpString(label2, resources.GetString("label2.HelpString"));
			label2.Name = "label2";
			helpProvider1.SetShowHelp(label2, ((bool)(resources.GetObject("label2.ShowHelp"))));
			//
			// outputFontCombo
			//
			resources.ApplyResources(this.outputFontCombo, "outputFontCombo");
			this.outputFontCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			helpProvider1.SetHelpString(this.outputFontCombo, resources.GetString("outputFontCombo.HelpString"));
			this.outputFontCombo.Name = "outputFontCombo";
			helpProvider1.SetShowHelp(this.outputFontCombo, ((bool)(resources.GetObject("outputFontCombo.ShowHelp"))));
			this.outputFontCombo.SelectedIndexChanged += new System.EventHandler(this.outputFontCombo_SelectedIndexChanged);
			//
			// convertButton
			//
			resources.ApplyResources(this.convertButton, "convertButton");
			helpProvider1.SetHelpString(this.convertButton, resources.GetString("convertButton.HelpString"));
			this.convertButton.Name = "convertButton";
			helpProvider1.SetShowHelp(this.convertButton, ((bool)(resources.GetObject("convertButton.ShowHelp"))));
			this.convertButton.Click += new System.EventHandler(this.convertButton_Click);
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			helpProvider1.SetShowHelp(label3, ((bool)(resources.GetObject("label3.ShowHelp"))));
			//
			// OutputPanel
			//
			resources.ApplyResources(this.OutputPanel, "OutputPanel");
			this.OutputPanel.BackColor = System.Drawing.SystemColors.Control;
			this.OutputPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			helpProvider1.SetHelpString(this.OutputPanel, resources.GetString("OutputPanel.HelpString"));
			this.OutputPanel.Name = "OutputPanel";
			helpProvider1.SetShowHelp(this.OutputPanel, ((bool)(resources.GetObject("OutputPanel.ShowHelp"))));
			//
			// saveFileButton
			//
			resources.ApplyResources(this.saveFileButton, "saveFileButton");
			helpProvider1.SetHelpString(this.saveFileButton, resources.GetString("saveFileButton.HelpString"));
			this.saveFileButton.Name = "saveFileButton";
			helpProvider1.SetShowHelp(this.saveFileButton, ((bool)(resources.GetObject("saveFileButton.ShowHelp"))));
			this.saveFileButton.Click += new System.EventHandler(this.saveFileButton_Click);
			//
			// txtInputFile
			//
			resources.ApplyResources(this.txtInputFile, "txtInputFile");
			this.txtInputFile.BackColor = System.Drawing.SystemColors.Window;
			this.txtInputFile.Cursor = System.Windows.Forms.Cursors.Arrow;
			this.txtInputFile.Name = "txtInputFile";
			this.txtInputFile.ReadOnly = true;
			helpProvider1.SetShowHelp(this.txtInputFile, ((bool)(resources.GetObject("txtInputFile.ShowHelp"))));
			this.txtInputFile.TabStop = false;
			//
			// ConverterTest
			//
			this.Controls.Add(this.txtInputFile);
			this.Controls.Add(this.convertButton);
			this.Controls.Add(this.saveFileButton);
			this.Controls.Add(this.OutputPanel);
			this.Controls.Add(label3);
			this.Controls.Add(label2);
			this.Controls.Add(this.outputFontCombo);
			this.Controls.Add(selectFileButton);
			this.Name = "ConverterTrial";
			helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			resources.ApplyResources(this, "$this");
			this.Load += new System.EventHandler(this.ConverterTest_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// <summary>
		/// The input conditions for convert changed. Clear the output window and figure
		/// whether the button should be enabled.
		/// </summary>
		private void InputArgsChanged()
		{
			convertButton.Enabled = !string.IsNullOrEmpty(ofDlg.FileName) && !string.IsNullOrEmpty(m_mapname);
			m_savedOutput = null;
			saveFileButton.Enabled = false;
		}

		/// <summary />
		internal void SelectMapping(string mapname)
		{
			m_mapname = mapname;
			InputArgsChanged();
		}

		private void convertButton_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(m_mapname))
			{
				using (new WaitCursor(this))
				{
					try
					{
						DoFileConvert(Converters[m_mapname], ofDlg.FileName);
					}
					catch
					{
						var resourceStrings = new ResourceManager("SIL.FieldWorks.FwCoreDlgs.AddConverterDlgStrings", Assembly.GetExecutingAssembly());
						MessageBox.Show(this, resourceStrings.GetString("kstidErrorConvertingTestFile"), resourceStrings.GetString("kstidConversionError"));
					}
				}
				saveFileButton.Enabled = m_fHasOutput;
				m_svOutput.Enabled = m_fHasOutput;
			}
		}

		private void selectFileButton_Click(object sender, EventArgs e)
		{
			if (ofDlg.ShowDialog() == DialogResult.Cancel)
			{
				ofDlg.FileName = string.Empty;
			}
			else
			{
				txtInputFile.Text = ofDlg.FileName;
				toolTipInputFile.SetToolTip(txtInputFile, ofDlg.FileName);
				InputArgsChanged();
			}
		}

		private void ConverterTest_Load(object sender, System.EventArgs e)
		{
			// This is a fall-back if the creator does not have a converters object.
			// It is generally preferable for the creator to make one and pass it in.
			// Multiple EncConverters objects are problematical because they don't all get
			// updated when something changes.
			if (Converters == null)
			{
				Converters = new EncConverters();
			}
			using (var installedFontCollection = new InstalledFontCollection())
			{
				// Get the array of FontFamily objects.
				var fontFamilies = installedFontCollection.Families;
				// The loop below creates a large string that is a comma-separated
				// list of all font family names.
				var count = fontFamilies.Length;
				for (var j = 0; j < count; ++j)
				{
					var familyName = fontFamilies[j].Name;
					outputFontCombo.Items.Add(familyName);
				}

				outputFontCombo.SelectedItem = m_svOutput.Font.Name;
			}
		}

		/// <summary>
		/// Convert a file and display it in the window
		/// </summary>
		public void DoFileConvert(IEncConverter ec, string inputFilename)
		{
			// start hour glass
			using (new WaitCursor(this))
			{
				// open the input and output files using the given encoding formats
				// 28591 is a 'magic' code page that stuffs each input byte into
				// the low byte of the unicode character, leaving the top byte zero.
				// This is a good code page to use because it is simple and fully reversible
				// for any input.
				using (var reader = new StreamReader(inputFilename, Encoding.GetEncoding(EncodingConstants.kMagicCodePage), true))
				{
					// This tells the converter that the input will be 16-bit characters
					// produced by converting the bytes of the file using CP28591.
					ec.CodePageInput = EncodingConstants.kMagicCodePage;
					ec.EncodingIn = EncodingForm.LegacyString;
					reader.BaseStream.Seek(0, SeekOrigin.Begin);
					// read the lines of the input file, (optionally convert,) and write them out.
					var sOutput = string.Empty;
					m_savedOutput = new StringBuilder();
					m_svOutput.Clear(false);
					m_fHasOutput = false;

					// Read the lines of the input file, convert them, and display them
					// in the view.
					while (reader.Peek() > -1)
					{
						var sInput = reader.ReadLine();

						if (sInput == string.Empty || sInput.StartsWith(@"\_sh ") || sInput.StartsWith(@"\id "))
						{
							sOutput = sInput;
						}
						else
						{
							sOutput = ConvertOneLine(ec, sInput);
						}
						m_svOutput.AddPara(sOutput);
						m_savedOutput.AppendLine(sOutput);
						m_fHasOutput = true;
					}

					reader.Close();
					m_svOutput.CompleteSetText();
				}
			}
		}

		/// <summary>
		/// Convert one line of text for display
		/// </summary>
		private string ConvertOneLine(IEncConverter ec, string input)
		{
			var marker = string.Empty;
			var remainder = string.Empty;

			// if the input string is empty, don't try to convert it
			if (input == string.Empty)
			{
				return input;
			}
			// split the marker and the remainder portions of the text line.
			if (input[0] != '\\')
			{
				remainder = input;
			}
			else
			{
				var spacePosition = input.IndexOf(" ");
				if (spacePosition == -1)
				{
					return input;
				}
				marker = input.Substring(0, spacePosition);
				remainder = input.Substring(spacePosition);
			}

			return marker + ec.Convert(remainder);
		}

		private void outputFontCombo_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (outputFontCombo.SelectedIndex >= 0) // valid selection item
			{
				m_svOutput.FontName = (string)outputFontCombo.SelectedItem;
			}
		}

		/// <summary>
		/// Handle clicking the save file button. Save the converted contents to a file.
		/// </summary>
		private void saveFileButton_Click(object sender, System.EventArgs e)
		{
			if (m_savedOutput == null)
			{
				return;
			}
			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				using (var myStream = saveFileDialog.OpenFile())
				{
					if (myStream != null)
					{
						using (var sw = new StreamWriter(myStream)) // defaults to UTF-8
						{
							sw.Write(m_savedOutput.ToString());
							sw.Flush();
							myStream.Close();
						}
					}
				}
			}
		}

		/// <summary />
		private sealed class SampleView : SimpleRootSite
		{
			private string m_fontName;
			private SampleVc m_svc;
			const int khvoFirstPara = 1000; // Arbitrarily, paragraphs are numbered from 1000.
			private int m_hvoNextPara = khvoFirstPara;
			private ISilDataAccess m_sda;
			private IVwCacheDa m_cd;

			#region IDisposable override

			/// <inheritdoc />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				base.Dispose(disposing);

				if (disposing)
				{
					// Dispose managed resources here.
					if (m_cd != null)
					{
						m_cd.ClearAllData();
						if (Marshal.IsComObject(m_cd))
						{
							Marshal.ReleaseComObject(m_cd);
						}
					}
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_svc = null;
				m_sda = null;
				m_cd = null;
			}

			#endregion IDisposable override

			/// <summary />
			public string FontName
			{
				get
				{
					return m_fontName;
				}
				set
				{
					m_fontName = value;
					m_svc.FontName = value;
					if (Enabled)
					{
						RootBox.Reconstruct();
					}
				}
			}

			/// <summary />
			private int HvoRoot { get; } = 1;

			/// <summary>
			/// Clear all text. If fReconstruct is true, also clears the display.
			/// </summary>
			internal void Clear(bool fReconstruct)
			{
				if (m_sda == null)
				{
					var cda = VwCacheDaClass.Create();
					cda.TsStrFactory = TsStringUtils.TsStrFactory;
					m_sda = cda;
					if (WritingSystemFactory != null)
					{
						m_sda.WritingSystemFactory = WritingSystemFactory;
					}
					m_cd = cda;
				}
				else
				{
					m_hvoNextPara = khvoFirstPara;
					m_cd.CacheVecProp(HvoRoot, (int)SampleTags.ktagTextParas, new int[0], 0);
					if (fReconstruct)
					{
						RootBox.Reconstruct();
					}
				}
			}

			/// <summary>
			/// Add a new string as the next line of the text. Does not display.
			/// Call Complete() to update after adding all lines.
			/// </summary>
			private void AddPara(ITsString tss)
			{
				var hvoPara = m_hvoNextPara++;
				m_cd.CacheStringProp(hvoPara, (int)SampleTags.ktagParaContents, tss);
			}

			/// <summary>
			/// Add a sample paragraph just using a string. A TsString encoded using the UI
			/// writing system is created.
			/// </summary>
			internal void AddPara(string para)
			{
				AddPara(TsStringUtils.MakeString(para, WritingSystemFactory.UserWs));
			}

			/// <summary>
			/// Call this when done adding paragraphs. The typical calling sequence is
			/// Clear(), AddPara() x n, CompleteSetText().
			/// </summary>
			internal void CompleteSetText()
			{
				var rghvo = new int[m_hvoNextPara - khvoFirstPara];
				for (var i = 0; i < rghvo.Length; ++i)
				{
					rghvo[i] = i + khvoFirstPara;
				}
				m_cd.CacheVecProp(HvoRoot, (int)SampleTags.ktagTextParas, rghvo, rghvo.Length);
				RootBox.Reconstruct();
				RootBox.MakeSimpleSel(true, true, false, true);
			}

			#region Overrides of RootSite

			/// <inheritdoc />
			public override void MakeRoot()
			{
				if (!GotCacheOrWs || DesignMode)
					return;

				base.MakeRoot();

				m_svc = new SampleVc
				{
					FontName = m_fontName
				};

				Clear(false);
				RootBox.DataAccess = m_sda;
				RootBox.SetRootObject(HvoRoot, m_svc, (int)SampleFrags.kfrText, null);
				m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			}

			/// <inheritdoc />
			protected override void OnKeyDown(KeyEventArgs e)
			{
				base.OnKeyDown(e);
				switch (e.KeyCode)
				{
					case Keys.Tab:
						FindForm().SelectNextControl(this, (e.Modifiers & Keys.Shift) != Keys.Shift, true, true, true);
						e.Handled = true;
						break;
					case Keys.Escape:
						FindForm().Close();
						e.Handled = true;
						break;
				}
			}
			#endregion

			/// <summary />
			private sealed class SampleVc : FwBaseVc
			{
				/// <summary />
				internal string FontName { private get; set; }

				/// <inheritdoc />
				public override void Display(IVwEnv vwenv, int hvo, int frag)
				{
					switch (frag)
					{
						case (int)SampleFrags.kfrText:
							if (!string.IsNullOrEmpty(FontName))
							{
								vwenv.set_StringProperty((int)FwTextPropType.ktptFontFamily, FontName);
							}
							// Force to 12 point.
							vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 12000);
							vwenv.OpenDiv();
							vwenv.AddLazyVecItems((int)SampleTags.ktagTextParas, this, (int)SampleFrags.kfrPara);
							vwenv.CloseDiv();
							break;
						case (int)SampleFrags.kfrPara:
							vwenv.AddStringProp((int)SampleTags.ktagParaContents, this);
							break;
					}
				}

				/// <inheritdoc />
				public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
				{
					// About 3 lines (in points). One line is probably more typical, but it
					// is better to guess high (which causes extra steps in the expansion process)
					// than to guess low (and expand stuff we don't need).
					return 39;
				}

				/// <inheritdoc />
				public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent, int tag, int frag, int ihvoMin)
				{
					// All our data is preloaded, there is nothing to do.
				}
			}

			/// <summary />
			private enum SampleTags
			{
				/// <summary />
				ktagTextParas = 1001,
				/// <summary />
				ktagParaContents = 1002
			}

			/// <summary />
			private enum SampleFrags
			{
				/// <summary />
				kfrText = 101,
				/// <summary />
				kfrPara = 102
			}
		}
	}
}