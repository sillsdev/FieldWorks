// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks
{
	/// <summary>
	/// Converts a WiX .wxs file to a .wxi (WiX Include) file by exchanging the base &lt;Wix&gt; and &lt;Fragment&gt; elements
	/// with an &lt;Include/&gt; element. This is useful for harvesting multiple groups of files using Heat and using the results with
	/// the generic installer (github.com/sillsdev/genericinstaller)
	/// </summary>
	public class WxsToWxi : Task
	{
		/// <summary/>
		[Required]
		public string SourceFile { get; set; }

		[SuppressMessage("ReSharper", "AssignNullToNotNullAttribute", Justification = "cannot recover from null source or dest file paths")]
		public override bool Execute()
		{
			var sourceDoc = XDocument.Load(SourceFile);

			// Heat includes a namespace that we don't need (and interferes with our processing). Remove it.
			// https://social.msdn.microsoft.com/Forums/en-US/bed57335-827a-4731-b6da-a7636ac29f21
			foreach (var elt in sourceDoc.Descendants().Where(e => e.Name.Namespace != XNamespace.None))
			{
				elt.Name = XNamespace.None.GetName(elt.Name.LocalName);
			}
			var elements = sourceDoc.Element("Wix")?.Elements().Elements();
			if (elements == null)
			{
				Log.LogError("No <Wix> element in {0}", SourceFile);
				return false;
			}
			var destDoc = new XDocument(new XElement("Include", elements));
			destDoc.Save(Path.ChangeExtension(SourceFile, "wxi"));
			return true;
		}
	}
}
