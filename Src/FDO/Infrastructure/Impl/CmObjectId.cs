using System;
using System.Diagnostics;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// A CmObjectId is basically a GUID. However, GUID is a struct, and hence, every Guid variable takes 16 bytes.
	/// CmObjectIds can be shared. Also, being our own class, we can implement some common methods with CmObject.
	/// CmObjectId also implements (secretly and very incompletely) ICmObjectOrSurrogate. This allows them to occur in the
	/// main dictionary of the IdentityMap.
	/// </summary>
	class CmObjectId : ICmObjectId, ICmObjectOrIdInternal, ICmObjectOrSurrogate
	{
		private readonly Guid m_guid;

		/// <summary>
		/// The goal is a private constructor which ensures the only way to get one is something
		/// that ensures we get the canonical one. Has to be protected to allow CmObjectIdWithHvo.
		/// Do NOT subclass this as some sort of trick to be able to create one.
		/// </summary>
		/// <param name="guid"></param>
		protected CmObjectId(Guid guid)
		{
			m_guid = guid;
		}

		// This is used only by the BEPs that create an ICmObjectId and then see that it gets added to the IdentityMap.
		static internal ICmObjectId Create(Guid guid)
		{
			return new CmObjectId(guid);
		}

		// This is used only by the factory methods (on IdentifyMap) that create one.
		static internal ICmObjectId FromGuid(Guid guid, IdentityMap map)
		{
			return map.GetCanonicalID(new CmObjectId(guid));
		}

		/// <summary>
		/// object ids are equal if their guids are.
		/// </summary>
		public override bool Equals(object obj)
		{
			return obj is CmObjectId && Equals((CmObjectId)obj);
		}

		/// <summary>
		/// object ids are equal if their guids are.
		/// </summary>
		public bool Equals(CmObjectId id)
		{
			return m_guid == id.m_guid;
		}

		// This is tempting, but it doesn't work for ICmObject, which is what almost everything has.
		// And it complicates the implementation of Equals above.
		// If you need to compare {I}CmObjectIds that may be from different identity maps and so not referentially equal,
		// use Equals.
		//public static bool operator ==(CmObjectId x, CmObjectId y)
		//{
		//    return x.Equals(y);
		//}

		//public static bool operator !=(CmObjectId x, CmObjectId y)
		//{
		//    return !x.Equals(y);
		//}

		/// <summary>
		/// consistent with equality.
		/// </summary>
		public override int GetHashCode()
		{
			return m_guid.GetHashCode();
		}

		int ICmObjectOrIdInternal.GetHvo(IdentityMap map)
		{
			return GetHvoUsing(map);
		}

		protected virtual int GetHvoUsing(IdentityMap map)
		{
			var canonicalId = map.GetObjectOrIdWithHvoFromGuid(Guid);
			// This will force an exception rather than an infinite loop if we don't get back a subclass
			// that doesn't need the map.
			return ((ICmObjectOrIdInternal) canonicalId).GetHvo(null);
		}

		public Guid Guid
		{
			get { return m_guid; }
		}

		#region ICmObjectOrId Members

		public ICmObjectId Id
		{
			get { return this; }
		}

		public ICmObject GetObject(ICmObjectRepository repo)
		{
			return repo.GetObject(this);
		}

		#endregion

		#region ICmObjectId Members


		/// <summary>
		/// Add to the writer the standard XML representation of a reference to this object,
		/// marked to indicate whether it is an owned or referenced object.
		/// </summary>
		public void ToXMLString(bool owning, System.Xml.XmlWriter writer)
		{
			writer.WriteStartElement("objsur");
			writer.WriteAttributeString("guid", Guid.ToString());
			writer.WriteAttributeString("t", (owning ? "o" : "r"));
			writer.WriteEndElement();
		}

		#endregion

		#region ICmObjectOrSurrogate Members

		string ICmObjectOrSurrogate.XML
		{
			get { throw new NotImplementedException(); }
		}

		byte[] ICmObjectOrSurrogate.XMLBytes
		{
			get { throw new NotImplementedException(); }
		}

		ICmObjectId ICmObjectOrSurrogate.Id
		{
			get { return this; }
		}

		string ICmObjectOrSurrogate.Classname
		{
			get { throw new NotImplementedException(); }
		}

		ICmObject ICmObjectOrSurrogate.Object
		{
			get { throw new NotImplementedException(); }
		}

		bool ICmObjectOrSurrogate.HasObject
		{
			get { return false; }
		}

		#endregion
	}

	/// <summary>
	/// This variation of a CmObjectId can be stored in a surrogate in place of the usual CmObjectId,
	/// when we need to associate an HVO with an object ID without actually fluffing up the object.
	/// It is also stored in the HVO to object map, until the object does get fluffed.
	/// </summary>
	internal class CmObjectIdWithHvo : CmObjectId
	{
		/// <summary>
		/// This is the point of the class: to be able to store an HVO with the object.
		/// </summary>
		public int Hvo { get; private set; }

		/// <summary>
		/// Don't just make one of these without making it the canonical one and registering
		/// it as the object of its HVO in the identity map.
		/// </summary>
		internal CmObjectIdWithHvo(Guid guid, int hvo) : base(guid)
		{
			Hvo = hvo;
		}

		/// <summary>
		/// The point of this subclass is to do this efficiently!
		/// </summary>
		protected override int GetHvoUsing(IdentityMap identityMap)
		{
			return Hvo;
		}
	}
}