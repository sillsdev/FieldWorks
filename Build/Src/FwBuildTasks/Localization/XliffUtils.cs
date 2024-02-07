// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	internal class XliffUtils
	{
		public class State
		{
			internal const string NeedsTranslation = "needs-translation";
			internal const string Translated = "translated";
			internal const string Final = "final";
		}

		internal const string BodyTemplate = @"<?xml version='1.0'?>
			<xliff version='1.2' xmlns:sil='software.sil.org'>
				<file source-language='EN' datatype='plaintext' original='{0}'>
					<body>
					</body>
				</file>
			</xliff>";
		internal const string EmptyGroup = "<group id='{0}'></group>";
		internal const string TransUnitTemplate = "<trans-unit id='{0}'><source>{1}</source></trans-unit>";
		internal const string ContextTemplate = "<context>{0}</context>";
		internal const string FinalTargetTemplate = "<target state='" + State.Final + "'>{0}</target>";
		internal const string TransUnit = "trans-unit";
		private static readonly string[] AcceptableTranslationStates = { State.Translated, State.Final };

		internal static readonly XmlNamespaceManager NameSpaceManager = MakeNamespaceManager();
		internal static readonly XNamespace SilNamespace = "software.sil.org";
		private static XmlNamespaceManager MakeNamespaceManager()
		{
			var nsm = new XmlNamespaceManager(new NameTable());
			nsm.AddNamespace("sil", "software.sil.org");
			return nsm;
		}

		internal static bool IsTranslated(XElement target)
		{
			return AcceptableTranslationStates.Contains(target?.Attribute("state")?.Value);
		}
	}

	internal struct ConversionMap
	{
		public ConversionMap(string elementName, string idSuffix)
		{
			ElementName = elementName;
			IdSuffix = idSuffix;
		}

		public string ElementName { get; }
		public string IdSuffix { get; }
	}

	internal struct AttConversionMap
	{
		public AttConversionMap(string attName, string silEltName)
		{
			AttName = attName;
			SilEltName = silEltName;
		}

		public string AttName { get; }
		public string SilEltName { get; }
	}
}
