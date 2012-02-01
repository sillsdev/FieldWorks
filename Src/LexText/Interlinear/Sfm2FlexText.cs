using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Sfm2Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SilEncConverters40;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class is responsible for converting SFM files to the FlexText interlinear XML format that we
	/// know how to import.
	/// The importer is designed to import a single text per operation. Therefore, we produce the output
	/// from a single stream as a dictionary from text name to a stream from which the FlexText for a text
	/// of that name can be read.
	/// </summary>
	internal class Sfm2FlexText
	{
		private Dictionary<string, InterlinearMapping> m_mappings = new Dictionary<string, InterlinearMapping>();
		private XmlWriter m_writer;
		private ILgWritingSystemFactory m_wsf;
		private EncConverters m_encConverters;
		private ITsStrFactory m_tsf;
		private ByteReader m_reader;
		// true when we have already added a "words" element to the current "phrase". Meaningless if there is not a phrase open at all.
		private bool m_phraseHasWords;
		private bool m_textHasContent;
		private string m_pendingMarker; // marker pushed back because not the extra one we were looking for
		private byte[] m_pendingData; // data corresponding to m_pendingMarker.
		List<string> m_openElements = new List<string>();
		List<string> m_docStructure = new List<string>(new [] { "document", "interlinear-text", "paragraphs", "paragraph", "phrases", "phrase", "words", "word" });
		// set emptied when we open a new phrase recording non-repeatable item-type/writing system combinations that have already occurred.
		private HashSet<Tuple<InterlinDestination, string>> m_itemsInThisPhrase = new HashSet<Tuple<InterlinDestination, string>>();
		// set emptied when we open a new text recording non-repeatable item-type/writing system combinations that have already occurred.
		private HashSet<Tuple<InterlinDestination, string>> m_itemsInThisText = new HashSet<Tuple<InterlinDestination, string>>();

		public byte[] Convert(ByteReader reader, List<InterlinearMapping> mappings, ILgWritingSystemFactory wsf)
		{
			m_reader = reader;
			m_wsf = wsf;
			m_tsf = TsStrFactoryClass.Create();
			var output = new MemoryStream();
			m_writer = XmlWriter.Create(output, new XmlWriterSettings() { CloseOutput = true });
			WriteStartElement("document");
			foreach (var mapping in mappings)
				m_mappings[mapping.Marker] = mapping;
			string marker;
			byte[] data;
			byte[] badData;
			while (GetNextSfmMarkerAndData(out marker, out data))
			{
				InterlinearMapping mapping;
				if (!m_mappings.TryGetValue(marker, out mapping))
					continue; // ignore any markers we don't know.
				switch (mapping.Destination)
				{
						// Todo: many cases need more checks for correct state.
					default: // Ignored
						break;
					case InterlinDestination.Source:
						MakeRootItem(mapping, data, "source");
						break;
					case InterlinDestination.Abbreviation:
						MakeRootItem(mapping, data, "title-abbreviation");
						break;
					case InterlinDestination.Title:
						MakeRootItem(mapping, data, "title");
						break;
					case InterlinDestination.Comment:
						MakeRootItem(mapping, data, "comment");
						break;
					case InterlinDestination.ParagraphBreak:
						HandleParaBreak();
						break;
					case InterlinDestination.Reference:
						HandleReference(mapping, data);
						break;
					case InterlinDestination.Baseline:
						HandleBaseline(mapping, data);
						break;
					case InterlinDestination.FreeTranslation:
						MakeRepeatableItem(mapping, data, "gls", "phrase", m_itemsInThisPhrase);
						break;
					case InterlinDestination.LiteralTranslation:
						MakeRepeatableItem(mapping, data, "lit", "phrase", m_itemsInThisPhrase);
						break;
					case InterlinDestination.Note:
						MakeItem(mapping, data, "note", "phrase");
						break;
				}
			}
			m_writer.Close();
			var result = output.ToArray();
			if (Form.ActiveForm != null && (Form.ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				// write out the intermediate file
				var intermediatePath = Path.Combine(Path.GetDirectoryName(reader.FileName),
					Path.GetFileNameWithoutExtension(reader.FileName) + "-intermediate.xml");
				var stream = File.Create(intermediatePath);
				stream.Write(result, 0, result.Length);
				stream.Close();
			}
			return result;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="mapping"></param>
		/// <param name="data"></param>
		/// <param name="itemType"></param>
		private void MakeRootItem(InterlinearMapping mapping, byte[] data, string itemType)
		{
			if (m_textHasContent)
				WriteStartElementIn("interlinear-text", "document");
			MakeRepeatableItem(mapping, data, itemType, "interlinear-text", m_itemsInThisText);
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

		private bool GetRawData(out string marker, out byte[] data)
		{
			byte[] badData;
			return m_reader.GetNextSfmMarkerAndData(out marker, out data, out badData);
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


		void WriteStartElementIn(string marker, string parentMarker)
		{
			AdjustDepth(parentMarker); //<-May initiate EndElement or StartElement calls as needed.
			WriteStartElement(marker);
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

		void WriteStartElement(string marker)
		{
			m_writer.WriteStartElement(marker);
			m_openElements.Add(marker);
			if (marker == "phrase")
			{
				m_phraseHasWords = false;
				m_itemsInThisPhrase.Clear();
			}
			else if (marker == "words")
			{
				m_phraseHasWords = true;
			}
			else if (marker == "interlinear-text")
			{
				m_textHasContent = false;
				m_itemsInThisText.Clear();
			}
		}

		void WriteEndElement()
		{
			m_writer.WriteEndElement();
			m_openElements.RemoveAt(m_openElements.Count -1);
		}

		private void HandleBaseline(InterlinearMapping mapping, byte[] data)
		{
			m_textHasContent = true;
			var text = GetString(data, mapping).Trim();

			var ws = m_wsf.get_Engine(mapping.WritingSystem).Handle; // don't use GetWsFromStr, fails if not a known WS
			var tss = m_tsf.MakeString(text, ws);
			var wordmaker = new WordMaker(tss, m_wsf);
			int ichLast = 0;
			int ichMin, ichLim;
			while (true)
			{
				var word = wordmaker.NextWord(out ichMin, out ichLim);
				if (word == null)
					ichMin = text.Length;
				if (ichLast < ichMin)
				{
					var punct = text.Substring(ichLast, ichMin - ichLast).Trim();
					if (punct.Length > 0)
					MakeWord(mapping, punct, "punct");
				}
				ichLast = ichLim;
				if (word != null)
					MakeWord(mapping, word.Text, "txt");
				else
					break;
			}
		}

		private void HandleParaBreak()
		{
			WriteStartElementIn("paragraph", "paragraphs");
		}

		private void HandleReference(InterlinearMapping mapping, byte[] data)
		{
			WriteStartElementIn("phrase", "phrases");
			MakeItem(mapping, data, "reference-label", "phrase");
		}

		private void MakeWord(InterlinearMapping mapping, string text, string itemType)
		{
			if (m_phraseHasWords && m_openElements.LastOrDefault() == "phrase")
			{
				// A phrase is currently open and already has a (completed) <words> element;
				// we need a new phrase.
				// We can accomplish this simply by terminating the current one;
				//the new one is automatically opened by WriteStartElementIn
				WriteEndElement();
			}
			WriteStartElementIn("word", "words");
			MakeItem(mapping, text, itemType, "word");
			WriteEndElement();
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
		/// <param name="phraseHasElement"></param>
		private void MakeRepeatableItem(InterlinearMapping mapping, byte[] data, string itemType, string parentMarker,
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

		// True if a phrase is open (some child element may also be open)
		private bool ParentElementIsOpen(string parent)
		{
			return m_openElements.Count() > m_docStructure.IndexOf(parent);
		}

		private void MakeItem(InterlinearMapping mapping, byte[] data, string itemType, string parentMarker)
		{
			var text = GetString(data, mapping).Trim();
			MakeItem(mapping, text, itemType, parentMarker);
		}

		private void MakeItem(InterlinearMapping mapping, string text, string itemType, string parentMarker)
		{
			WriteStartElementIn("item", parentMarker);
			m_writer.WriteAttributeString("type", itemType);
			if (!string.IsNullOrEmpty(mapping.WritingSystem))
				m_writer.WriteAttributeString("lang", mapping.WritingSystem);
			m_writer.WriteString(text);
			WriteEndElement();
		}

		private string GetString(byte[] data, InterlinearMapping mapping)
		{
			if (string.IsNullOrEmpty(mapping.Converter))
				return Encoding.UTF8.GetString(data); // todo: use encoding converter if present in mapping
			if (m_encConverters == null)
				m_encConverters = new EncConverters();
			var converter = m_encConverters[mapping.Converter];
			return converter.ConvertToUnicode(data);
		}
	}
}
