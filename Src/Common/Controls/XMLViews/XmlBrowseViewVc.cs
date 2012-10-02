// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlBrowseViewVc.cs
// Responsibility: WordWorks
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Xml;
using System.Drawing;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils; // for OleCnvt
using SIL.FieldWorks.Resources; // for check-box icons.


namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// View constructor for BrowseView. The argument XmlNode represents XML like this:
	/// <browseview>
	///		<columns>
	///			<column width="10%" label="MyColumn">
	///
	///			</column>
	///			...
	///		</columns>
	///		<fragments>
	///			<frag>...</frag>...
	///		</fragments>
	/// </browseview>
	/// The body of each <column></column> node is the same as that of a <frag></frag>
	/// node, and represents what will be shown for each column for each object in the list.
	/// The additional <fragments></fragments> are passed to the base constructor,
	/// for use in interpreting any frag arguments in elements of the columns.
	/// No fragment should be marked as a root.
	/// Fragment 100000 is the root.
	/// </summary>
	public class XmlBrowseViewVc : XmlBrowseViewBaseVc
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="xnSpec"></param>
		/// <param name="fakeFlid"></param>
		/// <param name="stringTable"></param>
		/// <param name="xbv"></param>
		public XmlBrowseViewVc(XmlNode xnSpec, int fakeFlid, StringTable stringTable, XmlBrowseViewBase xbv)
			: base(xnSpec, fakeFlid, stringTable, xbv)
		{
		}
	}
}
