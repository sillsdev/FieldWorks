using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.Utils.FileDialog;
using SilEncConverters40;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary></summary>
	public enum SampleFrags : int
	{
		/// <summary></summary>
		kfrText = 101,
		/// <summary></summary>
		kfrPara = 102
	}
	/// <summary></summary>
	public enum SampleTags : int
	{
		/// <summary></summary>
		ktagTextParas = 1001,
		/// <summary></summary>
		ktagParaContents = 1002
	}

	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// ConverterTest class.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	internal class ConverterTest : UserControl, IFWDisposable
	{
		private FwOverrideComboBox outputFontCombo;
		private OpenFileDialogAdapter ofDlg;
		private string m_mapname; // name of the conversion to apply when convert is pressed.
		private StringBuilder m_savedOutput; // saves the converted data for saving to a file
		private EncConverters m_encConverters;
		private SampleView m_svOutput;
		private bool m_fHasOutput;
		private System.Windows.Forms.Panel OutputPanel;
		private SaveFileDialogAdapter saveFileDialog;
//		private string m_sOrigMapfile;
		private System.Windows.Forms.TextBox txtInputFile;
		private System.Windows.Forms.ToolTip toolTipInputFile;
		private Button convertButton;
		private Button saveFileButton;
		private System.ComponentModel.IContainer components;

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

		/// <summary></summary>
		public EncConverters Converters
		{
			get
			{
				CheckDisposed();
				return m_encConverters;
			}
			set
			{
				CheckDisposed();
				m_encConverters = value;
			}
		}

		/// <summary></summary>
		public ConverterTest()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			ofDlg = new OpenFileDialogAdapter();
			ofDlg.DefaultExt = "txt";
			ofDlg.Filter = FileUtils.FileDialogFilterCaseInsensitiveCombinations(FwCoreDlgs.ofDlg_Filter);

			saveFileDialog = new SaveFileDialogAdapter();
			saveFileDialog.DefaultExt = "txt";
			saveFileDialog.RestoreDirectory = true;
			saveFileDialog.Filter = ofDlg.Filter;

			if (DesignMode)
				return;

			InputArgsChanged();	// set the initial state of the Convert button

			// Set view properties.
			m_fHasOutput = false;
			m_svOutput = new SampleView();
			m_svOutput.WritingSystemFactory = new PalasoWritingSystemManager();
			m_svOutput.Dock = DockStyle.Fill;
			m_svOutput.Visible = true;
			m_svOutput.Enabled = false;
			m_svOutput.BackColor = OutputPanel.BackColor;
			m_svOutput.TabIndex = 1;
			m_svOutput.TabStop = true;
			OutputPanel.Controls.Add(m_svOutput);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");

			if (disposing && !IsDisposed)
			{
				if (components != null)
				{
					components.Dispose();
				}

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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConverterTest));
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
			this.Name = "ConverterTest";
			helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			resources.ApplyResources(this, "$this");
			this.Load += new System.EventHandler(this.ConverterTest_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The input conditions for convert changed. Clear the output window and figure
		/// whether the button should be enabled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void InputArgsChanged()
		{
			convertButton.Enabled = !string.IsNullOrEmpty(ofDlg.FileName) &&
				!string.IsNullOrEmpty(m_mapname);
			m_savedOutput = null;
			saveFileButton.Enabled = false;
		}

		/// <summary></summary>
		public void SelectMapping(string mapname)
		{
			CheckDisposed();

			m_mapname = mapname;
			InputArgsChanged();
		}

		private void convertButton_Click(object sender, System.EventArgs e)
		{
			if (m_mapname != null && m_mapname != "")
			{
				using (new WaitCursor(this))
				{
					try
					{
						DoFileConvert((IEncConverter)m_encConverters[m_mapname], ofDlg.FileName);
					}
					catch
					{
						ResourceManager resourceStrings = new ResourceManager(
							"SIL.FieldWorks.FwCoreDlgs.AddConverterDlgStrings",
							Assembly.GetExecutingAssembly());
						MessageBox.Show(this, resourceStrings.GetString("kstidErrorConvertingTestFile"),
							resourceStrings.GetString("kstidConversionError"));
					}
				}
				saveFileButton.Enabled = m_fHasOutput;
				m_svOutput.Enabled = m_fHasOutput;
//				m_sOrigMapfile = m_mapname;
			}
//			convertButton.Enabled = false;
		}

		private void selectFileButton_Click(object sender, System.EventArgs e)
		{
			if (ofDlg.ShowDialog() == DialogResult.Cancel)
				ofDlg.FileName = string.Empty;
			else
			{
				txtInputFile.Text = ofDlg.FileName;
				toolTipInputFile.SetToolTip(txtInputFile, ofDlg.FileName);

				// the converter is used only to guess the input encoding. If the user hasn't
				// yet selected a mapping, just pass null.
//				IEncConverter converter = null;
//				if (m_mapname != null && m_mapname != "")
//					converter = (IEncConverter)m_encConverters[m_mapname];
				InputArgsChanged();
			}
		}

		private void ConverterTest_Load(object sender, System.EventArgs e)
		{
			// This is a fall-back if the creator does not have a converters object.
			// It is generally preferable for the creator to make one and pass it in.
			// Multiple EncConverters objects are problematical because they don't all get
			// updated when something changes.
			if (m_encConverters == null)
				m_encConverters = new SilEncConverters40.EncConverters();

			using (InstalledFontCollection installedFontCollection = new InstalledFontCollection())
			{
				// Get the array of FontFamily objects.
				FontFamily[] fontFamilies = installedFontCollection.Families;

				// The loop below creates a large string that is a comma-separated
				// list of all font family names.

				int count = fontFamilies.Length;
				for (int j = 0; j < count; ++j)
				{
					string familyName = fontFamilies[j].Name;
					outputFontCombo.Items.Add(familyName);
				}

				outputFontCombo.SelectedItem = m_svOutput.Font.Name;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Convert a file and display it in the window
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void DoFileConvert(IEncConverter ec, string inputFilename)
		{
			CheckDisposed();

			// start hour glass
			using (new WaitCursor(this))
			{
				// open the input and output files using the given encoding formats
				// 28591 is a 'magic' code page that stuffs each input byte into
				// the low byte of the unicode character, leaving the top byte zero.
				// This is a good code page to use because it is simple and fully reversible
				// for any input.
				using (StreamReader reader = new StreamReader(inputFilename,
					Encoding.GetEncoding(EncodingConstants.kMagicCodePage), true))
				{
					// This tells the converter that the input will be 16-bit characters
					// produced by converting the bytes of the file using CP28591.
					ec.CodePageInput = EncodingConstants.kMagicCodePage;
					ec.EncodingIn = ECInterfaces.EncodingForm.LegacyString;

					reader.BaseStream.Seek(0, SeekOrigin.Begin);

					// read the lines of the input file, (optionally convert,) and write them out.
					string sOutput = string.Empty;
					string sInput;
					m_savedOutput = new StringBuilder();

					m_svOutput.Clear(false);
					m_fHasOutput = false;

					// Read the lines of the input file, convert them, and display them
					// in the view.
					while (reader.Peek() > -1)
					{
						sInput = reader.ReadLine();

						if (sInput == string.Empty || sInput.StartsWith(@"\_sh ") || sInput.StartsWith(@"\id "))
							sOutput = sInput;
						else
							sOutput = ConvertOneLine(ec, sInput);
						m_svOutput.AddPara(sOutput);
						m_savedOutput.AppendLine(sOutput);
						m_fHasOutput = true;
					}

					reader.Close();
					m_svOutput.CompleteSetText();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert one line of text for display
		/// </summary>
		/// <param name="ec"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string ConvertOneLine(IEncConverter ec, string input)
		{
			string marker = string.Empty;
			string remainder = string.Empty;

			// if the input string is empty, don't try to convert it
			if (input == string.Empty)
				return input;

			// split the marker and the remainder portions of the text line.
			if (input[0] != '\\')
				remainder = input;
			else
			{
				int spacePosition = input.IndexOf(" ");
				if (spacePosition == -1)
					return input;
				else
				{
					marker = input.Substring(0, spacePosition);
					remainder = input.Substring(spacePosition);
				}
			}

			return marker + ec.Convert(remainder);
		}

		private void outputFontCombo_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (outputFontCombo.SelectedIndex >= 0)	// valid selection item
				m_svOutput.FontName = (string)outputFontCombo.SelectedItem;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle clicking the save file button. Save the converted contents to a file.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void saveFileButton_Click(object sender, System.EventArgs e)
		{
			if (m_savedOutput == null)
				return;

			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				using (Stream myStream = saveFileDialog.OpenFile())
				{
					if (myStream != null)
					{
						using (StreamWriter sw = new StreamWriter(myStream)) // defaults to UTF-8
						{
							sw.Write(m_savedOutput.ToString());
							sw.Flush();
							myStream.Close();
						}
					}
				}
			}
		}
	}

	/// <summary></summary>
	public class SampleVc : FwBaseVc
	{
		private string m_fontName;

		/// <summary></summary>
		public string FontName
		{
			get { return m_fontName; }
			set { m_fontName = value; }
		}

		/// <summary></summary>
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch(frag)
			{
			case (int)SampleFrags.kfrText:
				if (m_fontName != null && m_fontName.Length > 0)
					vwenv.set_StringProperty((int)FwTextPropType.ktptFontFamily, m_fontName);
				// Force to 12 point.
				vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize,
					(int)FwTextPropVar.ktpvMilliPoint, 12000);
				vwenv.OpenDiv();
				vwenv.AddLazyVecItems((int)SampleTags.ktagTextParas, this, (int)SampleFrags.kfrPara);
				vwenv.CloseDiv();
				break;
			case (int)SampleFrags.kfrPara:
				vwenv.AddStringProp((int)SampleTags.ktagParaContents, this);
				break;
			}
		}
		/// <summary></summary>
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			// About 3 lines (in points). One line is probably more typical, but it
			// is better to guess high (which causes extra steps in the expansion process)
			// than to guess low (and expand stuff we don't need).
			return 39;
		}

		/// <summary></summary>
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent, int tag, int frag, int ihvoMin)
		{
			// All our data is preloaded, there is nothing to do.
		}
	}

	/// <summary></summary>
	public class SampleView : SimpleRootSite
	{
		string m_fontName;
		SampleVc m_svc;
		int m_hvoRoot = 1;
		const int khvoFirstPara = 1000; // Arbitrarily, paragraphs are numbered from 1000.
		int m_hvoNextPara = khvoFirstPara;
		ISilDataAccess m_sda;
		IVwCacheDa m_cd;

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_cd != null)
				{
					m_cd.ClearAllData();
					if (Marshal.IsComObject(m_cd))
						Marshal.ReleaseComObject(m_cd);
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_svc = null;
			m_sda = null;
			m_cd = null;
		}

		#endregion IDisposable override

		/// <summary></summary>
		public string FontName
		{
			get
			{
				CheckDisposed();
				return m_fontName;
			}
			set
			{
				CheckDisposed();

				m_fontName = value;
				m_svc.FontName = value;
				if (Enabled)
					RootBox.Reconstruct();
			}
		}

		/// <summary></summary>
		public int HvoRoot
		{
			get
			{
				CheckDisposed();
				return m_hvoRoot;
			}
			set
			{
				CheckDisposed();
				m_hvoRoot = value;
			}
		}

		/// <summary>
		/// Clear all text. If fReconstruct is true, also clears the display.
		/// </summary>
		public void Clear(bool fReconstruct)
		{
			CheckDisposed();

			if (m_sda == null)
			{
				m_sda = VwCacheDaClass.Create();
				if (WritingSystemFactory != null)
					m_sda.WritingSystemFactory = WritingSystemFactory;
				m_cd = (IVwCacheDa) m_sda;
			}
			else
			{
				m_hvoNextPara = khvoFirstPara;
				m_cd.CacheVecProp(m_hvoRoot, (int)SampleTags.ktagTextParas, new int[0], 0);
				if (fReconstruct)
					RootBox.Reconstruct();
			}
		}

		/// <summary>
		/// Add a new string as the next line of the text. Does not display.
		/// Call Complete() to update after adding all lines.
		/// </summary>
		/// <param name="tss"></param>
		public void AddPara(ITsString tss)
		{
			CheckDisposed();

			int hvoPara = m_hvoNextPara++;
			m_cd.CacheStringProp(hvoPara, (int)SampleTags.ktagParaContents, tss);
		}

		/// <summary>
		/// Add a sample paragraph just using a string. A TsString encoded using the UI
		/// writing system is created.
		/// </summary>
		/// <param name="para"></param>
		public void AddPara(string para)
		{
			CheckDisposed();

			ITsStrFactory tsf = TsStrFactoryClass.Create();
			AddPara(tsf.MakeString(para, WritingSystemFactory.UserWs));
		}

		/// <summary>
		/// Call this when done adding paragraphs. The typical calling sequence is
		/// Clear(), AddPara() x n, CompleteSetText().
		/// </summary>
		public void CompleteSetText()
		{
			CheckDisposed();

			int[] rghvo = new int[m_hvoNextPara - khvoFirstPara];
			for (int i = 0; i < rghvo.Length; ++i)
				rghvo[i] = i + khvoFirstPara;
			m_cd.CacheVecProp(m_hvoRoot, (int)SampleTags.ktagTextParas, rghvo, rghvo.Length);
			RootBox.Reconstruct();
			RootBox.MakeSimpleSel(true, true, false, true);
		}

		#region Overrides of RootSite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the root box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (!GotCacheOrWs || DesignMode)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_svc = new SampleVc();
			m_svc.FontName = m_fontName;

			Clear(false);
			m_rootb.DataAccess = m_sda;
			m_rootb.SetRootObject(m_hvoRoot, m_svc, (int)SampleFrags.kfrText, null);
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			base.MakeRoot();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle TAB key
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode == Keys.Tab)
			{
				FindForm().SelectNextControl(this, (e.Modifiers & Keys.Shift) != Keys.Shift, true, true, true);
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Escape)
			{
				FindForm().Close();
				e.Handled = true;
			}
		}
#endregion
	}
}

// Todo:
// Create the view
// Finish the DoConvert method.
// Hook up change font for output window.
// Do something about initial font selection.
