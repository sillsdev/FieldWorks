using System;
using System.Collections;	// Needed for HashTable.
using System.Collections.Generic; // Needed for generic Dictionary class.
using System.Text;
using System.Diagnostics;
using System.Xml;
using System.Runtime.InteropServices; // needed for Marshal

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;

namespace SIL.FieldWorks.CacheLight
{
	/// <summary>
	/// Loads original styled Fieldworks XML data into a RealDataCache.
	/// </summary>
	public sealed class RealCacheLoader : IFWDisposable
	{
#if COUNTMethodhits
		private uint m_loadObjectCount = 0;
#endif
		private IFwMetaDataCache m_metaDataCache;
		private RealDataCache m_realDataCache;
		private ITsStrFactory m_itsf = TsStrFactoryClass.Create();
		private TsStringfactory m_tsf;
		private Dictionary<string, int> m_wsCache = new Dictionary<string,int>();
		private Dictionary<HvoFlidKey, XmlNode> m_delayedAtomicReferences = new Dictionary<HvoFlidKey, XmlNode>();
		private Dictionary<HvoFlidKey, List<XmlNode>> m_delayedVecterReferences = new Dictionary<HvoFlidKey, List<XmlNode>>();
		private Dictionary<ClidFieldnameKey, uint> m_cachedFlids = new Dictionary<ClidFieldnameKey, uint>();

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
		private bool m_isDisposed = false;

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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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
				if (m_tsf != null)
					m_tsf.Dispose();
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
		public RealDataCache LoadCache(string metadataPathname, string realDataPathname, Dictionary<int, uint> objects)
		{
			CheckDisposed();

			m_realDataCache = new RealDataCache();
			m_realDataCache.CheckWithMDC = false;

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
				DateTime end = DateTime.Now;
				TimeSpan span = new TimeSpan(end.Ticks - start.Ticks);
				string totalTime = String.Format("Hours: {0}, Minutes: {1}, Seconds: {2}, Millseconds: {3}",
					span.Hours.ToString(), span.Minutes.ToString(), span.Seconds.ToString(), span.Milliseconds.ToString());
				Debug.WriteLine("Time to load MDC: " + totalTime);
				start = end;
				//memory = objectBrowser.PrivateMemorySize64;
				//Debug.WriteLine(String.Format("Memory used (loaded MDC load): {0}.", memory.ToString()));
#endif

				XmlDocument doc = new XmlDocument();
				doc.Load(realDataPathname);
#if DEBUG
				end = DateTime.Now;
				span = new TimeSpan(end.Ticks - start.Ticks);
				totalTime = String.Format("Hours: {0}, Minutes: {1}, Seconds: {2}, Millseconds: {3}",
					span.Hours.ToString(), span.Minutes.ToString(), span.Seconds.ToString(), span.Milliseconds.ToString());
				Debug.WriteLine("Time to load XML: " + totalTime);
				start = end;
				//memory = objectBrowser.PrivateMemorySize64;
				//Debug.WriteLine(String.Format("Memory used (loaded data XML): {0}.", memory.ToString()));
#endif

				// Load Writing Systems first.
				int ord = 0;
				int hvo;
				uint clid = 0;
				{
					XmlNodeList wsNodes = doc.DocumentElement.SelectNodes("LgWritingSystem");
					uint flid = m_metaDataCache.GetFieldId("LgWritingSystem", "ICULocale", false);
					// We need a full list of ints and strings for Wses,
					// before we can load string data,
					// so cache the barebones first.
					foreach (XmlNode wsNode in wsNodes)
					{
						hvo = BootstrapWs(wsNode, flid, out clid, objects);
					}
					foreach (XmlNode wsNode in wsNodes)
					{
						string uid = wsNode.Attributes["id"].Value.Substring(1);
						hvo = m_realDataCache.get_ObjFromGuid(new Guid(uid));
						LoadObject(wsNode, hvo, clid, objects);
					}
				}
				// Now load other ownerless objects, except LangProject and Wses.
				foreach (XmlNode otherOwnerlessNode in doc.DocumentElement.ChildNodes)
				{
					if (otherOwnerlessNode.Name != "LangProject" && otherOwnerlessNode.Name != "LgWritingSystem")
					{
						hvo = LoadCmObjectProperties(otherOwnerlessNode, 0, 0, ord, out clid, objects);
						LoadObject(otherOwnerlessNode, hvo, clid, objects);
					}
				}
				// Now load LangProject
				XmlNode langProjectNode = doc.DocumentElement.SelectSingleNode("LangProject");
				hvo = LoadCmObjectProperties(langProjectNode, 0, 0, ord, out clid, objects);
				LoadObject(langProjectNode, hvo, clid, objects);

				// Set references
				// Set atomic references
				foreach (KeyValuePair<HvoFlidKey, XmlNode> kvp in m_delayedAtomicReferences)
				{
					string uid = kvp.Value.Attributes["target"].Value.Substring(1);
					try
					{
						int hvoTarget = m_realDataCache.get_ObjFromGuid(new Guid(uid));
						m_realDataCache.CacheObjProp(kvp.Key.Hvo, (int)kvp.Key.Flid, hvoTarget);
					}
					catch
					{
						// Invalid reference. Just clear the cache in case there is a save.
						m_realDataCache.SetObjProp(kvp.Key.Hvo, (int)kvp.Key.Flid, 0);
					}
				}
				//// Remove all items from m_delayedAtomicReferences that are in handledRefs.
				//// Theory has it that m_delayedAtomicReferences should then be empty.
				m_delayedAtomicReferences.Clear();

				// Set vector (col or seq) references.
				foreach (KeyValuePair<HvoFlidKey, List<XmlNode>> kvp in m_delayedVecterReferences)
				{
					List<int> hvos = new List<int>();
					foreach (XmlNode obj in kvp.Value)
					{
						string uid = obj.Attributes["target"].Value.Substring(1);
						try
						{
							int ownedHvo = m_realDataCache.get_ObjFromGuid(new Guid(uid));
							hvos.Add(ownedHvo);
						}
						catch
						{
							// Invalid reference. Just remove the bogus hvo.
							// Since the id is added after the exception, it is effectively 'removed'.
							Debug.WriteLine("Bogus Id found.");
						}
					}
					m_realDataCache.CacheVecProp(kvp.Key.Hvo, (int)kvp.Key.Flid, hvos.ToArray(), hvos.Count);
				}
				m_delayedVecterReferences.Clear();
#if DEBUG
				end = DateTime.Now;
				span = new TimeSpan(end.Ticks - start.Ticks);
				totalTime = String.Format("Hours: {0}, Minutes: {1}, Seconds: {2}, Millseconds: {3}",
					span.Hours.ToString(), span.Minutes.ToString(), span.Seconds.ToString(), span.Milliseconds.ToString());
				Debug.WriteLine("Time to load main Cache: " + totalTime);
				start = end;

				Debug.WriteLine(String.Format("Number of objects cached: {0}", (m_realDataCache.NextHvo - 1).ToString()));
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
		private int BootstrapWs(XmlNode wsNode, uint flid, out uint clid, Dictionary<int, uint> objects)
		{
			int hvo = LoadCmObjectProperties(wsNode, 0, 0, 0, out clid, objects);
			// <ICULocale24><Uni>fr</Uni></ICULocale24>
			XmlNode uniNode = wsNode.SelectSingleNode("ICULocale24/Uni");
			string wsText = uniNode.InnerText;
			m_realDataCache.CacheUnicodeProp(hvo, (int)flid, wsText, wsText.Length);
			m_wsCache[wsText] = hvo;

			return hvo;
		}

		private int LoadCmObjectProperties(XmlNode objectNode, int owner, uint owningFlid, int ord, out uint clid, Dictionary<int, uint> objects)
		{
			int hvo = m_realDataCache.NextHvo;
			clid = m_metaDataCache.GetClassId(objectNode.Name);
			objects.Add(hvo, clid);
			m_realDataCache.CacheIntProp(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
			string uid = objectNode.Attributes["id"].Value.Substring(1);
			m_realDataCache.CacheGuidProp(hvo, (int)CmObjectFields.kflidCmObject_Guid, new Guid(uid));
			if (owner > 0)
				m_realDataCache.CacheObjProp(hvo, (int)CmObjectFields.kflidCmObject_Owner, owner);
			m_realDataCache.CacheIntProp(hvo, (int)CmObjectFields.kflidCmObject_OwnFlid, (int)owningFlid);
			m_realDataCache.CacheIntProp(hvo, (int)CmObjectFields.kflidCmObject_OwnOrd, ord);
			// Maybe need to cache the two other CmObject columns (UpdStmp and UpdDttm)
			return hvo;
		}

		private void LoadObject(XmlNode objectNode, int hvo, uint clid, Dictionary<int, uint> objects)
		{
#if COUNTMethodhits
		m_loadObjectCount++;
#endif
			// Optimize by looping over the child nodes,
			// and dealing with relevant flid.
			// The idea is that most objects will only have a subset of fields filled in.
#if false
			foreach (XmlNode fieldNode in objectNode.ChildNodes)
			{
#else
			XmlNodeList nodeList = objectNode.ChildNodes;
			for (int i = 0; i < nodeList.Count; ++i)
			{
				XmlNode fieldNode = nodeList[i]; // Item(i);
#endif
				string fieldName = fieldNode.Name;
				int idx = fieldName.IndexOfAny(new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' });
				fieldName = fieldName.Substring(0, idx);
				ClidFieldnameKey cfk = new ClidFieldnameKey(clid, fieldName);
				uint uflid = 0;
				if (m_cachedFlids.ContainsKey(cfk))
				{
					uflid = m_cachedFlids[cfk];
				}
				else
				{
					uflid = m_metaDataCache.GetFieldId2(clid, fieldName, true);
					m_cachedFlids[cfk] = uflid;
				}

				int flidType = m_metaDataCache.GetFieldType(uflid);
				uint ownedClid;
				int ownedHvo;
				switch (flidType)
				{
					case (int)CellarModuleDefns.kcptBoolean:
						// <System18><Boolean val="true"/></System18>
						m_realDataCache.CacheBooleanProp(hvo, (int)uflid, bool.Parse(fieldNode.FirstChild.Attributes["val"].Value));
						break;
					case (int)CellarModuleDefns.kcptInteger:
						// <Type18><Integer val="1"/></Type18>
						m_realDataCache.CacheIntProp(hvo, (int)uflid, Int32.Parse(fieldNode.FirstChild.Attributes["val"].Value));
						break;
					case (int)CellarModuleDefns.kcptNumeric:
						break;
					case (int)CellarModuleDefns.kcptFloat:
						break;
					case (int)CellarModuleDefns.kcptTime:
						// <LastModified24><Time val="2005-11-18 02:48:33.000"/></LastModified24>
						DateTime valTime = DateTime.Parse(fieldNode.FirstChild.Attributes["val"].Value);
						m_realDataCache.CacheTimeProp(hvo, (int)uflid, valTime.Ticks);
						break;
					case (int)CellarModuleDefns.kcptGuid:
						if (uflid != (uint)CmObjectFields.kflidCmObject_Guid)
						{
							// <App18><Guid val="5EA62D01-7A78-11D4-8078-0000C0FB81B5"/></App18>
							string uid = fieldNode.FirstChild.Attributes["val"].Value;
							m_realDataCache.CacheGuidProp(hvo, (int)uflid, new Guid(uid));
						}
						break;
					case (int)CellarModuleDefns.kcptImage:
						break;
					case (int)CellarModuleDefns.kcptGenDate:
						// <DateOfEvent4006><GenDate val=\"193112111\" /></DateOfEvent4006>
						break;
					case (int)CellarModuleDefns.kcptBinary:
						// <Details18><Binary>03000000</Binary></Details18>
						// <Details18><Binary>05000000\r\n</Binary></Details18>
						break;
					case (int)CellarModuleDefns.kcptString:
					case (int)CellarModuleDefns.kcptBigString:
						// "<Str><Run ws=\"eZPI\">Te mgyeey ne la Benit nuu Pwert. Za men gun men inbitar xmig men ne la Jasint nuu San José. Za Benit. Weey Benit mël. Weey Benit mëlbyuu ne ygued Benit lo xmig Benit, Jasint. Chene wdxiin Benit ruxyuu Jasint, re Benit:</Run></Str>"
						foreach (XmlNode strNode in fieldNode.ChildNodes)
						{
							ITsString tssStr = m_tsf.CreateFromStr(strNode);
							m_realDataCache.CacheStringProp(hvo, (int)uflid, tssStr);
						}
						// CacheStringProp(hvo, tag, tss);
						break;
					case (int)CellarModuleDefns.kcptMultiString: // <AStr>
					case (int)CellarModuleDefns.kcptMultiBigString: // <AStr
						foreach (XmlNode aStrAlt in fieldNode.ChildNodes)
						{
							int wsAStr;
							ITsString tssAlt = m_tsf.CreateFromAStr(aStrAlt, out wsAStr);
							m_realDataCache.CacheStringAlt(hvo, (int)uflid, wsAStr, tssAlt);
						}
						break;
					case (int)CellarModuleDefns.kcptUnicode: // Fall through.
					case (int)CellarModuleDefns.kcptBigUnicode:
						string unicodeText = fieldNode.FirstChild.InnerText;
						m_realDataCache.CacheUnicodeProp(hvo, (int)uflid, unicodeText, unicodeText.Length);
						break;
					case (int)CellarModuleDefns.kcptMultiUnicode: // <AUni>
						foreach (XmlNode uniNode in fieldNode.ChildNodes)
						{
							int ws = m_wsCache[uniNode.Attributes["ws"].Value];
							string uniText = uniNode.InnerText;
							m_realDataCache.CacheStringAlt(hvo, (int)uflid, ws, m_itsf.MakeString(uniText, ws));
						}
						break;
					case (int)CellarModuleDefns.kcptMultiBigUnicode:
						break;

					// Cases for regular objects.
					case (int)CellarModuleDefns.kcptOwningAtom:
						XmlNode atomicOwnedObject = fieldNode.FirstChild;
						ownedHvo = LoadCmObjectProperties(atomicOwnedObject, hvo, uflid, 0, out ownedClid, objects);
						LoadObject(atomicOwnedObject, ownedHvo, ownedClid, objects);
						m_realDataCache.CacheObjProp(hvo, (int)uflid, ownedHvo);
						break;
					case (int)CellarModuleDefns.kcptReferenceAtom:
						/* Some are simple Guid links, but others contain more info.
						<Category5059>
							<Link target="I751B8DE1-089B-42B1-A35E-62CF838A27A3" ws="en" abbr="N" name="noun"/>
						</Category5059>
						<Morph5112>
							<Link target="I9370DD7D-978D-484D-B304-B5D4700BAA30"/>
						</Morph5112>
						*/
						// Defer caching references, until all objects are loaded.
						m_delayedAtomicReferences[new HvoFlidKey(hvo, uflid)] = fieldNode.FirstChild;
						break;
					case (int)CellarModuleDefns.kcptOwningCollection: // Fall through.
					case (int)CellarModuleDefns.kcptOwningSequence:
						List<int> hvos = new List<int>();
						int newOrd = 0;
						foreach (XmlNode obj in fieldNode.ChildNodes)
						{
							ownedHvo = LoadCmObjectProperties(obj, hvo, uflid, newOrd, out ownedClid, objects);
							LoadObject(obj, ownedHvo, ownedClid, objects);
							hvos.Add(ownedHvo);
							if (flidType == (int)CellarModuleDefns.kcptOwningSequence)
								newOrd++;
						}
						m_realDataCache.CacheVecProp(hvo, (int)uflid, hvos.ToArray(), hvos.Count);
						break;
					case (int)CellarModuleDefns.kcptReferenceCollection: // Fall through.
					case (int)CellarModuleDefns.kcptReferenceSequence:
						// <Link target="ID75F7FB5-BABD-4D60-B57F-E188BEF264B7" />
						// Defer caching references, until all objects are loaded.
						List<XmlNode> list = new List<XmlNode>();
						m_delayedVecterReferences[new HvoFlidKey(hvo, uflid)] = list;
						foreach (XmlNode linkNode in fieldNode.ChildNodes)
							list.Add(linkNode);
						break;
				}
			}
		}
	}

	internal struct ClidFieldnameKey
	{
		public uint m_clid;
		public string m_fieldname;

		public ClidFieldnameKey(uint clid, string fieldname)
		{
			m_clid = clid;
			m_fieldname = fieldname;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is ClidFieldnameKey))
				return false;

			ClidFieldnameKey cfk = (ClidFieldnameKey)obj;
			return (cfk.m_clid == m_clid)
				&& (cfk.m_fieldname == m_fieldname);
		}

		public override int GetHashCode()
		{
			return ((int)m_clid ^ m_fieldname.GetHashCode());
		}
	}
}
