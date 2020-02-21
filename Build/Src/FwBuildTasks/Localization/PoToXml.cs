// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	/// <summary>
	/// Store localized strings from a PO file in the corresponding strings-%locale%.xml file.
	/// Strings include localized attributes, literals, and context help from XML configuration files.
	/// This class was adapted from the LocaleStrings project in https://github.com/sillsdev/fwsupporttools
	/// </summary>
	public class PoToXml : Task
	{
		/// <summary>The PO file to extract into strings-%locale%.xml</summary>
		[Required]
		public string PoFile { get; set; }

		/// <summary>A localized version of strings-en.xml that needs to have additional items added for the target language</summary>
		[Required]
		public string StringsXml { get; set; }

		public override bool Execute()
		{
			Log.LogMessage($"Storing strings from '{PoFile}' in '{StringsXml}'.");
			StoreLocalizedStrings(PoFile, StringsXml, Log);
			return true;
		}

		/// <summary>
		/// Store localized strings from a PO file in the corresponding strings-%locale%.xml file
		/// </summary>
		internal static void StoreLocalizedStrings(string poFile, string stringsXmlFile, TaskLoggingHelper log)
		{
			var dictTrans = LoadPOFile(poFile, log);
			if (dictTrans.Count == 0)
			{
				throw new Exception($"No translations found in PO file '{poFile}'!");
			}
			var xdoc = new XmlDocument();
			xdoc.Load(stringsXmlFile);
			StoreTranslatedThings(xdoc.DocumentElement, dictTrans);
			xdoc.Save(stringsXmlFile);
		}

		private static Dictionary<string, POString> LoadPOFile(string sMsgFile, TaskLoggingHelper log)
		{
			using (var srIn = new StreamReader(sMsgFile, Encoding.UTF8))
			{
				return ReadPoFile(srIn, log);
			}
		}

		internal static Dictionary<string, POString> ReadPoFile(TextReader srIn, TaskLoggingHelper log)
		{
			POString.ResetInputLineNumber();
			var dictTrans = new Dictionary<string, POString>();
			POString.ReadFromFile(srIn); // read header
			var poStr = POString.ReadFromFile(srIn);
			while (poStr != null)
			{
				StoreString(dictTrans, poStr, log);
				poStr = POString.ReadFromFile(srIn);
			}
			return dictTrans;
		}

		/// <summary>
		/// Store a POString in the dictionary, unless another string with the same msgid already exists.
		/// </summary>
		public static void StoreString(Dictionary<string, POString> dictTrans, POString poStr, TaskLoggingHelper log)
		{
			if (poStr.IsObsolete)
				return;
			var msgid = poStr.MsgIdAsString();
			if (dictTrans.ContainsKey(msgid))
			{
				log?.LogMessage("The message id '{0}' already exists.  Ignoring this occurrence around line {1}.", msgid, POString.InputLineNumber);
			}
			else
			{
				dictTrans.Add(msgid, poStr);
			}
		}

		private static void StoreTranslatedThings(XmlElement xelRoot, Dictionary<string, POString> dictTrans)
		{
			// ReSharper disable once PossibleNullReferenceException - the caller always provides an element from a document
			var xelAttGroup = xelRoot.OwnerDocument.CreateElement("group");
			xelAttGroup.SetAttribute("id", "LocalizedAttributes");
			var xelLitGroup = xelRoot.OwnerDocument.CreateElement("group");
			xelLitGroup.SetAttribute("id", "LocalizedLiterals");
			var xelHelpGroup = xelRoot.OwnerDocument.CreateElement("group");
			xelHelpGroup.SetAttribute("id", "LocalizedContextHelp");
			foreach (var poStr in dictTrans.Values)
			{
				var value = poStr.MsgStrAsString();
				if (string.IsNullOrEmpty(value))
					continue;
				var autoComments = poStr.AutoComments;
				if (autoComments == null)
					continue;
				if (autoComments.Any(IsFromXmlAttribute))
				{
					AppendStringToGroup(xelAttGroup, poStr.MsgIdAsString(), value);
				}
				if (autoComments.Any(IsFromLiteral))
				{
					AppendStringToGroup(xelLitGroup, poStr.MsgIdAsString(), value);
				}
				foreach (var contextHelpId in autoComments.Select(FindContextHelpId).Where(id => !string.IsNullOrEmpty(id)))
				{
					AppendStringToGroup(xelHelpGroup, contextHelpId, value);
				}
			}
			xelRoot.AppendChild(xelAttGroup);
			xelRoot.AppendChild(xelLitGroup);
			xelRoot.AppendChild(xelHelpGroup);
		}

		private static void AppendStringToGroup(XmlElement xelGroup, string id, string txt)
		{
			// ReSharper disable once PossibleNullReferenceException - the caller always provides an element from a document
			var xelString = xelGroup.OwnerDocument.CreateElement("string");
			xelString.SetAttribute("id", id);
			xelString.SetAttribute("txt", txt);
			xelGroup.AppendChild(xelString);
		}

		private static bool IsFromXmlAttribute(string comment)
		{
			if (comment == null || !comment.StartsWith("/") && !comment.StartsWith("file:///"))
				return false;
			// a po msg is from an XML attribute if it has a comment ending in '/@.+'
			var idx = comment.LastIndexOf("/", StringComparison.Ordinal);
			return idx >= 0 && comment.Length > idx + 2 && comment[idx + 1] == '@';

		}

		private static bool IsFromLiteral(string comment)
		{
			return comment != null && comment.StartsWith("/") && comment.EndsWith("/lit");
		}

		internal static string FindContextHelpId(string comment)
		{
			const string contextMarker = "/ContextHelp.xml::/strings/item[@id=\"";
			const string contextTerminator = "\"]";
			if (comment == null || !comment.StartsWith("/"))
				return null;
			var idx = comment.IndexOf(contextMarker, StringComparison.Ordinal);
			if (idx <= 0)
				return null;
			var idPlus = comment.Substring(idx + contextMarker.Length);
			var idxEnd = idPlus.IndexOf('"');
			if (idxEnd <= 0 || !contextTerminator.Equals(idPlus.Substring(idxEnd)))
				return null;
			return idPlus.Remove(idxEnd);
		}
	}
}
