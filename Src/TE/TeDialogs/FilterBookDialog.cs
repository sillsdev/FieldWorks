// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FilterBookDialog.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.ScriptureUtils;
using Microsoft.Win32;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Summary description for FilterBookDialog.
	/// </summary>
	public class FilterBookDialog : FilterScriptureDialog
	{
		#region Constructor/Destructor
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FilterBookDialog"/> class.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoList">A list of books to check as an array of hvos</param>
		/// -----------------------------------------------------------------------------------
		public FilterBookDialog(FdoCache cache, int[] hvoList) : base (cache, hvoList)
		{
			m_helpTopicId = "khtpBookFilter";
			this.Text = DlgResources.ResourceString("kstidBookFilterCaption");
		}

		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a list of HVO values for all of the included books.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int[] GetListOfIncludedBooks()
		{
			CheckDisposed();
			return GetListOfIncludedScripture();
		}
		#endregion
	}
}
