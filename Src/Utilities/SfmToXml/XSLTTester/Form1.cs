using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Diagnostics;
using Sfm2Xml;		// for converter
using Microsoft.Win32;	// registry commands
using System.IO;		// file io


namespace XSLTTester
{
//	public class FormPosition : System.Windows.Forms.Form
//	{
//	}

	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class SFMChecker : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button SFMBrowseBtn;
		private System.Windows.Forms.TextBox sfmFileName;
		private System.Windows.Forms.Button mappingFileBrowseBTN;
		private System.Windows.Forms.TextBox mappingFileName;
		private System.Windows.Forms.Button DoItPhase1;
		private System.Windows.Forms.Button phase1outputBrowseBtn;
		private System.Windows.Forms.TextBox phase1output;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button DoItPhase2;
		private System.Windows.Forms.Button DoItPhase3;
		private System.Windows.Forms.Button DoItPhase4;
		private System.Windows.Forms.Button phase2Btn;
		private System.Windows.Forms.Button phase3Btn;
		private System.Windows.Forms.Button phase4Btn;
		private System.Windows.Forms.Button phase1Btn;
		private System.Windows.Forms.Button DoItBuildPhase2BTN;
		private System.Windows.Forms.Button buildphase2Btn;
		private System.Windows.Forms.Button doAllStepsBtn;
		private System.Windows.Forms.Button workingDirBtn;
		private System.Windows.Forms.TextBox workingDir;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button button1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;


/* Enum containing attribute values for controling collation behavior.
 * Here are all the allowable values. Not every attribute can take every value. The only
 * universal value is UCOL_DEFAULT, which resets the attribute value to the predefined
 * value for that locale
 * @stable ICU 2.0
 */
		public enum UColAttributeValue
		{
			/** accepted by most attributes */
			UCOL_DEFAULT = -1,

			/** Primary collation strength */
			UCOL_PRIMARY = 0,
			/** Secondary collation strength */
			UCOL_SECONDARY = 1,
			/** Tertiary collation strength */
			UCOL_TERTIARY = 2,
			/** Default collation strength */
			UCOL_DEFAULT_STRENGTH = UCOL_TERTIARY,
			UCOL_CE_STRENGTH_LIMIT,
			/** Quaternary collation strength */
			UCOL_QUATERNARY=3,
			/** Identical collation strength */
			UCOL_IDENTICAL=15,
			UCOL_STRENGTH_LIMIT,

			/** Turn the feature off - works for UCOL_FRENCH_COLLATION,
						UCOL_CASE_LEVEL, UCOL_HIRAGANA_QUATERNARY_MODE
						& UCOL_DECOMPOSITION_MODE*/
			UCOL_OFF = 16,
			/** Turn the feature on - works for UCOL_FRENCH_COLLATION,
						UCOL_CASE_LEVEL, UCOL_HIRAGANA_QUATERNARY_MODE
						& UCOL_DECOMPOSITION_MODE*/
			UCOL_ON = 17,

			/** Valid for UCOL_ALTERNATE_HANDLING. Alternate handling will be shifted */
			UCOL_SHIFTED = 20,
			/** Valid for UCOL_ALTERNATE_HANDLING. Alternate handling will be non ignorable */
			UCOL_NON_IGNORABLE = 21,

			/** Valid for UCOL_CASE_FIRST -
						lower case sorts before upper case */
			UCOL_LOWER_FIRST = 24,
			/** upper case sorts before lower case */
			UCOL_UPPER_FIRST = 25,

			UCOL_ATTRIBUTE_VALUE_COUNT
		};
////		UColAttributeValue UCollationStrength;

//		U_STABLE UCollator* U_EXPORT2 ucol_openRules  (  const UChar *  rules,
//															 int32_t  rulesLength,
//															 UColAttributeValue  normalizationMode,
//															 UCollationStrength  strength,
//															 UParseError *  parseError,
//		UErrorCode *  status
//			)

		private const string kIcuUcDllName
#if DEBUG
			= "icuuc34d.dll";
		private System.Windows.Forms.TextBox tbTestInputFile;
		private System.Windows.Forms.TextBox tbTestOutputFile;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox tbTestXSLTFile;
		private System.Windows.Forms.Button btnTestXSLT;
		private System.Windows.Forms.Button btnTestXSLT_Results;
		private ListBox lbTests;
		private Button btnDoTests;
		private GroupBox groupBox2;
#else
			= "icuuc34.dll";
#endif
		private const string kIcuVersion = "_3_4";

////		/// <summary>get the name of an ICU code point</summary>
////		[DllImport(kIcuUcDllName, EntryPoint="ucol_openRules" + kIcuVersion,
////			 CallingConvention=CallingConvention.Cdecl)]
////		private static extern int ucol_OpenRules(
////			IntPtr rules,
////			int rulesLength,
////			UCharNameChoice nameChoice,
////			IntPtr buffer,
////			int bufferLength,
////			out UErrorCode errorCode);
////
////

////		[DllImport("gdi32.dll")]
////		static public extern bool StretchBlt(IntPtr hDCDest, int XOriginDest, int YOriginDest, int WidthDest, int HeightDest,
////			IntPtr hDCSrc,  int XOriginScr, int YOriginSrc, int WidthScr, int HeightScr, uint Rop);
////		[DllImport("gdi32.dll")]
////		static public extern IntPtr CreateCompatibleDC(IntPtr hDC);
////		[DllImport("gdi32.dll")]
////		static public extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int Width, int Heigth);
////		[DllImport("gdi32.dll")]
////		static public extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

		public SFMChecker()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//

			ShowTestNames();

#if false
			tbTestXSLTFile.Text = @"C:\SIL\Ethnologue XML Data\Phase1.xsl";
			tbTestInputFile.Text = @"C:\SIL\Ethnologue XML Data\continent-page-Americas.xml";
			tbTestOutputFile.Text = @"C:\SIL\Ethnologue XML Data\continent-page-Americas.sgml";
			string summariesFile = @"C:\SIL\Ethnologue XML Data\summaries.xml";

			string dirName = @"C:\SIL\Ethnologue XML Data\";
			string Americas = dirName + "continent-page-Americas.xml";
			string Africa = dirName + "continent-page-Africa.xml";
			string Europe = dirName + "continent-page-Europe.xml";
			string Pacific = dirName + "continent-page-Pacific.xml";
			string Asia = dirName + "continent-page-Asia.xml";

			ReadCountryCodes(@"C:\SIL\Ethnologue XML Data\Country_Code.txt");
//			PreProcessCountryFile(tbTestInputFile.Text, tbTestInputFile.Text + "OUT");
			PreProcessCountryFile(summariesFile, summariesFile + "OUT");

			PreProcessCountryFile(Americas, Americas + "OUT");
			PreProcessCountryFile(Africa, Africa + "OUT");
			PreProcessCountryFile(Europe, Europe + "OUT");
			PreProcessCountryFile(Pacific, Pacific + "OUT");
			PreProcessCountryFile(Asia, Asia + "OUT");

			DoTransform(tbTestXSLTFile.Text, Americas + "OUT", Americas + ".sgml");
			DoTransform(tbTestXSLTFile.Text, Africa + "OUT", Africa + ".sgml");
			DoTransform(tbTestXSLTFile.Text, Europe + "OUT", Europe + ".sgml");
			DoTransform(tbTestXSLTFile.Text, Pacific + "OUT", Pacific + ".sgml");
			DoTransform(tbTestXSLTFile.Text, Asia + "OUT", Asia + ".sgml");
#endif
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SFMBrowseBtn = new System.Windows.Forms.Button();
			this.sfmFileName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.mappingFileBrowseBTN = new System.Windows.Forms.Button();
			this.mappingFileName = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.DoItPhase1 = new System.Windows.Forms.Button();
			this.phase1outputBrowseBtn = new System.Windows.Forms.Button();
			this.phase1output = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.phase1Btn = new System.Windows.Forms.Button();
			this.phase2Btn = new System.Windows.Forms.Button();
			this.DoItPhase2 = new System.Windows.Forms.Button();
			this.DoItPhase3 = new System.Windows.Forms.Button();
			this.phase3Btn = new System.Windows.Forms.Button();
			this.DoItPhase4 = new System.Windows.Forms.Button();
			this.phase4Btn = new System.Windows.Forms.Button();
			this.DoItBuildPhase2BTN = new System.Windows.Forms.Button();
			this.buildphase2Btn = new System.Windows.Forms.Button();
			this.doAllStepsBtn = new System.Windows.Forms.Button();
			this.workingDirBtn = new System.Windows.Forms.Button();
			this.workingDir = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.tbTestInputFile = new System.Windows.Forms.TextBox();
			this.tbTestOutputFile = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.tbTestXSLTFile = new System.Windows.Forms.TextBox();
			this.btnTestXSLT = new System.Windows.Forms.Button();
			this.btnTestXSLT_Results = new System.Windows.Forms.Button();
			this.lbTests = new System.Windows.Forms.ListBox();
			this.btnDoTests = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			//
			// SFMBrowseBtn
			//
			this.SFMBrowseBtn.Location = new System.Drawing.Point(480, 56);
			this.SFMBrowseBtn.Name = "SFMBrowseBtn";
			this.SFMBrowseBtn.Size = new System.Drawing.Size(24, 23);
			this.SFMBrowseBtn.TabIndex = 5;
			this.SFMBrowseBtn.Text = "...";
			this.SFMBrowseBtn.Visible = false;
			//
			// sfmFileName
			//
			this.sfmFileName.Location = new System.Drawing.Point(104, 48);
			this.sfmFileName.Name = "sfmFileName";
			this.sfmFileName.Size = new System.Drawing.Size(152, 20);
			this.sfmFileName.TabIndex = 4;
			this.sfmFileName.TextChanged += new System.EventHandler(this.sfmFileName_TextChanged);
			//
			// label1
			//
			this.label1.Location = new System.Drawing.Point(48, 48);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(58, 17);
			this.label1.TabIndex = 3;
			this.label1.Text = "SFM File :";
			//
			// mappingFileBrowseBTN
			//
			this.mappingFileBrowseBTN.Location = new System.Drawing.Point(480, 80);
			this.mappingFileBrowseBTN.Name = "mappingFileBrowseBTN";
			this.mappingFileBrowseBTN.Size = new System.Drawing.Size(24, 23);
			this.mappingFileBrowseBTN.TabIndex = 8;
			this.mappingFileBrowseBTN.Text = "...";
			this.mappingFileBrowseBTN.Visible = false;
			//
			// mappingFileName
			//
			this.mappingFileName.Location = new System.Drawing.Point(104, 72);
			this.mappingFileName.Name = "mappingFileName";
			this.mappingFileName.Size = new System.Drawing.Size(152, 20);
			this.mappingFileName.TabIndex = 7;
			this.mappingFileName.TextChanged += new System.EventHandler(this.mappingFileName_TextChanged);
			//
			// label2
			//
			this.label2.Location = new System.Drawing.Point(24, 72);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(80, 17);
			this.label2.TabIndex = 6;
			this.label2.Text = "Mapping File :";
			//
			// DoItPhase1
			//
			this.DoItPhase1.BackColor = System.Drawing.SystemColors.ControlLight;
			this.DoItPhase1.Location = new System.Drawing.Point(48, 176);
			this.DoItPhase1.Name = "DoItPhase1";
			this.DoItPhase1.Size = new System.Drawing.Size(160, 24);
			this.DoItPhase1.TabIndex = 9;
			this.DoItPhase1.Text = "&1 Run Sfm to Xml utility";
			this.DoItPhase1.UseVisualStyleBackColor = false;
			this.DoItPhase1.Click += new System.EventHandler(this.DoItPhase1_Click);
			//
			// phase1outputBrowseBtn
			//
			this.phase1outputBrowseBtn.Location = new System.Drawing.Point(480, 104);
			this.phase1outputBrowseBtn.Name = "phase1outputBrowseBtn";
			this.phase1outputBrowseBtn.Size = new System.Drawing.Size(24, 23);
			this.phase1outputBrowseBtn.TabIndex = 12;
			this.phase1outputBrowseBtn.Text = "...";
			this.phase1outputBrowseBtn.Visible = false;
			//
			// phase1output
			//
			this.phase1output.Location = new System.Drawing.Point(104, 96);
			this.phase1output.Name = "phase1output";
			this.phase1output.Size = new System.Drawing.Size(152, 20);
			this.phase1output.TabIndex = 11;
			this.phase1output.TextChanged += new System.EventHandler(this.phase1output_TextChanged);
			//
			// label3
			//
			this.label3.Location = new System.Drawing.Point(24, 96);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(72, 17);
			this.label3.TabIndex = 10;
			this.label3.Text = "Ouptput File :";
			//
			// groupBox1
			//
			this.groupBox1.Location = new System.Drawing.Point(8, 8);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(456, 120);
			this.groupBox1.TabIndex = 14;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Phase One";
			//
			// phase1Btn
			//
			this.phase1Btn.BackColor = System.Drawing.SystemColors.ControlLight;
			this.phase1Btn.Location = new System.Drawing.Point(216, 176);
			this.phase1Btn.Name = "phase1Btn";
			this.phase1Btn.Size = new System.Drawing.Size(24, 24);
			this.phase1Btn.TabIndex = 37;
			this.phase1Btn.UseVisualStyleBackColor = false;
			this.phase1Btn.Click += new System.EventHandler(this.phase1Btn_Click);
			//
			// phase2Btn
			//
			this.phase2Btn.BackColor = System.Drawing.SystemColors.ControlLight;
			this.phase2Btn.Location = new System.Drawing.Point(216, 208);
			this.phase2Btn.Name = "phase2Btn";
			this.phase2Btn.Size = new System.Drawing.Size(24, 23);
			this.phase2Btn.TabIndex = 36;
			this.phase2Btn.UseVisualStyleBackColor = false;
			//
			// DoItPhase2
			//
			this.DoItPhase2.BackColor = System.Drawing.SystemColors.ControlLight;
			this.DoItPhase2.Location = new System.Drawing.Point(48, 208);
			this.DoItPhase2.Name = "DoItPhase2";
			this.DoItPhase2.Size = new System.Drawing.Size(160, 23);
			this.DoItPhase2.TabIndex = 21;
			this.DoItPhase2.Text = "&2 Run Phase 2 XSLT";
			this.DoItPhase2.UseVisualStyleBackColor = false;
			this.DoItPhase2.Click += new System.EventHandler(this.DoItPhase2_Click);
			//
			// DoItPhase3
			//
			this.DoItPhase3.BackColor = System.Drawing.SystemColors.ControlLight;
			this.DoItPhase3.Location = new System.Drawing.Point(264, 208);
			this.DoItPhase3.Name = "DoItPhase3";
			this.DoItPhase3.Size = new System.Drawing.Size(160, 23);
			this.DoItPhase3.TabIndex = 28;
			this.DoItPhase3.Text = "&3 Run Phase 3 XSLT";
			this.DoItPhase3.UseVisualStyleBackColor = false;
			this.DoItPhase3.Click += new System.EventHandler(this.DoItPhase3_Click);
			//
			// phase3Btn
			//
			this.phase3Btn.BackColor = System.Drawing.SystemColors.ControlLight;
			this.phase3Btn.Location = new System.Drawing.Point(432, 208);
			this.phase3Btn.Name = "phase3Btn";
			this.phase3Btn.Size = new System.Drawing.Size(24, 23);
			this.phase3Btn.TabIndex = 37;
			this.phase3Btn.UseVisualStyleBackColor = false;
			//
			// DoItPhase4
			//
			this.DoItPhase4.BackColor = System.Drawing.SystemColors.ControlLight;
			this.DoItPhase4.Location = new System.Drawing.Point(48, 240);
			this.DoItPhase4.Name = "DoItPhase4";
			this.DoItPhase4.Size = new System.Drawing.Size(160, 23);
			this.DoItPhase4.TabIndex = 35;
			this.DoItPhase4.Text = "&4 Run Phase 4 XSLT";
			this.DoItPhase4.UseVisualStyleBackColor = false;
			this.DoItPhase4.Click += new System.EventHandler(this.DoItPhase4_Click);
			//
			// phase4Btn
			//
			this.phase4Btn.BackColor = System.Drawing.SystemColors.ControlLight;
			this.phase4Btn.Location = new System.Drawing.Point(216, 240);
			this.phase4Btn.Name = "phase4Btn";
			this.phase4Btn.Size = new System.Drawing.Size(24, 23);
			this.phase4Btn.TabIndex = 38;
			this.phase4Btn.UseVisualStyleBackColor = false;
			//
			// DoItBuildPhase2BTN
			//
			this.DoItBuildPhase2BTN.BackColor = System.Drawing.SystemColors.ControlLight;
			this.DoItBuildPhase2BTN.Location = new System.Drawing.Point(264, 176);
			this.DoItBuildPhase2BTN.Name = "DoItBuildPhase2BTN";
			this.DoItBuildPhase2BTN.Size = new System.Drawing.Size(160, 23);
			this.DoItBuildPhase2BTN.TabIndex = 39;
			this.DoItBuildPhase2BTN.Text = "&Build Phase 2 XSLT";
			this.DoItBuildPhase2BTN.UseVisualStyleBackColor = false;
			this.DoItBuildPhase2BTN.Click += new System.EventHandler(this.DoItBuildPhase2BTN_Click);
			//
			// buildphase2Btn
			//
			this.buildphase2Btn.BackColor = System.Drawing.SystemColors.ControlLight;
			this.buildphase2Btn.Location = new System.Drawing.Point(432, 176);
			this.buildphase2Btn.Name = "buildphase2Btn";
			this.buildphase2Btn.Size = new System.Drawing.Size(24, 23);
			this.buildphase2Btn.TabIndex = 40;
			this.buildphase2Btn.UseVisualStyleBackColor = false;
			this.buildphase2Btn.Click += new System.EventHandler(this.buildphase2Btn_Click);
			//
			// doAllStepsBtn
			//
			this.doAllStepsBtn.Location = new System.Drawing.Point(48, 272);
			this.doAllStepsBtn.Name = "doAllStepsBtn";
			this.doAllStepsBtn.Size = new System.Drawing.Size(408, 40);
			this.doAllStepsBtn.TabIndex = 41;
			this.doAllStepsBtn.Text = "Do &All Steps";
			this.doAllStepsBtn.Click += new System.EventHandler(this.doAllStepsBtn_Click);
			//
			// workingDirBtn
			//
			this.workingDirBtn.Enabled = false;
			this.workingDirBtn.Location = new System.Drawing.Point(432, 24);
			this.workingDirBtn.Name = "workingDirBtn";
			this.workingDirBtn.Size = new System.Drawing.Size(24, 23);
			this.workingDirBtn.TabIndex = 44;
			this.workingDirBtn.Text = "...";
			//
			// workingDir
			//
			this.workingDir.Location = new System.Drawing.Point(104, 24);
			this.workingDir.Name = "workingDir";
			this.workingDir.Size = new System.Drawing.Size(325, 20);
			this.workingDir.TabIndex = 43;
			this.workingDir.TextChanged += new System.EventHandler(this.workingDir_TextChanged);
			//
			// label4
			//
			this.label4.Location = new System.Drawing.Point(32, 26);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(72, 17);
			this.label4.TabIndex = 42;
			this.label4.Text = "Working Dir :";
			//
			// button1
			//
			this.button1.Location = new System.Drawing.Point(8, 144);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(488, 23);
			this.button1.TabIndex = 45;
			this.button1.Text = "Test New Dialog...";
			this.button1.Visible = false;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			//
			// tbTestInputFile
			//
			this.tbTestInputFile.Location = new System.Drawing.Point(104, 638);
			this.tbTestInputFile.Name = "tbTestInputFile";
			this.tbTestInputFile.Size = new System.Drawing.Size(325, 20);
			this.tbTestInputFile.TabIndex = 46;
			//
			// tbTestOutputFile
			//
			this.tbTestOutputFile.Location = new System.Drawing.Point(104, 662);
			this.tbTestOutputFile.Name = "tbTestOutputFile";
			this.tbTestOutputFile.Size = new System.Drawing.Size(328, 20);
			this.tbTestOutputFile.TabIndex = 48;
			//
			// label5
			//
			this.label5.Location = new System.Drawing.Point(24, 662);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(72, 17);
			this.label5.TabIndex = 47;
			this.label5.Text = "Output File :";
			//
			// label6
			//
			this.label6.Location = new System.Drawing.Point(24, 638);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(72, 17);
			this.label6.TabIndex = 49;
			this.label6.Text = "Input File :";
			//
			// label7
			//
			this.label7.Location = new System.Drawing.Point(24, 614);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(72, 17);
			this.label7.TabIndex = 51;
			this.label7.Text = "XSLT File:";
			//
			// tbTestXSLTFile
			//
			this.tbTestXSLTFile.Location = new System.Drawing.Point(104, 614);
			this.tbTestXSLTFile.Name = "tbTestXSLTFile";
			this.tbTestXSLTFile.Size = new System.Drawing.Size(325, 20);
			this.tbTestXSLTFile.TabIndex = 50;
			//
			// btnTestXSLT
			//
			this.btnTestXSLT.BackColor = System.Drawing.SystemColors.ControlLight;
			this.btnTestXSLT.Location = new System.Drawing.Point(104, 686);
			this.btnTestXSLT.Name = "btnTestXSLT";
			this.btnTestXSLT.Size = new System.Drawing.Size(160, 23);
			this.btnTestXSLT.TabIndex = 52;
			this.btnTestXSLT.Text = "Test XSLT";
			this.btnTestXSLT.UseVisualStyleBackColor = false;
			this.btnTestXSLT.Click += new System.EventHandler(this.btnTestXSLT_Click);
			//
			// btnTestXSLT_Results
			//
			this.btnTestXSLT_Results.BackColor = System.Drawing.SystemColors.ControlLight;
			this.btnTestXSLT_Results.Location = new System.Drawing.Point(272, 686);
			this.btnTestXSLT_Results.Name = "btnTestXSLT_Results";
			this.btnTestXSLT_Results.Size = new System.Drawing.Size(24, 23);
			this.btnTestXSLT_Results.TabIndex = 53;
			this.btnTestXSLT_Results.UseVisualStyleBackColor = false;
			this.btnTestXSLT_Results.Click += new System.EventHandler(this.btnTestXSLT_Results_Click);
			//
			// lbTests
			//
			this.lbTests.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
			this.lbTests.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbTests.ForeColor = System.Drawing.SystemColors.ControlText;
			this.lbTests.FormattingEnabled = true;
			this.lbTests.HorizontalScrollbar = true;
			this.lbTests.Location = new System.Drawing.Point(6, 19);
			this.lbTests.Name = "lbTests";
			this.lbTests.Size = new System.Drawing.Size(482, 238);
			this.lbTests.TabIndex = 54;
			this.lbTests.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lbTests_MouseDoubleClick);
			//
			// btnDoTests
			//
			this.btnDoTests.BackColor = System.Drawing.SystemColors.Control;
			this.btnDoTests.ForeColor = System.Drawing.SystemColors.WindowText;
			this.btnDoTests.Location = new System.Drawing.Point(6, 260);
			this.btnDoTests.Name = "btnDoTests";
			this.btnDoTests.Size = new System.Drawing.Size(483, 24);
			this.btnDoTests.TabIndex = 55;
			this.btnDoTests.Text = "DoTests";
			this.btnDoTests.UseVisualStyleBackColor = false;
			this.btnDoTests.Click += new System.EventHandler(this.btnDoTests_Click);
			//
			// groupBox2
			//
			this.groupBox2.Controls.Add(this.btnDoTests);
			this.groupBox2.Controls.Add(this.lbTests);
			this.groupBox2.Location = new System.Drawing.Point(8, 318);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(495, 290);
			this.groupBox2.TabIndex = 56;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Test XSLT Import Phase files";
			//
			// DictionaryImportTester
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(512, 721);
			this.Controls.Add(this.btnTestXSLT);
			this.Controls.Add(this.btnTestXSLT_Results);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.tbTestXSLTFile);
			this.Controls.Add(this.tbTestOutputFile);
			this.Controls.Add(this.tbTestInputFile);
			this.Controls.Add(this.workingDir);
			this.Controls.Add(this.phase1output);
			this.Controls.Add(this.mappingFileName);
			this.Controls.Add(this.sfmFileName);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.workingDirBtn);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.doAllStepsBtn);
			this.Controls.Add(this.DoItBuildPhase2BTN);
			this.Controls.Add(this.buildphase2Btn);
			this.Controls.Add(this.DoItPhase4);
			this.Controls.Add(this.DoItPhase3);
			this.Controls.Add(this.DoItPhase2);
			this.Controls.Add(this.phase1outputBrowseBtn);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.mappingFileBrowseBTN);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.SFMBrowseBtn);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.phase2Btn);
			this.Controls.Add(this.phase3Btn);
			this.Controls.Add(this.phase4Btn);
			this.Controls.Add(this.DoItPhase1);
			this.Controls.Add(this.phase1Btn);
			this.Controls.Add(this.groupBox2);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.MaximumSize = new System.Drawing.Size(522, 750);
			this.Name = "DictionaryImportTester";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Dictionary Import Tester";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.DictionaryImportTester_Closing);
			this.Load += new System.EventHandler(this.DictionaryImportTester_Load);
			this.groupBox2.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion


		// Member variables
		string m_mappingFileName;
		string m_sfmFileName;
		string m_sImportFields;
		string m_phase1output;
		string m_LastmappingFileName;
		string m_LastsfmFileName;
		string m_Lastphase1output;

		string m_Phase2XSLT;
		string m_Phase3XSLT;
		string m_Phase4XSLT;
		string m_BuildPhase2XSLT;

		string m_Phase2Output;
		string m_Phase3Output;
		string m_Phase4Output;
		private string nl = System.Environment.NewLine;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
#if false
			// Initialize the WebRequest.
			System.Net.WebRequest myRequest = System.Net.WebRequest.Create("http://microsoft.com");
			System.Net.HttpWebResponse myResponse = (System.Net.HttpWebResponse)myRequest.GetResponse();
//			System.Net.WebResponse myResponse = myRequest.GetResponse();

			string tmp;
			for(int i=0; i < myResponse.Headers.Count; ++i)
				System.Diagnostics.Debug.WriteLine("<"+myResponse.Headers.Keys[i]+"> <" +myResponse.Headers[i] + ">");

			System.IO.Stream receiveStream = myResponse.GetResponseStream();
			System.Text.Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
			// Pipes the stream to a higher level stream reader with the required encoding format.
			System.IO.StreamReader readStream = new System.IO.StreamReader( receiveStream, encode );
			Char[] read = new Char[256];
			// Reads 256 characters at a time.
			int count = readStream.Read( read, 0, 256 );
			while (count > 0)
			{
				String str = new String(read, 0, count);
				System.Diagnostics.Debug.Write(str);
				count = readStream.Read(read, 0, 256);
			}

			// Close the response to free resources.
			myResponse.Close();

#endif

			Application.Run(new SFMChecker());
		}

		private void UpdateFilePaths()
		{
			string wDir = workingDir.Text;
			if (wDir.EndsWith("\\") == false)
				wDir += "\\";

			m_mappingFileName = wDir + mappingFileName.Text;
			m_sfmFileName = wDir + sfmFileName.Text;
			m_phase1output = wDir + phase1output.Text;
			m_sImportFields = wDir + "ImportFields.xml";

			m_Phase2Output = wDir + @"Phase2Output.xml";
			m_Phase3Output = wDir + @"Phase3Output.xml";
			m_Phase4Output = wDir + @"Phase4Output.xml";

			// XSLT files
			m_Phase2XSLT = wDir + "Phase2.xsl";
			m_Phase3XSLT = wDir + "Phase3.xsl";
			m_Phase4XSLT = wDir + "Phase4.xsl";
			m_BuildPhase2XSLT = wDir + "BuildPhase2XSLT.xsl";

			// Verify the xslt's are valid (compile)
			UpdateButton(DoItBuildPhase2BTN, m_BuildPhase2XSLT, false);
			UpdateButton(DoItPhase2, m_Phase2XSLT, false);
			UpdateButton(DoItPhase3, m_Phase3XSLT, false);
			UpdateButton(DoItPhase4, m_Phase4XSLT, false);
		}

		private void DictionaryImportTester_Load(object sender, System.EventArgs e)
		{
			// do some testing here - not related!
////			{
////				System.Web.Mail.MailMessage email = new System.Web.Mail.MailMessage();
////				email.From = "dan_hinton@sil.org";
////				email.To = "dan_hinton@sil.org";
////				email.Subject = "testing email";
////				email.Priority = System.Web.Mail.MailPriority.Normal;
////				email.BodyFormat = System.Web.Mail.MailFormat.Text;
////				email.Body = "Here is some text body";
////
////				System.Web.Mail.SmtpMail.SmtpServer = "smtp.sbcglobal.net";
////				System.Web.Mail.SmtpMail.Send(email);
////
////				int asdf = 1234;
////				asdf ++;
////
////			}

			{
//				"stem" // no change
//				"infix"	// removed from start and end
//				"suffix"	// remove from start
//				"prefix"	// remove from end
				string data = "-abcd-", affixMarker = "-";
				string newData = RemoveAffixMarker(data, "stem", affixMarker);
				newData = RemoveAffixMarker(data, "infix", affixMarker);
				newData = RemoveAffixMarker(data, "suffix", affixMarker);
				newData = RemoveAffixMarker(data, "prefix", affixMarker);
				newData = RemoveAffixMarker("=abcd=", "prefix", affixMarker);

			}


			{
				////// added to SfmToXmlTests now  10/6/05
				////// some more testing
				//DoTheTest comparetoKey = new DoTheTest();
				//bool a = comparetoKey.Success;
				//a = a;

			}

			// get app data
			RegistryKey appData = Application.UserAppDataRegistry;
			workingDir.Text = appData.GetValue("WorkingDir", @"C:\fw\Src\Utilities\SfmToXml\TestData") as string;
			phase1output.Text = appData.GetValue("Phase1Output",@"Phase1Output.xml") as string;
			mappingFileName.Text = appData.GetValue("MapFileName",@"yigreenmap.xml") as string;
			sfmFileName.Text = appData.GetValue("SfmFileName","yigreen.di") as string;

			Point pos = Location;
			pos.X = int.Parse((appData.GetValue("Location-X", "10") as string));
			pos.Y = int.Parse((appData.GetValue("Location-Y", "10") as string));
			Location = pos;
			//			appData.SetValue("Location-Y", pos.Y.ToString());

			// initialize the fields from the data store
			// everything is based from the working directory
////			workingDir.Text = @"C:\fw\Src\Utilities\SfmToXml\TestData";

			// values for last time buttons were pressed
			m_LastmappingFileName = "";
			m_LastsfmFileName = "";
			m_Lastphase1output = "";
////			phase1output.Text = @"Phase1Output.xml";


			// phase 1 data
////			mappingFileName.Text = @"yigreenmap.xml";	// @"TestMapping.xml";
////			sfmFileName.Text = "yigreen.di";	// "glosses.sfm";	// @"test.sfm";

			// INITIALNAME setups for testing
////			mappingFileName.Text = @"beginfieldmapping.xml";
////			sfmFileName.Text = "beginfieldtest.sfm";

			UpdateFilePaths();

			// Process flow is:
			// 1 - Sfm2Xml produces xml outputfile : phase1output.xml
			// 2 - BuildPhase2XSLT.xsl uses phase1output.xml to produce phase2xslt.xsl
			// 3 - phase2xslt.xsl uses phase1output.xml to produce phase2output.xml
			// 4 - phase3xslt.xsl uses phase2output.xml to produce phase3output.xml
			// 5 - phase4xslt.xsl uses phase3output.xml to produce phase4output.xml
			//
			// The phase4output.xml is the file that is to be imported.
			double dval = nextKeyID("en");
			dval = nextKeyID("es");
			dval = nextKeyID("en");
			dval = nextKeyID("es");
			dval = nextKeyID("aes");

			TESTING();

			CreateAMapFile();
		}

		private void CreateAMapFile()
		{
			// ****************************************************************
			// build the list of Languages to be used
			Hashtable uiLangs = new Hashtable();
			uiLangs.Add("a", new LanguageInfoUI("English", "en", "Windows1252<>Unicode", "en"));
			uiLangs.Add("t", new LanguageInfoUI("French", "fr", "Windows1252<>Unicode", "fr"));
			uiLangs.Add("b", new LanguageInfoUI("Vernacular", Sfm2Xml.STATICS.Ignore, "", ""));
			uiLangs.Add("c", new LanguageInfoUI("Regional", Sfm2Xml.STATICS.Ignore, "Windows1252<>Unicode", ""));
			uiLangs.Add("d", new LanguageInfoUI("National", Sfm2Xml.STATICS.Ignore, "Windows1252<>Unicode", ""));

			// ****************************************************************
			// read in the XML ref file that has all the defaykt Hierarchy info
			ILexImportFields lfields = new LexImportFields();
			lfields.ReadLexImportFields(@"C:\fw\DistFiles\Language Explorer\Import\ImportFields.xml");

			// ****************************************************************
			// new way for passing in the information that is put out in the Field Descriptions section
			List<FieldHierarchyInfo> sfmInfo = new List<FieldHierarchyInfo>();
			sfmInfo.Add(new FieldHierarchyInfo("lx", "lex", "English", true));	// non-autoimport flavor
			sfmInfo.Add(new FieldHierarchyInfo("autoSfm", "English"));			// constructor for autoimport flavor
			sfmInfo.Add(new FieldHierarchyInfo("lc", "scit", "English", true));	// non-autoimport flavor
			sfmInfo.Add(new FieldHierarchyInfo("cf", "scref", "English", false));	// non-autoimport flavor
			sfmInfo.Add(new FieldHierarchyInfo("autoSfm2", "English"));			// constructor for autoimport flavor

			// ****************************************************************
			// build the list of infield markers here
			List<ClsInFieldMarker> ifMarker = new List<ClsInFieldMarker>();
			ifMarker.Add(new ClsInFieldMarker("beg", "end", false, false, "English", "xmlLang", "style", false));
			ifMarker.Add(new ClsInFieldMarker("|x", "*|x", false, false, "", "", "KeepStyle", false));

			// ****************************************************************
			// Now bild the output: the MAP file
			STATICS.NewMapFileBuilder(uiLangs, lfields, sfmInfo, ifMarker, "C:\\TestMapFileName.map");
		}


		#region PhaseOne processing

		private void DoItPhase1_Click(object sender, System.EventArgs e)
		{
			phase1Btn.BackColor = SystemColors.Control;
			phase2Btn.BackColor = SystemColors.Control;
			phase3Btn.BackColor = SystemColors.Control;
			phase4Btn.BackColor = SystemColors.Control;
			buildphase2Btn.BackColor = SystemColors.Control;
			Update();	// allow the buttons to repaint

			// Save current info for future ref
			m_LastmappingFileName = m_mappingFileName;
			m_LastsfmFileName = m_sfmFileName;
			m_Lastphase1output = m_phase1output;

			/// need to read the map file and pull out all the auto fields and add them to the converter
			Converter conv = new Converter();

			// read in the Lex Import Fields
			LexImportFields autoFields = new LexImportFields();
			autoFields.ReadLexImportFields(m_sImportFields);
			// if there are auto fields in the xml file, pass them on to the converter
			Hashtable htAutoFields = autoFields.GetAutoFields();
			foreach (DictionaryEntry laf in htAutoFields)
			{
				string entryClass = laf.Key as String;
				LexImportField lexField = laf.Value as LexImportField;
				string fwDest = lexField.ID;
				conv.AddPossibleAutoField(entryClass, fwDest);
			}

			conv.Convert(m_sfmFileName, m_mappingFileName, m_phase1output);
			ProcessPhase1Errors(false);

			//Microsoft.XmlDiffPatch.XmlDiff diff;

			//Microsoft.XmlDiffPatch; // Does the XML comparison and outputs a diff XML file.

		}

		private void phase1Btn_Click(object sender, System.EventArgs e)
		{
			ProcessPhase1Errors(true);
		}

		#endregion

		#region Do the XSLT transforms
		private void DoItBuildPhase2BTN_Click(object sender, System.EventArgs e)
		{
			phase2Btn.BackColor = SystemColors.Control;
			phase3Btn.BackColor = SystemColors.Control;
			phase4Btn.BackColor = SystemColors.Control;
			buildphase2Btn.BackColor = SystemColors.Control;
			Update();	// allow the buttons to repaint

			DoTransform(m_BuildPhase2XSLT, m_phase1output, m_Phase2XSLT);
			buildphase2Btn.BackColor = System.Drawing.Color.Green;
		}

		private void buildphase2Btn_Click(object sender, System.EventArgs e)
		{
			phase2Btn.BackColor = SystemColors.Control;
			phase3Btn.BackColor = SystemColors.Control;
			phase4Btn.BackColor = SystemColors.Control;
			buildphase2Btn.BackColor = SystemColors.Control;
			Update();	// allow the buttons to repaint

			if (UpdateButton(DoItBuildPhase2BTN, m_BuildPhase2XSLT, true))
			{
				DoTransform(m_BuildPhase2XSLT, m_phase1output, m_Phase2XSLT);
			}
		}

		private void DoItPhase2_Click(object sender, System.EventArgs e)
		{
			phase2Btn.BackColor = SystemColors.Control;
			phase3Btn.BackColor = SystemColors.Control;
			phase4Btn.BackColor = SystemColors.Control;
			Update();	// allow the buttons to repaint

			if (UpdateButton(DoItPhase2, m_Phase2XSLT, true))
			{
				DoTransform(m_Phase2XSLT, m_phase1output, m_Phase2Output);
				phase2Btn.BackColor = System.Drawing.Color.Green;
			}
		}

		private void DoItPhase3_Click(object sender, System.EventArgs e)
		{
			phase3Btn.BackColor = SystemColors.Control;
			phase4Btn.BackColor = SystemColors.Control;
			Update();	// allow the buttons to repaint

			if (UpdateButton(DoItPhase3, m_Phase3XSLT, true))
			{
				DoTransform(m_Phase3XSLT, m_Phase2Output, m_Phase3Output);
				phase3Btn.BackColor = System.Drawing.Color.Green;
			}
		}

		private void DoItPhase4_Click(object sender, System.EventArgs e)
		{
			phase4Btn.BackColor = SystemColors.Control;
			Update();	// allow the buttons to repaint

			if (UpdateButton(DoItPhase4, m_Phase4XSLT, true))
			{
				DoTransform(m_Phase4XSLT, m_Phase3Output, m_Phase4Output);
				phase4Btn.BackColor = System.Drawing.Color.Green;
			}
		}

		#endregion



		// helper routines...
		private void DoTransform(string xsl, string xml, string output)
		{
			try
			{
#if true
#if false
			//Create the XslTransform and load the stylesheet.
			XslTransform xslt = new XslTransform();
			xslt.Load(xsl);


			//Load the XML data file.
			XPathDocument doc = new XPathDocument(xml);

			//Create an XmlTextWriter to output to the console.
			XmlTextWriter writer = new XmlTextWriter(output, null);	// System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;

			//Transform the file.
			xslt.Transform(doc, null, writer, null);
			writer.Close();
#else
			/// new way
			///
			//Create the XslTransform and load the stylesheet.
			System.Xml.Xsl.XslCompiledTransform xslt = new System.Xml.Xsl.XslCompiledTransform();
			xslt.Load(xsl, System.Xml.Xsl.XsltSettings.TrustedXslt, null);

			//Load the XML data file.
			System.Xml.XPath.XPathDocument doc = new System.Xml.XPath.XPathDocument(xml);

			//Create an XmlTextWriter to output to the console.
			System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(output, System.Text.Encoding.UTF8);
			writer.Formatting = System.Xml.Formatting.Indented;

			//Transform the file.
			xslt.Transform(doc, null, writer);
			writer.Close();

#endif
			//// SIL.Utils.XmlUtils.TransformFileToFile(xsl, xml, output);
#endif
			}
			catch(Exception ee)
			{
				System.Diagnostics.Debug.WriteLine(ee.Message);
				MessageBox.Show(this, ee.Message, ee.Source);
			}

		}

		private void doAllStepsBtn_Click(object sender, System.EventArgs e)
		{
			DoItPhase1.PerformClick();
			DoItBuildPhase2BTN.PerformClick();
			DoItPhase2.PerformClick();
			DoItPhase3.PerformClick();
			DoItPhase4.PerformClick();
		}

		private void ProcessPhase1Errors(bool show)
		{
			System.Xml.XmlDocument xmlMap = new System.Xml.XmlDocument();
			try
			{
				xmlMap.Load(m_phase1output);
			}
			catch (System.Exception e)	// .Xml.XmlException e)
			{
				MessageBox.Show(e.Message, "Phase 1 XML Error: ");
			}

			int errorCount = 0;
			String errMsg = "";

			System.Xml.XmlNode errorsNode = xmlMap.SelectSingleNode("database/ErrorLog/Errors");
			if (errorsNode != null)
			{
				foreach(System.Xml.XmlAttribute Attribute in errorsNode.Attributes)
				{
					switch (Attribute.Name)
					{
						case "count":
							errorCount = Convert.ToInt32(Attribute.Value);
							break;
					}
				}
				if (errorCount > 0)
				{
					errMsg += errorCount.ToString() + " Error(s):" + System.Environment.NewLine;
					System.Xml.XmlNodeList errorList = errorsNode.SelectNodes("Error");
					foreach (System.Xml.XmlNode errorNode in errorList)
					{
						errMsg += "   - " + errorNode.InnerText + System.Environment.NewLine;
					}
				}
			}

			int warningCount = 0;
			System.Xml.XmlNode warningsNode = xmlMap.SelectSingleNode("database/ErrorLog/Warnings");
			if (warningsNode != null)
			{
				foreach(System.Xml.XmlAttribute Attribute in warningsNode.Attributes)
				{
					switch (Attribute.Name)
					{
						case "count":
							warningCount = Convert.ToInt32(Attribute.Value);
							break;
					}
				}
				if (warningCount > 0)
				{
					errMsg += warningCount.ToString() + " Warning";
					if (warningCount > 1)
						errMsg += "s";
					errMsg += ":" + System.Environment.NewLine;
					System.Xml.XmlNodeList warningList = warningsNode.SelectNodes("Warning");
					foreach (System.Xml.XmlNode warningNode in warningList)
					{
						errMsg += "   - " + warningNode.InnerText + System.Environment.NewLine;
					}
				}
			}

			// set the result button color
			if (errorCount > 0)
				phase1Btn.BackColor = System.Drawing.Color.Red;
			else if (warningCount > 0)
				phase1Btn.BackColor = System.Drawing.Color.Yellow;
			else
				phase1Btn.BackColor = System.Drawing.Color.Green;

			// show the error and warning message if present
			if (show && (warningCount + errorCount > 0))
				MessageBox.Show(errMsg, "Phase 1 ErrorLog");
		}

		private bool UpdateButton( System.Windows.Forms.Button btn, string xsltFile, bool showMsg)
		{
			// Verify the xslt's are valid (compile)
			XslTransform xslt = new XslTransform();
			bool error = false;
			int lineNumber = 0;
			int linePosition = 0;
			string errorMsg = "";

			try
			{
				xslt.Load(xsltFile);
			}
			catch (System.Xml.Xsl.XsltCompileException bad)
			{
				error = true;
				lineNumber = bad.LineNumber;
				linePosition = bad.LinePosition;
				errorMsg = bad.Message;
			}
			catch (System.Xml.XmlException bad)
			{
				error = true;
				lineNumber = bad.LineNumber;
				linePosition = bad.LinePosition;
				errorMsg = bad.Message;
			}
			catch (Exception e)
			{
				error = true;
				errorMsg = e.Message;
			}

			if (error)
			{
				btn.BackColor = System.Drawing.Color.IndianRed;
				if (showMsg)
				{
					string msg = "Error loading: " + xsltFile + nl;
					msg += "Line : " + lineNumber + nl;
					msg += "Pos  : " + linePosition + nl;
					msg += nl;
					msg += errorMsg;
					MessageBox.Show(msg, "Error in XSLT");
				}
			}
			else
				btn.BackColor = System.Drawing.Color.LightSeaGreen;

			return error == false;
		}

		private void sfmFileName_TextChanged(object sender, System.EventArgs e)
		{
			UpdateFilePaths();
			//			m_LastmappingFileName == workingDir.Text m_mappingFileName;
//			m_LastsfmFileName = m_sfmFileName;
//			m_Lastphase1output = m_phase1output;

		}

		private void mappingFileName_TextChanged(object sender, System.EventArgs e)
		{
			UpdateFilePaths();

		}

		private void phase1output_TextChanged(object sender, System.EventArgs e)
		{
			UpdateFilePaths();

		}

		private void workingDir_TextChanged(object sender, System.EventArgs e)
		{
			UpdateFilePaths();
		}

		public static Hashtable IDvalues = new Hashtable();
		public double nextKeyID(string name)
		{
			if (!IDvalues.ContainsKey(name))
				IDvalues.Add(name, (double)0);
			double nValue = (double)IDvalues[name];
			IDvalues[name] = ++nValue;
			return nValue;
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			SIL.FieldWorks.LexText.Controls.ImportLexiconDlg dlg = new SIL.FieldWorks.LexText.Controls.ImportLexiconDlg(m_mappingFileName, m_sfmFileName);
			DialogResult result = dlg.ShowDialog(this);
		}

		private void DictionaryImportTester_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			RegistryKey appData = Application.UserAppDataRegistry;

			// save app data
			appData.SetValue("WorkingDir", workingDir.Text);
			appData.SetValue("MapFileName", mappingFileName.Text);
			appData.SetValue("SfmFileName", sfmFileName.Text);
			appData.SetValue("Phase1Output", phase1output.Text);

			Point pos = Location;
			appData.SetValue("Location-X", pos.X.ToString());
			appData.SetValue("Location-Y", pos.Y.ToString());
		}

		public string GetAffixType(string data, string affixMarker)
		{
			if (data.StartsWith(affixMarker) && data.EndsWith(affixMarker))
				return "infix";
			else if (data.StartsWith(affixMarker))
				return "suffix";
			else if (data.EndsWith(affixMarker))
				return "prefix";
			return "stem";
		}

		public string RemoveAffixMarker(string data, string affixType, string affixMarker)
		{
			if (affixType == "stem")
				return data;	// no change, do first as most common(?)
			string newData;
			if (affixType == "infix")
			{
				newData = data.TrimStart(affixMarker.ToCharArray());
				newData = newData.TrimEnd(affixMarker.ToCharArray());
			}
			else if (affixType == "suffix")
			{
				newData = data.TrimStart(affixMarker.ToCharArray());
			}
			else if (affixType == "prefix")
			{
				newData = data.TrimEnd(affixMarker.ToCharArray());
			}
			else
				return data;	// just in case

			return newData;
		}


		/// This is a place for test code.
		///
		public class FileInfo
		{
			private string m_name;
			private Int32 m_attribCRC;
			private Int32 m_dataCRC;

			public FileInfo(string name, Int32 attribCRC, Int32 dataCRC)
			{
				m_name = name;
				m_attribCRC = attribCRC;
				m_dataCRC = dataCRC;
			}
			public string Name
			{
				get { return m_name;}
			}
			public Int32 AttributeCRC
			{
				get { return m_attribCRC;}
			}
			public Int32 ContentCRC
			{
				get { return m_dataCRC;}
			}
		}

		public long GetFileContentCRC(string filename)
		{
			System.IO.FileStream reader = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
			byte[] fileData = new byte[reader.Length];
			reader.Read(fileData, 0, (int)(reader.Length));
			reader.Close();

			// now have the byte content - comput the crc
			return 0;
		}

		private void btnTestXSLT_Click(object sender, System.EventArgs e)
		{
			btnTestXSLT.BackColor = SystemColors.Control;
			Update();	// allow the buttons to repaint

			if (UpdateButton(btnTestXSLT, tbTestXSLTFile.Text, true))
			{
				DoTransform(tbTestXSLTFile.Text, tbTestInputFile.Text, tbTestOutputFile.Text);
				btnTestXSLT.BackColor = System.Drawing.Color.Green;
			}
		}

		private void btnTestXSLT_Results_Click(object sender, System.EventArgs e)
		{

		}

		public class TestResultInfo
		{
			public TestResultInfo(string msg, string finalFile, string keyFile)
			{
				m_msg = msg;
				m_finalFile = finalFile;
				m_keyFile = keyFile;
			}
			private string m_msg;
			private string m_finalFile;
			private string m_keyFile;

			public string KeyFile { get { return m_keyFile; } }
			public string FinalFile { get { return m_finalFile; } }

			public override string ToString()
			{
				return m_msg;
			}
		}

		private class CountryCodeInfo
		{
			private string m_isoCode;
			private int m_count;	// replacement useage count
			private string m_name;

			public CountryCodeInfo(string code, string name)
			{
				m_isoCode = code;
				m_name = name;
				m_count = 0;
			}
			public string ISOCode { get { return m_isoCode; }}
			public string Name { get { return m_name; }}
			public int Count { get { return m_count; }}
			public int BumpCount() { m_count++; return m_count;}

		}

		Hashtable isoCountryCodeToName = new Hashtable();	// key=string(iso), value=CountryCodeInfo
		private void ReadCountryCodes(string fileName)
		{
			StreamReader sr = new StreamReader(fileName);
			string line;
			string key, name;
			string [] parts = null;
			while ((line = sr.ReadLine()) != null)
			{
				parts = line.Split(';');
				key = parts[0].Replace("\"", "");
				name = parts[2].Replace("\"", "");
				// if name is "XYZ, SOMETHING" ==> change it to "SOMETHING XYZ"
				int pos = name.IndexOf(',');
				if (pos >= 0)
				{
					string last = name.Substring(0, pos);
					string first = name.Remove(0, pos+1);
					name = first + " " + last;
					name = name.Trim();
				}
				isoCountryCodeToName[key] = new CountryCodeInfo(key, name);
			}
			sr.Close();
		}

		private void PreProcessCountryFile(string inFileName, string outFileName)
		{
			StreamReader sr = new StreamReader(inFileName);
			string fileData = sr.ReadToEnd();
			sr.Close();

			// now replace all the country names with a link to them

			foreach (DictionaryEntry entry in isoCountryCodeToName)
			{
				CountryCodeInfo cci = entry.Value as CountryCodeInfo;
				string header = "<div class=\"country\"><h2>";
				string replaceString = "<see t=\"" + cci.ISOCode + "\">" + cci.Name + "</see>";
				int pos = fileData.IndexOf(cci.Name);
				int headerPos;
				while (pos >= 0)
				{
					headerPos = fileData.IndexOf(header, pos-header.Length-2, header.Length+4);
					bool skip = (pos > header.Length) && (headerPos >= 0);
					if (skip)	// this is the country one, insert the tag in the div element
					{
						// make sure the found country name isn't a subset of the current one,
						// to do this make sure it has the element tags around it
						if (fileData[pos-1] == '>' && fileData[pos+cci.Name.Length] == '<')
						{
							// found one to replace, replace it
							fileData = fileData.Insert(headerPos+20, " id=\"" + cci.ISOCode + "\"");
							pos = fileData.IndexOf(cci.Name, pos+cci.Name.Length-1);
						}
						else
							pos = fileData.IndexOf(cci.Name, pos+cci.Name.Length-1);

					}
					else
					{
						// make sure the found name is not a subset of a larger word
						bool goodHit = true;
						if (pos < fileData.Length)
						{
							// get the following character --- ALSO GET THE PRECEEDING CHARACTER
							char postChar = fileData[pos+cci.Name.Length];
							if (char.IsLetterOrDigit(postChar))
								goodHit = false;
						}
						// get the preceeding character
						char preChar = fileData[pos-1];
						if (char.IsLetterOrDigit(preChar))
							goodHit = false;

						if (goodHit)
						{
							cci.BumpCount();
							// found one to replace, replace it
							fileData = fileData.Remove(pos, cci.Name.Length);
							fileData = fileData.Insert(pos, replaceString);
							pos = fileData.IndexOf(cci.Name, pos+replaceString.Length-1);
						}
						else
							pos = fileData.IndexOf(cci.Name, pos+cci.Name.Length-1);

					}
				}
				System.Diagnostics.Debug.WriteLine(cci.Count + " - " + cci.ISOCode + ", " + cci.Name);
			}
			StreamWriter sw = new StreamWriter(outFileName);
			sw.Write(fileData);
			sw.Close();

		}

		// create a new GUID and return it
		public string CreateGUID()
		{
			System.Guid newGuid = Guid.NewGuid();
			return "I" + newGuid.ToString().ToUpper();
		}

		// Create a global hashtable for key - id pairs

		// Create a global hashtable for key - guid pairs
		public static Hashtable GUIDvalues = new Hashtable();


		public string GetKeyGUIDPairs()
		{
			System.Text.StringBuilder text = new System.Text.StringBuilder();
			string nl = System.Environment.NewLine;
			foreach(DictionaryEntry entry in GUIDvalues)
			{
				text.Append("<div type=\"footnote\" id=\"");
				text.Append(entry.Value);
				text.Append("\"");
				text.Append(nl);
				text.Append("<p><pict eat=\"");
				text.Append(entry.Key);
				text.Append("\"></pict></p>");
				text.Append(nl);
			}
			return text.ToString();
		}

		public double FoundGUIDKey(string name)
		{
			if (!GUIDvalues.ContainsKey(name))
				return 1;
			return 0;
		}

		// Get the guid for a given key
		public string GetKeyGUID(string name)
		{
			if (!GUIDvalues.ContainsKey(name))
				GUIDvalues.Add(name, CreateGUID());

			return (string)GUIDvalues[name];
		}

		public void ResetGUIDKeyPairs()
		{
			GUIDvalues.Clear();
		}

		public static double ID = 1000;	// this is a global ID
		public double nextID()
		{
			return ID++;
		}


		public string Trim(string data)
		{
			return data.Trim();
		}
		struct testFiles
		{
			public testFiles(string _db, string _map, string _key, string _finalFile)
			{
				db = _db;
				map = _map;
				key = _key;
				finalFile = _finalFile;
			}
			public string db;
			public string map;
			public string key;
			public string finalFile;
		};
		private void ShowTestNames()
		{
			lbTests.Items.Clear();
			string path = @"C:\fw\DistFiles\Language Explorer\Import\";
			lbTests.Items.Add(path + "lt4849.txt");
			lbTests.Items.Add(path + "dup entry tests.db");
			lbTests.Items.Add(path + "Full sample.db");
			lbTests.Items.Add(path + "Test links2.db");
			lbTests.Items.Add(path + "Test links.db");
			lbTests.Items.Add(path + "test main sub fields.db");

		}

		private void btnDoTests_Click(object sender, EventArgs e)
		{
			btnDoTests.BackColor = SystemColors.Control;
			lbTests.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));


			string path = @"C:\fw\DistFiles\Language Explorer\Import\";
			LexImportTest test = new LexImportTest(
				path + "BuildPhase2XSLT.xsl",
				path + "Phase3.xsl",
				path + "Phase4.xsl",
				path + "Phase5.xsl"
				);

			path += @"CurrentXSLT_Tests\";
			ArrayList tests = new ArrayList();
			tests.Add(new testFiles(path + "lt4849.txt", path + "lt4849-import-settings.map", path + "lt4849.key", path + "lt4849_lastRun.out"));
			tests.Add(new testFiles(path + "dup entry tests.db", path + "dup entry tests-import-settings.map", path + "dup entry tests.key", path + "dup entry tests_lastRun.out"));
			tests.Add(new testFiles(path + "Full sample.db", path + "Full sample-import-settings.map", path + "Full sample.key", path + "Full sample_lastRun.out"));
			tests.Add(new testFiles(path + "Test links2.db", path + "Test links2-import-settings.map", path + "Test links2.key.xml", path + "Test links2_lastRun.out"));
			tests.Add(new testFiles(path + "Test links.db", path + "Test links-import-settings.map", path + "Test links.key", path + "Test links_lastRun.out"));
			tests.Add(new testFiles(path + "test main sub fields.db", path + "test main sub fields-import-settings.map", path + "test main sub fields.key.xml", path + "Test main sub fields_lastRun.out"));

			lbTests.Items.Clear();	// make sure we start fresh

			bool success, allSuccess = true;
			foreach(testFiles pinfo in tests)
			{
				try
				{
					success = test.DoTest(pinfo.db, pinfo.map, pinfo.key, pinfo.finalFile);
					allSuccess &= success;
					if (success)
					{
						lbTests.Items.Add(new TestResultInfo("SUCCESS : " + pinfo.db, pinfo.finalFile, pinfo.key));
					}
					else
					{
						lbTests.Items.Add(new TestResultInfo("Different : " + pinfo.db, pinfo.finalFile, pinfo.key));
					}
				}
				catch (Exception ex)
				{
					allSuccess = false;
					lbTests.Items.Add("ERROR : " + ex.Message);
				}
			}
			lbTests.ForeColor = SystemColors.WindowText;
			if (allSuccess)
			{
				btnDoTests.BackColor = Color.MediumSeaGreen;
//				lbTests.BackColor = Color.MediumSeaGreen;
			}
			else
			{
				btnDoTests.BackColor = Color.Salmon;
			}
		}

		private void lbTests_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			TestResultInfo results = lbTests.SelectedItem as TestResultInfo;
			if (results == null)
				return;

			// create temp file with the two files to compare
			string tmpFileName = System.IO.Path.GetTempFileName();
			StreamWriter sw = new StreamWriter(tmpFileName);
			sw.Write("\"" + results.KeyFile + "\"   \"" + results.FinalFile);
			sw.Close();


			// now launch the windiff program with those files
			Process windiff = new Process();
			windiff.StartInfo.WorkingDirectory = "c:\\bin\\misctools";
			windiff.StartInfo.Arguments = " -I \"" + tmpFileName + "\"";
			windiff.StartInfo.FileName = "windiff.exe";
			windiff.StartInfo.CreateNoWindow = true;
			windiff.StartInfo.UseShellExecute = false;
			windiff.Start();
		}


		private void TESTING()
		{
			//string convertedDataString = @"24/4/98" + System.Environment.NewLine;
			//// handle any date processing here - validating and changing forms
			//try
			//{
			//    DateTime dto;
			//    bool success = DateTime.TryParse(convertedDataString, out dto);
			//    if (success)
			//    {
			//    }
			//    else
			//    {
			//        IFormatProvider ifp;
			//        success = DateTime.TryParse(convertedDataString, ifp, System.Globalization.DateTimeStyles.None, out dto);

			//    }
			//    DateTime dt = System.DateTime.Parse(convertedDataString);
			//    string newDate = dt.ToString("yyy-MM-dd hh:mm:ss.fff");
			//    convertedDataString = convertedDataString.Replace(System.Environment.NewLine, "");	// remove newlines

			//    //					if (convertedDataString.IndexOf(newDate) < 0)
			//    if (newDate.IndexOf(convertedDataString) < 0)
			//    {
			//    }
			//    convertedDataString = newDate;
			//}
			//catch
			//{
			//    convertedDataString = "";	// don't pass it on - ignore it
			//}
		}



	}

	public class DoTheTest
	{
		private bool m_success = false;
		public bool Success
		{
			get { return m_success; }
		}
		public DoTheTest()
		{
			// read the "ImportFields.xml" file and then populate the converter with autoimport fields



			string path = @"C:\fw\DistFiles\Language Explorer\Import\";
			LexImportTest test = new LexImportTest(
				path + "BuildPhase2XSLT.xsl",
				path + "Phase3.xsl",
				path + "Phase4.xsl",
				path + "Phase5.xsl");
			try
			{
				m_success = test.DoTest(path+"ken3.db", path+"ken3.map", path+"ken2_key.xml", "c:\\dotest.out");
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}
//			m_success = test.DoTest(path+"NewSfmOrderTest.db", path+"NewSfmOrderTest.map", path+"ken2_key.xml");
//			m_success = test.DoTest(path+"BamBaraDemo.db", path+"BambaraDemo-import-settings.map", path+"ken2_key.xml");
//			m_success = test.DoTest(path+"Full sample.db", path+"xxxzzzaasss.map", path+"ken2_key.xml");
//			m_success = test.DoTest(path+"testorder.db", path+"testorder-import-settings.map", path+"ken2_key.xml");
//			m_success = test.DoTest(path+"grntdi1.db", path+"grntdi1-import-settings.map", path+"ken2_key.xml");
//			m_success = test.DoTest(path+"inlineTest.db", path+"inlineTest.map", path+"ken2_key.xml");

		}
	}

	public class LexImportTest
	{
		// input members used to run the tests
		private string m_sfmName;
		private string m_mapName;
		private string m_keyName;

		// temp location to stash these away in [not needed after done]
		private string m_phase1output = @"c:\phase1output.xml";
		private string m_Phase2Output = @"c:\phase2output.xml";
		private string m_Phase3Output = @"c:\phase3output.xml";
		private string m_Phase4Output = @"c:\phase4output.xml";
		private string m_Phase5Output = @"c:\phase5output.xml";
		private string m_KeyOutput = @"c:\keyoutput.xml";
		private string m_Phase2XSLT = @"c:\phase2.xslt";

		// input reference files
		private string m_BuildPhase2XSLT;
		private string m_Phase3XSLT;
		private string m_Phase4XSLT;
		private string m_Phase5XSLT;

		/// <summary>
		///
		/// </summary>
		/// <param name="buildp2">Build Phase 2 XSLT</param>
		/// <param name="p3">Phase 3 XSLT</param>
		/// <param name="p4">Phase 4 XSLT</param>
		/// <param name="p5">Extra step to remove GUID attributes from comparison</param>
		public LexImportTest(string buildp2, string p3, string p4, string p5)
		{
			m_BuildPhase2XSLT = buildp2;
			m_Phase3XSLT = p3;
			m_Phase4XSLT = p4;
			m_Phase5XSLT = p5;
		}

		public bool DoTest(string sfmFileName, string mapFileName, string phase4KeyFileName, string locationOfFinalFile)
		{
			m_sfmName = sfmFileName;
			m_mapName = mapFileName;
			m_keyName = phase4KeyFileName;
			m_Phase5Output = locationOfFinalFile;

			Converter conv = new Converter();

			// read in the Lex Import Fields
			LexImportFields autoFields = new LexImportFields();
			autoFields.ReadLexImportFields(@"C:\fw\DistFiles\Language Explorer\Import\ImportFields.xml");
			// if there are auto fields in the xml file, pass them on to the converter
			Hashtable htAutoFields = autoFields.GetAutoFields();
			foreach (DictionaryEntry laf in htAutoFields)
			{
				string entryClass = laf.Key as String;
				LexImportField lexField = laf.Value as LexImportField;
				string fwDest = lexField.ID;
				conv.AddPossibleAutoField(entryClass, fwDest);
			}

			//// here the Auto import fields needs to be added to the converter as it is in the actual import process
			//conv.AddPossibleAutoField("Entry", "eires");
			//conv.AddPossibleAutoField("Sense", "sires");
			//conv.AddPossibleAutoField("Subentry", "seires");
			//conv.AddPossibleAutoField("Variant", "veires");

			conv.Convert(m_sfmName, m_mapName, m_phase1output);
			ProcessPhase1Errors();

			DoTransform(m_BuildPhase2XSLT, m_phase1output, m_Phase2XSLT);
			DoTransform(m_Phase2XSLT, m_phase1output, m_Phase2Output);
			DoTransform(m_Phase3XSLT, m_Phase2Output, m_Phase3Output);
#if true
			// put the phase4output in to the 'phase 5' file for comparing as there is no phase 5 now.
			DoTransform(m_Phase4XSLT, m_Phase3Output, m_Phase5Output);
			Microsoft.XmlDiffPatch.XmlDiff diff = new Microsoft.XmlDiffPatch.XmlDiff(
				Microsoft.XmlDiffPatch.XmlDiffOptions.IgnoreChildOrder |
				Microsoft.XmlDiffPatch.XmlDiffOptions.IgnoreComments |
				Microsoft.XmlDiffPatch.XmlDiffOptions.IgnoreWhitespace);
			bool same = diff.Compare(m_keyName, m_Phase5Output, false);

#else
			DoTransform(m_Phase4XSLT, m_Phase3Output, m_Phase4Output);

			// strip out the id and target attributes as the guids WILL be different
			DoTransform(m_Phase5XSLT, m_Phase4Output, m_Phase5Output);
			DoTransform(m_Phase5XSLT, m_keyName, m_KeyOutput);
			Microsoft.XmlDiffPatch.XmlDiff diff = new Microsoft.XmlDiffPatch.XmlDiff(
				Microsoft.XmlDiffPatch.XmlDiffOptions.IgnoreChildOrder |
				Microsoft.XmlDiffPatch.XmlDiffOptions.IgnoreComments |
				Microsoft.XmlDiffPatch.XmlDiffOptions.IgnoreWhitespace );
			bool same = diff.Compare(m_KeyOutput, m_Phase5Output, false);
#endif

			return same;
		}


		/// <summary>
		/// Just see if there is an error in the building of phase 1, and throw if so.
		/// </summary>
		private void ProcessPhase1Errors()
		{
			System.Xml.XmlDocument xmlMap = new System.Xml.XmlDocument();
			try
			{
				xmlMap.Load(m_phase1output);
			}
			catch (System.Exception e)	// .Xml.XmlException e)
			{
				MessageBox.Show(e.Message, "Phase 1 XML Error: ");
			}

			int errorCount = 0;
			String errMsg = "";

			System.Xml.XmlNode errorsNode = xmlMap.SelectSingleNode("database/ErrorLog/Errors");
			if (errorsNode != null)
			{
				foreach(System.Xml.XmlAttribute Attribute in errorsNode.Attributes)
				{
					switch (Attribute.Name)
					{
						case "count":
							errorCount = Convert.ToInt32(Attribute.Value);
							break;
					}
				}
				if (errorCount > 0)
				{
					errMsg += errorCount.ToString() + " Error(s):" + System.Environment.NewLine;
					System.Xml.XmlNodeList errorList = errorsNode.SelectNodes("Error");
					foreach (System.Xml.XmlNode errorNode in errorList)
					{
						errMsg += "   - " + errorNode.InnerText + System.Environment.NewLine;
					}
				}
			}

			int warningCount = 0;
			System.Xml.XmlNode warningsNode = xmlMap.SelectSingleNode("database/ErrorLog/Warnings");
			if (warningsNode != null)
			{
				foreach(System.Xml.XmlAttribute Attribute in warningsNode.Attributes)
				{
					switch (Attribute.Name)
					{
						case "count":
							warningCount = Convert.ToInt32(Attribute.Value);
							break;
					}
				}
				if (warningCount > 0)
				{
					errMsg += warningCount.ToString() + " Warning";
					if (warningCount > 1)
						errMsg += "s";
					errMsg += ":" + System.Environment.NewLine;
					System.Xml.XmlNodeList warningList = warningsNode.SelectNodes("Warning");
					foreach (System.Xml.XmlNode warningNode in warningList)
					{
						errMsg += "   - " + warningNode.InnerText + System.Environment.NewLine;
					}
				}
			}
			/*
			if (errorCount > 0)
				throw new Exception("Phase1 errors : " + m_sfmName + ": " + errMsg);
			else if (warningCount > 0)
				throw new Exception("Phase1 warnings : " + m_sfmName + ": " + errMsg);
			 * */
		}


		private void DoTransform(string xsl, string xml, string output)
		{
			try
			{
#if true
				/// new way
				///
				//Create the XslTransform and load the stylesheet.
				System.Xml.Xsl.XslCompiledTransform xslt = new System.Xml.Xsl.XslCompiledTransform();
				xslt.Load(xsl, System.Xml.Xsl.XsltSettings.TrustedXslt, null);

				//Load the XML data file.
				System.Xml.XPath.XPathDocument doc = new System.Xml.XPath.XPathDocument(xml);

				//Create an XmlTextWriter to output to the console.
				System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(output, System.Text.Encoding.UTF8);
				writer.Formatting = System.Xml.Formatting.Indented;

				//Transform the file.
				xslt.Transform(doc, null, writer);
				writer.Close();


#if false
				//Create the XslTransform and load the stylesheet.
				XslTransform xslt = new XslTransform();
				xslt.Load(xsl);

				//Load the XML data file.
				XPathDocument doc = new XPathDocument(xml);

				//Create an XmlTextWriter to output to the console.
				XmlTextWriter writer = new XmlTextWriter(output, System.Text.Encoding.UTF8);
				writer.Formatting = Formatting.Indented;

				//Transform the file.
				xslt.Transform(doc, null, writer, null);
				writer.Close();
#endif
#else
			SIL.Utils.XmlUtils.TransformFileToFile(xsl, xml, output);
#endif
			}
			catch(Exception ee)
			{
				System.Diagnostics.Debug.WriteLine(ee.Message);
			}
		}

	}

#if false
	public class FieldHierarchyInfo
	{
		private string srcMarker;	// marker in the data
		private string fwDestID;	// fw destination ID "lex", "eires", etc.
		private string fwLang;		// "English", etc
		private bool beginMarker;	// true if this is a beging marker
		private bool autoImport;	// true if this marker is auto imported based on the owning class in the data (dynamically determined)

		public FieldHierarchyInfo(string marker, string dest, string lang, bool begin)
		{
			srcMarker = marker;
			fwDestID = dest;
			fwLang = lang;
			beginMarker = begin;
			autoImport = false;
		}

		public FieldHierarchyInfo(string marker, string lang)
		{
			srcMarker = marker;
			fwLang = lang;

			autoImport = true;
			fwDestID = "Determined by location in Data";
			beginMarker = false;
		}

		public string SFM { get { return srcMarker; } }
		public string FwDestID { get { return fwDestID; } }
		public string Lang { get { return fwLang; } }
		public bool IsBegin { get { return beginMarker; } }
		public bool IsAuto { get { return autoImport; } }
	}
#endif

}
