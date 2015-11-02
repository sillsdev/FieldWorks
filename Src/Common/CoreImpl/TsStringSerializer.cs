// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TsStringSerializer.cs
// Responsibility: FW Team
//
// <remarks>
// </remarks>

using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class for serializing and deserializing TsStrings stored in XML format
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class TsStringSerializer
	{
		#region Member Variables
		private static readonly ITsStrFactory s_strFactory = TsStrFactoryClass.Create();
		#endregion

		#region Public Deserialization Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deserializes a TsString from the specified XML data.
		/// NOTE: This overload is slower then using the overload that takes an XElement.
		/// </summary>
		/// <param name="xml">The XML data.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// <returns>The created TsString. Will never be <c>null</c> because an exception is
		/// thrown if anything is invalid.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static ITsString DeserializeTsStringFromXml(string xml, ILgWritingSystemFactory lgwsf)
		{
			return DeserializeTsStringFromXml(XElement.Parse(xml, LoadOptions.PreserveWhitespace), lgwsf);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deserializes a TsString from the XML node.
		/// </summary>
		/// <param name="xml">The XML node.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// <returns>The created TsString. Will never be <c>null</c> because an exception is
		/// thrown if anything is invalid.</returns>
		/// ------------------------------------------------------------------------------------
		public static ITsString DeserializeTsStringFromXml(XElement xml, ILgWritingSystemFactory lgwsf)
		{
			if (xml != null)
			{
				switch (xml.Name.LocalName)
				{
					case "AStr":
					case "Str":
						return HandleSimpleString(xml, lgwsf) ?? HandleComplexString(xml, lgwsf);
					default:
						throw new XmlSchemaException("TsString XML must contain a <Str> or <AStr> root element");
				}
			}
			return null;
		}

		#endregion

		#region Deserialization Helper Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a complex string that contains multiple runs with optional multiple
		/// text props applied.
		/// </summary>
		/// <param name="xml">The XML.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// <returns>The created TsString</returns>
		/// ------------------------------------------------------------------------------------
		private static ITsString HandleComplexString(XElement xml, ILgWritingSystemFactory lgwsf)
		{
			var runs = xml.Elements("Run");
			if (runs.Count() == 0)
			{
				if (xml.Name.LocalName == "AStr" && xml.Attributes().Count() == 1)
				{
					// This duplicates a little bit of code from HandleSimpleRun, but I wanted to keep that really simple
					// and fast, and this case hardly ever happens...maybe not at all in real life.
					XAttribute wsAttribute = xml.Attributes().First();
					if (wsAttribute.Name.LocalName != "ws")
						return null; // we handle only single runs with only the ws attribute.
					// Make sure the text is in the decomposed form (FWR-148)
					string runText = Icu.Normalize(xml.Value, Icu.UNormalizationMode.UNORM_NFD);
					return s_strFactory.MakeString(runText, GetWsForId(wsAttribute.Value, lgwsf));
				}
				return null;	// If we don't have any runs, we don't have a string!
			}

			var strBldr = TsIncStrBldrClass.Create();

			foreach (XElement runElement in runs)
			{
				if (runElement == null)
					throw new XmlSchemaException("TsString XML must contain a <Run> element contained in a <" + xml.Name.LocalName + "> element");
				string runText = runElement.Value;
				if (runElement.Attribute("ws") == null && (runText.Length == 0 || runText[0] > 13))
					throw new XmlSchemaException("Run element must contain a ws attribute. Run text: " + runElement.Value);

				// Make sure the text is in the decomposed form (FWR-148)
				runText = Icu.Normalize(runText, Icu.UNormalizationMode.UNORM_NFD);
				bool isOrcNeeded = TsPropsSerializer.GetPropAttributesForElement(runElement, lgwsf, strBldr);

				// Add an ORC character, if needed, for the run
				if (runText.Length == 0 && isOrcNeeded)
					runText = StringUtils.kszObject;

				// Add the text with the properties to the builder
				strBldr.Append(runText);
			}

			return strBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles creating simple TsStrings that contain one run with only a writing system.
		/// </summary>
		/// <param name="rootXml">The element including the Str or AStr tag</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// <returns>The created TsString or null if the XML was too complext to be handled
		/// by this method.</returns>
		/// ------------------------------------------------------------------------------------
		private static ITsString HandleSimpleString(XElement rootXml, ILgWritingSystemFactory lgwsf)
		{
			if (rootXml.Elements().Count() != 1)
				return null;

			XElement textElement = rootXml.Elements().First();
			if (textElement.Name.LocalName != "Run")
				return null; // probably an error, anyway not simple case we are optimizing.

			int cTextElementAtribs = textElement.Attributes().Count();
			if (cTextElementAtribs != 1)
				return null; // Way too complex for this simple case

			XAttribute wsAttribute = textElement.Attributes().First();
			if (wsAttribute.Name.LocalName != "ws")
				return null; // we handle only single runs with only the ws attribute.
			// Make sure the text is in the decomposed form (FWR-148)
			string runText = Icu.Normalize(textElement.Value, Icu.UNormalizationMode.UNORM_NFD);
			return s_strFactory.MakeString(runText, GetWsForId(wsAttribute.Value, lgwsf));
		}
		#endregion

		#region Internal methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ws handle for specified writing system identifier.
		/// </summary>
		/// <param name="wsId">The writing system identifier.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// ------------------------------------------------------------------------------------
		internal static int GetWsForId(string wsId, ILgWritingSystemFactory lgwsf)
		{
			try
			{
				ILgWritingSystem ws = lgwsf.get_Engine(wsId);
				return ws.Handle;
			}
			catch (Exception e)
			{
				throw new XmlSchemaException("Unable to create writing system: " + wsId, e);
			}
		}
		#endregion
	}
}
