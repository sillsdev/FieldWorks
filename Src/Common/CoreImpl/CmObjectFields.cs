// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// --------------------------------------------------------------------------------------------

namespace SIL.CoreImpl
{
	/// <summary>
	/// Provides enums for predefined CmObject fields.
	///</summary>
	public enum CmObjectFields
	{
		/// <summary> We start high enough to allow the CM to add CmObject fields. </summary>
		kflidCmObject_Id = 100,

		/// <summary> </summary>
		kflidCmObject_Guid,

		/// <summary> </summary>
		kflidCmObject_Class,

		/// <summary> </summary>
		kflidCmObject_Owner,

		/// <summary> </summary>
		kflidCmObject_OwnFlid,

		/// <summary> </summary>
		kflidCmObject_OwnOrd,

		/// <summary>
		/// flids larger than this are considered dummies, and it is not an error if we don't
		/// find information about them in the database.
		/// </summary>
		/// <remarks>Note: FwMetaDataCache::GetFieldType knows this value, though it does not
		/// use the constant here because (to reduce cyclic dependencies) it does not include
		/// this header.
		/// Note: currently any flid >= kflidDummyFlids is interpreted as a dummy. I (JohnT)
		/// recommend that we limit ourselves to dummies in the 1000 domain (1000000000 to
		/// 1000999999) just on the offchance that one day we want more than 1000 domains.
		/// </remarks>
		kflidStartDummyFlids = 1000000000,
	}
}
