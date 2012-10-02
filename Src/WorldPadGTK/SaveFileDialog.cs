using Mono.Unix;
using System;

namespace System.Windows.Forms
{
	/// <summary>
	/// System.Windows.Forms wrapper class for the SaveFileDialog.
	/// </summary>
	public class SaveFileDialog : FileDialog
	{
		private bool m_createPrompt = false;
		private bool m_overwritePrompt = true;
		private static SaveFileDialog sfd_;

		private Gtk.CheckButton m_addExtension;

		/// <summary>
		/// Private constructor to allow singleton pattern.
		/// </summary>
		private SaveFileDialog() : base(Gtk.FileChooserAction.Save, Catalog.GetString("Save As"),
			Gtk.Stock.Save)
		{
			Console.WriteLine("SaveFileDialog ctor invoked");
			Catalog.Init("MessageBox", "locale");

			//m_dialog.DoOverwriteConfirmation = true;

			m_addExtension = new Gtk.CheckButton("_Automatic file name extension");
			((Gtk.HBox) m_dialog.ExtraWidget).PackEnd(m_addExtension, false, false, 0);
			m_addExtension.Active = true;
			m_addExtension.Toggled += AddExtensionToggledHandler;
			m_addExtension.Visible = false;

			Gtk.Widget entry = FindWidget(m_dialog.VBox, typeof(Gtk.Entry));
			if (entry != null)
			{
				m_dialog.SetResponseSensitive(Gtk.ResponseType.Ok, false);
				((Gtk.Entry) entry).Changed += FilenameChangedHandler;
			}

			Gtk.Widget expander = FindWidget(m_dialog.VBox, typeof(Gtk.Expander));
			if (expander != null)
			{
				((Gtk.Expander) expander).Activated += ExpanderActivatedHandler;
			}
		}

		/// <summary>
		/// Get singleton instance of of the SaveFileDialog.
		/// </summary>
		/// <returns>
		/// A <see cref="SaveFileDialog"/>, the requested SaveFileDialog.
		/// </returns>
		public static SaveFileDialog GetInstance()
		{
			if (sfd_ == null)
				sfd_ = new SaveFileDialog();
			return sfd_;
		}

		// JMG: UNKNOWN
		private Gtk.Widget FindWidget(Gtk.Container parent, Type type)
		{
			Gtk.Widget widget = null;

			foreach (Gtk.Widget child in parent.Children)
			{
				if (child.GetType() == type)
					return (Gtk.Widget) child;

				if (child is Gtk.Container)
				{
					widget = FindWidget((Gtk.Container) child, type);

					if (widget != null)
						break;
				}
			}

			return widget;
		}

		/// <summary>Handler for when an extension is added.</summary>
		private void AddExtensionToggledHandler(object sender, EventArgs e)
		{
			Console.WriteLine("SaveFileDialog.AddExtensionToggledHandler() invoked");

			AddExtension = m_addExtension.Active;
		}

		/// <summary>Handler for when the selected file changes.</summary>
		private void FilenameChangedHandler(object sender, EventArgs e)
		{
			Console.WriteLine("SaveFileDialog.FilenameChangedHandler() invoked");

			Gtk.Entry entry = (Gtk.Entry) sender;

			m_dialog.SetResponseSensitive(Gtk.ResponseType.Ok, entry.Text.Length > 0);
		}

		/// <summary>Handler for when the expander is activated.</summary>
		private void ExpanderActivatedHandler(object sender, EventArgs e)
		{
			Console.WriteLine("SaveFileDialog.ExpanderActivatedHandler() invoked");

			Gtk.Expander expander = (Gtk.Expander) sender;

			m_addExtension.Visible = expander.Expanded;
		}

		/*protected override void okButtonClickedHandler(object sender, EventArgs e)
		{
			Console.WriteLine("SaveFileDialog.okButtonClickedHandler() invoked");
		}*/

		/// <summary>Set the filename.</summary>
		protected override void SetFileName(string fileName)
		{
			Console.WriteLine("SaveFileDialog.SetFileName() invoked");

			m_dialog.CurrentName = fileName;
		}

		/// <summary>Quick check to makes sure it's okay to continue on.</summary>
		/// <returns>If it's okay to continue or not.</returns>
		protected override bool OkToContinue()
		{
			Console.WriteLine("SaveFileDialog.OkToContinue() invoked");

			/*bool passed = true;*/

			if (AddExtension && System.IO.Path.GetExtension(FileName) == String.Empty)
				AddFileExtension();

			/*passed = System.IO.File.Exists(FileName) ? OkToOverwrite() : OkToCreate();

			return passed;*/
			base.OkToContinue();
			return (System.IO.File.Exists(FileName) ? OkToOverwrite() : OkToCreate());
		}

		/// <summary>
		/// Check with the user to assure it is okay to overwrite the file.
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/> Whether or not it's okay to overwrite.
		/// </returns>
		private bool OkToOverwrite()
		{
			Console.WriteLine("SaveFileDialog.OkToOverwrite() invoked");

			if (!m_overwritePrompt)
				return true;

			string pangoTags = "<span weight=\"bold\" size=\"larger\">{0}</span>";
			string msg = String.Format(pangoTags,
				"A file named \"{0}\" already exists. Do you want to replace it?");
			msg += "\n\nThe file already exists in \"{1}\".";
			msg += " Replacing it will overwrite its contents.";
			string pathName = System.IO.Path.GetDirectoryName(FileName);

			Gtk.MessageDialog dlg = new Gtk.MessageDialog(m_dialog,
				Gtk.DialogFlags.DestroyWithParent,
				Gtk.MessageType.Question,
				Gtk.ButtonsType.Cancel,  // Note: Save button is added below
				true,                    // Message text uses Pango markup
				String.Format(msg, System.IO.Path.GetFileName(FileName),
					System.IO.Path.GetFileName(pathName)));
			Gtk.Button okButton =
				(Gtk.Button) dlg.AddButton("_Replace", Gtk.ResponseType.Ok);
			okButton.Image = new Gtk.Image(Gtk.Stock.SaveAs, Gtk.IconSize.Button);

			Gtk.ResponseType response = (Gtk.ResponseType) dlg.Run();

			dlg.Hide();

			return (response == Gtk.ResponseType.Ok);
		}


		/// <summary>
		/// Quick check to assure it is okay to create the given file.
		/// Ie. Directory is writable by the user.
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/> whether or not the file may be created.
		/// </returns>
		private bool OkToCreate()
		{
			Console.WriteLine("SaveFileDialog.OkToCreate() invoked");

			if (!m_createPrompt)
				return true;

			string pangoTags = "<span weight=\"bold\" size=\"larger\">{0}</span>";
			string msg = String.Format(pangoTags,
				"The file named \"{0}\" does not exist. Do you want to save it?");
			msg += "\n\nThe file does not exist in \"{1}\".";
			msg += " Saving it will create a new file.";
			string pathName = System.IO.Path.GetDirectoryName(FileName);

			Gtk.MessageDialog dlg = new Gtk.MessageDialog(m_dialog,
				Gtk.DialogFlags.DestroyWithParent,
				Gtk.MessageType.Question,
				Gtk.ButtonsType.Cancel,  // Note: Save button is added below
				true,                    // Message text uses Pango markup
				String.Format(msg, System.IO.Path.GetFileName(FileName),
					System.IO.Path.GetFileName(pathName)));
			dlg.AddButton(Gtk.Stock.Save, Gtk.ResponseType.Ok);

			Gtk.ResponseType response = (Gtk.ResponseType) dlg.Run();

			dlg.Hide();

			return (response == Gtk.ResponseType.Ok);
		}

		/// <summary>
		/// Add an file type extension as a filter.
		/// </summary>
		private void AddFileExtension()
		{
			Console.WriteLine("SaveFileDialog.AddFileExtension() invoked");

			string[] text = Filter.Split(new char[] {'|'});

			string patterns = text[(FilterIndex * 2) - 1];
			//Console.WriteLine("patterns: {0}", patterns);

			string[] pattern = PatternsToArray(patterns);
			//Console.WriteLine("pattern[0]: {0}", pattern[0]);

			// TODO: Possibly try all patterns looking for first one that doesn't contain
			//       a '*'. If none is found, then use DefaultExt.
			int periodPos = pattern[0].LastIndexOf('.');
			string extension = pattern[0].Substring(periodPos + 1);

			Console.WriteLine("extension: {0}", extension);

			if (extension.IndexOf('*') >= 0)  // If extension contains '*'
				return;

			FileName = FileName + "." + extension;
		}

		// JMG: UNKNOWN
		public bool AddExtension
		{
			get { return m_addExtension.Active; }
			set { m_addExtension.Active = value; }
		}

		// JMG: UNKNOWN
		public bool CreatePrompt
		{
			get { return m_createPrompt; }
			set { m_createPrompt = value; }
		}

		// JMG: UNKNOWN
		public bool OverwritePrompt
		{
			/*get { return m_dialog.DoOverwriteConfirmation; }
			set { m_dialog.DoOverwriteConfirmation = value; }*/
			get { return m_overwritePrompt; }
			set { m_overwritePrompt = value; }
		}
	}
}
