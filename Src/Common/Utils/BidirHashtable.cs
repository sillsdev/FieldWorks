using System;
using System.Collections;
//using System.Runtime.Serialization;

namespace SIL.FieldWorks.Common.Utils
{
	/// <summary>
	/// BidirHashtable is a simple, bidirectional data structure
	/// designed around Hashtables and accessed like a more robust Hashtable.
	/// Internally it just contains two hashtables:
	/// one maps from key to value, the other maps from value to key.
	/// (Therefore, note that both types of objects must have reasonable
	/// GetHashCode() and Equals() implementations.)
	/// Lookup in either direction is quick;
	/// changes take twice as long since two Hashtables are accessed.
	/// It is not currently serializable, but aside from that,
	/// it implements the same interfaces as a Hashtable.
	/// Forward lookup is just through the [] as in Hashtable.
	/// Reverse lookup is through ReverseLookup().
	/// Adding and setting elements is done with forward syntax identical to
	/// in Hashtable, but both internal Hashtables are affected.
	/// The intention is for this to be used for constant kinds of lookups.
	/// If your data is rapidly changing, be careful not to push this
	/// class to hard, such as by getting the internal collections and
	/// editing them.
	/// The explicit conversions make it simple to get a BidirHashtable
	/// from another Hashtable (or other IDictionary).  There are no
	/// implicit conversions, however, because the cost to do so, and the
	/// fact that it results in new objects which are separate from the
	/// originals, should be taken into account.
	/// An example use for this class is to store a mapping of
	/// numerical enumerations which map to class types.  Then, at
	/// startup, this kind of table can be filled, and at runtime it
	/// can be looked up in either direction, depending on your
	/// activity.
	///
	///
	/// This class was written by Todd C. Gleason.  ( www.cool-man.org )
	/// Version history:
	/// 08-30-2003  1.0  Initial revision
	/// This class is free to use, and the class name or namespace
	/// can be modified (such as to integrate with a company's
	/// set of class and namespace naming conventions),
	/// but the credit, version history, and this
	/// block of paragraphs must be maintained somewhere in the same
	/// source file as the class itself.  (The remainder of the
	/// documentation may be edited.)
	/// There are no warranties of any kind in using this class.
	/// Developers using it assume all responsibility for any debugging,
	/// though the original author can be contacted about bug fixes.
	/// The author requests for developers to let him know if they
	/// find this class useful.
	/// </summary>
	public class BidirHashtable : IDictionary, ICollection, IEnumerable,
		//ISerializable  // could implement this sometime
		ICloneable
	{
		private Hashtable m_htFwd = null;
		private Hashtable m_htBkwd = null;

		/// <summary>
		/// Standard constructor.
		/// Eventually it might be nice to add more, but in most cases
		/// the default should do fine.
		/// </summary>
		public BidirHashtable()
		{
			m_htFwd = new Hashtable();
			m_htBkwd = new Hashtable();
		}

		/// <summary>
		/// This constructor initializes from an existing
		/// IDictionary (such as another Hashtable).
		/// It sets up both the forward and backward tables.
		/// A use for this would be to take an existing Hashtable
		/// that you don't want rewritten as a BidirHashtable, and
		/// set it up for efficient reverse lookups.
		/// Obviously, it's more efficient to use a BidirHashtable
		/// from the start, but the cost of converting a lot of code
		/// may be too high to justify.
		/// </summary>
		public BidirHashtable(IDictionary dict)
		{
			m_htFwd = new Hashtable();
			m_htBkwd = new Hashtable();

			foreach( object key in dict.Keys )
			{
				this[key] = dict[key];
			}
		}

		/// <summary>
		/// Private constructor used when explicitly attaching to
		/// an existing Hashtable and then setting up the reverse
		/// mapping from it.
		/// </summary>
		/// <param name="ht">Hashtable to attach to.</param>
		/// <param name="bytDummyIndicatesAttach">Dummy parameter (just to give it a different signature)</param>
		private BidirHashtable(Hashtable ht, byte bytDummyIndicatesAttach)
		{
			m_htFwd = ht;
			m_htBkwd = new Hashtable();

			foreach( object key in ht.Keys )
			{
				m_htBkwd[ht[key]] = key;
			}
		}

		/// <summary>
		///
		/// </summary>
		public int Count {get { return m_htFwd.Count;  } }
		/// <summary>
		///
		/// </summary>
		public bool IsSynchronized {get { return m_htFwd.IsSynchronized; } }
		/// <summary>
		///
		/// </summary>
		public object SyncRoot {get { return m_htFwd.SyncRoot; } }
		/// <summary>
		///
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		public void CopyTo(
			Array array,
			int index
			)
		{
			m_htFwd.CopyTo( array, index );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		public void CopyValuesTo(
			Array array,
			int index
			)
		{
			m_htBkwd.CopyTo( array, index );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <param name="val"></param>
		public void Add( object key, object val )
		{
			m_htFwd.Add( key, val );
			m_htBkwd.Add( val, key );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		public void Remove( object key )
		{
			object val = m_htFwd[key];
			m_htFwd.Remove( key );
			m_htBkwd.Remove( val );
		}

		/// <summary>
		///
		/// </summary>
		public void Clear()
		{
			m_htFwd.Clear();
			m_htBkwd.Clear();
		}

		/// <summary>
		/// Forward lookup and set.
		/// </summary>
		public object this[ object key ]
		{
			get {  return m_htFwd[key];  }
			set
			{
				// If the forward map contains this key, changing it
				// means removing it from the reverse map by its value,
				// which is the key for the reverse map.
				if ( m_htFwd.ContainsKey(key) )
				{
					m_htBkwd.Remove( m_htFwd[key] );
				}
				m_htFwd[key] = value;
				m_htBkwd[value] = key;
			}
		}

		/// <summary>
		/// This is the key addition in this class--a reverse lookup
		/// which operates at the speed of a regular Hashtable.
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public object ReverseLookup( object val )
		{
			return m_htBkwd[val];
		}

		/// <summary>
		///
		/// </summary>
		public bool IsFixedSize {
			get {  return m_htFwd.IsFixedSize;  }
		}

		/// <summary>
		///
		/// </summary>
		public bool IsReadOnly {
			get {  return m_htFwd.IsReadOnly;  }
		}

		/// <summary>
		/// Note:  Editing the keys would make this class inconsistent.
		/// </summary>
		public ICollection Keys
		{
			get {  return m_htFwd.Keys;  }
		}

		/// <summary>
		/// Note:  Editing the values would make this class inconsistent,
		/// as they are used for the reverse mapping (and editing would change
		/// their hashcodes, plus make the forward and reverse hashmaps
		/// inconsistent.
		/// </summary>
		public ICollection Values
		{
			get {  return m_htFwd.Values;  }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool Contains( object key )
		{
			return m_htFwd.Contains( key );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public bool ContainsValue( object val )
		{
			return m_htBkwd.Contains( val );
		}

		//public
		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_htFwd.GetEnumerator();
		}

		//public
		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return m_htFwd.GetEnumerator();
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public object Clone()
		{
			BidirHashtable bh = new BidirHashtable();
			bh.m_htFwd = (Hashtable) m_htFwd.Clone();
			bh.m_htBkwd = (Hashtable) m_htBkwd.Clone();
			return bh;
		}

		#region Explicit conversion to/from Hashtable
		/// <summary>
		///
		/// </summary>
		/// <param name="ht"></param>
		/// <returns></returns>
		public static explicit operator BidirHashtable(Hashtable ht)
		{
			return new BidirHashtable( ht );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="bd"></param>
		/// <returns></returns>
		public static explicit operator Hashtable(BidirHashtable bd)
		{
			return (Hashtable) bd.m_htFwd.Clone();
		}
		#endregion

		#region Access to private Hashtables
		/// <summary>
		/// Gives direct access to forward hashtable.
		/// Although provided as a "just-in-case" kind of convenience,
		/// it should be used carefully, if ever, as it gives direct
		/// access to internal data, and if altered, the state of this
		/// object will become inconsistent.
		/// Preferably, use (Hashtable) conversion instead, which
		/// returns a clone.
		/// </summary>
		public Hashtable ForwardHashtable
		{
			get {  return m_htFwd;  }
		}

		/// <summary>
		/// Gives direct access to backward hashtable.
		/// Although provided as a "just-in-case" kind of convenience,
		/// it should be used carefully, if ever, as it gives direct
		/// access to internal data, and if altered, the state of this
		/// object will become inconsistent.
		/// Preferably, use BackwardHashtableClone instead, which
		/// returns a clone.
		/// </summary>
		public Hashtable BackwardHashtable
		{
			get {  return m_htBkwd;  }
		}

		/// <summary>
		/// Returns a clone of the backward table which can be
		/// passed off and edited.
		/// </summary>
		public Hashtable BackwardHashtableClone
		{
			get {  return (Hashtable) m_htBkwd.Clone();  }
		}
		#endregion

		#region Attach and ReverseDirection
		/// <summary>
		/// Reverses the direction of the BidirHashtable.
		/// This just swaps the two internal Hashtables.
		/// </summary>
		public void ReverseDirection()
		{
			Hashtable htTemp = m_htFwd;
			m_htFwd = m_htBkwd;
			m_htBkwd = htTemp;
		}

		/// <summary>
		/// Creates a new BidirHashtable which is attached to
		/// the input Hashtable.  The reverse mapping is set up
		/// automatically.
		/// Note that there is no need for a Detach(); just stop using
		/// this object and let it get garbage-collected aside from
		/// the attached table, which could still be used (though the
		/// user would have to be careful not to use the attached
		/// table while the BidirHashtable might still be used).
		/// While it is possible that someone might want to set up
		/// the reverse mapping by doing an Attach(), for that you
		/// must just use Attach() and then ReverseDirection().
		/// </summary>
		/// <param name="ht"></param>
		/// <returns></returns>
		public static BidirHashtable Attach(Hashtable ht)
		{
			return new BidirHashtable( ht, (byte) 0 );
		}
		#endregion
	}
}
