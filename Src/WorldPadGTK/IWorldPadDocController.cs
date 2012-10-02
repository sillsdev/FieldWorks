/*
 *    IWorldPadDocController.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.WorldPad
{

	/// <summary>
	/// Allows Initializing dialogs while still using the factory pattern.
	/// </summary>
	public delegate bool BeforeShow(System.Windows.Forms.Form dialog);
	/// <summary>
	/// Allows getting dialogs' results while still using the factory pattern.
	/// </summary>
	public delegate bool AfterShow(System.Windows.Forms.Form dialog);

	public interface IWorldPadDocController
	{
		IWorldPadAppController AppController {get;/* set;*/}

		IWorldPadDocModel DocModel {get;/* set;*/}

		IWorldPadPaneView UpperPane {get;}

		IWorldPadPaneView LowerPane {get;}

		void SplitPane();

		void ClosePane();

		void SetFontSize(string fontSize);

		/// <summary>
		/// Sets the Model's writing system to allow updating of the UI.
		/// </summary>
		void SetWritingSystem(string writingSystem);

		void SetFontFamily(string fontFamily);

		void SetStyle(string style);

		void SetBold(ThreeState boldState);

		void SetItalic(ThreeState italicState);

		void SetAlign(FwTextAlign textAlign);

		void Init();

		void Quit();

		void FileNew();

		void FileOpen(string filename);

		void FileClose();

		DialogController ShowDialog(DialogFactory.DialogType type);

		System.Windows.Forms.Form ShowSWFDialog(DialogFactory.DialogType type);

		/// <summary>
		/// Variation on ShowSWFDialog allowing delegates for initialization and applying results
		/// </summary>
		System.Windows.Forms.Form ShowSWFDialog(DialogFactory.DialogType type,
			BeforeShow before, AfterShow after);
	}
}
