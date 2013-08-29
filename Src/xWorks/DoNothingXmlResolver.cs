using System;
using System.Net;
using System.Xml;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Passing null as XmlResolver does not work on Linux. To avoid trying to resolve URLs in a way that works on both
	/// platforms we use this dummy class.
	/// </summary>
	public class DoNothingXmlResolver : XmlResolver
	{
		/// <summary>
		/// Get an instance using GetNullResolver.
		/// </summary>
		private DoNothingXmlResolver()
		{}

		/// <summary>
		/// Returns a value that can be set as XmlDocument.XmlResolver when we don't want to validate any URLs.
		/// This also means not validating against any DTD.
		/// On Windows null works (and an instance of this class, in general, doesn't); on Linux an instance of this works, for now.
		/// As Mono gets more compatible we may be able (even forced) to retire this.
		/// </summary>
		/// <returns></returns>
		public static XmlResolver GetNullResolver()
		{
			if (MiscUtils.IsUnix)
				return new DoNothingXmlResolver();
			else
				return null;
		}
		/// <summary>
		/// This should not be called since ResolveUri returns nothing.
		/// </summary>
		/// <param name="absoluteUri"></param>
		/// <param name="role"></param>
		/// <param name="ofObjectToReturn"></param>
		/// <returns></returns>
		public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
		{
			throw new NotImplementedException();
		}

		public override ICredentials Credentials
		{
			set {}
		}

		public override Uri ResolveUri(Uri baseUri, string relativeUri)
		{
			return null;
		}
	}
}