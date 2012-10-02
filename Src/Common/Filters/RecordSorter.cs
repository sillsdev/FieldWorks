// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: .cs
// History: John Hatton, created
// Last reviewed:
//
// <remarks>
//	At the moment (April 2004), the only concrete instance of this class (PropertyRecordSorter)
//		does sorting in memory,	based on a FDO property.
//	This does not imply that all sorting will always be done in memory, only that we haven't
//	yet designed or implemented a way to do the sorting while querying.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;
using System.Xml;
using System.Runtime.InteropServices;

using XCore;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Collections.Generic;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// Just a shell class for containing runtime Switches for controling the diagnostic output.
	/// </summary>
	public class RuntimeSwitches
	{
		/// Tracing variable - used to control when and what is output to the debug and trace listeners
		public static TraceSwitch RecordTimingSwitch = new TraceSwitch("FilterRecordTiming", "Used for diagnostic timing output", "Off");
	}

	/// <summary>
	/// sort (in memory) based on in FDO property
	/// </summary>
	public class PropertyRecordSorter : RecordSorter
	{
		/// <summary></summary>
		protected string m_propertyName;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PropertyRecordSorter"/> class.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// ------------------------------------------------------------------------------------
		public PropertyRecordSorter(string propertyName)
		{
			Init(propertyName);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PropertyRecordSorter"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PropertyRecordSorter()
		{

		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the specified property name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// ------------------------------------------------------------------------------------
		protected void Init(string propertyName)
		{
			m_propertyName = propertyName;
		}

		/// <summary>
		/// Add to the specified XML node information required to create a new
		/// record sorter equivalent to yourself.
		/// In this case we just need to add the sortProperty attribute.
		/// </summary>
		/// <param name="node"></param>
		public override void PersistAsXml(XmlNode node)
		{
			XmlAttribute xaSort = node.OwnerDocument.CreateAttribute("sortProperty");
			xaSort.Value = m_propertyName;
			node.Attributes.Append(xaSort);
		}

		/// <summary>
		/// Initialize an instance into the state indicated by the node, which was
		/// created by a call to PersistAsXml.
		/// </summary>
		/// <param name="node"></param>
		public override void InitXml(XmlNode node)
		{
			Init(node);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		/// <value>The name of the property.</value>
		/// ------------------------------------------------------------------------------------
		public string PropertyName
		{
			get { return m_propertyName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the specified configuration.
		/// </summary>
		/// <param name="configuration">The configuration.</param>
		/// ------------------------------------------------------------------------------------
		protected void Init(XmlNode configuration)
		{
			m_propertyName =XmlUtils.GetManditoryAttributeValue(configuration, "sortProperty");
		}

		/// <summary>
		/// Get the object that does the comparisons.
		/// </summary>
		/// <returns></returns>
		protected internal override IComparer getComparer()
		{
			RecordSorter.FdoCompare fc = (RecordSorter.FdoCompare)new RecordSorter.FdoCompare(m_propertyName);
			return (IComparer)fc;
		}

		/// <summary>
		/// Release the object that does the comparisons.
		/// </summary>
		/// <param name="comp"></param>
		protected internal override void releaseComparer(IComparer comp)
		{
			RecordSorter.FdoCompare fc = (RecordSorter.FdoCompare)comp;
			fc.CloseCollatingEngine();	// We MUST release the ICU data file.
			fc.Dispose();
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the specified records.
		/// </summary>
		/// <param name="records">The records.</param>
		/// ------------------------------------------------------------------------------------
		public override void Sort(/*ref*/ ArrayList records)
		{
#if DEBUG
			DateTime dt1 = DateTime.Now;
			int tc1 = Environment.TickCount;
#endif
			IComparer fc = getComparer();
			records.Sort((IComparer)fc);
			releaseComparer(fc);
			fc = null; // Just to make sure we don't accidently use it later

#if DEBUG
			// only do this if the timing switch is info or verbose
			if (RuntimeSwitches.RecordTimingSwitch.TraceInfo)
			{
			int tc2 = Environment.TickCount;
			TimeSpan ts1 = DateTime.Now - dt1;
			string s = "PropertyRecordSorter: Sorting " + records.Count + " records took " +
				(tc2 - tc1) + " ticks," + " or " + ts1.Minutes + ":" + ts1.Seconds + "." +
				ts1.Milliseconds.ToString("d3") + " min:sec.";
				Debug.WriteLine(s, RuntimeSwitches.RecordTimingSwitch.DisplayName);
			}
#endif
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the into.
		/// </summary>
		/// <param name="records">The records.</param>
		/// <param name="newRecords">The new records.</param>
		/// ------------------------------------------------------------------------------------
		public override void MergeInto(/*ref*/ ArrayList records, ArrayList newRecords)
		{
			using (RecordSorter.FdoCompare fc = (RecordSorter.FdoCompare)new RecordSorter.FdoCompare(m_propertyName))
			{
				MergeInto(records, newRecords, (IComparer)fc);
				fc.CloseCollatingEngine();		// We MUST release the ICU data file.
			}
		}

		/// <summary>
		/// a factory method for property sorders
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="configuration"></param>
		/// <returns></returns>
		static public PropertyRecordSorter Create(FdoCache cache, XmlNode configuration)
		{
			PropertyRecordSorter sorter = (PropertyRecordSorter)DynamicLoader.CreateObject(configuration);
			sorter. Init (configuration);
			return sorter;
		}
	}

	/// <summary>
	/// Summary description for RecordSorter.
	/// </summary>
	public abstract class RecordSorter : IPersistAsXml, IStoresFdoCache, IAcceptsStringTable
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RecordSorter"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RecordSorter()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// <summary>
		/// Add to the specified XML node information required to create a new
		/// record sorter equivalent to yourself.
		/// The default is not to add any information.
		/// It may be assumed that the node already contains the assembly and class
		/// information required to create an instance of the sorter in a default state.
		/// An equivalent XmlNode will be passed to the InitXml method of the
		/// re-created sorter.
		/// This default implementation does nothing.
		/// </summary>
		/// <param name="node"></param>
		public virtual void PersistAsXml(XmlNode node)
		{
		}

		/// <summary>
		/// Initialize an instance into the state indicated by the node, which was
		/// created by a call to PersistAsXml.
		/// </summary>
		/// <param name="node"></param>
		public virtual void InitXml(XmlNode node)
		{
		}

		#region IStoresFdoCache
		/// <summary>
		/// Set an FdoCache for anything that needs to know.
		/// </summary>
		public virtual FdoCache Cache
		{
			set
			{
				// do nothing by default.
			}
		}
		#endregion IStoresFdoCache

		#region IAcceptsStringTable Members

		public virtual StringTable StringTable
		{
			set
			{
				// do nothing by default
			}
		}

		#endregion


		/// <summary>
		/// Method to retrieve the IComparer used by this sorter
		/// </summary>
		/// <returns>The IComparer</returns>
		protected internal abstract IComparer getComparer();
		/// <summary>
		/// Releases the IComparer that was given by getComparer().  Any neccesary cleanup work should be done here
		/// </summary>
		/// <param name="comp">The IComparer that was given by getComparer()</param>
		protected internal abstract void releaseComparer(IComparer comp);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the specified records.
		/// </summary>
		/// <param name="records">The records.</param>
		/// ------------------------------------------------------------------------------------
		public abstract void Sort(/*ref*/ ArrayList records);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the into.
		/// </summary>
		/// <param name="records">The records.</param>
		/// <param name="newRecords">The new records.</param>
		/// ------------------------------------------------------------------------------------
		public abstract void MergeInto(/*ref*/ ArrayList records, ArrayList newRecords);

		/// <summary>
		/// Merge the new records into the original list using the specified comparer
		/// </summary>
		/// <param name="records"></param>
		/// <param name="newRecords"></param>
		/// <param name="comparer"></param>
		protected void MergeInto(/*ref*/ ArrayList records, ArrayList newRecords, IComparer comparer)
		{
			for (int i = 0, j = 0; j < newRecords.Count; i++)
			{
				if (i >= records.Count || comparer.Compare(records[i], newRecords[j]) > 0)
				{
					// object at i is greater than current object to insert, or no more obejcts in records;
					// insert next object here.
					records.Insert(i, newRecords[j]);
					j++;
					// i gets incremented as usual past the inserted object. It will index the same
					// object next iteration as it did this one.
				}
			}
		}

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="collector"></param>
		public virtual void CollectItems(ICmObject obj, ArrayList collector)
		{
			collector.Add(new ManyOnePathSortItem(obj));
		}

		/// <summary>
		/// May be implemented to preload before sorting large collections (typically most of the instances).
		/// Currently will not be called if the column is also filtered; typically the same Preload() would end
		/// up being done.
		/// </summary>
		public virtual void Preload()
		{
		}

		/// <summary>
		/// Return true if the other sorter is 'compatible' with this, in the sense that
		/// either they produce the same sort sequence, or one derived from it (e.g., by reversing).
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public virtual bool CompatibleSorter(RecordSorter other)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		///
		/// ------------------------------------------------------------------------------------

		protected class FdoCompare : IComparer, IPersistAsXml, IFWDisposable
		{
			/// <summary></summary>
			protected string m_propertyName;
			/// <summary></summary>
			protected System.Collections.Hashtable m_values;
			/// <summary></summary>
			protected ILgCollatingEngine m_lce;
			/// <summary></summary>
			protected bool m_fUseKeys = false;
			/// <summary></summary>
			protected System.Collections.Hashtable m_values2;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:FdoCompare"/> class.
			/// </summary>
			/// <param name="propertyName">Name of the property.</param>
			/// --------------------------------------------------------------------------------
			public FdoCompare(string propertyName)
			{
				Init();
				m_propertyName= propertyName;
				m_fUseKeys = propertyName == "ShortName";
			}

			/// <summary>
			/// This constructor is intended to be used for persistence with IPersistAsXml
			/// </summary>
			public FdoCompare()
			{
				Init();
			}

			private void Init()
			{
				m_values = new System.Collections.Hashtable();
				m_values2 = new System.Collections.Hashtable();
				m_lce = null;
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
			~FdoCompare()
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
			protected virtual void Dispose(bool disposing)
			{
				//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
				// Must not be run more than once.
				if (m_isDisposed)
					return;

				if (disposing)
				{
					// Dispose managed resources here.
					if (m_values != null)
					{
						m_values.Clear();
					}
					if (m_values2 != null)
					{
						m_values2.Clear();
					}
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				CloseCollatingEngine();
				m_values = null;
				m_values2 = null;
				m_propertyName = null;
				m_lce = null;

				m_isDisposed = true;
			}

			#endregion IDisposable & Co. implementation

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the name of the property.
			/// </summary>
			/// <value>The name of the property.</value>
			/// --------------------------------------------------------------------------------
			public string PropertyName
			{
				get
				{
					CheckDisposed();
					return m_propertyName;
				}
			}
			/// <summary>
			/// Add to the specified XML node information required to create a new
			/// object equivalent to yourself. The node already contains information
			/// sufficient to create an instance of the proper class.
			/// </summary>
			/// <param name="node"></param>
			public void PersistAsXml(XmlNode node)
			{
				CheckDisposed();

				XmlUtils.AppendAttribute(node, "property", m_propertyName);
			}

			/// <summary>
			/// Initialize an instance into the state indicated by the node, which was
			/// created by a call to PersistAsXml.
			/// </summary>
			/// <param name="node"></param>
			public void InitXml(XmlNode node)
			{
				CheckDisposed();

				m_propertyName = XmlUtils.GetManditoryAttributeValue(node, "property");
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the property.
			/// </summary>
			/// <param name="target">The target.</param>
			/// <param name="property">The property.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			protected object GetProperty(ICmObject target, string property)
			{
				Type type = target.GetType();
				System.Reflection.PropertyInfo info = type.GetProperty(property,
					System.Reflection.BindingFlags.Instance |
					System.Reflection.BindingFlags.Public |
					System.Reflection.BindingFlags.FlattenHierarchy );
				if (info == null)
					throw new ArgumentException("There is no public property named '"
						+ property + "' in " + type.ToString()
						+ ". Remember, properties often end in a two-character suffix such as OA, OS, RA, or RS.");
				return info.GetValue(target,null);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Opens the collating engine.
			/// </summary>
			/// <param name="sWs">The s ws.</param>
			/// --------------------------------------------------------------------------------
			public void OpenCollatingEngine(string sWs)
			{
				CheckDisposed();

				if (m_lce == null)
				{
					m_lce = LgIcuCollatorClass.Create();
				}
				else
				{
					m_lce.Close();
				}
				m_lce.Open(sWs);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Closes the collating engine.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void CloseCollatingEngine()
			{
				CheckDisposed();

				if (m_lce != null)
				{
					m_lce.Close();
					m_lce = null;
				}
			}

			ICmObject GetObjFromItem(object x)
			{
				ManyOnePathSortItem itemX = x as ManyOnePathSortItem;
				// This is slightly clumsy but currently it's the only way we have to get a cache.
				return itemX.KeyCmObject;
			}

			// Compare two objects (expected to be ManyOnePathSortItems).
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
			/// </summary>
			/// <param name="x">The first object to compare.</param>
			/// <param name="y">The second object to compare.</param>
			/// <returns>
			/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
			/// </returns>
			/// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
			/// --------------------------------------------------------------------------------
			public int Compare(object x,object y)
			{
				CheckDisposed();

				try
				{
					if (x == y)
						return 0;
					if (x == null)
						return -1;
					if (y == null)
						return 1;

					// Enhance JohnT: this could be significantly optimized. In particular
					// we don't need to make an ICmObject of the key if we've already cached
					// the key for that HVO. But is this comparer used enough to be worth it?

					//	string property = "ShortName";
					ICmObject a = GetObjFromItem(x);
					ICmObject b = GetObjFromItem(y);

					if (m_fUseKeys)		// m_property == "ShortName"
					{
						byte[] ka = null;
						byte[] kb = null;
						if (m_values.Count == 0)
							OpenCollatingEngine(a.SortKeyWs);
						if (m_values.Contains(a.Hvo))
						{
							ka = (byte[])m_values[a.Hvo];
						}
						else
						{
							ka = (byte[])m_lce.get_SortKeyVariant(a.SortKey,
								LgCollatingOptions.fcoDefault);
							m_values.Add(a.Hvo, ka);
						}
						if (m_values.Contains(b.Hvo))
						{
							kb = (byte[])m_values[b.Hvo];
						}
						else
						{
							kb = (byte[])m_lce.get_SortKeyVariant(b.SortKey,
								LgCollatingOptions.fcoDefault);
							m_values.Add(b.Hvo, kb);
						}
						// This is what m_lce.CompareVariant(ka,kb,...) would do.
						// Simulate strcmp on the two NUL-terminated byte strings.
						// This avoids marshalling back and forth.
						// JohnT: but apparently the strings are not null-terminated if the input was empty.
						int nVal = 0;
						if (ka.Length == 0)
							nVal = -kb.Length; // zero if equal, neg if b is longer (considered larger)
						else if (kb.Length == 0)
							nVal = 1; // ka is longer and considered larger.
						else
						{
							// Normal case, null termination should be present.
							int ib;
							for (ib = 0; ka[ib] == kb[ib] && ka[ib] != 0; ++ib)
							{
								// skip merrily along until strings differ or end.
							}
							nVal = (int)(ka[ib] - kb[ib]);
						}
						if (nVal == 0)
						{
							// Need to get secondary sort keys.
							int na;
							if (m_values2.Contains(a.Hvo))
							{
								na = (int)m_values2[a.Hvo];
							}
							else
							{
								na = a.SortKey2;
								m_values2.Add(a.Hvo, na);
							}
							int nb;
							if (m_values2.Contains(b.Hvo))
							{
								nb = (int)m_values2[b.Hvo];
							}
							else
							{
								nb = b.SortKey2;
								m_values2.Add(b.Hvo, nb);
							}
							return na - nb;
						}
						else
						{
							return nVal;
						}
					}
					else // use default C# string comparisons
					{
						string sa = null;
						string sb = null;
						if (m_values.Contains(a.Hvo))
						{
							sa = (string)m_values[a.Hvo];
						}
						else
						{
							sa = (string)GetProperty(a, m_propertyName);
							m_values.Add(a.Hvo, sa);
						}
						if (m_values.Contains(b.Hvo))
						{
							sb = (string)m_values[b.Hvo];
						}
						else
						{
							sb = (string)GetProperty(b, m_propertyName);
							m_values.Add(b.Hvo, sb);
						}
						return sa.CompareTo(sb);
					}
				}
				catch (Exception)
				{
					throw;
				}
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="obj"></param>
			/// <returns></returns>
			public override bool Equals(object obj)
			{
				CheckDisposed();

				if (obj == null)
					return false;
				if (this.GetType() != obj.GetType())
					return false;
				FdoCompare that = (FdoCompare)obj;
				if (this.m_fUseKeys != that.m_fUseKeys)
					return false;
				if (this.m_isDisposed != that.m_isDisposed)
					return false;
				if (this.m_lce != that.m_lce)
					return false;
				if (this.m_propertyName != that.m_propertyName)
					return false;
				if (m_values == null)
				{
					if (that.m_values != null)
						return false;
				}
				else
				{
					if (that.m_values == null)
						return false;
					if (this.m_values.Count != that.m_values.Count)
						return false;
					IDictionaryEnumerator ie = that.m_values.GetEnumerator();
					while (ie.MoveNext())
					{
						if (!m_values.ContainsKey(ie.Key) || m_values[ie.Key] != ie.Value)
							return false;
					}
				}
				if (m_values2 == null)
				{
					if (that.m_values2 != null)
						return false;
				}
				else
				{
					if (that.m_values2 == null)
						return false;
					if (this.m_values2.Count != that.m_values2.Count)
						return false;
					IDictionaryEnumerator ie = that.m_values2.GetEnumerator();
					while (ie.MoveNext())
					{
						if (!m_values2.ContainsKey(ie.Key) || m_values2[ie.Key] != ie.Value)
							return false;
					}
				}
				return true;
			}

			/// <summary>
			///
			/// </summary>
			/// <returns></returns>
			public override int GetHashCode()
			{
				CheckDisposed();

				int hash = GetType().GetHashCode();
				if (m_fUseKeys)
					hash *= 3;
				if (m_isDisposed)
					hash += 1;
				if (m_lce != null)
					hash += m_lce.GetHashCode();
				if (m_propertyName != null)
					hash *= m_propertyName.GetHashCode();
				if (m_values != null)
					hash += m_values.Count * 17;
				if (m_values2 != null)
					hash += m_values2.Count * 53;
				return hash;
			}
		}
	}

	public class AndSorter : RecordSorter
	{
		ArrayList m_sorters = new ArrayList();

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a sorter
		/// </summary>
		/// <param name="sorter">The sorter</param>
		/// ------------------------------------------------------------------------------------------
		public void Add(RecordSorter sorter)
		{
			m_sorters.Add(sorter);
		}

		public AndSorter() { }

		public AndSorter(ArrayList sorters) : base()
		{
			foreach (RecordSorter rs in sorters)
				Add(rs);
		}

		public ArrayList Sorters
		{
			get { return m_sorters; }
		}

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="collector"></param>
		public override void CollectItems(ICmObject obj, ArrayList collector)
		{
			if (m_sorters.Count > 0)
				(m_sorters[0] as RecordSorter).CollectItems(obj, collector);
			else
				base.CollectItems(obj, collector);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml (node);
			foreach(RecordSorter rs in m_sorters)
				DynamicLoader.PersistObject(rs, node, "sorter");
		}

		// This will probably never be used, but for the sake of completeness, here it is
		protected internal override IComparer getComparer()
		{
			IComparer comp = new AndSorterComparer(m_sorters);
			return comp;
		}
		protected internal override void releaseComparer(IComparer comp)
		{
			if (comp is AndSorterComparer) // It really should be
				(comp as AndSorterComparer).Dispose();
			comp = null;
		}

		private class AndSorterComparer : IComparer, IFWDisposable
		{
			ArrayList m_sorters;
			ArrayList m_comps;

			public AndSorterComparer(ArrayList sorters) : base()
			{
				m_sorters = sorters;
				m_comps = new ArrayList();
				foreach (RecordSorter rs in m_sorters)
				{
					IComparer comp = rs.getComparer();
					if (comp is StringFinderCompare)
						(comp as StringFinderCompare).Init();
					m_comps.Add(comp);
				}
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
				~AndSorterComparer()
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
			protected virtual void Dispose(bool disposing)
			{
				//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
				// Must not be run more than once.
				if (m_isDisposed)
					return;

				if (disposing)
				{
					// Dispose managed resources here.
					for (int i = 0; i < m_sorters.Count; i++)
					{
						if (m_comps[i] is StringFinderCompare)
							(m_comps[i] as StringFinderCompare).Cleanup();

						((RecordSorter)m_sorters[i]).releaseComparer((IComparer)m_comps[i]);
					}
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_comps = null;
				m_sorters = null;

				m_isDisposed = true;
			}

			#endregion IDisposable & Co. implementation

			#region IComparer Members

			public int Compare(object x, object y)
			{
				CheckDisposed();

				int ret = 0;
				for(int i = 0; i < m_sorters.Count; i++)
				{
					ret = ((IComparer)m_comps[i]).Compare(x, y);
					if (ret != 0)
						break;
				}

				return ret;
			}

			#endregion

			/// <summary>
			///
			/// </summary>
			/// <param name="obj"></param>
			/// <returns></returns>
			public override bool Equals(object obj)
			{
				CheckDisposed();

				if (obj == null)
					return false;
				if (this.GetType() != obj.GetType())
					return false;
				AndSorterComparer that = (AndSorterComparer)obj;
				if (m_comps == null)
				{
					if (that.m_comps != null)
						return false;
				}
				else
				{
					if (that.m_comps == null)
						return false;
					if (this.m_comps.Count != that.m_comps.Count)
						return false;
					for (int i = 0; i < m_comps.Count; ++i)
					{
						if (this.m_comps[i] != that.m_comps[i])
							return false;
					}
				}
				if (m_sorters == null)
				{
					if (that.m_sorters != null)
						return false;
				}
				else
				{
					if (that.m_sorters == null)
						return false;
					if (this.m_sorters.Count != that.m_sorters.Count)
						return false;
					for (int i = 0; i < m_sorters.Count; ++i)
					{
						if (this.m_sorters[i] != that.m_sorters[i])
							return false;
					}
				}
				return true;
			}

			/// <summary>
			///
			/// </summary>
			/// <returns></returns>
			public override int GetHashCode()
			{
				CheckDisposed();

				int hash = GetType().GetHashCode();
				if (m_comps != null)
					hash += m_comps.Count * 3;
				if (m_sorters != null)
					hash += m_sorters.Count * 17;
				return hash;
			}
		}

		public override void Sort(/*ref*/ ArrayList records)
		{
#if DEBUG
			DateTime dt1 = DateTime.Now;
			int tc1 = Environment.TickCount;
#endif

			using (AndSorterComparer comp = new AndSorterComparer(m_sorters))
			{
				MergeSort.Sort(ref records, (IComparer) comp);
			}

#if DEBUG
			// only do this if the timing switch is info or verbose
			if (RuntimeSwitches.RecordTimingSwitch.TraceInfo)
			{
			int tc2 = Environment.TickCount;
			TimeSpan ts1 = DateTime.Now - dt1;
			string s = "AndSorter:  Sorting " + records.Count + " records took " +
				(tc2 - tc1) + " ticks," + " or " + ts1.Minutes + ":" + ts1.Seconds + "." +
				ts1.Milliseconds.ToString("d3") + " min:sec.";
				Debug.WriteLine(s, RuntimeSwitches.RecordTimingSwitch.DisplayName);
			}
#endif
		}

		public override void MergeInto(ArrayList records, ArrayList newRecords)
		{
			using (AndSorterComparer comp = new AndSorterComparer(m_sorters))
				base.MergeInto(records, newRecords, comp);
		}

		public override bool CompatibleSorter(RecordSorter other)
		{
			foreach(RecordSorter rs in m_sorters)
				if(rs.CompatibleSorter(other))
					return true;

			return false;
		}

		public int CompatibleSorterIndex(RecordSorter other)
		{
			for (int i = 0; i < m_sorters.Count; i++)
				if ((m_sorters[i] as RecordSorter).CompatibleSorter(other))
					return i;

			return -1;
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml (node);
			m_sorters = new ArrayList(node.ChildNodes.Count);
			foreach (XmlNode child in node.ChildNodes)
				m_sorters.Add(DynamicLoader.RestoreFromChild(child, "."));
		}

		public override FdoCache Cache
		{
			set
			{
				foreach (RecordSorter rs in m_sorters)
					rs.Cache = value;
			}
		}
	}

	/// <summary>
	/// A very general record sorter class, based on an arbitrary implementation of IComparer
	/// that can compare two FDO objects.
	/// </summary>
	public class GenRecordSorter : RecordSorter, IFWDisposable
	{
		IComparer m_comp;

		protected internal override IComparer getComparer()
		{
			CheckDisposed();

			return m_comp;
		}
		protected internal override void releaseComparer(IComparer comp)
		{
			CheckDisposed();

			// Don't need to do anything in this case
		}

		/// <summary>
		/// See whether the comparer can preload. Currently we only know about one kind that can.
		/// </summary>
		public override void Preload()
		{
			base.Preload();
			if (m_comp is StringFinderCompare)
				(m_comp as StringFinderCompare).Preload();
		}

		/// <summary>
		/// Normal constructor.
		/// </summary>
		/// <param name="comp"></param>
		public GenRecordSorter(IComparer comp)
		{
			m_comp = comp;
		}

		/// <summary>
		/// Default constructor for IPersistAsXml
		/// </summary>
		public GenRecordSorter()
		{
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
		~GenRecordSorter()
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
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				//if (m_comp != null && m_comp is IDisposable)
				//	(m_comp as IDisposable).Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_comp = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the other sorter is 'compatible' with this, in the sense that
		/// either they produce the same sort sequence, or one derived from it (e.g., by reversing).
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool CompatibleSorter(RecordSorter other)
		{
			CheckDisposed();

			GenRecordSorter grsOther = other as GenRecordSorter;
			if (grsOther == null)
				return false;
			if (CompatibleComparers(m_comp, grsOther.m_comp))
				return true;
			// Currently the only other kind of compatibility we know how to detect
			// is StringFinderCompares that do more-or-less the same thing.
			StringFinderCompare sfcThis = m_comp as StringFinderCompare;
			StringFinderCompare sfcOther = grsOther.Comparer as StringFinderCompare;
			if (sfcThis == null || sfcOther == null)
				return false;
			if (!sfcThis.Finder.SameFinder(sfcOther.Finder))
				return false;
			// We deliberately don't care if one has a ReverseCompare and the other
			// doesn't. That's handled by a different icon.
			IComparer subCompOther = UnpackReverseCompare(sfcOther);
			IComparer subCompThis = UnpackReverseCompare(sfcThis);
			return CompatibleComparers(subCompThis, subCompOther);
		}

		private IComparer UnpackReverseCompare(StringFinderCompare sfc)
		{
			IComparer subComp = sfc.SubComparer;
			if (subComp is ReverseComparer)
				subComp = (subComp as ReverseComparer).SubComp;
			return subComp;
		}

		/// <summary>
		/// Return true if the two comparers will give the same result. Ideally this would
		/// be an interface method on ICompare, but that interface is defined by .NET so we
		/// can't enhance it. This knows about a few interesting cases.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		bool CompatibleComparers(IComparer first, IComparer second)
		{
			// identity
			if (first == second)
				return true;

			// IcuComparers on same Ws?
			IcuComparer firstIcu = first as IcuComparer;
			IcuComparer secondIcu = second as IcuComparer;
			if (firstIcu != null && secondIcu != null && firstIcu.WsCode == secondIcu.WsCode)
				return true;

			// Both IntStringComparers?
			if (first is IntStringComparer && second is IntStringComparer)
				return true;

			// FdoComparers on the same property?
			FdoCompare firstFdo = first as FdoCompare;
			FdoCompare secondFdo = second as FdoCompare;
			if (firstFdo != null && secondFdo != null && firstFdo.PropertyName == secondFdo.PropertyName)
				return true;

			// not the same any way we know about
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the comparer.
		/// </summary>
		/// <value>The comparer.</value>
		/// ------------------------------------------------------------------------------------
		public IComparer Comparer
		{
			get
			{
				CheckDisposed();
				return m_comp;
			}
			set
			{
				CheckDisposed();
				m_comp = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add to the specified XML node information required to create a new
		/// record sorter equivalent to yourself.
		/// The default is not to add any information.
		/// It may be assumed that the node already contains the assembly and class
		/// information required to create an instance of the sorter in a default state.
		/// An equivalent XmlNode will be passed to the InitXml method of the
		/// re-created sorter.
		/// This default implementation does nothing.
		/// </summary>
		/// <param name="node"></param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			CheckDisposed();

			base.PersistAsXml (node); // does nothing, but in case needed later...
			IPersistAsXml persistComparer = m_comp as IPersistAsXml;
			if (persistComparer == null)
				throw new Exception("cannot persist GenRecSorter with comparer class " + m_comp.GetType().AssemblyQualifiedName);
			DynamicLoader.PersistObject(persistComparer, node, "comparer");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize an instance into the state indicated by the node, which was
		/// created by a call to PersistAsXml.
		/// </summary>
		/// <param name="node"></param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			CheckDisposed();

			base.InitXml (node);
			XmlNode compNode = node.ChildNodes[0];
			if (compNode.Name != "comparer")
				throw new Exception("persist info for GenRecordSorter must have comparer child element");
			m_comp = DynamicLoader.RestoreObject(compNode) as IComparer;
			if (m_comp == null)
				throw new Exception("restoring sorter failed...comparer does not implement IComparer");
		}

		/// <summary>
		/// Set an FdoCache for anything that needs to know.
		/// </summary>
		public override FdoCache Cache
		{
			set
			{
				CheckDisposed();

				if (m_comp is IStoresFdoCache)
					(m_comp as IStoresFdoCache).Cache = value;
			}
		}

		public override StringTable StringTable
		{
			set
			{
				CheckDisposed();

				if (m_comp is IAcceptsStringTable)
					(m_comp as IAcceptsStringTable).StringTable = value;
			}
		}

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="collector"></param>
		public override void CollectItems(ICmObject obj, ArrayList collector)
		{
			CheckDisposed();

			if (m_comp is StringFinderCompare)
			{
				(m_comp as StringFinderCompare).CollectItems(obj, collector);
			}
			else
				base.CollectItems(obj, collector);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the specified records.
		/// </summary>
		/// <param name="records">The records.</param>
		/// ------------------------------------------------------------------------------------
		public override void Sort(/*ref*/ ArrayList records)
		{
			CheckDisposed();

#if DEBUG
			DateTime dt1 = DateTime.Now;
			int tc1 = Environment.TickCount;
#endif
			if (m_comp is StringFinderCompare)
				(m_comp as StringFinderCompare).Init();

			//records.Sort(m_comp);
			MergeSort.Sort(ref records, m_comp);

			if (m_comp is StringFinderCompare)
				(m_comp as StringFinderCompare).Cleanup();
#if DEBUG
			// only do this if the timing switch is info or verbose
			if (RuntimeSwitches.RecordTimingSwitch.TraceInfo)
			{
			int tc2 = Environment.TickCount;
			TimeSpan ts1 = DateTime.Now - dt1;
			string s = "GenRecordSorter:  Sorting " + records.Count + " records took " +
				(tc2 - tc1) + " ticks," + " or " + ts1.Minutes + ":" + ts1.Seconds + "." +
				ts1.Milliseconds.ToString("d3") + " min:sec.";
				Debug.WriteLine(s, RuntimeSwitches.RecordTimingSwitch.DisplayName);
			}
#endif
		}
		/// <summary>
		/// Required implementation.
		/// </summary>
		/// <param name="records"></param>
		/// <param name="newRecords"></param>
		public override void MergeInto(/*ref*/ ArrayList records, ArrayList newRecords)
		{
			CheckDisposed();

			if (m_comp is StringFinderCompare)
				(m_comp as StringFinderCompare).Init();

			//records.Sort(m_comp);
			MergeInto(records, newRecords, m_comp);

			if (m_comp is StringFinderCompare)
				(m_comp as StringFinderCompare).Cleanup();
		}

		/// <summary>
		/// Check whether this GenRecordSorter is equal to another object.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			CheckDisposed();

			if (obj == null)
				return false;
			if (this.GetType() != obj.GetType())
				return false;
			GenRecordSorter that = (GenRecordSorter)obj;
			if (this.m_isDisposed != that.m_isDisposed)
				return false;
			if (m_comp == null)
			{
				if (that.m_comp != null)
					return false;
			}
			else
			{
				if (that.m_comp == null)
					return false;
				if (this.m_comp.GetType() != that.m_comp.GetType())
					return false;
				else
					return this.m_comp.Equals(that.m_comp);
			}
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			CheckDisposed();

			int hash = GetType().GetHashCode();
			if (m_comp != null)
				hash *= m_comp.GetHashCode();
			if (m_isDisposed)
				hash += 1;
			return hash;
		}
	}

	/// <summary>
	/// This class compares two ManyOnePathSortItems by making use of the ability of a StringFinder
	/// object to obtain strings from the KeyObject hvo, then
	/// a (simpler) IComparer to compare the strings.
	/// </summary>
	public class StringFinderCompare : IComparer, IPersistAsXml, IStoresFdoCache, IAcceptsStringTable, IFWDisposable
	{
		/// <summary></summary>
		protected IComparer m_subComp;
		/// <summary></summary>
		protected IStringFinder m_finder;
		/// <summary></summary>
		protected bool m_fSortedFromEnd;
		/// <summary></summary>
		protected bool m_fSortedByLength;
		// This is used, during a single sort, to cache keys.
		/// <summary></summary>
		protected Hashtable m_objToKey = new Hashtable();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:StringFinderCompare"/> class.
		/// </summary>
		/// <param name="finder">The finder.</param>
		/// <param name="subComp">The sub comp.</param>
		/// ------------------------------------------------------------------------------------
		public StringFinderCompare(IStringFinder finder, IComparer subComp)
		{
			m_finder = finder;
			m_subComp = subComp;
			m_fSortedFromEnd = false;
			m_fSortedByLength = false;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		public StringFinderCompare()
		{
			m_fSortedFromEnd = false;
			m_fSortedByLength = false;
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
		~StringFinderCompare()
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
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				//if (m_subComp != null && m_subComp is IDisposable)
				//	(m_subComp as IDisposable).Dispose();
				//if (m_finder is IDisposable)
				//	(m_finder as IDisposable).Dispose();
				if (m_objToKey != null)
					m_objToKey.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_subComp = null;
			m_finder = null;
			m_objToKey = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the finder.
		/// </summary>
		/// <value>The finder.</value>
		/// ------------------------------------------------------------------------------------
		public IStringFinder Finder
		{
			get
			{
				CheckDisposed();
				return m_finder;
			}
		}

		/// <summary>
		/// May be implemented to preload before sorting large collections (typically most of the instances).
		/// Currently will not be called if the column is also filtered; typically the same Preload() would end
		/// up being done.
		/// </summary>
		public virtual void Preload()
		{
			m_finder.Preload();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sub comparer.
		/// </summary>
		/// <value>The sub comparer.</value>
		/// ------------------------------------------------------------------------------------
		public IComparer SubComparer
		{
			get
			{
				CheckDisposed();
				return m_subComp;
			}
		}

		/// <summary>
		/// Copy our comparer's SubComparer and SortedFromEnd to another comparer.
		/// </summary>
		/// <param name="copyComparer"></param>
		public void CopyTo(StringFinderCompare copyComparer)
		{
			CheckDisposed();

			copyComparer.m_subComp = this.m_subComp;
			copyComparer.m_fSortedFromEnd = this.m_fSortedFromEnd;
			copyComparer.m_fSortedByLength = this.m_fSortedByLength;
		}

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="collector"></param>
		public void CollectItems(ICmObject obj, ArrayList collector)
		{
			CheckDisposed();

			m_finder.CollectItems(obj, collector);
		}

		/// <summary>
		/// Give the sort order. Considered to be ascending unless it has been reversed.
		/// </summary>
		public SortOrder Order
		{
			get
			{
				CheckDisposed();

				return m_subComp is ReverseComparer ?
					SortOrder.Descending : SortOrder.Ascending;
			}
		}

		/// <summary>
		/// Flag whether to sort normally from the beginnings of words, or to sort from the
		/// ends of words.  This is useful for grouping words by suffix.
		/// </summary>
		public bool SortedFromEnd
		{
			get
			{
				CheckDisposed();
				return m_fSortedFromEnd;
			}
			set
			{
				CheckDisposed();
				m_fSortedFromEnd = value;
			}
		}

		/// <summary>
		/// Flag whether to sort normally from the beginnings of words, or to sort from the
		/// ends of words.  This is useful for grouping words by suffix.
		/// </summary>
		public bool SortedByLength
		{
			get
			{
				CheckDisposed();
				return m_fSortedByLength;
			}
			set
			{
				CheckDisposed();

				m_fSortedByLength = value;
				if (m_fSortedByLength)
					Icu.InitIcuDataDir();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Init()
		{
			CheckDisposed();

			m_objToKey.Clear();
			if (m_subComp is IcuComparer)
			{
				(m_subComp as IcuComparer).OpenCollatingEngine();
			}
			else if (m_subComp is ReverseComparer)
			{
				IComparer ct = ReverseComparer.Reverse(m_subComp);
				if (ct is IcuComparer)
					(ct as IcuComparer).OpenCollatingEngine();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleanups this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Cleanup()
		{
			CheckDisposed();

			m_objToKey.Clear(); // redundant, but may help free memory.
			if (m_subComp is IcuComparer)
			{
				(m_subComp as IcuComparer).CloseCollatingEngine();
			}
			else if (m_subComp is ReverseComparer)
			{
				IComparer ct = ReverseComparer.Reverse(m_subComp);
				if (ct is IcuComparer)
					(ct as IcuComparer).CloseCollatingEngine();
			}
		}

		/// <summary>
		/// Reverse the order of the sort.
		/// </summary>
		public void Reverse()
		{
			CheckDisposed();

			// Dangerous, since m_subComp may be replaced,
			// and may need to be disposed.
			// m_subComp = ReverseComparer.Reverse(m_subComp);
			IComparer reversed = ReverseComparer.Reverse(m_subComp);
			//if (m_subComp != null
			//	&& m_subComp is IDisposable
			//	&& m_subComp != reversed
			//	&& m_subComp != (reversed as ReverseComparer).SubComp)
			//{
			//	(m_subComp as IDisposable).Dispose();
			//}
			m_subComp = reversed;
		}

		string[] GetValue(object key, bool sortedFromEnd)
		{
			try
			{
				string[] result = m_objToKey[key] as string[];
				if (result != null)
					return result;
				ManyOnePathSortItem item = key as ManyOnePathSortItem;

				// This may help with solving LT-2205, but is probably too expensive to run always.
				// if (!item.KeyCmObjectUsing(item.RootObject.Cache).IsValidObject())
				//	throw new Exception("Sorter found ManyOnePathSortItem with invalid key object with HVO " + item.KeyObject);

				result =  m_finder.SortStrings(item, sortedFromEnd);
				m_objToKey[key] = result;
				return result;
			}
			catch (Exception e)
			{
				throw new Exception("StringFinderCompare could not get key for " + key.ToString(), e);
			}
		}

		#region IComparer Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal
		/// to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero
		/// x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the
		/// <see cref="T:System.IComparable"></see> interface.-or- x and y are of different
		/// types and neither one can handle comparisons with the other. </exception>
		/// ------------------------------------------------------------------------------------
		public int Compare(object x,object y)
		{
			CheckDisposed();

			try
			{
				if (x == y)
					return 0;
				if (x == null)
					return m_subComp is ReverseComparer ? 1 : -1;
				if (y == null)
					return m_subComp is ReverseComparer ? -1 : 1;

				// We pass GetValue m_fSortedFromEnd, and it is responsible for taking care of flipping the string if that's needed.
				// The reason to do it there is because sometimes a sort key will have a homograph number appeneded to the end.
				// If we do the flipping here, the resulting string will be a backwards homograph number followed by the backwards
				// sort key itself.  GetValue should know enough to return the flipped string followed by a regular homograph number.
				string[] keysA = GetValue(x, m_fSortedFromEnd);
				string[] keysB = GetValue(y, m_fSortedFromEnd);

				// There will usually only be one element in the array, but just in case...
				if (m_fSortedFromEnd)
				{
					Array.Reverse(keysA);
					Array.Reverse(keysB);
				}

				int cstrings = Math.Min(keysA.Length, keysB.Length);
				for (int i = 0; i < cstrings; i++)
				{
					// Sorted by length (if enabled) will be the primary sorting factor
					if (m_fSortedByLength)
					{
						int cchA = OrthographicLength(keysA[i]);
						int cchB = OrthographicLength(keysB[i]);
						if (cchA < cchB)
							return m_subComp is ReverseComparer ? 1 : -1;
						else if (cchB < cchA)
							return m_subComp is ReverseComparer ? -1 : 1;
					}
					// However, if there's no difference in length, we continue with the
					// rest of the sort
					int result = m_subComp.Compare(keysA[i], keysB[i]);
					if (result != 0)
						return result;
				}
				// All corresponding strings are equal according to the comparer, so sort based on the number of strings
				if (keysA.Length < keysB.Length)
					return m_subComp is ReverseComparer ? 1 : -1;
				else if (keysA.Length > keysB.Length)
					return m_subComp is ReverseComparer ? -1 : 1;
				else return 0;
			}
			catch (Exception error)
			{
				throw new Exception("Comparing objects failed", error);
			}
		}

		/// <summary>
		/// Count the number of orthographic characters (word-forming, nondiacritic) in the
		/// key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		private int OrthographicLength(string key)
		{
			int cchOrtho = 0;
			char[] rgch = key.ToCharArray();
			for (int i = 0; i < rgch.Length; ++i)
			{
				// Handle surrogate pairs carefully!
				int ch;
				char ch1 = rgch[i];
				if (Surrogates.IsLeadSurrogate(ch1))
				{
					char ch2 = rgch[++i];
					ch = Surrogates.Int32FromSurrogates(ch1, ch2);
				}
				else
				{
					ch = (int)ch1;
				}
				if (Icu.IsAlphabetic(ch))
				{
					++cchOrtho;		// Seems not to include UCHAR_DIACRITIC.
				}
				else
				{
					if (Icu.IsIdeographic(ch))
						++cchOrtho;
				}
			}
			return cchOrtho;
		}
		#endregion

		#region IPersistAsXml Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public void PersistAsXml(XmlNode node)
		{
			CheckDisposed();

			DynamicLoader.PersistObject(m_finder, node, "finder");
			DynamicLoader.PersistObject(m_subComp, node, "comparer");
			if (m_fSortedFromEnd)
				XmlUtils.AppendAttribute(node, "sortFromEnd", "true");
			if (m_fSortedByLength)
				XmlUtils.AppendAttribute(node, "sortByLength", "true");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public void InitXml(XmlNode node)
		{
			CheckDisposed();

			m_finder = DynamicLoader.RestoreFromChild(node, "finder") as IStringFinder;
			m_subComp = DynamicLoader.RestoreFromChild(node, "comparer") as IComparer;
			m_fSortedFromEnd = XmlUtils.GetOptionalBooleanAttributeValue(node, "sortFromEnd", false);
			m_fSortedByLength = XmlUtils.GetOptionalBooleanAttributeValue(node, "sortByLength", false);
			if (m_fSortedByLength)
				Icu.InitIcuDataDir();
		}

		#endregion

		#region IStoresFdoCache members

		/// <summary>
		/// Given a cache, see whether your finder wants to know about it.
		/// </summary>
		public FdoCache Cache
		{
			set
			{
				CheckDisposed();

				if (m_finder is IStoresFdoCache)
					(m_finder as IStoresFdoCache).Cache = value;
			}
		}
		#endregion

		#region IAcceptsStringTable

		public StringTable StringTable
		{
			set
			{
				CheckDisposed();
				if (m_finder is IAcceptsStringTable)
					(m_finder as IAcceptsStringTable).StringTable = value;
			}
		}

		#endregion IAcceptsStringTable

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			CheckDisposed();

			if (obj == null)
				return false;
			if (this.GetType() != obj.GetType())
				return false;
			StringFinderCompare that = (StringFinderCompare)obj;
			if (m_finder == null)
			{
				if (that.m_finder != null)
					return false;
			}
			else
			{
				if (that.m_finder == null)
					return false;
				if (!this.m_finder.SameFinder(that.m_finder))
					return false;
			}
			if (this.m_fSortedByLength != that.m_fSortedByLength)
				return false;
			if (this.m_fSortedFromEnd != that.m_fSortedFromEnd)
				return false;
			if (this.m_isDisposed != that.m_isDisposed)
				return false;
			if (m_objToKey == null)
			{
				if (that.m_objToKey != null)
					return false;
			}
			else
			{
				if (that.m_objToKey == null)
					return false;
				if (this.m_objToKey.Count != that.m_objToKey.Count)
					return false;
				IDictionaryEnumerator ie = that.m_objToKey.GetEnumerator();
				while (ie.MoveNext())
				{
					if (!m_objToKey.ContainsKey(ie.Key) || m_objToKey[ie.Key] != ie.Value)
						return false;
				}
			}
			if (m_subComp == null)
				return that.m_subComp == null;
			else
				return this.m_subComp.Equals(that.m_subComp);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			CheckDisposed();

			int hash = GetType().GetHashCode();
			if (m_finder != null)
				hash += m_finder.GetHashCode();
			if (m_fSortedByLength)
				hash *= 3;
			if (m_fSortedFromEnd)
				hash *= 17;
			if (m_isDisposed)
				hash += 1;
			if (m_objToKey != null)
				hash += m_objToKey.Count * 53;
			if (m_subComp != null)
				hash += m_subComp.GetHashCode();
			return hash;
		}
	}

	/// <summary>
	/// This class reverses the polarity of another IComparer.
	/// Note especially the Reverse(IComparer) static function, which creates
	/// a ReverseComparer if necessary, but can also unwrap an existing one to retrieve
	/// the original comparer.
	/// </summary>
	public class ReverseComparer : IComparer, IPersistAsXml, IFWDisposable
	{
		IComparer m_comp;

		/// <summary>
		/// normal constructor
		/// </summary>
		/// <param name="comp"></param>
		public ReverseComparer(IComparer comp)
		{
			m_comp = comp;
		}

		/// <summary>
		/// default for persistence
		/// </summary>
		public ReverseComparer()
		{
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
		~ReverseComparer()
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
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				//if (m_comp != null && m_comp is IDisposable)
				//	(m_comp as IDisposable).Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_comp = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IComparer Members

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
		/// ------------------------------------------------------------------------------------------
		public int Compare(object x, object y)
		{
			CheckDisposed();

			return -m_comp.Compare(x, y);
		}

		#endregion

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sub comp.
		/// </summary>
		/// <value>The sub comp.</value>
		/// ------------------------------------------------------------------------------------------
		public IComparer SubComp
		{
			get
			{
				CheckDisposed();
				return m_comp;
			}
		}

		/// <summary>
		/// Return a comparer with the opposite sense of comp. If it is itself a ReverseComparer,
		/// achieve this by unwrapping and returning the original comparer; otherwise, create
		/// a ReverseComparer.
		/// </summary>
		/// <param name="comp"></param>
		/// <returns></returns>
		public static IComparer Reverse(IComparer comp)
		{
			ReverseComparer rc = comp as ReverseComparer;
			if (rc == null)
				return new ReverseComparer(comp);
			else
				return rc.SubComp;
		}
		#region IPersistAsXml Members

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public void PersistAsXml(XmlNode node)
		{
			CheckDisposed();

			DynamicLoader.PersistObject(m_comp, node, "comparer");
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public void InitXml(XmlNode node)
		{
			CheckDisposed();

			m_comp = DynamicLoader.RestoreFromChild(node, "comparer") as IComparer;
		}

		#endregion

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			CheckDisposed();

			if (obj == null)
				return false;
			if (this.GetType() != obj.GetType())
				return false;
			ReverseComparer that = (ReverseComparer)obj;
			if (this.m_isDisposed != that.m_isDisposed)
				return false;
			if (m_comp == null)
			{
				return that.m_comp == null;
			}
			else
			{
				if (that.m_comp == null)
					return false;
				else
					return this.m_comp.Equals(that.m_comp);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			CheckDisposed();

			int hash = GetType().GetHashCode();
			if (m_comp != null)
				hash *= m_comp.GetHashCode();
			if (m_isDisposed)
				hash += 1;
			return hash;
		}
	}

	/// <summary>
	/// This class compares two integers represented as strings using integer comparison.
	/// </summary>
	public class IntStringComparer : IComparer, IPersistAsXml
	{
		#region IComparer Members

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
		/// ------------------------------------------------------------------------------------------
		public int Compare(object x, object y)
		{
			int xn = Int32.Parse(x.ToString());
			int yn = Int32.Parse(y.ToString());
			if (xn < yn)
				return -1;
			else if (xn > yn)
				return 1;
			else
				return 0;
		}

		#endregion

		#region IPersistAsXml Members

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public void PersistAsXml(XmlNode node)
		{
			// nothing to do.
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public void InitXml(XmlNode node)
		{
			// Nothing to do
		}

		#endregion

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return this.GetType() == obj.GetType();
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return GetType().GetHashCode();
		}
	}
	/// -------------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	///
	/// -------------------------------------------------------------------------------------------

	public class IcuComparer : IComparer, IPersistAsXml, IFWDisposable
	{
		/// <summary></summary>
		protected ILgCollatingEngine m_lce = null;
		/// <summary></summary>
		protected string m_sWs;
		// Key for the Hashtable is a string.
		// Value is a byte[].
		/// <summary></summary>
		protected Hashtable m_htskey = new Hashtable();

		/// <summary>
		/// Made accessible for testing.
		/// </summary>
		public string WsCode
		{
			get { return m_sWs; }
		}

		#region Constructors, etc.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IcuComparer(string sWs)
		{
			m_sWs = sWs;
		}

		/// <summary>
		/// Default constructor for use with IPersistAsXml
		/// </summary>
		public IcuComparer()
		{
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Opens the collating engine.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		public void OpenCollatingEngine()
		{
			CheckDisposed();

			if (m_lce == null)
			{
				m_lce = LgIcuCollatorClass.Create();
			}
			else
			{
				m_lce.Close();
			}
			// Ensure that ICU has been initialzed before we dump anything.  This should
			// help fix LT-3970.
			Icu.InitIcuDataDir();
			m_lce.Open(m_sWs);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the collating engine.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		public void CloseCollatingEngine()
		{
			CheckDisposed();

			if (m_lce != null)
			{
				m_lce.Close();
				//Marshal.ReleaseComObject(m_lce);
				m_lce = null;
			}
			if (m_htskey != null)
				m_htskey.Clear();
		}
		#endregion

		#region IComparer Members
		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
		/// ------------------------------------------------------------------------------------------
		public int Compare(object x, object y)
		{
			CheckDisposed();

			string a = x as string;
			string b = y as string;
			if (a == b)
				return 0;
			if (a == null)
				return 1;
			if (b == null)
				return -1;
			byte[] ka = null;
			byte[] kb = null;
			if (m_lce != null)
			{
				object kaObj = m_htskey[a];
				if (kaObj != null)
				{
					ka = (byte[])kaObj;
				}
				else
				{
					ka = (byte[])m_lce.get_SortKeyVariant(a,
						LgCollatingOptions.fcoDefault);
					m_htskey.Add(a, ka);
				}
				object kbObj = m_htskey[b];
				if (kbObj != null)
				{
					kb = (byte[])kbObj;
				}
				else
				{
					kb = (byte[])m_lce.get_SortKeyVariant(b,
						LgCollatingOptions.fcoDefault);
					m_htskey.Add(b, kb);
				}
			}
			else
			{
				OpenCollatingEngine();
				ka = (byte[])m_lce.get_SortKeyVariant(a,
					LgCollatingOptions.fcoDefault);
				kb = (byte[])m_lce.get_SortKeyVariant(b,
					LgCollatingOptions.fcoDefault);
				CloseCollatingEngine();
			}
			// This is what m_lce.CompareVariant(ka,kb,...) would do.
			// Simulate strcmp on the two NUL-terminated byte strings.
			// This avoids marshalling back and forth.
			int nVal = 0;
			if (ka.Length == 0)
				nVal = -kb.Length; // zero if equal, neg if b is longer (considered larger)
			else if (kb.Length == 0)
				nVal = 1; // ka is longer and considered larger.
			else
			{
				// Normal case, null termination should be present.
				int ib;
				for (ib = 0; ka[ib] == kb[ib] && ka[ib] != 0; ++ib)
				{
					// skip merrily along until strings differ or end.
				}
				nVal = (int)(ka[ib] - kb[ib]);
			}
			return nVal;
		}
		#endregion

		#region IPersistAsXml Members

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public void PersistAsXml(XmlNode node)
		{
			CheckDisposed();

			XmlUtils.AppendAttribute(node, "ws", m_sWs);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public void InitXml(XmlNode node)
		{
			CheckDisposed();

			m_sWs = XmlUtils.GetManditoryAttributeValue(node, "ws");
		}

		#endregion

		#region IDisposable & Co. implementation
		// Region last reviewed: RandyR, Oct 10, 2005.

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
		~IcuComparer()
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
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			CloseCollatingEngine();
			m_htskey = null;
			m_lce = null;
			m_sWs = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			CheckDisposed();

			if (obj == null)
				return false;
			if (this.GetType() != obj.GetType())
				return false;
			IcuComparer that = (IcuComparer)obj;
			if (m_htskey == null)
			{
				if (that.m_htskey != null)
					return false;
			}
			else
			{
				if (that.m_htskey == null)
					return false;
				if (this.m_htskey.Count != that.m_htskey.Count)
					return false;
				IDictionaryEnumerator ie = that.m_htskey.GetEnumerator();
				while (ie.MoveNext())
				{
					if (!m_htskey.ContainsKey(ie.Key) || m_htskey[ie.Key] != ie.Value)
						return false;
				}
			}
			if (this.m_isDisposed != that.m_isDisposed)
				return false;
			if (this.m_lce != that.m_lce)
				return false;
			return this.m_sWs == that.m_sWs;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			CheckDisposed();

			int hash = GetType().GetHashCode();
			if (m_htskey != null)
				hash += m_htskey.Count * 53;
			if (m_isDisposed)
				hash += 1;
			if (m_lce != null)
				hash += m_lce.GetHashCode();
			if (m_sWs != null)
				hash *= m_sWs.GetHashCode();
			return hash;
		}
	}
}
