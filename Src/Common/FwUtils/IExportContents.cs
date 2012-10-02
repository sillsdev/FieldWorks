// --------------------------------------------------------------------------------------------
// <copyright file="IExportContents.cs" from='2009' to='2009' company='SIL International'>
//      Copyright © 2009, SIL International. All Rights Reserved.
//
//      Distributable under the terms of either the Common Public License or the
//      GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// <author>Greg Trihus</author>
// <email>greg_trihus@sil.org</email>
// Last reviewed:
//
// <remarks>
// IExporter allows TePsExport.dll to be separately installed and dynamically loaded.
// </remarks>
// --------------------------------------------------------------------------------------------

using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>IExporteContents allows the PrintVia dialog (for dictionaries) of Pathway to be separately installed and dynamically loaded.</summary>
	public interface IExportContents
	{
		#region Properties
		/// <summary>Name of database (used to create default path).</summary>
		string DatabaseName { set; get; }

		/// <summary>True if Main Configured Dictionary section is to be exported</summary>
		bool ExportMain { get; set; }

		/// <summary>True if Reversal Section is to be exported</summary>
		bool ExportReversal { get; set; }

		/// <summary>True if Grammar Section is to be exported</summary>
		bool ExportGrammar { get; set; }

		/// <summary>True if Reversal Section exists and can be exported</summary>
		bool ReversalExists { set; }

		/// <summary>True if Grammar Section exists and can be exported</summary>
		bool GrammarExists { set; }

		/// <summary>Directory to put exported data.</summary>
		string OutputLocationPath { get; }

		/// <summary>True if data is to be loaded from an existing directory (and not exported from Flex)</summary>
		bool ExistingDirectoryInput { get; }

		/// <summary>Directory containing existing data</summary>
		string ExistingDirectoryLocationPath { get; }

		/// <summary>Name to be used for new dictionary (becomes a folder name)</summary>
		string DictionaryName { get; }
		#endregion Properties

		/// <summary>Method to display form dialog</summary>
		DialogResult ShowDialog();
	}
}