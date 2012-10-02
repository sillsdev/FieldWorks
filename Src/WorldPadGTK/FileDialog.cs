using System;
using Gtk;
using System.Reflection;

namespace System.Windows.Forms
{
	/// <summary>
	/// A Wrapper Class for the System.Windows.Forms class. Used primarily as a superclass
	/// for the OpenFileDialog and SaveFileDialog.
	/// </summary>
	public abstract class FileDialog : IWin32Window
	{
		protected Gtk.FileChooserDialog m_dialog;
		private static string ICON_FILE = "ApplicationIcon";

		private string m_defaultTitle;
		private bool m_checkFileExists = false;
		private bool m_checkPathExists = true;
		private string m_defaultExt = String.Empty;
		private string m_fileName = String.Empty;
		private string m_filter = String.Empty;
		private string m_initialDirectory = String.Empty;
		private static string m_lastUsed = String.Empty;
		private bool m_restoreDirectory = false;
		private Gtk.Widget m_helpButton;
		private string m_title = String.Empty;
		private bool m_validateNames = true;

		/// <summary>
		/// Construct a FileDialog
		/// </summary>
		/// <param name="action">
		/// A <see cref="Gtk.FileChooserAction"/> that determines what this FileDialog does,
		/// ie. Save or Open.
		/// </param>
		/// <param name="defaultTitle">
		/// Title of the dialog.
		/// </param>
		/// <param name="label">
		/// String containing the icon for the dialog's button.
		/// </param>
		public FileDialog(Gtk.FileChooserAction action, string defaultTitle, string label)
		{
			Console.WriteLine("FileDialog.ctor invoked");

			m_defaultTitle = defaultTitle;
			m_dialog = new Gtk.FileChooserDialog(m_defaultTitle, null, action);

			SetResourceIcon(ICON_FILE);
			// Add buttons to the dialog 'action area'.
			m_helpButton = m_dialog.AddButton(Gtk.Stock.Help, Gtk.ResponseType.Help);
			m_dialog.AddButton(Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
			/*Gtk.Button okButton =
				(Gtk.Button) m_dialog.AddButton(label, Gtk.ResponseType.Ok);
			okButton.Clicked += okButtonClickedHandler;
			m_dialog.Default = okButton;*/
			m_dialog.Default = m_dialog.AddButton(label, Gtk.ResponseType.Ok);
			m_dialog.ActionArea.ShowAll();
			m_helpButton.Visible = false;

			//Icon = new Gdk.Pixbuf("WorldPad.ico");

			// Add container for optional extra widgets (see derived classes)
			m_dialog.ExtraWidget = new Gtk.HBox();
		}

		/*protected virtual void okButtonClickedHandler(object sender, EventArgs e)
		{
			Console.WriteLine("FileDialog.okButtonClickedHandler() invoked");
		}*/

		public virtual System.IO.Stream OpenFile()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Show this dialog.
		/// </summary>
		/// <param name="parent">
		/// A <see cref="IWin32Window"/> which is the parent window within which this dialog
		/// will appear. Can be null.
		/// </param>
		/// <returns>
		/// A <see cref="DialogResult"/>, indicating what the user's action was.
		/// </returns>
		public virtual DialogResult ShowDialog(IWin32Window parent)
		{
			Console.WriteLine("FileDialog.ShowDialog(IWin32Window) invoked");

			// Note: Needed for GTK+ to honour WindowPosition.CenterOnParent.
			if (parent != null)
				m_dialog.TransientFor = parent.Window;

			return ShowDialog();
		}

		/// <summary>
		/// Show this dialog. Returns the correct DialogResult.
		/// </summary>
		/// <returns>
		/// A <see cref="DialogResult"/> indicating what the user's action was.
		/// </returns>
		public virtual DialogResult ShowDialog()
		{
			Console.WriteLine("FileDialog.ShowDialog() invoked");

			m_dialog.SetCurrentFolder(GetInitialDirectory());

			SetFileName(System.IO.Path.GetFileName(m_fileName));

			DialogResult result = DialogResult.None;

			do
			{
				switch ((Gtk.ResponseType) m_dialog.Run())
				{
					case Gtk.ResponseType.Cancel:
					case Gtk.ResponseType.DeleteEvent:
						result = DialogResult.Cancel;
						break;
					case Gtk.ResponseType.Ok:
						FileName = m_dialog.Filename;
						if (OkToContinue())
						{
							result = DialogResult.OK;
						}
						break;
				}
			} while (result == DialogResult.None);

			Hide();  // TODO: possibly call m_dialog.Hide() directly?

			return result;
		}

		/// <summary>
		/// When a user opens up a Open/Save dialog, it is often useful to have the current
		/// working directory to be one that seems like a logic place to start. The rules
		/// for this starting place are as follows:
		/// 1. If the user has previously navigated to a directory in an Open or
		///    Save As dialog in this application, then that will be the initial
		///    directory.
		/// 2. If InitialDirectory is not an empty string, then its value is used.
		/// 3. If InitialDirectory is an empty string and the current working
		///    directory contains any files of the specified filter types, then the
		///    initial directory is the current working directory.
		/// 4. If InitialDirectory is an empty string and there is at least one
		///    non-home directory in the shortcuts, then the initial directory is
		///    the first non-home directory.
		/// 5. Otherwise, the initial directory is the user's home directory.
		/// </summary>
		/// <returns>
		/// The initial directory.
		/// </returns>
		private string GetInitialDirectory()
		{
			Console.WriteLine("FileDialog.GetInitialDirectory() invoked");

			// The following is similar to the Windows 2000/XP behaviour (see MSDN)
			// ====================================================================
			//


			//string dir = GetDirectoryName(m_fileName);
			string dir = String.Empty;

			Console.WriteLine("Use the most recent Open or Save As directory which is {0}", LastUsedDirectory);
			// Use the most recent Open or Save As directory
			dir = LastUsedDirectory;

			if (dir == String.Empty)
			{
				Console.WriteLine("Use the supplied initial directory which is {0}", m_initialDirectory);
				// Use the supplied initial directory
				dir = m_initialDirectory;
			}

			if (dir == String.Empty)
			{
				// Look for 'candidate' files in the working directory
				string wrkDir = System.IO.Directory.GetCurrentDirectory();
				if (FilesFoundInDirectory(wrkDir))
					dir = wrkDir;
			}

			if (dir == String.Empty)
			{
				Console.WriteLine("Set to the user's home directory");
				// Set to the user's home directory
				Environment.SpecialFolder home =
					Environment.SpecialFolder.Personal;
				dir = System.Environment.GetFolderPath(home);
			}

			return dir;
		}

		/// <summary>
		/// Get the DirectoryName of a given file.
		/// </summary>
		/// <param name="path">
		/// A <see cref="System.String"/> The full path to the file.
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/> The directory name of the file.
		/// </returns>
		private string GetDirectoryName(string path)
		{
			string dir = String.Empty;

			try
			{
				dir = System.IO.Path.GetDirectoryName(path);
			}
			catch (Exception ex) { }  // TODO: Should we do something here?

			if (dir == null)
				dir = String.Empty;

			if (dir != String.Empty)
			{
				try
				{
					dir = System.IO.Path.GetFullPath(dir);
				}
				catch (Exception ex) { }  // TODO: Should we do something here?
			}

			return dir;
		}

		// JMG: UNKNOWN
		private bool FilesFoundInDirectory(string wrkDir)
		{
			Console.WriteLine("Look for 'candidate' files in the working directory");
			bool fileFound = false;
			if (m_dialog.Filter != null)
			{
				Gtk.FileFilter filter = m_dialog.Filter;
				Gtk.FileFilterFlags flags = filter.Needed;

				if (flags == Gtk.FileFilterFlags.Filename ||
					flags == Gtk.FileFilterFlags.Uri ||
					flags == Gtk.FileFilterFlags.MimeType)
				{
					throw new Exception();  // TODO: Only the DisplayName flag is expected
				}

				Gtk.FileFilterInfo info = Gtk.FileFilterInfo.Zero;
				info.Contains = flags;

				string[] files = System.IO.Directory.GetFiles(wrkDir);

				foreach (string file in files)
				{
					string displayName = System.IO.Path.GetFileName(file);
					info.DisplayName = displayName;
					if (filter.Filter(info))
					{
						Console.WriteLine("file \"{0}\" is displayed", displayName);
						fileFound = true;
						break;
					}
				}
			}
			return fileFound;
		}

		/// <summary>
		/// Set the filename of the currently selected file for this dialog.
		/// </summary>
		/// <param name="fileName">
		/// A <see cref="System.String"/> the filename to set the dialog to.
		/// </param>
		protected virtual void SetFileName(string fileName) { }

		/// <summary>
		/// Quick check to makes sure it's okay to continue on.
		/// </summary>
		/// <returns>
		/// If it's okay to continue or not.
		/// </returns>
		protected virtual bool OkToContinue()
		{
			Console.WriteLine("FileDialog.OkToContinue() invoked with Filename: " + m_fileName);
			int direct = m_fileName.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
			Console.WriteLine("direct: " + direct);
			LastUsedDirectory = m_fileName.Substring(0, direct);
			Console.WriteLine("Setting LastUsed to: " + LastUsedDirectory);
			return true;
		}

		/// <summary>
		/// Hide this dialog.
		/// </summary>
		public virtual void Hide()
		{
			Console.WriteLine("FileDialog.Hide() invoked");

			m_dialog.Hide();
		}

		// Note: This property has no effect whatsoever.
		public bool CheckFileExists
		{
			get { return m_checkFileExists; }
			set { m_checkFileExists = value; }
		}

		// Note: This property has no effect whatsoever.
		public bool CheckPathExists
		{
			get { return m_checkPathExists; }
			set { m_checkPathExists = value; }
		}

		// Note: This property has no effect whatsoever.
		public string DefaultExt
		{
			get { return m_defaultExt; }
			set { m_defaultExt = value; }
		}

		/// <value>
		/// The filename currently used by the dialog.
		/// </value>
		public string FileName
		{
			/*get { return m_dialog.Filename; }  // Includes the path*/
			get { return m_fileName; }  // Includes the path
			set { m_fileName = value; }
		}

		// JMG: UNKNOWN
		public string[] FileNames
		{
			get { return m_dialog.Filenames; }
		}

		/// <value>
		/// A filter by which to decide which types of files are selectable in the dialog.
		/// </value>
		public string Filter
		{
			get { return m_filter; }

			set  // Note: SWF limits us to filter description/pattern pairs
			{
				// If value is empty or null, remove all filters.
				if (String.Empty == value || null == value)
				{
					m_filter = String.Empty;
					foreach (Gtk.FileFilter filter in m_dialog.Filters)
						m_dialog.RemoveFilter(filter);
					return;
				}

				string[] text = value.Split(new char[] {'|'});

				if (text.Length % 2 != 0)
					throw new ArgumentException();

				for (int i = 0; i + 1 < text.Length; i += 2)
				{
					Gtk.FileFilter filter = new Gtk.FileFilter();
					filter.Name = text[i];

					foreach (string pattern in PatternsToArray(text[i + 1]))
					{
						filter.AddPattern(pattern);
					}

					m_dialog.AddFilter(filter);
				}

				m_dialog.Filter = m_dialog.Filters[0];

				m_filter = value;
			}
		}

		// JMG: UNKNOWN
		protected string[] PatternsToArray(string patterns)
		{
			// Note: Linux is case-sensitive, so filter patterns need to be duplicated
			string lower = patterns.ToLower();
			string upper = patterns.ToUpper();

			string lowerAndUpper = patterns;
			if (!upper.Equals(lower))
				lowerAndUpper = lower + ";" + upper;

			return lowerAndUpper.Split(new char[] {';'});
		}

		// JMG: UNKNOWN
		public int FilterIndex
		{
			get
			{
				int i = 0;

				foreach (Gtk.FileFilter filter in m_dialog.Filters)
				{
					i++;

					if (filter == m_dialog.Filter)
						return i;
				}

				return 1;  // TODO: Indicate that this is an error or return default?
			}

			set
			{
				if (value <= 0 || value > m_dialog.Filters.Length)
					throw new ArgumentOutOfRangeException();

				m_dialog.Filter = m_dialog.Filters[--value];
			}
		}

		/// <value>
		/// The initial directory to look in.
		/// </value>
		public string InitialDirectory
		{
			get { return m_initialDirectory; }

			set
			{
				m_initialDirectory = String.Empty;
				try
				{
					if (System.IO.Directory.Exists(value))
						m_initialDirectory = value;
				}
				catch (Exception ex) { }
			}
		}

		// Note: This property has no effect whatsoever.
		public bool RestoreDirectory
		{
			get { return m_restoreDirectory; }
			set { m_restoreDirectory = value; }
		}

		// JMG: UNKNOWN
		public bool ShowHelp
		{
			get { return m_helpButton.Visible; }
			set { m_helpButton.Visible = value; }
		}

		/// <value>
		/// The title of the dialog.
		/// </value>
		public string Title
		{
			get { return m_title; }

			set
			{
				m_dialog.Title = value == String.Empty ? m_defaultTitle : value;

				m_title = value;
			}
		}

		// Note: This property has no effect whatsoever.
		public bool ValidateNames
		{
			get { return m_validateNames; }
			set { m_validateNames = value; }
		}

		/// <value>
		/// The dialog window itself.
		/// </value>
		public Gtk.Window Window
		{
			get { return m_dialog; }
		}

		/// <summary>
		/// The resource icon to use for this dialog.
		/// </summary>
		/// <param name="icon">
		/// A <see cref="System.String"/> the path/name of the icon.
		/// </param>
		public void SetResourceIcon(string icon)
		{
			Assembly entryAssembly = Assembly.GetEntryAssembly();
			try { m_dialog.Icon = new Gdk.Pixbuf(entryAssembly, icon); }
			catch (Exception e) { Console.WriteLine("Could not find {0}", icon); }
		}

		/// <value>
		/// A string containing the last directory used by any FileDialog.
		/// </value>
		public string LastUsedDirectory
		{
			get { return m_lastUsed; }
			set
			{
				try
				{
					if (System.IO.Directory.Exists(value))
						m_lastUsed = value;
				}
				catch (Exception ex) { }
			}
		}
	}
}
