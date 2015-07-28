// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Paragraphs
{
	/// <summary>
	/// ClientRun represents something that can be inserted into a paragaph as a logical component. More specifically, it is one of the
	/// things that can be put into the list used to construct the TextSource for a paragraph. As well as the classes in this file,
	/// LeafBox and InnerPile implement this (marker) interface.
	/// </summary>
	public interface IClientRun
	{
		InsertionPoint SelectAtEnd(ParaBox para);
		InsertionPoint SelectAtStart(ParaBox para);
		// Length of the run (in logical characters)
		int Length { get; }
		AssembledStyles Style { get; }
		string Text { get; }

		/// <summary>
		/// Returns ClientRun Hookup for all ClientRuns except LeafBox, for which it will return null.
		/// </summary>
		LiteralStringParaHookup Hookup { get; set; }

		/// <summary>
		/// The writing system of the character at the (logical) index indicated, relative to the start of the client run.
		/// </summary>
		int WritingSystemAt(int index);

		string CharacterStyleNameAt(int index);
	}

	/// <summary>
	/// Used for things in a paragraph that contain text. Abstracts the behavior of TsString and ordinary strings that is important
	/// for paragraph layout.
	/// </summary>
	public abstract class TextClientRun : IClientRun
	{
		// The number of UniformRuns in the TextClientRun
		public abstract int UniformRunCount { get; }
		public abstract string UniformRunText(int irun);
		public abstract AssembledStyles UniformRunStyles(int irun);
		// count of logical characters from the start of the client run to the start of the irun'th uniformrun
		public abstract int UniformRunStart(int irun);
		// count of logical characters that make up the run.
		public abstract int UniformRunLength(int irun);
		public abstract string Text { get; }
		public virtual int WritingSystemAt(int index)
		{
			return Style.Ws;
		}

		public virtual string CharacterStyleNameAt(int index)
		{
			return Style.StyleName;
		}

		public AssembledStyles Style { get; internal set; } // set is only intended for use by internal subclasses.

		/// <summary>
		/// The total length of the client run, in logical characters.
		/// </summary>
		public virtual int Length
		{
			get
			{
				if (UniformRunCount == 0)
					return 0;
				return UniformRunStart(UniformRunCount - 1) + UniformRunLength(UniformRunCount - 1);
			}
		}


		// The hookup (if any) responsible for this.
		// It would be more elegant to put here the logic (currently in SelectAt) which makes a dummy one if it does not
		// exist, but currently there is no path to determine the required paragraph and index.
		public LiteralStringParaHookup Hookup { get; set; }

		/// <summary>
		/// Return an insertion point at specified position in THIS RUN within the paragraph.
		/// </summary>
		public InsertionPoint SelectAt(ParaBox para, int ichRun, bool associatePrevious)
		{
			LiteralStringParaHookup hookup = Hookup;
			if (hookup == null)
			{
				hookup = new LiteralStringParaHookup(null, para);
				hookup.ClientRunIndex = para.Source.ClientRuns.IndexOf(this);
			}
			return new InsertionPoint(hookup, ichRun, associatePrevious);

		}
		/// <summary>
		/// Return an insertion point at the end of the specified paragraph
		/// </summary>
		public InsertionPoint SelectAtEnd(ParaBox para)
		{
			// Review JohnT: do we want associatePrevious true or false if the paragraph is empty?
			return SelectAt(para, Length, true);
		}

		/// <summary>
		/// Return an insertion point at the start of the specified paragraph (of which this is the first run)
		/// </summary>
		public InsertionPoint SelectAtStart(ParaBox para)
		{
			// Review JohnT: do we want associatePrevious true or false if the paragraph is empty?
			return SelectAt(para, 0, false);
		}

		/// <summary>
		/// The alternative that should be displayed when the run is empty.
		/// </summary>
		public virtual string Substitute
		{
			get { return null;}
		}

		/// <summary>
		/// The styles to use in displaying the Substitute string.
		/// </summary>
		public virtual AssembledStyles SubstituteStyle
		{
			get { return null;}
		}

		/// <summary>
		/// Return true if the substitute string should be used. Typically if it exists and our own
		/// contents are not empty.
		/// </summary>
		public virtual bool ShouldUseSubstitute
		{
			get { return false; }
		}
	}

	/// <summary>
	/// Implementation of TextClientRun based on ITsString.
	/// </summary>
	public class MlsClientRun : TssClientRun
	{

		public MlsClientRun(IViewMultiString mls, AssembledStyles style)
			:base(mls.get_String(style.Ws), style)
		{
			Style = style;
		}

		/// <summary>
		/// Return an otherwise equivalent Tss client run that has the specified Contents.
		/// Subclasses should override to return the appropriate subclass and copy any additional information.
		/// </summary>
		internal virtual MlsClientRun CopyWithNewContents(IViewMultiString newContents)
		{
			var result = new MlsClientRun(newContents, Style);
			result.Hookup = Hookup;
			return result;
		}
	}

	class SubstituteMlsClientRun : MlsClientRun
	{
		private string m_substitute;
		private AssembledStyles m_substituteStyle;

		public SubstituteMlsClientRun(IViewMultiString mls, AssembledStyles style, string substitute, AssembledStyles substituteStyle)
			: base(mls, style)
		{
			m_substitute = substitute;
			m_substituteStyle = substituteStyle;
		}

		/// <summary>
		/// The alternative that should be displayed when the run is empty.
		/// </summary>
		public override string Substitute
		{
			get { return m_substitute; }
		}

		/// <summary>
		/// The styles to use in displaying the Substitute string.
		/// </summary>
		public override AssembledStyles SubstituteStyle
		{
			get { return m_substituteStyle; }
		}

		/// <summary>
		/// Return true if the substitute string should be used. Typically if it exists and our own
		/// contents are not empty.
		/// </summary>
		public override bool ShouldUseSubstitute
		{
			get
			{
				return (Tss == null || Tss.Length == 0 && !String.IsNullOrEmpty(m_substitute));
			}
		}

		/// <summary>
		/// Return an otherwise equivalent Tss client run that has the specified Contents.
		/// Subclasses should override to return the appropriate subclass and copy any additional information.
		/// </summary>
		internal override MlsClientRun CopyWithNewContents(IViewMultiString newContents)
		{
			var result = new SubstituteMlsClientRun(newContents, Style, m_substitute, m_substituteStyle);
			result.Hookup = Hookup;
			return result;
		}
	}

	/// <summary>
	/// Implementation of TextClientRun based on ITsString.
	/// </summary>
	public class TssClientRun : TextClientRun
	{
		public ITsString Tss { get; private set; }

		public TssClientRun(ITsString tss, AssembledStyles style)
		{
			if (tss == null)
				throw new ArgumentNullException("tss", "Cannot create TssClientRun with null TsString");
			Tss = tss;
			Style = style;
		}

		/// <summary>
		/// Override to get the actual WS known in the TsString.
		/// </summary>
		public override int WritingSystemAt(int index)
		{
			int var;
			return Tss.get_PropertiesAt(index).GetIntPropValues((int) FwTextPropType.ktptWs, out var);
		}

		public override string CharacterStyleNameAt(int index)
		{
			return Tss.get_PropertiesAt(index).GetStrPropValue((int) FwTextPropType.ktptCharStyle);
		}

		public override string Text { get { return Tss.Text ?? ""; } }

		public override int UniformRunCount
		{
			get { return Tss.RunCount; }
		}

		public override string UniformRunText(int irun)
		{
			TsRunInfo info;
			Tss.FetchRunInfo(irun, out info);
			var result = Tss.GetChars(info.ichMin, info.ichLim);
			if (result == null)
				return "";
			return result;
		}

		public override AssembledStyles UniformRunStyles(int irun)
		{
			ITsTextProps props = Tss.get_Properties(irun);
			return Style.ApplyTextProps(props);
		}

		public override int UniformRunStart(int irun)
		{
			TsRunInfo info;
			Tss.FetchRunInfo(irun, out info);
			return info.ichMin;
		}

		public override int UniformRunLength(int irun)
		{
			TsRunInfo info;
			Tss.FetchRunInfo(irun, out info);
			return info.ichLim - info.ichMin;
		}

		/// <summary>
		/// Return an otherwise equivalent Tss client run that has the specified Contents.
		/// Subclasses should override to return the appropriate subclass and copy any additional information.
		/// </summary>
		internal virtual TssClientRun CopyWithNewContents(ITsString newContents)
		{
			ITsString tss = newContents;
			int var;
			if (tss == null)
				tss = TsStrFactoryClass.Create().EmptyString(Tss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var));
			var result = new TssClientRun(tss, Style);
			result.Hookup = Hookup;
			return result;
		}
	}
	/// <summary>
	/// The special TssClientRun we use to support displaying a substitute string when the run is empty.
	/// </summary>
	class SubstituteTssClientRun : TssClientRun
	{
		private string m_substitute;
		private AssembledStyles m_substituteStyle;

		public SubstituteTssClientRun(ITsString tss, AssembledStyles style, string substitute, AssembledStyles substituteStyle)
			: base(tss, style)
		{
			m_substitute = substitute;
			m_substituteStyle = substituteStyle;
		}

		/// <summary>
		/// The alternative that should be displayed when the run is empty.
		/// </summary>
		public override string Substitute
		{
			get { return m_substitute; }
		}

		/// <summary>
		/// The styles to use in displaying the Substitute string.
		/// </summary>
		public override AssembledStyles SubstituteStyle
		{
			get { return m_substituteStyle; }
		}

		/// <summary>
		/// Return true if the substitute string should be used. Typically if it exists and our own
		/// contents are not empty.
		/// </summary>
		public override bool ShouldUseSubstitute
		{
			get { return (Tss == null || Tss.Length == 0) && !String.IsNullOrEmpty(m_substitute); }
		}

		/// <summary>
		/// Return an otherwise equivalent Tss client run that has the specified Contents.
		/// Subclasses should override to return the appropriate subclass and copy any additional information.
		/// </summary>
		internal override TssClientRun CopyWithNewContents(ITsString newContents)
		{
			ITsString tss = newContents;
			int var;
			if (tss == null)
				tss = TsStrFactoryClass.Create().EmptyString(Tss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var));
			var result = new SubstituteTssClientRun(tss, Style, m_substitute, m_substituteStyle);
			result.Hookup = Hookup;
			return result;
		}
	}

	/// <summary>
	/// An implementation of TextClientRun allowing us to use ordinary strings in paragraph layout.
	/// The AssembledStyles must specify a valid writing system.
	/// </summary>
	public class StringClientRun : TextClientRun
	{
		public StringClientRun(string contents, AssembledStyles style)
		{
			Contents = contents;
			Style = style;
		}

		internal string Contents { get; private set; }

		public override string Text {get { return Contents ?? "";}}

		public override int UniformRunCount
		{
			get { return 1; }
		}

		/// <summary>
		/// Return an otherwise equivalent string client run that has the specified Contents.
		/// Subclasses should override to return the appropriate subclass and copy any additional information.
		/// </summary>
		internal virtual StringClientRun CopyWithNewContents(string newContents)
		{
			var result = new StringClientRun(newContents, Style);
			result.Hookup = Hookup;
			return result;
		}

		public override string UniformRunText(int irun)
		{
			Debug.Assert(irun == 0);
			return Contents;
		}

		public override AssembledStyles UniformRunStyles(int irun)
		{
			Debug.Assert(irun == 0);
			return Style;
		}

		public override int UniformRunStart(int irun)
		{
			Debug.Assert(irun == 0);
			return 0;
		}

		public override int UniformRunLength(int irun)
		{
			Debug.Assert(irun == 0);
			return Contents.Length;
		}
	}

	/// <summary>
	/// The special StringClientRun we use to support displaying a substitute string when the run is empty.
	/// </summary>
	class SubstituteStringClientRun : StringClientRun
	{
		private string m_substitute;
		private AssembledStyles m_substituteStyle;

		public SubstituteStringClientRun(string contents, AssembledStyles style, string substitute, AssembledStyles substituteStyle)
			: base(contents, style)
		{
			m_substitute = substitute;
			m_substituteStyle = substituteStyle;
		}

		/// <summary>
		/// The alternative that should be displayed when the run is empty.
		/// </summary>
		public override string Substitute
		{
			get { return m_substitute; }
		}

		/// <summary>
		/// The styles to use in displaying the Substitute string.
		/// </summary>
		public override AssembledStyles SubstituteStyle
		{
			get { return m_substituteStyle; }
		}

		/// <summary>
		/// Return true if the substitute string should be used. Typically if it exists and our own
		/// contents are not empty.
		/// </summary>
		public override bool ShouldUseSubstitute
		{
			get { return String.IsNullOrEmpty(Contents) && !String.IsNullOrEmpty(m_substitute); }
		}

		/// <summary>
		/// Return an otherwise equivalent string client run that has the specified Contents.
		/// Subclasses should override to return the appropriate subclass and copy any additional information.
		/// </summary>
		internal override StringClientRun CopyWithNewContents(string newContents)
		{
			var result = new SubstituteStringClientRun(newContents, Style, m_substitute, m_substituteStyle);
			result.Hookup = Hookup;
			return result;
		}
	}
}
