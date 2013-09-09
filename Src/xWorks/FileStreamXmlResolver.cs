using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Xml;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Passing null as XmlResolver does not work on Linux. To avoid trying to resolve URLs in a way that works on both
	/// platforms we use this dummy class. It does the minimum URL handling to make the Mono validator work.
	/// That means it has to handle file:// URLs, because even the base XML file is passed to this routine to resolve.
	/// However, it seems to work to return null when resolving a non-file URL (like a DTD on the web),
	/// or if we are asked to return some other type besides a stream.
	/// The current version returns an empty stream if it is asked for a stream and passed a URI it can't handle.
	/// I think this was required in at least one case.
	/// </summary>
	public class FileStreamXmlResolver : XmlResolver
	{
		/// <summary>
		/// Get an instance using GetNullResolver.
		/// </summary>
		private FileStreamXmlResolver()
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
				return new FileStreamXmlResolver();
			else
				return null;
		}
		/// <summary>
		/// This will be called with URIs returened from ResolveUri. Typically they will all be file ones, or null.
		/// </summary>
		/// <param name="absoluteUri"></param>
		/// <param name="role"></param>
		/// <param name="ofObjectToReturn"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "This API requires us to return an open stream. Hopefully the client closes it.")]
		public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
		{
			if (absoluteUri != null && absoluteUri.IsFile && ofObjectToReturn.IsAssignableFrom(typeof(FileStream)))
			{
				// local file: return a stream on it. This might be the main file we are checking.
				var path = absoluteUri.LocalPath;
				return new FileStream(path, FileMode.Open);
			}
			if (ofObjectToReturn.IsAssignableFrom(typeof(MemoryStream)))
				return new MemoryStream();
			return null;
		}

		public override ICredentials Credentials
		{
			set {}
		}

		public override Uri ResolveUri(Uri baseUri, string relativeUri)
		{
			if (relativeUri.StartsWith(@"file://"))
				return base.ResolveUri(baseUri, relativeUri);

			return null;
		}
	}
}