// --------------------------------------------------------------------------------------------
// Copyright (C) 2002 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: CmObject.cs
// Responsibility: John Hatton and Randy Regnier
// Last reviewed: never
//
//
// <remarks>
// Implementation of:
//		CmObject : Object
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices; // needed for Marshal
using System.Text;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls; // for ProgressState
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils; // for LanguageDefinitionFactory

namespace SIL.FieldWorks.FDO
{
	#region NamedWritingSystem
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A quick-and-dirty little class to encapsulate a locale ID with a single "best"
	/// name. That way, these things can be used in combo boxes, etc. because ToString will
	/// return the preferred name.
	///
	/// Enhanced by JohnT to record the icuLocale instead of a full LgWritingSystem object.
	/// This and the name are all that is wanted in most situations, and when we create one
	/// from an XML file, it saves computation if we don't actually create an LgWritingSystem
	/// object unless it's really needed. Furthermore, when used in the process of creating
	/// a new database, we need to be able to create these objects even though a database
	/// does not yet exist, which makes it impossible to create LgWritingSystem objects.
	/// Not requiring a database may also be helpful if some of the controls which use these
	/// objects are used in WorldPad or other non-database applications.
	/// </summary>
	/// <remarks>
	/// Objects of this type can become "dirty" if the underlying writing system name changes.
	/// Do not use in situations where up-to-the minute names changes are required. Also, for
	/// persistence purposes, the ICU locale of the writing system should always be used.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public class NamedWritingSystem : IComparable
	{
		private string m_name;
		private string m_icuLocale;
		private int m_hvo;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a brand spankin' new <see cref="NamedWritingSystem"/>
		/// </summary>
		/// <param name="name">The "best" (preferred) name for the writing system</param>
		/// <param name="icuLocale">The ICU locale that identifies the ws.</param>
		/// ------------------------------------------------------------------------------------
		public NamedWritingSystem(string name, string icuLocale)
		{
			m_name = name;
			m_icuLocale = icuLocale;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a brand spankin' new <see cref="NamedWritingSystem"/>
		/// </summary>
		/// <param name="name">The "best" (preferred) name for the writing system</param>
		/// <param name="icuLocale">The ICU locale that identifies the ws.</param>
		/// <param name="hvo"></param>
		/// ------------------------------------------------------------------------------------
		public NamedWritingSystem(string name, string icuLocale, int hvo)
		{
			m_name = name;
			m_icuLocale = icuLocale;
			m_hvo = hvo;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the "best" (preferred) name for the writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get { return m_name; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hvo of the ws, if it's been set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Hvo
		{
			get { return m_hvo; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ICU locale.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string IcuLocale
		{
			get { return m_icuLocale; }
		}

		/// <summary>
		/// Get the first part of the IcuLocale without the variant and region info.
		/// Helps to identify related writing systems.
		/// Returns empty string, if
		/// </summary>
		public string GetLanguageAbbr()
		{
			if (String.IsNullOrEmpty(m_icuLocale))
				return "";
			return m_icuLocale.Split('_')[0].ToLowerInvariant();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="lgWs"></param>
		/// <returns>true if lgWs shares the same Language</returns>
		public bool IsRelatedWs(ILgWritingSystem lgWs)
		{
			ILgWritingSystemFactory lgwsf = lgWs.Cache.LanguageWritingSystemFactoryAccessor;
			IWritingSystem ws = lgwsf.get_Engine(lgWs.ICULocale);
			return IsRelatedWs(ws);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>true if ws shares the same Language</returns>
		public bool IsRelatedWs(IWritingSystem ws)
		{
			return ws.LanguageAbbr.Equals(this.GetLanguageAbbr());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets (or makes) the writing system. If there isn't already one with this locale id,
		/// we assume there is an XML language definition we can load.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IWritingSystem EnsureRealWs(ILgWritingSystemFactory wsf)
		{
			if (wsf.GetWsFromStr(m_icuLocale) == 0)
			{
				// Need to create a new writing system from the XML file.
				LanguageDefinitionFactory ldf =
					new LanguageDefinitionFactory(wsf, m_icuLocale);
				string pathname = DirectoryFinder.LanguagesDirectory + "\\" + m_icuLocale + ".xml";
				ldf.Deserialize(pathname);
				ldf.LanguageDefinition.SaveWritingSystem(m_icuLocale);
			}
			return wsf.get_EngineOrNull(wsf.GetWsFromStr(m_icuLocale));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain an interface to C# LgWritingSystem object for this locale in the database represented by this
		/// cache. If necessary create it from the XML and install it.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ILgWritingSystem GetLgWritingSystem(FdoCache cache)
		{
			IWritingSystem wsEngine = EnsureRealWs(cache.LanguageWritingSystemFactoryAccessor);
			cache.ResetLanguageEncodings();
			return (ILgWritingSystem)CmObject.CreateFromDBObject(cache, wsEngine.WritingSystem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string that represents this object: the name of this writing system.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests whether or not this NamedWritingSystem is equal to the specified object
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			if (!(obj is NamedWritingSystem))
				return false;
			return ((NamedWritingSystem)obj).m_name == m_name &&
				((NamedWritingSystem)obj).m_icuLocale == m_icuLocale;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a hash code for this NamedWritingSystem
		/// </summary>
		/// <returns>
		/// the hash code for the icu local added to the hash code for the name
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return m_name.GetHashCode() + m_icuLocale.GetHashCode();
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the current instance with another object of the same type.
		/// </summary>
		/// <param name="obj">An object to compare with this instance. </param>
		/// <returns>Returns 0 if the name and writing system is equal to <paramref name="obj"/>,
		/// less than zero if the name of this instance is less than <paramref name="obj"/>,
		/// or greater than zero if the name of this instance is greater than
		/// <paramref name="obj"/></returns>
		/// ------------------------------------------------------------------------------------
		public int CompareTo(object obj)
		{
			return Name.CompareTo(((NamedWritingSystem)obj).Name);
		}
	}
	#endregion

	#region Interface IDummy
	/// <summary>
	/// Interface for owners to convert dummy object member to a real one.
	/// </summary>
	public interface IDummy
	{
		/// <summary>
		/// Allows owners to convert dummy object member to a real one.
		/// </summary>
		/// <param name="owningFlid"></param>
		/// <param name="hvoDummy"></param>
		/// <returns></returns>
		ICmObject ConvertDummyToReal(int owningFlid, int hvoDummy);

		/// <summary>
		/// Notify owners that their dummy objects are about to become invalid.
		/// </summary>
		/// <param name="args"></param>
		bool OnPrepareToRefresh(object args);
	}
	#endregion

	/// <summary>
	/// class corresponding to the CmObject type in the FieldWorks SQL database.
	/// </summary>
	public class CmObject : ICmObject
	{
#if DEBUG
		/// <summary>True to make CmObjects check to make sure they are valid, false otherwise
		/// (Default is false)</summary>
		public static bool s_checkValidity = false;
#endif

		#region Data members
		/// <summary>
		/// Defines HVOs with special meanings
		/// </summary>
		public enum SpecialHVOValues: int
		{
			/// <summary>No owner is set</summary>
			kHvoOwnerPending = -1,
			/// <summary>Underlaying object was deleted</summary>
			kHvoUnderlyingObjectDeleted = -2
		};

		/// <summary>
		/// Defines tags (flids) with special meanings.
		/// </summary>
		public enum SpecialTagValues : int
		{
			/// <summary>
			/// Minimum tag value for virtual properties.
			/// This must match the value given in VwCacheDa.h.
			/// </summary>
			ktagMinVp = 0x7f000000
		}
		/// <summary>The HVO of this object</summary>
		protected int m_hvo;
		/// <summary> FdoCache object</summary>
		protected FdoCache m_cache;

		/// <summary></summary>
		internal static Dictionary<int, Type> s_classIdToType = new Dictionary<int, Type>();

		//this version can be used in static contexts, where you do not have an instance of the class.
		/// <summary>the Class$ number of this FieldWorks class </summary>
		public static readonly int kClassId;

		//this version can be used in switch statements
		/// <summary>the Class$ number of this FieldWorks class </summary>
		public const int kclsidCmObject = 0;

		/// <summary>
		/// Used when caching properties of an object(s).
		/// Subclasses, generated by the code generator, actually put something in here, though CmObject does not.
		/// Notice that this is not virtual, because each class returns all of the information for itself and
		/// its superclasses.  This is an optimization.
		/// </summary>
		/// <returns>an array of flids</returns>
		protected internal static readonly int[] OwningAtomicFlids = { };

		/// <summary>
		/// Used when caching vector properties of an object(s).
		/// Subclasses, generated by the code generator, actually put something in here, though CmObject does not.
		/// Notice that this is not virtual, because each class returns all of the information for itself and
		/// its superclasses.  This is an optimization.
		/// </summary>
		/// <returns>an array of flids</returns>
		protected internal static readonly int[] VectorFlids = { };

		/// <summary></summary>
		protected internal static readonly bool[] VectorIsSequence = { };

		/// <summary>
		/// Used when caching vector properties of an object(s).
		/// </summary>
		/// <remarks>
		/// Subclasses, generated by the code generator, actually put something in here, though CmObject does not.
		/// Notice that this is not virtual, because each class returns all of the information for itself and
		/// its superclasses.  This is an optimization.
		/// </remarks>
		/// <returns>an array of strings, which are  names of views in the FieldWorks database</returns>
		protected internal static readonly string[] VectorViewNames =  { ""/*dummy last one*/ };

		// Table keyed by C# type giving string like "HomographNo, IsIncludedAsHeadword"
		// that is the list of fields to load for the indicated class in LoadBasicData.
		// Does not include the 8 basic fields.
		// Enhance JohnH (JohnT): it would be even more elegant if the value stored were a struct
		// holding both the string and the IDbColSpec.
		private static Dictionary<Type, string> s_colNames = new Dictionary<Type, string>();

		/// <summary>Name of the view for this object in the database</summary>
		protected static readonly string FullViewName ="CmObject_";
		#endregion	// Data members

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the Object is complete (true)
		/// or if it still needs to have work done on it (false).
		/// Subclasses thaqt override this property should call the superclass property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsComplete
		{
			get
			{
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all linked objects. This includes all owned objects, and any objects,
		/// external to the ownership structure, that refer into any owned object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<LinkedObjectInfo> LinkedObjects
		{
			get
			{
				List<int> inParm = new List<int>(1);
				inParm.Add(m_hvo);
				return m_cache.GetLinkedObjects(inParm,
					LinkedObjectType.OwningAndReference, true, true, true,
					ReferenceDirection.Inbound, 0, false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all objects that refer to this object, but not to objects it owns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<LinkedObjectInfo> BackReferences
		{
			get
			{
				List<int> inParm = new List<int>(1);
				inParm.Add(m_hvo);
				return m_cache.GetLinkedObjects(inParm,
					LinkedObjectType.Reference, true, false, false,
					ReferenceDirection.Inbound, 0, false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Id of the object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Hvo
		{
			get { return m_hvo; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the id of the object's owner.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int OwnerHVO
		{
			get
			{
				return m_cache.GetObjProperty(m_hvo, (int)CmObjectFields.kflidCmObject_Owner);
			}
		}

		/// <summary>
		/// Owner for this object.
		/// </summary>
		public ICmObject Owner
		{
			get
			{
				return CmObject.CreateFromDBObject(Cache, OwnerHVO);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the owning flid for object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int OwningFlid
		{
			get
			{
				return m_cache.GetIntProperty(m_hvo, (int)CmObjectFields.kflidCmObject_OwnFlid);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the index of this object in the owner's collection
		/// </summary>
		/// <returns>Index in owner's collection, or -1 if not in collection.</returns>
		/// ------------------------------------------------------------------------------------
		public int IndexInOwner
		{
			get { return m_cache.GetObjIndex(OwnerHVO, this.OwningFlid, m_hvo); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the class ID of the object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int ClassID
		{
			get { return 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets delete status for the object.
		/// True means it can be deleted, otherwise false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool CanDelete
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the Guid for the object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid Guid
		{
			get { return m_cache.GetGuidProperty(Hvo, (int)CmObjectFields.kflidCmObject_Guid); }

			//This is needed for importing and testing
			set { m_cache.SetGuidProperty(Hvo, (int)CmObjectFields.kflidCmObject_Guid, value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the FDO cache representing the DB connection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get	{return m_cache;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a list of ClassAndPropInfo objects giving information about the classes that can be
		/// stored in the owning properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<ClassAndPropInfo> PropsAndClassesOwnedBy
		{
			get { return m_cache.GetPropsAndClasses(ClassID, FieldType.kgrfcptOwning, true); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the shortest, non-abbreviated label for the content of this object.
		/// This is the name that you would want to show up in a chooser list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string ShortName
		{
			get
			{
				/* until this method is implemented for all objects that could be displayed in a list,
				 * the following will at least give us something to show for any class that implements
				 * a property called "Name" (which is pretty common for things that we know will be in a list)
				 */
				System.Reflection.PropertyInfo pi = this.GetType().GetProperty("Name");
				if(pi != null)
				{
					object obj = pi.GetValue(this, null); // call Name on the class of this type
					MultiUnicodeAccessor accessor = obj as MultiUnicodeAccessor;
					if(accessor != null)
					{
						string name = accessor.AnalysisDefaultWritingSystem;
						if(name != null && name.Length >0)
							return name;
					}
					else if (obj is string)
						return (string)obj;
				}

				//oh well, at least tell us what the type of the class is.
				return String.Format(Strings.ksAX, this.GetType().Name);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a TsString that represents this object as it could be used in a deletion confirmaion dialogue.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortNameTSS.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual ITsString DeletionTextTSS
		{
			get { return ShortNameTSS; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a TsString that represents the shortname of this object.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than
		/// the regular ShortName string.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual ITsString ShortNameTSS
		{
			get
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString(ShortName, m_cache.DefaultAnalWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than
		/// the regular ShortNameTSS string.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual ITsString ChooserNameTS
		{
			get { return ShortNameTSS; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sort key for sorting a list of ShortNames.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string SortKey
		{
			get { return ShortName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a secondary sort key for sorting a list of ShortNames.  Defaults to zero.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int SortKey2
		{
			get { return 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an alphabetic version of SortKey2. This should always be used when appending
		/// to another string sort key, so as to get the right order for values greater than 9.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string SortKey2Alpha
		{
			get
			{
				int val = SortKey2;
				if (val < 0)
				{
					string sVal = (0 - val).ToString();
					return "-" + new String('0', 11 - sVal.Length) + sVal;
				}
				else
				{
					string sVal = val.ToString();
					return new String('0', 11 - sVal.Length) + sVal;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system for sorting a list of ShortNames.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string SortKeyWs
		{
			get
			{
				string sWs = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(
					m_cache.DefaultAnalWs);

				if (sWs == null || sWs == string.Empty)
					sWs = m_cache.FallbackUserLocale;

				if (sWs == null || sWs == string.Empty)
					sWs = "en";

				return sWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This allows each class to verify that it is OK to delete the object.
		/// If it is not Ok to delete the object, a message should be given explaining
		/// why the object can't be deleted.
		/// </summary>
		/// <returns>True if Ok to delete.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool ValidateOkToDelete()
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// returns the object found in the given flid
		/// </summary>
		/// <remarks> assumes that this property has been loaded into the cache,
		/// which was always true at the time of this writing.</remarks>
		/// <param name="flid"></param>
		/// <returns>a CmObject or null</returns>
		/// ------------------------------------------------------------------------------------
		public ICmObject GetObjectInAtomicField(int flid)
		{
			int hvo = m_cache.GetObjProperty(m_hvo, flid);
			if (hvo > 0)
				return CmObject.CreateFromDBObject(m_cache, hvo);
			else
				return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// get the objects found in the given flid
		/// </summary>
		/// <remarks> assumes that this property has been loaded into the cache,
		/// which was always true at the time of this writing.</remarks>
		/// <param name="flid"></param>
		/// <returns>A List containing CmObjects</returns>
		/// ------------------------------------------------------------------------------------
		public List<ICmObject> GetObjectsInVectorField(int flid)
		{
			List<ICmObject> objects = new List<ICmObject>();
			int[] hvos = m_cache.GetVectorProperty(m_hvo, flid, false);
			foreach(int hvo in hvos)
			{
				objects.Add(CmObject.CreateFromDBObject(m_cache, hvo));
			}
			return objects;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// get the objects found in the given flid
		/// </summary>
		/// <remarks> assumes that this property has been loaded into the cache,
		/// which was always true at the time of this writing.</remarks>
		/// <param name="flid"></param>
		/// <param name="maxNumber"></param>
		/// <returns>A List containing CmObjects</returns>
		/// ------------------------------------------------------------------------------------
		public List<ICmObject> GetObjectsInVectorField(int flid, int maxNumber)
		{
			List<ICmObject> objects = new List<ICmObject>();
			int[] hvos = m_cache.GetVectorProperty(m_hvo, flid, false);
			foreach(int hvo in hvos)
			{
				objects.Add(CmObject.CreateFromDBObject(m_cache, hvo));
				if(objects.Count >= maxNumber)
					break;
			}
			return objects;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the OwnOrd$ field
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int OwnOrd
		{
			get
			{
				return m_cache.GetIntProperty(m_hvo, (int)CmObjectFields.kflidCmObject_OwnOrd);
			}
		}

		#endregion	// Properties

		#region Construction and Initializing
		/// ------------------------------------------------------------------------------------
		/// <summary>Used for code like this foo.blah = new Blah()</summary>
		///	Here owner is not known so obj can't really be created yet.
		/// ------------------------------------------------------------------------------------
		public CmObject()	// Init must be called later (like, real quick!)
		{
			m_hvo = (int)SpecialHVOValues.kHvoOwnerPending;
			m_cache = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the CmObject class. It will be loaded if it has not already been.
		/// </summary>
		/// <param name="fcCache">fdoCache</param>
		/// <param name="hvo">HVO</param>
		/// ------------------------------------------------------------------------------------
		protected CmObject(FdoCache fcCache, int hvo)
		{
			InitExisting(fcCache, hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes an instance of the CmObject class for an existing object. It will be
		/// loaded if it has not already been.
		/// </summary>
		/// <param name="fcCache">fdoCache</param>
		/// <param name="hvo">HVO</param>
		/// ------------------------------------------------------------------------------------
		protected void InitExisting(FdoCache fcCache, int hvo)
		{
			bool fCheckValidity = false;
#if DEBUG
			if (s_checkValidity && !fcCache.IsDummyObject(hvo))
				fCheckValidity = true;
#endif
			// If the object's class is already cached, assume all the FDO preload data is too.
			// Occasionally we will get a miss, where the class has been loaded some other way,
			// but we will auto-load the properties actually used in that case, and on the other
			// hand we save a huge amount of time not reloading objects we've already loaded.
			bool fLoadIntoCache =
				!fcCache.MainCacheAccessor.get_IsPropInCache(hvo, (int)CmObjectFields.kflidCmObject_Class,
				(int)CellarModuleDefns.kcptInteger, 0);
			InitExisting(fcCache, hvo, fCheckValidity, fLoadIntoCache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the CmObject class
		/// </summary>
		/// <param name="fcCache">fdoCache</param>
		/// <param name="hvo">HVO</param>
		/// <param name="fCheckValidity"><c>true</c> to check if object is valid</param>
		/// <param name="fLoadIntoCache"><c>true</c> to load object into cache if not already loaded</param>
		/// ------------------------------------------------------------------------------------
		protected CmObject(FdoCache fcCache, int hvo, bool fCheckValidity, bool fLoadIntoCache)
		{
			InitExisting(fcCache, hvo, fCheckValidity, fLoadIntoCache);
		}

		/// ------------------------------------------------------------------------------------
		///<summary>Create a new FDO object, based on an existing database object.</summary>
		/// <param name="fcCache">The FDO cache object.</param>
		/// <param name="hvo">The database id of the existing object.</param>
		/// <param name="fCheckValidity">true if you are willing to have a round-trip to the database to verify the hvo</param>
		/// <param name="fLoadIntoCache">false if the object is already in the cache</param>
		///<exception cref="System.Exception">
		///Thrown when object is not valid.
		///This may be because the cache is missing, or the ID is bad, or not in database,
		///or that the class of the FDO object does not match the database class.
		///</exception>
		/// ------------------------------------------------------------------------------------
		protected virtual void InitExisting(FdoCache fcCache, int hvo, bool fCheckValidity, bool fLoadIntoCache)
		{
			Debug.Assert(fcCache != null);

			m_hvo = hvo;
			m_cache = fcCache;

			if(fCheckValidity && !IsValidObject())
			{
				m_hvo = (int)SpecialHVOValues.kHvoOwnerPending;
				m_cache = null;
				throw new System.ArgumentException("The object is not valid.");
			}
			if(fLoadIntoCache)
			{
				Debug.Assert(hvo > 0);
				LoadIntoCache();
			}
		}

		/// ------------------------------------------------------------------------------------
		///<summary>
		///Initialize a new FDO object, based on a newly created database object.
		///Override this method to perform additional initialization.
		///</summary>
		/// <param name="fcCache">The FDO cache object.</param>
		/// <param name="hvoOwner">ID of the owning object.</param>
		/// <param name="flidOwning">Field ID that will own the new object.</param>
		///<param name="ihvo">Index for where to insert new object in a sequence.
		///[Note: This parameter is ignored when ft is not kcptOwningSequence.]
		///</param>
		/// ------------------------------------------------------------------------------------
		protected internal virtual void InitNew(FdoCache fcCache, int hvoOwner,
			int flidOwning, int ihvo)
		{
			Debug.Assert(fcCache != null);
			Debug.Assert(hvoOwner > 0);
			Debug.Assert(flidOwning > 0);
			Debug.Assert(m_hvo == (int)SpecialHVOValues.kHvoOwnerPending);
			m_hvo = fcCache.CreateObject(ClassID, hvoOwner, flidOwning, ihvo);
			m_cache = fcCache;
			Debug.Assert(m_hvo > 0);
		}

		/// ------------------------------------------------------------------------------------
		///<summary>
		///Initialize a new ownerless FDO object,
		///based on a newly created database object.
		///Override this method to perform additional initialization.
		///</summary>
		/// <param name="fcCache">The FDO cache object.</param>
		/// ------------------------------------------------------------------------------------
		protected internal virtual void InitNew(FdoCache fcCache)
		{
			Debug.Assert(fcCache != null);
			Debug.Assert(m_hvo == (int)SpecialHVOValues.kHvoOwnerPending);
			m_hvo = fcCache.CreateObject(ClassID);
			m_cache = fcCache;
			Debug.Assert(m_hvo > 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Subclasses should override this, if special behavior is needed to initialize a new object.
		/// It may be public, but its only expected caller is the CreateObject methods of FdoCache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void InitNewInternal()
		{
		}
		#endregion	// Construction and Initializing

		#region Destruction

		/// <summary>
		/// The underlying object was deleted
		/// </summary>
		public void UnderlyingObjectDeleted()
		{
			m_cache = null;
			m_hvo = (int)SpecialHVOValues.kHvoUnderlyingObjectDeleted;
		}


		/// <summary>
		/// Delete the underlying object, without reporting progress.
		/// </summary>
		public void DeleteUnderlyingObject()
		{
			using (NullProgressState progress = new NullProgressState())
			{
				DeleteUnderlyingObject(progress);
			}
		}
		/// <summary>
		/// Delete the underlying object. Note that this is deliberatly NOT virtual. Special delete
		/// behaviors should be implemented on DeleteObjectSideEffects.
		/// </summary>
		public void DeleteUnderlyingObject(ProgressState state)
		{
			Set<int> idsToDelete = new Set<int>();
			FdoCache cache = m_cache; // BEFORE DeleteObjectSideEffects, which erases it!
			state.SetMilestone(Strings.ksGatheringInfo);
			state.Breath();
			DeleteObjectSideEffects(idsToDelete, state);
			state.SetMilestone(Strings.ksActualDelete);
			DeleteObjects(idsToDelete, cache);
		}

		/// <summary>
		/// This method is the one to override if you need side effects when DeleteUnderlyingObject
		/// is called. If other objects should be deleted also, do NOT delete them directly; this
		/// tends to produce abysmal performance. Rather, add them to objectsToDeleteAlso, and the
		/// whole lot (including this) will be deleted in one relatively efficient operation.
		/// You should not modify objectsToDeleteAlso except to add HVOs to it.
		/// You must not use the FDO object after calling this, it has been put into the deleted state.
		/// </summary>
		/// <param name="objectsToDeleteAlso">hashtable of HVOs (value typically just true, it's really a set).</param>
		/// <param name="state"></param>
		public virtual void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, ProgressState state)
		{
			// Default just puts our own hvo into the map so it actually gets deleted.
			objectsToDeleteAlso.Add(Hvo);
			UnderlyingObjectDeleted();
			if (state != null)
				state.Breath();
		}
		/// <summary>
		/// Class to handle Undo/Redo for deleting a collection of objects.
		/// </summary>
		class ObjectGroupUndoItem : UndoActionBase
		{
			FdoCache m_cache;
			Set<int> m_ids;
			IUndoAction m_delObjAction;
			bool m_fRequiresFullRefreshOfViewInUndoRedo = true;

			/// <summary>
			/// Make one.
			/// </summary>
			/// <param name="ids">Set in which the ids are used to delete/recreate.</param>
			/// <param name="hvoList">ids in the form of a comma delimited list in a string</param>
			/// <param name="cache"></param>
			/// <param name="fRequiresFullRefreshOfViewInUndoRedo">should be true, unless you know that deleting
			/// the object will not require completely refreshing/sync'ing a display during undo/redo. </param>
			/// <param name="fUndo">flag whether to create the undo action</param>
			public ObjectGroupUndoItem(Set<int> ids, string hvoList, FdoCache cache,
				bool fRequiresFullRefreshOfViewInUndoRedo, bool fUndo)
			{
				m_cache = cache;
				m_ids = ids;
				m_fRequiresFullRefreshOfViewInUndoRedo = fRequiresFullRefreshOfViewInUndoRedo;
				if (fUndo && cache.ActionHandlerAccessor != null)
				{
					IInitUndoDeleteObject udo = UndoDeleteObjectActionClass.Create();
					m_delObjAction = udo as IUndoAction;
					// mark this Com object as something that may need to be released when disposing the cache.
					cache.TrackComObject(udo);
					udo.GatherUndoInfo(hvoList, cache.DatabaseAccessor, cache.MetaDataCacheAccessor, cache.VwCacheDaAccessor);
				}
			}

			#region Overrides of UndoActionBase

			public override void Commit()
			{
				if (m_delObjAction == null)
					return;
				m_delObjAction.Commit();
				ReleaseComObject();
			}

			/// <summary>
			///	This helps to ensure that a stray UndoDeleteObjectAction doesn't prevent restores,
			/// just because this object hasn't been collected.
			/// </summary>
			private void ReleaseComObject()
			{
				if (m_delObjAction != null)
					m_cache.ReleaseComObject(m_delObjAction, true);
				m_delObjAction = null;
				m_cache = null;
			}

			/// <summary>
			/// This actually undoes the delete. It assumes we will do a Refresh to fix the cache.
			/// </summary>
			/// <returns></returns>
			public override bool Undo(bool fRefreshPending)
			{
				if (m_delObjAction != null)
					return m_delObjAction.Undo(fRefreshPending);
				else
					return false;
			}

			/// <summary>
			/// Return true to indicate that this undo item actually changes data.
			/// </summary>
			/// <returns></returns>
			public override bool IsDataChange()
			{
				return true;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Returns <c>true</c> because we do need an extra refresh of the view
			/// </summary>
			/// <returns>always <c>true</c></returns>
			/// ------------------------------------------------------------------------------------
			public override bool RequiresRefresh()
			{
				return m_fRequiresFullRefreshOfViewInUndoRedo;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Sets whether or not any subordinate undo actions should have their notifications
			/// supressed.
			/// </summary>
			/// <value></value>
			/// --------------------------------------------------------------------------------
			public override bool SuppressNotification
			{
				set
				{
					if (m_delObjAction != null)
					{
						m_delObjAction.SuppressNotification = value;
					}
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// This actually redoes the delete. It assumes we will do a Refresh to fix the cache.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public override bool Redo(bool fRefreshPending)
			{
				if (m_delObjAction != null)
					return m_delObjAction.Redo(fRefreshPending);
				else
					return false;
			}

			/// <summary>
			/// This does the original delete. We do our best to fix the cache without Refresh,
			/// though it's possible one or more deleted objects is still displayed somewhere
			/// as part of a reference attribute.
			/// </summary>
			/// <param name="vwClearInfoAction">specifies the level of information that needs to be cleared from the cache for each object. </param>
			/// <returns></returns>
			public bool DoIt(VwClearInfoAction vwClearInfoAction)
			{
				Set<KeyValuePair<int, int>> modifiedOwningProps = new Set<KeyValuePair<int, int>>();
				ISilDataAccess sda = m_cache.MainCacheAccessor;
				foreach (int hvo in m_ids)
				{
					if (m_cache.IsDummyObject(hvo))
						continue;	// don't clear it from the cache, or we won't be able to Undo.

					int hvoOwner = m_cache.GetOwnerOfObject(hvo);
					if (hvoOwner != 0 && !m_ids.Contains(hvoOwner))
					{
						// It has an owner and the owner is not also being deleted.
						int flid = m_cache.GetOwningFlidOfObject(hvo);
						// If we don't get one, we can't clean up the owning property; presume it has not
						// been loaded into the cache and doesn't need clearing.
						// Since it is a set, it won't matter if the same pair is added repeatedly.
						if (flid != 0)
							modifiedOwningProps.Add(new KeyValuePair<int, int>(hvoOwner, flid));
					}
				}
				// Forget anything we ever knew about the deleted objects and things they owned.
				m_cache.VwCacheDaAccessor.ClearInfoAboutAll(m_ids.ToArray(), m_ids.Count, vwClearInfoAction);
				// Delete all the objects at once

				// TODO (SteveM and SteveM): This block should probably be moved to a new
				// DbOps method.

				IOleDbCommand odc = null;
				IOleDbEncap dbAccess = m_cache.DatabaseAccessor;
				dbAccess.CreateCommand(out odc);
				try
				{
					// execute the command on groups of hvos
					string sSqlCommand = "exec DeleteObjects '{0}'";
					int iNextGroup = 0;
					while (true)
					{
						string hvoList = CmObject.MakePartialIdList(ref iNextGroup, m_ids.ToArray());
						if (hvoList == string.Empty)
							break; // Finished with the list of objects to delete

						COMException originalException = null;
						int cFailures = 0;
						while (true)
						{
							// Make sure there is a transaction or savepoint that we can rollback
							bool fNewTransaction = !dbAccess.IsTransactionOpen();
							string sSavePointName = string.Empty;
							if (fNewTransaction)
								dbAccess.BeginTrans();
							else
								dbAccess.SetSavePoint(out sSavePointName);
							try
							{
								odc.ExecCommand(string.Format(sSqlCommand, hvoList),
									(int)SqlStmtType.knSqlStmtStoredProcedure);
								if (fNewTransaction)
									dbAccess.CommitTrans();
								break; // Successfully completed the call so get out of the loop
							}
							catch (COMException ex)
							{
								if (cFailures++ == 0)
									originalException = ex;
								// An error occured while executing the command. Rollback whatever
								// we got so we don't keep the DB in a bad state.
								if (fNewTransaction)
									dbAccess.RollbackTrans();
								else
									dbAccess.RollbackSavePoint(sSavePointName);
								if (cFailures == 4)
								{
									// Tried 4 times to complete this command, but it still failed,
									// so just give up. Throw the first exception we got so we have
									// a better idea of what started the problem.
									throw originalException;
								}
							}
						}
					}
				}
				finally
				{
					DbOps.ShutdownODC(ref odc);
				}

				List<PropChangeInfo> propsToChange = new List<PropChangeInfo>();
				// Now we've actually made the change, clean up the cache.
				foreach (KeyValuePair<int, int> kvp in modifiedOwningProps)
				{
					int hvoOwner = kvp.Key;
					int flid = kvp.Value;
					int chvo;
					int cpt = m_cache.MetaDataCacheAccessor.GetFieldType((uint)flid);
					if (sda.get_IsPropInCache(hvoOwner, flid, cpt, 0))
					{
						// Since it is cached, we should fix the cache.
						if (cpt == (int)CellarModuleDefns.kcptOwningAtom)
						{
							m_cache.VwCacheDaAccessor.CacheObjProp(hvoOwner, flid, 0);
							propsToChange.Add(new PropChangeInfo(hvoOwner, flid, 0, 0, 1));
						}
						else
						{
							// Fix the vector or collection, removing ALL things in it that are in the
							// delete collection.
							int ihvoFirstDel = -1;
							int ihvoLastDel = -1;
							int[] contents;
							int chvoMax = sda.get_VecSize(hvoOwner, flid);
							using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(chvoMax, typeof(int)))
							{
								sda.VecProp(hvoOwner, flid, chvoMax, out chvo, arrayPtr);
								Debug.Assert(chvo == chvoMax);
								contents = (int[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(int));
							}
							// remove from currentValue all items in idsToDel
							int iout = 0;
							for (int i = 0; i < contents.Length; ++i)
							{
								if(!m_ids.Contains(contents[i]))
								{
									contents[iout++] = contents[i];
								}
								else
								{
									// object at i is being deleted.
									ihvoLastDel = i;
									if (ihvoFirstDel < 0)
										ihvoFirstDel = i;
								}
							}
							m_cache.VwCacheDaAccessor.CacheVecProp(hvoOwner, flid, contents, iout);
							if (ihvoFirstDel >= 0) // Just possible the cache is out of date or already fixed.
							{
								// Fix the display of the owning property.
								// We simulate a replace from the first to the last deleted object.
								int chvoDel = ihvoLastDel - ihvoFirstDel + 1;
								int chvoIns = chvoDel + iout - contents.Length;
								propsToChange.Add(new PropChangeInfo(hvoOwner, flid, ihvoFirstDel, chvoIns, chvoDel));
							}
						}
					}
					else
					{
						// It's not in the cache. This SHOULD mean nothing cares about it changing, but
						// sometimes we kick object info out of the cache for various reasons. Just in case,
						// issue a complete PropChanged for it. If it's really not in use, it won't take long.
						// (It may take a while to load, of course...but play safe. See LT-3945 for one problem.)
						// This may not give a totally accurate number for chvoDel, of course...best we can do.
						chvo = sda.get_VecSize(hvoOwner, flid); // will load it with correct current value
						propsToChange.Add(new PropChangeInfo(hvoOwner, flid, 0, chvo, chvo));
					}
				}
				// We save up all the prop changes until we've finished clearing things out of the cache
				// because of problems like LT-7972, where a CmPicture and CmFile were deleted, and the
				// PropChanged for deleting the CmFile triggered something that failed because we had not
				// yet cleared the owner of the CmPicture.
				foreach(PropChangeInfo info in propsToChange)
					info.DoIt(sda);
				return true;
			}

			#endregion

		}

		class PropChangeInfo
		{
			private int m_obj;
			private int m_flid;
			private int m_ihvo;
			private int m_cadd;
			private int m_cdel;
			public PropChangeInfo(int obj, int flid, int ihvo, int cadd, int cdel)
			{
				m_obj = obj;
				m_flid = flid;
				m_ihvo = ihvo;
				m_cadd = cadd;
				m_cdel = cdel;
			}

			public void DoIt(ISilDataAccess sda)
			{
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_obj, m_flid, m_ihvo, m_cadd, m_cdel);
			}
		}

		/// <summary>
		/// Delete the objects which are the ids in the given Set.
		/// Includes creating a suitable Undo action.
		/// </summary>
		/// <param name="idsToDel">Set of object IDs to be deleted.</param>
		/// <param name="cache"></param>
		public static void DeleteObjects(Set<int> idsToDel, FdoCache cache)
		{
			DeleteObjects(idsToDel, cache, true);
		}

		/// <summary>
		/// Delete the objects which are the ids in the Set.
		/// Includes creating a suitable Undo action.
		/// </summary>
		/// <param name="idsToDel">Set of object IDs to be deleted.</param>
		/// <param name="cache"></param>
		/// <param name="vwClearInfoAction">specifies the level of information to clear from the cache for each id.</param>
		public static void DeleteObjects(Set<int> idsToDel, FdoCache cache, VwClearInfoAction vwClearInfoAction)
		{
			DeleteObjects(idsToDel, cache, true, vwClearInfoAction);
		}

		/// <summary>
		/// Delete the objects which are the ids in the Set.
		/// </summary>
		public static void DeleteObjects(Set<int> idsToDel, FdoCache cache, bool fRequiresFullRefreshOfViewInUndoRedo)
		{
			DeleteObjects(idsToDel, cache, fRequiresFullRefreshOfViewInUndoRedo, VwClearInfoAction.kciaRemoveAllObjectInfo);
		}

		/// <summary>
		/// Delete the objects which are the ids in the Set.
		/// </summary>
		public static void DeleteObjects(Set<int> idsToDel, FdoCache cache, bool fRequiresFullRefreshOfViewInUndoRedo, VwClearInfoAction vwClearInfoAction)
		{
			DeleteObjects(idsToDel, cache, fRequiresFullRefreshOfViewInUndoRedo, vwClearInfoAction, true, null);
		}

		/// <summary>
		/// Delete the objects which are the ids in the Set.
		/// </summary>
		public static void DeleteObjects(Set<int> idsToDel, FdoCache cache,
			bool fRequiresFullRefreshOfViewInUndoRedo,
			VwClearInfoAction vwClearInfoAction, bool fUndo, ProgressState state)
		{
			Set<int> realIdsToDel = new Set<int>();
			foreach (int hvo in idsToDel)
			{
				if (cache.IsDummyObject(hvo))
					continue;	// don't try removing dummy object from cache. otherwise, you can't undo it.
				realIdsToDel.Add(hvo);
			}
			if (realIdsToDel.Count == 0)
				return;
			if (fUndo && realIdsToDel.Count == 1 && fRequiresFullRefreshOfViewInUndoRedo || cache.DatabaseAccessor == null)
			{
				// Just one object...more efficient to to it directly, also, it lets many
				// tests pass that would otherwise need a real database.
				foreach (int hvo in realIdsToDel)
				{
					cache.DeleteObject(hvo);
				}
			}
			else
			{
				IActionHandler acth = cache.ActionHandlerAccessor;
				if (acth == null)
					fUndo = false;	// May be null if we are currently suppressing Subtasks
				int iMin = 0;
				int[] ids = realIdsToDel.ToArray();
				int step = 50;
				if (state != null)
				{
					// 600 4-digit ids, 500 5-digit ids, 428 6-digit ids, 375 7-digit ids
					int cChunks = (ids.Length / 450) + 1;
					step = 60 / cChunks;
					if (step == 0)
						step = 1;
				}
				while (iMin < ids.Length)
				{
					int iLim = iMin;
					string hvoList = CmObject.MakePartialIdList(ref iLim, ids);
					ObjectGroupUndoItem item;
					if (iMin > 0 || iLim < ids.Length)
					{
						Set<int> idsDel = new Set<int>(iLim - iMin);
						for (int i = iMin; i < iLim; ++i)
							idsDel.Add(ids[i]);
						item = new ObjectGroupUndoItem(idsDel, hvoList, cache,
							fRequiresFullRefreshOfViewInUndoRedo, fUndo);
					}
					else
					{
						item = new ObjectGroupUndoItem(realIdsToDel, hvoList, cache,
							fRequiresFullRefreshOfViewInUndoRedo, fUndo);
					}
					item.DoIt(vwClearInfoAction);
					if (fUndo)
						acth.AddAction(item);
					if (state != null)
					{
						int percent = state.PercentDone + step;
						state.PercentDone = percent;
						state.Breath();
					}
					iMin = iLim;
				}
			}
		}

		/// <summary>
		/// Count the number of base objects that would be deleted by DeleteOrphanedObjects().
		/// </summary>
		public static int CountOrphanedObjects(FdoCache cache)
		{
			int cOrphans = 0;
			string sQry = "SELECT COUNT(lr.Id) " +
				"FROM LexReference lr " +
				"LEFT OUTER JOIN LexReference_Targets t ON t.Src=lr.Id " +
				"WHERE t.Dst IS NULL";
			DbOps.ReadOneIntFromCommand(cache, sQry, null, out cOrphans);
			sQry = "SELECT COUNT(msa.id) " +
				"FROM MoMorphSynAnalysis_ msa " +
				"LEFT OUTER JOIN LexSense sen ON sen.MorphoSyntaxAnalysis=msa.id " +
				"WHERE sen.MorphoSyntaxAnalysis IS NULL AND msa.OwnFlid$=5002009";
			int cOrphanMSAs = 0;
			if (DbOps.ReadOneIntFromCommand(cache, sQry, null, out cOrphanMSAs))
				cOrphans += cOrphanMSAs;
			return cOrphans;
		}

		/// <summary>
		/// Delete objects which may have been orphaned by a previous deletion.
		/// </summary>
		public static void DeleteOrphanedObjects(FdoCache cache, bool fUndo, ProgressState state)
		{
			string sQry = "SELECT DISTINCT lr.Id " +
				"FROM LexReference lr " +
				"LEFT OUTER JOIN LexReference_Targets t ON t.Src=lr.Id " +
				"WHERE t.Dst IS NULL";
			DeleteAnyOrphansFound(cache, fUndo, state, sQry);

			sQry = "SELECT DISTINCT msa.id " +
				"FROM MoMorphSynAnalysis_ msa " +
				"LEFT OUTER JOIN LexSense sen ON sen.MorphoSyntaxAnalysis=msa.id " +
				"WHERE sen.MorphoSyntaxAnalysis IS NULL AND msa.OwnFlid$=5002009";
			DeleteAnyOrphansFound(cache, fUndo, state, sQry);

			sQry = "DECLARE @human INT; " +
				"SELECT @human = id FROM CmAgent_ WHERE Guid$ = '9303883A-AD5C-4CCF-97A5-4ADD391F8DCB'; " +
				"SELECT DISTINCT wa.id FROM WfiMorphBundle_ mb " +
				"JOIN WfiAnalysis wa ON wa.id = mb.owner$ " +
				"LEFT OUTER JOIN CmAgentEvaluation_ aeh ON aeh.target = wa.id AND aeh.owner$ = @human " +
				"LEFT OUTER JOIN MoMorphSynAnalysis msa ON msa.Id = mb.Msa " +
				"WHERE aeh.id IS NULL AND msa.Id IS NULL";
			DeleteAnyOrphansFound(cache, fUndo, state, sQry);

			sQry = "SELECT DISTINCT ae.id FROM CmAgentEvaluation ae " +
				"LEFT OUTER JOIN CmObject co ON co.id = ae.target " +
				"WHERE co.Id IS NULL";
			DeleteAnyOrphansFound(cache, fUndo, state, sQry);

			sQry = "declare @vern int, @fmtVern varbinary(4000) " +
				"select top 1 @vern = dst from LangProject_CurVernWss " +
				"select top 1 @fmtVern = Fmt from WfiMorphBundle_Form where ws = @vern order by Fmt " +
				"insert into MultiStr$ " +
				"select 5112001, mb.id, wf.ws, wf.txt, @fmtVern from WfiMorphBundle_ mb " +
				"join WfiAnalysis_ wa on wa.id = mb.owner$ " +
				"left outer join WfiWordform_Form wf on wf.obj = wa.owner$ and wf.ws = @vern " +
				"left outer join WfiMorphBundle_Form mbf on mbf.obj = mb.id " +
				"where mb.morph is null and mb.msa is null and mb.sense is null and mbf.txt is null and @fmtVern is not null";
			DbOps.ExecuteStatementNoResults(cache, sQry, null);

			sQry = "UPDATE WfiMorphBundle SET msa = sen.MorphoSyntaxAnalysis " +
				"FROM WfiMorphBundle mb " +
				"LEFT OUTER JOIN MoMorphSynAnalysis msa ON msa.Id=mb.Msa " +
				"JOIN LexSense sen ON sen.Id=mb.Sense " +
				"WHERE msa.Id IS NULL";
			DbOps.ExecuteStatementNoResults(cache, sQry, null);

			sQry = "SELECT DISTINCT map.Id FROM MoMorphAdhocProhib map " +
				"LEFT OUTER JOIN MoMorphAdhocProhib_Morphemes mapm ON mapm.Src=map.Id " +
				"LEFT OUTER JOIN MoMorphSynAnalysis msa on msa.Id=mapm.Dst " +
				"WHERE msa.Id IS NULL";
			DeleteAnyOrphansFound(cache, fUndo, state, sQry);
		}

		private static void DeleteAnyOrphansFound(FdoCache cache, bool fUndo, ProgressState state, string sQry)
		{
			int[] orphans = DbOps.ReadIntArrayFromCommand(cache, sQry, null);
			if (orphans.Length > 0)
			{
				Set<int> ids = new Set<int>();
				ids.AddRange(orphans);
				CmObject.DeleteObjects(ids, cache, true, VwClearInfoAction.kciaRemoveAllObjectInfo, fUndo, null);
				if (state != null)
				{
					int percent = state.PercentDone + 5;
					state.PercentDone = percent;
					state.Breath();
				}
			}
		}
		#endregion	// Destruction

		#region	Set atomic properties

		///<summary>Set an atomic owning property.</summary>
		///<param name="flid">Field ID in which new object will be owned.</param>
		///<param name="objNewValue">Newly owned object.</param>
		///<exception cref="System.ArgumentException">
		///Thrown when flid is not an atomic owning property.
		///</exception>
		protected void SetOwningProperty(int flid, ICmObject objNewValue)
		{
			Debug.Assert(m_cache !=null);

			if (m_cache.GetFieldType(flid) != FieldType.kcptOwningAtom)
				throw new System.ArgumentException("flid is not an atomic owning property.",
					"flid");

			int hvoCurrent = m_cache.GetObjProperty(m_hvo, flid);

			if ((objNewValue != null)
				&& (hvoCurrent == objNewValue.Hvo))
				return;	// Same ownee, so just quit.

			if(hvoCurrent > 0)
				m_cache.DeleteObject(hvoCurrent);	// Get rid of old geezer.

			if (objNewValue == null)
				return;		// Quit, if no new value.

			// Was this obj just now created with a statement like this: foo.blah = new Blah()?
			// then we need to actually create the underlying object
			if(objNewValue.Hvo == (int)CmObject.SpecialHVOValues.kHvoOwnerPending)
			{
				(objNewValue as CmObject).InitNew(m_cache, m_hvo, flid, 0);
				return;
			}
			m_cache.ChangeOwner(objNewValue.Hvo, m_hvo, flid);
		}


		///<summary>Set an atomic reference property.</summary>
		///<param name="flid">Field ID from which new object will be referred.</param>
		///<param name="objNewValue">Newly referred to object.</param>
		///<exception cref="System.ArgumentException">
		///Thrown when objNewValue is less than 1.
		///</exception>
		///<exception cref="System.ArgumentException">
		///Thrown when objNewValue has no owner.
		///</exception>
		protected void SetReferenceProperty(int flid, ICmObject objNewValue)
		{
			Debug.Assert(m_cache !=null);
			if (objNewValue == null)
			{
				int hvo = m_cache.GetObjProperty(m_hvo, flid);
				m_cache.RemoveReference(m_hvo, flid, hvo);
				return;
			}
			if (objNewValue.Hvo < 1)
				throw new System.ArgumentException("The ID of the new value is invalid.", "objNewValue");
			bool targetIsOwnerless = (objNewValue.ClassID == FDO.LangProj.LangProject.kClassId
				|| objNewValue.ClassID == FDO.Cellar.LgWritingSystem.kClassId
				|| objNewValue.ClassID == FDO.Cellar.UserView.kClassId
				|| objNewValue.ClassID == FDO.Cellar.CmPicture.kClassId // In FLEx pictures are owned, but they aren't required to be (e.g. in Scripture)
				|| objNewValue.ClassID == FDO.Ling.ReversalIndex.kClassId);
			if (objNewValue.OwnerHVO < 1
				&& !targetIsOwnerless)
				throw new System.ArgumentException("The new value has no owner.", "objNewValue");

			// Go ahead and set it.
			m_cache.SetObjProperty(m_hvo, flid, objNewValue.Hvo);
		}


		#endregion	// Set atomic properties

		#region Caching
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// load the data which is found in the tables representing the class and its superclasses
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="fdoClassType">the CSharp type corresponding to the array of hvos</param>
		/// <param name="hvos">If null, just load all of the objects of this type(much faster for long lists)</param>
		/// ------------------------------------------------------------------------------------
		private static void LoadBasicData(FdoCache cache, Type fdoClassType, int[] hvos)
		{
			if (hvos != null && hvos.Length == 0 || cache.VwOleDbDaAccessor == null)
				return; // nothing needs to be loaded (and our query would fail with empty list), or non-database cache.
			Debug.Assert(fdoClassType != null);

			// populate column spec
			IDbColSpec dcs = DbColSpecClass.Create();
			AddBasicFieldsToColumnSpec(fdoClassType, dcs, cache);

			// Build select query
			string sViewName = (string)GetStaticField(fdoClassType, "FullViewName");
			Debug.Assert(sViewName != null);

			// We want to use explicit field names here because custom fields don't get added to dcs.
			StringBuilder sQryBldr = new StringBuilder("select id, guid$, class$, owner$, ownflid$, ownord$, updstmp, upddttm");
			string fields = "";
			if (s_colNames.ContainsKey(fdoClassType))
			{
				fields = s_colNames[fdoClassType];
			}
			else
			{
				IFwMetaDataCache mdc = cache.MetaDataCacheAccessor;
				if (mdc != null)
				{
					// Should never be null, except when testing with a mock cache.
					StringBuilder fieldsBldr = new StringBuilder("");
					int cspec;
					dcs.Size(out cspec);
					for (int i = 8; i < cspec; i++)
					{
						int tag;
						dcs.GetTag(i, out tag);
						string fieldName = mdc.GetFieldName((uint)tag);
						int oct;
						dcs.GetDbColType(i, out oct);
						fieldsBldr.AppendFormat(", [{0}", fieldName);
						if (oct == (int)DbColType.koctFmt)
							fieldsBldr.Append("_Fmt");
						fieldsBldr.Append("]");
					}
					// NB: Don't cache the result if no mdc, as later tests may have one!
					fields = fieldsBldr.ToString();
					s_colNames[fdoClassType] = fields;
				}
			}
			if (fields.Length > 0)
				sQryBldr.Append(fields);
			sQryBldr.AppendFormat(" FROM {0} ", sViewName);

			if (hvos != null)
			{
				sQryBldr.Append(" where id in (");
				int iNextGroup = 0;
				while (iNextGroup < hvos.Length)
				{
					string sQry2 = String.Format("{0}{1})", sQryBldr.ToString(), CmObject.MakePartialIdList(ref iNextGroup, hvos));
					cache.LoadData(sQry2, dcs, 0);
				}
			}
			else
			{
				cache.LoadData(sQryBldr.ToString(), dcs, 0);
			}
			System.Runtime.InteropServices.Marshal.ReleaseComObject(dcs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// using reflection, get the value of a static property
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static object GetStaticField(Type fdoClassType,string propertyName)
		{
			System.Reflection.FieldInfo fiView = fdoClassType.GetField(propertyName,
				System.Reflection.BindingFlags.NonPublic |
				System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.FlattenHierarchy |
				System.Reflection.BindingFlags.Static);
			Debug.Assert(fiView != null);
			return fiView.GetValue(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// invoke a method which populates the columnSpec
		/// </summary>
		/// <param name="fdoClassType"></param>
		/// <param name="columnSpec"></param>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		static private void AddBasicFieldsToColumnSpec(Type fdoClassType, IDbColSpec columnSpec, FdoCache cache)
		{
			// Change (EberhardB): don't call PopulateCsBasic/FullViewName on the base
			// class any more, because we might have additional classes where the base
			// class doesn't implement these methods. Providing the FlattenHierarchy
			// flag takes whatever it finds.
			System.Reflection.MethodInfo mi = fdoClassType.GetMethod("PopulateCsBasic",
				System.Reflection.BindingFlags.NonPublic |
				System.Reflection.BindingFlags.FlattenHierarchy |
				System.Reflection.BindingFlags.Static);
			Debug.Assert(mi != null);
			object[] prms = new Object[1];
			prms[0] = columnSpec;
			//review JH(JH): having the cache listed here has the "obj" parameter has me confused...
			//	this attribute does not appear to be available to the invoked method...
			//	would it be clearer to just send NULL in this parameter?
			mi.Invoke(cache, prms); // call PopulateCsBasic() on the class of this type
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// load into the cache the IDs of the objects which are owned in atomic properties by these objects
		/// </summary>
		/// <param name="cache">FdoCache</param>
		/// <param name="csharpType">Type of the objects to load</param>
		/// <param name="hvos">an array of hvos of homogeneous objects, which
		/// all must be of type csharpType (not checked).
		/// Set to null if you just want to load all of the objects of this type.</param>
		/// <param name="classId">Required if hvos is null</param>
		/// ------------------------------------------------------------------------------------
		public static void LoadOwningAtomicData(FdoCache cache,Type csharpType, int[] hvos, int classId)
		{
			Debug.Assert(csharpType != null);
			//hvos == null is now allowed  Debug.Assert(hvos != null);
			if(hvos != null)
			{
				Debug.Assert(hvos.Length>0);
			}
			if (cache.VwOleDbDaAccessor == null)
				return; // some sort of memory cache, assume preloaded.

			string sSelectClause = "obj.Id, ";
			string sFromClause="CmObject as obj ";
			string sWhereClause="";

			// begin the column spec.  Later, we will push a new item for each field.
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);	// ID

			// begin the where clause, which will be added to later by BuildOwningAtomicLoadSpec()
			//sWhereClause = "obj.Id in (";
			if(hvos == null)
			{
				Debug.Assert(classId > -1, "You must provide a classId to if you are not specifying the hvos.");
				sWhereClause = " where obj.class$ = " + classId.ToString();
			}
			else
			{
				sWhereClause = " where obj.Id in (";
				for(int i =hvos.Length -1; i>0; i--)
				{
					if (hvos[i] > 0)
						sWhereClause += hvos[i].ToString() + ",";
				}
				if (hvos[0] > 0)
					sWhereClause += hvos[0].ToString();
				sWhereClause += ") ";	// no "," after this last one
			}

			BuildOwningAtomicLoadSpec(csharpType, dcs, ref sSelectClause, ref sFromClause);

			//put all of these pieces together into a select statement
			//string sQuery ="select " +sSelectClause+" From " + sFromClause + "  where " + sWhereClause;
			string sQuery ="select " +sSelectClause+" from " + sFromClause + sWhereClause;
			cache.LoadData(sQuery, dcs, 0);
			System.Runtime.InteropServices.Marshal.ReleaseComObject(dcs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collect the information needed to load one or more members of the
		/// given class into the cache.
		/// </summary>
		/// <param name="csharpType">Class of objects that will be loaded.</param>
		/// <param name="dcs">
		/// Column spec, which is assumed to already have the first column loaded.
		/// </param>
		/// <param name="sSelectClause">SQL select clause("select" not included)</param>
		/// <param name="sFromClause">SQL from clause ("From" not included)</param>
		/// ------------------------------------------------------------------------------------
		private static void BuildOwningAtomicLoadSpec(Type csharpType, IDbColSpec dcs, ref string sSelectClause,   ref string sFromClause)
		{
			// note: the "FlattenHierarchy" is needed here because the class we are giving it is "XXX" one,
			//	but this property is on its superclass,"BaseXXXX"
			System.Reflection.FieldInfo fi = csharpType.GetField("OwningAtomicFlids",
				System.Reflection.BindingFlags.FlattenHierarchy |
				System.Reflection.BindingFlags.NonPublic | //these are marked "internal"
				System.Reflection.BindingFlags.Static);
			Debug.Assert(fi != null);
			int[] flids = (int[]) fi.GetValue(null);
			foreach(int flid in flids)
			{
				if(flid !=0) // 0 is an artifact of the xslt generator
				{
					dcs.Push((int)DbColType.koctObj, 1, (int)flid, 0);

					// (SteveMiller) This code originally built a query using
					// left outer joins. This is actually faster than using
					// subselects. However, The complexity of the query was
					// causing table spooling in the large English Websters
					// and Bible database, making it considerably slower. It
					// also was coming up with inexplicable "missing join
					// predicate" errors on the first five of the table spools.
					// (23 March 2007)

					sSelectClause += " (select Id from CmObject where Owner$ = obj.Id and OwnFlid$ = "
						+ flid.ToString() + "), ";
				}
			}
			int commapos = sSelectClause.LastIndexOf(',');
			if (commapos >= 0)
				sSelectClause = sSelectClause.Remove(commapos, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// given a type and an array of hvos of objects of that type, cache the vector properties of these objects
		/// </summary>
		/// <param name="cache">FdoCache</param>
		/// <param name="csharpType"></param>
		/// <param name="hvos">an array of hvos of homogeneous objects, which all must be of
		/// type csharpType (not checked).
		/// If null, just load all of the objects of this type(much faster for long lists)</param>
		/// ------------------------------------------------------------------------------------
		public static void LoadVectorData(FdoCache cache, Type csharpType, int[] hvos)
		{
			Debug.Assert(csharpType != null);
			//NO: hvos=null now means we should load them all (*much faster*): Debug.Assert(hvos != null);
			if (hvos != null)
			{
				Debug.Assert(hvos.Length>0);
			}

			// note: the "FlattenHierarchy" is needed here because the class we are giving it is "XXX" one,
			//	but this property is on its superclass,"BaseXXXX"
			System.Reflection.FieldInfo fi = csharpType.GetField("VectorFlids",
				System.Reflection.BindingFlags.FlattenHierarchy |
				System.Reflection.BindingFlags.NonPublic | //these are marked "internal"
				System.Reflection.BindingFlags.Static);
			Debug.Assert(fi != null);
			int[] flids = (int[]) fi.GetValue(null);

			fi = csharpType.GetField("VectorViewNames",
				System.Reflection.BindingFlags.FlattenHierarchy |
				System.Reflection.BindingFlags.NonPublic | //these are marked "internal"
				System.Reflection.BindingFlags.Static);
			Debug.Assert(fi != null);
			string[] views = (string[]) fi.GetValue(null);

			fi = csharpType.GetField("VectorIsSequence",
				System.Reflection.BindingFlags.FlattenHierarchy |
				System.Reflection.BindingFlags.NonPublic | //these are marked "internal"
				System.Reflection.BindingFlags.Static);
			Debug.Assert(fi != null);
			bool[] sequenceBools = (bool[]) fi.GetValue(null);

			for(int i=0; i < flids.Length; i++)
			{
				if(flids[i] !=0) // 0 is an artifact of the xslt generator
				{
					IDbColSpec dcs = DbColSpecClass.Create();
					//dcs.Clear();
					dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
					dcs.Push((int)DbColType.koctObjVec, 1, (int)flids[i], 0);

					//Enhance (JH): this would be much faster using the stringBuilder, for large lists

					// BUILD THE QUERY FOR THIS VECTOR PROPERTY
					string sQry = "select * from " + views[i] + " ";

					if(hvos != null)
					{
						sQry += "where src in ("; // don't just add to original sQry, that is used in else branch.
						int iNextGroup = 0;
						while (iNextGroup < hvos.Length)
						{
							string sQry2 = sQry + CmObject.MakePartialIdList(ref iNextGroup, hvos) + ") order by Src";
							// See John Hatton's comment below about ordering, which was made before this section was added.
							if(sequenceBools[i])
								sQry2 += ", ord";
							cache.LoadData(sQry2, dcs, 0);
						}
					}
					else
					{
						/*	(John Hatton) I removed this ordering (12 Nov 2002) because it can cause the underlying cache to miss some values.
							For example, with "order by ord", we get the following:
								Src         Dst         Ord
							----------- ----------- -----------
							11656       9183        0
							11657       9183        0
							11658       9183        0
							11659       9183        0
							11659       1596        1
							11658       2269        1
							11657       4565        1
							11656       4572        1

							Here, the objects 11656, 11657, and 11658 were getting the element 918 (i.e. the ord 0 elements),
							but were not getting the ord 1 elements.  Only object 11659 was getting both of its elements correctly,
							because there was no break.  In other words, it is as if the underlying cache requires that these things
							be sorted by Src.

							if(sequenceBools[i])
								sQry += " order by ord";
						*/

						sQry += "order by Src";
						if(sequenceBools[i])
							sQry += ", ord";

						cache.LoadData(sQry, dcs, 0);
					}
					System.Runtime.InteropServices.Marshal.ReleaseComObject(dcs);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// load this CmObject from the database into the cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void LoadIntoCache()
		{
			Type tType = this.GetType();
			Debug.Assert( tType!= null);

			//here we are stepping into the static methods
			LoadObjectsIntoCache(m_cache, tType, new int[]{m_hvo});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the given objects into the cache
		/// </summary>
		/// <remarks> This is a static method because it is used outside of the context
		/// of an instance of CmObject. That is, it is used when we are trying to instantiate one or more CmObjects
		/// which do not exist yet.
		/// </remarks>
		/// <param name="cache">fdoCache</param>
		/// <param name="csharpType">all the object must match this csharp class</param>
		/// <param name="hvos">an array of the IDs of 0 or more homogeneous objects.
		/// Set to null if you just want to load all of the objects of this type.</param>
		/// ------------------------------------------------------------------------------------
		public static void LoadObjectsIntoCache(FdoCache cache, Type csharpType, int[] hvos)
		{
			LoadObjectsIntoCache(cache, csharpType, hvos, -1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the given objects into the cache
		/// </summary>
		/// <remarks> This is a static method because it is used outside of the context
		/// of an instance of CmObject. That is, it is used when we are trying to instantiate one or more CmObjects
		/// which do not exist yet.
		/// </remarks>
		/// <param name="cache">fdoCache</param>
		/// <param name="csharpType">all the object must match this csharp class</param>
		/// <param name="hvos">an array of the IDs of 0 or more homogeneous objects.
		/// Set to null if you just want to load all of the objects of this type.</param>
		/// <param name="classId">provide if hvos is null </param>
		/// ------------------------------------------------------------------------------------
		public static void LoadObjectsIntoCache(FdoCache cache, Type csharpType, int[] hvos, int classId)
		{
			// If we're in a load-all mode, it's actually more efficient to let the cache do it,
			// since it will refrain from doing it repeatedly, and will get everything in one call.
			if (cache.VwOleDbDaAccessor == null)
				return; // can't do any useful preloading.
			AutoloadPolicies alp = cache.VwOleDbDaAccessor.AutoloadPolicy;
			switch(alp)
			{
				case AutoloadPolicies.kalpLoadAllOfClassForReadOnly:
				case AutoloadPolicies.kalpLoadForAllOfBaseClass:
				case AutoloadPolicies.kalpLoadAllOfClassIncludingAllVirtuals:
				case AutoloadPolicies.kalpLoadForAllOfObjectClass:
					return;
			}
			Debug.Assert(cache != null);
			Debug.Assert(csharpType != null);
			//don't want to assert this anymore Debug.Assert(hvos != null);

			if (hvos != null && hvos.Length ==0)
				return;

			// data is divided into three categories:
			// load the data which is found in the tables representing the class and its superclasses
			LoadBasicData(cache, csharpType, hvos);

			// load the IDs of the objects which are owned in atomic properties by these objects
			LoadOwningAtomicData(cache, csharpType, hvos,classId);

			// load the IDs of the objects which are contained in collection or sequence properties of these objects
			// JohnT: no point in doing this for just one object, because it loads them one query per property,
			// and the VwOleDbDa cache will do as well on demand (and not at all for props never used).
			// If hvos is null we are loading all of type so go ahead.
			if (hvos == null || hvos.Length > 1)
				LoadVectorData(cache, csharpType, hvos);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load all fields of all objects of this type that have a owning flid contained in
		/// flids.
		/// </summary>
		/// <param name="fdoClassType">The class of the object</param>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="flids">The owning fields of the owner of the objects we want to load
		/// data for. Might be <c>null</c> for ownerless objects</param>
		/// <param name="parentViewName">The view name of the parent class.</param>
		/// ------------------------------------------------------------------------------------
		public static void LoadDataForFlids(Type fdoClassType, FdoCache cache, int[] flids,
			string parentViewName)
		{
			Debug.Assert(flids == null || flids.Length != 0);

			string sViewName = (string)GetStaticField(fdoClassType, "FullViewName");
			Debug.Assert(sViewName != null);

			StringBuilder sQry = new StringBuilder("select cmo.* from " + sViewName + " cmo");
			if (flids != null)
			{
				sQry.Append(" join " +
					parentViewName + " x on cmo.owner$ = x.id and x.ownflid$ in (");
				foreach (int flid in flids)
				{
					sQry.Append(flid);
					sQry.Append(",");
				}
				// replace last , with )
				sQry[sQry.Length - 1] = ')';
			}

			IDbColSpec dcs = DbColSpecClass.Create();
			try
			{
				AddBasicFieldsToColumnSpec(fdoClassType, dcs, cache);

				cache.LoadData(sQry.ToString(), dcs, 0);
			}
			finally
			{
				Marshal.ReleaseComObject(dcs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// fill in the columnSpec with the columns which contain the basic cable information,
		/// corresponding to a "classname_" SQL view
		/// </summary>
		/// <remarks> This is a static method because it is used outside of the context
		/// of an instance of CmObject. That is, it is used when we are trying to instantiate
		/// one or more CmObjects which do not exist yet. Although this is a static method, it
		/// is used something like a virtual method.
		/// That is, the corresponding method on the actual class of the object is first called,
		/// and that then calls the corresponding method on its base class. this continues until
		/// this method, on CmObject, is called.</remarks>
		/// <param name="cs">the columnSpec which will be added to</param>
		/// ------------------------------------------------------------------------------------
		protected static void PopulateCsBasic(IDbColSpec cs)
		{
			cs.Push((int)DbColType.koctBaseId, 0, 0, 0);	//id
			cs.Push((int)DbColType.koctGuid, 1,
				(int)CmObjectFields.kflidCmObject_Guid, 0);
			cs.Push((int)DbColType.koctInt, 1,
				(int)CmObjectFields.kflidCmObject_Class, 0);
			cs.Push((int)DbColType.koctObj, 1,
				(int)CmObjectFields.kflidCmObject_Owner, 0);
			cs.Push((int)DbColType.koctInt, 1,
				(int)CmObjectFields.kflidCmObject_OwnFlid, 0);
			cs.Push((int)DbColType.koctInt, 1,
				(int)CmObjectFields.kflidCmObject_OwnOrd, 0);//OwnOrd$
			cs.Push((int)DbColType.koctTimeStamp, 1, 0, 0); //UpdStmp$
			cs.Push((int)DbColType.koctTime, 1, 0, 0);// UpdDttm$
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add any custom fields to the columnSpec.
		/// </summary>
		/// <remarks> note that we should be able to use the code here to handle the entire CS
		/// creation, since all fields are listed in Field$.  for now, I am just using this
		/// for custom fields in order to have the least impact on the code.</remarks>
		/// ------------------------------------------------------------------------------------
		protected static void PopulateColumnSpecWithCustomFields(Type fdoClassType, FdoCache cache,IDbColSpec columnSpec)
		{
			//ToDo: This method kills InMemoryFdoCache
			if(cache.GetType() != typeof(FdoCache))
				return;

			int iclassId =(int)GetStaticField(fdoClassType, "kClassId");
			uint classId= (uint)iclassId;

			foreach(ClassAndPropInfo info in cache.GetFieldsOfClass(classId))
			{
				if(!info.isCustom)
					continue;
				if(!info.isBasic)
					continue;
				switch(info.fieldType)
				{
					default:
						continue;
					case (int)FldType.kftString:
						columnSpec.Push((int)DbColType.koctString, 1, (int)info.flid, 0);
						columnSpec.Push((int)DbColType.koctFmt, 1, (int)info.flid, 0);
						break;
				}
			}
		}
		#endregion // Caching

		#region Misc methods

		/// <summary>
		/// Implement any side effects that should be done when an object is moved.
		/// Note: not all subclasses necessarily implement all that should be done;
		/// for example, if this is used to move senses from one entry to another, there
		/// ought to be something to copy the MSA, but that isn't done yet.
		/// </summary>
		/// <param name="hvoOldOwner"></param>
		public virtual void MoveSideEffects(int hvoOldOwner)
		{
		}

		/// <summary>
		///
		/// </summary>
		public DateTime UpdTime
		{
			get
			{
				string sQry = "select UpdDttm from CmObject where id=" + Hvo;
				IDbColSpec dcs = DbColSpecClass.Create();
				try
				{
					// The update time field gets changed in the database through triggers,
					// so the cache is probably not up-to-date. Therefore we force loading it.
					dcs.Push((int)DbColType.koctTime, 0, 0, 0);	// UpdDttm$
					m_cache.LoadData(sQry, dcs, Hvo);
				}
				finally
				{
					Marshal.ReleaseComObject(dcs);
				}

				return m_cache.GetTimeProperty(Hvo, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a String that represents the current Object.
		/// </summary>
		/// <returns>A String that represents the current Object.</returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return String.Format("CmObject id={0} class={1} owner={2}",
				m_hvo, ClassID, OwnerHVO);
		}

		/// <summary>
		/// Gets the object which, for the indicated property of the recipient, the user is
		/// most likely to want to edit if the ReferenceTargetCandidates do not include the
		/// target he wants.
		/// The canonical example, supported by the default implementation of
		/// ReferenceTargetCandidates, is a possibility list, where the targets are the items.
		/// Subclasses which have reference properties edited by the simple list chooser
		/// should generally override either this or ReferenceTargetCandidates or both.
		/// The implementations of the two should naturally be consistent.
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public virtual ICmObject ReferenceTargetOwner(int flid)
		{
			// Is this the best default? It clearly indicates that no target is known.
			// However, the default implementation of ReferenceTargetCandidates returns the
			// current contents of the list. It would be consistent with that for this method
			// to return 'this'. But that would seldom be useful (the user is presumably
			// already editing 'this'), and would require overrides wherever there is
			// definitely NO sensible object to jump to and edit. On the whole I (JohnT) think
			// it is best to make null the default.
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a set of hvos that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of IDs,
		/// and string representations of the objects. Alternatively, or as well, they
		/// should override ReferenceTargetOwner (the latter alone may be overridden if the
		/// candidates are the items in a possibility list, independent of the recipient object).
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>A set of hvos</returns>
		/// ------------------------------------------------------------------------------------
		public virtual Set<int> ReferenceTargetCandidates(int flid)
		{
			ICmPossibilityList owner = ReferenceTargetOwner(flid) as ICmPossibilityList;
			if (owner != null)
				return new Set<int>(owner.PossibilitiesOS.HvoArray);
			IFwMetaDataCache mdc = m_cache.MetaDataCacheAccessor;
			int iType = mdc.GetFieldType((uint)flid);
			if (iType != (int)FieldType.kcptReferenceSequence && iType != (int)FieldType.kcptReferenceCollection)
				return new Set<int>(0);
			int[] targetHvos = m_cache.GetVectorProperty(this.Hvo, flid, false);
			if (targetHvos.Length > 0)
				return new Set<int>(targetHvos);
			return new Set<int>(0); // todo
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <remarks>e.e. MoAdhocProhib makes no sense without "morphemes".
		/// e.g. MoEndoCompound makes no sense without a left and a right compound.
		/// </remarks>
		/// <returns>true, if the field is required.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsFieldRequired(int flid)
		{
			return false;//it is up to subclasses to override and return true where needed.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// tells whether the given field is relevant given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <remarks>e.g. "color" would not be relevant on a part of speech, ever.
		/// e.g.  MoAffixForm.inflection classes are only relevant if the MSAs of the
		/// entry include an inflectional affix MSA.
		/// </remarks>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsFieldRelevant(int flid)
		{
			return true;//it is up to subclasses to override and return false where needed.
		}

		/// <summary>
		/// Determine whether the object is valid, whether dummy or real.
		/// </summary>
		/// <returns></returns>
		public virtual bool IsValidObject()
		{
			if (m_cache == null)
				return false;
			return m_cache.IsValidObject(m_hvo, ClassID);
		}

		/// <summary>
		/// Uses CmObject base class to determine validity, since
		/// subclasses can override IsValidObject() to use different validation logic.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		static public bool IsValidObject(FdoCache cache, int hvo)
		{
			if (hvo == 0)
				return false;
			int clsid = cache.GetClassOfObject(hvo);
			if (clsid == 0)
				return false;
			ICmObject co = CmObject.CreateFromDBObject(cache, hvo, GetTypeFromFWClassID(cache, clsid), false, false);
			return co.IsValidObject();
		}

		/// <summary>
		/// Test whether the current object is a DummyObject.
		/// Does not test whether we're in a valid state.
		/// </summary>
		/// <returns></returns>
		public virtual bool IsDummyObject
		{
			get
			{
				if (m_cache == null)
					return false;
				return m_cache.IsDummyObject(m_hvo, false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object is real. Note that this was recently changed to EXCLUDE
		/// objects that are dummies; use IsValidObject for that.
		/// </summary>
		/// <remarks> some statistics on the cost of this function:
		/// Loading the 412 WfiWordforms in WFI.Wordforms vector one-by-one on a 1.6 GHz Athlon, in debug mode:
		///		4.4 seconds without validation, 4.9 sec. with validation.
		///		loading that vector all at once (using foreach which uses an ObjectSet)
		///		.3 seconds without validation, .42 seconds with validation
		///</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsRealObject
		{
			get
			{
				if ((m_cache == null) || (m_hvo < 1))
					return false;

				return m_cache.IsRealObject(m_hvo, ClassID);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the object satisfies constraints imposed by the class
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="failure">an explanation of what constraint failed, if any. Will be null if the method returns true.</param>
		/// <returns>true if the object is all right</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool CheckConstraints(int flidToCheck, out ConstraintFailure failure)
		{
			failure = null;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified Object is equal to the current Object.
		/// </summary>
		/// <param name="obj">The Object to compare with the current Object.</param>
		/// <returns>true if the specified Object is equal to the current Object; otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(Object obj)
		{
			// This is the behavior defined by Object.Equals().
			if (obj == null)
				return false;

			// Make sure that we can  cast this object to a CmObject.
			if (!(obj is ICmObject))
				return false;

			CmObject o = (CmObject)obj;
			return (m_hvo == o.m_hvo && m_cache == o.m_cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the hash code
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return Hvo; //  REVIEW:  is this a good hash code?
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges object into this object.
		/// For atomic properties, if this object has something in the property, the source
		/// property is ignored. For sequence properties, the objects in the source will be
		/// moved and appended to the properties in this object. Any references to the
		/// source object will be transferred to this object. The source object is not deleted
		/// here in case the caller wants to do any cleanup after the merge, such as copying
		/// back references for owned atomic objects in the source that weren't moved.
		/// </summary>
		/// <param name="objSrc">Object whose properties will be merged into this object's properties</param>
		/// <remarks>
		/// NB: The given object will be deleted in this method, so don't expect it to be valid, afterwards.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void MergeObject(ICmObject objSrc)
		{
			MergeObject(objSrc, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges object into this object.
		/// if fLoseNoStringData is false:
		/// For atomic properties, if this object has something in the property, the source
		/// property is ignored. For sequence properties, the objects in the source will be
		/// moved and appended to the properties in this object. Any references to the
		/// source object will be transferred to this object. The source object is deleted
		/// at the end of this method (objSrc.DeleteUnderlyingObject() call).
		/// String properties are copied from the source if the destination (this) has no value
		/// and the source has a value.
		///
		/// if fLoseNoStringData is true, the above is modified as follows:
		/// 1. If a string property has a value in both source and destination, and the values
		/// are different, append the source onto the destination.
		/// 2. If an atomic object property has a value in both source and destination,
		/// recursively merge the value in the source with the value in the destination.
		/// </summary>
		/// <param name="objSrc">Object whose properties will be merged into this object's properties</param>
		/// <remarks>
		/// NB: The given object will be deleted in this method, so don't expect it to be valid, afterwards.
		/// </remarks>
		/// <param name="fLoseNoStringData"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void MergeObject(ICmObject objSrc, bool fLoseNoStringData)
		{
			Debug.Assert(m_cache != null);
			// We don't allow merging items of different classes.
			Debug.Assert(ClassID == objSrc.ClassID);
			if (ClassID != objSrc.ClassID)
				return;

			IFwMetaDataCache mdc = m_cache.MetaDataCacheAccessor;
			PropertyInfo[] myProperties = GetType().GetProperties();
			PropertyInfo[] srcProperties = objSrc.GetType().GetProperties();
			string fieldname;
			// Process all the fields in the source.
			foreach(uint flid in DbOps.GetFieldsInClassOfType(mdc, ClassID, FieldType.kgrfcptAll))
			{
				/* These values will also be returned because they are for most of CmObject's flids.
				 * I think it will do this for each superclass, so there could be some repeats on them.
				 *
				 * pvuFields->Push(101);	// kflidCmObject_Guid
				 * pvuFields->Push(102);	// kflidCmObject_Class
				 * pvuFields->Push(103);	// kflidCmObject_Owner
				 * pvuFields->Push(104);	// kflidCmObject_OwnFlid
				 * pvuFields->Push(105);	// kflidCmObject_OwnOrd
				 * //pvuFields->Push(106);	// kflidCmObject_UpdStmp
				 * //pvuFields->Push(107);	// kflidCmObject_UpdDttm
				 *
				*/
				if (flid < 1000)
					continue; // Do nothing for the CmObject flids.
				if (flid >= (int)SpecialTagValues.ktagMinVp)
					continue;	// Do nothing for virtual properties.

				int nType = mdc.GetFieldType(flid);
				fieldname = mdc.GetFieldName(flid);
					//|| fieldname == "DateModified"
					//|| nType == (int)FieldType.kcptTime // This is handled by a separate connection, so it can time out, if another transaction is open.
				if (fieldname == "DateCreated"
					|| nType == (int)FieldType.kcptImage // FDO does not support this one.
					|| nType == (int)FieldType.kcptGenDate) // FDO does not support setter for gendate.
					continue; // Don't mess with this one.

				// Set suffixes on some of the types.
				switch (nType)
				{
					case (int)FieldType.kcptOwningAtom: // 23
					{
						fieldname += "OA";
						break;
					}
					case (int)FieldType.kcptReferenceAtom: // 24
					{
						fieldname += "RA";
						break;
					}
					case (int)FieldType.kcptOwningCollection: // 25
					{
						fieldname += "OC";
						break;
					}
					case (int)FieldType.kcptReferenceCollection: // 26
					{
						fieldname += "RC";
						break;
					}
					case (int)FieldType.kcptOwningSequence: // 27
					{
						fieldname += "OS";
						break;
					}
					case (int)FieldType.kcptReferenceSequence: // 28
					{
						fieldname += "RS";
						break;
					}
				}
				Object myCurrentValue = null;
				MethodInfo mySetMethod = null;
				Object srcCurrentValue = null;

				PropertyInfo pi = this.GetType().GetProperty(fieldname);
				if (pi != null)
				{
					myCurrentValue = pi.GetGetMethod().Invoke(this, null);
					mySetMethod = pi.GetSetMethod();
					srcCurrentValue = objSrc.GetType().GetProperty(fieldname).GetGetMethod().Invoke(objSrc, null);
				}
				else
				{
					// We must have a custom field, and it needs special treatment.
					Debug.Assert(m_cache.GetIsCustomField(flid));
					mySetMethod = null;
					string classname = mdc.GetOwnClsName(flid);
					string sView = classname + "_" + fieldname;
					switch (nType)
					{
					case (int)FieldType.kcptString:
					case (int)FieldType.kcptBigString:
						myCurrentValue = new TsStringAccessor(m_cache, m_hvo, (int)flid);
						srcCurrentValue = new TsStringAccessor(objSrc.Cache, objSrc.Hvo, (int)flid);
						break;
					case (int)FieldType.kcptMultiString:
					case (int)FieldType.kcptMultiBigString:
						myCurrentValue = new MultiStringAccessor(m_cache, m_hvo,
							(int)flid, sView);
						srcCurrentValue = new MultiStringAccessor(objSrc.Cache, objSrc.Hvo,
							(int)flid, sView);
						break;
					case (int)FieldType.kcptMultiUnicode:
					case (int)FieldType.kcptMultiBigUnicode:
						myCurrentValue = new MultiUnicodeAccessor(m_cache, m_hvo,
							(int)flid, sView);
						srcCurrentValue = new MultiUnicodeAccessor(objSrc.Cache, objSrc.Hvo,
							(int)flid, sView);
						break;
					}
				}
				if (srcCurrentValue == null)
					continue; // Nothing to merge.
				Debug.Assert(srcCurrentValue != null);

				/*
				 * NOTE: Each of the cases (except the exception, which can't be tested)
				 * is tested in the MergeObjectsTests class in the unit tests.
				 * If any additions are made, or if some currently unused cases are enabled,
				 * be sure to add them (or enable them) to that class, as well.
				 */
				switch (nType)
				{
					default:
						throw new ApplicationException("Unrecognized data type for merging: " + nType.ToString());

						/* 0 -> 9 */
					case (int)FieldType.kcptBoolean: // 1
					{
						// Can't be null, so we have to live with default of 0 (false).
						// 0 gets replaced with source data, if 1 (true).
						bool myBool = (bool)myCurrentValue;
						bool srcBool = (bool)srcCurrentValue;
						if (!myBool && srcBool)
						{
							Debug.Assert(mySetMethod != null);
							mySetMethod.Invoke(this, new object[] {srcCurrentValue});
						}
						break;
					}
						//
					case (int)FieldType.kcptInteger: // 2 Fall through
					// Setter not implemented in FDO. case (int)FieldType.kcptGenDate: // 8
					{
						// Can't be null, so we have to live with default of 0.
						// Zero gets replaced with source data, if greater than 0.
						int myInt = (int)myCurrentValue;
						int srcInt = (int)srcCurrentValue;
						if (myInt == 0 && srcInt > 0)
						{
							Debug.Assert(mySetMethod != null);
							mySetMethod.Invoke(this, new object[] {srcCurrentValue});
						}
						break;
					}
					case (int)FieldType.kcptTime: // 5
					{
						// If it is DateCreated, we won't even be here,
						// since we will have already skipped it.
						bool resetTime = false;
						DateTime srcTime = DateTime.Now;
						// If it is DateModified, always set it to 'now'.
						if (fieldname == "DateModified")
						{
							// Already using 'Now'.
							resetTime = true;
						}
						else
						{
						// Otherwise, a later source will replace an older target.
						DateTime myTime = (DateTime)myCurrentValue;
							srcTime = (DateTime)srcCurrentValue;
							resetTime = (myTime < srcTime);
						if (myTime < srcTime)
						{
							Debug.Assert(mySetMethod != null);
							mySetMethod.Invoke(this, new object[] {srcTime});
						}
						}
						if (resetTime)
						{
							Debug.Assert(mySetMethod != null);
							mySetMethod.Invoke(this, new object[] {srcTime});
						}
						break;
					}
					case (int)FieldType.kcptGuid: // 6
					{
						// May be null.
						Guid myGuidValue = (Guid)myCurrentValue;
						Guid srcGuidValue = (Guid)srcCurrentValue;
						if (myGuidValue == Guid.Empty && srcGuidValue != Guid.Empty)
						{
							Debug.Assert(mySetMethod != null);
							mySetMethod.Invoke(this, new object[] {srcGuidValue});
							mySetMethod.Invoke(objSrc, new object[] {Guid.Empty});
						}
						break;
					}
					//case (int)FieldType.kcptImage: // 7 Fall through.
					case (int)FieldType.kcptBinary: // 8
					{
						if (myCurrentValue == null)
						{
							Debug.Assert(mySetMethod != null);
							mySetMethod.Invoke(this, new object[] {srcCurrentValue});
						}
						break;
					}

					/* 13 -> 20 */
					case (int)FieldType.kcptString: // 13 Fall through
					case (int)FieldType.kcptBigString: // 17
					{
						if (MergeStringProp((int)flid, nType, objSrc, fLoseNoStringData, myCurrentValue, srcCurrentValue))
							break;
						TsStringAccessor myTsa = myCurrentValue as TsStringAccessor;
						myTsa.MergeString(srcCurrentValue as TsStringAccessor, fLoseNoStringData);
						break;
					}

					case (int)FieldType.kcptMultiString: // 14 Fall through.
					case (int)FieldType.kcptMultiBigString: // 18
					{
						if (MergeStringProp((int)flid, nType, objSrc, fLoseNoStringData, myCurrentValue, srcCurrentValue))
							break;
						MultiStringAccessor myMsa = myCurrentValue as MultiStringAccessor;
						myMsa.MergeAlternatives(srcCurrentValue as MultiStringAccessor, fLoseNoStringData);
						break;
					}

					case (int)FieldType.kcptUnicode: // 15 Fall through.
					case (int)FieldType.kcptBigUnicode: // 19
					{
						if (MergeStringProp((int)flid, nType, objSrc, fLoseNoStringData, myCurrentValue, srcCurrentValue))
							break;
						string myUCurrent = myCurrentValue as string;
						string srcUValue = srcCurrentValue as string;
						if ((myUCurrent == null || myUCurrent == String.Empty)
							&& srcUValue != String.Empty)
						{
							Debug.Assert(mySetMethod != null);
							mySetMethod.Invoke(this, new object[] {srcUValue});
						}
						else if (fLoseNoStringData
							&& myUCurrent != null && myUCurrent != String.Empty
							&& srcUValue != null && srcUValue != String.Empty
							&& srcUValue != myUCurrent)
						{
							Debug.Assert(mySetMethod != null);
							mySetMethod.Invoke(this, new object[] {myUCurrent + ' ' + srcUValue});
						}
						break;
					}

					case (int)FieldType.kcptMultiUnicode: // 16 Fall through
					case (int)FieldType.kcptMultiBigUnicode: // 20 This one isn't actually used yet, but I hope it is the same as the small MultiUnicode
					{
						if (MergeStringProp((int)flid, nType, objSrc, fLoseNoStringData, myCurrentValue, srcCurrentValue))
							break;
						MultiUnicodeAccessor myMua = myCurrentValue as MultiUnicodeAccessor;
						myMua.MergeAlternatives(srcCurrentValue as MultiUnicodeAccessor, fLoseNoStringData);
						break;
					}

					/* 23 -> 28 */
					case (int)FieldType.kcptOwningAtom:
					case (int)FieldType.kcptReferenceAtom: // 24
					{
						ICmObject srcObj = srcCurrentValue as ICmObject;
						ICmObject currentObj = myCurrentValue as ICmObject;
						if (myCurrentValue == null)
						{
							Debug.Assert(mySetMethod != null);
							mySetMethod.Invoke(this, new object[] {srcObj});
							break;
						}
						else if (fLoseNoStringData && nType == (int)FieldType.kcptOwningAtom && srcObj != null
							&& currentObj.GetType() == srcObj.GetType())
						{
							// merge the child objects.
							currentObj.MergeObject(srcObj, true);
						}
						break;
					}

					case (int)FieldType.kcptOwningCollection: // 25 Fall through, since the collection class knows how to merge itself properly.
					case (int)FieldType.kcptReferenceCollection: // 26
					{
						PropertyInfo piCol = FdoVector<ICmObject>.HvoArrayPropertyInfo(srcCurrentValue);
						MethodInfo myAddMethod = FdoCollection<ICmObject>.AddIntMethodInfo(myCurrentValue);
						foreach (int hvo in (int[])piCol.GetGetMethod().Invoke(srcCurrentValue, null))
						{
							myAddMethod.Invoke(myCurrentValue, new object[] { hvo });
						}
						break;
					}

					case (int)FieldType.kcptOwningSequence: // 27 Fall through, since the collection class knows how to merge itself properly.
					case (int)FieldType.kcptReferenceSequence: // 28
					{
						PropertyInfo piCol = FdoVector<ICmObject>.HvoArrayPropertyInfo(srcCurrentValue);
						MethodInfo myAppendMethod = FdoSequence<ICmObject>.AppendIntMethodInfo(myCurrentValue);
						foreach (int hvo in (int[])piCol.GetGetMethod().Invoke(srcCurrentValue, null))
						{
							myAppendMethod.Invoke(myCurrentValue, new object[] { hvo });
						}
						break;
					}
				}
			}

			// Now move all incoming references.
			CmObject.ReplaceReferences(m_cache, objSrc, this);
			objSrc.DeleteUnderlyingObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override this to provide special handling on a per-property basis for merging
		/// strings. For example, certain Sense properties insert semi-colon.
		/// If it answers false, the default merge is performed; if it answers true,
		/// an appropriate merge is presumed to have already been done.
		/// Note that on multistring properties it is called only once;
		/// override is responsible to merge all writing systems.
		/// </summary>
		/// <param name="flid">Field to merge.</param>
		/// <param name="cpt">Field type (cpt enumeration)</param>
		/// <param name="objSrc">Object being merged into this.</param>
		/// <param name="fLoseNoStringData">Currently always true, supposed to indicate that the merge should not lose anything.</param>
		/// <param name="myCurrentValue">Value for this object obtained from the relevant method for the property.
		/// Depending on the type, may be string, TsStringAccessor, MultiStringAccessor, or MultiUnicodeAccessor.</param>
		/// <param name="srcCurrentValue">Same thing for other.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool MergeStringProp(int flid, int cpt, ICmObject objSrc, bool fLoseNoStringData,
			object myCurrentValue, object srcCurrentValue)
		{
			return false;
		}

		/// <summary>
		/// Update the timestamp for this object, presumably because a virtual property
		/// has changed which implies something has changed for this object.  For instance,
		/// adding or removing a LexSense from a LexReference should update the timestamp of
		/// the LexSense.  (See LT-5523)
		/// </summary>
		public void UpdateTimestampForVirtualChange()
		{
			m_cache.CreateModifyManager.PropChanged(this.Hvo,
				(int)CmObjectFields.kflidCmObject_Guid, 0, 1, 1);
		}

		/// <summary>
		/// Notifies those interested that this object has been created, initialized, and added to its owner.
		/// When a new <c>CmObject</c> is created it does not automatically send a notification, so that the
		/// application has a chance to initialize it. This is called after the object has been added to its
		/// owner and initialized. As of 04/08/09, owning collections call <c>PropChanged</c> when a new item is
		/// added to it, so it is not necessary to call this when the object is added to an owning collection.
		/// </summary>
		public void NotifyNew()
		{
			if (Hvo == (int)SpecialHVOValues.kHvoOwnerPending)
				throw new System.ArgumentException("The object has not been created.");

			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, OwnerHVO, OwningFlid, IndexInOwner, 1, 0);
		}
		#endregion	// Misc methods

		#region Static utility functions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="l"></param>
		/// <param name="r"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool operator ==(CmObject l, CmObject r)
		{
			if ((Object)l == null && (Object)r == null)
				return true;
			if ((Object)l == null || (Object)r == null)
				return false;
			return l.Equals(r);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="l"></param>
		/// <param name="r"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool operator !=(CmObject l, CmObject r)
		{
			if ((Object)l == null && (Object)r == null)
				return false;
			if ((Object)l == null || (Object)r == null)
				return true;
			return !l.Equals(r);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Instantiate an object of the correct type, given the hvo.
		/// </summary>
		/// <remarks>
		/// Needed when the signature of the property is CmObject (and the actual class of the
		/// object is unknown).
		/// Notice that, even though the return signature of this method is CmObject, it
		/// will actually be returning an object of either CmObject or one of its subclasses.
		/// Note that in debug builds, this will include a validity check on the object.  If you want
		/// a validity check during a release build, use the version of this method which
		/// includes a parameter to control this explicitly.
		/// </remarks>
		/// <param name="fcCache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ICmObject CreateFromDBObject(FdoCache fcCache,int hvo)
		{
			Type cSharpType = GetTypeFromFWClassID(fcCache, fcCache.GetClassOfObject(hvo));

			// Now construct the object
			// If the object's class is already cached, assume all the FDO preload data is too.
			// Occasionally we will get a miss, where the class has been loaded some other way,
			// but we will auto-load the properties actually used in that case, and on the other
			// hand we save a huge amount of time not reloading objects we've already loaded.
			bool fLoadIntoCache =
				!fcCache.MainCacheAccessor.get_IsPropInCache(hvo, (int)CmObjectFields.kflidCmObject_Class,
				(int)CellarModuleDefns.kcptInteger, 0);

			return CreateFromDBObject(fcCache, hvo, cSharpType,
//#if DEBUG
//                true/*validity check*/,
//#else
				false,
//#endif
				fLoadIntoCache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This variant looks up the type, but never checks validity, and allows control of whether
		/// to load into cache.
		/// </summary>
		/// <param name="fcCache"></param>
		/// <param name="hvo"></param>
		/// <param name="bLoadIntoCache"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ICmObject CreateFromDBObject(FdoCache fcCache, int hvo, bool bLoadIntoCache)
		{
			return CreateFromDBObject(fcCache, hvo,
				GetTypeFromFWClassID(fcCache, fcCache.GetClassOfObject(hvo)),
				false, // don't validity check
				bLoadIntoCache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Instantiate an FDO object, given its hvo and CSharp type.
		/// </summary>
		/// <param name="fcCache"></param>
		/// <param name="hvo"></param>
		/// <param name="cSharpType">the CSharp type corresponding to the type of Cellar object.
		/// If unknown, use the version of this method which does not include this parameter.
		/// </param>
		/// <param name="bCheckValidity"></param>
		/// <param name="bLoadIntoCache">set to false if the object is already loaded into the
		/// cache</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ICmObject CreateFromDBObject(FdoCache fcCache, int hvo, Type cSharpType,
			bool bCheckValidity, bool bLoadIntoCache)
		{
			Debug.Assert(cSharpType!= null);

			//  construct the object
			//CmObject x = (CmObject)cSharpType.GetConstructor(new System.Type[] { typeof(FdoCache), typeof(int), typeof(bool), typeof(bool) }).Invoke(
			//    new object[] { fcCache, hvo, bCheckValidity, bLoadIntoCache });
			object [] objs = new object[] { fcCache, hvo, bCheckValidity, bLoadIntoCache };
			System.Type [] types = new System.Type[] { typeof(FdoCache), typeof(int), typeof(bool), typeof(bool)};
			ConstructorInfo cons = cSharpType.GetConstructor(types);
			CmObject x = (CmObject)cons.Invoke(objs);
			// Rather than creating with the default constructor and then initializing the object with an hvo and cache,
			// we just use the appropriate constructor so we don't have to do this. This helps avoid
			// accidental early initialization for certain classes, such as ScrImportSet.
			//x.InitExisting(fcCache, hvo,  bCheckValidity,  bLoadIntoCache);
			return x as ICmObject;
		}

		/// <summary>
		/// Copy contents of this object to another one
		/// </summary>
		/// <param name="objNew">target object</param>
		/// <remarks>override this to copy the content</remarks>
		public virtual void CopyTo(ICmObject objNew)
		{
			if (objNew == null)
			{
				throw new ApplicationException("Attempted to copy an object to a non-existant object.");
			}
			if (ClassID != objNew.ClassID)
			{
				throw new ApplicationException("Attempted to copy an object to a different class of object.");
			}
		}

		/// <summary>
		/// Takes the information from a dummy object and allows its owner to create a real object in the database.
		/// NOTE: after calling this, users need to make sure they no longer try to use the old hvoDummy object.
		/// </summary>
		/// <param name="fcCache"></param>
		/// <param name="hvoDummy">id corresponding to the object to convert. Minimally it should have a class id cached
		/// and an OwningFlid corresponding to a virtual handler that implements IDummyRequestConversion. </param>
		/// <returns>real object based on new database entry created for the dummy object,
		/// null if conversion did not take place.</returns>
		public static ICmObject ConvertDummyToReal(FdoCache fcCache, int hvoDummy)
		{
			// suppress changes in display.
			using (new IgnorePropChanged(fcCache, PropChangedHandling.SuppressView))
			{
				// This conversion should not be an undoable task, so suppress the action handler.
				// (cf. LT-5330, LT-5417).
				using (SuppressSubTasks supressActionHandler = new SuppressSubTasks(fcCache, true))
				{
					ICmObject realObj = null;
					Debug.Assert(fcCache.IsDummyObject(hvoDummy));
					if (fcCache.IsDummyObject(hvoDummy))
					{
						// see if we can convert this to a real object before loading it.
						int owningFlid = fcCache.GetOwningFlidOfObject(hvoDummy);
						IVwVirtualHandler vh = fcCache.VwCacheDaAccessor.GetVirtualHandlerId(owningFlid);
						Debug.Assert(vh != null && vh is IDummyRequestConversion);
						if (vh != null && vh is IDummyRequestConversion)
						{
							RequestConversionToRealEventArgs args = new RequestConversionToRealEventArgs(hvoDummy,
								0, null, true);
							args.OwningFlid = owningFlid;
							(vh as IDummyRequestConversion).OnRequestConversionToReal(hvoDummy, args);
							realObj = args.RealObject as ICmObject;
						}
					}
					return realObj;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// returns the CSharp type corresponding to be fully qualified name given
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="sFullClassName">e.g. "SIL.FieldWorks.FDO.Ling.WordformLookupList"</param>
		/// <returns>the CSharp type</returns>
		/// ------------------------------------------------------------------------------------
		public static Type GetTypeFromFullClassName(FdoCache cache, string sFullClassName)
		{
			Type t = cache.GetTypeInAssembly(sFullClassName);
			Debug.Assert(t != null);
			return t;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// gives the CSharp type corresponding to the given the fieldworks class ID
		/// </summary>
		/// <param name="fcCache"></param>
		/// <param name="iClassId"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static Type GetTypeFromFWClassID(FdoCache fcCache, int iClassId)
		{
			//CmObject is a special case because it is not part of a module.
			if (iClassId == CmObject.kClassId)
				return typeof(CmObject);
			// if the class ID is cached then return the type now.
			if (s_classIdToType.ContainsKey(iClassId))
				return s_classIdToType[iClassId];

			Type t = null;
			// Find the class name of this object
			string sClassName = fcCache.GetClassName((uint)iClassId);
			// find the Type for this class, which is painful 'cause we don't know the namespace
			string[] modules = new string[]{"Cellar", "Ling", "Scripture", "FeatSys", "LangProj", "Notebk"};
			foreach(string moduleName in modules)
			{
				string fullTypeName = string.Format("SIL.FieldWorks.FDO.{0}.{1}", moduleName, sClassName);
				t = fcCache.GetTypeInAssembly(fullTypeName);
				//t = Assembly.GetExecutingAssembly().GetType(fullTypeName, false, false);
				if (t != null)
					break;
			}
			Debug.Assert(t != null);

			// cache the type info and return it.
			s_classIdToType.Add(iClassId, t);
			return t;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether objects of this class never have an owner.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool IsOwnerless
		{
			get { return ClassIsOwnerless(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if objects of this class never have an owner. Derived classes
		/// have to implement this method.
		/// </summary>
		/// <returns><c>false</c> in most cases, <c>true</c> if class is ownerless.</returns>
		/// <remarks>This method is internal because clients should use FdoCache.IsOwnerless().
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual bool ClassIsOwnerless()
		{
			throw new NotImplementedException(
				"Derived classes have to implement ClassIsOwnerless()!");
		}

		/// <summary>
		/// Creates and returns a string containing some or all of the members of m_hvos. Function
		/// stops adding members when the overall string reaches the length defined in internal
		/// constant kcchMaxIdList.
		/// </summary>
		/// <param name="iNextGroup">Index of first member of m_hvos to add.
		/// </param>
		/// <param name="hvos">The objects to select a group of.</param>
		/// <returns>String made of comma-separated integers
		/// </returns>
		internal static string MakePartialIdList(ref int iNextGroup, int[] hvos)
		{
			return DbOps.MakePartialIdList(ref iNextGroup, hvos);
		}

		/// <summary>
		/// Joins a set of ids into a string separated by the given separator.
		/// </summary>
		/// <param name="ids"></param>
		/// <param name="sep"> separator to use for the join.</param>
		/// <returns></returns>
		public static string JoinIds(int[] ids, string sep)
		{
			string[] strings = new string[ids.Length];
			// convert ids to string[]
			for (int i = 0; i < ids.Length; ++i)
				strings[i] = ids[i].ToString();
			return String.Join(sep, strings);
		}

		/// <summary>
		/// Update all existing references to point to objNew instead of objOld (which may be
		/// getting deleted as soon as we return from this method).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="objNew"></param>
		/// <param name="objOld"></param>
		internal static void ReplaceReferences(FdoCache cache, ICmObject objOld, ICmObject objNew)
		{
			if (objOld == null)
				return;
			Debug.Assert(cache != null);
			// NOTE: the call to BackReferences appears to return backreferences to owned
			// objects as well as backreferences to the object itself.  This is a bug in the
			// GetLinkObjs$ stored procedure, which we can't figure out at the moment.  The
			// code below should work properly even when the stored procedure is fixed.
			foreach (LinkedObjectInfo loi in objOld.BackReferences)
			{
				if (loi.ObjId != objOld.Hvo)
					continue;
				ICmObject referrer = CmObject.CreateFromDBObject(cache, loi.RelObjId);
				string fieldname = cache.MetaDataCacheAccessor.GetFieldName((uint)loi.RelObjField);
				object vecObj;
				MethodInfo propMethod;
				MethodInfo removeMethod;
				MethodInfo addMethod;
				switch (loi.RelType)
				{
					case (int)FieldType.kcptReferenceAtom: // 24
						propMethod = referrer.GetType().GetProperty(fieldname + "RA").GetSetMethod();
						propMethod.Invoke(referrer, new object[] { objNew });
						break;
					case (int)FieldType.kcptReferenceCollection: // 26
						propMethod = referrer.GetType().GetProperty(fieldname + "RC").GetGetMethod();
						vecObj = propMethod.Invoke(referrer, null);
						removeMethod = FdoSequence<ICmObject>.RemoveIntMethodInfo(vecObj);
						removeMethod.Invoke(vecObj, new object[] { loi.ObjId });
						if (objNew != null)
						{
							addMethod = FdoCollection<ICmObject>.AddIntMethodInfo(vecObj);
							addMethod.Invoke(vecObj, new object[] { objNew.Hvo });
						}
						break;
					case (int)FieldType.kcptReferenceSequence: // 28
						propMethod = referrer.GetType().GetProperty(fieldname + "RS").GetGetMethod();
						vecObj = propMethod.Invoke(referrer, null);
						removeMethod = FdoSequence<ICmObject>.RemoveIntMethodInfo(vecObj);
						removeMethod.Invoke(vecObj, new object[] { loi.ObjId });
						if (objNew != null)
						{
							addMethod = FdoSequence<ICmObject>.AppendIntMethodInfo(vecObj);
							addMethod.Invoke(vecObj, new object[] { objNew.Hvo });
						}
						break;
				}
			}
		}
		#endregion
	}
}
