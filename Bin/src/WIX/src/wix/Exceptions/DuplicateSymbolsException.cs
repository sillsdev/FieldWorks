//-------------------------------------------------------------------------------------------------
// <copyright file="DuplicateSymbolsException.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// WiX cab creation exception.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;

	/// <summary>
	/// Duplicate symbols exception.
	/// </summary>
	public class DuplicateSymbolsException : Exception
	{
		private Symbol[] duplicateSymbols;

		/// <summary>
		/// Instantiate a new DuplicateSymbolException.
		/// </summary>
		/// <param name="symbols">The duplicated symbols.</param>
		public DuplicateSymbolsException(ArrayList symbols)
		{
			this.duplicateSymbols = (Symbol[])symbols.ToArray(typeof(Symbol));
		}

		/// <summary>
		/// Gets the duplicate symbols.
		/// </summary>
		/// <returns>List of duplicate symbols.</returns>
		public Symbol[] GetDuplicateSymbols()
		{
			return this.duplicateSymbols;
		}
	}
}
