using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// Delegate used to interpret ORC in TextClientRun.
	/// </summary>
	public delegate ClientRun OrcInterpreter(TextClientRun run, int offset);
	/// <summary>
	/// A TextSource represents the contents of a paragraph in several ways. Initially, and from the point of view of a client,
	/// a paragraph is made up of a sequence of ClientRuns, which are passed as a list to the constructor. Some ClientRuns are
	/// inherently boxes (indeed, the base interface ClientRun is just a marker interface which certain box classes claim to implement).
	/// Some are TextClientRuns, which represent sequences of characters.
	/// TextClientRuns may have UniformRuns, each of which represents a sequence of characters having uniform properties. These are not
	/// actual objects, just a conceptual bundle of properties which may be obtained from a TextClientRun by passing an index.
	/// An UniformRun may, in turn, contain object replacement characters (ORCs, 0xfffc). These get interpreted (by a delgate passed to the
	/// TextSource) into further ClientRuns, either boxes or TextClientRuns, currently limited to non-empty TextClientRuns with only one
	/// unirofm run. The process of inserting these replacements leads to the 'rendering' character sequuence, containing the non-ORC
	/// original characters, the characters produced by expanding the original ORCs into text, and a single ORC as a placeholder
	/// for each box.
	/// The paragraph is further represented as a sequence of MapRuns. A MapRun represents a sequence of 'rendering' characters
	/// having uniform properties, belonging to the same ClientRun, and further broken down so that there is a single MapRun for each box
	/// and ORC in the original. Thus, on the rendering side, a MapRun is either a single ORC (corresponding to a box) or a
	/// sequence of non-ORC characters belonging to the same ClientRun and having uniform properties. MapRuns also represent the
	/// relationship between the original ('logical') characters and the rendering ones, since each MapRun stores not only an
	/// offset into the rendering sequence, but one into the logical sequence as well.
	/// The next stage is to group adjacent MapRuns which represent text on the rendering side into groups, producing a sequence
	/// of RenderRuns each of which is either a single box or a run of contiguous characters which may differ in some properties
	/// but which share the same writing system.
	/// </summary>
	public class TextSource : IVwTextSource
	{
		internal List<ClientRun> ClientRuns { get; set; }

		MapRun[] m_runs;

		/// <summary>
		/// The individual runs of the paragraph. Currently this is public only to support testing.
		/// </summary>
		public MapRun[] Runs
		{
			get
			{
				if (m_runs == null)
					ComputeRuns();
				return m_runs;
			}
			private set
			{
				m_runs = value;
			}
		}

		/// <summary>
		/// Insert another run. This causes the MapRuns to be recalculated when needed, but does not redo layout
		/// or update display; it is intended for use by the ViewBuilder during box construction.
		/// May also be used during later edits, but then the caller is responsible to relayout.
		/// </summary>
		internal void InsertRun(int index, ClientRun run)
		{
			ClientRuns.Insert(index, run);
			AdjustClientRunIndexes(index); // Enhance: want a test that shows this is needed.
			Runs = null; // ensure recalculated when needed.
		}

		/// <summary>
		/// Remove some of your runs. This causes the MapRuns to be recalculated when needed, but does not redo layout
		/// or update display; the caller is responsible to relayout.
		/// </summary>
		internal void RemoveRuns(int first, int count)
		{
			ClientRuns.RemoveRange(first, count);
			AdjustClientRunIndexes(first);
			Runs = null; // ensure recalculated when needed.
		}

		private void AdjustClientRunIndexes(int first)
		{
			for (int i = first; i < ClientRuns.Count; i++ )
			{
				var textRun = ClientRuns[i] as TextClientRun;
				if (textRun == null)
					continue; // eventually may somehow be hookups for child boxes?
				var hookup = textRun.Hookup;
				if (hookup == null)
					continue;
				hookup.ClientRunIndex = i;
			}
		}

		OrcInterpreter Interpreter { get; set; }

		public TextSource(List<ClientRun> runs)
			: this(runs, null)
		{}

		public TextSource(List<ClientRun> runs, OrcInterpreter interpreter)
		{
			ClientRuns = runs;
			Interpreter = interpreter;
		}

		/// <summary>
		/// Make yourself equivalent to the passed source.
		/// </summary>
		/// <param name="other"></param>
		internal void Copyfrom(TextSource other)
		{
			ClientRuns = other.ClientRuns;
			Runs = other.Runs;
			Interpreter = other.Interpreter;
		}

		/// <summary>
		/// This private constructor is used by routines that make a modified copy of the recipient.
		/// </summary>
		private TextSource()
		{}

		private const char orc = '\xfffc';

		/// <summary>
		/// Create the Runs array which maps logical and rendering indexes to paragraph runs and offsets (and each other).
		/// Enhance JohnT: Not sure what we should do here (so no tests cover this) when an ORC expands to an empty string.
		/// Logically this is an empty run, which should disappear if adjacent to other text. Should it disappear anyway?
		/// </summary>
		private void ComputeRuns()
		{
			List<MapRun> runs = new List<MapRun>(ClientRuns.Count + 2);
			int ichLog = 0;
			int ichRen = 0;
			foreach (ClientRun clientRun in ClientRuns)
			{
				AddMapRunsForClientRun(clientRun, runs, ref ichLog, ref ichRen);
			}
			RemoveUnwantedRuns(runs);
			Runs = runs.ToArray();
		}

		private void AddMapRunsForClientRun(ClientRun input, List<MapRun> runs, ref int ichLog, ref int ichRen)
		{
			TextClientRun textRun = input as TextClientRun;
			if (textRun == null)
			{
				// inserting a box!
				runs.Add(new MapRun(ichLog, input, ichRen, 0, 1));
				ichLog++;
				ichRen++;
				return;
			}
			if (textRun.Length == 0 && textRun.Substitute != null)
			{
				runs.Add(new SubstituteMapRun(ichLog, textRun, ichRen, textRun.Substitute, textRun.SubstituteStyle));
				ichRen += textRun.Substitute.Length;
				return;
			}
			int cRuns = textRun.UniformRunCount;
			Debug.Assert(cRuns > 0, "we can't replace a client run reliably if it has no uniform runs");
			for (int irun = 0; irun < cRuns; irun++)
			{
				AddRunsForUniformRun(textRun, runs, ref ichLog, ref ichRen, irun);
			}
		}

		private void AddRunsForUniformRun(TextClientRun textRun, List<MapRun> runs, ref int ichLog, ref int ichRen, int irun)
		{
			int offset = 0;
			string contents = textRun.UniformRunText(irun);
			int orcIndex = contents.IndexOf(orc);
			int len = textRun.UniformRunLength(irun);
			int fullLength = len;
			while (orcIndex >= 0)
			{
				// Make a run for the (possibly empty) text before the ORC
				len = fullLength - orcIndex - 1;
				runs.Add(new MapRun(ichLog, textRun, ichRen, offset, irun, orcIndex));
				ichLog += orcIndex;
				ichRen += orcIndex;
				offset += orcIndex;

				// Make a run for the ORC itself.
				ClientRun orcRun = Interpreter(textRun, offset);
				TextClientRun textOrcRun = orcRun as TextClientRun;
				if (textOrcRun == null)
				{
					// interpreted as a box! Need to put the ORC run (the box) in as the contents of
					// the run.
					runs.Add(new OrcMapRun(ichLog, orcRun, ichRen, offset, irun, 1, orcRun));
					ichRen++;
				}
				else
				{
					int renderLength = textOrcRun.UniformRunLength(0); // not yet supporting multi-run TextClientRuns for ORCs.
					runs.Add(new OrcMapRun(ichLog, textRun, ichRen, offset, irun, renderLength, orcRun));
					Debug.Assert(textOrcRun.UniformRunCount == 1);
					ichRen += renderLength;
				}
				offset++;
				ichLog++;
				orcIndex = contents.IndexOf(orc, orcIndex + 1);
			}
			// Make a (possibly empty) run for any text left over (most commonly the whole contents)
			runs.Add(new MapRun(ichLog, textRun, ichRen, offset, irun, len));
			ichLog += len;
			ichRen += len;
			offset += len;
		}

		// Remove empty runs adjacent to non-empty text ones.
		private void RemoveUnwantedRuns(List<MapRun> runs)
		{
			for (int irun = 0; irun < runs.Count; )
			{
				MapRun run = runs[irun];
				if (run.LogLength > 0 || run.RenderLength > 0)
				{
					irun++;
					continue; // target run is not empty (or displaying a substitute), don't consider removing it.
				}
				if (irun + 1 < runs.Count)
				{
					MapRun runNext = runs[irun + 1];
					if (runNext.ClientRun is TextClientRun && runNext.LogLength > 0)
					{
						// following text run is non-empty, remove this empty run.
						runs.RemoveAt(irun);
						continue;
					}
				}
				if (irun > 0 && runs[irun - 1].ClientRun is TextClientRun)
				{
					// there is a preceding text run, remove this run, even if preceding one is empty; don't want two adjacent empties.
					runs.RemoveAt(irun);
					continue;
				}
				irun++;
			}
		}

		#region IVwTextSource Members

		/// <summary>
		/// Fetch some of the text. The ich values are in rendered characters.
		/// </summary>
		public void Fetch(int ichMin, int ichLim1, IntPtr ptr)
		{
			int ichLim = Math.Min(ichLim1, Length);
			string text = GetRenderText(ichMin, ichLim - ichMin);
			IntPtr current = (IntPtr)ptr;
			for (int i = 0; i < text.Length; i++)
			{
				Marshal.WriteInt16(current, i * sizeof(short), (short)text[i]);
			}
		}

		public string GetRenderText(int ichMin, int length)
		{
			if (Runs.Length == 0)
			{
				if (ichMin != 0)
					throw new ArgumentException("ichMin is beyond the length of the (empty) paragraph.");
				if (length != 0)
					throw new ArgumentException("asked for characters from an empty paragraph");
				return "";
			}
			int ichLim = ichMin + length;
			int firstRunIndex = RunContaining(ichMin);
			int lastRunIndex = RunContaining(ichLim);
			MapRun firstRun = Runs[firstRunIndex];
			MapRun lastRun = Runs[lastRunIndex];
			int ichMinRun = ichMin - firstRun.RenderStart;
			int ichLimRun = ichLim - lastRun.RenderStart;
			string text;
			if (firstRunIndex == lastRunIndex)
				text = GetRunText(firstRun, ichMinRun, ichLimRun);
			else
			{
				StringBuilder builder = new StringBuilder(GetRunText(firstRun, ichMinRun, firstRun.RenderLength));
				for (int i = firstRunIndex + 1; i < lastRunIndex; i++)
				{
					MapRun run = Runs[i];
					builder.Append(run.RenderText);
				}
				builder.Append(GetRunText(lastRun, 0, ichLimRun));
				text = builder.ToString();
			}
			return text;
		}

		/// <summary>
		/// Get the text from the specified run. indexes are offsets into the run, in render characters.
		/// </summary>
		/// <param name="run"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <returns></returns>
		string GetRunText(MapRun run, int ichMin, int ichLim)
		{
			return run.RenderText.Substring(ichMin, ichLim - ichMin);
		}

		// Determine the run of the character with the specified render index.
		internal int RunContaining(int ichRen)
		{
			Debug.Assert(ichRen >= 0);
			MapRun key = new MapRun(0, null, ichRen, 0, 0);
			int index = Array.BinarySearch(Runs, key, RenderStartComparer);
			if (index < 0) // exact match
			{
				index = ~index; // now the index of the first run with RenderStart > ichRen
				index--; // so this one contains the character
			}
			return index;
		}

		/// <summary>
		/// Return the client run containing the indicated character (the one at position ichLog if associatePrevious
		/// is false, the previous one if it is true...except we always answer the last one if ichLog is after its start.
		/// Also return the start of the indicated client run.
		/// </summary>
		int ClientRunContaining(int ichLog, bool associatePrevious, out int ichStartOut)
		{
			Debug.Assert(ichLog >= 0);
			int ichStart = 0; // updated to be the start of the ith run in each iteration.
			for (int i = 0; i < ClientRuns.Count; i++)
			{
				int ichEnd = ichStart + ClientRuns[i].Length;
				if (ichLog < ichEnd)
				{
					ichStartOut = ichStart;
					return i; // must be in this segment.
				}
				else if (ichLog == ichEnd)
				{
					// at the boundary, if we want to associate previous or i is the last run return it.
					if (associatePrevious || i == ClientRuns.Count - 1)
					{
						ichStartOut = ichStart;
						return i; // must be in this segment.

					}
					else
					{
						ichStartOut = ichEnd;
						return i + 1;
					}
				}
				ichStart = ichEnd;
			}
			throw new ArgumentOutOfRangeException("ichLog", ichLog, "index passed to ClientRunContaining should be within length of paragraph which is " + ichStart);
		}
		/// <summary>
		/// Return the client run containing the indicated character (the one at position ichRenTarget if associatePrevious
		/// is false, the previous one if it is true...except we always answer the last one if ichRen is after its start.
		/// Also return the character offset into the indicated clientrun.
		/// </summary>
		int ClientRunContainingRender(int ichRenTarget, bool associatePrevious, out int offset)
		{
			if (m_runs.Length == 0)
			{
				offset = 0;
				return 0;
			}
			int iMapRun = 0;
			while (iMapRun < m_runs.Length && m_runs[iMapRun].RenderLim < ichRenTarget)
				iMapRun++;
			// every earlier run ends strictly before ichRenTarget and cannot contain the target character
			if (iMapRun >= m_runs.Length)
			{
				// it's at or beyond the end of the paragraph
				var result = m_runs.Last().ClientRun;
				offset = result.Length;
				return m_runs.Length - 1;
			}
			var mapRun = m_runs[iMapRun];
			// since we didn't run out of runs, any previous run must end strictly before ichRenTarget.
			// However, it's possible, if ichRenTarget is equal to the limit of this run, that
			// the character we want is in the next run.
			if (iMapRun < m_runs.Length - 1 && ichRenTarget == mapRun.RenderLim && !associatePrevious)
			{
				iMapRun++; // it's at the start of the next run.
				mapRun = m_runs[iMapRun];
			}
			var clientRun = mapRun.ClientRun;
			if (mapRun.RenderLength == mapRun.LogLength)
			{
				// no complications: offset into the mapRun plus the offset of the mapRun into the client run.
				offset = ichRenTarget - mapRun.RenderStart + mapRun.Offset;
			}
			else
			{
				// So far all other runs have a logical length of zero or one.
				// Review JohnT: I'm not sure what it is most useful to return for an expanded ORC...possibly change the API so
				// we can return a range? Return start or end of it, whichever we are closest to?
				// for now we'll let both these cases be handled as at the start of the run.
				offset = mapRun.Offset;
			}
			return ClientRuns.IndexOf(clientRun);
		}

		/// <summary>
		/// If possible, find a non-empty client run which ends at the start position of the argument run.
		/// This is usually the run immediately before it but might be earlier if there are adjacent empty runs.
		/// Otherwise return null.
		/// </summary>
		internal StringClientRun NonEmptyStringClientRunEndingBefore(int clientRunIndex)
		{
			for (int index = clientRunIndex - 1; index >= 0; index--)
			{
				var possibleResult = ClientRuns[index];
				if (possibleResult.Length == 0)
					continue;
				if (possibleResult is StringClientRun)
					return possibleResult as StringClientRun;
				return null; // non-empty run that isn't a StringClientRun
			}
			return null; // ran out of previous runs
		}

		public void FetchSearch(int ichMin, int ichLim, IntPtr _rgchBuf)
		{
			throw new NotImplementedException();
		}

		public void GetCharProps(int ich, out LgCharRenderProps chrp, out int ichMin, out int ichLim)
		{
			int runIndex = RunContaining(ich);
			MapRun run = Runs[runIndex];
			ichMin = run.RenderStart;
			ichLim = ichMin + run.RenderLength;
			chrp = run.Chrp;
		}

		internal FwUnderlineType GetUnderlineInfo(int ich, out int underColor, out int ichLim)
		{
			int runIndex = RunContaining(ich);
			MapRun run = Runs[runIndex];
			ichLim = run.RenderStart + run.RenderLength;
			underColor = (int)run.Chrp.clrUnder;
			return (FwUnderlineType)run.Chrp.unt;
		}

		public void GetCharStringProp(int ich, int nId, out string _bstr, out int _ichMin, out int _ichLim)
		{
			throw new NotImplementedException();
		}

		public void GetParaProps(int ich, out LgParaRenderProps _chrp, out int _ichMin, out int _ichLim)
		{
			throw new NotImplementedException();
		}

		public void GetParaStringProp(int ich, int nId, out string _bstr, out int _ichMin, out int _ichLim)
		{
			throw new NotImplementedException();
		}

		public ITsString GetSubString(int ichMin, int ichLim)
		{
			throw new NotImplementedException();
		}

		private ILgWritingSystemFactory m_wsFactory;

		public ILgWritingSystemFactory GetWsFactory()
		{
			return m_wsFactory;
		}

		internal void SetWsFactory(ILgWritingSystemFactory wsFactory)
		{
			m_wsFactory = wsFactory;
		}
		/// <summary>
		/// Length (in rendered characters).
		/// </summary>
		public int Length
		{
			get
			{
				if (Runs.Length == 0)
					return 0;
				MapRun last = Runs[Runs.Length - 1];
				return last.RenderStart + last.RenderLength;
			}
		}

		public int LengthSearch
		{
			get { throw new NotImplementedException(); }
		}

		private class CompareRunsByLogStart : IComparer<MapRun>
		{
			public int Compare(MapRun x, MapRun y)
			{
				return x.LogStart.CompareTo(y.LogStart);
			}
		}

		static CompareRunsByLogStart LogStartComparer = new CompareRunsByLogStart();

		public int LogToRen(int ichLog)
		{
			Debug.Assert(ichLog >= 0);
			MapRun key = new MapRun(ichLog, null, 0,0, 0);
			int index = Array.BinarySearch(Runs, key, LogStartComparer);
			if (index < 0)
			{
				index = ~index ;
				// index is now the index of the first element with LogStart > ichLog (or Runs.Length, if it is greater than any LogStart in the array).
				index--; // if it doesn't match exactly we want the previous run (which contains this index)
			}
			MapRun run = Runs[index];
			return run.RenderStart + (ichLog - run.LogStart);
		}

		public int LogToSearch(int ichlog)
		{
			throw new NotImplementedException();
		}

		private class CompareRunsByRenderStart : IComparer<MapRun>
		{
			public int Compare(MapRun x, MapRun y)
			{
				return x.RenderStart.CompareTo(y.RenderStart);
			}
		}

		static CompareRunsByRenderStart RenderStartComparer = new CompareRunsByRenderStart();
		public int RenToLog(int ichRen)
		{
			Debug.Assert(ichRen >= 0);
			MapRun key = new MapRun(0, null, ichRen, 0, 0);
			int index = Array.BinarySearch(Runs, key, RenderStartComparer);
			if (index >= 0) // exact match
				return Runs[index].LogStart;
			index = ~index;
			// index is now the index of the first element with RenderStart > ichRen (or Runs.Length, if it is greater than any RenderStart in the array).

			MapRun run = Runs[index - 1]; // run containing the indicated position.
			// If this is an ORC run, we want its start index (all the render characters for the run correspond to that one ORC)
			if (run is OrcMapRun)
				return run.LogStart;
			// In ordinary runs, the logical and rendered characters correspond one for one.
			return run.LogStart + (ichRen - run.RenderStart);
		}

		public int RenToSearch(int ichRen)
		{
			throw new NotImplementedException();
		}

		public int SearchToLog(int ichSearch, bool fAssocPrev)
		{
			throw new NotImplementedException();
		}

		public int SearchToRen(int ichSearch, bool fAssocPrev)
		{
			throw new NotImplementedException();
		}

		#endregion

		/// <summary>
		/// Gets the sequence of runs (of characters in the same writing system) that need distinct handling in laying out the paragraph.
		/// </summary>
		public List<RenderRun> RenderRuns
		{
			get
			{
				List<RenderRun> result = new List<RenderRun>();
				int start = 0;
				while (start < Runs.Length)
				{
					MapRun first = Runs[start];
					if (first.IsBox)
					{
						// Boxes are always unique runs. Since MapRun implements RenderRun, it can actually be the RenderRun
						result.Add(first);
						start++;
						continue;
					}
					int ws = first.Chrp.ws;
					int end = start + 1;
					int length = first.RenderLength;
					while (end < Runs.Length)
					{
						MapRun current = Runs[end];
						if (current.IsBox)
							break; // current will have to be its own run
						if (current.Chrp.ws != ws)
							break; // can't merge these runs
						length += current.RenderLength;
						end++;
					}
					end--; // finishes loop indexing first run we can't merge
					if (start == end)
						result.Add(first);
					else
						result.Add(new GroupRenderRun(first.RenderStart, length, first.Ws));
					start = end + 1;
				}
				return result;
			}
		}

		internal string RenderText
		{
			get
			{
				using (ArrayPtr ptr = new ArrayPtr((Length) * 2 + 2))
				{
					Fetch(0, Length, ptr.IntPtr);
					return MarshalEx.NativeToString(ptr, Length, true);
				}

			}
		}


		internal InsertionPoint SelectAtEnd(ParaBox para)
		{
			if (ClientRuns.Count == 0)
				return null;
			return ClientRuns.Last().SelectAtEnd(para);
		}
		internal InsertionPoint SelectAtStart(ParaBox para)
		{
			if (ClientRuns.Count == 0)
				return null;
			return ClientRuns.First().SelectAtStart(para);
		}
		/// <summary>
		/// Make a selection at the specified character offset (associated with the previous character if associatePrevious is true).
		/// </summary>
		internal InsertionPoint SelectAt(ParaBox para, int ichLog, bool associatePreviousIn)
		{
			if (ClientRuns.Count == 0)
				return null;
			bool associatePrevious = associatePreviousIn;
			int ichStart;
			int irun = ClientRunContaining(ichLog, associatePrevious, out ichStart);
			var run = ClientRuns[irun] as TextClientRun;
			if (run == null && ichLog == ichStart && irun > 0)
			{
				// run irun is a box; try end of previous run
				irun--;
				run = ClientRuns[irun] as TextClientRun; // move back to start of previous run
				ichStart -= run.Length;
				associatePrevious = true;
			}
			else if (run == null && ichLog == ichStart + 1 && irun < ClientRuns.Count - 1)
			{
				// run irun is a box and something follows it; try to select there...
				irun++;
				ichStart += run.Length; // advance to end of current run
				run = ClientRuns[irun] as TextClientRun;
				associatePrevious = false;
			}
			if (run == null)
				return null; // Can't make a text selection if there's no text at the relevant spot.
			return run.SelectAt(para, ichLog - ichStart, associatePrevious);
		}
		/// <summary>
		/// Make a selection at the specified character offset (associated with the previous character if associatePrevious is true).
		/// </summary>
		internal InsertionPoint SelectAtRender(ParaBox para, int ichRen, bool associatePrevious)
		{
			if (ClientRuns.Count == 0)
				return null;
			int offset;
			int irun = ClientRunContainingRender(ichRen, associatePrevious, out offset);
			var run = ClientRuns[irun] as TextClientRun;
			if (run == null && irun > 0)
			{
				// run irun is a box; try end of previous run
				irun--;
				run = ClientRuns[irun] as TextClientRun;
				if (run != null)
					offset = run.Length;
				associatePrevious = true; // we're at the end of the only available text run at this point.
			}
			if (run == null && irun < ClientRuns.Count - 1)
			{
				// If we can't do the run before try the one after.
				irun++;
				run = ClientRuns[irun] as TextClientRun;
				offset = 0; // I think it always is, already, but make sure.
				associatePrevious = false; // we're at the start of the only available text run at this point.
			}
			if (run == null)
				return null; // Can't make a text selection if there's no text at the relevant spot.
			return run.SelectAt(para, offset, associatePrevious);
		}

		/// <summary>
		/// This is the basic way we make edits to paragraph contents.
		/// Optimize JohnT: take advantage of existing information in the old source to avoid
		/// recomputing, especially for unchanged client runs.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="newRun"></param>
		/// <returns></returns>
		internal SourceChangeDetails ClientRunChanged(int index, ClientRun newRun)
		{
			return new ClientRunChangedMethod(this, index, newRun).Run();
		}

		class ClientRunChangedMethod
		{
			private int m_clientRunIndex;
			ClientRun m_newRun;
			private TextSource m_source;
			private ClientRun m_oldRun; // the one we are replacing.
			private List<MapRun> m_newRuns;
			private TextSource m_newSource;
			private SourceChangeDetails m_result;
			// range in m_source.Runs of items whose client run is m_oldRun
			private int m_replacedRunMin;
			private int m_replacedRunLim;
			// end of range of new MapRuns made from newRun; start is also m_replacedRunMin;
			private int m_newRunLim;
			public ClientRunChangedMethod(TextSource source, int index, ClientRun newRun)
			{
				m_clientRunIndex = index;
				m_newRun = newRun;
				m_source = source;
				m_oldRun = m_source.ClientRuns[m_clientRunIndex];
				m_newRuns = new List<MapRun>(m_source.Runs.Length);
				m_newSource = new TextSource();
				m_result = new SourceChangeDetails(m_newSource);
			}

			public SourceChangeDetails Run()
			{
				var newClientRuns = new List<ClientRun>(m_source.ClientRuns);
				newClientRuns[m_clientRunIndex] = m_newRun;
				m_newSource.ClientRuns = newClientRuns;
				m_newSource.Interpreter = m_source.Interpreter;
				bool madeNewRuns = false;
				int ichLog = 0;
				int ichRen = 0;
				int isourceRun = -1;
				foreach (var mr in m_source.Runs)
				{
					isourceRun++;
					if (mr.ClientRun == m_oldRun)
					{
						if (!madeNewRuns)
						{
							// First run we are replacing.
							m_replacedRunMin = isourceRun;
							madeNewRuns = true;
							m_source.AddMapRunsForClientRun(m_newRun, m_newRuns, ref ichLog, ref ichRen);
							m_newRunLim = m_newRuns.Count;
						}
						m_replacedRunLim = isourceRun + 1;
					}
					else
					{
						m_newRuns.Add(mr.CopyWithLogStart(ichLog, ichRen));
						ichLog += mr.LogLength;
						ichRen += mr.RenderLength;
					}
				}
				ComputeDifferences();
				Debug.Assert(madeNewRuns, "we should have found the client run");
				m_source.RemoveUnwantedRuns(m_newRuns);

				m_newSource.Runs = m_newRuns.ToArray();
				return m_result;
			}
			void ComputeDifferences()
			{
				// First approximation: all the characters replaced.
				int ichStartChange = m_source.Runs[m_replacedRunMin].RenderStart;
				int ichLimChangeOld = m_source.Runs[m_replacedRunLim - 1].RenderLim;
				int ichLimChangeNew = m_newRuns[m_newRunLim - 1].RenderLim;
				ichStartChange = TrimMatchingAtStart(ichStartChange);
				// If we found a difference, also trim back from end; if not, indicate no diffs
				// at the start position. (Careful if you think about removing this check;
				// running both trim methods when there are no diffs produces a start
				// index at the end and negative character counts!)
				if (ichStartChange < ichLimChangeNew || ichStartChange < ichLimChangeOld)
				{
					TrimMatchingAtEnd(ref ichLimChangeNew, ref ichLimChangeOld);
					m_result.StartChange = ichStartChange;
					m_result.InsertCount = ichLimChangeNew - ichStartChange;
					m_result.DeleteCount = ichLimChangeOld - ichStartChange;
				}
				else
				{
					m_result.StartChange = m_source.Runs[m_replacedRunMin].RenderStart;
					m_result.InsertCount = 0;
					m_result.DeleteCount = 0;
				}
			}

			private void TrimMatchingAtEnd(ref int ichLimChangeNew, ref int ichLimChangeOld)
			{
				int ioldRun = m_replacedRunLim - 1;
				int inewRun = m_newRunLim - 1;
				while (ioldRun >= m_replacedRunMin && inewRun >= m_replacedRunMin)
				{
					var oldRun = m_source.Runs[ioldRun];
					if (oldRun.LogLength == 0)
					{
						ioldRun--;
						continue;
					}
					var newRun = m_newRuns[inewRun];
					if (newRun.LogLength == 0)
					{
						inewRun--;
						continue;
					}
					if (!oldRun.Chrp.Equals(newRun.Chrp))
						break;
					string oldText = oldRun.RenderText;
					string newText = newRun.RenderText;
					int ichOld = oldText.Length - 1;
					int ichNew = newText.Length - 1;
					for (; ichOld >= 0 && ichNew >= 0; ichOld--, ichNew--)
					{
						if (oldText[ichOld] != newText[ichNew])
							return; // break from both loops.
						ichLimChangeNew--;
						ichLimChangeOld--;
					}
					if (oldText.Length != newText.Length)
						break;
					ioldRun--;
					inewRun--;
				}
			}

			private int TrimMatchingAtStart(int ichStartChange)
			{
				int ioldRun = m_replacedRunMin;
				int inewRun = m_replacedRunMin;
				while (ioldRun < m_replacedRunLim && inewRun < m_newRunLim)
				{
					var oldRun = m_source.Runs[ioldRun];
					if (oldRun.LogLength == 0)
					{
						ioldRun++;
						continue;
					}
					var newRun = m_newRuns[inewRun];
					if (newRun.LogLength == 0)
					{
						inewRun++;
						continue;
					}
					if (!oldRun.Chrp.Equals(newRun.Chrp))
						break;
					string oldText = oldRun.RenderText;
					string newText = newRun.RenderText;
					int cch = Math.Min(oldText.Length, newText.Length);

					for (int ich = 0; ich < cch; ich++)
					{
						if (oldText[ich] != newText[ich])
							return ichStartChange; // exit BOTH loops!
						ichStartChange++;
					}
					if (oldText.Length != newText.Length)
						break;
					ioldRun++;
					inewRun++;
				}
				return ichStartChange;
			}
		}
	}
	/// <summary>
	/// This class represents the outcome of something that changes a text source. It includes a new text source and
	/// information about what changed.
	/// </summary>
	internal class SourceChangeDetails
	{
		public SourceChangeDetails(TextSource newSource)
		{
			NewSource = newSource;
		}
		public TextSource NewSource { get; private set; }

		/// <summary>
		/// Index of first render character that is different between old source and new one.
		/// </summary>
		public int StartChange { get; internal set; }
		/// <summary>
		/// Count of render characters deleted from old source to produce new one.
		/// </summary>
		public int DeleteCount { get; internal set; }
		/// <summary>
		/// Count of render characters inserted in old source to produce new one.
		/// </summary>
		public int InsertCount { get; internal set; }
	}
}