using System;
using System.Collections.Generic; // Needed for generic Di
using System.Diagnostics;
using System.Xml;
using System.Runtime.InteropServices; // needed for Marshal
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.CacheLight
{
	/// <summary>
	/// Loads original styled Fieldworks XML data into a RealDataCache.
	/// </summary>
	public sealed class RealCacheLoader : IFWDisposable
	{
		private IFwMetaDataCache m_metaDataCache;
		private RealDataCache m_realDataCache;
		private ITsStrFactory m_itsf = TsStrFactoryClass.Create();
		private TsStringfactory m_tsf;
		private Dictionary<string, int> m_wsCache = new Dictionary<string,int>();
		private Dictionary<HvoFlidKey, XmlNode> m_delayedAtomicReferences = new Dictionary<HvoFlidKey, XmlNode>();
		private Dictionary<HvoFlidKey, List<XmlNode>> m_delayedVecterReferences = new Dictionary<HvoFlidKey, List<XmlNode>>();
		private Dictionary<ClidFieldnameKey, int> m_cachedFlids = new Dictionary<ClidFieldnameKey, int>();

		/// <summary>
		/// Constructor.
		/// </summary>
		public RealCacheLoader()
		{
			m_tsf = new TsStringfactory(m_itsf, m_wsCache);
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

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

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~RealCacheLoader()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
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
		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				if (m_wsCache != null)
					m_wsCache.Clear();
				if (m_delayedAtomicReferences != null)
					m_delayedAtomicReferences.Clear();
				if (m_delayedVecterReferences != null)
					m_delayedVecterReferences.Clear();
				if (m_cachedFlids != null)
					m_cachedFlids.Clear();
				if (m_realDataCache != null)
					m_realDataCache.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			if (m_itsf != null)
			{
				if (Marshal.IsComObject(m_itsf))
					Marshal.ReleaseComObject(m_itsf);
				m_itsf = null;
			}
			m_tsf = null;
			m_metaDataCache = null;
			m_realDataCache = null;
			m_wsCache = null;
			m_delayedAtomicReferences = null;
			m_delayedVecterReferences = null;
			m_cachedFlids = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Create a RealDataCache object, and laod it with metadata and real data.
		/// </summary>
		/// <param name="metadataPathname"></param>
		/// <param name="realDataPathname"></param>
		/// <param name="objects"></param>
		/// <returns></returns>
		public ISilDataAccess LoadCache(string metadataPathname, string realDataPathname, Dictionary<int, int> objects)
		{
			CheckDisposed();

			m_realDataCache = new RealDataCache {CheckWithMDC = false};

			try
			{
#if DEBUG
				//Process objectBrowser = Process.GetCurrentProcess();
				//long memory = objectBrowser.PrivateMemorySize64;
				//Debug.WriteLine(String.Format("Memory used (start load): {0}.", memory.ToString()));

				DateTime start = DateTime.Now;
#endif
				m_metaDataCache = new MetaDataCache();
				m_metaDataCache.InitXml(metadataPathname, true);
				m_realDataCache.MetaDataCache = m_metaDataCache;
#if DEBUG
				var end = DateTime.Now;
				var span = new TimeSpan(end.Ticks - start.Ticks);
				var totalTime = String.Format("Hours: {0}, Minutes: {1}, Seconds: {2}, Millseconds: {3}",
					span.Hours, span.Minutes, span.Seconds, span.Milliseconds);
				Debug.WriteLine("Time to load MDC: " + totalTime);
				start = end;
				//memory = objectBrowser.PrivateMemorySize64;
				//Debug.WriteLine(String.Format("Memory used (loaded MDC load): {0}.", memory.ToString()));
#endif

				var doc = new XmlDocument();
				doc.Load(realDataPathname);
#if DEBUG
				end = DateTime.Now;
				span = new TimeSpan(end.Ticks - start.Ticks);
				totalTime = String.Format("Hours: {0}, Minutes: {1}, Seconds: {2}, Millseconds: {3}",
					span.Hours, span.Minutes, span.Seconds, span.Milliseconds);
				Debug.WriteLine("Time to load XML: " + totalTime);
				start = end;
				//memory = objectBrowser.PrivateMemorySize64;
				//Debug.WriteLine(String.Format("Memory used (loaded data XML): {0}.", memory.ToString()));
#endif
				int hvo, clid = 0;
				const int ord = 0;

				// First load all objects as if they were ownerless, except top-level objects.
				foreach (XmlNode ownedNode in doc.DocumentElement.ChildNodes)
				{
					if (ownedNode.Attributes.GetNamedItem("owner").Value == "none")
						continue;

					hvo = LoadCmObjectProperties(ownedNode, 0, 0, ord, out clid, objects);
					LoadObject(ownedNode, hvo, clid, objects);
				}
				// Now load all owned objects
				//XmlNode langProjectNode = doc.DocumentElement.SelectSingleNode("LangProject");
				//hvo = LoadCmObjectProperties(langProjectNode, 0, 0, ord, out clid, objects);
				//LoadObject(langProjectNode, hvo, clid, objects);
				foreach (XmlNode unownedNode in doc.DocumentElement.ChildNodes)
				{
					if (unownedNode.Attributes.GetNamedItem("owner").Value == "none")
					{
						hvo = LoadCmObjectProperties(unownedNode, 0, 0, ord, out clid, objects);
						LoadObject(unownedNode, hvo, clid, objects);
					}
				}

				// Set references
				// Set atomic references
				foreach (var kvp in m_delayedAtomicReferences)
				{
					var id = kvp.Value.Attributes["target"].Value.Substring(1);
					try
					{
						var hvoTarget = m_realDataCache.get_ObjFromGuid(new Guid(id));
						m_realDataCache.CacheObjProp(kvp.Key.Hvo, kvp.Key.Flid, hvoTarget);
					}
					catch
					{
						// Invalid reference. Just clear the cache in case there is a save.
						m_realDataCache.SetObjProp(kvp.Key.Hvo, kvp.Key.Flid, 0);
					}
				}
				//// Remove all items from m_delayedAtomicReferences that are in handledRefs.
				//// Theory has it that m_delayedAtomicReferences should then be empty.
				m_delayedAtomicReferences.Clear();

				// Set vector (col or seq) references.
				foreach (var kvp in m_delayedVecterReferences)
				{
					var hvos = new List<int>();
					foreach (XmlNode obj in kvp.Value)
					{
						var id = obj.Attributes["target"].Value.Substring(1);
						try
						{
							var ownedHvo = m_realDataCache.get_ObjFromGuid(new Guid(id));
							hvos.Add(ownedHvo);
						}
						catch
						{
							// Invalid reference. Just remove the bogus hvo.
							// Since the id is added after the exception, it is effectively 'removed'.
							Debug.WriteLine("Bogus Id found.");
						}
					}
					m_realDataCache.CacheVecProp(kvp.Key.Hvo, kvp.Key.Flid, hvos.ToArray(), hvos.Count);
				}
				m_delayedVecterReferences.Clear();
#if DEBUG
				end = DateTime.Now;
				span = new TimeSpan(end.Ticks - start.Ticks);
				totalTime = String.Format("Hours: {0}, Minutes: {1}, Seconds: {2}, Millseconds: {3}",
					span.Hours, span.Minutes, span.Seconds, span.Milliseconds);
				Debug.WriteLine("Time to load main Cache: " + totalTime);

				Debug.WriteLine(String.Format("Number of objects cached: {0}", (m_realDataCache.NextHvo - 1)));
				//memory = objectBrowser..PrivateMemorySize64;
				//Debug.WriteLine(String.Format("Memory used (cache loaded): {0}.", memory.ToString()));
#endif
			}
			finally
			{
				m_realDataCache.CheckWithMDC = true;
			}
			return m_realDataCache;
		}

		/// <summary>
		/// Need to cache its class and ICULocale properties, so we can then load string data.
		/// </summary>
		/// <param name="wsNode"></param>
		/// <param name="flid"></param>
		/// <param name="clid"></param>
		/// <param name="objects"></param>
		private void BootstrapWs(XmlNode wsNode, int flid, out int clid, IDictionary<int, int> objects)
		{
			var hvo = LoadCmObjectProperties(wsNode, 0, 0, 0, out clid, objects);
			// <ICULocale24><Uni>fr</Uni></ICULocale24>
			XmlNode uniNode = wsNode.SelectSingleNode("ICULocale24/Uni");
			var wsText = uniNode.InnerText;
			m_realDataCache.CacheUnicodeProp(hvo, flid, wsText, wsText.Length);
			m_wsCache[wsText] = hvo;
		}

		private int LoadCmObjectProperties(XmlNode objectNode, int owner, int owningFlid, int ord, out int clid, IDictionary<int, int> objects)
		{
			var hvo = m_realDataCache.NextHvo;
			clid = m_metaDataCache.GetClassId(objectNode.Name);
			objects.Add(hvo, clid);
			m_realDataCache.CacheIntProp(hvo, (int)CmObjectFields.kflidCmObject_Class, clid);
			var id = objectNode.Attributes["id"].Value.Substring(1);
			m_realDataCache.CacheGuidProp(hvo, (int)CmObjectFields.kflidCmObject_Guid, new Guid(id));
			if (owner > 0)
				m_realDataCache.CacheObjProp(hvo, (int)CmObjectFields.kflidCmObject_Owner, owner);
			m_realDataCache.CacheIntProp(hvo, (int)CmObjectFields.kflidCmObject_OwnFlid, owningFlid);
			m_realDataCache.CacheIntProp(hvo, (int)CmObjectFields.kflidCmObject_OwnOrd, ord);
			// Maybe need to cache the two other CmObject columns (UpdStmp and UpdDttm)
			return hvo;
		}

		private void LoadObject(XmlNode objectNode, int hvo, int clid, IDictionary<int, int> objects)
		{
			// Optimize by looping over the child nodes,
			// and dealing with relevant flid.
			// The idea is that most objects will only have a subset of fields filled in.
			var nodeList = objectNode.ChildNodes;
			for (var i = 0; i < nodeList.Count; ++i)
			{
				var fieldNode = nodeList[i]; // Item(i);
				var fieldName = fieldNode.Name;
				var idx = fieldName.IndexOfAny(new[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' });
				fieldName = fieldName.Substring(0, idx);
				var cfk = new ClidFieldnameKey(clid, fieldName);
				int flid;
				if (m_cachedFlids.ContainsKey(cfk))
				{
					flid = m_cachedFlids[cfk];
				}
				else
				{
					flid = m_metaDataCache.GetFieldId2(clid, fieldName, true);
					m_cachedFlids[cfk] = flid;
				}

				var flidType = (CellarPropertyType)m_metaDataCache.GetFieldType(flid);
				int ownedClid;
				int ownedHvo;
				switch (flidType)
				{
					case CellarPropertyType.Boolean:
						// <System18><Boolean val="true"/></System18>
						m_realDataCache.CacheBooleanProp(hvo, flid, bool.Parse(fieldNode.FirstChild.Attributes["val"].Value));
						break;
					case CellarPropertyType.Integer:
						// <Type18><Integer val="1"/></Type18>
						m_realDataCache.CacheIntProp(hvo, flid, Int32.Parse(fieldNode.FirstChild.Attributes["val"].Value));
						break;
					case CellarPropertyType.Numeric:
						break;
					case CellarPropertyType.Float:
						break;
					case CellarPropertyType.Time:
						// <LastModified24><Time val="2005-11-18 02:48:33.000"/></LastModified24>
						var valTime = DateTime.Parse(fieldNode.FirstChild.Attributes["val"].Value);
						m_realDataCache.CacheTimeProp(hvo, flid, valTime.Ticks);
						break;
					case CellarPropertyType.Guid:
						if (flid != (int)CmObjectFields.kflidCmObject_Guid)
						{
							// <App18><Guid val="5EA62D01-7A78-11D4-8078-0000C0FB81B5"/></App18>
							var id = fieldNode.FirstChild.Attributes["val"].Value;
							m_realDataCache.CacheGuidProp(hvo, flid, new Guid(id));
						}
						break;
					case CellarPropertyType.Image:
						break;
					case CellarPropertyType.GenDate:
						// <DateOfEvent4006><GenDate val=\"193112111\" /></DateOfEvent4006>
						break;
					case CellarPropertyType.Binary:
						// <Details18><Binary>03000000</Binary></Details18>
						// <Details18><Binary>05000000\r\n</Binary></Details18>
						break;
					case CellarPropertyType.String:
					case CellarPropertyType.BigString:
						// "<Str><Run ws=\"eZPI\">Te mgyeey ne la Benit nuu Pwert. Za men gun men inbitar xmig men ne la Jasint nuu San José. Za Benit. Weey Benit mël. Weey Benit mëlbyuu ne ygued Benit lo xmig Benit, Jasint. Chene wdxiin Benit ruxyuu Jasint, re Benit:</Run></Str>"
						foreach (XmlNode strNode in fieldNode.ChildNodes)
						{
							var tssStr = m_tsf.CreateFromStr(strNode);
							m_realDataCache.CacheStringProp(hvo, flid, tssStr);
						}
						// CacheStringProp(hvo, tag, tss);
						break;
					case CellarPropertyType.MultiString: // <AStr>
					case CellarPropertyType.MultiBigString: // <AStr
						foreach (XmlNode aStrAlt in fieldNode.ChildNodes)
						{
							int wsAStr;
							var tssAlt = m_tsf.CreateFromAStr(aStrAlt, out wsAStr);
							m_realDataCache.CacheStringAlt(hvo, flid, wsAStr, tssAlt);
						}
						break;
					case CellarPropertyType.Unicode: // Fall through.
					case CellarPropertyType.BigUnicode:
						string unicodeText = fieldNode.FirstChild.InnerText;
						m_realDataCache.CacheUnicodeProp(hvo, flid, unicodeText, unicodeText.Length);
						break;
					case CellarPropertyType.MultiUnicode: // <AUni>
						foreach (XmlNode uniNode in fieldNode.ChildNodes)
						{
							var ws = m_wsCache[uniNode.Attributes["ws"].Value];
							var uniText = uniNode.InnerText;
							m_realDataCache.CacheStringAlt(hvo, flid, ws, m_itsf.MakeString(uniText, ws));
						}
						break;
					case CellarPropertyType.MultiBigUnicode:
						break;

					// Cases for regular objects.
					case CellarPropertyType.OwningAtomic:
						XmlNode atomicOwnedObject = fieldNode.FirstChild;
						ownedHvo = LoadCmObjectProperties(atomicOwnedObject, hvo, flid, 0, out ownedClid, objects);
						LoadObject(atomicOwnedObject, ownedHvo, ownedClid, objects);
						m_realDataCache.CacheObjProp(hvo, flid, ownedHvo);
						break;
					case CellarPropertyType.ReferenceAtomic:
						/* Some are simple Guid links, but others contain more info.
						<Category5059>
							<Link target="I751B8DE1-089B-42B1-A35E-62CF838A27A3" ws="en" abbr="N" name="noun"/>
						</Category5059>
						<Morph5112>
							<Link target="I9370DD7D-978D-484D-B304-B5D4700BAA30"/>
						</Morph5112>
						*/
						// Defer caching references, until all objects are loaded.
						m_delayedAtomicReferences[new HvoFlidKey(hvo, flid)] = fieldNode.FirstChild;
						break;
					case CellarPropertyType.OwningCollection: // Fall through.
					case CellarPropertyType.OwningSequence:
						var hvos = new List<int>();
						var newOrd = 0;
						foreach (XmlNode obj in fieldNode.ChildNodes)
						{
							ownedHvo = LoadCmObjectProperties(obj, hvo, flid, newOrd, out ownedClid, objects);
							LoadObject(obj, ownedHvo, ownedClid, objects);
							hvos.Add(ownedHvo);
							if (flidType == CellarPropertyType.OwningSequence)
								newOrd++;
						}
						m_realDataCache.CacheVecProp(hvo, flid, hvos.ToArray(), hvos.Count);
						break;
					case CellarPropertyType.ReferenceCollection: // Fall through.
					case CellarPropertyType.ReferenceSequence:
						// <Link target="ID75F7FB5-BABD-4D60-B57F-E188BEF264B7" />
						// Defer caching references, until all objects are loaded.
						var list = new List<XmlNode>();
						m_delayedVecterReferences[new HvoFlidKey(hvo, flid)] = list;
						foreach (XmlNode linkNode in fieldNode.ChildNodes)
							list.Add(linkNode);
						break;
				}
			}
		}
	}

	internal struct ClidFieldnameKey
	{
		public int m_clid;
		public string m_fieldname;

		public ClidFieldnameKey(int clid, string fieldname)
		{
			m_clid = clid;
			m_fieldname = fieldname;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is ClidFieldnameKey))
				return false;

			var cfk = (ClidFieldnameKey)obj;
			return (cfk.m_clid == m_clid)
				&& (cfk.m_fieldname == m_fieldname);
		}

		public override int GetHashCode()
		{
			return (m_clid ^ m_fieldname.GetHashCode());
		}
	}
}
