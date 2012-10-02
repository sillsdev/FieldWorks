// --------------------------------------------------------------------------------------------
// <copyright file="IExporter.cs" from='2009' to='2009' company='SIL International'>
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
	/// <summary>IExporter allows PsExport.dll aseembly of Pathway to be separately installed and dynamically loaded.</summary>
	public interface IExporter
	{
		#region Properties
		/// <summary>Gets or sets Output format (ODT, PDF, INX, TeX, HTM, PDB, etc.)</summary>
		string Destination { get; set; }

		/// <summary>Gets or sets data type (Scripture, Dictionary)</summary>
		string DataType { get; set; }

		/// <summary>Gets or sets data type (Scripture, Dictionary)</summary>
		ProgressBar ProgressBar { get; set; }
		#endregion Properties

		#region Export
		/// <summary>Export outpath. (normally outPath will end in .xhtml)</summary>
		void Export(string outPath);
		#endregion Export
	}
}