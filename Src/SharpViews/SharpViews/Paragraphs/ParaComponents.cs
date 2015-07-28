// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Paragraphs
{
	/// <summary>
	/// A map run represents a section of a paragraph: either a sequence of characters not including an ORC and having
	/// uniform properties and belonging to a single TextClientRun, or a non-text ClientRun, or the expansion of an ORC.
	/// It's main responsibility is to represent the relationship between a run of logical characters on the client side,
	/// and a run of rendering characters.
	/// </summary>
	public class MapRun : IRenderRun
	{
		/// <summary>
		/// Copy the MapRun except for substituting the new starts.
		/// MUST override in EVERY subclass, to ensure that the correct class is returned.
		/// </summary>
		public virtual MapRun CopyWithLogStart(int newLogStart, int newRenStart)
		{
			return new MapRun(newLogStart, ClientRun, newRenStart, Offset, ClientUniformRunIndex, RenderLength);
		}

		/// <summary>
		/// The start of the run in 'logical' characters from the start of the paragraph.
		/// </summary>
		public int LogStart { get; private set; }
		/// <summary>
		/// The logical length of the run.
		/// </summary>
		public virtual int LogLength
		{
			get { return RenderLength; }
		}
		/// <summary>
		/// The ClientRun of which the run is a part (or the whole). Null for the final, dummy run in the list that indicates the end.
		/// </summary>
		public IClientRun ClientRun { get; private set; }
		/// <summary>
		/// The start of the MapRun in rendering characters. Usually the same as LogStart, but where an ORC is expanded into more
		/// text (or none) it may be different.
		/// </summary>
		public int RenderStart { get; private set; }
		/// <summary>
		/// The rendering length of the run; for this base class, this is also the logical length.
		/// This is redundant information for the containing text source, except for the last run, since it is just the difference
		/// between RenderStart for this run and the next one. However, it greatly simplifies many bits of code. Also, without it, we
		/// either have to give the source an overall RenderLength, and figure the length of the last run using that instead of the
		/// start of the following segment, which would be very ugly; or we have to put an extra, dummy MapRun at the end of the array.
		/// For a typical paragraph with few runs, that wastes more memory than adding one variable to each run.
		/// </summary>
		public int RenderLength { get; private set; }

		/// <summary>
		/// Limit of rendered characters (index of following character).
		/// </summary>
		public int RenderLim { get { return RenderStart + RenderLength; } }
		/// <summary>
		/// The offset into UniformRunText[ClientUniformRunIndex] of the start of the MapRun. Zero for non-text client runs.
		/// This is in logical characters.
		/// </summary>
		public int Offset { get; private set; }
		/// <summary>
		/// Where ClientRun is a TextClientRun, gives the uniform run within that ClientRun which this MapRun is, or is part of.
		/// </summary>
		public int ClientUniformRunIndex { get; private set; }

		public MapRun(int logical, IClientRun clientRun, int render, int offset, int renderLength)
			: this(logical, clientRun, render, offset, 0, renderLength)
		{
		}

		public MapRun(int logical, IClientRun clientRun, int render, int offset, int clientUniformRunIndex, int renderLength)
		{
			LogStart = logical;
			ClientRun = clientRun;
			RenderStart = render;
			Offset = offset;
			ClientUniformRunIndex = clientUniformRunIndex;
			RenderLength = renderLength;
		}

		// The text of the run as rendered (except that a box comes back as \xfffc)
		public virtual string RenderText
		{
			get
			{
				var textRun = ClientRun as TextClientRun;
				if (textRun == null)
					return "\xfffc";
				string chunk = textRun.UniformRunText(ClientUniformRunIndex);
				return chunk.Substring(Offset, RenderLength);
			}
		}

		public virtual LgCharRenderProps Chrp
		{
			get { return Styles.Chrp; }
		}

		// Since a para run represents a uniform block of characters, it has a single AssembledStyles.
		public virtual AssembledStyles Styles
		{
			get { return StylesFromUniformRun(ClientRun, ClientUniformRunIndex); }
		}

		// The writing system is also uniform.
		public int Ws { get { return Chrp.ws; } }

		internal AssembledStyles StylesFromUniformRun(IClientRun run, int irun)
		{
			var textRun = run as TextClientRun;
			if (textRun != null)
				return textRun.UniformRunStyles(ClientUniformRunIndex);
			return ((Box)run).Style;
		}

		/// <summary>
		/// Return true if the run displays as a single box.
		/// </summary>
		internal bool IsBox
		{
			get
			{
				return Box != null;
			}
		}

		public virtual Box Box
		{
			get
			{
				if (ClientRun is Box)
					return ClientRun as Box;
				return null;
			}
		}

		internal InsertionPoint SelectAt(ParaBox para, int ich, bool asscociatePrevious)
		{
			throw new NotImplementedException();
		}
	}
	/// <summary>
	/// MapRun for the special run we make when we encounter an ORC. It always corresponds to exactly one logical character, the orc.
	/// </summary>
	internal class OrcMapRun : MapRun
	{
		public OrcMapRun(int logical, IClientRun clientRun, int render, int offset, int clientUniformRunIndex, int renderLength, IClientRun orcExpansion)
			: base(logical, clientRun, render, offset, clientUniformRunIndex, renderLength)
		{
			OrcExpansion = orcExpansion;
		}

		public override int LogLength
		{
			get
			{
				return 1;
			}
		}

		/// <summary>
		/// Copy the MapRun except for substituting the new starts.
		/// </summary>
		public override MapRun CopyWithLogStart(int newLogStart, int newRenStart)
		{
			return new OrcMapRun(newLogStart, ClientRun, newRenStart, Offset, ClientUniformRunIndex, RenderLength, OrcExpansion);
		}

		/// <summary>
		/// What the ORC expanded to.
		/// </summary>
		public IClientRun OrcExpansion { get; private set; }

		public override string RenderText
		{
			get
			{
				TextClientRun orxExp = OrcExpansion as TextClientRun;
				if (orxExp == null)
					return "\xfffc";
				return orxExp.UniformRunText(0); // Curreently we don't support multi-run ClientRuns generated from ORCs.
			}
		}

		public override AssembledStyles Styles
		{
			get
			{
				return StylesFromUniformRun(OrcExpansion, 0);
			}
		}

		public override Box Box
		{
			get
			{
				if (OrcExpansion is Box)
					return OrcExpansion as Box;
				return null;
			}
		}
	}

	/// <summary>
	/// A subclass used to display a substitute for an empty string. The clientRun is always empty.
	/// </summary>
	internal class SubstituteMapRun : MapRun
	{
		private string m_substitute;
		private AssembledStyles m_substituteStyle;

		public SubstituteMapRun(int logical, IClientRun clientRun, int render, string substitute, AssembledStyles substituteStyle) :
			base(logical, clientRun, render, 0, substitute.Length)
		{
			Debug.Assert(clientRun.Length == 0);
			m_substitute = substitute;
			m_substituteStyle = substituteStyle;
		}

		/// <summary>
		/// Copy the MapRun except for substituting the new starts.
		/// </summary>
		public override MapRun CopyWithLogStart(int newLogStart, int newRenStart)
		{
			return new SubstituteMapRun(newLogStart, ClientRun, newRenStart, m_substitute, m_substituteStyle);
		}

		/// <summary>
		/// A substitute map run ALWAYS represents an empty underlying string.
		/// </summary>
		public override int LogLength
		{
			get
			{
				return 0;
			}
		}

		public override string RenderText
		{
			get
			{
				return m_substitute;
			}
		}

		public override AssembledStyles Styles
		{
			get
			{
				return m_substituteStyle;
			}
		}
	}

	/// <summary>
	/// RenderRuns represent either a single box or a run of contiguous 'rendering' characters which may differ in some properties
	/// but which share the same writing system.
	/// </summary>
	public interface IRenderRun
	{
		/// <summary>
		/// Start of the run (rendering characters of the whole paragraph)
		/// </summary>
		int RenderStart { get; }
		/// <summary>
		/// Length of the run (rendering characters)
		/// </summary>
		int RenderLength { get; }

		/// <summary>
		/// Should always be RenderStart + RenderLength; provided for convenience.
		/// </summary>
		int RenderLim { get; }

		/// <summary>
		/// If this run corresponds to a single box, return it, otherwise, return null.
		/// </summary>
		Box Box { get; }

		/// <summary>
		/// If the run does not have a box, it represents a sequence of (non-ORC) charcters
		/// in the same writing system; this gives the writing system.
		/// </summary>
		int Ws { get; }
	}

	class SimpleRenderRun : IRenderRun
	{
		public int RenderStart { get; private set; }

		public int RenderLength { get; private set; }

		public int RenderLim
		{
			get { return RenderStart + RenderLength; }
		}

		public virtual Box Box
		{
			get { return null; }
		}

		protected SimpleRenderRun(int start, int length)
		{
			RenderStart = start;
			RenderLength = length;
		}

		public virtual int Ws { get { return 0; } }
	}

	class GroupRenderRun : SimpleRenderRun
	{
		private int m_ws;
		public GroupRenderRun(int start, int length, int ws): base(start, length)
		{
			m_ws = ws;
		}

		public override int Ws
		{
			get
			{
				return m_ws;
			}
		}
	}

}
