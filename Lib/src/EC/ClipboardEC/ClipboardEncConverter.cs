#define Csc30   // turn off CSC30 features

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Diagnostics;       // for Debug
using Microsoft.Win32;          // for RegistryKey
using ECInterfaces;
using SilEncConverters40;
using System.IO;                // for FileInfo
using System.Text;              // for Encoding
using System.Runtime.InteropServices;   // DllImport

#if !Csc30
using SpellingFixer30;
#else
// if we add a reference to SpellFixerEC assembly (in order to call it), then the SpellFixerEC
//  assembly must exist in the local folder. But I don't want to *require* it to have been installed.
//  If it isn't loadable, then this app fatal excepts (since my installer doesn't include the SF assembly)
//  There is a way to call SF (if installed) by using reflection. So, do that for now. If we ever decide
//  to ship ClipboardEC with a SpellFixer merge module, then we can define 'IncludeSpellFixer'
//  to get the real thing (of course, after having added a reference to it).
//  Otherwise, I've hacked a wrapper to call it and then we don't require it to be present.
#if IncludeSpellFixer
using SpellingFixerEC;          // to access SpellFixer (if adding a reference to it)
#else
using System.Reflection;        // to access SpellFixer via reflection (so it doesn't have to be present)
#endif
#endif

// put this in 'HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run' to get it to run
//  in the system tray at startup
namespace ClipboardEC
{
	/// <summary>
	/// ClipboardEncConverter: convert the text on the clipboard using one of the system converters.
	/// </summary>
	public class FormClipboardEncConverter : System.Windows.Forms.Form
	{
		public const string cstrCaption = "Clipboard EncConverter";
		public const string  cstrProjectMemoryKey = @"SOFTWARE\SIL\SilEncConverters40\ClipboardEC";
		private string  cstrProjectShowPreviewLastState = "LastShowPreviewState";
		private string  cstrProjectDebugModeLastState = "LastDebugModeState";
		public const string  cstrProjectImplTypeFilterLastState = "LastImplTypeFilterState";
		public const string  cstrProjectTransTypeFilterLastState = "LastProcessTypeFilterState";
		public const string  cstrProjectEncodingFilterLastState = "LastEncodingFilterState";
		internal const string cstrSpellFixerProgID = "SpellingFixerEC.SpellingFixerEC";
		internal const string cstrProjectEncodingFilterOffDisplayString = "Show All Encoding IDs";

		private ContextMenuStrip contextMenuStripEC;
		private System.Windows.Forms.NotifyIcon notifyIconClipboardEC;
		private System.Windows.Forms.ToolTip toolTip;
		private ProcTypeMenuItem unicodeEncodingConversionToolStripMenuItem;
		private ProcTypeMenuItem transliterationToolStripMenuItem;
		private ProcTypeMenuItem icuTransliterationToolStripMenuItem;
		private ProcTypeMenuItem icuRegularExpressionToolStripMenuItem;
		private ProcTypeMenuItem icuConverterToolStripMenuItem;
		private ProcTypeMenuItem codePageToolStripMenuItem;
		private ProcTypeMenuItem nonUnicodeEncodingConversionToolStripMenuItem;
		private ProcTypeMenuItem pythonScriptToolStripMenuItem;
		private ProcTypeMenuItem spellingFixerProjectToolStripMenuItem;
		private ProcTypeMenuItem perlExpressionToolStripMenuItem;
		private ProcTypeMenuItem spare1userdefinableToolStripMenuItem;
		private ProcTypeMenuItem spare2userdefinableToolStripMenuItem;
		private ToolStripMenuItem normalizationToolStripMenuItem;
		private ToolStripMenuItem noneToolStripMenuItem;
		private ToolStripMenuItem composedToolStripMenuItem;
		private ToolStripMenuItem decomposedToolStripMenuItem;
		private ToolStripMenuItem forwardToolStripMenuItem;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStripMenuItem previewToolStripMenuItem;
		private ToolStripMenuItem debugToolStripMenuItem;
		private ToolStripMenuItem filteringToolStripMenuItem;
		internal ToolStripMenuItem byTransductionTypeToolStripMenuItem;
		internal ToolStripMenuItem byImplementationTypeToolStripMenuItem;
		internal ToolStripMenuItem byEncodingToolStripMenuItem;
		internal ToolStripMenuItem showAllTransductionTypesToolStripMenuItem;
		private ToolStripSeparator toolStripSeparator3;
		private ToolStripMenuItem launchSILConvertersSetupToolStripMenuItem;
		private ToolStripMenuItem addConverterToolStripMenuItem;
		private ToolStripMenuItem editOrDeleteConverterToolStripMenuItem;
		private ToolStripSeparator toolStripSeparator4;
		private ToolStripMenuItem exitToolStripMenuItem;
		private ToolStripMenuItem spellFixerToolStripMenuItem;
		private ToolStripMenuItem resetToolStripMenuItem;
		private ToolStripSeparator toolStripSeparator5;
		private ToolStripMenuItem displaySpellingFixToolStripMenuItem;
		private ToolStripMenuItem editSpellingFixesToolStripMenuItem;
		private ToolStripMenuItem editDictionaryToolStripMenuItem;
		private ToolStripMenuItem selectProjectToolStripMenuItem;
		private ToolStripSeparator toolStripSeparator6;
		private ToolStripMenuItem consistentSpellingFixerToolStripMenuItem;
		private ToolStripMenuItem legacySpellFixerToolStripMenuItem;
		private ToolStripSeparator toolStripSeparator7;
		private ToolStripSeparator toolStripSeparator8;
		private System.ComponentModel.IContainer components;

		private bool _windowInitialised = false;

		[DllImport("user32", SetLastError=true)]
		static extern bool IsGUIThread(bool bConvert);

		public delegate void FakeDelegate();
		public FormClipboardEncConverter()
		{
			// from the website: http://forums.msdn.microsoft.com/en-US/netfxbcl/thread/fb267827-1765-4bd9-ae2f-0abbd5a2ae22/
			//  the following snippet is supposed to get rid of the .NET-BroadcastEventWindow fatal exception when the process
			//  is ended.
			if (!_windowInitialised && IsGUIThread(false))
			{
				Microsoft.Win32.SystemEvents.InvokeOnEventsThread(new FakeDelegate(delegate()
				{
					;   // noop
				}));
				_windowInitialised = true;
			}

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			CreateContextMenu();

			this.noneToolStripMenuItem.Click += new EventHandler(NormalizeClick);
			this.composedToolStripMenuItem.Click += new EventHandler(NormalizeClick);
			this.decomposedToolStripMenuItem.Click += new EventHandler(NormalizeClick);

			this.forwardToolStripMenuItem.Checked = true;
			this.forwardToolStripMenuItem.Click += new EventHandler(DirectionForwardClick);

			this.notifyIconClipboardEC.MouseUp += new MouseEventHandler(notifyIconClipboardEC_MouseUp);

			RegistryKey keyLastState = Registry.CurrentUser.OpenSubKey(cstrProjectMemoryKey);
			try
			{
				this.ShowPreview = ((int)keyLastState.GetValue(cstrProjectShowPreviewLastState) != 0);
			}
			catch {}
			this.previewToolStripMenuItem.Checked = ShowPreview;

			try
			{
				this.DebugState = ((int)keyLastState.GetValue(cstrProjectDebugModeLastState) != 0);
			}
			catch {}
			this.debugToolStripMenuItem.Checked = DebugState;

			try
			{
				this.ImplTypeFilter = (string)keyLastState.GetValue(cstrProjectImplTypeFilterLastState);
			}
			catch {}

			try
			{
				this.EncodingFilter = (string)keyLastState.GetValue(cstrProjectEncodingFilterLastState);
			}
			catch {}

			try
			{
				Int32 lValue = (Int32)keyLastState.GetValue(cstrProjectTransTypeFilterLastState);
				this.ProcessTypeFilter = (ProcessTypeFlags)lValue;
			}
			catch {}

			this.showAllTransductionTypesToolStripMenuItem.Checked = (this.ProcessTypeFilter == ProcessTypeFlags.DontKnow);

			this.unicodeEncodingConversionToolStripMenuItem.InitializeComponent(ProcessTypeFlags.UnicodeEncodingConversion, this);
			this.transliterationToolStripMenuItem.InitializeComponent(ProcessTypeFlags.Transliteration, this);
			this.icuTransliterationToolStripMenuItem.InitializeComponent(ProcessTypeFlags.ICUTransliteration, this);
			this.icuRegularExpressionToolStripMenuItem.InitializeComponent(ProcessTypeFlags.ICURegularExpression, this);
			this.icuConverterToolStripMenuItem.InitializeComponent(ProcessTypeFlags.ICUConverter, this);
			this.codePageToolStripMenuItem.InitializeComponent(ProcessTypeFlags.CodePageConversion, this);
			this.nonUnicodeEncodingConversionToolStripMenuItem.InitializeComponent(ProcessTypeFlags.NonUnicodeEncodingConversion, this);
			this.pythonScriptToolStripMenuItem.InitializeComponent(ProcessTypeFlags.PythonScript, this);
			this.spellingFixerProjectToolStripMenuItem.InitializeComponent(ProcessTypeFlags.SpellingFixerProject, this);
			this.perlExpressionToolStripMenuItem.InitializeComponent(ProcessTypeFlags.PerlExpression, this);
			this.spare1userdefinableToolStripMenuItem.InitializeComponent(ProcessTypeFlags.UserDefinedSpare1, this);
			this.spare2userdefinableToolStripMenuItem.InitializeComponent(ProcessTypeFlags.UserDefinedSpare2, this);

			UpdateFilteringIndication();

#if !Csc30
			this.notifyIconClipboardEC.Text = "Right-click: system converter; Left-click: Consistent Spelling";
#else
			UpdateIconText();
#endif
		}

		public void UpdateFilteringIndication()
		{
			// to avoid support problems, make it more clear when filtering is happening
			if(     this.showAllTransductionTypesToolStripMenuItem.Checked
				&&  String.IsNullOrEmpty(this.ImplTypeFilter)
				&&  String.IsNullOrEmpty(this.EncodingFilter))
			{
				this.filteringToolStripMenuItem.Text = "&Filtering";
			}
			else
			{
				this.filteringToolStripMenuItem.Text = "&Filtering (on)";
			}
		}

		public void UpdateToolTip(Control ctrl, string sTip)
		{
			toolTip.SetToolTip(ctrl, sTip);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				// this supposedly makes the icon go away on exit (otherwise, it seems to take
				//  until you move the cursor over it).
				if( this.notifyIconClipboardEC != null )
					this.notifyIconClipboardEC.Dispose();

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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormClipboardEncConverter));
			this.notifyIconClipboardEC = new System.Windows.Forms.NotifyIcon(this.components);
			this.contextMenuStripEC = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.normalizationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.decomposedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.composedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.forwardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.previewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.filteringToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.byTransductionTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showAllTransductionTypesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.unicodeEncodingConversionToolStripMenuItem = new ClipboardEC.ProcTypeMenuItem();
			this.transliterationToolStripMenuItem = new ClipboardEC.ProcTypeMenuItem();
			this.icuTransliterationToolStripMenuItem = new ClipboardEC.ProcTypeMenuItem();
			this.icuRegularExpressionToolStripMenuItem = new ClipboardEC.ProcTypeMenuItem();
			this.icuConverterToolStripMenuItem = new ClipboardEC.ProcTypeMenuItem();
			this.codePageToolStripMenuItem = new ClipboardEC.ProcTypeMenuItem();
			this.nonUnicodeEncodingConversionToolStripMenuItem = new ClipboardEC.ProcTypeMenuItem();
			this.pythonScriptToolStripMenuItem = new ClipboardEC.ProcTypeMenuItem();
			this.spellingFixerProjectToolStripMenuItem = new ClipboardEC.ProcTypeMenuItem();
			this.perlExpressionToolStripMenuItem = new ClipboardEC.ProcTypeMenuItem();
			this.spare1userdefinableToolStripMenuItem = new ClipboardEC.ProcTypeMenuItem();
			this.spare2userdefinableToolStripMenuItem = new ClipboardEC.ProcTypeMenuItem();
			this.byImplementationTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.byEncodingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.launchSILConvertersSetupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.addConverterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editOrDeleteConverterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.spellFixerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.selectProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.displaySpellingFixToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editSpellingFixesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.resetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.consistentSpellingFixerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.legacySpellFixerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.editDictionaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.contextMenuStripEC.SuspendLayout();
			this.SuspendLayout();
			//
			// notifyIconClipboardEC
			//
			this.notifyIconClipboardEC.ContextMenuStrip = this.contextMenuStripEC;
			this.notifyIconClipboardEC.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIconClipboardEC.Icon")));
			this.notifyIconClipboardEC.Text = "Loading... Please wait...";
			this.notifyIconClipboardEC.Visible = true;
			//
			// contextMenuStripEC
			//
			this.contextMenuStripEC.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripSeparator1,
			this.normalizationToolStripMenuItem,
			this.forwardToolStripMenuItem,
			this.toolStripSeparator2,
			this.previewToolStripMenuItem,
			this.debugToolStripMenuItem,
			this.filteringToolStripMenuItem,
			this.toolStripSeparator3,
			this.launchSILConvertersSetupToolStripMenuItem,
			this.addConverterToolStripMenuItem,
			this.editOrDeleteConverterToolStripMenuItem,
			this.toolStripSeparator5,
			this.spellFixerToolStripMenuItem,
			this.toolStripSeparator4,
			this.exitToolStripMenuItem});
			this.contextMenuStripEC.Name = "contextMenuStripEC";
			this.contextMenuStripEC.Size = new System.Drawing.Size(203, 276);
			this.contextMenuStripEC.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStripEC_Opening);
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(199, 6);
			//
			// normalizationToolStripMenuItem
			//
			this.normalizationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.noneToolStripMenuItem,
			this.decomposedToolStripMenuItem,
			this.composedToolStripMenuItem});
			this.normalizationToolStripMenuItem.Name = "normalizationToolStripMenuItem";
			this.normalizationToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
			this.normalizationToolStripMenuItem.Text = "&Normalization";
			this.normalizationToolStripMenuItem.ToolTipText = "Unicode Normalization Forms for the output of the conversion";
			//
			// noneToolStripMenuItem
			//
			this.noneToolStripMenuItem.Checked = true;
			this.noneToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.noneToolStripMenuItem.Name = "noneToolStripMenuItem";
			this.noneToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
			this.noneToolStripMenuItem.Text = "N&one";
			this.noneToolStripMenuItem.ToolTipText = "Output of the conversion is returned as is (no change)";
			//
			// decomposedToolStripMenuItem
			//
			this.decomposedToolStripMenuItem.Name = "decomposedToolStripMenuItem";
			this.decomposedToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
			this.decomposedToolStripMenuItem.Text = "&Decomposed";
			this.decomposedToolStripMenuItem.ToolTipText = "Output of the conversion is returned in Unicode Normalization Form Decomposed";
			//
			// composedToolStripMenuItem
			//
			this.composedToolStripMenuItem.Name = "composedToolStripMenuItem";
			this.composedToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
			this.composedToolStripMenuItem.Text = "&Composed";
			this.composedToolStripMenuItem.ToolTipText = "Output of the conversion is returned in Unicode Normalization Form Composed";
			//
			// forwardToolStripMenuItem
			//
			this.forwardToolStripMenuItem.Checked = true;
			this.forwardToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.forwardToolStripMenuItem.Name = "forwardToolStripMenuItem";
			this.forwardToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
			this.forwardToolStripMenuItem.Text = "&Forward";
			this.forwardToolStripMenuItem.ToolTipText = "Specifies the direction of the conversion (checked=Forward)";
			//
			// toolStripSeparator2
			//
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(199, 6);
			//
			// previewToolStripMenuItem
			//
			this.previewToolStripMenuItem.Name = "previewToolStripMenuItem";
			this.previewToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
			this.previewToolStripMenuItem.Text = "&Preview";
			this.previewToolStripMenuItem.ToolTipText = "Specifies whether or not to show a preview of the conversion (checked=Yes)";
			this.previewToolStripMenuItem.Click += new System.EventHandler(this.previewToolStripMenuItem_Click);
			//
			// debugToolStripMenuItem
			//
			this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
			this.debugToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
			this.debugToolStripMenuItem.Text = "&Debug";
			this.debugToolStripMenuItem.ToolTipText = "Specifies whether to display debug information sent to/received from the underlyi" +
				"ng conversion engine";
			this.debugToolStripMenuItem.Click += new System.EventHandler(this.debugToolStripMenuItem_Click);
			//
			// filteringToolStripMenuItem
			//
			this.filteringToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.byTransductionTypeToolStripMenuItem,
			this.byImplementationTypeToolStripMenuItem,
			this.byEncodingToolStripMenuItem});
			this.filteringToolStripMenuItem.Name = "filteringToolStripMenuItem";
			this.filteringToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
			this.filteringToolStripMenuItem.Text = "&Filtering";
			this.filteringToolStripMenuItem.ToolTipText = "Allows you to filter the list of converters (to reduce processing time)";
			//
			// byTransductionTypeToolStripMenuItem
			//
			this.byTransductionTypeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.showAllTransductionTypesToolStripMenuItem,
			this.unicodeEncodingConversionToolStripMenuItem,
			this.transliterationToolStripMenuItem,
			this.icuTransliterationToolStripMenuItem,
			this.icuRegularExpressionToolStripMenuItem,
			this.icuConverterToolStripMenuItem,
			this.codePageToolStripMenuItem,
			this.nonUnicodeEncodingConversionToolStripMenuItem,
			this.pythonScriptToolStripMenuItem,
			this.spellingFixerProjectToolStripMenuItem,
			this.perlExpressionToolStripMenuItem,
			this.spare1userdefinableToolStripMenuItem,
			this.spare2userdefinableToolStripMenuItem});
			this.byTransductionTypeToolStripMenuItem.Name = "byTransductionTypeToolStripMenuItem";
			this.byTransductionTypeToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.byTransductionTypeToolStripMenuItem.Text = "By &Transduction Type";
			//
			// showAllTransductionTypesToolStripMenuItem
			//
			this.showAllTransductionTypesToolStripMenuItem.Name = "showAllTransductionTypesToolStripMenuItem";
			this.showAllTransductionTypesToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.showAllTransductionTypesToolStripMenuItem.Text = "Show All Transduction Types";
			this.showAllTransductionTypesToolStripMenuItem.Click += new System.EventHandler(this.showAllTransductionTypesToolStripMenuItem_Click);
			//
			// unicodeEncodingConversionToolStripMenuItem
			//
			this.unicodeEncodingConversionToolStripMenuItem.Name = "unicodeEncodingConversionToolStripMenuItem";
			this.unicodeEncodingConversionToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.unicodeEncodingConversionToolStripMenuItem.Text = "Unicode Encoding Conversion";
			//
			// transliterationToolStripMenuItem
			//
			this.transliterationToolStripMenuItem.Name = "transliterationToolStripMenuItem";
			this.transliterationToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.transliterationToolStripMenuItem.Text = "Transliteration";
			//
			// icuTransliterationToolStripMenuItem
			//
			this.icuTransliterationToolStripMenuItem.Name = "icuTransliterationToolStripMenuItem";
			this.icuTransliterationToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.icuTransliterationToolStripMenuItem.Text = "ICU Transliteration";
			//
			// icuRegularExpressionToolStripMenuItem
			//
			this.icuRegularExpressionToolStripMenuItem.Name = "icuRegularExpressionToolStripMenuItem";
			this.icuRegularExpressionToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.icuRegularExpressionToolStripMenuItem.Text = "ICU Regular Expression";
			//
			// icuConverterToolStripMenuItem
			//
			this.icuConverterToolStripMenuItem.Name = "icuConverterToolStripMenuItem";
			this.icuConverterToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.icuConverterToolStripMenuItem.Text = "ICU Converter";
			//
			// codePageToolStripMenuItem
			//
			this.codePageToolStripMenuItem.Name = "codePageToolStripMenuItem";
			this.codePageToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.codePageToolStripMenuItem.Text = "Code Page";
			//
			// nonUnicodeEncodingConversionToolStripMenuItem
			//
			this.nonUnicodeEncodingConversionToolStripMenuItem.Name = "nonUnicodeEncodingConversionToolStripMenuItem";
			this.nonUnicodeEncodingConversionToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.nonUnicodeEncodingConversionToolStripMenuItem.Text = "Non-Unicode Encoding Conversion";
			//
			// pythonScriptToolStripMenuItem
			//
			this.pythonScriptToolStripMenuItem.Name = "pythonScriptToolStripMenuItem";
			this.pythonScriptToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.pythonScriptToolStripMenuItem.Text = "Python script";
			//
			// spellingFixerProjectToolStripMenuItem
			//
			this.spellingFixerProjectToolStripMenuItem.Name = "spellingFixerProjectToolStripMenuItem";
			this.spellingFixerProjectToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.spellingFixerProjectToolStripMenuItem.Text = "Spelling Fixer Project";
			//
			// perlExpressionToolStripMenuItem
			//
			this.perlExpressionToolStripMenuItem.Name = "perlExpressionToolStripMenuItem";
			this.perlExpressionToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.perlExpressionToolStripMenuItem.Text = "Perl Expression";
			//
			// spare1userdefinableToolStripMenuItem
			//
			this.spare1userdefinableToolStripMenuItem.Name = "spare1userdefinableToolStripMenuItem";
			this.spare1userdefinableToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.spare1userdefinableToolStripMenuItem.Text = "Spare 1 (user-definable)";
			//
			// spare2userdefinableToolStripMenuItem
			//
			this.spare2userdefinableToolStripMenuItem.Name = "spare2userdefinableToolStripMenuItem";
			this.spare2userdefinableToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.spare2userdefinableToolStripMenuItem.Text = "Spare 2 (user-definable)";
			//
			// byImplementationTypeToolStripMenuItem
			//
			this.byImplementationTypeToolStripMenuItem.Name = "byImplementationTypeToolStripMenuItem";
			this.byImplementationTypeToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.byImplementationTypeToolStripMenuItem.Text = "By &Implementation Type";
			//
			// byEncodingToolStripMenuItem
			//
			this.byEncodingToolStripMenuItem.Name = "byEncodingToolStripMenuItem";
			this.byEncodingToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.byEncodingToolStripMenuItem.Text = "By &Encoding";
			//
			// toolStripSeparator3
			//
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(199, 6);
			//
			// launchSILConvertersSetupToolStripMenuItem
			//
			this.launchSILConvertersSetupToolStripMenuItem.Name = "launchSILConvertersSetupToolStripMenuItem";
			this.launchSILConvertersSetupToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
			this.launchSILConvertersSetupToolStripMenuItem.Text = "&Launch Converter Installer";
			this.launchSILConvertersSetupToolStripMenuItem.ToolTipText = "Click here to launch the Converter Installer";
			this.launchSILConvertersSetupToolStripMenuItem.Click += new System.EventHandler(this.launchSILConvertersSetupToolStripMenuItem_Click);
			//
			// addConverterToolStripMenuItem
			//
			this.addConverterToolStripMenuItem.Name = "addConverterToolStripMenuItem";
			this.addConverterToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
			this.addConverterToolStripMenuItem.Text = "&Add Converter";
			this.addConverterToolStripMenuItem.ToolTipText = "Click here to add a new converter to the list";
			this.addConverterToolStripMenuItem.Click += new System.EventHandler(this.addConverterToolStripMenuItem_Click);
			//
			// editOrDeleteConverterToolStripMenuItem
			//
			this.editOrDeleteConverterToolStripMenuItem.Name = "editOrDeleteConverterToolStripMenuItem";
			this.editOrDeleteConverterToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
			this.editOrDeleteConverterToolStripMenuItem.Text = "Ed&it or Delete Converter";
			this.editOrDeleteConverterToolStripMenuItem.ToolTipText = "Click here to bring up the Choose Converter dialog from which you can right-click" +
				" on a converter to edit or delete it";
			this.editOrDeleteConverterToolStripMenuItem.Click += new System.EventHandler(this.editOrDeleteConverterToolStripMenuItem_Click);
			//
			// toolStripSeparator5
			//
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(199, 6);
			//
			// spellFixerToolStripMenuItem
			//
			this.spellFixerToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.selectProjectToolStripMenuItem,
			this.toolStripSeparator8,
			this.displaySpellingFixToolStripMenuItem,
			this.editSpellingFixesToolStripMenuItem,
			this.toolStripSeparator7,
			this.resetToolStripMenuItem});
			this.spellFixerToolStripMenuItem.Name = "spellFixerToolStripMenuItem";
			this.spellFixerToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
			this.spellFixerToolStripMenuItem.Text = "&Spell Fixer";
			this.spellFixerToolStripMenuItem.ToolTipText = "SpellFixer options";
			this.spellFixerToolStripMenuItem.DropDownOpening += new System.EventHandler(this.spellFixerToolStripMenuItem_DropDownOpening);
			//
			// selectProjectToolStripMenuItem
			//
			this.selectProjectToolStripMenuItem.Name = "selectProjectToolStripMenuItem";
			this.selectProjectToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.selectProjectToolStripMenuItem.Text = "&Select Project";
			this.selectProjectToolStripMenuItem.ToolTipText = "Load a SpellFixer project to work with";
			this.selectProjectToolStripMenuItem.Click += new System.EventHandler(this.selectProjectToolStripMenuItem_Click);
			//
			// toolStripSeparator8
			//
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(161, 6);
			//
			// displaySpellingFixToolStripMenuItem
			//
			this.displaySpellingFixToolStripMenuItem.Name = "displaySpellingFixToolStripMenuItem";
			this.displaySpellingFixToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.displaySpellingFixToolStripMenuItem.Text = "&Display Spelling Fix";
			this.displaySpellingFixToolStripMenuItem.ToolTipText = "Click to search the database for a spelling fix for the word on the clipboard";
			this.displaySpellingFixToolStripMenuItem.Click += new System.EventHandler(this.displaySpellingFixToolStripMenuItem_Click);
			//
			// editSpellingFixesToolStripMenuItem
			//
			this.editSpellingFixesToolStripMenuItem.Name = "editSpellingFixesToolStripMenuItem";
			this.editSpellingFixesToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.editSpellingFixesToolStripMenuItem.Text = "&Edit Spelling Fixes";
			this.editSpellingFixesToolStripMenuItem.ToolTipText = "Click to edit the spelling fix database in a grid editor";
			this.editSpellingFixesToolStripMenuItem.Click += new System.EventHandler(this.editSpellingFixesToolStripMenuItem_Click);
			//
			// toolStripSeparator7
			//
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(161, 6);
			//
			// resetToolStripMenuItem
			//
			this.resetToolStripMenuItem.Name = "resetToolStripMenuItem";
			this.resetToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.resetToolStripMenuItem.Text = "&Reset";
			this.resetToolStripMenuItem.ToolTipText = "Click to turn off SpellFixer mode";
			this.resetToolStripMenuItem.Click += new System.EventHandler(this.resetToolStripMenuItem_Click);
			//
			// toolStripSeparator4
			//
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(199, 6);
			//
			// exitToolStripMenuItem
			//
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
			this.exitToolStripMenuItem.Text = "&Exit";
			this.exitToolStripMenuItem.ToolTipText = "Click to exit the Clipboard EncConverter";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.menuItemExit_Click);
			//
			// consistentSpellingFixerToolStripMenuItem
			//
			this.consistentSpellingFixerToolStripMenuItem.Name = "consistentSpellingFixerToolStripMenuItem";
			this.consistentSpellingFixerToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
			this.consistentSpellingFixerToolStripMenuItem.Text = "&Consistent Spelling Fixer";
			this.consistentSpellingFixerToolStripMenuItem.ToolTipText = "Click to load a Consistent Spelling Fixer project (for whole word spelling fixes)" +
				"";
			//
			// legacySpellFixerToolStripMenuItem
			//
			this.legacySpellFixerToolStripMenuItem.Name = "legacySpellFixerToolStripMenuItem";
			this.legacySpellFixerToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
			this.legacySpellFixerToolStripMenuItem.Text = "&Legacy SpellFixer";
			this.legacySpellFixerToolStripMenuItem.ToolTipText = "Click to load a Legacy Spell Fixer project (supports partial word spelling change" +
				"s)";
			//
			// toolStripSeparator6
			//
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(161, 6);
			//
			// editDictionaryToolStripMenuItem
			//
			this.editDictionaryToolStripMenuItem.Name = "editDictionaryToolStripMenuItem";
			this.editDictionaryToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.editDictionaryToolStripMenuItem.Text = "Edit &Dictionary";
			this.editDictionaryToolStripMenuItem.ToolTipText = "Click to edit the list of known good spellings in a list editor";
			//
			// toolTip
			//
			this.toolTip.ShowAlways = true;
			//
			// FormClipboardEncConverter
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CausesValidation = false;
			this.ClientSize = new System.Drawing.Size(166, 253);
			this.ControlBox = false;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormClipboardEncConverter";
			this.Opacity = 0;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
			this.contextMenuStripEC.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.Run(new FormClipboardEncConverter());
		}

		private const ProcessTypeFlags  constAllProcessTypes =
			(
			ProcessTypeFlags.UnicodeEncodingConversion |
			ProcessTypeFlags.Transliteration |
			ProcessTypeFlags.ICUTransliteration	|
			ProcessTypeFlags.ICUConverter |
			ProcessTypeFlags.CodePageConversion |
			ProcessTypeFlags.NonUnicodeEncodingConversion |
			ProcessTypeFlags.SpellingFixerProject |
			ProcessTypeFlags.ICURegularExpression |
			ProcessTypeFlags.PythonScript |
			ProcessTypeFlags.PerlExpression |
			ProcessTypeFlags.UserDefinedSpare1 |
			ProcessTypeFlags.UserDefinedSpare2
			);

		private bool    m_bShowPreview = true;
		public  bool    ShowPreview
		{
			get { return m_bShowPreview; }
			set { m_bShowPreview = value; }
		}

		private bool    m_bDebugState = false;
		public  bool    DebugState
		{
			get { return m_bDebugState; }
			set { m_bDebugState = value; }
		}

		private ProcessTypeFlags m_lProcessTypeFilter = ProcessTypeFlags.DontKnow;
		public ProcessTypeFlags ProcessTypeFilter
		{
			get { return m_lProcessTypeFilter; }
			set { m_lProcessTypeFilter = value; }
		}

		private string m_strImplTypeFilter = null;
		public string ImplTypeFilter
		{
			get { return m_strImplTypeFilter; }
			set { m_strImplTypeFilter = value; }
		}

		private string m_strEncodingFilter = null;
		public string EncodingFilter
		{
			get { return m_strEncodingFilter; }
			set { m_strEncodingFilter = value; }
		}

		private DateTime        m_timeModified = DateTime.MinValue;
		private EncConverters   m_aECs = null;
		public EncConverters    GetEncConverters
		{
			get
			{
				DateTime timeModified = DateTime.MinValue;
				if(     (   (DoesFileExist(EncConverters.GetRepositoryFileName(), ref timeModified))
						&&  (timeModified > m_timeModified)
						)
					||  (m_aECs == null)
					)
				{
					m_aECs = new EncConverters();

					// keep track of the modified date, so we can detect a new version to reload
					m_timeModified = timeModified;
				}

				return m_aECs;
			}
		}

		protected bool DoesFileExist(string strFileName, ref DateTime TimeModified)
		{
			bool bRet = true;

			try
			{
				FileInfo fi = new FileInfo(strFileName);
				TimeModified = fi.LastWriteTime;
				bRet = fi.Exists;
			}
			catch
			{
				bRet = false;
			}

			return bRet;
		}

		private void contextMenuStripEC_Opening(object sender, CancelEventArgs e)
		{
			populateImplType_Opening();
			populateByEncoding_Opening();

			this.Cursor = Cursors.WaitCursor;
			try
			{
				CreateContextMenu();
			}
			catch { }
			this.Cursor = Cursors.Default;
		}

		private const int nFixedMenuItems = 15;
		private const int cnMaxPreviewLength = 30; // max chars to show for preview (so we don't go off the screen)
		static private string strInstallerLocationRegKey    = @"SOFTWARE\SIL\SilEncConverters40\Installer";
		static private string strInstallerPathKey           = "InstallerPath";

		private void CreateContextMenu()
		{
			this.Hide();

			while( this.contextMenuStripEC.Items.Count > nFixedMenuItems )
				this.contextMenuStripEC.Items.RemoveAt(0);

			// enable or disable the Launch Setup command depending on whether we can
			// find it or not
			this.launchSILConvertersSetupToolStripMenuItem.Enabled = false;
			RegistryKey keyInstallLocation = Registry.LocalMachine.OpenSubKey(strInstallerLocationRegKey);
			if( keyInstallLocation != null )
			{
				string strInstallPath = (string)keyInstallLocation.GetValue(strInstallerPathKey);
				if(!String.IsNullOrEmpty(strInstallPath) && File.Exists(strInstallPath))
					this.launchSILConvertersSetupToolStripMenuItem.Enabled = true;
			}

			// get the contents of the clipboard (using the correct code page, etc.)
			string strInput = null;
			if( ShowPreview )
			{
				IDataObject iData = Clipboard.GetDataObject();

				// Determines whether the data is in a format you can use.
				if( iData.GetDataPresent(DataFormats.UnicodeText) )
				{
					strInput = (string)iData.GetData(DataFormats.UnicodeText);
				}
			}

			// filter out the list based on the three filtering mechanisms:
			//  ProcessType (e.g. UnicodeEncodingConverter)
			//  Implementation Type (e.g. SIL.tec)
			//  Encoding ID (e.g. UNICODE)
			EncConverters aECs = GetEncConverters;
			if( (ProcessTypeFilter != constAllProcessTypes) && (ProcessTypeFilter != ProcessTypeFlags.DontKnow) )
				aECs = aECs.FilterByProcessType(ProcessTypeFilter);

			if( !String.IsNullOrEmpty(this.ImplTypeFilter) )
				aECs = aECs.FilterByImplementationType(ImplTypeFilter, ProcessTypeFilter);

			if( !String.IsNullOrEmpty(this.EncodingFilter) )
				aECs = aECs.FilterByEncodingID(this.EncodingFilter, ProcessTypeFilter);

			// sort the list so they are listed alphabetically
			SortedList aSL = new SortedList(aECs.Count);
			foreach(IEncConverter aEC in aECs.Values)
			{
				string sName = aEC.Name;
				aSL.Add(sName, sName);
			}

			for(int i = aSL.Count; i-- > 0; )
			{
				string strText = (string)aSL.GetKey(i);
				string strOutput = null;
				if( /* (aEC != null) && */ ShowPreview && !String.IsNullOrEmpty(strInput) )
				{
					try
					{
						IEncConverter aEC = aECs[strText];
						strOutput = this.ConvertData(aEC,strInput);
						// if( strOutput != null )
						//     strText += String.Format(":\t{0}", strOutput);
					}
					catch(Exception e)
					{
						// since this is just a 'preview' (and exceptions can be expected), don't
						//  allow them to be throw up.
						strOutput = String.Format("Error: {0}", e.Message);
					}
				}

				ToolStripMenuItem menuItem = new ToolStripMenuItem(strText);
				menuItem.ToolTipText = strOutput;
				if (!String.IsNullOrEmpty(strOutput) && (strOutput.Length > cnMaxPreviewLength))
					strOutput = strOutput.Substring(0, cnMaxPreviewLength) + "...";
				menuItem.ShortcutKeyDisplayString = strOutput;
				menuItem.Click += new EventHandler(ConverterClick);
				this.contextMenuStripEC.Items.Insert(0,menuItem);
			}
		}

		private void NormalizeClick(Object sender, EventArgs e)
		{
			// Determine if clicked menu item is the Blue menu item.
			if(sender == this.noneToolStripMenuItem)
			{
				// Set the checkmark for the menuItemBlue menu item.
				this.noneToolStripMenuItem.Checked = true;
				// Uncheck the menuItemRed and menuItemGreen menu items.
				this.composedToolStripMenuItem.Checked = false;
				this.decomposedToolStripMenuItem.Checked = false;
			}
			else if(sender == this.composedToolStripMenuItem)
			{
				// Set the checkmark for the menuItemBlue menu item.
				this.composedToolStripMenuItem.Checked = true;
				// Uncheck the menuItemRed and menuItemGreen menu items.
				this.noneToolStripMenuItem.Checked = false;
				this.decomposedToolStripMenuItem.Checked = false;
			}
			else
			{
				// Set the checkmark for the menuItemBlue menu item.
				this.decomposedToolStripMenuItem.Checked = true;
				// Uncheck the menuItemRed and menuItemGreen menu items.
				this.composedToolStripMenuItem.Checked = false;
				this.noneToolStripMenuItem.Checked = false;
			}
		}

		private void DirectionForwardClick(Object sender, EventArgs e)
		{
			// toggle
			this.forwardToolStripMenuItem.Checked = !this.forwardToolStripMenuItem.Checked;
		}

		private void ConverterClick(Object sender, EventArgs e)
		{
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			string strConverterName = item.Text;

			IEncConverter aEC = GetEncConverters[strConverterName];

			// now convert the contents of the clipboard (using the correct code page, etc.)
			IDataObject iData = Clipboard.GetDataObject();

			// Determines whether the data is in a format you can use.
			if( iData.GetDataPresent(DataFormats.UnicodeText) )
			{
				string strInput = (string)iData.GetData(DataFormats.UnicodeText);
				string strOutput = ConvertDataWithProps(aEC, strInput, DebugState);
				if( strOutput != null )
					Clipboard.SetDataObject(strOutput);
			}
		}

		private string ConvertDataWithProps(IEncConverter aEC, string strInput, bool bDebugState)
		{
			aEC.Debug = bDebugState;

			if( this.noneToolStripMenuItem.Checked )
				aEC.NormalizeOutput = NormalizeFlags.None;
			else if( this.composedToolStripMenuItem.Checked )
				aEC.NormalizeOutput = NormalizeFlags.FullyComposed;
			else if( this.decomposedToolStripMenuItem.Checked )
				aEC.NormalizeOutput = NormalizeFlags.FullyDecomposed;

			string strOutput = null;
			try
			{
				strOutput = ConvertData(aEC, strInput);
			}
			catch(Exception e)
			{
				// since this is just a 'preview' (and exceptions can be expected), don't
				//  allow them to be throw up.
				strOutput = "Error: " + e.Message;
			}

			aEC.Debug = false;  // so next time, we don't do debug during preview
			aEC.NormalizeOutput = NormalizeFlags.None;

			return strOutput;
		}

		private string ConvertData(IEncConverter aEC, string strInput)
		{
			bool bDirForward = this.forwardToolStripMenuItem.Checked;
			if(     !bDirForward
				&&  (   (aEC.ConversionType == ConvType.Legacy_to_Legacy)
					||  (aEC.ConversionType == ConvType.Legacy_to_Unicode)
					||  (aEC.ConversionType == ConvType.Unicode_to_Legacy)
					||  (aEC.ConversionType == ConvType.Unicode_to_Unicode)
					)
			)
			{
				// these types of converters aren't reversable! So just return null
				return null;
			}

			aEC.DirectionForward = bDirForward;

			string strOutput = null;
			if (!String.IsNullOrEmpty(strInput))
			{
				// if the input to the conversion is legacy and not encoded correctly, we have
				//  to fix that up.
				if (    (   bDirForward
						&&  (EncConverter.NormalizeLhsConversionType(aEC.ConversionType) == NormConversionType.eLegacy)
						&&  (aEC.CodePageInput != 0)
						&&  (aEC.CodePageInput != Encoding.Default.CodePage)
						)
					||  (   !bDirForward
						&&  (EncConverter.NormalizeRhsConversionType(aEC.ConversionType) == NormConversionType.eLegacy)
						&&  (aEC.CodePageOutput != 0)
						&&  (aEC.CodePageOutput != Encoding.Default.CodePage)
						)
				)
				{
					// we get the legacy data from the clipboard with Encoding 0 == CP_ACP (or the default code page
					//  for this computer), but if the CodePageInput used by EncConverters is a different code page,
					//  then this will fail.
					//  If so, then convert it to a byte array and pass that
					byte[] abyInput = Encoding.Default.GetBytes(strInput);
					strInput = ECNormalizeData.ByteArrToString(abyInput);
					EncodingForm ef = aEC.EncodingIn;
					aEC.EncodingIn = EncodingForm.LegacyBytes;
					strOutput = aEC.Convert(strInput);
					aEC.EncodingIn = ef;    // reset for later (e.g. in case the user switches directions)
				}
				else
					strOutput = aEC.Convert(strInput);

				// similarly, if the output is legacy, then if the code page used was not the same as the
				//  default code page, then we have to convert it so it'll produce the correct answer
				//  (this probably doesn't work for Legacy<>Legacy code pages)
				if (    (   bDirForward
						&&  (EncConverter.NormalizeRhsConversionType(aEC.ConversionType) == NormConversionType.eLegacy)
						&&  (aEC.CodePageOutput != 0)
						&&  (aEC.CodePageOutput != Encoding.Default.CodePage)
						)
					|| (    !bDirForward
						&&  (EncConverter.NormalizeLhsConversionType(aEC.ConversionType) == NormConversionType.eLegacy)
						&&  (aEC.CodePageInput != 0)
						&&  (aEC.CodePageInput != Encoding.Default.CodePage)
					   )
				)
				{
					int nCP = (!aEC.DirectionForward) ? aEC.CodePageInput : aEC.CodePageOutput;
					byte[] abyOutput = EncConverters.GetBytesFromEncoding(nCP, strOutput, true);
					strOutput = new string(Encoding.Default.GetChars(abyOutput));
				}
			}

			return strOutput;
		}

		private void menuItemExit_Click(object sender, System.EventArgs e)
		{
			this.Dispose(true);
			Application.Exit();
		}

		private void previewToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.ShowPreview = this.previewToolStripMenuItem.Checked = !this.previewToolStripMenuItem.Checked;

			// add that to the registry also, so we remember it.
			RegistryKey keyLastShowPreviewState = Registry.CurrentUser.CreateSubKey(cstrProjectMemoryKey);
			keyLastShowPreviewState.SetValue(cstrProjectShowPreviewLastState, (ShowPreview) ? 1 : 0);
		}

		private void debugToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.DebugState = this.debugToolStripMenuItem.Checked = !this.debugToolStripMenuItem.Checked;

			// add that to the registry also, so we remember it.
			RegistryKey keyLastDebugState = Registry.CurrentUser.CreateSubKey(cstrProjectMemoryKey);
			keyLastDebugState.SetValue(cstrProjectDebugModeLastState, (DebugState) ? 1 : 0);
		}

		private void launchSILConvertersSetupToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// launch the Setup program (short-cut to add new converters)
			RegistryKey keyInstallLocation = Registry.LocalMachine.OpenSubKey(strInstallerLocationRegKey);
			if (keyInstallLocation != null)
			{
				string strInstallPath = (string)keyInstallLocation.GetValue(strInstallerPathKey);
				if (!String.IsNullOrEmpty(strInstallPath) && File.Exists(strInstallPath))
				{
					LaunchProgram(strInstallPath, null);
					return;
				}
			}

			MessageBox.Show("Unable to Launch the Converter Installer. Reinstall.", cstrCaption);
		}

		static protected void LaunchProgram(string strProgram, string strArguments)
		{
			try
			{
				Process myProcess = new Process();

				myProcess.StartInfo.FileName = strProgram;
				myProcess.StartInfo.Arguments = strArguments;
				myProcess.Start();
			}
			catch {}    // we tried...
		}

		private void addConverterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// call the v2.2 interface to "AutoConfigure" a converter
			string strFriendlyName = null;
			EncConverters aECs = GetEncConverters;
			aECs.AutoConfigure(ConvType.Unknown, ref strFriendlyName);
		}

		private void editOrDeleteConverterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			EncConverters aECs = GetEncConverters;

			string strInput = null;
			IDataObject iData = Clipboard.GetDataObject();
			if (iData.GetDataPresent(DataFormats.UnicodeText))
				strInput = (string)iData.GetData(DataFormats.UnicodeText);

			aECs.AutoSelectWithData(strInput, null, ConvType.Unknown, "Right-click on Converter to Edit/Delete");
		}

		private void populateImplType_Opening()
		{
			// populate this menu with the implementations defined in the repository (but only once)
			if( this.byImplementationTypeToolStripMenuItem.DropDownItems.Count == 0 )
			{
				ImplTypeMenuItem aMenuItem = new ImplTypeMenuItem(null, "Show All Implementation Types", this);
				aMenuItem.Checked = (String.IsNullOrEmpty(this.ImplTypeFilter));
				// aMenuItem.DefaultItem = true;
				this.byImplementationTypeToolStripMenuItem.DropDownItems.Add(aMenuItem);

#if !DontUseRegistry
				string[] astrImplementationTypes, astrDisplayNames;
				GetEncConverters.GetImplementationDisplayNames(out astrImplementationTypes, out astrDisplayNames);
				int nNumImplementationTypes = astrImplementationTypes.Length;
				for (int i = 0; i < nNumImplementationTypes; i++)
				{
					string sImplementType = astrImplementationTypes[i];
					string strDisplayName = astrDisplayNames[i];

					aMenuItem = new ImplTypeMenuItem(sImplementType, strDisplayName, this);
					this.byImplementationTypeToolStripMenuItem.DropDownItems.Add(aMenuItem);
					aMenuItem.Checked = (sImplementType == this.ImplTypeFilter);
				}
#else
				RegistryKey keyCnvtrsSupported = Registry.LocalMachine.OpenSubKey(EncConverters.HKLM_CNVTRS_SUPPORTED);
				if( keyCnvtrsSupported != null )
				{
					foreach( string sImplementType in keyCnvtrsSupported.GetSubKeyNames() )
					{
						RegistryKey keyDisplayName = keyCnvtrsSupported.OpenSubKey(sImplementType);
						if( keyDisplayName != null )
						{
							string strDisplayName = (string)keyDisplayName.GetValue(EncConverters.strRegKeyForFriendlyName);

							if( strDisplayName == null )
								strDisplayName = sImplementType;

							aMenuItem = new ImplTypeMenuItem(sImplementType, strDisplayName, this);
							this.byImplementationTypeToolStripMenuItem.DropDownItems.Add(aMenuItem);
							aMenuItem.Checked = (sImplementType == this.ImplTypeFilter);
						}
					}
				}
#endif
			}
		}

		private void populateByEncoding_Opening()
		{
			// populate this menu with the implementations defined in the repository (but only once)
			if (this.byEncodingToolStripMenuItem.DropDownItems.Count == 0)
			{
				EncodingFilterMenuItem aMenuItem = new EncodingFilterMenuItem(cstrProjectEncodingFilterOffDisplayString, this);
				aMenuItem.Checked = (String.IsNullOrEmpty(this.EncodingFilter));
				// aMenuItem.DefaultItem = true;
				this.byEncodingToolStripMenuItem.DropDownItems.Add(aMenuItem);

				// the possible Encoding IDs comes from the repository object
				foreach(string strEncodingID in GetEncConverters.Encodings)
				{
					if( !String.IsNullOrEmpty(strEncodingID) )
					{
						aMenuItem = new EncodingFilterMenuItem(strEncodingID, this);
						this.byEncodingToolStripMenuItem.DropDownItems.Add(aMenuItem);
						aMenuItem.Checked = (strEncodingID == this.EncodingFilter);
					}
				}
			}
		}

		private void showAllTransductionTypesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			foreach (ToolStripMenuItem aMenuItem in this.byTransductionTypeToolStripMenuItem.DropDownItems)
				aMenuItem.Checked = false;
			this.showAllTransductionTypesToolStripMenuItem.Checked = true;

			this.ProcessTypeFilter = ProcessTypeFlags.DontKnow;
			UpdateFilteringIndication();

			// add that to the registry also, so we remember it.
			RegistryKey keyLastDebugState = Registry.CurrentUser.CreateSubKey(cstrProjectMemoryKey);
			keyLastDebugState.SetValue(cstrProjectTransTypeFilterLastState, (Int32)this.ProcessTypeFilter);
		}

#if !Csc30
		protected SpellingFixer30.CscProject m_aCscProject = null;
		protected SpellingFixer30.SpellingFixer m_aSpellFixerLegacy = null;

		protected bool TrySelectProject()
		{
			try
			{
				m_aCscProject = CscProject.SelectProject();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, cstrCaption);
				return false;
			}
			finally
			{
				m_aSpellFixerLegacy = null; // just in case
			}
			return true;
		}

		protected bool TryLoginProject()
		{
			try
			{
				if (m_aSpellFixerLegacy == null)
					m_aSpellFixerLegacy = new SpellingFixer30.SpellingFixer();
				m_aSpellFixerLegacy.LoginProject();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, cstrCaption);
				return false;
			}
			finally
			{
				m_aCscProject = null;   // just in case
			}
			return true;
		}

		protected void QuerySpellFixProject()
		{
			bool bCscProject = false;
			try
			{
				if (m_aSpellFixerLegacy == null)
					m_aSpellFixerLegacy = new SpellingFixer30.SpellingFixer();

				if (m_aSpellFixerLegacy.QuerySpellFixProject() == SpellFixerMode.eConsistentSpellingChecker)
				{
					bCscProject = TrySelectProject();
				}
				else
					legacySpellFixerToolStripMenuItem_Click(null, null);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, cstrCaption);
			}
			finally
			{
				if (bCscProject)
					m_aSpellFixerLegacy = null; // just in case
			}
		}

		private void DealWithLeftClick()
		{
			if (!IsCscProject && !IsSpellFixerLegacyProject)
				QuerySpellFixProject();

			// if it's now available...
			if (IsSpellFixerLegacyProject || IsCscProject)
			{
				// ... go ahead and try to convert what's on the clipboard
				IDataObject iData = Clipboard.GetDataObject();

				// Determines whether the data is in a format you can use.
				if( iData.GetDataPresent(DataFormats.UnicodeText) )
				{
					string strInput = (string)iData.GetData(DataFormats.UnicodeText);
					if (strInput.Length > 0)
					{
						try
						{
							IEncConverter aEC = null;
							if (IsSpellFixerLegacyProject)
							{
								m_aSpellFixerLegacy.AssignCorrectSpelling(strInput);
								aEC = m_aSpellFixerLegacy.SpellFixerEncConverter;
							}
							else if (IsCscProject)
							{
								m_aCscProject.AssignCorrectSpelling(strInput);
								aEC = m_aCscProject.SpellFixerEncConverter;
							}

							string strOutput = aEC.Convert(strInput);
							if (strOutput != null)
								Clipboard.SetDataObject(strOutput);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, cstrCaption);
						}
					}
				}
			}
		}

		protected bool IsCscProject
		{
			get { return (m_aCscProject != null); }
		}

		protected bool IsSpellFixerLegacyProject
		{
			get { return (m_aSpellFixerLegacy != null); }
		}

		private void resetToolStripMenuItem_Click(object sender, EventArgs e)
		{
			m_aCscProject = null;
			m_aSpellFixerLegacy = null;
		}

		private void displaySpellingFixToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// if it's now available...
			if (IsSpellFixerLegacyProject || IsCscProject)
			{
				// ... get the word on the clipboard and call the 'FindReplacementRule' method
				IDataObject iData = Clipboard.GetDataObject();

				// Determines whether the data is in a format you can use.
				if (iData.GetDataPresent(DataFormats.UnicodeText))
				{
					string strInput = (string)iData.GetData(DataFormats.UnicodeText);
					if (strInput.Length > 0)
					{
						if (IsCscProject)
							m_aCscProject.FindReplacementRule(strInput);
						else if (IsSpellFixerLegacyProject)
							m_aSpellFixerLegacy.FindReplacementRule(strInput);
					}
				}
			}
		}

		private void editSpellingFixesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// if it's now available...
			if (IsCscProject)
				m_aCscProject.EditSpellingFixes();
			else if (IsSpellFixerLegacyProject)
				m_aSpellFixerLegacy.EditSpellingFixes();
		}

		private void editDictionaryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// if it's now available...
			if (IsCscProject)
				m_aCscProject.EditDictionary();
		}

		private void spellFixerToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			bool bProjectSelected = (IsCscProject || IsSpellFixerLegacyProject);
			displaySpellingFixToolStripMenuItem.Enabled = bProjectSelected;
			editSpellingFixesToolStripMenuItem.Enabled = bProjectSelected;
			editDictionaryToolStripMenuItem.Enabled = IsCscProject;
			resetToolStripMenuItem.Enabled = bProjectSelected;
		}

		private void consistentSpellingFixerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			m_aSpellFixerLegacy = null; // just in case
			TrySelectProject();
		}

		private void legacySpellFixerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TryLoginProject();
		}

		private void selectProjectToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			consistentSpellingFixerToolStripMenuItem.Checked = IsCscProject;
			legacySpellFixerToolStripMenuItem.Checked = IsSpellFixerLegacyProject;
		}
#else
		private SpellFixerByReflection m_aSpellFixer = null;

		protected bool IsSpellFixerProject
		{
			get { return (m_aSpellFixer != null); }
		}

		private void spellFixerToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			displaySpellingFixToolStripMenuItem.Enabled =
			editSpellingFixesToolStripMenuItem.Enabled =
			resetToolStripMenuItem.Enabled = IsSpellFixerProject;
		}

		private void selectProjectToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TryLoginProject();
			UpdateIconText();
		}

		protected bool TryLoginProject()
		{
			try
			{
				if (m_aSpellFixer == null)
					m_aSpellFixer = new SpellFixerByReflection();
				m_aSpellFixer.LoginProject();

				// to avoid the annoying error from CC about Converting on an empty table...
				//  go ahead and try to convert what's on the clipboard
				string strBadWord = "incorect";
				IDataObject iData = Clipboard.GetDataObject();
				if (iData.GetDataPresent(DataFormats.UnicodeText))
					strBadWord = (string)iData.GetData(DataFormats.UnicodeText);
				m_aSpellFixer.QueryForSpellingCorrectionIfTableEmpty(strBadWord);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, cstrCaption);
				return false;
			}
			return true;
		}

		protected void UpdateIconText()
		{
			// if a spell fixer project is selected, then make that the left-click behavior
			if (IsSpellFixerProject)
			{
				this.notifyIconClipboardEC.Text = "Left-click: SpellFixer shortcut; Right-click: system converters";
				MessageBox.Show(String.Format("Now you can click on the ClipboardEncConverter icon with the left mouse button to add a spelling correction to the{0}'{1}' SpellFixer project. The right mouse button still provides normal ClipboardEncConverter functionality.",
					Environment.NewLine, m_aSpellFixer.SpellFixerEncConverterName), cstrCaption);
			}
			else
				this.notifyIconClipboardEC.Text = "Convert clipboard data with a system converter (right-click)";
		}

		private void DealWithLeftClick()
		{
			// if the SpellFixer isn't installed... then just skip this.
			if (SpellFixerByReflection.IsSpellFixerAvailable)
			{
				/* Kent thought we should turn this off... so turn it off unless the user has enabled a project
				// okay, it's installed, but the user may not realize that the 'Click' event defaults
				//  to be for SpellFixer so make sure this is what they intended.
				if (m_aSpellFixer == null)
				{
					DialogResult res = MessageBox.Show(String.Format("Turn on 'one-click SpellFixer' mode? {0}{0}[otherwise, if you were just trying to convert the text on the clipboard, then right-click the icon instead]", Environment.NewLine), cstrCaption, MessageBoxButtons.YesNoCancel);
					if (res == DialogResult.Yes)
						TryLoginProject();
				}
				*/
				// if it's now available...
				if (IsSpellFixerProject)
				{
					// ... go ahead and try to convert what's on the clipboard
					IDataObject iData = Clipboard.GetDataObject();

					// Determines whether the data is in a format you can use.
					if (iData.GetDataPresent(DataFormats.UnicodeText))
					{
						string strInput = (string)iData.GetData(DataFormats.UnicodeText);
						if (strInput.Length > 0)
						{
							m_aSpellFixer.AssignCorrectSpelling(strInput);

							// when the ACS method returns, the couplet has been (probably) added
							IEncConverter aEC = m_aSpellFixer.SpellFixerEncConverter;
							string strOutput = aEC.Convert(strInput);
							if (strOutput != null)
								Clipboard.SetDataObject(strOutput);
						}
					}
				}
			}
			/* do nothing if it isn't installed or if SpellFixer isn't enabled...
			else
			{
				MessageBox.Show("Use the right-mouse button to bring up the list of converters to choose from", cstrCaption);
			}
			*/
		}

		private void displaySpellingFixToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// if it's now available...
			if (IsSpellFixerProject)
			{
				// ... get the word on the clipboard and call the 'FindReplacementRule' method
				IDataObject iData = Clipboard.GetDataObject();

				// Determines whether the data is in a format you can use.
				if (iData.GetDataPresent(DataFormats.UnicodeText))
				{
					string strInput = (string)iData.GetData(DataFormats.UnicodeText);
					if (strInput.Length > 0)
					{
						try
						{
							m_aSpellFixer.FindReplacementRule(strInput);
						}
						catch (Exception ex)
						{
							string strError = ex.Message;
							if (ex.InnerException != null)
								strError += String.Format("{0}Cause: {1}", Environment.NewLine, ex.InnerException.Message);
							MessageBox.Show(strError, cstrCaption);
						}
					}
				}
			}
		}

		private void editSpellingFixesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// if it's now available...
			if (IsSpellFixerProject)
			{
				try
				{
					m_aSpellFixer.EditSpellingFixes();
				}
				catch (Exception ex)
				{
					string strError = ex.Message;
					if (ex.InnerException != null)
						strError += String.Format("{0}Cause: {1}", Environment.NewLine, ex.InnerException.Message);
					MessageBox.Show(strError, cstrCaption);
				}
			}
		}

		private void resetToolStripMenuItem_Click(object sender, EventArgs e)
		{
			m_aSpellFixer = null;
			UpdateIconText();
		}
#endif

		private void notifyIconClipboardEC_MouseUp(object sender, MouseEventArgs e)
		{
			if( e.Button == MouseButtons.Left )
				DealWithLeftClick();
		}
	}
}
