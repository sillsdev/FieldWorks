// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ParagraphCollection.cs
// Responsibility: TE Team

using System.Collections.Generic;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Diagnostics;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a collection of paragraphs usually used for StTexts or StJournalTexts when
	/// holding the information for serialization/deserialization.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ParagraphCollection : List<StTxtParaBldr>
	{
		#region ParaMatchType enum
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This enumerates the possibilities for matching an old and a new paragraph.  (Only
		/// the text of the contents is compared.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal enum ParaMatchType
		{
			/// <summary>not a match.</summary>
			None,
			/// <summary>old and new match exactly</summary>
			Exact,
			/// <summary>old contains the new</summary>
			Contains,
			/// <summary>old is contained in the new</summary>
			IsContained
		}

		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParagraphCollection"/> class.
		/// </summary>
		/// <param name="list">The list of XmlNoteParas to use to create this collection</param>
		/// <param name="styleSheet">The style sheet</param>
		/// ------------------------------------------------------------------------------------
		public ParagraphCollection(List<XmlNotePara> list, FwStyleSheet styleSheet) :
			this(list, styleSheet, styleSheet.Cache.DefaultAnalWs)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParagraphCollection"/> class.
		/// </summary>
		/// <param name="list">The list of XmlNoteParas to use to create this collection</param>
		/// <param name="styleSheet">The style sheet</param>
		/// <param name="wsDefault">The default writing system to use</param>
		/// ------------------------------------------------------------------------------------
		public ParagraphCollection(List<XmlNotePara> list, FwStyleSheet styleSheet, int wsDefault)
		{
			if (list != null)
			{
				foreach (XmlNotePara para in list)
				{
					StTxtParaBldr bldr = para.BuildParagraph(styleSheet, wsDefault);
					if (bldr != null)
						Add(bldr);
				}
			}
		}
		#endregion

		#region Methods for writing the paragraph collection to the cache
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the paragraphs contained in this paragraph collection to the specified text
		/// </summary>
		/// <param name="text">The journal text (i.e., quote, discussion, suggestion,
		/// resolution, etc.) to write the paragraphs to</param>
		/// ------------------------------------------------------------------------------------
		internal void WriteToCache(IStJournalText text)
		{
			Debug.Assert(text != null);
			if (text == null)
				return;

			if (text.ParagraphsOS.Count == 0 && Count == 0)
			{
				// Create one empty paragraph even if there's no data.
				IStPara para = text.Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
				text.ParagraphsOS.Add(para);
				para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.Remark);
				return;
			}

			foreach (IStTxtPara para in text.ParagraphsOS)
			{
				if (para == null)
					continue;

				ParaMatchType type;
				int iPara = FindMatchingParagraph(para, out type);
				switch (type)
				{
					case ParaMatchType.Exact:
						// new is same as old, just discard the imported paragraph.
						RemoveAt(iPara);
						break;
					case ParaMatchType.Contains:
						// no new information, just discard the imported paragraph.
						// REVIEW: this may indicate a deletion.
						RemoveAt(iPara);
						break;
					case ParaMatchType.IsContained:
						// we have new information added to an existing paragraph.
						// (or could it be a deletion?)
						// replace the current paragraph.
						para.Contents = this[iPara].StringBuilder.GetString();
						para.StyleName = this[iPara].ParaStylePropsProxy.StyleId;
						RemoveAt(iPara);
						break;
					case ParaMatchType.None:
						// Existing paragraph was not found in the list of imported paras.
						// REVIEW: this may indicate a deletion.
						break;
				}
			}

			// Append any new paragraphs to the list of paragraphs in the text.
			foreach (StTxtParaBldr paraBldr in this)
				paraBldr.CreateParagraph(text);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find the first paragraph in this paragraph collection that matches up with the
		/// given paragraph contents.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="type">returns the type of match found</param>
		/// <returns>index of the matching paragraph, or -1 if no match is found</returns>
		/// -----------------------------------------------------------------------------------
		private int FindMatchingParagraph(IStTxtPara para, out ParaMatchType type)
		{
			type = ParaMatchType.None;
			if (para == null)
				return -1;

			string sPara = para.Contents.Text; // may be null

			for (int i = 0; i < Count; ++i)
			{
				Debug.Assert(this[i] != null, "We shouldn't have a null paragraph in our collection");

				if (para.Contents.Equals(this[i].StringBuilder.GetString()))
				{
					type = ParaMatchType.Exact;
					return i; // perfect match!
				}
				else if (para.Contents.Length == 0)
				{
					type = ParaMatchType.IsContained;
					return i;
				}
				else if (this[i].Length == 0)
				{
					type = ParaMatchType.Contains;
					return i; // partial match!
				}
				else if (this[i].StringBuilder.Text.Contains(sPara))
				{
					type = ParaMatchType.IsContained;
					return i;
				}
				else if (sPara.Contains(this[i].StringBuilder.Text))
				{
					type = ParaMatchType.Contains;
					return i; // partial match!
				}
			}
			return -1;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a value indicating whether or not the specified ISJournalText contains
		/// the same paragraphs (and in the same order) as those in the list of paragraphs
		/// in the ParagraphCollection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Equals(IStJournalText text)
		{
			if (text.ParagraphsOS.Count != Count)
				return false;

			for (int i = 0; i < Count; i++)
			{
				ITsString textContent = ((IStTxtPara)text.ParagraphsOS[i]).Contents;
				ITsString ourContent = this[i].StringBuilder.GetString();

				if (!ourContent.Equals(textContent))
					return false;
			}

			return true;
		}
	}
}
