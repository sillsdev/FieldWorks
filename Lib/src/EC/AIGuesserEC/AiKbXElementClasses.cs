using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using ECInterfaces;

namespace SilEncConverters40
{

	[ClassInterface(ClassInterfaceType.None)]
	internal class TargetWordElement
	{
		public string TargetWord { get; set; }
		public int NumberOfOccurrences { get; set; }

		public TargetWordElement()
		{
		}

		public TargetWordElement(XElement elem)
		{
			NumberOfOccurrences = AdaptItKBReader.GetAttributeValue(elem, Cstrn, 1);
			TargetWord = AdaptItKBReader.GetAttributeValue(elem, Cstra, "");
		}

		public const string CstrRS = "RS";
		public const string Cstrn = "n";
		public const string Cstra = "a";

		public XElement GetXml
		{
			/*
				<RS n="1" a="ασδγδ" />
			*/
			get
			{
				string strTargetWordForm = TargetWord;
				if (strTargetWordForm == null)
					strTargetWordForm = "";

				return new XElement(CstrRS,
									new XAttribute(Cstrn, NumberOfOccurrences),
									new XAttribute(Cstra, strTargetWordForm));
			}
		}
	}

	[ClassInterface(ClassInterfaceType.None)]
	internal class SourceWordElement : List<TargetWordElement>
	{
		public string SourceWord { get; set; }
		public string Force { get; set; }

		public SourceWordElement()
		{
		}

		public SourceWordElement(XElement elem)
		{
			Force = AdaptItKBReader.GetAttributeValue(elem, Cstrf, "0");
			SourceWord = AdaptItKBReader.GetAttributeValue(elem, Cstrk, "");

			foreach (var targetWordElement in
				elem.Descendants(TargetWordElement.CstrRS).Select(targetWordElement
					=> new TargetWordElement(targetWordElement)))
			{
				if (!Contains(targetWordElement))
					Add(targetWordElement);
			}
		}

		public const string CstrTU = "TU";
		public const string Cstrf = "f";
		public const string Cstrk = "k";

		public XElement GetXml
		{
			/*
			  <TU f="0" k="Δ">
				<RS n="1" a="ασδγδ" />
			  </TU>
			*/
			get
			{
				var elem = new XElement(CstrTU,
										new XAttribute(Cstrf, Force),
										new XAttribute(Cstrk, SourceWord));

				foreach (var targetWordElement in this)
					elem.Add(targetWordElement.GetXml);

				return elem;
			}
		}

		public bool Contains(string strTargetWordForm)
		{
			return this.Any(targetWordForm
				=> targetWordForm.TargetWord == strTargetWordForm);
		}

		/// <summary>
		/// This property can be used to get a new TargetWordElement associated with
		/// this Source word element
		/// </summary>
		public TargetWordElement GetNewTargetWord
		{
			get
			{
				var targetWordElement = new TargetWordElement { NumberOfOccurrences = 1 };
				Add(targetWordElement);
				return targetWordElement;
			}
		}

		/// <summary>
		/// This method is used to (possibly) add a new word form (e.g. via AddNewPair)
		/// But if it already exists, then it just bumps the count.
		/// </summary>
		/// <param name="strRhs">the target word form</param>
		public void AddWord(string strRhs)
		{
			Debug.Assert(!String.IsNullOrEmpty(strRhs));
			// if there's already one...
			if (this.Where(targetWordElement =>
				targetWordElement.TargetWord == strRhs).Any())
			{
				// just return (I don't care about the "NumberOfOccurrences" that I should
				//  bump the count and it just seems to make the ChorusMerger go into an
				//  infinite processing loop when they're different
				return;
			}

			Debug.Assert(!Contains(strRhs));
			Add(new TargetWordElement { TargetWord = strRhs, NumberOfOccurrences = 1 });
		}
	}

	[ClassInterface(ClassInterfaceType.None)]
	internal class MapOfSourceWordElements : Dictionary<string, SourceWordElement>
	{
		public int NumOfWordsPerPhrase;

		public MapOfSourceWordElements(int nNumOfWordsPerPhrase)
		{
			NumOfWordsPerPhrase = nNumOfWordsPerPhrase;
		}

		public MapOfSourceWordElements(XElement elem)
		{
			NumOfWordsPerPhrase = AdaptItKBReader.GetAttributeValue(elem, Cstrmn, 1);

			foreach (var sourceWordElement in
				elem.Descendants(SourceWordElement.CstrTU).Select(elemSourceWordElement
					=> new SourceWordElement(elemSourceWordElement)))
			{
				if (!ContainsKey(sourceWordElement.SourceWord))
					Add(sourceWordElement.SourceWord, sourceWordElement);
			}
		}

		public const string CstrMAP = "MAP";
		public const string Cstrmn = "mn";

		public XElement GetXml
		{
			get
			{
				/*
				<MAP mn="1">
				  <TU f="0" k="Δ">
					<RS n="1" a="ασδγδ" />
				  </TU>
				</MAP>
				*/
				var elem = new XElement(CstrMAP,
										new XAttribute(Cstrmn, NumOfWordsPerPhrase));

				foreach (var sourceWordElement in Values)
					elem.Add(sourceWordElement.GetXml);

				return elem;
			}
		}

		public SourceWordElement AddCouplet(string strLhs, string strRhs)
		{
			Debug.Assert(!String.IsNullOrEmpty(strLhs) && !String.IsNullOrEmpty(strRhs));
			SourceWordElement sourceWordElement;
			if (!TryGetValue(strLhs, out sourceWordElement))
			{
				sourceWordElement = new SourceWordElement { Force = "0", SourceWord = strLhs };
				Add(strLhs, sourceWordElement);
			}

			sourceWordElement.AddWord(strRhs);
			return sourceWordElement;
		}
	}

	[ClassInterface(ClassInterfaceType.None)]
	internal class MapOfMaps : Dictionary<int, MapOfSourceWordElements>
	{
		public string DocVersion { get; set; }
		public string SrcLangName { get; set; }
		public string TgtLangName { get; set; }
		public string Max { get; set; }

		public const string CstrKB = "KB";
		public const string CstrdocVersion = "docVersion";
		public const string CstrsrcName = "srcName";
		public const string CstrtgtName = "tgtName";
		public const string Cstrmax = "max";

		public static MapOfMaps LoadXml(string strFilename)
		{
			if (!File.Exists(strFilename))
				EncConverters.ThrowError(ErrStatus.CantOpenReadMap, strFilename);

			var doc = XDocument.Load(strFilename);
			var mapOfMaps = new MapOfMaps();
			mapOfMaps.LoadFromXElement(doc.Elements().First());
			return mapOfMaps;
		}

		private void LoadFromXElement(XElement elemKb)
		{
			DocVersion = AdaptItKBReader.GetAttributeValue(elemKb, CstrdocVersion, "4");
			SrcLangName = AdaptItKBReader.GetAttributeValue(elemKb, CstrsrcName, null);
			TgtLangName = AdaptItKBReader.GetAttributeValue(elemKb, CstrtgtName, null);
			Max = AdaptItKBReader.GetAttributeValue(elemKb, Cstrmax, "1");

			foreach (var mapOfSourceWordElements in
				elemKb.Descendants(MapOfSourceWordElements.CstrMAP).Select(elemMap
					=> new MapOfSourceWordElements(elemMap)))
			{
				if (!ContainsKey(mapOfSourceWordElements.NumOfWordsPerPhrase))
					Add(mapOfSourceWordElements.NumOfWordsPerPhrase, mapOfSourceWordElements);
			}
		}

		public SourceWordElement AddCouplet(string strLhs, string strRhs)
		{
			Debug.Assert(!String.IsNullOrEmpty(strLhs) && !String.IsNullOrEmpty(strRhs));
			string[] astrLhsWords = strLhs.Split(AdaptItKBReader.CaSplitChars, StringSplitOptions.RemoveEmptyEntries);
			if (astrLhsWords.Length == 0)
				throw new ApplicationException(Properties.Resources.IDS_CantHaveEmptySourceWord);

			MapOfSourceWordElements mapOfSourceWordElements;
			if (!TryGetValue(astrLhsWords.Length, out mapOfSourceWordElements))
			{
				mapOfSourceWordElements = new MapOfSourceWordElements(astrLhsWords.Length);
				Add(astrLhsWords.Length, mapOfSourceWordElements);
			}

			return mapOfSourceWordElements.AddCouplet(strLhs, strRhs);
		}

		public void SaveFile(string strFilename)
		{
			// first make a backup
			if (!Directory.Exists(Path.GetDirectoryName(strFilename)))
				Directory.CreateDirectory(Path.GetDirectoryName(strFilename));

			// save it with an extra extn.
			const string CstrExtraExtnToAvoidClobberingFilesWithFailedSaves = ".bad";
			string strTempFilename = strFilename + CstrExtraExtnToAvoidClobberingFilesWithFailedSaves;

#if !NewWay
			// this new way of writing it involves a sorting pass with xslt
			XElement elem = GetXml(SrcLangName, TgtLangName);

			// create the root portions of the XML document and tack on the fragment we've been building
			var doc = new XDocument(
				new XDeclaration("1.0", "utf-8", "yes"),
				new XComment(Properties.Resources.AdaptItKbDescription));

			var xslt = new XslCompiledTransform();
			xslt.Load(XmlReader.Create(new StringReader(Properties.Resources.SortAIKB)));

			using (XmlWriter writer = doc.CreateWriter())
			{
				// Execute the transform and output the results to a writer.
				xslt.Transform(elem.CreateReader(), writer);
				writer.Close();
			}

			doc.Save(strTempFilename);
#else
			// create the root portions of the XML document and tack on the fragment we've been building
			var doc = new XDocument(
				new XDeclaration("1.0", "utf-8", "yes"),
				new XComment(Properties.Resources.AdaptItKbDescription),
				GetXml(SrcLangName, TgtLangName));

			// sort it (as AI does)
			var newTree = new XDocument();
			using (XmlWriter writer = newTree.CreateWriter())
			{
				// Load the style sheet.
				var xslt = new XslCompiledTransform();
				xslt.Load(XmlReader.Create(new StringReader(Properties.Resources.SortAIKB)));

				// Execute the transform and output the results to a writer.
				xslt.Transform(doc.CreateReader(), writer);
			}

			newTree.Save(strTempFilename);
#endif

			// backup the last version to appdata
			// Note: doing File.Move leaves the old file security settings rather than replacing them
			// based on the target directory. Copy, on the other hand, inherits
			// security settings from the target folder, which is what we want to do.
			if (File.Exists(strFilename))
				File.Copy(strFilename, GetBackupFilename(strFilename), true);
			File.Delete(strFilename);
			File.Copy(strTempFilename, strFilename, true);
			File.Delete(strTempFilename);
		}

		private static string GetBackupFilename(string strFilename)
		{
			return Application.UserAppDataPath + @"\Backup of " + Path.GetFileName(strFilename);
		}

		protected XElement GetXml(string strSrcLangName, string strTgtLangName)
		{
			/*
			  <KB docVersion="4" srcName="Greek" tgtName="English" max="1">
				<MAP mn="1">
				  <TU f="0" k="Δ">
					<RS n="1" a="ασδγδ" />
				  </TU>
				</MAP>
			  </KB>
			*/
			var elem = new XElement(CstrKB,
									new XAttribute(CstrdocVersion, 4),
									new XAttribute(CstrsrcName, SrcLangName),
									new XAttribute(CstrtgtName, TgtLangName),
									new XAttribute(Cstrmax, Max));

			foreach (var mapOfSourceWordElements in Values)
				elem.Add(mapOfSourceWordElements.GetXml);

			return elem;
		}

		public bool TryGetValue(string strSourceWord, out MapOfSourceWordElements mapOfSourceWordElements)
		{
			// this is a helper to find the proper MapOfSourceWordElements based on the
			//  source word. First we have to find the map to look into
			// first get the map for the number of words in the source string (e.g. "ke picche" would be map=2)
			Debug.Assert(!String.IsNullOrEmpty(strSourceWord));
			int nMapValue = strSourceWord.Split(AdaptItKBReader.CaSplitChars, StringSplitOptions.RemoveEmptyEntries).Length;
			if (ContainsKey(nMapValue))
			{
				mapOfSourceWordElements = this[nMapValue];
				return true;
			}

			mapOfSourceWordElements = null;
			return false;
		}

		/// <summary>
		/// This helper can be used to change the source word to some other value (which
		/// might include it being put into a different map)
		/// </summary>
		/// <param name="strOldSourceWord"></param>
		/// <param name="strNewSourceWord"></param>
		/// <returns></returns>
		public SourceWordElement ChangeSourceWord(string strOldSourceWord,
			string strNewSourceWord)
		{
			Debug.Assert(!String.IsNullOrEmpty(strOldSourceWord) && !String.IsNullOrEmpty(strNewSourceWord));
			MapOfSourceWordElements mapOfSourceWordElements;
			if (TryGetValue(strOldSourceWord, out mapOfSourceWordElements))
			{
				SourceWordElement sourceWordElement = mapOfSourceWordElements[strOldSourceWord];
				mapOfSourceWordElements.Remove(strOldSourceWord);

				string strTargetWord = "";
				if (sourceWordElement.Count > 0)
					strTargetWord = sourceWordElement[0].TargetWord;

				// copy over the first target form this way (since it's already done)
				SourceWordElement sourceWordElementNew = AddCouplet(strNewSourceWord,
																	strTargetWord);

				// then copy the rest over one-by-one
				for (int i = 1; i < sourceWordElement.Count; i++)
					sourceWordElementNew.Add(sourceWordElement[i]);

				return sourceWordElementNew;
			}
			return null;
		}
	}
}
