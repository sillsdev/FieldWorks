/*
 *    DialogFactory.cs
 *
 *    <purpose>
 *
 *    Jean-Marc Giffin - 2008-05-21
 *
 *    $Id$
 */

using System;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.WorldPad
{
	/// -------------------------------------------------------------------------------------------
	/// <summary>
	/// A factory for Dialogs, creating the right kind as necessary.
	/// Currently, it also stores the models, so that there is only ever one instance of a model ever created.
	/// </summary>
	/// -------------------------------------------------------------------------------------------
	public class DialogFactory
	{
		/// <summary>
		/// Different kinds of Dialogs that can be created.
		/// </summary>
		public enum DialogType
		{
			FileOpen,
			FileSave,
			PageFormat,
			Fonts,
			FindReplace,
			WritingSystem,
			Paragraph,
			BulletNumbering,
			Borders,
			Styles,
			Options,
			OldWritingSystems,
			About
		}

		private FileOpenModel fileOpenModel_;
		private OptionsDlgModel optionsDlgModel_;
		private HelpAboutDlgModel helpAboutDlgModel_;
		private FmtWrtSysDlgModel fmtWrtSysDlgModel_;
		private FmtBdrDlgPModel fmtBdrDlgPModel_;
		private FmtParaDlgRtlModel fmtParaDlgRtlModel_;
		// TODO Remove comment when missing file is added.
		// private FmtBulNumDlgModel fmtBulNumDlgModel_;
		private OldWritingSystemsDlgModel oldWritingSystemsDlgModel_;
		private FileSaveModel fileSaveModel_;
		private IWorldPadDocModel worldPadDocModel_;
		private FilPgSetDlgModel page_;


		public DialogFactory(IWorldPadDocModel worldPadDocModel)
		{
			page_ = new FilPgSetDlgModel();
			fileOpenModel_ = new FileOpenModel();
			fileSaveModel_ = new FileSaveModel();
			optionsDlgModel_ = new OptionsDlgModel();
			helpAboutDlgModel_ = new HelpAboutDlgModel();
			fmtWrtSysDlgModel_ = new FmtWrtSysDlgModel();
			fmtBdrDlgPModel_ = new FmtBdrDlgPModel();
			fmtParaDlgRtlModel_ = new FmtParaDlgRtlModel();
			// TODO Remove comment when missing file is added.
			//fmtBulNumDlgModel_ = new FmtBulNumDlgModel();
			oldWritingSystemsDlgModel_ = new OldWritingSystemsDlgModel();
			worldPadDocModel_ = worldPadDocModel;
		}

		/// <summary>
		/// The function that creates and returns the requested Dialog type.
		/// </summary>
		///
		/// <param name="type">The type of DialogController requested,
		/// which is gotten from referring to this class' enumeration.</param>
		///
		/// <returns>A DialogController of requested type.</returns>
		public DialogController CreateDialog(DialogFactory.DialogType type)
		{
			DialogController d = null;
			if (type == DialogType.PageFormat)
				d = new FilPgSetDlgController(page_);
			else if (type == DialogType.Styles)
				d = new AfStyleDlgController(page_);
			else if (type == DialogType.About)
				d = new FwHelpAbout(helpAboutDlgModel_);
			else if (type == DialogType.Options)
				d = new OptionsDlgController(optionsDlgModel_);
			else if (type == DialogType.OldWritingSystems)
				d = new OldWritingSystemsDlgController(oldWritingSystemsDlgModel_);
			else if (type == DialogType.Borders)
				d = new FmtBdrDlgPController(fmtBdrDlgPModel_);
			// TODO Remove comment when missing file is added.
			//else if (type == DialogType.BulletNumbering)
			//		d = new FmtBulNumDlgController(fmtBulNumDlgModel_);
			else if (type == DialogType.Paragraph)
				d = new FmtParaDlgRtlController(fmtParaDlgRtlModel_);
			else if (type == DialogType.WritingSystem)
				d = new FmtWrtSysDlgController(fmtWrtSysDlgModel_);
			else if (type == DialogType.FindReplace)
				d = new WpFindReplaceDlgController(page_);
			else if (type == DialogType.FileOpen)
				d = new FileOpenDlgController(fileOpenModel_);
			else if (type == DialogType.FileSave)
				d = new FileSaveDlgController(fileSaveModel_, worldPadDocModel_);
			return d;
		}

		/// <summary>
		/// The function that creates and returns the requested SWF Form type.
		/// </summary>
		///
		/// <param name="type">The type of SWF Form requested,
		/// which is gotten from referring to this class' enumeration.</param>
		///
		/// <returns>A SWF Form of requested type.</returns>
		public System.Windows.Forms.Form CreateSWFDialog(DialogFactory.DialogType type)
		{
			System.Windows.Forms.Form d = null;
			if (type == DialogType.Fonts)
				d = new FwFontDialog(/*IHelpTopicProvider */null);
			return d;
		}
	}
}
