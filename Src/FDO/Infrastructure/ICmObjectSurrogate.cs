using System;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	/// Internal interface for the CmObjectSurrogate.
	/// </summary>
	internal interface ICmObjectSurrogate : ICmObjectOrSurrogate
	{
		/// <summary>
		/// Connect an object with a surrogate, during bootstrap of extant system.
		/// </summary>
		/// <param name="obj"></param>
		void AttachObject(ICmObject obj);

		/// <summary>
		/// Get the main XML string converted to a byte array encoded in UTF8. Typically this is how it is
		/// actually stored, so it is more efficient to work with this than the XML string unless you
		/// really need a string. Note that this (unlike ICmObjectOrSurrogate.XMLBytes) may answer null;
		/// it will NOT generate the XML from the object.
		/// </summary>
		byte[] RawXmlBytes { get; }

		/// <summary>
		/// Get the Object's Guid.
		/// </summary>
		Guid Guid { get; }

		/// <summary>
		/// Reset the class and xml, after a data migration (and before reconstitution).
		/// </summary>
		/// <param name="className">Class name. (May be the same).</param>
		/// <param name="xml">New XML.</param>
		void Reset(string className, string xml);

		/// <summary>
		/// Reset the class and xml, after a data migration (and before reconstitution).
		/// </summary>
		/// <param name="className">Class name. (May be the same).</param>
		/// <param name="xmlBytes">New XML.</param>
		void Reset(string className, byte[] xmlBytes);
	}

	/// <summary>
	/// This interface encapsulates the behaviors needed in sets of objects that might be either CmObjects
	/// or surrogates, as used in persistence and Unit of Work.
	/// </summary>
	internal interface ICmObjectOrSurrogate
	{
		/// <summary>
		/// Get the main XML string for the internal CmObject.
		/// </summary>
		string XML { get; }

		/// <summary>
		/// Get the main byte array of the XML string for the internal CmObject. This should never be null;
		/// if an XML representation is not stored it will be computed from the CmObject.
		/// Enhance JohnT: this method should be renamed or merged with XmlBytes.
		/// </summary>
		byte[] XMLBytes { get; }

		ICmObjectId Id { get; }

		/// <summary>
		/// Get the Object's classname.
		/// </summary>
		string Classname { get; }

		/// <summary>
		/// Get the CmObject.
		/// </summary>
		ICmObject Object { get; }

		/// <summary>
		/// Find out if the surrogate has the actual object.
		/// </summary>
		bool HasObject { get; }
	}

	/// <summary>
	/// ICmObjectSurrogate factory.
	/// </summary>
	internal interface ICmObjectSurrogateFactory
	{
		/// <summary>
		/// Create a surrogate from the data store.
		/// This gets the full XML string of the object from the BEP.
		/// </summary>
		ICmObjectSurrogate Create(string xmlData);

		/// <summary>
		/// Create a surrogate from the data store.
		/// This gets the full XML string of the object from the BEP.
		/// </summary>
		ICmObjectSurrogate Create(byte[] xmlData);

		/// <summary>
		/// Create a surrogate from the data store.
		/// This gets the full XML string of the object from the BEP.
		/// </summary>
		ICmObjectSurrogate Create(Guid guid, string classname, string xmlData);

		/// <summary>
		/// Create a surrogate from the data store.
		/// This gets the full XML string of the object from the BEP.
		/// </summary>
		ICmObjectSurrogate Create(Guid guid, string classname, byte[] xmlData);

		/// <summary>
		/// Create a surrogate from the data store.
		/// This gets the full XML string of the object from the BEP.
		/// </summary>
		ICmObjectSurrogate Create(ICmObjectId objId, string classname, string xmlData);

		/// <summary>
		/// Create a surrogate from the data store.
		/// This gets the full XML string of the object from the BEP.
		/// </summary>
		ICmObjectSurrogate Create(ICmObjectId objId, string classname, byte[] xmlData);

		/// <summary>
		/// Create a surrogate from the data store.
		/// This gets the full XML string of the object from the BEP.
		/// </summary>
		ICmObjectSurrogate Create(ICmObjectOrSurrogate sourceSurrogate);

		/// <summary>
		/// Create one from an existing object; set its XML to the current state of the object.
		/// </summary>
		ICmObjectSurrogate Create(ICmObject obj);
	}

	/// <summary>
	/// ICmObjectSurrogate repository.
	/// </summary>
	internal interface ICmObjectSurrogateRepository
	{
		/// <summary>
		/// Get an id from the Guid in an XElement.
		/// Enhance JohnT: this belongs in some other interface now it no longer returns a surrogate.
		/// </summary>
		ICmObjectId GetId(XElement reader);

		/// <summary>
		/// Get a surrogate of the ICmObject.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>The surrogate of the ICmObject.</returns>
		ICmObjectSurrogate GetSurrogate(ICmObject obj);
	}
}