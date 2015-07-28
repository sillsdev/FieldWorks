// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using Sfm2Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SilEncConverters40;

namespace SIL.FieldWorks.LexText.Controls
{

	/// <summary>
	/// These are the destinations we currently care about in SFM interlinear import.
	/// For each of these there should be a ksFldX that is its localizable name (see
	/// InterlinearSfmImportWizard.GetDestinationName()).
	/// It is public only because XmlSerializer requires everything to be.
	/// </summary>
	public enum InterlinDestination
	{
		Ignored, // pay no attention to this field (except it terminates the previous one).
		Id, // marks start of new text (has no data)
		Abbreviation, // maps to Text.Abbreviation (and may start new text)
		Title, // maps to Text.Name (inherited from CmMajorObject) (and may start new text)
		Source, // Text.Source (and may start new text)
		Comment, // Text.Description (and may start new text)
		ParagraphBreak, // causes us to start a new paragraph
		Reference, // forcees segment break and sets Segment.Reference
		Baseline, // Becomes part of the StTxtPara.Contents
		FreeTranslation, // Segment.FreeTranslation
		LiteralTranslation, // Segment.LiteralTranslation
		Note, // each generats a Segment.Note and is its content.
		Wordform,
		WordGloss
	}

	/// <summary>
	/// Simple class to record the bits of information we want about how one marker maps onto FieldWorks.
	/// This is serialized to form the .map file, so change with care.
	/// It is public only because XmlSerializer requires everything to be.
	/// </summary>
	[Serializable]
	public class InterlinearMapping : Sfm2FlexTextMappingBase
	{
		public InterlinearMapping()
		{
		}
		public InterlinearMapping(InterlinearMapping copyFrom)
			: base(copyFrom)
		{
		}
	}

	/// <summary>
	/// This converts Sfm to a subset of the FlexText xml standard that only deals with Words, their Glosses and their Morphology.
	/// This frag is special case (non-conforming) in that it can have multiple glosses in the same writing system.
	/// </summary>
	public class Sfm2FlexTextWordsFrag : Sfm2FlexTextBase<InterlinearMapping>
	{
		HashSet<Tuple<InterlinDestination, string>> m_txtItemsAddedToWord = new HashSet<Tuple<InterlinDestination, string>>();

		public Sfm2FlexTextWordsFrag()
			: base(new List<string>(new[] { "document", "word" }))
		{}
		protected override void WriteToDocElement(byte[] data, InterlinearMapping mapping)
		{

			switch (mapping.Destination)
			{
				// Todo: many cases need more checks for correct state.
				default: // Ignored
					break;
				case InterlinDestination.Wordform:
					var key = new Tuple<InterlinDestination, string>(mapping.Destination, mapping.WritingSystem);
					// don't add more than one "txt" to word parent element
					if (m_txtItemsAddedToWord.Contains(key) && ParentElementIsOpen("word"))
					{
						WriteEndElement();
						m_txtItemsAddedToWord.Clear();
					}
					MakeItem(mapping, data, "txt", "word");
					m_txtItemsAddedToWord.Add(key);
					break;
				case InterlinDestination.WordGloss:
					// (For AdaptIt Knowledge Base sfm) it is okay to add more than one "gls" with same writing system to word parent element
					// this is a special case and probably doesn't strictly conform to FlexText standard.
					MakeItem(mapping, data, "gls", "word");
					break;
			}
		}
	}

	/// <summary>
	/// Simple class to record the bits of information we want about how one marker maps onto FieldWorks.
	/// This is serialized to form the .map file, so change with care.
	/// It is public only because XmlSerializer requires everything to be.
	/// </summary>
	[Serializable]
	public class Sfm2FlexTextMappingBase
	{
		public Sfm2FlexTextMappingBase()
		{
		}
		public Sfm2FlexTextMappingBase(Sfm2FlexTextMappingBase copyFrom)
		{
			Marker = copyFrom.Marker;
			Destination = copyFrom.Destination;
			Converter = copyFrom.Converter;
			WritingSystem = copyFrom.WritingSystem;
			Count = copyFrom.Count;
		}
		public string Marker;
		public InterlinDestination Destination;
		public string WritingSystem;
		public string Converter;
		[XmlIgnore]
		public string Count;
	}

	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_writer is disposed at end of method that creates it")]
	public class Sfm2FlexTextBase<TMapping> where TMapping : Sfm2FlexTextMappingBase
	{
		protected Dictionary<string, TMapping> m_mappings = new Dictionary<string, TMapping>();
		protected EncConverters m_encConverters;
		protected ITsStrFactory m_tsf;
		protected ByteReader m_reader;
		protected ILgWritingSystemFactory m_wsf;
		protected XmlWriter m_writer;
		protected List<string> m_openElements = new List<string>();
		protected string m_pendingMarker; // marker pushed back because not the extra one we were looking for
		protected byte[] m_pendingData; // data corresponding to m_pendingMarker.

		IList<string> m_docStructure = new List<string>();

		public Sfm2FlexTextBase(IList<string> docStructure)
		{
			m_docStructure = docStructure;
		}

		public byte[] Convert(ByteReader reader, List<TMapping> mappings, ILgWritingSystemFactory wsf)
		{
			m_reader = reader;
			m_wsf = wsf;
			m_tsf = TsStrFactoryClass.Create();
			using (var output = new MemoryStream())
			{
				using (m_writer = XmlWriter.Create(output, new XmlWriterSettings() { CloseOutput = true }))
				{
					WriteStartElement("document");
					foreach (var mapping in mappings)
						m_mappings[mapping.Marker] = mapping;
					string marker;
					byte[] data;
					byte[] badData;
					while (GetNextSfmMarkerAndData(out marker, out data))
					{
						TMapping mapping;
						if (!m_mappings.TryGetValue(marker, out mapping))
							continue; // ignore any markers we don't know.
						WriteToDocElement(data, mapping);
					}
					m_writer.Close();
				}
				var result = output.ToArray();
				if (Form.ActiveForm != null && (Form.ModifierKeys & Keys.Shift) == Keys.Shift)
				{
					// write out the intermediate file
					var intermediatePath = Path.Combine(Path.GetDirectoryName(reader.FileName),
					Path.GetFileNameWithoutExtension(reader.FileName) + "-intermediate.xml");
					using (var stream = File.Create(intermediatePath))
					{
						stream.Write(result, 0, result.Length);
						stream.Close();
					}
				}
				return result;
			}
		}

		protected virtual void WriteStartElement(string marker)
		{
			m_writer.WriteStartElement(marker);
			m_openElements.Add(marker);
		}

		protected void WriteEndElement()
		{
			m_writer.WriteEndElement();
			m_openElements.RemoveAt(m_openElements.Count - 1);
		}

		protected virtual void WriteToDocElement(byte[] data, TMapping mapping)
		{
			// override
		}

		private bool GetNextSfmMarkerAndData(out string marker, out byte[] data)
		{
			if (m_pendingMarker != null)
			{
				marker = m_pendingMarker;
				data = m_pendingData;
				m_pendingMarker = null;
				return true;
			}
			return GetRawData(out marker, out data);
		}

		protected bool GetRawData(out string marker, out byte[] data)
		{
			byte[] badData;
			return m_reader.GetNextSfmMarkerAndData(out marker, out data, out badData);
		}

		/// <summary>
		/// Make an item of the type indicated by the mapping by first converting the given data using the mapping's converter.
		/// It should have the specified item type and parent marker.
		/// If there are following consecutive fields with the same marker combine them into a single item.
		/// If a phrase is open and already has this type of element, ignore this one.
		/// Set phraseHasElement to indicate that now it does.
		/// </summary>
		/// <param name="mapping"></param>
		/// <param name="data"></param>
		/// <param name="itemType"></param>
		/// <param name="parentMarker"></param>
		/// <param name="itemsInThisParent"></param>
		protected virtual void MakeRepeatableItem(TMapping mapping, byte[] data, string itemType, string parentMarker,
			HashSet<Tuple<InterlinDestination, string>> itemsInThisParent)
		{
			var text = GetString(data, mapping).Trim();
			byte[] moreData;
			while (GetMoreData(mapping.Marker, out moreData))
			{
				var moreText = GetString(moreData, mapping).Trim();
				if (string.IsNullOrEmpty(text))
					text = moreText;
				else
					text = text + " " + moreText;
			}
			var key = new Tuple<InterlinDestination, string>(mapping.Destination, mapping.WritingSystem);
			if (itemsInThisParent.Contains(key)
				&& ParentElementIsOpen(parentMarker))
			{
				return;
			}
			itemsInThisParent.Add(key);
			MakeItem(mapping, text, itemType, parentMarker);
		}

		/// <summary>
		/// Read one more marker and data from the input. If it is the continuation marker, return true and the data;
		/// otherwise save the data to be read later and return false.
		/// </summary>
		bool GetMoreData(string marker, out byte[] data)
		{
			string nextMarker;
			if (!GetRawData(out nextMarker, out data))
				return false;
			if (nextMarker == marker)
				return true;
			m_pendingData = data;
			m_pendingMarker = nextMarker;
			data = null;
			return false;
		}

		// True if a phrase is open (some child element may also be open)
		protected bool ParentElementIsOpen(string parent)
		{
			return m_openElements.Count > m_docStructure.IndexOf(parent);
		}

		protected void MakeItem(TMapping mapping, byte[] data, string itemType, string parentMarker)
		{
			var text = GetString(data, mapping).Trim();
			MakeItem(mapping, text, itemType, parentMarker);
		}

		protected void MakeItem(TMapping mapping, string text, string itemType, string parentMarker)
		{
			WriteStartElementIn("item", parentMarker);
			m_writer.WriteAttributeString("type", itemType);
			if (!string.IsNullOrEmpty(mapping.WritingSystem))
				m_writer.WriteAttributeString("lang", mapping.WritingSystem);
			m_writer.WriteString(text);
			WriteEndElement();
		}

		protected void WriteStartElementIn(string marker, string parentMarker)
		{
			AdjustDepth(parentMarker); //<-May initiate EndElement or StartElement calls as needed.
			WriteStartElement(marker);
		}

		protected string GetString(byte[] data, TMapping mapping)
		{
			if (string.IsNullOrEmpty(mapping.Converter))
				return Encoding.UTF8.GetString(data); // todo: use encoding converter if present in mapping
			if (m_encConverters == null)
				m_encConverters = new EncConverters();
			var converter = m_encConverters[mapping.Converter];
			return converter.ConvertToUnicode(data);
		}

		/// <summary>
		/// Do whatever start or end element calls are needed so that we are at the level where the current
		/// open element is parentMarker
		/// <note>This implementation precludes any element name re-use in the output format,
		/// i.e. a phrase element could not have a phrase element nested in it, or in any of its children</note>
		/// </summary>
		private void AdjustDepth(string parentMarker)
		{
			int depth = m_docStructure.IndexOf(parentMarker) + 1;
			while (m_openElements.Count > depth)
				WriteEndElement();
			while (m_openElements.Count < depth)
				WriteStartElement(m_docStructure[m_openElements.Count]);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="mapping"></param>
		/// <param name="data"></param>
		/// <param name="itemType"></param>
		protected virtual void MakeRootItem(TMapping mapping, byte[] data, string itemType)
		{
			// override
		}


	}
}
