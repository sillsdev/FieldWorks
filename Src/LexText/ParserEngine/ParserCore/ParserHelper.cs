// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2014, SIL International. All Rights Reserved.
// <copyright from='2014' to='2014' company='SIL International'>
//		Copyright (c) 2014, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class ParserHelper
	{
		public static bool TryCreateParseMorph(FdoCache cache, XElement morphElem, out ParseMorph morph)
		{
			XElement formElement = morphElem.Element("MoForm");
			Debug.Assert(formElement != null);
			var hvoForm = (string)formElement.Attribute("DbRef");

			XElement msiElement = morphElem.Element("MSI");
			Debug.Assert(msiElement != null);
			var msaHvoStr = (string)msiElement.Attribute("DbRef");

			return TryCreateParseMorph(cache, hvoForm, msaHvoStr, out morph);
		}

		/// <summary>
		/// Creates a single ParseMorph object
		/// Handles special cases where the MoForm hvo and/or MSI hvos are
		/// not actual MoForm or MSA objects.
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="formHvo">The form hvo.</param>
		/// <param name="msaHvo">The msa hvo.</param>
		/// <param name="morph">a new ParseMorph object or null if the morpheme should be skipped</param>
		/// <returns></returns>
		public static bool TryCreateParseMorph(FdoCache cache, string formHvo, string msaHvo, out ParseMorph morph)
		{
			// Normally, the hvo for MoForm is a MoForm and the hvo for MSI is an MSA
			// There are four exceptions, though, when an irregularly inflected form is involved:
			// 1. <MoForm DbRef="x"... and x is an hvo for a LexEntryInflType.
			//       This is one of the null allomorphs we create when building the
			//       input for the parser in order to still get the Word Grammar to have something in any
			//       required slots in affix templates.  The parser filer can ignore these.
			// 2. <MSI DbRef="y"... and y is an hvo for a LexEntryInflType.
			//       This is one of the null allomorphs we create when building the
			//       input for the parser in order to still get the Word Grammar to have something in any
			//       required slots in affix templates.  The parser filer can ignore these.
			// 3. <MSI DbRef="y"... and y is an hvo for a LexEntry.
			//       The LexEntry is an irregularly inflected form for the first set of LexEntryRefs.
			// 4. <MSI DbRef="y"... and y is an hvo for a LexEntry followed by a period and an index digit.
			//       The LexEntry is an irregularly inflected form and the (non-zero) index indicates
			//       which set of LexEntryRefs it is for.
			ICmObject objForm;
			if (!cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(int.Parse(formHvo), out objForm))
			{
				morph = null;
				return false;
			}
			var form = objForm as IMoForm;
			if (form == null)
			{
				morph = null;
				return true;
			}

			// Irregulary inflected forms can have a combination MSA hvo: the LexEntry hvo, a period, and an index to the LexEntryRef
			string[] msaHvoParts = msaHvo.Split('.');
			ICmObject objMsa;
			if (!cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(int.Parse(msaHvoParts[0]), out objMsa))
			{
				morph = null;
				return false;
			}
			var msa = objMsa as IMoMorphSynAnalysis;
			if (msa != null)
			{
				morph = new ParseMorph(form, msa);
				return true;
			}

			var msaAsLexEntry = objMsa as ILexEntry;
			if (msaAsLexEntry != null)
			{
				// is an irregularly inflected form
				// get the MoStemMsa of its variant
				if (msaAsLexEntry.EntryRefsOS.Count > 0)
				{
					int index = msaHvoParts.Length == 2 ? int.Parse(msaHvoParts[1]) : 0;
					ILexEntryRef lexEntryRef = msaAsLexEntry.EntryRefsOS[index];
					ILexSense sense = MorphServices.GetMainOrFirstSenseOfVariant(lexEntryRef);
					var inflType = (ILexEntryInflType)lexEntryRef.VariantEntryTypesRS[0];
					morph = new ParseMorph(form, sense.MorphoSyntaxAnalysisRA, inflType);
					return true;
				}
			}

			// if it is anything else, we ignore it
			morph = null;
			return true;
		}

		/// <summary>
		/// Convert any characters in the name which are higher than 0x00FF to hex.
		/// Neither XAmple nor PC-PATR can read a file name containing letters above 0x00FF.
		/// </summary>
		/// <param name="originalName">The original name to be converted</param>
		/// <returns>Converted name</returns>
		public static string ConvertNameToUseAnsiCharacters(string originalName)
		{
			var sb = new StringBuilder();
			char[] letters = originalName.ToCharArray();
			foreach (var letter in letters)
			{
				int value = Convert.ToInt32(letter);
				if (value > 255)
				{
					string hex = value.ToString("X4");
					sb.Append(hex);
				}
				else
				{
					sb.Append(letter);
				}
			}
			return sb.ToString();
		}
	}
}
