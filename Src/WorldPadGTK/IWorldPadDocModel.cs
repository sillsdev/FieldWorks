/*
 *    IWorldPadDocModel.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using System;
using System.Collections;
using System.IO;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.WorldPad
{
	public interface IDocModelChangedEventArgs
	{ }

	public delegate void DocModelChangedEventHandler(object sender,
		IDocModelChangedEventArgs e);

	public interface IWorldPadDocModel
	{
		event DocModelChangedEventHandler ModelChanged;

		string DocName {get;}

		string FileName {get;}

		bool IsDefault {get;}

		Hashtable Styles {get;}

		string ParagraphStyle {get;}

		Hashtable WritingSystems {get;}

		string SelectionWritingSystem {get;}

		string SelectionFontFamily {get;}

		string SelectionStyle {get;}

		string SelectionFontSize {get;}

		ThreeState SelectionBold {get;}

		ThreeState SelectionItalic {get;}

		bool JustificationLeft {get;}

		bool JustificationRight {get;}

		bool JustificationCenter {get;}

		/// <value>
		/// MainWnd, storing margins, header, footer, and pointing to stylesheet.
		/// Needed for interacting with load+save over COM.
		/// </value>
		WpgtkMainWnd MainWnd {get;set;}

		void ActionPerformed();

//		IWorldPadPaneModel AddPane();

		void Init();

		void Subscribe(DocModelChangedEventHandler handler);

		/// <summary>
		/// Sets the Model's writing system to allow updating of the UI.
		/// </summary>
		void SetWritingSystem(string writingSystem);

		void SetFontFamily(string fontFamily);

		void SetFontSize(string fontSize);

		void SetStyle(string style);

		void SetBold(ThreeState boldOn);

		void SetItalic(ThreeState italicOn);

		void SetAlign(FwTextAlign textAlign);
	}
}
