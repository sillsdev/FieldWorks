using System;
using System.ComponentModel;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using System.Collections.Generic;
using SILUBS.SharedScrControls;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.TE
{
	///-------------------------------------------------------------------------------
	/// <summary>
	/// ImportDialog - gather information for a data import
	/// </summary>
	///-------------------------------------------------------------------------------
	public class ImportDialog : Form, IFWDisposable
	{
		#region Member data
		// Make these static so their values will be retained
		// when/if the user opens this dialog more than once.
		static BCVRef s_StartRef;
		static BCVRef s_EndRef;

		/// <summary></summary>
		protected IScripture m_scr;
		/// <summary></summary>
		protected FwStyleSheet m_StyleSheet;
		/// <summary></summary>
		protected IScrImportSet m_importSettings;
		/// <summary></summary>
		protected ScrBookControl scrPsgFrom;
		/// <summary></summary>
		protected ScrBookControl scrPsgTo;

		private Label label2;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;
		private RadioButton radImportEntire;
		private RadioButton radImportRange;
		private CheckBox chkTranslation;
		private CheckBox chkBackTranslation;
		private CheckBox chkBookIntros;
		private Button btnOK;
		private CheckBox chkOther;
		#endregion

		#region Constructor and initialization
		///-------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor for import dialog.
		/// Don't use this constructor at run time.
		/// </summary>
		///-------------------------------------------------------------------------------
		public ImportDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		///-------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for import dialog, requiring a language project.
		/// Use this constructor at run time.
		/// </summary>
		///-------------------------------------------------------------------------------
		public ImportDialog(FwStyleSheet styleSheet, FdoCache cache, IScrImportSet settings,
			IHelpTopicProvider helpTopicProvider, IApp app) : this()
		{
			m_StyleSheet = styleSheet;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_scr = cache.LangProject.TranslatedScriptureOA;
			m_importSettings = settings;

			//InitBookNameList();

			// Set the initial values for the controls from the static variables.
			radImportEntire.Checked = ImportEntire;
			radImportRange.Checked = !ImportEntire;
			chkTranslation.Checked = ImportTranslation;
			chkBackTranslation.Checked = ImportBackTranslation;
			chkBookIntros.Checked = ImportBookIntros;
			chkOther.Checked = ImportAnnotations;

			// Restore any saved settings.
			if (s_StartRef != null)
				StartRef = s_StartRef;
			else
				SetStartRefToFirstImportableBook();

			if (s_EndRef != null)
				EndRef = s_EndRef;
			else
				SetEndRefToLastImportableBook();

			// Finish constructing the ScrBookControl objects.
			InitializeStartAndEndRefControls();
		}

		static ImportDialog()
		{
			ImportAnnotations = true;
			ImportBookIntros = true;
			ImportBackTranslation = true;
			ImportTranslation = true;
			ImportEntire = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the starting and ending references in this dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void ClearDialogReferences()
		{
			s_StartRef = null;
			s_EndRef = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a list of book names available from the import file list.
		/// </summary>
		/// <returns>the number of books available for import</returns>
		/// ------------------------------------------------------------------------------------
		private int InitBookNameList()
		{
			List<int> booksPresent = null;
			try
			{
				booksPresent = m_importSettings.BooksForProject;
			}
			catch
			{
				// TODO: Add a message to tell the user that the paratext project could
				// not be loaded.
			}
			if (booksPresent == null || booksPresent.Count == 0)
			{
				// This can probably only happen in the weird case where a Paratext project
				// that previously had books now has none.
				radImportRange.Enabled = scrPsgFrom.Enabled = scrPsgTo.Enabled = false;
				return 0;
			}
			radImportRange.Enabled = true;
			scrPsgFrom.Enabled = scrPsgTo.Enabled = radImportRange.Checked;

			MultilingScrBooks mlBook = new MultilingScrBooks((IScrProjMetaDataProvider)m_scr);
			// Get list of books in import files
			BookLabel[] bookNames = new BookLabel[booksPresent.Count];
			int iName = 0;
			foreach (int bookOrd in booksPresent)
				bookNames[iName++] = new BookLabel(mlBook.GetBookName(bookOrd), bookOrd);
			scrPsgFrom.BookLabels = bookNames;
			scrPsgTo.BookLabels = bookNames;

			return bookNames.Length;
		}
		#endregion

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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.Button btnCancel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportDialog));
			System.Windows.Forms.Button btnSource;
			System.Windows.Forms.Button btnHelp;
			System.Windows.Forms.GroupBox grpDataTypes;
			System.Windows.Forms.GroupBox grpImportWhat;
			this.chkBackTranslation = new System.Windows.Forms.CheckBox();
			this.chkBookIntros = new System.Windows.Forms.CheckBox();
			this.chkOther = new System.Windows.Forms.CheckBox();
			this.chkTranslation = new System.Windows.Forms.CheckBox();
			this.scrPsgFrom = new SILUBS.SharedScrControls.ScrBookControl();
			this.label2 = new System.Windows.Forms.Label();
			this.scrPsgTo = new SILUBS.SharedScrControls.ScrBookControl();
			this.radImportRange = new System.Windows.Forms.RadioButton();
			this.radImportEntire = new System.Windows.Forms.RadioButton();
			this.btnOK = new System.Windows.Forms.Button();
			btnCancel = new System.Windows.Forms.Button();
			btnSource = new System.Windows.Forms.Button();
			btnHelp = new System.Windows.Forms.Button();
			grpDataTypes = new System.Windows.Forms.GroupBox();
			grpImportWhat = new System.Windows.Forms.GroupBox();
			grpDataTypes.SuspendLayout();
			grpImportWhat.SuspendLayout();
			this.SuspendLayout();
			//
			// btnCancel
			//
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Name = "btnCancel";
			//
			// btnSource
			//
			resources.ApplyResources(btnSource, "btnSource");
			btnSource.Name = "btnSource";
			btnSource.Click += new System.EventHandler(this.btnSource_Click);
			//
			// btnHelp
			//
			resources.ApplyResources(btnHelp, "btnHelp");
			btnHelp.Name = "btnHelp";
			btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// grpDataTypes
			//
			grpDataTypes.Controls.Add(this.chkBackTranslation);
			grpDataTypes.Controls.Add(this.chkBookIntros);
			grpDataTypes.Controls.Add(this.chkOther);
			grpDataTypes.Controls.Add(this.chkTranslation);
			resources.ApplyResources(grpDataTypes, "grpDataTypes");
			grpDataTypes.Name = "grpDataTypes";
			grpDataTypes.TabStop = false;
			//
			// chkBackTranslation
			//
			resources.ApplyResources(this.chkBackTranslation, "chkBackTranslation");
			this.chkBackTranslation.Name = "chkBackTranslation";
			this.chkBackTranslation.CheckedChanged += new System.EventHandler(this.TypeOfDataToImportChanged);
			//
			// chkBookIntros
			//
			resources.ApplyResources(this.chkBookIntros, "chkBookIntros");
			this.chkBookIntros.Name = "chkBookIntros";
			//
			// chkOther
			//
			resources.ApplyResources(this.chkOther, "chkOther");
			this.chkOther.Name = "chkOther";
			this.chkOther.CheckedChanged += new System.EventHandler(this.TypeOfDataToImportChanged);
			//
			// chkTranslation
			//
			this.chkTranslation.Checked = true;
			this.chkTranslation.CheckState = System.Windows.Forms.CheckState.Checked;
			resources.ApplyResources(this.chkTranslation, "chkTranslation");
			this.chkTranslation.Name = "chkTranslation";
			this.chkTranslation.CheckedChanged += new System.EventHandler(this.TypeOfDataToImportChanged);
			//
			// grpImportWhat
			//
			grpImportWhat.Controls.Add(this.scrPsgFrom);
			grpImportWhat.Controls.Add(this.label2);
			grpImportWhat.Controls.Add(this.scrPsgTo);
			grpImportWhat.Controls.Add(this.radImportRange);
			grpImportWhat.Controls.Add(this.radImportEntire);
			resources.ApplyResources(grpImportWhat, "grpImportWhat");
			grpImportWhat.Name = "grpImportWhat";
			grpImportWhat.TabStop = false;
			//
			// scrPsgFrom
			//
			this.scrPsgFrom.BackColor = System.Drawing.SystemColors.Window;
			resources.ApplyResources(this.scrPsgFrom, "scrPsgFrom");
			this.scrPsgFrom.Name = "scrPsgFrom";
			this.scrPsgFrom.Reference = "textBox1";
			this.scrPsgFrom.PassageChanged += new SILUBS.SharedScrControls.ScrPassageControl.PassageChangedHandler(this.scrPsgFrom_PassageChanged);
			//
			// label2
			//
			this.label2.BackColor = System.Drawing.SystemColors.Control;
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// scrPsgTo
			//
			this.scrPsgTo.BackColor = System.Drawing.SystemColors.Window;
			resources.ApplyResources(this.scrPsgTo, "scrPsgTo");
			this.scrPsgTo.Name = "scrPsgTo";
			this.scrPsgTo.Reference = "textBox1";
			this.scrPsgTo.PassageChanged += new SILUBS.SharedScrControls.ScrPassageControl.PassageChangedHandler(this.scrPsgTo_PassageChanged);
			//
			// radImportRange
			//
			resources.ApplyResources(this.radImportRange, "radImportRange");
			this.radImportRange.Name = "radImportRange";
			this.radImportRange.CheckedChanged += new System.EventHandler(this.radImportRange_CheckedChanged);
			//
			// radImportEntire
			//
			resources.ApplyResources(this.radImportEntire, "radImportEntire");
			this.radImportEntire.Name = "radImportEntire";
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Name = "btnOK";
			//
			// ImportDialog
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = btnCancel;
			this.Controls.Add(grpImportWhat);
			this.Controls.Add(grpDataTypes);
			this.Controls.Add(btnHelp);
			this.Controls.Add(btnSource);
			this.Controls.Add(btnCancel);
			this.Controls.Add(this.btnOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ImportDialog";
			this.ShowInTaskbar = false;
			grpDataTypes.ResumeLayout(false);
			grpImportWhat.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		#region Overrides
		///-------------------------------------------------------------------------------
		/// <summary>
		/// If the user didn't choose cancel, save the controls' values.
		/// </summary>
		/// <param name="e"></param>
		///-------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			// If the scripture range is selected, then make sure both scripture references are
			// valid. Focus the incorrect one.
			if (this.DialogResult == DialogResult.OK && radImportRange.Checked)
			{
				if (!scrPsgFrom.Valid)
				{
					scrPsgFrom.Focus();
					e.Cancel = true;
					return;
				}
				if (!scrPsgTo.Valid)
				{
					scrPsgTo.Focus();
					e.Cancel = true;
					return;
				}
			}
			else if (DialogResult == DialogResult.OK && radImportEntire.Checked)
			{
				m_importSettings.StartRef = StartRef;
				m_importSettings.EndRef = EndRef;
				NonUndoableUnitOfWorkHelper.Do(ActionHandler, () => m_importSettings.SaveSettings());
			}

			base.OnClosing(e);

			if (this.DialogResult == DialogResult.OK)
			{
				ImportEntire = radImportEntire.Checked;
				ImportTranslation = chkTranslation.Checked;
				ImportBackTranslation = chkBackTranslation.Checked;
				ImportBookIntros = chkBookIntros.Checked;
				ImportAnnotations = chkOther.Checked;
				s_StartRef = scrPsgFrom.ScReference;
				s_EndRef = scrPsgTo.ScReference;
				if (m_importSettings != null)
				{
					m_importSettings.StartRef = StartRef;
					m_importSettings.EndRef = EndRef;
					m_importSettings.ImportTranslation = chkTranslation.Checked;
					m_importSettings.ImportBackTranslation = chkBackTranslation.Checked;
					m_importSettings.ImportBookIntros = chkBookIntros.Checked;
					m_importSettings.ImportAnnotations = chkOther.Checked;
				}
			}
		}

		#endregion

		#region Event handlers
		///-------------------------------------------------------------------------------
		/// <summary>
		/// The scripture passage controls are only relevant when the user wants to
		/// import a range.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		///-------------------------------------------------------------------------------
		private void radImportRange_CheckedChanged(object sender, System.EventArgs e)
		{
			scrPsgFrom.Enabled = radImportRange.Checked;
			scrPsgTo.Enabled = radImportRange.Checked;
		}

		///-------------------------------------------------------------------------------
		/// <summary>
		/// Bring up the help when the help button is clicked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		///-------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpImportDialog");
		}

		///-------------------------------------------------------------------------------
		/// <summary>
		/// When the Source button is clicked, display the import wizard.
		/// </summary>
		///-------------------------------------------------------------------------------
		private void btnSource_Click(object sender, System.EventArgs e)
		{
			using (ImportWizard importWizard = new ImportWizard(m_scr.Cache.ProjectId.Name,
				m_scr, m_StyleSheet, m_helpTopicProvider, m_app))
			{
				using (NonUndoableUnitOfWorkHelper undoHelper = new NonUndoableUnitOfWorkHelper(ActionHandler))
				{
					if (importWizard.ShowDialog() == DialogResult.Cancel)
					{
						// Ditch any in-memory changes made to the settings. Reload from the DB.
						m_importSettings.RevertToSaved();
					}
					else
						undoHelper.RollBack = false;
				}

				// If there are no files after showing the wizard, close the import dialog
				if (InitBookNameList() == 0)
				{
					MessageBox.Show(this, DlgResources.ResourceString("kstidImportFilesUnavailable"),
						m_app.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
					Close();
					return;
				}

				// The default set of settings may have changed.
				m_importSettings = m_scr.DefaultImportSettings;

				// Update the file ranges for import because they may have changed.
				InitializeStartAndEndRefControls();

				// Update the passage controls to reflect the new range of files available
				// Only make changes that do not expand the available range of books since a
				// range may have been specified before the wizard was run that we do not
				// want to overwrite
				if (!scrPsgFrom.IsReferenceValid(scrPsgFrom.ScReference))
					SetStartRefToFirstImportableBook();

				if (!scrPsgTo.IsReferenceValid(scrPsgTo.ScReference))
					SetEndRefToLastImportableBook();
			}
			btnOK.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the start and end ref controls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeStartAndEndRefControls()
		{
			scrPsgFrom.Initialize(StartRef.Book, m_importSettings.BooksForProject.ToArray());
			scrPsgTo.Initialize(EndRef.Book, m_importSettings.BooksForProject.ToArray());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the start ref to first book available for import.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetStartRefToFirstImportableBook()
		{
			StartRef = new BCVRef(FirstImportableBook, 1, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the end ref to last book available for import.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetEndRefToLastImportableBook()
		{
			EndRef = new BCVRef(LastImportableBook, 1, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a change in the scripture passage "from" control
		/// </summary>
		/// <param name="newReference">The new reference.</param>
		/// ------------------------------------------------------------------------------------
		protected void scrPsgFrom_PassageChanged(ScrReference newReference)
		{
			Logger.WriteEvent(string.Format("New scrPsgFrom reference is {0}", newReference.AsString));

			ScrReference scRefFrom = scrPsgFrom.ScReference;
			ScrReference scRefTo = scrPsgTo.ScReference;

			if (scRefFrom.Book > scRefTo.Book)
			{
				//set the scrPsgTo to the end of the book
				scRefTo.Book = scRefFrom.Book;
				scRefTo.Chapter = scRefTo.LastChapter;
				scRefTo.Verse = scRefTo.LastVerse;
				scrPsgTo.ScReference = scRefTo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a change in the scripture passage "to" control
		/// </summary>
		/// <param name="newReference">The new reference.</param>
		/// ------------------------------------------------------------------------------------
		protected void scrPsgTo_PassageChanged(ScrReference newReference)
		{
			Logger.WriteEvent(string.Format("New scrPsgTo reference is {0}", newReference.AsString));

			ScrReference scRefFrom = scrPsgFrom.ScReference;
			ScrReference scRefTo = scrPsgTo.ScReference;
			if (scRefTo.Book < scRefFrom.Book)
				scrPsgFrom.ScReference = scRefTo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a change to the import type
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void TypeOfDataToImportChanged(object sender, System.EventArgs e)
		{
			btnOK.Enabled = chkTranslation.Checked || chkBackTranslation.Checked || chkOther.Checked;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or Sets whether the import is to include the entire project (or a range of
		/// refs).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool ImportEntire { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to import the translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool ImportTranslation { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to import the back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool ImportBackTranslation { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to import book intros.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool ImportBookIntros { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to import annotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool ImportAnnotations { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the book number for the first available book in the project we're importing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int FirstImportableBook
		{
			get
			{
				if (m_importSettings != null && m_importSettings.BooksForProject.Count > 0)
					return m_importSettings.BooksForProject[0];

				return ScrReference.StartOfBible(m_scr.Versification).Book;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the book number for the last available book in the project we're importing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int LastImportableBook
		{
			get
			{
				if (m_importSettings != null && m_importSettings.BooksForProject.Count > 0)
					return m_importSettings.BooksForProject[m_importSettings.BooksForProject.Count - 1];

				return ScrReference.EndOfBible(m_scr.Versification).Book;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the action handler.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IActionHandler ActionHandler
		{
			get { return m_scr.Cache.ServiceLocator.GetInstance<IActionHandler>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the import settings (may or may not be the same settings that were passed in
		/// to the constructor).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrImportSet ImportSettings
		{
			get { CheckDisposed(); return m_importSettings; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or Sets whether the import is to include the entire project (or a range of
		/// refs).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ImportEntireProject
		{
			get
			{
				CheckDisposed();
				return radImportEntire.Checked;
			}
			set
			{
				CheckDisposed();

				radImportEntire.Checked = value;
				radImportRange.Checked = !value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The starting reference for the import
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef StartRef
		{
			get
			{
				CheckDisposed();

				return (ImportEntireProject ? new BCVRef(FirstImportableBook, 1, 1) :
					scrPsgFrom.ScReference);
			}
			set	{scrPsgFrom.ScReference = new ScrReference(value,
				m_scr.Versification);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The end reference for the import
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef EndRef
		{
			get
			{
				CheckDisposed();

				return (ImportEntireProject ? new BCVRef(LastImportableBook, 1, 1) :
					scrPsgTo.ScReference);
			}
			set {scrPsgTo.ScReference = new ScrReference(value, m_scr.Versification);}
		}
		#endregion
	}
}
