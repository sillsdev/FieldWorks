// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IPrintLayoutConfigurer.cs
// Responsibility: TE Team
//
// <remarks>
// IPrintLayoutConfigurer interface.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.PrintLayout
{

	#region IPrintLayoutConfigurer
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// IPrintLayoutConfigurer is implemented by classes that need to configure a print layout.
	/// The idea is to minimize the need to subclass PrintLayout, so that code for a particular
	/// print layout can more easily be shared (e.g., between print layout views and regular
	/// views). The interface also serves to document the required ways that configuration
	/// is expected.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public interface IPrintLayoutConfigurer
	{
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Typically makes zero or more calls to MakeSubordinateView, to add views containing
		/// various kinds of notes.
		/// </summary>
		/// <param name="div"></param>
		/// -------------------------------------------------------------------------------------
		void ConfigureSubordinateViews (DivisionLayoutMgr div);

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and returns the primary view construtor for the main view in the layout.
		/// This is only called once.
		/// </summary>
		/// <param name="div"></param>
		/// -------------------------------------------------------------------------------------
		IVwViewConstructor MakeMainVc(DivisionLayoutMgr div);

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the id of the top-level fragment for the main view (the one used to display
		/// MainObjectId).
		/// </summary>
		/// -------------------------------------------------------------------------------------
		int MainFragment {get;}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the id of the top-level object that the main view displays.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		int MainObjectId {get;}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the stylesheet to use for all views. May be null.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		IVwStylesheet StyleSheet {get;}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the data access object used for all views.
		/// Review JohnT: is this the right way to get this? And, should it be an FdoCache?
		/// I'd prefer not to make PrintLayout absolutely dependent on having an FdoCache,
		/// but usually it will have, and there are things to take advantage of if it does.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		ISilDataAccess DataAccess {get;}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find the ID of the CmObject that matches this guid.
		/// </summary>
		/// <param name="guid">A GUID that represents an object in the cache.</param>
		/// <returns>The ID corresponding to the GUID (the Id field), or zero if the
		/// GUID is invalid.</returns>
		/// <remarks>For DB-based implementations, this is usually implemented by calling
		/// FdoCache.GetIdFromGuid</remarks>
		/// -----------------------------------------------------------------------------------
		int GetIdFromGuid(Guid guid);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an object that can supply view constructors for headers and footers. This is
		/// only called once.
		/// </summary>
		/// <remarks>Most implementations can simply return an instance of the
		/// HeaderFooterConfigurer class. If an implementation returns null, no header or footer
		/// streams will be created for the division.</remarks>
		/// ------------------------------------------------------------------------------------
		IHeaderFooterConfigurer HFConfigurer {get;}

		/// <summary>
		/// If this property is true, ConfigureSubordinateViews will not be called, and there
		/// will be no subordinate streams divided across pages. Instead, a root box will
		/// be created for the dependent objects on each page. If there is more than one
		/// division on each page, the configurer for any of them may be asked to provide the
		/// information needed to initialize it.
		///
		/// In the RootOnEachPage mode, the DLM creates for each page a root box and a
		/// object ID which is the root object for the dependent objects on the page.
		/// The configurer provides a view constructor, a fragment ID, an owner hvo, and a flid.
		/// The DLM will arrange that the IDs of the dependent objects on the page are made the
		/// value of the specified flid of the root object. The VC will be asked to display
		/// the root object using the root fragment, which should somehow display the
		/// specified properties.
		/// </summary>
		bool RootOnEachPage
		{
			get;
		}

		/// <summary>
		/// When RootOnEachPage is true, this obtains the property of the root object
		/// under which the dependent object IDs are stored.
		/// </summary>
		int DependentRootTag
		{
			get;
		}

		/// <summary>
		/// When RootOnEachPage is true, this obtains the hvo of the root object under which the
		/// dependent object is stored
		/// </summary>
		int DependentRootHvo
		{
			get;
		}

		/// <summary>
		/// When RootOnEachPage is true, this obtains the fragment ID that should be used to
		/// display the root object.
		/// </summary>
		int DependentRootFrag
		{
			get;
		}

		/// <summary>
		/// When RootOnEachPage is true, this obtains the VC which is used to display things
		/// in the dependent root view.
		/// </summary>
		IDependentObjectsVc DependentRootVc
		{
			get;
		}
	}
	#endregion

	#region IDependentObjectsVc
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for view constructors that show dependent objects in print layout views
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public interface IDependentObjectsVc : IVwViewConstructor
	{
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the starting dependent object that will be shown.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		int StartingObjIndex { get; set; }

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the ending dependent object that will be shown (inclusive).
		/// </summary>
		/// -------------------------------------------------------------------------------------
		int EndingObjIndex { get; set; }
	}
	#endregion

	#region IHeaderFooterConfigurer
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// IHeaderFooterConfigurer is implemented to provide header and
	/// footer view constructors to a division.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IHeaderFooterConfigurer
	{
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and returns the header view construtor for the layout.
		/// </summary>
		/// <param name="page">Page info</param>
		/// -------------------------------------------------------------------------------------
		IVwViewConstructor MakeHeaderVc(IPageInfo page);

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and returns the footer view construtor for the layout.
		/// </summary>
		/// <param name="page">Page info</param>
		/// -------------------------------------------------------------------------------------
		IVwViewConstructor MakeFooterVc(IPageInfo page);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the hvo of the PubHeader that should be used as the root Id when the header or
		/// footer stream is constructed for the given page.
		/// </summary>
		/// <param name="pageNumber">Used for determining whether this page is first, even, or
		/// odd</param>
		/// <param name="fHeader"><c>true</c> if the id for header is desired; <c>false</c> if
		/// the id for the footer is desired</param>
		/// <param name="fDifferentFirstHF">Indicates whether caller wishes to get a different
		/// header or footer if this is the first page (and if the HF set has a different value
		/// set for the first page)</param>
		/// <param name="fDifferentEvenHF">Indicates whether caller wishes to get a different
		/// header or footer if this is an even page (and if the HF set has a different value
		/// set for even pages)</param>
		/// <returns>the hvo of the requested PubHeader</returns>
		/// ------------------------------------------------------------------------------------
		int GetHvoRoot(int pageNumber, bool fHeader, bool fDifferentFirstHF,
			bool fDifferentEvenHF);

//		/// -------------------------------------------------------------------------------------
//		/// <summary>
//		/// Returns the id of the top-level fragment for the header view (the one used to display
//		/// HeaderObjectId).
//		/// </summary>
//		/// -------------------------------------------------------------------------------------
//		int HeaderFragment {get;}
//
//		/// -------------------------------------------------------------------------------------
//		/// <summary>
//		/// Returns the id of the top-level object that the header view displays.
//		/// </summary>
//		/// -------------------------------------------------------------------------------------
//		int HeaderObjectId {get;}
//
//		/// -------------------------------------------------------------------------------------
//		/// <summary>
//		/// Returns the id of the top-level fragment for the footer view (the one used to display
//		/// FooterObjectId).
//		/// </summary>
//		/// -------------------------------------------------------------------------------------
//		int FooterFragment {get;}
//
//		/// -------------------------------------------------------------------------------------
//		/// <summary>
//		/// Returns the id of the top-level object that the footer view displays.
//		/// </summary>
//		/// -------------------------------------------------------------------------------------
//		int FooterObjectId {get;}

	}
	#endregion

	#region ISubordinateStreamDelegate
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// An instance of this interface is passed to the AddSubordinateStream method.
	/// It implements behaviors (besides those in the VC) that are specific to the
	/// particular view. The PrintLayout makes callbacks to these methods.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public interface ISubordinateStreamDelegate
	{
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// If the object specified by the HVO is one of the reference targets for this
		/// subordinate view, return a 'path' for locating the object in the view.
		/// The path works like the VwSelLevInfo objects passed to IVwRootBox.MakeTextSelection:
		/// that is, the LAST one specifies a property of the root object and an index within
		/// that property (and, in the unlikely event that it is needed, which of several
		/// occurrences of that property); if necessary the next-to-last specifies a property
		/// and index within the object indicated by the last one; and so forth.
		///
		/// If the object specified by the HVO is not a reference target for this view at all
		/// (perhaps it is a different kind of note), return null.
		///
		/// Note that the NLevelOwnerSvd class can be used if the object is consistently found
		/// at a certain depth in the view and the properties involves are owning ones.
		/// </summary>
		/// <param name="hvo">The id of the object whose path is needed</param>
		/// <returns></returns>
		/// -------------------------------------------------------------------------------------
		SelLevInfo[] GetPathToObject(int hvo);
	}
	#endregion

	#region NLevelOwnerSvd implementation of ISubordinateStreamDelegate
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// This implmenetation of ISubordinateStreamDelegate may be used when the referenced
	/// objects occur one or more levels from the root object of the view,
	/// and the intervening properties are all owning. It generates VwSelLevInfo
	/// objects by finding the owner and owning property of the specified HVO,
	/// and searching it to find the position; if there are more levels it repeats.
	/// If it reaches its maximum number of levels without finding the correct root
	/// object, it returns null, indicating that the target object is not present in
	/// this subordinate view.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public class NLevelOwnerSvd : ISubordinateStreamDelegate
	{
		private int m_maxLevels;
		private ISilDataAccess m_sda;
		private int m_hvoRoot;
		/// <summary>
		/// A map from database tags to virtual tags used in filtering.
		/// </summary>
		private Dictionary<int, int> m_tagMap = new Dictionary<int,int>();

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Create one for a particular number of levels and particular data access object.
		/// </summary>
		/// <param name="nLevels">The max number of VwSelLevInfo objects to create</param>
		/// <param name="sda">DA for finding the owner etc.</param>
		/// <param name="hvoRoot">The root object that should be found in the ownership
		/// chain of referenced objects in this view.</param>
		/// -------------------------------------------------------------------------------------
		public NLevelOwnerSvd(int nLevels, ISilDataAccess sda, int hvoRoot)
		{
			m_maxLevels = nLevels;
			m_sda = sda;
			m_hvoRoot = hvoRoot;
		}

		#region ISubordinateStreamDelegate Members
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// If the object specified by the HVO is one of the reference targets for this
		/// subordinate view, return a 'path' for locating the object in the view.
		/// The path works like the VwSelLevInfo objects passed to IVwRootBox.MakeTextSelection:
		/// that is, the LAST one specifies a property of the root object and an index within
		/// that property.
		///
		/// If the object specified by the HVO is not a reference target for this view at all
		/// (perhaps it is a different kind of note), return null.
		/// </summary>
		/// <param name="hvo">The id of the object whose path is needed</param>
		/// <returns>Selection level info identifying where the object is in the subordinate
		/// stream</returns>
		/// -------------------------------------------------------------------------------------
		public SelLevInfo[] GetPathToObject(int hvo)
		{
			List<SelLevInfo> rgSelLevInfo = new List<SelLevInfo>(m_maxLevels);
			// Loop over multiple owners up to m_hvoRoot and create a VwLevInfo for each level.
			int hvoCurr = hvo;
			for (int iLev = 0; iLev < m_maxLevels; iLev++)
			{
				int hvoOwner = m_sda.get_ObjectProp(hvoCurr,
					(int)CmObjectFields.kflidCmObject_Owner);
				int tag = m_sda.get_IntProp(hvoCurr,
					(int)CmObjectFields.kflidCmObject_OwnFlid);
				// use filtered tag if one has been provided.
				if (m_tagMap.ContainsKey(tag))
					tag = m_tagMap[tag];

				// Search for hvoCurr in the owning property to determine its index.
				// Todo: handle non-sequence property case.
				int chvo = m_sda.get_VecSize(hvoOwner, tag);
				int i;
				for (i = 0; i < chvo; i++)
				{
					if (m_sda.get_VecItem(hvoOwner, tag, i) == hvoCurr)
						break;
				}
				Debug.Assert(i < chvo);

				SelLevInfo selLevInfo = new SelLevInfo();
				selLevInfo.ihvo = i;
				selLevInfo.tag = tag;
				selLevInfo.cpropPrevious = 0;
				rgSelLevInfo.Add(selLevInfo);

				if (hvoOwner == m_hvoRoot)
					return rgSelLevInfo.ToArray();
				hvoCurr = hvoOwner;
			}
			return null; // Didn't find the root within the maximum number of levels, so give up.
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a relation between a database tag and a virtual tag used for filtering.
		/// </summary>
		/// <param name="originalTag"></param>
		/// <param name="filteredTag"></param>
		/// ------------------------------------------------------------------------------------
		public void AddTagLookup(int originalTag, int filteredTag)
		{
			m_tagMap[originalTag] = filteredTag;
		}
	}
	#endregion
}
