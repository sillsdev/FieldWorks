// --------------------------------------------------------------------------------------------
// <copyright file="IScriptureContents.cs" from='2009' to='2009' company='SIL International'>
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
// IScriptureContents allows PublishingSolution to be separately installed and
//   dynamically load CssDialog.ScriptureContents.
// </remarks>
// --------------------------------------------------------------------------------------------

using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>Interface for PrintVia dialog of Pathway for Scripture to be separately installed and dynamically loaded.</summary>
	public interface IScriptureContents
	{
		#region Properties
		/// <summary>Name of database (used to create default path).</summary>
		string DatabaseName { set;  get; }

		/// <summary>Directory to put exported data.</summary>
		string OutputLocationPath { get; }

		/// <summary>True if data is to be loaded from an existing directory (and not exported from Flex)</summary>
		bool ExistingPublication { get; }

		/// <summary>Directory containing existing data</summary>
		string ExistingLocationPath { get; }

		/// <summary>Name to be used for new dictionary (becomes a folder name)</summary>
		string PublicationName { set; get; }
		#endregion Properties

		/// <summary>Method to display form dialog</summary>
		DialogResult ShowDialog();
	}
}