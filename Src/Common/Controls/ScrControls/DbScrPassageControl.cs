// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2003' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DbScrPassageControl.cs
// --------------------------------------------------------------------------------------------
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SILUBS.SharedScrControls;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for ScrPassageControl.
	/// </summary>
	public class DbScrPassageControl : ScrPassageControl
	{
		#region Data members
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the Scripture object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScripture ScriptureObject { get; private set; }
		#endregion

		#region Construction, Destruction, and initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Non-default constructor
		/// </summary>
		/// <param name="reference">Initial reference</param>
		/// <param name="scr">Scripture project</param>
		/// ------------------------------------------------------------------------------------
		public DbScrPassageControl(ScrReference reference, IScripture scr) :
			base(reference, scr as IScrProjMetaDataProvider, scr.Versification)
		{
			if (DesignMode)
				return;

			ScriptureObject = scr;
			scr.BooksChanged += BooksChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the object that can provide multi-lingual names and abbreviations for
		/// Scripture books.
		/// </summary>
		/// <param name="scrProj">The Scripture project.</param>
		/// <param name="versification">ignored</param>
		/// ------------------------------------------------------------------------------------
		protected override void CreateMultilingScrBooks(IScrProjMetaDataProvider scrProj, ScrVers versification)
		{
			m_mulScrBooks = new DBMultilingScrBooks((IScripture)scrProj);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
				ScriptureObject.BooksChanged -= BooksChanged;
			ScriptureObject = null;
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forces the control to reload the book names based on the current Scripture project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void BooksChanged(ICmObject sender)
		{
			BookLabels = m_mulScrBooks.BookLabels;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Invoke the PassageChanged event
		/// </summary>
		/// <param name="reference">The reference.</param>
		/// ------------------------------------------------------------------------------------
		protected override void InvokePassageChanged(ScrReference reference)
		{
			Logger.WriteEvent(string.Format("New reference is {0}", reference.AsString));
			base.InvokePassageChanged(reference);
		}
		#endregion
	}
}
