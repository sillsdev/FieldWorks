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
// File: InMemoryFdoCache.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using NMock;
using NMock.Constraints;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Notebk;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.CacheLight;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region NewCacheBase class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This cache base implements some additional methods required by NewFdoCache to allow
	/// tests without using a real underlying database.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class NewCacheBase : CacheBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CacheBase"/> class.
		/// </summary>
		/// <param name="mdc"></param>
		/// ------------------------------------------------------------------------------------
		public NewCacheBase(IFwMetaDataCache mdc)
			: base(mdc)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hash table
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Hashtable Hashtable
		{
			get
			{
				CheckDisposed();
				return m_htCache;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the timestamp
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public DateTime GetTimeStamp(object key)
		{
			CheckDisposed();
			return m_htCache.GetTimeStamp(key);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the timestamp
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// ------------------------------------------------------------------------------------
		public void SetTimeStamp(object key, DateTime value)
		{
			CheckDisposed();
			m_htCache.SetTimeStamp(key, value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the last assigned HVO
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int LastHvo
		{
			get
			{
				CheckDisposed();
				return s_lastHvo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a property from the cache.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override object this[CacheKey key]
		{
			get
			{
				CheckDisposed();
				return base[key];
			}
			set
			{
				CheckDisposed();
				if (m_acth != null)
				{
					InMemoryUndoAction undoAct = new InMemoryUndoAction(this);
					undoAct.AddUndo(key, base[key]);
					undoAct.AddRedo(key, value);
					m_acth.AddAction(undoAct);
				}
				base[key] = value;
			}
		}
	}
	#endregion

	#region NewFdoCache class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The New FdoCache will implement some of the methods of FdoCache
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class NewFdoCache : FdoCache
	{
		#region Overridden methods of FdoCache
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="classID"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int CreateObject(int classID)
		{
			CheckDisposed();
			return ((CacheBase)m_odde).NewHvo(classID);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the date and time from the cache.
		/// </summary>
		/// <param name="hvoOwner">Id of the owning object.</param>
		/// <param name="flidProperty">Flid that has the date and time.</param>
		/// <returns>The date and time from the database.</returns>
		/// ------------------------------------------------------------------------------------
		public override DateTime GetTimeProperty(int hvoOwner, int flidProperty)
		{
			CheckDisposed();
			return new DateTime(((NewCacheBase)m_odde).get_TimeProp(hvoOwner, flidProperty));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the date and time on a database object.
		/// </summary>
		/// <param name="hvoOwner">Id of the owning object.</param>
		/// <param name="flidProperty">Flid that gets updated.</param>
		/// <param name="dtValue">New date and time value.</param>
		/// ------------------------------------------------------------------------------------
		public override void SetTimeProperty(int hvoOwner, int flidProperty, DateTime dtValue)
		{
			CheckDisposed();
			((NewCacheBase)m_odde).SetTime(hvoOwner, flidProperty, dtValue.Ticks);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Since we don't have a real database we don't have to synchronize between multiple
		/// dbs
		/// </summary>
		/// <param name="appGuid"></param>
		/// <param name="sync"></param>
		/// ------------------------------------------------------------------------------------
		public override void StoreSync(Guid appGuid, SyncInfo sync)
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't do anything here
		/// </summary>
		/// <param name="hvoObj"></param>
		/// ------------------------------------------------------------------------------------
		public override void LoadBasicObjectInfo(int hvoObj)
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the object <paramref name="hvoSrc"/> to the field
		/// <paramref name="flidDestOwner"/> owned by <paramref name="hvoDestOwner"/>
		/// </summary>
		/// <param name="hvoSrc">The object to copy</param>
		/// <param name="hvoDestOwner">The new owner</param>
		/// <param name="flidDestOwner">The field in which to copy</param>
		/// <param name="hvoDstStart">The ID of the object before which the copied object will
		/// be inserted, for owning sequences. This must be -1 for fields that are not owning
		/// sequences. If -1 for owning sequences, the object will be appended to the list.
		/// </param>
		/// <returns>HVO of the new copied object</returns>
		/// ------------------------------------------------------------------------------------
		public override int CopyObject(int hvoSrc, int hvoDestOwner, int flidDestOwner,
			int hvoDstStart)
		{
			CheckDisposed();
			return ((CacheBase)m_odde).CopyObject(hvoSrc, hvoDestOwner, flidDestOwner,
				hvoDstStart);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a list of all translation writing systems used for the specified paragraph
		/// in Scripture.
		/// </summary>
		/// <param name="hvoPara">The hvo of the specified paragraph</param>
		/// <returns>array list of translation writing system HVOs</returns>
		/// ------------------------------------------------------------------------------------
		public override List<int> GetUsedScriptureTransWsForPara(int hvoPara)
		{
			// This override is hopefully temporary until TE-5047 is complete.

			// For now we return all writing systems that could possibly be used for a translation!
			List<int> wsEncodingList = new List<int>();
			foreach (LgWritingSystem lgws in LanguageEncodings)
				wsEncodingList.Add(lgws.Hvo);
			return wsEncodingList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a list of all back translation writing systems used for Scripture.
		/// </summary>
		/// <returns>
		/// array list of back translation writing system HVOs
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override List<int> GetUsedScriptureBackTransWs()
		{
			List<int> wsEncodingList = new List<int>();
			// for now we just return the default analysis WS
			wsEncodingList.Add(DefaultAnalWs);
			return wsEncodingList;
		}
		#endregion

		#region Additional getter/setter methods for testing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the data access object used for testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ISilDataAccess DataAccess
		{
			set
			{
				CheckDisposed();
				m_odde = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the meta data cache object used for testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IFwMetaDataCache MetaDataCache
		{
			set
			{
				CheckDisposed();
				m_mdc = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the writing system factory used for testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILgWritingSystemFactory WritingSystemFactory
		{
			set
			{
				CheckDisposed();
				m_lef = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the language project
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetLangProject(LangProject lp)
		{
			CheckDisposed();
			m_lp = lp;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the scripture reference system
		/// </summary>
		/// <param name="srs"></param>
		/// ------------------------------------------------------------------------------------
		public void SetScriptureReferenceSystem(IScrRefSystem srs)
		{
			CheckDisposed();
			m_srs = srs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the language encodings
		/// </summary>
		/// <param name="wsc"></param>
		/// ------------------------------------------------------------------------------------
		public void SetLanguageEncodings(LgWritingSystemCollection wsc)
		{
			CheckDisposed();
			m_cle = wsc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the userview collection
		/// </summary>
		/// <param name="uvc"></param>
		/// ------------------------------------------------------------------------------------
		public void SetUserViewSpecs(UserViewCollection uvc)
		{
			CheckDisposed();
			m_cuv = uvc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the action handler. This property is provided so that we can use a mocked
		/// action handler.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IActionHandler ActionHandler
		{
			set
			{
				CheckDisposed();
				base.SetActionHandler(value);
			}
		}
		#endregion

		#region Debug methods - dump cache content in debugger
#if DEBUG
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dumps the content of the cache. This can be called from the Watch window in the
		/// debugger.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DumpCache()
		{
			CheckDisposed();
			Debug.WriteLine("************** Start of Cache Dump *****************");
			foreach (DictionaryEntry entry in ((NewCacheBase)m_odde).Hashtable)
			{
				CacheKey key = entry.Key as CacheKey;
				string other = string.Empty;
				if (entry.Key is CacheKeyEx)
				{
					CacheKeyEx keyEx = entry.Key as CacheKeyEx;
					other = string.Format(", other={0}", keyEx.Other);
				}

				int classIdOfValue = -1;
				string className = "non-int";
				if (entry.Value is int && (int)entry.Value > 0)
				{
					classIdOfValue = GetClassOfObject((int)entry.Value);
					className = GetClassName((uint)classIdOfValue);
				}

				Debug.WriteLine(string.Format("hvo={0} ({1}), tag={2}{3}, value={4} ({5}/{6}), {7}",
					key.Hvo, GetClassOfObject(key.Hvo), key.Tag, other, DumpValue(entry.Value),
					classIdOfValue > 0 ? classIdOfValue.ToString() : "", className,
					((NewCacheBase)m_odde).GetTimeStamp(key)));
			}
			Debug.WriteLine("************** End of Cache Dump *****************");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dumps value of object, especially int arrays
		/// </summary>
		/// <param name="value"></param>
		/// <returns>String to print</returns>
		/// ------------------------------------------------------------------------------------
		private string DumpValue(object value)
		{
			if (value is int[])
			{
				StringBuilder bldr = new StringBuilder();
				bldr.Append("(");
				foreach (int i in (int[])value)
				{
					if (bldr.Length > 1)
						bldr.Append(", ");
					bldr.Append(i.ToString());
				}
				bldr.Append(")");
				return bldr.ToString();
			}

			return value.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dumps the content of the cache. This can be called from the Watch window in the
		/// debugger.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DumpCacheTree()
		{
			CheckDisposed();
			Debug.WriteLine("************** Start of Cache Tree Dump *****************");
			for (int hvo = 10001; hvo <= ((NewCacheBase)m_odde).LastHvo; hvo++)
			{
				try
				{
					if (GetOwnerOfObject(hvo) == 0)
					{
						// dump out ownerless object
						PrintObject(hvo);
					}
				}
				catch
				{
					// just ignore all errors
					Debug.IndentLevel = 0;
				}
			}
			Debug.WriteLine("************** End of Cache Tree Dump *****************");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dumps one object
		/// </summary>
		/// <param name="hvo"></param>
		/// ------------------------------------------------------------------------------------
		private void PrintObject(int hvo)
		{
			if (hvo <= 0)
			{
				Debug.WriteLine("<null>");
				return;
			}
			Debug.WriteLine("");

			int classId = GetClassOfObject(hvo);
			string className = GetClassName((uint)classId);
			int owner = GetOwnerOfObject(hvo);
			int ownFlid = GetOwningFlidOfObject(hvo);
			int ownOrd = m_odde.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_OwnOrd);
			Debug.WriteLine(string.Format("{0}: hvo={1}, owner={2}, flid={3}, ownOrd={4}", className, hvo, owner,
				ownFlid, ownOrd));
			Debug.WriteLine("Fields:");
			Debug.Indent();
			foreach (ClassAndPropInfo field in GetFieldsOfClass((uint)classId))
			{
				Debug.Write(field.fieldName + " ");
				int flid = (int)field.flid;
				int[] childHvos;
				switch ((FieldType)field.fieldType)
				{
					case FieldType.kcptOwningAtom:
						PrintObject(m_odde.get_IntProp(hvo, flid));
						break;
					case FieldType.kcptOwningCollection:
					case FieldType.kcptOwningSequence:
						childHvos = (int[])((CacheBase)m_odde).Get(hvo, flid);
						if (childHvos == null)
							Debug.WriteLine("<null>");
						else
						{
							Debug.Indent();
							foreach (int hvoChild in childHvos)
								PrintObject(hvoChild);
							Debug.Unindent();
						}
						break;
					case FieldType.kcptReferenceAtom:
						Debug.WriteLine(string.Format("- References (Atomic) {0}", m_odde.get_ObjectProp(hvo, flid)));
						break;
					case FieldType.kcptReferenceCollection:
					case FieldType.kcptReferenceSequence:
						childHvos = (int[])((CacheBase)m_odde).Get(hvo, flid);
						if (childHvos == null)
							Debug.WriteLine("<null>");
						else
						{
							Debug.WriteLine("");
							Debug.Indent();
							Debug.Write(string.Format("References ({0}) ", field.fieldType));
							foreach (int hvoChild in childHvos)
								Debug.Write(string.Format("{0}, ", hvoChild));
							Debug.WriteLine("");
							Debug.Unindent();
						}
						break;
					default:
						Debug.WriteLine(string.Format("\"{0}\"", ((CacheBase)m_odde).Get(hvo, flid)));
						break;
				}
			}
			Debug.Unindent();
		}
#endif
		#endregion

		#region GetLinkedObj related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Wraps the GetLinkedObjs$ stored procedure.
		/// </summary>
		/// <param name="ids">Array of ids to process.</param>
		/// <param name="linkedObjectType">Mask that indicates what types of related objects
		/// should be retrieved.</param>
		/// <param name="fIncludeBaseClasses">A flag that determines if the base classes of owned
		/// objects are included in the object list (e.g., rows for each object + all
		/// superclasses except CmObject. So if a CmPerson is included, it will also have a row
		/// for CmPossibility)</param>
		/// <param name="fIncludeSubClasses">A flag that determines if the sub classes of owned
		/// objects are included in the object list.</param>
		/// <param name="fRecurse">A flag that determines if the owning tree is traversed.
		/// </param>
		/// <param name="referenceDirection">Determines which reference directions will be
		/// included in the results.</param>
		/// <param name="filterByClass">only return objects of this class (including subclasses
		/// of this class). Zero (0) returns all classes.</param>
		/// <param name="fCalculateOrderKey">A flag that determines if the order key is
		/// calculated.</param>
		///
		/// <returns>A generic list that contains zero, or more, LinkedObjectInfo objects.</returns>
		/// <remarks>
		/// The <b>ids</b> parameter handles the first two parameters in the actual stored
		/// procedure (@ObjId and @hXMLDocObjList).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override List<LinkedObjectInfo> GetLinkedObjects(List<int> ids, LinkedObjectType linkedObjectType,
			bool fIncludeBaseClasses, bool fIncludeSubClasses, bool fRecurse,
			ReferenceDirection referenceDirection, int filterByClass, bool fCalculateOrderKey)
		{
			CheckDisposed();
			List<LinkedObjectInfo> result = new List<LinkedObjectInfo>();
			Hashtable ht = ((NewCacheBase)m_odde).Hashtable;

			if (linkedObjectType == LinkedObjectType.Reference ||
				linkedObjectType == LinkedObjectType.OwningAndReference)
			{
				if (referenceDirection == ReferenceDirection.Inbound
					|| referenceDirection == ReferenceDirection.InboundAndOutbound)
				{
					// at the moment, we expect this method to get called only by
					// CmObject.BackReferences or CmObject.LinkedObjects.
					Debug.Assert(fCalculateOrderKey == false);
					Debug.Assert(filterByClass == 0);
					Debug.Assert(fIncludeBaseClasses == true);
					// NOTE: cacheEntry is a System.Collections.DictionaryEntry, which has NOTHING to do with
					// linguistic lexicon stuff!
					foreach (DictionaryEntry cacheEntry in ht)
					{
						CacheKey key = cacheEntry.Key as CacheKey;
						bool fOwningProp = IsOwningProperty(key.Tag);
						bool fFoundLinkedObj = false;
						if (cacheEntry.Value is int && ids.Contains((int)cacheEntry.Value))
						{
							fFoundLinkedObj = true;
							int hvoVal = (int)cacheEntry.Value;
							foreach (int id in ids)
							{
								if (id == hvoVal)
								{
									if (fOwningProp && key.Hvo == GetOwnerOfObject(id))
										fFoundLinkedObj = false;
									break;
								}
							}
							if (fFoundLinkedObj)
								SaveLinkedObjInfo(hvoVal, key, linkedObjectType, result);
						}
						else if (cacheEntry.Value is int[])
						{
							ArrayList hvos = new ArrayList((int[])cacheEntry.Value);
							foreach (int id in ids)
							{
								if (hvos.Contains(id) && !(fOwningProp && key.Hvo == GetOwnerOfObject(id)))
								{
									SaveLinkedObjInfo(id, key, linkedObjectType, result);
								}
							}
						}
					}
				}
				return result;
			}
			throw new NotImplementedException("Not yet implemented in NewFdoCache");
		}

		private void SaveLinkedObjInfo(int hvoObj, CacheKey key, LinkedObjectType linkedObjectType, List<LinkedObjectInfo> result)
		{
			LinkedObjectInfo loi = GetLinkedObjectInfo(hvoObj, key.Hvo,
				key.Tag, linkedObjectType);
			if (loi != null)
				result.Add(loi);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if the hash entry with the given tag links to an HVO and not
		/// to a class, field, guid or OwnOrd.
		/// </summary>
		/// <param name="tag">Tag</param>
		/// <returns><c>true</c> if linked to an HVO, <c>false</c> if linked to a guid, class,
		/// flid or OwnOrd.</returns>
		/// ------------------------------------------------------------------------------------
		private bool LinkedToHvo(int tag)
		{
			return tag != (int)CmObjectFields.kflidCmObject_Guid &&
				tag != (int)CmObjectFields.kflidCmObject_Class &&
				tag != (int)CmObjectFields.kflidCmObject_OwnFlid &&
				tag != (int)CmObjectFields.kflidCmObject_OwnOrd;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks wether to include the object or not
		/// </summary>
		/// <param name="desiredTypes">The desired types of objects to include</param>
		/// <param name="objType">The type of the field</param>
		/// <returns><c>true</c> to include this object, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool IncludeObject(LinkedObjectType desiredTypes, FieldType objType)
		{
			return (desiredTypes == LinkedObjectType.OwningAndReference
				||
				(desiredTypes == LinkedObjectType.Reference &&
				(objType == FieldType.kcptReferenceAtom ||
				objType == FieldType.kcptReferenceCollection ||
				objType == FieldType.kcptReferenceSequence))
				||
				(desiredTypes == LinkedObjectType.Owning &&
				(objType == FieldType.kcptOwningAtom ||
				objType == FieldType.kcptOwningCollection ||
				objType == FieldType.kcptOwningSequence)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gather the information for the LinkedObjectInfo and return that
		/// </summary>
		/// <param name="hvoObj">Hvo of the object</param>
		/// <param name="hvoRel">Hvo of the related object</param>
		/// <param name="flidRel">Field id of the object in the related object</param>
		/// <param name="linkedObjectType"></param>
		/// <returns>LinkedObjectInfo object</returns>
		/// ------------------------------------------------------------------------------------
		private LinkedObjectInfo GetLinkedObjectInfo(int hvoObj, int hvoRel, int flidRel,
			LinkedObjectType linkedObjectType)
		{
			if (hvoRel <= 0 || flidRel == (int)CmObjectFields.kflidCmObject_Owner)
				return null;

			LinkedObjectInfo loi = new LinkedObjectInfo();
			loi.RelObjField = flidRel;
			FieldType type = GetFieldType(flidRel);
			loi.RelType = (int)type;
			if (!IncludeObject(linkedObjectType, type))
				return null;

			loi.ObjId = hvoObj;
			loi.ObjClass = GetClassOfObject(hvoObj);
			loi.OwnerDepth = 0;
			loi.RelObjId = hvoRel;
			loi.RelObjClass = GetClassOfObject(hvoRel);

			loi.RelOrder = m_odde.get_IntProp(hvoObj,
				(int)CmObjectFields.kflidCmObject_OwnOrd);

			// not doing the OrdKey calculation for now
			loi.OrdKey = null;
			return loi;
		}
		#endregion GetLinkedObj
	}
	#endregion

	#region ArbitrarySequenceVh class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A simple virtual handler that allows us to store a sequence of CmObjects.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ArbitrarySequenceVh : BaseVirtualHandler
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No-op (at least for now)
		/// </summary>
		/// <param name="hvo">Object having the virtual property</param>
		/// <param name="tag">Virtual flid</param>
		/// <param name="ws">Not used</param>
		/// <param name="cda">The cache DA</param>
		/// ------------------------------------------------------------------------------------
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			//ISilDataAccess sda = (ISilDataAccess)cda;
			//cda.CacheVecProp(hvo, tag, sda.get_VecSize(hvo,
			//    (int)StText.StTextTags.kflidParagraphs));
		}
	}
	#endregion

	#region InMemoryFdoCache class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class that mocks FdoCache. It creates a cache without database connection.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InMemoryFdoCache : IFWDisposable
	{
		#region Struct for writing systems
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hvos used for writing systems
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public struct HvoWS
		{
			/// <summary>English</summary>
			public int En;
			/// <summary>Spanish</summary>
			public int Es;
			/// <summary>French</summary>
			public int Fr;
			/// <summary>German</summary>
			public int De;
			/// <summary>IPA</summary>
			public int Ipa;
			/// <summary>Kalaba</summary>
			public int XKal;
			/// <summary>Urdu</summary>
			public int Ur;
			/// <summary>Koine Greek</summary>
			public int Grc;
			/// <summary>Ancient Hebrew</summary>
			public int Hbo;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Define IDs of writing systems.
			/// </summary>
			/// <param name="initialValues"></param>
			/// --------------------------------------------------------------------------------
			public HvoWS(int initialValues)
			{
				En = Es = Fr = De = Ipa = XKal = Ur = Grc = Hbo = initialValues;
			}
		}
		#endregion

		#region Member variables
		/// <summary>Hvos for the WSs (initialized when the Cache is created)</summary>
		public static HvoWS s_wsHvos = new HvoWS(-1);
		/// <summary></summary>
		protected FdoCache m_fdoCache;
		/// <summary></summary>
		protected CacheBase m_cacheBase;

		// These have to be a member variable so that it doesn't go out of scope prematurely
		/// <summary></summary>
		protected LangProject m_lp;
		/// <summary></summary>
		protected DynamicMock m_acth;
		/// <summary>Contains the modules that have meta data loaded. This is used to
		/// make so that we can test the initialization in several places before we call
		/// methods that require that data.</summary>
		protected List<string> m_metaDataModulesLoaded = new List<string>();

		/// <summary></summary>
		public CmPossibility m_categoryDiscourse;
		/// <summary></summary>
		public CmPossibility m_categoryGrammar;
		/// <summary></summary>
		public CmPossibility m_categoryGrammar_PronominalRef;
		/// <summary></summary>
		public CmPossibility m_categoryGrammar_PronominalRef_ExtendedUse;
		/// <summary></summary>
		public CmPossibility m_categoryGnarly;

		/// <summary></summary>
		public CmAnnotationDefn m_consultantNoteDefn;
		/// <summary></summary>
		public CmAnnotationDefn m_translatorNoteDefn;

		/// <summary></summary>
		protected static string[] m_SIL_BookCodes = new string[]
		{
			"",	// 0th entry is invalid, book indices are 1-based
			"GEN", "EXO", "LEV", "NUM", "DEU", "JOS", "JDG", "RUT", "1SA", "2SA",
			"1KI", "2KI", "1CH", "2CH", "EZR", "NEH", "EST", "JOB", "PSA", "PRO",
			"ECC", "SNG", "ISA", "JER", "LAM", "EZK", "DAN", "HOS", "JOL", "AMO",
			"OBA", "JON", "MIC", "NAM", "HAB", "ZEP", "HAG", "ZEC", "MAL", "MAT",
			"MRK", "LUK", "JHN", "ACT", "ROM", "1CO", "2CO", "GAL", "EPH", "PHP",
			"COL", "1TH", "2TH", "1TI", "2TI", "TIT", "PHM", "HEB", "JAS", "1PE",
			"2PE", "1JN", "2JN", "3JN", "JUD", "REV"
		};
		#endregion

		#region Construction
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates and initializes new instance of the <see cref="InMemoryFdoCache"/> class.
		/// </summary>
		/// <param name="wsFactProvider"></param>
		/// -----------------------------------------------------------------------------------
		protected InMemoryFdoCache(IWsFactoryProvider wsFactProvider)
		{
			m_fdoCache = new NewFdoCache();
			m_fdoCache.TestMode = true;

			IFwMetaDataCache metaCache = new MetaDataCache();
			((NewFdoCache)m_fdoCache).MetaDataCache = metaCache;
			LoadMetaData("Cellar");
			LoadMetaData("LangProj");
			LoadMetaData("Ling");

			// create the DB-less cache
			m_cacheBase = new NewCacheBase(metaCache);
			((NewFdoCache)m_fdoCache).DataAccess = m_cacheBase;

			// create a LgWritingSystemFactory and setup the default WSs
			ILgWritingSystemFactory wsFactory = wsFactProvider.NewILgWritingSystemFactory;
			// We don't want InstallLanguage being called in these tests.
			wsFactory.BypassInstall = true;
			m_cacheBase.WritingSystemFactory = wsFactory;
			// only do this if it hasn't been done yet
			s_wsHvos.En = SetupWs("en");
			s_wsHvos.Es = SetupWs("es");
			s_wsHvos.De = SetupWs("de");
			s_wsHvos.Fr = SetupWs("fr");
			s_wsHvos.Ipa = SetupWs("ipa");
			s_wsHvos.XKal = SetupWs("xkal");
			s_wsHvos.Ur = SetupWs("ur", true);
			s_wsHvos.Grc = SetupWs("grc");
			s_wsHvos.Hbo = SetupWs("hbo", true);

			((NewFdoCache)m_fdoCache).WritingSystemFactory = wsFactory;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the meta data for the given module to the meta data cache.
		/// </summary>
		/// <param name="moduleName"></param>
		/// ------------------------------------------------------------------------------------
		protected void LoadMetaData(string moduleName)
		{
			// Make sure the module only gets loaded once
			if (IsModuleLoaded(moduleName))
			if (m_metaDataModulesLoaded.Contains(moduleName.ToLower()))
				return;

			string fileName = string.Format(@"{0}\xml\{0}.cm", moduleName);
			Cache.MetaDataCacheAccessor.InitXml(Path.Combine(DirectoryFinder.FwSourceDirectory,
				fileName), false);
			m_metaDataModulesLoaded.Add(moduleName.ToLower());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests if the meta data for a module is loaded in the meta data cache.
		/// </summary>
		/// <param name="moduleName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool IsModuleLoaded(string moduleName)
		{
			return m_metaDataModulesLoaded.Contains(moduleName.ToLower());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the cache to know about the specified left-to-right writing system
		/// </summary>
		/// <param name="icuLocale">the icu locale for the writing system</param>
		/// <returns>the hvo of the writing system</returns>
		/// ------------------------------------------------------------------------------------
		public int SetupWs(string icuLocale)
		{
			CheckDisposed();
			return SetupWs(icuLocale, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the cache to know about the specified writing system
		/// </summary>
		/// <param name="icuLocale">the icu locale for the writing system</param>
		/// <param name="rightToLeft">true if the writing system is right-to-left</param>
		/// <returns>the hvo of the writing system</returns>
		/// ------------------------------------------------------------------------------------
		public int SetupWs(string icuLocale, bool rightToLeft)
		{
			CheckDisposed();

			int hvo = m_cacheBase.WritingSystemFactory.get_Engine(icuLocale).WritingSystem;
			m_cacheBase.SetBoolean(hvo, (int)LgWritingSystem.LgWritingSystemTags.kflidRightToLeft,
				rightToLeft);

			m_cacheBase.SetInt(hvo, (int)CmObjectFields.kflidCmObject_Class,
				LgWritingSystem.kClassId);
			Cache.SetUnicodeProperty(hvo,
				(int)LgWritingSystem.LgWritingSystemTags.kflidICULocale, icuLocale);
			return hvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new in-memory FdoCache. Sets default analysis writing system to English and
		/// default vernacular writing system to French. Hvo for language project is set to
		/// 1.</summary>
		/// <returns>In memory FdoCache object</returns>
		/// ------------------------------------------------------------------------------------
		public static InMemoryFdoCache CreateInMemoryFdoCache()
		{
			return new InMemoryFdoCache(new DefaultWsFactoryProvider());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new in-memory FdoCache. Sets default analysis writing system to English and
		/// default vernacular writing system to French. Hvo for language project is set to
		/// 1.</summary>
		/// <returns>In-memory FdoCache object</returns>
		/// ------------------------------------------------------------------------------------
		public static InMemoryFdoCache CreateInMemoryFdoCache(IWsFactoryProvider provider)
		{
			return new InMemoryFdoCache(provider);
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the internal cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CacheBase CacheAccessor
		{
			get
			{
				CheckDisposed();
				return m_cacheBase;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the FdoCache object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_fdoCache;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the mock action handler object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DynamicMock MockActionHandler
		{
			get
			{
				CheckDisposed();
				return m_acth;
			}
		}
		#endregion

		#region Language Project
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the language project in the cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeLangProject()
		{
			CheckDisposed();
			int lpHvo = m_cacheBase.NewHvo(LangProject.kClassId);
			m_cacheBase.SetBasicProps(lpHvo, 0, LangProject.kClassId, 0, 0);

			// setup the default WSs
			m_cacheBase.CacheVecProp(lpHvo,
				(int)LangProject.LangProjectTags.kflidCurAnalysisWss,
				new int[] { s_wsHvos.En, s_wsHvos.De, s_wsHvos.Es }, 3);
			m_cacheBase.CacheVecProp(lpHvo,
				(int)LangProject.LangProjectTags.kflidAnalysisWss,
				new int[] { s_wsHvos.En, s_wsHvos.De, s_wsHvos.Es, s_wsHvos.Ipa }, 4);
			m_cacheBase.CacheVecProp(lpHvo,
				(int)LangProject.LangProjectTags.kflidCurVernWss,
				new int[] { s_wsHvos.Fr, s_wsHvos.Ur }, 2);
			m_cacheBase.CacheVecProp(lpHvo,
				(int)LangProject.LangProjectTags.kflidVernWss,
				new int[] { s_wsHvos.Fr, s_wsHvos.Ur }, 2);
			// make the new LP
			m_lp = new LangProject(Cache, lpHvo);

			((NewFdoCache)m_fdoCache).SetLangProject(m_lp);
			Debug.Assert(m_fdoCache.LangProject.CurAnalysisWssRS.Count > 0);
			Debug.Assert(m_fdoCache.LangProject.AnalysisWssRC.Count > 0);

			m_lp.AnthroListOAHvo = m_cacheBase.NewHvo(CmPossibilityList.kClassId);
			m_lp.MsFeatureSystemOAHvo = m_cacheBase.NewHvo(FsFeatureSystem.kClassId);

			CmAgent agent = new CmAgent();
			m_lp.AnalyzingAgentsOC.Add(agent);
			agent.Human = true;
			agent.Guid = LangProject.kguidAgentDefUser;
			agent = new CmAgent();
			m_lp.AnalyzingAgentsOC.Add(agent);
			agent.Human = false;
			agent.Guid = LangProject.kguidAgentM3Parser;
			agent.Version = "Normal";
			agent = new CmAgent();
			m_lp.AnalyzingAgentsOC.Add(agent);
			agent.Human = false;
			agent.Guid = LangProject.kguidAgentComputer;

			// Create the valid Translation Types
			m_lp.TranslationTagsOAHvo = m_cacheBase.NewHvo(CmPossibilityList.kClassId);
			m_cacheBase.SetBasicProps(m_lp.TranslationTagsOAHvo, m_lp.Hvo, (int)CmPossibilityList.kClassId,
				(int)LangProject.LangProjectTags.kflidTranslationTags, 0);
			AddTransType(LangProject.kguidTranBackTranslation);
			AddTransType(LangProject.kguidTranFreeTranslation);
			AddTransType(LangProject.kguidTranLiteralTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a translation type to the list of types that a CmTranslation can be.
		/// </summary>
		/// <param name="type">The GUID that uniquely identifies the translation type</param>
		/// ------------------------------------------------------------------------------------
		private void AddTransType(Guid type)
		{
			CmPossibility transType = new CmPossibility();
			m_lp.TranslationTagsOA.PossibilitiesOS.Append(transType);
			//int hvoTransType = m_cacheBase.NewHvo(CmPossibility.kClassId);
			//CmPossibility transType = new CmPossibility(m_fdoCache, hvoTransType);
			//m_cacheBase.SetBasicProps(hvoTransType, m_lp.TranslationTagsOAHvo, CmPossibility.kClassId,
			//    (int)CmPossibilityList.CmPossibilityListTags.kflidPossibilities, 0);
			transType.Guid = type;
		}
		#endregion

		#region Footnote stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an arbitrary footnote sequence.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoOwningSequence<IStFootnote> CreateArbitraryFootnoteSequence(ICmObject owner)
		{
			uint flid = m_fdoCache.MetaDataCacheAccessor.GetFieldId2((uint)owner.ClassID,
				"DummyFootnotesOS", false);
			if (flid == 0)
			{
				ArbitrarySequenceVh vh = new ArbitrarySequenceVh();
				vh.ClassName = m_fdoCache.MetaDataCacheAccessor.GetClassName((uint)owner.ClassID);
				vh.FieldName = "DummyFootnotesOS";
				vh.Type = (int)CellarModuleDefns.kcptOwningSequence;
				m_cacheBase.InstallVirtual(vh);
				flid = (uint)vh.Tag;
				((MetaDataCache)(m_fdoCache.MetaDataCacheAccessor)).SetDstClsId(flid,
					StFootnote.kClassId);
			}

			return new FdoOwningSequence<IStFootnote>(m_fdoCache, owner.Hvo, (int)flid);
			//m_cacheBase.CacheVecProp(m_lp.Hvo, -478576, new int[0], 0);
			//return new FdoOwningSequence<IStFootnote>(m_fdoCache, m_lp.Hvo, -478576);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a mindless footnote (i.e., it's marker, paragraph style, etc. won't be set)
		/// to a paragraph.
		/// </summary>
		/// <param name="footnoteSequence">Sequence of footnotes into which to insert</param>
		/// <param name="para">the paragraph into which to insert the footnote ORC</param>
		/// <param name="ichPos">the zero-based character offset at which to insert the footnote
		/// ORC into the paragraph</param>
		/// <param name="footnoteText">text for the footnote (no footnote paragraph created if
		/// null)</param>
		/// <returns>the new footnote</returns>
		/// ------------------------------------------------------------------------------------
		public StFootnote AddFootnote(FdoOwningSequence<IStFootnote> footnoteSequence,
			IStTxtPara para, int ichPos, string footnoteText)
		{
			CheckDisposed();
			// Create the footnote
			StFootnote footnote = new StFootnote();
			footnoteSequence.Append(footnote);

			// Update the paragraph contents to include the footnote marker ORC
			ITsStrBldr tsStrBldr = para.Contents.UnderlyingTsString.GetBldr();
			footnote.InsertOwningORCIntoPara(tsStrBldr, ichPos, m_fdoCache.DefaultVernWs);
			para.Contents.UnderlyingTsString = tsStrBldr.GetString();

			if (footnoteText != null)
			{
				// Create the footnote paragraph with the given footnoteText
				StTxtParaBldr paraBldr = new StTxtParaBldr(m_fdoCache);
				paraBldr.ParaProps = StyleUtils.ParaStyleTextProps("Note General Paragraph");
				paraBldr.AppendRun(footnoteText, StyleUtils.CharStyleTextProps(null, m_fdoCache.DefaultVernWs));
				paraBldr.CreateParagraph(footnote.Hvo);
			}

			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a back translation footnote ref ORC in the given translation for the given footnote
		/// </summary>
		/// <param name="trans">The given translation, usually a back translation</param>
		/// <param name="ichPos">The 0-based character offset into the translation string
		/// at which we will insert the reference ORC</param>
		/// <param name="ws">writing system of the ORC</param>
		/// <param name="footnote">The given footnote</param>
		/// ------------------------------------------------------------------------------------
		public void AddFootnoteORCtoTrans(ICmTranslation trans, int ichPos, int ws, StFootnote footnote)
		{
			CheckDisposed();
			// Insert a footnote reference ORC into the given translation string
			ITsStrBldr tsStrBldr = trans.Translation.GetAlternative(ws).UnderlyingTsString.GetBldr();
			footnote.InsertRefORCIntoTrans(tsStrBldr, ichPos, ws);
			trans.Translation.SetAlternative(tsStrBldr.GetString(), ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a back translation footnote in the given translation for the given footnote.
		/// Inserts a ref ORC in the translation and sets the the BT text in the footnote.
		/// </summary>
		/// <param name="trans">The given back translation of an StTxtPara</param>
		/// <param name="ichPos">The 0-based character offset into the back translation string
		/// at which we will insert the reference ORC</param>
		/// <param name="ws">writing system of the BT and ORC</param>
		/// <param name="footnote">The given footnote</param>
		/// <param name="footnoteBtText">text for the back translation of the footnote</param>
		/// <returns>the back translation of the given footnote</returns>
		/// ------------------------------------------------------------------------------------
		public ICmTranslation AddFootnoteORCtoTrans(ICmTranslation trans, int ichPos, int ws,
			StFootnote footnote, string footnoteBtText)
		// TODO: rename this method to AddBtFootnote(), so it reads similar to AddFootnote() in tests
		{
			CheckDisposed();
			AddFootnoteORCtoTrans(trans, ichPos, ws, footnote);

			// Add the given footnote BT text to the footnote.
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			ICmTranslation footnoteTrans = para.GetOrCreateBT();
			ITsStrBldr tssFootnoteBldr = footnoteTrans.Translation.GetAlternative(ws).UnderlyingTsString.GetBldr();
			tssFootnoteBldr.ReplaceRgch(0, 0, footnoteBtText, footnoteBtText.Length,
				StyleUtils.CharStyleTextProps(null, ws));
			footnoteTrans.Translation.SetAlternative(tssFootnoteBldr.GetString(), ws);
			return footnoteTrans;
		}
		#endregion

		#region Structured Text stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an arbitrary structured text (as the Contents of a Text in the Language
		/// Project's Texts collection).
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IStText CreateArbitraryStText()
		{
			IText text = new Text();
			m_lp.TextsOC.Add(text);
			IStText stText = new StText();
			text.ContentsOA = stText;
			return stText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a content paragraph to the specified StText in the mock fdocache
		/// </summary>
		/// <param name="textHvo">the hvo of the StText</param>
		/// <param name="paraStyleName">the paragraph style name</param>
		/// ------------------------------------------------------------------------------------
		public StTxtPara AddParaToMockedText(int textHvo, string paraStyleName)
		{
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			return AddParaToMockedText(textHvo, propFact.MakeProps(paraStyleName, s_wsHvos.Fr, 0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a content paragraph to the specified StText in the mock fdocache
		/// </summary>
		/// <param name="textHvo">the hvo of the StText</param>
		/// <param name="styleRules">the paragraph style props</param>
		/// ------------------------------------------------------------------------------------
		public StTxtPara AddParaToMockedText(int textHvo, ITsTextProps styleRules)
		{
			CheckDisposed();
			StTxtPara para = new StTxtPara(Cache, m_cacheBase.NewHvo(StTxtPara.kClassId));

			// Append the paragraph to the specified book and section
			m_cacheBase.AppendToFdoVector(textHvo, (int)StText.StTextTags.kflidParagraphs,
				para.Hvo);

			// Setup the new paragraph
			m_cacheBase.SetBasicProps(para.Hvo, textHvo, (int)StTxtPara.kClassId,
				(int)StText.StTextTags.kflidParagraphs, 1);
			para.StyleRules = styleRules;
			ITsStrFactory fact = TsStrFactoryClass.Create();
			para.Contents.UnderlyingTsString = fact.MakeString(string.Empty,
				(int)InMemoryFdoCache.s_wsHvos.Fr);

			m_cacheBase.SetGuid(para.Hvo, (int)CmObjectFields.kflidCmObject_Guid,
				Guid.NewGuid());
			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a run of text to the specified paragraph
		/// </summary>
		/// <param name="para"></param>
		/// <param name="runText"></param>
		/// <param name="runStyleName"></param>
		/// ------------------------------------------------------------------------------------
		public void AddRunToMockedPara(StTxtPara para, string runText, string runStyleName)
		{
			CheckDisposed();
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps runStyle = propFact.MakeProps(runStyleName, (int)InMemoryFdoCache.s_wsHvos.Fr, 0);
			TsStringAccessor contents = para.Contents;
			ITsStrBldr bldr = contents.UnderlyingTsString.GetBldr();
			bldr.Replace(bldr.Length, bldr.Length, runText, runStyle);
			contents.UnderlyingTsString = bldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a run of text to the specified paragraph
		/// </summary>
		/// <param name="para"></param>
		/// <param name="runText"></param>
		/// <param name="ws"></param>
		/// ------------------------------------------------------------------------------------
		public void AddRunToMockedPara(StTxtPara para, string runText, int ws)
		{
			CheckDisposed();
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps runStyle = propFact.MakeProps(null, ws, 0);
			TsStringAccessor contents = para.Contents;
			ITsStrBldr bldr = contents.UnderlyingTsString.GetBldr();
			bldr.Replace(bldr.Length, bldr.Length, runText, runStyle);
			contents.UnderlyingTsString = bldr.GetString();
		}
		#endregion

		#region Data Notebook
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize data notebook in the cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeDataNotebook()
		{
			CheckDisposed();
			LoadMetaData("notebk");

			m_lp.ResearchNotebookOAHvo = m_cacheBase.NewHvo(RnResearchNbk.kClassId);
		}
		#endregion

		#region Writing Stystem stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add the writing systems to the collection in the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeWritingSystemEncodings()
		{
			CheckDisposed();
			FdoCache fdoCache = Cache;
			LgWritingSystemCollection wsCollection = new LgWritingSystemCollection(fdoCache);

			// HVO  | ICU Locale | English Name | Spanish Name | French Name | IPA Name |
			// -----+------------+--------------+--------------+-------------+-----------
			// 1001 | en         | English      | inglés       |             |          |
			// 2346 | es         | Spanish      | español      | espagnol    | español  |
			// 4568 | fr         |              | francés      | francais    |          |
			// 2398 | en-ipa     | English IPA  |              |             | aipie    |
			// ???? | de         | German       |              |             | Deutsch  |
			// ???? | xkal       | Kalaba       |              |             |          |
			// ???? | ur         | Urdu         |              |             |          |

			wsCollection.Add(CreateWritingSystem(fdoCache, s_wsHvos.En,
				"en", new int[] { s_wsHvos.En, s_wsHvos.Es }, new string[] { "English", "inglés" },
				"Arial", "Times New Roman", "Charis SIL"));
			wsCollection.Add(CreateWritingSystem(fdoCache, s_wsHvos.Es,
				"es", new int[] { s_wsHvos.En, s_wsHvos.Es, s_wsHvos.Fr, s_wsHvos.Ipa },
				new string[] { "Spanish", "español", "espagnol", "español" },
				"Arial", "Times New Roman", "Charis SIL"));
			wsCollection.Add(CreateWritingSystem(fdoCache, s_wsHvos.Fr,
				"fr", new int[] { s_wsHvos.Es, s_wsHvos.Fr }, new string[] { "francés", "francais" },
				"Arial", "Times New Roman", "Charis SIL"));
			wsCollection.Add(CreateWritingSystem(fdoCache, s_wsHvos.Ipa,
				"en-IPA", new int[] { s_wsHvos.En, s_wsHvos.Ipa }, new string[] { "English IPA", "aipie" },
				null, "SILDoulos IPA93", "Charis SIL"));
			wsCollection.Add(CreateWritingSystem(fdoCache, s_wsHvos.De,
				"de", new int[] { s_wsHvos.En, s_wsHvos.Ipa }, new string[] { "German", "Deutsch" },
				"Arial", "Times New Roman", "Charis SIL"));
			wsCollection.Add(CreateWritingSystem(fdoCache, s_wsHvos.XKal,
				"xkal", new int[] { s_wsHvos.En }, new string[] { "Kalaba" },
				"Arial", "Times New Roman", "Charis SIL"));
			wsCollection.Add(CreateWritingSystem(fdoCache, s_wsHvos.Ur,
				"ur", new int[] { s_wsHvos.En }, new string[] { "Urdu" },
				"Arial Unicode MS", null, "Charis SIL"));

			((NewFdoCache)m_fdoCache).SetLanguageEncodings(wsCollection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the default vernacular writing system. (This removes any previous vernacular
		/// writing system(s) from the list.)
		/// </summary>
		/// <param name="hvoWs">the hvo of the writing system which will become the default
		/// vernacular writing system</param>
		/// ------------------------------------------------------------------------------------
		public void ChangeDefaultVernWs(int hvoWs)
		{
			CheckDisposed();
			m_fdoCache.LangProject.CurVernWssRS.RemoveAll();
			m_fdoCache.LangProject.CurVernWssRS.Append(hvoWs);
			m_fdoCache.LangProject.CacheDefaultWritingSystems();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the default analysis writing system. (This removes any previous analysis
		/// writing system(s) from the list.)
		/// </summary>
		/// <param name="hvoWs">the hvo of the writing system which will become the default
		/// analysis writing system</param>
		/// ------------------------------------------------------------------------------------
		public void ChangeDefaultAnalWs(int hvoWs)
		{
			CheckDisposed();
			m_fdoCache.LangProject.CurAnalysisWssRS.RemoveAll();
			m_fdoCache.LangProject.CurAnalysisWssRS.Append(hvoWs);
			m_fdoCache.LangProject.CacheDefaultWritingSystems();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new writing system
		/// </summary>
		/// <param name="fdoCache">The FDO cache</param>
		/// <param name="hvo">The HVO of the new writing system</param>
		/// <param name="icuLocale">The ICU locale</param>
		/// <param name="wsHvos">Array of writing system codes for the names of this new
		/// writing system.</param>
		/// <param name="names">Array of names for this writing system. Each member of this
		/// array is the name of the writing system in the corresponding language/ws in
		/// <paramref name="wsHvos"/></param>
		/// <returns>A new writing system object</returns>
		/// ------------------------------------------------------------------------------------
		public LgWritingSystem CreateWritingSystem(FdoCache fdoCache, int hvo, string icuLocale,
			int[] wsHvos, string[] names)
		{
			CheckDisposed();
			LgWritingSystem ws = new LgWritingSystem(fdoCache, hvo);
			ws.ICULocale = icuLocale;
			System.Diagnostics.Debug.Assert(wsHvos.Length == names.Length);

			for (int i = 0; i < wsHvos.Length; i++)
			{
				fdoCache.SetMultiUnicodeAlt(hvo,
					(int)LgWritingSystem.LgWritingSystemTags.kflidName, wsHvos[i],
					names[i]);
				if (Cache.LanguageWritingSystemFactoryAccessor.BypassInstall)
				{
					IWritingSystem lgws = Cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(hvo);
					string displayName = lgws.get_Name(wsHvos[i]);
					if (String.IsNullOrEmpty(displayName))
						lgws.set_Name(wsHvos[i], names[i]);
					lgws.Dirty = false;
				}
			}

			return ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new writing system and sets the font names for it.
		/// </summary>
		/// <param name="fdoCache">The FDO cache</param>
		/// <param name="hvo">The HVO of the new writing system</param>
		/// <param name="icuLocale">The ICU locale</param>
		/// <param name="wsHvos">Array of writing system codes for the names of this new
		/// writing system.</param>
		/// <param name="names">Array of names for this writing system. Each member of this
		/// array is the name of the writing system in the corresponding language/ws in
		/// <paramref name="wsHvos"/></param>
		/// <param name="defaultHeadingFont">The default heading font.</param>
		/// <param name="defaultFont">The default data font.</param>
		/// <param name="defaultBodyFont">The default body font.</param>
		/// <returns>A new writing system object</returns>
		/// ------------------------------------------------------------------------------------
		public LgWritingSystem CreateWritingSystem(FdoCache fdoCache, int hvo, string icuLocale,
			int[] wsHvos, string[] names, string defaultHeadingFont, string defaultFont,
			string defaultBodyFont)
		{
			CheckDisposed();
			LgWritingSystem ws = CreateWritingSystem(fdoCache, hvo, icuLocale,
				wsHvos, names);
			ws.DefaultSansSerif = defaultHeadingFont;
			ws.DefaultSerif = defaultFont;
			ws.DefaultBodyFont = defaultBodyFont;
			return ws;
		}
		#endregion

		#region Lexical database
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize lexical database and morph types
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeLexDb()
		{
			CheckDisposed();
			LoadMetaData("ling");

			int hvoLexDb = m_cacheBase.NewHvo(LexDb.kClassId);
			m_lp.LexDbOAHvo = hvoLexDb;
			LexDb lexDb = new LexDb(Cache, hvoLexDb);

			AddMorphTypes(lexDb);

			m_lp.PartsOfSpeechOAHvo = m_cacheBase.NewHvo(CmPossibilityList.kClassId);

			m_lp.SemanticDomainListOAHvo = m_cacheBase.NewHvo(CmPossibilityList.kClassId);

			lexDb.UsageTypesOAHvo = m_cacheBase.NewHvo(CmPossibilityList.kClassId);

			m_lp.PhonologicalDataOAHvo = m_cacheBase.NewHvo(PhPhonData.kClassId);

			m_lp.WordformInventoryOAHvo = m_cacheBase.NewHvo(WordformInventory.kClassId);

			m_lp.MorphologicalDataOAHvo = m_cacheBase.NewHvo(MoMorphData.kClassId);

			m_lp.ConfidenceLevelsOAHvo = m_cacheBase.NewHvo(CmPossibilityList.kClassId);

			// TODO: add lexDb.Introduction, lexDb.Domain/Subentry/Sense,
			// lexDb.Status
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an interlinear text to the language projectin the mocked fdocache
		/// </summary>
		/// <param name="name">The name (in English).</param>
		/// <returns>The new text</returns>
		/// ------------------------------------------------------------------------------------
		public IText AddInterlinearTextToLangProj(string name)
		{
			return AddInterlinearTextToLangProj(name, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an interlinear text to the language projectin the mocked fdocache
		/// </summary>
		/// <param name="name">The name (in English).</param>
		/// <param name="fCreateContents">if set to <c>true</c> also creates an StText for the
		/// Contents.</param>
		/// <returns>The new text</returns>
		/// ------------------------------------------------------------------------------------
		public IText AddInterlinearTextToLangProj(string name, bool fCreateContents)
		{
			CheckDisposed();
			Debug.Assert(IsModuleLoaded("Ling"), "Need to load meta data for module Ling first");

			int hvoText = m_cacheBase.NewHvo(Text.kClassId);

			// set up the text
			m_cacheBase.SetBasicProps(hvoText, m_lp.Hvo, Text.kClassId,
				(int)LangProject.LangProjectTags.kflidTexts, 1);
			IText text = CmObject.CreateFromDBObject(Cache, hvoText) as IText;
			text.Name.SetAlternative(name, (int)s_wsHvos.En);

			m_cacheBase.AppendToFdoVector(m_lp.Hvo,
				(int)LangProject.LangProjectTags.kflidTexts, hvoText);

			if (fCreateContents)
			{
				text.ContentsOA = new StText();
				//text.ContentsOA = new StText(Cache, m_cacheBase.NewHvo(StText.kClassId));
				//// setup the new StText
				//m_cacheBase.SetBasicProps(text.ContentsOA.Hvo, text.Hvo, (int)StText.kClassId,
				//    (int)Text.TextTags.kflidContents, 1);
			}
			return text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a paragraph with the specified text to the given interlinear text
		/// </summary>
		/// <param name="itext">The itext.</param>
		/// <param name="paraText">Paragraph contents.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara AddParaToInterlinearTextContents(IText itext, string paraText)
		{
			CheckDisposed();
			Debug.Assert(IsModuleLoaded("Ling"), "Need to load meta data for module Ling first");

			// add paragraph
			StTxtPara para = new StTxtPara(Cache, m_cacheBase.NewHvo(StTxtPara.kClassId));

			// Append the paragraph to the contents of the specified interlinear text
			m_cacheBase.AppendToFdoVector(itext.ContentsOAHvo,
				(int)StText.StTextTags.kflidParagraphs, para.Hvo);

			// Setup the new paragraph
			m_cacheBase.SetBasicProps(para.Hvo, itext.ContentsOAHvo, (int)StTxtPara.kClassId,
				(int)StText.StTextTags.kflidParagraphs, 1);

			ITsStrFactory fact = TsStrFactoryClass.Create();
			para.Contents.UnderlyingTsString = fact.MakeString(paraText, (int)InMemoryFdoCache.s_wsHvos.Fr);

			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="lexDb"></param>
		/// ------------------------------------------------------------------------------------
		private void AddMorphTypes(LexDb lexDb)
		{
			int hvoMorphTypesList = m_cacheBase.NewHvo(CmPossibilityList.kClassId);
			lexDb.MorphTypesOAHvo = hvoMorphTypesList;
			CmPossibilityList morphTypesList = new CmPossibilityList(Cache, hvoMorphTypesList);

			for (int i = 1; i <= MoMorphTypeCollection.kmtLimit; i++)
			{
				int hvoMorphType = m_cacheBase.NewHvo(MoMorphType.kClassId);
				MoMorphType morphType = new MoMorphType(Cache, hvoMorphType);
				switch (i)
				{
					case 1:
						InitMorphType(new Guid(MoMorphType.kguidMorphBoundRoot), "*", null, 2, morphType);
						break;
					case 2:
						InitMorphType(new Guid(MoMorphType.kguidMorphBoundStem), "*", null, 2, morphType);
						break;
					case 3:
						InitMorphType(new Guid(MoMorphType.kguidMorphCircumfix), null, null, 1, morphType);
						break;
					case 4:
						InitMorphType(new Guid(MoMorphType.kguidMorphEnclitic), "=", null, 7, morphType);
						break;
					case 5:
						InitMorphType(new Guid(MoMorphType.kguidMorphInfix), "-", "-", 5, morphType);
						break;
					case 6:
						InitMorphType(new Guid(MoMorphType.kguidMorphParticle), null, null, 1, morphType);
						break;
					case 7:
						InitMorphType(new Guid(MoMorphType.kguidMorphPrefix), null, "-", 3, morphType);
						break;
					case 8:
						InitMorphType(new Guid(MoMorphType.kguidMorphProclitic), null, "=", 4, morphType);
						break;
					case 9:
						InitMorphType(new Guid(MoMorphType.kguidMorphRoot), null, null, 1, morphType);
						break;
					case 10:
						InitMorphType(new Guid(MoMorphType.kguidMorphSimulfix), "=", "=", 5, morphType);
						break;
					case 11:
						InitMorphType(new Guid(MoMorphType.kguidMorphStem), null, null, 1, morphType);
						break;
					case 12:
						InitMorphType(new Guid(MoMorphType.kguidMorphSuffix), "-", null, 6, morphType);
						break;
					case 13:
						InitMorphType(new Guid(MoMorphType.kguidMorphSuprafix), "~", "~", 5, morphType);
						break;
					case 14:
						InitMorphType(new Guid(MoMorphType.kguidMorphInfixingInterfix), "-", "-", 0, morphType);
						break;
					case 15:
						InitMorphType(new Guid(MoMorphType.kguidMorphPrefixingInterfix), null, "-", 0, morphType);
						break;
					case 16:
						InitMorphType(new Guid(MoMorphType.kguidMorphSuffixingInterfix), "-", null, 0, morphType);
						break;
					case 17:
						InitMorphType(new Guid(MoMorphType.kguidMorphPhrase), null, null, 0, morphType);
						break;
					case 18:
						InitMorphType(new Guid(MoMorphType.kguidMorphDiscontiguousPhrase), null, null, 0, morphType);
						break;
					case 19:
						InitMorphType(new Guid(MoMorphType.kguidMorphClitic), null, null, 0, morphType);
						break;
				}
				m_cacheBase.SetBasicProps(hvoMorphType, hvoMorphTypesList, (int)MoMorphType.kClassId,
					(int)CmPossibilityList.CmPossibilityListTags.kflidPossibilities, i);
				m_cacheBase.AppendToFdoVector(hvoMorphTypesList,
					(int)CmPossibilityList.CmPossibilityListTags.kflidPossibilities, hvoMorphType);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the attributes for a MoMorphType
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="prefix"></param>
		/// <param name="postfix"></param>
		/// <param name="secondaryOrder"></param>
		/// <param name="morphType"></param>
		/// ------------------------------------------------------------------------------------
		private void InitMorphType(Guid guid, string prefix, string postfix, int secondaryOrder,
			MoMorphType morphType)
		{
			m_cacheBase.SetGuid(morphType.Hvo, (int)CmObjectFields.kflidCmObject_Guid, guid);
			morphType.Prefix = prefix;
			morphType.Postfix = postfix;
			morphType.SecondaryOrder = secondaryOrder;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes WordForm inventory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeWordFormInventory()
		{
			CheckDisposed();
			// Since we're using the WordformInventory now, we better make a fully intialized new
			// one (we stuck a dummy HVO in the cache in InitializeLexDb(), but now
			// we need a fully initialized one).
			m_lp.WordformInventoryOA = new WordformInventory();
			m_lp.WordformInventoryOA.WordformsOC.Add(new WfiWordform());
		}
		#endregion

		#region Discourse charts

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an empty chart on the specified text.
		/// </summary>
		/// <param name="name">Chart name.</param>
		/// <param name="iText">Chart is BasedOn this text.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IDsConstChart AddChartToLangProj(string name, IStText iText)
		{
			CheckDisposed();
			Debug.Assert(IsModuleLoaded("Ling"), "Need to load meta data for module Ling first");

			// Add a chart object
			DsConstChart chart = new DsConstChart(Cache, m_cacheBase.NewHvo(DsConstChart.kClassId));

			// Add the DiscourseData object to LangProj
			if (m_lp.DiscourseDataOA == null)
				m_lp.DiscourseDataOA = new DsDiscourseData();

			// Add the chart to the DiscourseData object
			m_cacheBase.AppendToFdoVector(m_lp.DiscourseDataOAHvo,
				(int)DsDiscourseData.DsDiscourseDataTags.kflidCharts, chart.Hvo);

			// Setup the new chart
			m_cacheBase.SetBasicProps(chart.Hvo, m_lp.DiscourseDataOAHvo, (int)DsConstChart.kClassId,
				(int)DsDiscourseData.DsDiscourseDataTags.kflidCharts, 1);
			chart.Name.AnalysisDefaultWritingSystem = name;
			chart.BasedOnRA = iText;

			return chart; // This chart has no template or rows, so far!!
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an empty template to the DiscourseData object.
		/// </summary>
		/// <param name="name">Template name. (default)</param>
		/// <param name="discData"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ICmPossibility AddEmptyTemplateToDiscData(string name, IDsDiscourseData discData)
		{
			CheckDisposed();

			if (discData == null)
				discData = m_lp.DiscourseDataOA = new DsDiscourseData();

			// Add an empty PossibilityList
			discData.ConstChartTemplOA = new CmPossibilityList();
			// Add an empty template object
			ICmPossibility template = new CmPossibility(Cache, m_cacheBase.NewHvo(CmPossibility.kClassId));

			// Add the template to the DiscourseData object
			m_cacheBase.AppendToFdoVector(discData.ConstChartTemplOAHvo,
				(int)DsDiscourseData.DsDiscourseDataTags.kflidConstChartTempl, template.Hvo);

			// Setup the new template
			m_cacheBase.SetBasicProps(template.Hvo, discData.ConstChartTemplOAHvo, (int)CmPossibility.kClassId,
				(int)DsDiscourseData.DsDiscourseDataTags.kflidConstChartTempl, 1);
			Cache.SetMultiStringAlt(template.Hvo, (int)CmPossibility.CmPossibilityTags.kflidName,
				Cache.LanguageEncodings.GetWsFromIcuLocale("en"), Cache.MakeAnalysisTss(name));

			return template; // This template has no columns, so far!!
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an annotation (wfic) on a StText to the language project in the mocked fdocache.
		/// </summary>
		/// <param name="begOffset"></param>
		/// <param name="endOffset"></param>
		/// <param name="stText"></param>
		/// <param name="stTextPara"></param>
		/// <returns>The new annotation</returns>
		/// ------------------------------------------------------------------------------------
		public ICmBaseAnnotation AddWficToLangProj(int begOffset, int endOffset, IStText stText, IStTxtPara stTextPara)
		{
			CheckDisposed();
			Debug.Assert(IsModuleLoaded("Ling"), "Need to load meta data for module Ling first");
			Debug.Assert(stText != null, "No StText available.");

			int hvoAnn = m_cacheBase.NewHvo(CmBaseAnnotation.kClassId);

			// set up the annotation
			m_cacheBase.SetBasicProps(hvoAnn, m_lp.Hvo, CmBaseAnnotation.kClassId,
				(int)LangProject.LangProjectTags.kflidAnnotations, 1);
			ICmBaseAnnotation ann = CmObject.CreateFromDBObject(Cache, hvoAnn) as ICmBaseAnnotation;

			// Set annotation internals
			ann.BeginOffset = begOffset;
			ann.EndOffset = endOffset;
			ann.BeginObjectRA = stTextPara;
			ann.EndObjectRA = stTextPara;
			ann.Flid = (int)StTxtPara.StTxtParaTags.kflidContents;

			m_cacheBase.AppendToFdoVector(m_lp.Hvo,
				(int)LangProject.LangProjectTags.kflidAnnotations, hvoAnn);

			return ann;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a fake chart template column to the fake template in the mocked LangProj.
		/// </summary>
		/// <param name="template">fake template for tests</param>
		/// <param name="name">Column name</param>
		/// <returns>The new possibility(column)</returns>
		/// ------------------------------------------------------------------------------------
		public int AddColumnToTemplate(ICmPossibility template, string name)
		{
			CheckDisposed();
			Debug.Assert(template != null, "No Template available.");

			int hvoCol = m_cacheBase.NewHvo(CmPossibility.kClassId);

			// set up the fake column
			m_cacheBase.SetBasicProps(hvoCol, template.Hvo, CmPossibility.kClassId,
				(int)CmPossibility.CmPossibilityTags.kflidSubPossibilities, 1);

			// Set column internals
			Cache.SetMultiStringAlt(hvoCol, (int)CmPossibility.CmPossibilityTags.kflidName,
				Cache.LanguageEncodings.GetWsFromIcuLocale("en"), Cache.MakeAnalysisTss(name));

			m_cacheBase.AppendToFdoVector(template.Hvo,
				(int)CmPossibility.CmPossibilityTags.kflidSubPossibilities, hvoCol);

			return hvoCol;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an indirect annotation to the language project for a chart in the mocked fdocache.
		/// </summary>
		/// <param name="hvoAppliesTo"></param>
		/// <param name="comment"></param>
		/// <param name="hvoInstanceOf"></param>
		/// <returns>The new annotation</returns>
		/// ------------------------------------------------------------------------------------
		public int AddIndirAnnToLangProj(int[] hvoAppliesTo, int hvoInstanceOf, string comment)
		{
			CheckDisposed();
			Debug.Assert(IsModuleLoaded("Ling"), "Need to load meta data for module Ling first");

			int hvoAnn = m_cacheBase.NewHvo(CmIndirectAnnotation.kClassId);

			// set up the annotation
			m_cacheBase.SetBasicProps(hvoAnn, m_lp.Hvo, CmIndirectAnnotation.kClassId,
				(int)LangProject.LangProjectTags.kflidAnnotations, 1);
			ICmIndirectAnnotation ann = CmObject.CreateFromDBObject(Cache, hvoAnn) as ICmIndirectAnnotation;

			// Set annotation internals
			ann.Comment.SetAlternative(comment, CacheAccessor.WritingSystemFactory.GetWsFromStr("en"));
			ann.InstanceOfRAHvo = hvoInstanceOf;
			foreach (int hvoTarget in hvoAppliesTo)
				ann.AppliesToRS.Append(hvoTarget);

			m_cacheBase.AppendToFdoVector(m_lp.Hvo,
				(int)LangProject.LangProjectTags.kflidAnnotations, hvoAnn);

			return hvoAnn;
		}

		#endregion

		#region User views
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes user views (creates a single user view with no records)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeUserViews()
		{
			CheckDisposed();
			int uvHvo = m_cacheBase.NewHvo(UserView.kClassId);
			m_cacheBase.SetBasicProps(uvHvo, 0, UserView.kClassId, 0, 0);

			((NewFdoCache)m_fdoCache).SetUserViewSpecs(UserView.Load(m_fdoCache,
				new Set<int>(new int[] { uvHvo })));
		}
		#endregion

		#region Publication
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a publication and adds it to a collection of Publications on the
		/// CmMajorObject AnnotationDefs.
		/// </summary>
		/// <param name="pageHeight">Height of the page.</param>
		/// <param name="pageWidth">Width of the page.</param>
		/// <param name="fIsLandscape">if set to <c>true</c> the publication is landscape.</param>
		/// <param name="name">The name of the publication.</param>
		/// <param name="gutterMargin">The gutter margin.</param>
		/// <param name="bindingSide">The side on which the publication will be bound (i.e., the
		/// gutter location).</param>
		/// <param name="footnoteSepWidth">Width of the footnote seperator.</param>
		/// <returns>the new publication</returns>
		/// <remarks>Adds the publication to AnnotationDefs because we need a
		/// CmMajorObject where we can attach the Publication and Scripture is not visible here.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public IPublication CreatePublication(int pageHeight, int pageWidth, bool fIsLandscape,
			string name, int gutterMargin, BindingSide bindingSide, int footnoteSepWidth)
		{
			Debug.Assert(Cache.LangProject != null, "The language project is null");
			Debug.Assert(Cache.LangProject.AnnotationDefsOA != null,
				"The annotation definitions are null.");

			Publication pub = new Publication();
			Cache.LangProject.AnnotationDefsOA.PublicationsOC.Add(pub);
			pub.PageHeight = pageHeight;
			pub.PageWidth = pageWidth;
			pub.IsLandscape = fIsLandscape;
			pub.Name = name;
			pub.GutterMargin = gutterMargin;
			pub.BindingEdge = bindingSide;
			pub.FootnoteSepWidth = footnoteSepWidth;

			return (IPublication)pub;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the division to the publication.
		/// </summary>
		/// <param name="pub">The publication where the division will be added.</param>
		/// <param name="fDifferentFirstHF">if set to <c>true</c> publication has a different
		/// first header/footer].</param>
		/// <param name="fDifferentEvenHF">if set to <c>true</c> publication has a different even
		/// header/footer.</param>
		/// <param name="startAt">Enumeration of options for where the content of the division
		/// begins</param>
		/// <returns>the new division</returns>
		/// ------------------------------------------------------------------------------------
		public IPubDivision AddDivisionToPub(IPublication pub, bool fDifferentFirstHF,
			bool fDifferentEvenHF, DivisionStartOption startAt)
		{
			PubDivision div = new PubDivision();
			pub.DivisionsOS.Append(div);
			div.DifferentFirstHF = fDifferentFirstHF;
			div.DifferentEvenHF = fDifferentEvenHF;
			div.StartAt = startAt;
			return (IPubDivision)div;
		}
		#endregion

		#region Action handler
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes an ActionHandler mock object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void InitializeActionHandler()
		{
			CheckDisposed();
			m_acth = new DynamicMock(typeof(IActionHandler));
			m_acth.SetupResult("CurrentDepth", 0); // TODO: update our action handler depth to be 1.
			m_acth.SetupResult("IsUndoOrRedoInProgress", false);
			m_acth.SetupResult("CanUndo", false);
			m_acth.SetupResult("Mark", 432);
			m_acth.Ignore("AddAction");

			((NewFdoCache)m_fdoCache).ActionHandler = (IActionHandler)m_acth.MockInstance;
		}
		#endregion

		#region Annotations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes annotation definitions (annotation types)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeAnnotationDefs()
		{
			CheckDisposed();
			// Initialize the annotation definition possibility list.
			m_lp.AnnotationDefsOAHvo = m_cacheBase.NewHvo(CmPossibilityList.kClassId);

			// Add an annotation definition (i.e. type) for scripture notes
			CmAnnotationDefn annotationDefn = new CmAnnotationDefn();
			m_lp.AnnotationDefsOA.PossibilitiesOS.Append(annotationDefn);
			m_cacheBase.CacheGuidProp(annotationDefn.Hvo, (int)CmObjectFields.kflidCmObject_Guid,
				LangProject.kguidAnnNote);

			// Add a sub-annotation type (for "Consultant Note")
			m_consultantNoteDefn = new CmAnnotationDefn();
			annotationDefn.SubPossibilitiesOS.Append(m_consultantNoteDefn);
			m_cacheBase.CacheGuidProp(m_consultantNoteDefn.Hvo, (int)CmObjectFields.kflidCmObject_Guid,
				LangProject.kguidAnnConsultantNote);
			m_consultantNoteDefn.Name.SetAlternative("Consultant", s_wsHvos.En);
			m_consultantNoteDefn.UserCanCreate = true;

			// Add a sub-annotation type for "Translator Note"
			m_translatorNoteDefn = new CmAnnotationDefn();
			annotationDefn.SubPossibilitiesOS.Append(m_translatorNoteDefn);
			m_cacheBase.CacheGuidProp(m_translatorNoteDefn.Hvo, (int)CmObjectFields.kflidCmObject_Guid,
				LangProject.kguidAnnTranslatorNote);
			m_translatorNoteDefn.Name.SetAlternative("Translator", s_wsHvos.En);
			m_translatorNoteDefn.UserCanCreate = true;

			// Add an annotation definition (i.e. type) for scripture checking errors
			MakeAnnotationDefn("Errors", LangProject.kguidAnnCheckingError);
			// And the interlinear ones.
			MakeAnnotationDefn("Punctuation", new Guid(LangProject.kguidAnnPunctuationInContext));
			MakeAnnotationDefn("Wfic", new Guid(LangProject.kguidAnnWordformInContext));
		}

		private void MakeAnnotationDefn(string name, Guid guid)
		{
			CmAnnotationDefn annotationDefn;
			annotationDefn = new CmAnnotationDefn();
			m_lp.AnnotationDefsOA.PossibilitiesOS.Append(annotationDefn);
			m_cacheBase.CacheGuidProp(annotationDefn.Hvo, (int)CmObjectFields.kflidCmObject_Guid,
									  guid);
			annotationDefn.Name.SetAlternative(name, s_wsHvos.En);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes annotation categories - we used to use scripture.ScriptureNotesCategories,
		/// now we use LangProject.AffixCategories instead. It doesn't really matter as long
		/// as we set the variables that the tests expect.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeAnnotationCategories()
		{
			CheckDisposed();
			// Initialize the annotation category possibility list.
			m_lp.AffixCategoriesOAHvo = m_cacheBase.NewHvo(CmPossibilityList.kClassId);

			// Add an annotation category (for Discourse)
			m_categoryDiscourse = new CmPossibility();
			m_lp.AffixCategoriesOA.PossibilitiesOS.Append(m_categoryDiscourse);
			m_cacheBase.CacheGuidProp(m_categoryDiscourse.Hvo,
				(int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());

			// Add an annotation category (for Grammar)
			m_categoryGrammar = new CmPossibility();
			m_lp.AffixCategoriesOA.PossibilitiesOS.Append(m_categoryGrammar);
			m_cacheBase.CacheGuidProp(m_categoryGrammar.Hvo,
				(int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());

			// add a sub-annotation category (for "Pronominal reference")
			m_categoryGrammar_PronominalRef = new CmPossibility();
			m_categoryGrammar.SubPossibilitiesOS.Append(m_categoryGrammar_PronominalRef);
			m_cacheBase.CacheGuidProp(m_categoryGrammar_PronominalRef.Hvo,
				(int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());

			// add a sub-sub-annotation category (for "Extended use")
			m_categoryGrammar_PronominalRef_ExtendedUse = new CmPossibility();
			m_categoryGrammar_PronominalRef.SubPossibilitiesOS.Append(m_categoryGrammar_PronominalRef_ExtendedUse);
			m_cacheBase.CacheGuidProp(m_categoryGrammar_PronominalRef_ExtendedUse.Hvo,
				(int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());

			// Add an annotation category (for Gnarly)
			m_categoryGnarly = new CmPossibility();
			m_lp.AffixCategoriesOA.PossibilitiesOS.Append(m_categoryGnarly);
			m_cacheBase.CacheGuidProp(m_categoryGnarly.Hvo,
				(int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setups the Scripture annotation categories.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetupScriptureAnnotationCategories()
		{
			CheckDisposed();
			// Initialize the Scripture annotation category possibility list.
			m_lp.TranslatedScriptureOA.NoteCategoriesOAHvo = m_cacheBase.NewHvo(CmPossibilityList.kClassId);
		}
		#endregion

		#region CmTranslation stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an empty translation of the specified type to the paragraph, or return the
		/// existing one.
		/// </summary>
		/// <param name="owner">the owning paragraph</param>
		/// <param name="transType">The type of translation to create</param>
		/// <param name="wsTrans">The writing system of the translation</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ICmTranslation AddTransToMockedParagraph(StTxtPara owner, Guid transType,
			int wsTrans)
		{
			CheckDisposed();
			ICmTranslation trans = owner.GetTrans(transType);
			if (trans == null)
			{
				int hvo = m_cacheBase.NewHvo(CmTranslation.kClassId);
				m_cacheBase.AppendToFdoVector(owner.Hvo,
					(int)StTxtPara.StTxtParaTags.kflidTranslations, hvo);
				m_cacheBase.SetBasicProps(hvo, owner.Hvo, (int)CmTranslation.kClassId,
					(int)StTxtPara.StTxtParaTags.kflidTranslations, 1);
				trans = new CmTranslation(Cache, hvo);
				trans.TypeRA = m_lp.TranslationTagsOA.LookupPossibilityByGuid(transType);
			}
			trans.Translation.GetAlternative(wsTrans).Text = string.Empty;
			return trans;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an empty back translation to the paragraph, or return the existing one.
		/// </summary>
		/// <param name="owner">the owning paragraph</param>
		/// <param name="wsTrans"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ICmTranslation AddBtToMockedParagraph(StTxtPara owner, int wsTrans)
		{
			CheckDisposed();
			return AddTransToMockedParagraph(owner, LangProject.kguidTranBackTranslation,
				wsTrans);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a run of text to the specified translation
		/// </summary>
		/// <param name="trans"></param>
		/// <param name="btWS"></param>
		/// <param name="runText"></param>
		/// <param name="runStyleName"></param>
		/// ------------------------------------------------------------------------------------
		public void AddRunToMockedTrans(ICmTranslation trans, int btWS, string runText,
			string runStyleName)
		{
			CheckDisposed();
			AddRunToMockedTrans(trans, btWS, btWS, runText, runStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a run of text to the specified translation
		/// </summary>
		/// <param name="trans">The translation where the run of text will be appended.</param>
		/// <param name="btWS">The writing system of the back translation</param>
		/// <param name="runWS">The writing system of the run</param>
		/// <param name="runText">The run text.</param>
		/// <param name="runStyleName">Name of the run style.</param>
		/// ------------------------------------------------------------------------------------
		public void AddRunToMockedTrans(ICmTranslation trans, int btWS, int runWS, string runText,
			string runStyleName)
		{
			CheckDisposed();
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps runProps = propFact.MakeProps(runStyleName, runWS, 0);
			TsStringAccessor contents = trans.Translation.GetAlternative(btWS);
			ITsStrBldr bldr = contents.UnderlyingTsString.GetBldr();
			bldr.Replace(bldr.Length, bldr.Length, runText, runProps);
			contents.UnderlyingTsString = bldr.GetString();
		}

		/// <summary>
		/// Modify the run of the text by the given bounds with the replacement string.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichEnd"></param>
		/// <param name="replacement"></param>
		public void ModifyRunAt(IStTxtPara para, int ichMin, int ichEnd, string replacement)
		{
			CheckDisposed();
			TsStringAccessor contents = para.Contents;
			ITsStrBldr bldr = contents.UnderlyingTsString.GetBldr();
			ITsTextProps runStyle = bldr.get_PropertiesAt(ichMin);
			TsRunInfo tri;
			bldr.FetchRunInfoAt(ichMin, out tri);
			// truncate the replacement by the run bounds.
			int ichLim = ichEnd > tri.ichLim ? tri.ichLim : ichEnd;
			bldr.Replace(ichMin, ichLim, replacement, runStyle);
			contents.UnderlyingTsString = bldr.GetString();
		}

		#endregion

		#region Style creation stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates some styles that are added to the language project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CreateDefaultLangProjStyles()
		{
			AddStyle(m_lp.StylesOC, "Normal", ContextValues.Internal, StructureValues.Undefined,
				FunctionValues.Prose, false, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style on the specified style list.
		/// </summary>
		/// <param name="styleList">The style list to add the style to</param>
		/// <param name="name">style name</param>
		/// <param name="context">style context</param>
		/// <param name="structure">style structure</param>
		/// <param name="function">style function</param>
		/// <param name="isCharStyle">true if character style, otherwise false</param>
		/// <param name="userLevel">User level</param>
		/// ------------------------------------------------------------------------------------
		public void AddStyle(FdoOwningCollection<IStStyle> styleList, string name,
			ContextValues context, StructureValues structure, FunctionValues function,
			bool isCharStyle, int userLevel)
		{
			CheckDisposed();
			StStyle style = new StStyle();
			styleList.Add(style);
			style.Name = name;
			style.Context = context;
			style.Structure = structure;
			style.Function = function;
			style.Type = (isCharStyle ? StyleType.kstCharacter : StyleType.kstParagraph);
			style.UserLevel = userLevel;
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			style.Rules = bldr.GetTextProps();
		}
		#endregion

		#region IDisposable & Co. implementation

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		private bool m_isDisposed = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		~InMemoryFdoCache()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disposes and cleans up this object
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;


			IFwMetaDataCache metaCache = null;
			ILgWritingSystemFactory wsFactory = null;
			if (disposing)
			{
				// Dispose managed resources here.
				if (m_fdoCache != null)
				{
					metaCache = m_fdoCache.MetaDataCacheAccessor;
					// Don't set these to null, since the Dispose method on the
					// cache needs them to remove ChangeWatchers from them.
					//(m_fdoCache as NewFdoCache).MetaDataCache = null;
					//(m_fdoCache as NewFdoCache).DataAccess = null;
					m_fdoCache.Dispose();
				}
				if (m_cacheBase != null)
				{
					wsFactory = m_cacheBase.WritingSystemFactory;
					m_cacheBase.WritingSystemFactory = null;
					m_cacheBase.Dispose();
				}
			}
			// Dispose unmanaged resources here, whether disposing is true or false.
			if (metaCache != null && Marshal.IsComObject(metaCache))
				Marshal.ReleaseComObject(metaCache);
			if (wsFactory != null)
				wsFactory.Shutdown();
			m_lp = null;
			m_fdoCache = null;
			m_acth = null;
			m_cacheBase = null;
			m_metaDataModulesLoaded = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region Other methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an object that happens to have a MultiUnicode property and return its
		/// accessor.
		/// </summary>
		/// <returns>The accessor</returns>
		/// ------------------------------------------------------------------------------------
		public MultiUnicodeAccessor CreateArbitraryMultiUnicodeAccessor()
		{
			CheckDisposed();
			CmPossibility poss = new CmPossibility(Cache, m_cacheBase.NewHvo(CmPossibility.kClassId));
			return poss.Abbreviation;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new HVO for given class
		/// </summary>
		/// <param name="classId"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int NewHvo(int classId)
		{
			CheckDisposed();
			return m_cacheBase.NewHvo(classId);
		}
		#endregion
	}
	#endregion

	#region DefaultWsFactoryProvider class
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// This is just a class that provides a normal WS factory.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public class DefaultWsFactoryProvider : IWsFactoryProvider
	{
		#region IWsFactoryProvider Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// By default, just return a normal factory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILgWritingSystemFactory NewILgWritingSystemFactory
		{
			get
			{
				return LgWritingSystemFactoryClass.Create();
			}
		}
		#endregion
	}
	#endregion
}
