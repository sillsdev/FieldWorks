using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents an output format that writes a simple, human-readable text representation of HC objects to
	/// the provided text writer.
	/// </summary>
	public class DefaultOutput : IOutput
	{
		protected TextWriter m_out;

		public DefaultOutput(TextWriter outWriter)
		{
			m_out = outWriter;
		}

		/// <summary>
		/// Gets the output text writer that is used to output results.
		/// </summary>
		/// <value>The output text writer.</value>
		public TextWriter Out
		{
			get
			{
				return m_out;
			}
		}

		public virtual void MorphAndLookupWord(Morpher morpher, string word, bool prettyPrint, bool printTraceInputs)
		{
			m_out.WriteLine("Morph and Lookup: " + word);
			WordAnalysisTrace trace;
			ICollection<WordSynthesis> results = morpher.MorphAndLookupWord(word, out trace);
			m_out.WriteLine("*Results*");
			if (results.Count == 0)
			{
				m_out.WriteLine("None found");
				m_out.WriteLine();
			}
			else
			{
				foreach (WordSynthesis ws in results)
					Write(ws, prettyPrint);
			}

			if (prettyPrint && morpher.IsTracing)
			{
				m_out.WriteLine("*Trace*");
				Write(trace, prettyPrint, printTraceInputs);
				m_out.WriteLine();
			}
		}

		public virtual void Write(WordSynthesis ws, bool prettyPrint)
		{
			if (prettyPrint)
				PrettyPrintWordSynthesis(ws);
			else
				m_out.WriteLine(ws.ToString());
			m_out.WriteLine();
		}

		void PrettyPrintWordSynthesis(WordSynthesis ws)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("ID: ");
			sb.AppendLine(ws.Root.ID);
			sb.Append("POS: ");
			sb.AppendLine(ws.POS.Description);

			sb.Append("Morphs: ");
			bool firstItem = true;
			foreach (Morph morph in ws.Morphs)
			{
				string gl = morph.Allomorph.Morpheme.Gloss == null ? "?" : morph.Allomorph.Morpheme.Gloss.Description;
				string shapeStr = ws.Stratum.CharacterDefinitionTable.ToString(morph.Shape, ModeType.SYNTHESIS, false);
				int len = Math.Max(shapeStr.Length, gl.Length);
				if (len > 0)
				{
					if (!firstItem)
						sb.Append(' ');
					sb.Append(shapeStr.PadRight(len));
					firstItem = false;
				}
			}
			sb.AppendLine();
			sb.Append("Gloss:  ");
			firstItem = true;
			foreach (Morph morph in ws.Morphs)
			{
				string gl = morph.Allomorph.Morpheme.Gloss == null ? "?" : morph.Allomorph.Morpheme.Gloss.Description;
				string shapeStr = ws.Stratum.CharacterDefinitionTable.ToString(morph.Shape, ModeType.SYNTHESIS, false);
				int len = Math.Max(shapeStr.Length, gl.Length);
				if (len > 0)
				{
					if (!firstItem)
						sb.Append(' ');
					sb.Append(gl.PadRight(len));
					firstItem = false;
				}
			}
			sb.AppendLine();
			sb.Append("MPR Features: ");
			sb.AppendLine(ws.MPRFeatures.ToString());
			sb.Append("Head Features: ");
			sb.AppendLine(ws.HeadFeatures.ToString());
			sb.Append("Foot Features: ");
			sb.Append(ws.FootFeatures.ToString());
			m_out.WriteLine(sb.ToString());
		}

		public virtual void Write(Trace trace, bool prettyPrint, bool printTraceInputs)
		{
			Set<int> lineIndices = new Set<int>();
			PrintTraceRecord(trace, 0, lineIndices, printTraceInputs);
		}

		void PrintTraceRecord(Trace trace, int indent, Set<int> lineIndices, bool printTraceInputs)
		{
			m_out.WriteLine(trace.ToString(printTraceInputs));
			for (int i = 0; i < trace.ChildCount; i++)
			{
				PrintIndent(indent, lineIndices);
				m_out.WriteLine("|");
				PrintIndent(indent, lineIndices);
				m_out.Write("+-");
				if (i != trace.ChildCount - 1)
					lineIndices.Add(indent);
				PrintTraceRecord(trace.GetChildAt(i), indent + 2, lineIndices, printTraceInputs);
				if (i != trace.ChildCount - 1)
					lineIndices.Remove(indent);
			}
		}

		void PrintIndent(int indent, Set<int> lineIndices)
		{
			for (int i = 0; i < indent; i++)
				m_out.Write(lineIndices.Contains(i) ? "|" : " ");
		}

		public virtual void Write(MorphException me)
		{
			m_out.WriteLine("Morph Error: " + me.Message);
			m_out.WriteLine();
		}

		public virtual void Write(LoadException le)
		{
			m_out.WriteLine("Load Error: " + le.Message);
			m_out.WriteLine();
		}

		public virtual void Flush()
		{
			m_out.Flush();
		}

		public virtual void Close()
		{
			m_out.Close();
		}
	}
}
