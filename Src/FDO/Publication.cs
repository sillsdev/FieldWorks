// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Publication.cs
// Responsibility: FieldWorks Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;

using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.FDO.Cellar
{
	/// <summary>
	/// Publication class coded manually to add some static methods.
	/// </summary>
	public partial class Publication
	{
		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the publication with a requested name.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="publicationName">Name of publication</param>
		/// <returns>found publication or null</returns>
		/// ------------------------------------------------------------------------------------
		public static IPublication FindByName(FdoCache cache, string publicationName)
		{
			foreach (IPublication pub in cache.LangProject.TranslatedScriptureOA.PublicationsOC)
				if (pub.Name == publicationName)
					return pub;

			return null;
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets whether the publication is left bound.
		/// REVIEW: When we start supporting top-binding, this property might not serve our
		/// needs anymore.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsLeftBound
		{
			get { return BindingEdge == 0; }
			set { BindingEdge = value ? BindingSide.Left : BindingSide.Right; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the thickness of the footnote separator line, in millipoints.
		/// TODO: This should be added to the model and be configurable in Page Setup Dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FootnoteSepThickness
		{
			get { return FootnoteSepWidth > 0 ? 1000 : 0; }
		}
		#endregion
	}
}