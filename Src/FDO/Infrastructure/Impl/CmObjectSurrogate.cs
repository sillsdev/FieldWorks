#define SecondByStringManipulation

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using SIL.Utils;
using System.Threading;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// This class holds a CmObject and its Guid.
	/// The class is used to support lazy loading (on demand) for CmObjects,
	/// where we initially store only the Guid of the object for
	/// lazy loading scenarios. At runtime, the actual object
	/// is then fetched from the backend data store,
	/// and is then stored in this class for subsequent calls.
	///
	/// This class allows for bulk loading of objects, as well.
	/// In the case where bulk loading is best, then both the Guid and the object are stored
	/// at the same time.
	/// </summary>
	internal sealed class CmObjectSurrogate : ICmObjectSurrogate //, IEquatable<CmObjectSurrogate>
	{
		private static Dictionary<string, ConstructorInfo> s_classToConstructorInfo;
		/// <summary>
		/// It's common that hundreds of thousands of surrogates only use a few hundred class names. This is a local interning
		/// of those names.
		/// </summary>
		private static readonly Dictionary<string, string> s_canonicalClassNames = new Dictionary<string, string>();

		private FdoCache m_cache;
		private ICmObjectId m_guid;
		// No. Takes 3 seconds on my ZPI data set to add the empty Guid.
		// They all get set to real Guid values in each Constructor.
		// private Guid m_guid = Guid.Empty;

		// This stores the real object for which the surrogate stands. Lock SyncRoot before working on fluffing one
		// up or setting m_object to an existing fluffed object.
		// Once it is set to the fluffed object, it should never be set back to null. This constraint is
		// required to ensure that we can safely test for null and return a non-null result WITHOUT locking.
		// Also, it is required that if a fluffed object exists for a surrogate, m_object must point at it;
		// the only exception is one temporarily created (and never put in the repository or shared across
		// threads) by CreateSnapshot, purely for testing.
		// Damien recommended making this volatile to reduce the dangers inherent in accessing it without
		// locking every time.
		// Enhance JohnT: when we go to .NET 4.0, we can probably use a lower-cost lock to protect all access
		// to m_object so we don't have to be quite so careful about it.
		private volatile ICmObject m_object;
		private string m_classname;
		private byte[] m_xml;
		private bool m_objectWasAttached;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <remarks>
		/// This Constructor is used for lazy load cases.
		/// The stored XML string (from the data store) is used to instantiate m_object.
		/// </remarks>
		internal CmObjectSurrogate(FdoCache cache, string xmlData)
		{
			if (cache == null) throw new ArgumentNullException("cache");
			if (xmlData == null) throw new ArgumentNullException("xmlData");

			m_cache = cache;
			m_object = null;
			Xml = xmlData;
			SetBasics();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <remarks>
		/// This Constructor is used for lazy load cases.
		/// The stored XML string (from the data store) is used to instantiate m_object.
		/// </remarks>
		internal CmObjectSurrogate(FdoCache cache, byte[] xmlData)
		{
			if (cache == null) throw new ArgumentNullException("cache");
			if (xmlData == null) throw new ArgumentNullException("xmlData");

			m_cache = cache;
			m_object = null;
			RawXmlBytes = xmlData;
			SetBasics();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <remarks>
		/// This Constructor is used for lazy load cases.
		/// The stored XML string (from the data store) is used to instantiate m_object.
		/// </remarks>
		internal CmObjectSurrogate(FdoCache cache, Guid guid, string classname, string xmlData)
			: this(cache, ((IServiceLocatorInternal)cache.ServiceLocator).IdentityMap.CreateObjectIdWithHvo(guid), classname, xmlData)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <remarks>
		/// This Constructor is used for lazy load cases.
		/// The stored XML string (from the data store) is used to instantiate m_object.
		/// </remarks>
		internal CmObjectSurrogate(FdoCache cache, Guid guid, string classname, byte[] xmlData)
			: this(cache, ((IServiceLocatorInternal)cache.ServiceLocator).IdentityMap.CreateObjectIdWithHvo(guid), classname, xmlData)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <remarks>
		/// This Constructor is used for lazy load cases.
		/// The stored XML string (from the data store) is used to instantiate m_object.
		/// </remarks>
		internal CmObjectSurrogate(FdoCache cache, ICmObjectId objId, string classname, string xmlData)
		{
			if (cache == null) throw new ArgumentNullException("cache");
			if (objId == null) throw new ArgumentNullException("objId");
			if (string.IsNullOrEmpty(classname)) throw new ArgumentNullException("classname");
			if (string.IsNullOrEmpty(xmlData)) throw new ArgumentNullException("xmlData");

			m_cache = cache;
			m_object = null;
			Xml = xmlData;
			m_guid = objId is CmObjectIdWithHvo ? objId
				: ((IServiceLocatorInternal)cache.ServiceLocator).IdentityMap.CreateObjectIdWithHvo(objId.Guid);
			SetClassName(classname);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <remarks>
		/// This Constructor is used for lazy load cases.
		/// The stored XML string (from the data store) is used to instantiate m_object.
		/// </remarks>
		internal CmObjectSurrogate(FdoCache cache, ICmObjectId objId, string classname, byte[] xmlData)
		{
			if (cache == null) throw new ArgumentNullException("cache");
			if (objId == null) throw new ArgumentNullException("objId");
			if (xmlData == null) throw new ArgumentNullException("xmlData");
			if (string.IsNullOrEmpty(classname)) throw new ArgumentNullException("classname");

			m_cache = cache;
			m_object = null;
			RawXmlBytes = xmlData;
			m_guid = objId is CmObjectIdWithHvo ? objId
				: ((IServiceLocatorInternal)cache.ServiceLocator).IdentityMap.CreateObjectIdWithHvo(objId.Guid);
			SetClassName(classname);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <remarks>
		/// This Constructor is used for porting from one BEP to another.
		/// It's faster than getting all the stuff from the xml string.
		/// </remarks>
		internal CmObjectSurrogate(FdoCache cache, ICmObjectSurrogate sourceSurrogate)
		{
			if (cache == null) throw new ArgumentNullException("cache");
			if (sourceSurrogate == null) throw new ArgumentNullException("sourceSurrogate");

			var surr = (CmObjectSurrogate) sourceSurrogate;
			m_cache = cache;
			m_object = null;
			if (surr.RawXmlBytes != null)
				RawXmlBytes = surr.RawXmlBytes;
			else
				Xml = sourceSurrogate.XML;
			ICmObjectId objId = surr.Id;
			m_guid = objId is CmObjectIdWithHvo ? objId
				: ((IServiceLocatorInternal)cache.ServiceLocator).IdentityMap.CreateObjectIdWithHvo(objId.Guid);
			SetClassName(surr.m_classname);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <remarks>
		/// This Constructor is used for newly created CmObject cases.
		/// </remarks>
		internal CmObjectSurrogate(ICmObject obj)
		{
			if (obj == null) throw new ArgumentNullException("obj");

			m_guid = obj.Id;
			m_object = obj;
			m_cache = obj.Cache;
			if (m_cache == null)
				throw new InvalidOperationException("'obj' has no FdoCache.");
			SetClassName(obj.ClassName);
		}

		/// <summary>
		/// This is used to create a snapshot that is equivalent to the current state of a particular CmObject,
		/// but not linked to it. Currently this is just used in testing, to simulate state obtained from
		/// another client.
		/// </summary>
		internal static CmObjectSurrogate CreateSnapshot(ICmObject obj)
		{
			var result = new CmObjectSurrogate(obj);
			result.m_object = null;
			result.Xml = ((ICmObjectInternal)obj).ToXmlString();
			return result;
		}



		internal static void InitializeConstructors(List<Type> cmObjectTypes)
		{
			if (s_classToConstructorInfo != null) return;

			s_classToConstructorInfo = new Dictionary<string, ConstructorInfo>();
			// Get default constructor.
			// Only do this once, since they are stored in a static data member.
			foreach (var fdoType in cmObjectTypes)
			{
				if (fdoType.IsAbstract) continue;

				s_classToConstructorInfo.Add(fdoType.Name, fdoType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
			}
		}

		/// <summary>
		/// Initialize from the data store (which uses byte arrays).
		/// </summary>
		internal void InitializeFromDataStore(FdoCache cache, ICmObjectId objId, string className, byte[] xmlData)
		{
			if (cache == null)
				throw new ArgumentNullException("cache");
			if (objId == null)
				throw new ArgumentNullException("objId");
			if (xmlData == null)
				throw new ArgumentNullException("xmlData");
			if (string.IsNullOrEmpty(className))
				throw new ArgumentNullException("className");

			lock (SyncRoot)
			{
				m_cache = cache;
				// Don't do this! If it's a new surrogate, the object is already null, and if not,
				// and we've already fluffed it, it's an invalid state for the object of the surrogate
				// to be null when it is fluffed.
				// m_object = null;
				// in fact, if we're re-creating a surrogate that got fluffed and garbage collected,
				// e.g., in Refreshing a surrogate to align it with another client,
				// we might be making a new object, yet the CmObject might already exist!
				m_object = ((ICmObjectRepositoryInternal) m_cache.ServiceLocator.ObjectRepository).GetObjectIfFluffed(objId);
				RawXmlBytes = xmlData;
				m_guid = objId is CmObjectIdWithHvo ? objId
					: ((IServiceLocatorInternal)cache.ServiceLocator).IdentityMap.CreateObjectIdWithHvo(objId.Guid);
				SetClassName(className);
			}
		}

		private string Xml
		{
			get
			{
				byte[] xmlBytes = m_xml; // Use local variable to prevent race condition
				return xmlBytes == null ? null : Encoding.UTF8.GetString(xmlBytes);
			}
			set
			{
				if (value == null)
				{
					m_xml = null;
					return;
				}
				m_xml = Encoding.UTF8.GetBytes(value);
			}
		}

		/// <summary>
		/// Get the main XML string converted to a byte array encoded in UTF8. Typically this is how it is
		/// actually stored, so it is more efficient to work with this than the XML string unless you
		/// really need a string. Note that this (unlike ICmObjectOrSurrogate.XMLBytes) may answer null;
		/// it will NOT generate the XML from the object.
		/// </summary>
		public byte[] RawXmlBytes
		{
			get { return m_xml;}
			set { m_xml = value; }
		}

		/// <summary>
		/// Gets the synchronization root. This is the object that should be
		/// used for all locking in this surrogate. Used for locking m_object when fluffing up the real object,
		/// so only one thread fluffs it up.
		/// </summary>
		/// <value>The synchronization root.</value>
		private object SyncRoot
		{
			get
			{
				// the best practice is to lock on a non-publicly accessible object,
				// so that locking can be better controlled and encapsulated. We are
				// not following this practice, because of the extra memory that is
				// used to create a separate lock object.
				return this;
		}
		}

		/// <summary>
		/// Connect an object with a surrogate, during bootstrap of extant system.
		/// </summary>
		/// <param name="obj"></param>
		void ICmObjectSurrogate.AttachObject(ICmObject obj)
		{
			if (m_object != null)
				throw new ArgumentException("Already have the 'm_object'.");
			// We have to use Equals here because the guids are from different identity maps and so may not be identical,
			// and (AFAIK - JohnT) there is no way to override the == and != operators on interfaces.
			if (!obj.Id.Equals(m_guid))
				throw new ArgumentException("Guid of 'obj' does not match Guid of surrogate.");

			m_objectWasAttached = true;
			m_object = obj;
			if (m_classname == null)
				SetClassName(obj.ClassName);
		}


#if SecondByStringManipulation
		private static readonly byte[] GuidEquals = Encoding.UTF8.GetBytes("guid=\"");
		private static readonly byte[] ClassEquals = Encoding.UTF8.GetBytes("class=\"");
		private static readonly byte QuoteChar = Encoding.UTF8.GetBytes("\"")[0];

		public static Guid GuidFromByteSubArray(byte[] bytes, int startIdx)
		{
			return new Guid(Encoding.UTF8.GetString(bytes, startIdx, 36));
		}
#endif

		private void SetBasics()
		{
#if FirstByLINQXML // 20.112/20.289 (s)
			var element = XElement.Parse(m_xml);
			m_guid = new Guid(element.Attribute("guid").Value);
			m_classname = element.Attribute("class").Value;
#endif
#if FirstByStringManipulation // 3.795/3.902 (s)
			var startIdx = m_xml.IndexOf("guid=");
			var endIdx = m_xml.IndexOf("\"", startIdx + 7);
			m_guid = new Guid(m_xml.Substring(startIdx + 6, endIdx - 6 - startIdx));

			startIdx = m_xml.IndexOf("class=");
			endIdx = m_xml.IndexOf("\"", startIdx + 8);
			m_classname = m_xml.Substring(startIdx + 7, endIdx - 7 - startIdx);
#endif
#if SecondByStringManipulation // 3.407/3.518 (s) // Seems to be the fastest, to date.
			var startIdx = RawXmlBytes.IndexOfSubArray(GuidEquals) + GuidEquals.Length;
			m_guid = ((IServiceLocatorInternal)m_cache.ServiceLocator).IdentityMap.CreateObjectIdWithHvo(GuidFromByteSubArray(RawXmlBytes, startIdx));

			startIdx = RawXmlBytes.IndexOfSubArray(ClassEquals) + ClassEquals.Length;
			var endIdx = Array.IndexOf(RawXmlBytes, QuoteChar, startIdx + 1);
			SetClassName(Encoding.UTF8.GetString(RawXmlBytes, startIdx, endIdx - startIdx));
#endif
#if ThirdByStringManipulation // 4.371/4.479 (s)
			var haveGuid = false;
			var haveClass = false;
			var xmlLength = m_xml.Length;
			for (var i = 0; i < xmlLength; i++)
			{
				var currentChar = m_xml[i];
				switch (currentChar)
				{
					case 'g':
						m_guid = new Guid(m_xml.Substring(i + 6, 36));
						i += 42;
						haveGuid = true;
						break;
					case 'c':
						i += 7;
						for (var j = i; j < xmlLength; ++j)
						{
							if (m_xml[j] != '"') continue;
							m_classname = m_xml.Substring(i, j - i);
							haveClass = true;
							i = j;
							break;
						}
						break;
				}
				if (haveGuid && haveClass)
					break;
			}
#endif
#if FourthByStringManipulation // 4.318/4.434 (s)
			var xmlLength = m_xml.Length;
			var mtGuid = Guid.Empty;
			for (var i = 0; i < xmlLength; i++)
			{
				switch (m_xml[i])
				{
					case 'g':
						m_guid = new Guid(m_xml.Substring(i + 6, 36));
						if (m_classname != null) return;
						i += 42;
						break;
					case 'c':
						i += 7;
						for (var j = i; j < xmlLength; ++j)
						{
							if (m_xml[j] != '"') continue;
							m_classname = m_xml.Substring(i, j - i);
							if (m_guid != mtGuid) return;
							i = j;
							break;
						}
						break;
				}
			}
#endif
		}

		/// <summary>
		/// Get the main XML string for the internal CmObject.
		/// </summary>
		string ICmObjectOrSurrogate.XML
		{
			get
			{
				string sXml = Xml; // Use local variable to prevent race conditions
				if (sXml == null)
				{
					var asInternal = (ICmObjectInternal)(((ICmObjectSurrogate)this).Object);
					var result = asInternal.ToXmlString();
					Xml = result;
					return result; // avoids converting back again!
				}
				return sXml;
			}
		}

		/// <summary>
		/// Get the main byte array of the XML string for the internal CmObject.
		/// </summary>
		byte[] ICmObjectOrSurrogate.XMLBytes
		{
			get
			{
				// Note: for data migration, it is VERY important that we don't try to compute this.Object if m_xml is not null.
				return m_xml ?? Encoding.UTF8.GetBytes(((ICmObjectInternal)(((ICmObjectSurrogate)this).Object)).ToXmlString());
			}
		}

		/// <summary>
		/// Find out if the surrogate has the actual object.
		/// </summary>
		bool ICmObjectOrSurrogate.HasObject
		{
			get
			{
				return m_object != null;
			}
		}

		internal ICmObjectOrId ObjectOrIdWithHvo
		{
			get
			{
				// JohnT: I don't understand why this is necessary, but somehow in one of my
				// tests, at least for language project, the surrogate hangs around in the
				// identity map even after the real object is fluffed. If that has happened,
				// we want the existing HVO, not a new one.
				lock (SyncRoot)
				{
					if (m_object != null)
						return m_object;
				}
				return m_guid;
			}
		}

		/// <summary>
		/// Get the CmObject.
		/// </summary>
		[Browsable(false)]
		ICmObject ICmObjectOrSurrogate.Object
		{
			get
			{
				string sXml = null;
				try
				{
					lock (SyncRoot)
					{
						// Must check for null again AFTER we have the lock, to guard against another thread
						// fluffing it up in the meantime.
						if (m_object == null || m_objectWasAttached)
						{
							if (RawXmlBytes == null)
								throw new InvalidOperationException("Can't load an object with no XML data.");

							sXml = Xml; // Must be inside the lock to prevent race conditions (FWR-3624)
							var rtElement = XElement.Parse(sXml);
							RawXmlBytes = null;
							if (!m_objectWasAttached)
							{
								m_object = (ICmObject)s_classToConstructorInfo[m_classname].Invoke(null);
								try
								{
									((ICmObjectInternal) m_object).LoadFromDataStore(
										m_cache,
										rtElement,
										((IServiceLocatorInternal) m_cache.ServiceLocator).LoadingServices);
								}
								catch (InvalidOperationException ioe)
								{   // Asserting just so developers know that this is happening
									Debug.Assert(false, "See LT-13574: something is corrupt in this database.");
									// LT-13574 had a m_classname that was different from the that in rtElement.
									// That causes attributes to be leftover or missing - hence the exception.
									rtElement = XElement.Parse(sXml); // rtElement is consumed in loading, so re-init
									var className = rtElement.Attribute("class").Value;
									if (className != m_classname)
									{
										m_object = (ICmObject)s_classToConstructorInfo[className].Invoke(null);
										((ICmObjectInternal)m_object).LoadFromDataStore(
											m_cache,
											rtElement,
											((IServiceLocatorInternal)m_cache.ServiceLocator).LoadingServices);
									}
								}
							}
						// Have to set m_objectWasAttached to false, before the registration,
							// since RegisterActivatedSurrogate calls this' Object prop,
							// and it would result in a stack overflow, with it still being true,
							// as it would try again to create the object.
							m_objectWasAttached = false;
							((IServiceLocatorInternal)m_cache.ServiceLocator).IdentityMap.RegisterActivatedSurrogate(this);
						}
					}
					Debug.Assert(m_object != null, "Surrogate should not exist without being able to create an object");
				}
				catch (ThreadAbortException)
				{
					// Ignore. This can happen if the domain loading thread is excessively busy when the application is being shut down.
				}
				catch (Exception e)
				{
					// The point of this is partly to force the stack to unwind far enough to release the lock
					// before any attempt to report an unhandled exception. Otherwise we may get a deadlock
					// as the background thread (inside the lock) tries to Invoke showing the green screen,
					// while the main thread which needs to display the dialog is waiting for the lock
					// we are holding.
					// Also, throwing an unhandled exception in a background thread can abort the process
					// possibly without ever showing a message.
					string fullMsg = string.Format(Strings.ksBadData, m_cache.ProjectId.UiName, e.Message, sXml);
					string msg = fullMsg;
					// limit length of message so that message box is more likely to fit on the screen.
					var lines = msg.Split('\n');
					const int nLines = 40;
					if (lines.Length > nLines)
						msg = String.Join("\n", lines.Take(nLines).ToArray()) + "\n...";
					var userAction = m_cache.ServiceLocator.GetInstance<IFdoUI>();
					userAction.DisplayMessage(MessageType.Error, msg, Strings.ksErrorCaption, null);
					userAction.ReportException(new Exception(fullMsg, e), true);
				}
				return m_object;
			}
		}

		///// <summary>
		///// If the surrogate already has an object, return it, but don't create it.
		///// </summary>
		//internal ICmObject ExistingObject
		//{
		//    get
		//    {
		//    	return m_object;
		//    }
		//}

		/// <summary>
		/// Get the Object's Guid. If the result will hang around, prefer to get the Id
		/// </summary>
		Guid ICmObjectSurrogate.Guid
		{
			get
			{
				return m_guid.Guid;
			}
		}

		/// <summary>
		/// Get the ID of the object this surrogate is for.
		/// </summary>
		public ICmObjectId Id
		{
			get
			{
				return m_guid;
			}
		}

		/// <summary>
		/// Get the Object's classname.
		/// </summary>
		string ICmObjectOrSurrogate.Classname
		{
			get { return m_classname; }
		}

		private void SetClassName(string name)
		{
			if (s_canonicalClassNames.TryGetValue(name, out m_classname)) return;

			m_classname = name;
			s_canonicalClassNames[name] = name;
		}

		/// <summary>
		/// Reset the class and xml, after a data migration (and before reconstitution).
		/// </summary>
		/// <param name="className">Class name. (May be the same).</param>
		/// <param name="xml">New XML as UTF-16.</param>
		public void Reset(string className, string xml)
		{
			if (string.IsNullOrEmpty(className)) throw new ArgumentNullException("className");
			if (string.IsNullOrEmpty(xml)) throw new ArgumentNullException("xml");
			if (m_object != null)
				throw new InvalidOperationException("Cannot reset after reconstitution has taken place.");

			if (m_classname != className)
				SetClassName(className);
			Xml = xml;
		}

		/// <summary>
		/// Reset the class and xml, after a data migration (and before reconstitution).
		/// </summary>
		/// <param name="className">Class name. (May be the same).</param>
		/// <param name="xmlBytes">New XML as UTF-8.</param>
		public void Reset(string className, byte[] xmlBytes)
		{
			if (String.IsNullOrEmpty(className)) throw new ArgumentException("className");
			if (xmlBytes == null || xmlBytes.Length == 0) throw new ArgumentException("xmlBytes");
			if (m_object != null)
				throw new InvalidOperationException("Cannot reset after reconstitution has taken place.");

			if (m_classname != className)
				SetClassName(className);
			m_xml = xmlBytes;
		}

		/// <summary>
		/// Update the surrogate, in order to write it out (to the DB4O backend).
		/// This may only change the class name if the object does not exist.
		/// This is very similar to Reset(), but I needed a method which can be used with
		/// fluffed-up surrogates (in normal saves) and which can also change the class
		/// (in data migration cases). I didn't want to modify Reset() because I don't know all
		/// the ways it is used and whether relaxing the constraint there might be dangerous.
		/// </summary>
		/// <param name="className"></param>
		/// <param name="xmlBytes"></param>
		public void Update(string className, byte[] xmlBytes)
		{
			if (String.IsNullOrEmpty(className)) throw new ArgumentException("className");
			if (xmlBytes == null || xmlBytes.Length == 0) throw new ArgumentException("xmlBytes");
			if (m_object != null && className != m_object.ClassName)
				throw new InvalidOperationException("Cannot change class after reconstitution has taken place.");

			if (m_classname != className)
				SetClassName(className);
			m_xml = xmlBytes;
		}

		#region Object overrides

		// Surrogates are unique, such that there is never more than one with the same Guid.
		// They are what is stored unbiquely in the IdentityMap.
		// So, '==' and 'Equals', as defined on Object, is fine.

		///// <summary>
		/////
		///// </summary>
		///// <param name="obj"></param>
		///// <returns></returns>
		//public override bool Equals(object obj)
		//{
		//    if (obj == null || !(obj is CmObjectSurrogate))
		//        return false;
		//    return m_guid == ((CmObjectSurrogate)obj).m_guid;
		//}

		//#region IEquatable<CmObjectSurrogate> Members

		//public bool Equals(CmObjectSurrogate other)
		//{
		//    //return m_guid == other.m_guid;
		//    return this == other;
		//}

		//#endregion

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return m_guid.GetHashCode();
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			lock (SyncRoot) // I (JohnT) don't think this lock is necessary but this is not a performance-critical method so I'll leave it.
				return m_guid.Guid + ((m_object != null) ? " : " + m_object : "");
		}

		#endregion Object overrides
	}

	/// <summary>
	/// CmObjectSurrogate repository.
	/// </summary>
	internal sealed class CmObjectSurrogateRepository : ICmObjectSurrogateRepository
	{
		private readonly IdentityMap m_identityMap;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="identityMap"></param>
		internal CmObjectSurrogateRepository(IdentityMap identityMap)
		{
			if (identityMap == null) throw new ArgumentNullException("identityMap");
			m_identityMap = identityMap;
		}

		#region Implementation of ICmObjectSurrogateRepository

		/// <summary>
		/// Get an id from the Guid in an XElement.
		/// Enhance JohnT: this belongs in some other interface now it no longer returns a surrogate.
		/// </summary>
		public ICmObjectId GetId(XElement reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			return (m_identityMap as ICmObjectIdFactory).FromGuid(new Guid(reader.Attribute("guid").Value));
		}

		/// <summary>
		/// Get a surrogate of the ICmObject.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>The surrogate of the ICmObject.</returns>
		public ICmObjectSurrogate GetSurrogate(ICmObject obj)
		{
			return m_identityMap.GetSurrogate(obj);
		}

		#endregion
	}

	/// <summary>
	/// Factory for creating ICmObjectSurrogate instances.
	/// </summary>
	internal sealed class CmObjectSurrogateFactory : ICmObjectSurrogateFactory
	{
		private readonly FdoCache m_cache;

		/// <summary>
		/// Constructor.
		/// </summary>
		internal CmObjectSurrogateFactory(FdoCache cache)
		{
			if (cache == null) throw new ArgumentNullException("cache");

			m_cache = cache;
		}

		#region Implementation of ICmObjectSurrogateFactory

		/// <summary>
		/// Create a surrogate from the data store.
		/// This gets the full XML string of the object from the BEP.
		/// </summary>
		/// <param name="xmlData"></param>
		public ICmObjectSurrogate Create(string xmlData)
		{
			return new CmObjectSurrogate(m_cache, xmlData);
		}

		/// <summary>
		/// Create a surrogate from the data store.
		/// This gets the full XML string of the object from the BEP.
		/// </summary>
		public ICmObjectSurrogate Create(byte[] xmlData)
		{
			return new CmObjectSurrogate(m_cache, xmlData);
		}

		/// <summary>
		/// Create a surrogate from the data store.
		/// This gets the full XML string of the object from the BEP.
		/// </summary>
		public ICmObjectSurrogate Create(Guid guid, string classname, string xmlData)
		{
			return new CmObjectSurrogate(m_cache, guid, classname, xmlData);
		}

		/// <summary>
		/// Create a surrogate from the data store.
		/// This gets the full XML string of the object from the BEP.
		/// </summary>
		public ICmObjectSurrogate Create(Guid guid, string classname, byte[] xmlData)
		{
			return new CmObjectSurrogate(m_cache, guid, classname, xmlData);
		}

		/// <summary>
		/// Create a surrogate from the data store.
		/// This gets the full XML string of the object from the BEP.
		/// </summary>
		public ICmObjectSurrogate Create(ICmObjectId objId, string classname, string xmlData)
		{
			return new CmObjectSurrogate(m_cache, objId, classname, xmlData);
		}

		/// <summary>
		/// Create a surrogate from the data store.
		/// This gets the full XML string of the object from the BEP.
		/// </summary>
		public ICmObjectSurrogate Create(ICmObjectId objId, string classname, byte[] xmlData)
		{
			return new CmObjectSurrogate(m_cache, objId, classname, xmlData);
		}

		/// <summary>
		/// Create a surrogate from some other surrogate (or CmObject).
		/// This is used for porting from one BEP to another,
		/// and it is faster than getting all the stuff from the xml string.
		/// </summary>
		public ICmObjectSurrogate Create(ICmObjectOrSurrogate source)
		{
			var sourceSurrogate = source as ICmObjectSurrogate;
			// No! This is not what is needed in porting,
			// since it makes a new surrogate with the ICmObject from the source BEP.
			//if (sourceSurrogate == null)
			//    return Create(source as ICmObject);
			if (sourceSurrogate == null)
			{
				// Have to make a new surrogate from the extant ICmObject information,
				// since we don't even want to think of just reusing the ICmObject.
				var asCmObject = (ICmObject)source;
				var asInternal = (ICmObjectInternal)asCmObject;
				return new CmObjectSurrogate(
					m_cache,
					CmObjectId.Create(asCmObject.Guid),
					asCmObject.ClassName,
					asInternal.ToXmlString());
			}
			return new CmObjectSurrogate(m_cache, sourceSurrogate);
		}

		/// <summary>
		/// Create one from an existing object; set its XML to the current state of the object.
		/// </summary>
		public ICmObjectSurrogate Create(ICmObject obj)
		{
			return CmObjectSurrogate.CreateSnapshot(obj);
		}


		#endregion
	}
}
