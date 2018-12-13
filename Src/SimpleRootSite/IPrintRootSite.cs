// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing.Printing;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Interface to allow standard error handling when printing IVwRootSites using
	/// SimpleRootSite.PrintWithErrorHandling()
	/// </summary>
	public interface IPrintRootSite
	{
		/// <summary>
		/// Prints the given document
		/// </summary>
		void Print(PrintDocument pd);
	}
}