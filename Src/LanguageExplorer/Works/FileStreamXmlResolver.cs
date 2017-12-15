// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;

namespace LanguageExplorer.Works
{
	/// <summary>
	/// The latest mono code uses .net code for XmlReader and XmlResolver. This resolver only
	/// reads local files not Internet files.
	/// </summary>
	public class FileStreamXmlResolver : XmlUrlResolver
	{
		public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
		{
			return !absoluteUri.IsFile ? null : base.GetEntity(absoluteUri, role, ofObjectToReturn);
		}
	}
}