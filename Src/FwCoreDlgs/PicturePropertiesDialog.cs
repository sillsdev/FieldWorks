// Copyright (c) 2004-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.Reporting;
using SIL.Windows.Forms.ClearShare;
using SIL.Windows.Forms.ImageToolbox;
using SIL.Windows.Forms.ImageToolbox.ImageGallery;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog for editing picture properties
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class PicturePropertiesDialog
	{
		#region Member variables
		private const string HelpTopic = "khtpPictureProperties";
		private readonly string m_rbCopyText;
		private readonly string m_rbMoveText;

		private string m_filePath;
		private readonly ICmPicture m_initialPicture;
		private FileLocationChoice m_fileLocChoice = s_defaultFileLocChoiceForSession;
		private bool m_isSaveAsChosen;

		private readonly LcmCache m_cache;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly IApp m_app;
		private readonly HelpProvider m_helpProvider;

		private static string s_sExternalLinkDestinationDir;
		private static string s_defaultPicturesFolder;
		private static FileLocationChoice s_defaultFileLocChoiceForSession = FileLocationChoice.Copy;
		#endregion

		#region Private properties and backing variables
		private Size m_imageInitialSize;

		/// <summary>True if the image has no license and the user has not selected one, either</summary>
		private bool m_isSuggestingLicense;

		/// <remarks>
		/// For some reason, when switching to the crop control from the Art Of Reading gallery chooser, metadata is marked dirty.
		/// But Palaso is smart enough not to let users edit this metadata, so it is never dirty.
		/// Request at https://github.com/sillsdev/libpalaso/issues/1268 ~Hasso, 2023.06
		/// </remarks>
		private bool m_isAOR;

		private bool IsDirty => (!m_isAOR && imageToolbox.ImageInfo.Metadata.HasChanges) || IsCropped;

		private bool IsCropped => !m_imageInitialSize.Equals(imageToolbox.ImageInfo.Image.Size);

		private bool m_imageExistsOutsideProject;

		private bool ImageExistsOutsideProject
		{
			get => m_imageExistsOutsideProject;
			set => m_imageExistsOutsideProject = m_grpFileLocOptions.Visible = value;
		}

		private bool FileFormatSupportsMetadata
		{
			get
			{
				try
				{
					return imageToolbox.ImageInfo.FileFormatSupportsMetadata;
				}
				catch
				{
					return false;
				}
			}
		}
		#endregion Private properties and backing variables

		#region Construction, initialization, and disposal
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PicturePropertiesDialog"/> class.
		/// </summary>
		/// <param name="cache">The LcmCache to use</param>
		/// <param name="initialPicture">The CmPicture object to set all of the dialog
		/// properties to, or null to edit a new picture</param>
		/// <param name="helpTopicProvider">typically IHelpTopicProvider.App</param>
		/// <param name="app">The application</param>
		/// ------------------------------------------------------------------------------------
		public PicturePropertiesDialog(LcmCache cache, ICmPicture initialPicture,
			IHelpTopicProvider helpTopicProvider, IApp app)
		{
			Guard.AgainstNull(cache, nameof(cache));

			Logger.WriteEvent("Opening 'Picture Properties' dialog");

			m_cache = cache;
			m_initialPicture = initialPicture;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;

			InitializeComponent();
			AccessibleName = GetType().Name;
			m_rbCopyText = m_rbCopy_rbSave.Text;
			m_rbMoveText = m_rbMove_rbSaveAs.Text;

			if (m_helpTopicProvider != null) // Could be null during tests
			{
				m_helpProvider = new FlexHelpProvider();
				m_helpProvider.HelpNamespace = FwDirectoryFinder.CodeDirectory +
					m_helpTopicProvider.GetHelpString("UserHelpFile");
				m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(HelpTopic));
				m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
		}

		/// <summary/>
		public void Initialize()
		{
			CheckDisposed();

			s_defaultPicturesFolder = Path.Combine(m_cache.LanguageProject.LinkedFilesRootDir, "Pictures");
			try
			{
				if (!Directory.Exists(m_cache.LanguageProject.LinkedFilesRootDir))
					Directory.CreateDirectory(m_cache.LanguageProject.LinkedFilesRootDir);
				if (!Directory.Exists(s_defaultPicturesFolder))
					Directory.CreateDirectory(s_defaultPicturesFolder);
			}
			catch (Exception e)
			{
				var errorMsg = string.Format("Error creating one of the following directories '{0}' or '{1}'",
											 m_cache.LanguageProject.LinkedFilesRootDir, s_defaultPicturesFolder);
				Logger.WriteEvent(errorMsg);
				Logger.WriteError(e);
				MessageBoxUtils.Show(errorMsg);
			}

			m_txtDestination.Text = s_sExternalLinkDestinationDir ?? s_defaultPicturesFolder;

			if (m_initialPicture != null)
			{
				if (m_initialPicture.PictureFileRA == null)
					m_filePath = string.Empty;
				else
				{
					m_filePath = m_initialPicture.PictureFileRA.AbsoluteInternalPath;
					if (m_filePath == StringServices.EmptyFileName)
						m_filePath = string.Empty;
				}

				if (FileUtils.TrySimilarFileExists(m_filePath, out m_filePath))
					imageToolbox.ImageInfo = PalasoImage.FromFile(m_filePath);
				else
				{
					// use an image that indicates the image file could not be opened.
					imageToolbox.ImageInfo.Image = SimpleRootSite.ImageNotFoundX;
				}
				UpdateInfoForNewPic();
				m_rbLeave.Checked = true;
				return;
			}

			// there is no picture yet; hide the saving controls
			panelFileName.Visible = false;
			ImageExistsOutsideProject = false;
			m_btnOK.Enabled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				components?.Dispose();
				m_helpProvider?.Dispose();
			}
			base.Dispose(disposing);
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the file the user chose (if the user chose to move or copy a file
		/// to the "internal" folder (i.e., the LinkedFiles folder), this will be the path of
		/// the file in the new location.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CurrentFile
		{
			get
			{
				CheckDisposed();
				return m_filePath;
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show help for this dialog
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, HelpTopic);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnBrowseDest control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnBrowseDest_Click(object sender, EventArgs e)
		{
			Logger.WriteEvent("Browsing for destination folder in 'Picture Properties' dialog");
			using (var dlg = new FolderBrowserDialogAdapter())
			{
				dlg.SelectedPath = m_txtDestination.Text;
				dlg.Description = string.Format(FwCoreDlgs.kstidSelectLinkedFilesSubFolder,
					s_defaultPicturesFolder);
				dlg.ShowNewFolderButton = true;

				if (dlg.ShowDialog() == DialogResult.OK)
				{
					if (ValidateDestinationFolder(dlg.SelectedPath))
						m_txtDestination.Text = dlg.SelectedPath;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Log a message when the dialog is closed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosed(EventArgs e)
		{
			Logger.WriteEvent("Closing 'Picture Properties' dialog with result " + DialogResult);

			if (DialogResult == DialogResult.OK)
			{
				var action = (m_initialPicture == null ? "Creating" : "Changing");
				Logger.WriteEvent(
					$"{action} Picture Properties: file: {m_filePath}, {imageToolbox.ImageInfo.Metadata.MinimalCredits(new[] { "en" }, out _)}");
			}

			base.OnClosed(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Closing"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs"/> that
		/// contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			if (DialogResult == DialogResult.OK)
			{
				if (m_isSuggestingLicense)
				{
					// The user didn't select a license; don't save one
					imageToolbox.ImageInfo.Metadata.License = new NullLicense();
					imageToolbox.ImageInfo.Metadata.HasChanges = false;
				}
				if (!m_rbLeave.Checked && !ValidateDestinationFolder(m_txtDestination.Text))
				{
					e.Cancel = true;
				}
				else if (IsDirty)
				{
					ApplySaveFile(e);
				}
				else if (ImageExistsOutsideProject)
				{
					ApplyMoveCopyOrLeaveFile(e);
				}
			}

			base.OnClosing(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes sure the destination text box and chooser button are disabled when the
		/// user chooses to leave the picture in its original folder.
		/// Handles other behaviour when the user chooses to Save or Save As
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleLocationCheckedChanged(object sender, EventArgs e)
		{
			if (!((RadioButton)sender).Checked)
			{
				return;
			}
			m_txtDestination.Visible = m_btnBrowseDest.Visible = m_lblDestination.Visible =
				txtFileName.Visible = lblFileName.Visible = (sender != m_rbLeave);
			if (IsDirty)
			{
				// If it's dirty, it needs to be saved [as]
				if (sender == m_rbCopy_rbSave)
				{
					txtFileName.Visible = lblFileName.Visible = false;
					txtFileName.Text = Path.GetFileName(lblSourcePath.Text);
					m_isSaveAsChosen = false;
				}
				else // save as
				{
					txtFileName.Visible = lblFileName.Visible = true;
					m_isSaveAsChosen = true;
				}
			}
			else
			{
				m_fileLocChoice = m_rbCopy_rbSave.Checked ? FileLocationChoice.Copy :
					m_rbMove_rbSaveAs.Checked ? FileLocationChoice.Move : FileLocationChoice.Leave;
			}
		}
		#endregion

		#region Private methods
		/// <summary>
		/// Called when the user clicked the OK button and the file needs to be moved or copied into the linked media folder or left where it is.
		/// Cancels if the file is unable to be moved or copied for any reason.
		/// </summary>
		private void ApplyMoveCopyOrLeaveFile(CancelEventArgs e)
		{
			// If this dialog is being displayed for the purpose of inserting a new
			// picture or changing which picture is being displayed, remember the user's
			// copy/move/leave choice
			if (m_initialPicture == null || m_initialPicture.PictureFileRA.AbsoluteInternalPath != m_filePath)
				s_defaultFileLocChoiceForSession = m_fileLocChoice;

			var filePath = MoveOrCopyFilesController.PerformMoveCopyOrLeaveFile(m_filePath, m_txtDestination.Text, m_fileLocChoice, false,
				string.IsNullOrEmpty(txtFileName.Text) ? null : txtFileName.Text);
			if (filePath == null)
			{
				e.Cancel = true;
			}
			else
			{
				m_filePath = filePath;
				s_sExternalLinkDestinationDir = m_txtDestination.Text;
			}
		}

		/// <summary>
		/// Called when the user clicked the OK button and the file has any changes to be saved (as).
		/// Cancels if the file is unable to be moved or copied for any reason.
		/// </summary>
		private void ApplySaveFile(CancelEventArgs e)
		{
			// ReSharper disable once AssignNullToNotNullAttribute - txtFileName should always have text
			var savePath = Path.Combine(m_txtDestination.Text, txtFileName.Text);
			// If Save As and there is a conflict, prompt to overwrite (cancel if user clicks no)
			if (m_rbMove_rbSaveAs.Checked && File.Exists(savePath) &&
				MessageBox.Show(string.Format(FwCoreDlgs.ksAlreadyExists, savePath), FwCoreDlgs.kstidWarning, MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning) == DialogResult.No)
			{
				e.Cancel = true;
			}
			else
			{
				try
				{
					imageToolbox.ImageInfo.Save(savePath);
					m_filePath = savePath;
				}
				catch (Exception ex)
				{
					Logger.WriteEvent($"Error saving file to '{savePath}'");
					Logger.WriteError(ex);
					// This file is probably open somewhere.
					MessageBox.Show(FwCoreDlgs.ksErrorFileInUse, FwCoreDlgs.ksError);
					e.Cancel = true;
				}
			}
		}

		/// <summary/>
		private bool ValidateDestinationFolder(string proposedDestFolder)
		{
			if (!IsFolderInLinkedFilesFolder(proposedDestFolder))
			{
				MessageBoxUtils.Show(this, string.Format(FwCoreDlgs.kstidDestFolderMustBeInLinkedFiles,
					s_defaultPicturesFolder), m_app.ApplicationName, MessageBoxButtons.OK);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Applies the user's file location choice.
		/// </summary>
		private void ApplyFileLocationChoice()
		{
			switch (m_fileLocChoice)
			{
				case FileLocationChoice.Copy:
					m_rbCopy_rbSave.Checked = true;
					break;
				case FileLocationChoice.Move:
					m_rbMove_rbSaveAs.Checked = true;
					break;
				case FileLocationChoice.Leave:
					m_rbLeave.Checked = true;
					break;
			}
		}

		/// <remarks>The Enter key is used to search the AOR gallery. Register our OK button only when it doesn't conflict.</remarks>
		private void ImageToolbox_Enter(object sender, EventArgs e) { AcceptButton = null; }
		/// <remarks>The Enter key is used to search the AOR gallery. Register our OK button only when it doesn't conflict.</remarks>
		private void ImageToolbox_Leave(object sender, EventArgs e) { AcceptButton = m_btnOK; }

		private void ImageToolbox_ImageChanged(object sender, EventArgs e)
		{
			// AcquireImage includes scan, camera, and filesystem; ImageGallery has its own implementation
			if (sender is AcquireImageControl || sender is ImageGalleryControl)
			{
				// A new image has been selected
				m_isAOR = sender is ImageGalleryControl;
				m_isSaveAsChosen = false;
				m_filePath = imageToolbox.ImageInfo.OriginalFilePath;
				UpdateInfoForNewPic();
				panelFileName.Visible = true;
				if (ImageExistsOutsideProject)
				{
					// rbSave is hidden if metadata is added and the original file format doesn't support it
					m_rbCopy_rbSave.Visible = true;
					m_fileLocChoice = s_defaultFileLocChoiceForSession;
					ApplyFileLocationChoice();
				}
			}
			else
			{
				// As of 2023.06, the only other option is crop.
			}
			OnDirtyChanged();
		}

		private void ImageToolbox_MetadataChanged(object sender, EventArgs e)
		{
			m_isSuggestingLicense = false;
			if (!FileFormatSupportsMetadata)
			{
				txtFileName.Text = Path.ChangeExtension(txtFileName.Text, "png");
				m_isSaveAsChosen = m_rbMove_rbSaveAs.Checked = true;
				m_rbCopy_rbSave.Visible = false;
			}
			OnDirtyChanged();
		}

		/// <summary>
		/// Changes the controls to reflect whether an image is dirty (needs to be saved) or not (can be moved, copied, or left where it is).
		/// </summary>
		private void OnDirtyChanged()
		{
			if (IsDirty)
			{
				m_rbCopy_rbSave.Text = FwCoreDlgs.ksSaveChanges;
				m_rbMove_rbSaveAs.Text = FwCoreDlgs.ksSaveChangesAs;
				m_rbLeave.Visible = false;
				m_grpFileLocOptions.Visible = true;
				if (!m_isSaveAsChosen && !IsCropped && FileIsInLinkedFilesFolder(lblSourcePath.Text))
				{
					m_rbCopy_rbSave.Checked = true;
				}
				else
				{
					m_rbMove_rbSaveAs.Checked = true;
				}
			}
			else
			{
				m_rbCopy_rbSave.Text = m_rbCopyText;
				m_rbMove_rbSaveAs.Text = m_rbMoveText;
				m_rbLeave.Visible = true;
				m_grpFileLocOptions.Visible = ImageExistsOutsideProject;
				txtFileName.Visible = lblFileName.Visible = true;
				if (ImageExistsOutsideProject)
					ApplyFileLocationChoice();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the information in the dialog after a new image has been selected
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateInfoForNewPic()
		{
			// Add "(not found)" if the original file isn't available
			var tmpOriginalPath = m_filePath;
			if (string.IsNullOrEmpty(tmpOriginalPath))
				ImageExistsOutsideProject = false;
			else
			{
				if (!File.Exists(tmpOriginalPath))
					tmpOriginalPath = tmpOriginalPath.Normalize();
				if (!File.Exists(tmpOriginalPath))
					tmpOriginalPath = tmpOriginalPath.Normalize(System.Text.NormalizationForm.FormD);
				if (!File.Exists(tmpOriginalPath))
				{
					m_imageInitialSize = Size.Empty;
					tmpOriginalPath =
						string.Format(FwCoreDlgs.kstidPictureUnavailable, tmpOriginalPath.Normalize());
					ImageExistsOutsideProject = false;
					m_btnOK.Enabled = false;
				}
				else
				{
					m_imageInitialSize = imageToolbox.ImageInfo.Image.Size;
					ImageExistsOutsideProject = !FileIsInLinkedFilesFolder(tmpOriginalPath);
					m_btnOK.Enabled = true;
				}
			}

			if (imageToolbox.ImageInfo.Metadata.IsLicenseNotSet)
			{
				imageToolbox.ImageInfo.Metadata.License = CreativeCommonsLicense.FromToken("cc0");
				m_isSuggestingLicense = true;
			}
			else
			{
				m_isSuggestingLicense = false;
			}
			// Palaso always sets HasChanges=true, but a freshly-selected image has no changes. In the future, we may wish to
			// investigate a change in Palaso (https://github.com/sillsdev/libpalaso/issues/1268) ~Hasso, 2023.06
			imageToolbox.ImageInfo.Metadata.HasChanges = false;

			// update the file name
			lblSourcePath.Text = tmpOriginalPath;
			txtFileName.Text = Path.GetFileName(tmpOriginalPath);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether file is in the LinkedFiles folder.
		/// </summary>
		/// <param name="sFilePath">The file path.</param>
		/// ------------------------------------------------------------------------------------
		private static bool FileIsInLinkedFilesFolder(string sFilePath)
		{
			return MoveOrCopyFilesController.IsFileInFolder(sFilePath, s_defaultPicturesFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether given folder is in LinkedFiles folder.
		/// </summary>
		/// <param name="sFolder">The full path of the folder to check.</param>
		/// ------------------------------------------------------------------------------------
		private static bool IsFolderInLinkedFilesFolder(string sFolder)
		{
			if (!Directory.Exists(sFolder))
				return false;
			sFolder = sFolder.ToLowerInvariant().TrimEnd(Path.DirectorySeparatorChar);
			string sExtLinksRoot = s_defaultPicturesFolder;
			sExtLinksRoot = sExtLinksRoot.ToLowerInvariant().TrimEnd(Path.DirectorySeparatorChar);
			// Check whether the file is found within the directory.  If so, just return.
			if (sFolder.StartsWith(sExtLinksRoot))
			{
				int cchDir = sExtLinksRoot.Length;
				return (sFolder.Length == cchDir ||
					(sFolder.Length > cchDir && sFolder[cchDir] == Path.DirectorySeparatorChar));
			}
			return false;
		}
		#endregion
	}
}
