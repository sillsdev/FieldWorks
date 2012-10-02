// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlNoteResponse.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Diagnostics;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores information about a single category in a Scripture annotation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("notationResponse")]
	public class XmlNoteResponse
	{
		#region Member variables
		private List<XmlNotePara> m_paras = new List<XmlNotePara>();
		#endregion

		#region XML elements
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the response list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement("para")]
		public List<XmlNotePara> Paragraphs
		{
			get { return m_paras; }
			set { m_paras = value; }
		}
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlNoteResponse"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlNoteResponse()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlNoteResponse"/> class.
		/// </summary>
		/// <param name="text">The journal text to initialize from</param>
		/// <param name="wsDefault">The default writing system</param>
		/// <param name="lgwsf">The writing system factory to use</param>
		/// ------------------------------------------------------------------------------------
		public XmlNoteResponse(IStJournalText text, int wsDefault, ILgWritingSystemFactory lgwsf)
		{
			Debug.Assert(text.ParagraphsOS.Count > 0, "Unexpected paragraph count");
			foreach (IStTxtPara para in text.ParagraphsOS)
				m_paras.Add(new XmlNotePara(para, wsDefault, lgwsf));
		}
		#endregion

		#region Methods for writing the response to the cache
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes this response to the the specified annotation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void WriteToCache(IScrScriptureNote ann, FwStyleSheet styleSheet)
		{
			ParagraphCollection parasResponse = new ParagraphCollection(Paragraphs, styleSheet);

			ParagraphCollection.ParaMatchType type;
			int matchIndex = FindMatchingResponse(parasResponse, ann.ResponsesOS, out type);

			switch (type)
			{
				case ParagraphCollection.ParaMatchType.Exact:
				case ParagraphCollection.ParaMatchType.Contains:
					break; // we can ignore the new response -- it's a subset of the old.
				case ParagraphCollection.ParaMatchType.IsContained:
					IStJournalText oldText = ann.ResponsesOS[matchIndex];
					oldText.ParagraphsOS.Clear();
					parasResponse.WriteToCache(oldText);
					break;
				case ParagraphCollection.ParaMatchType.None:
					IStJournalText newText = ann.Cache.ServiceLocator.GetInstance<IStJournalTextFactory>().Create();
					ann.ResponsesOS.Add(newText);
					parasResponse.WriteToCache(newText);
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a text that matches the paragraph strings in rgtss, if any.
		/// </summary>
		/// <param name="paragraphs">The paragraphs for the response.</param>
		/// <param name="texts">The sequence of journal texts to search.</param>
		/// <param name="type">The type of match found (or not).</param>
		/// <returns>The index of the existing text found to be a match; or -1 if no match is
		/// found</returns>
		/// ------------------------------------------------------------------------------------
		private static int FindMatchingResponse(ParagraphCollection paragraphs,
			IFdoOwningSequence<IStJournalText> texts, out ParagraphCollection.ParaMatchType type)
		{
			for (int i = 0; i < texts.Count; ++i)
			{
				if (paragraphs.Equals(texts[i]))
				{
					type = ParagraphCollection.ParaMatchType.Exact;
					return i;
				}
			}

			type = ParagraphCollection.ParaMatchType.None;
			return -1;
		}

		//        /// ------------------------------------------------------------------------------------
		//        /// <summary>
		//        /// Find out how this text matches up to the paragraph strings in bldrs.
		//        /// </summary>
		//        /// <param name="text">The journal text.</param>
		//        /// <param name="paragraphs">The list of paragraphs.</param>
		//        /// <returns></returns>
		//        /// <remarks>THIS NEEDS SOME THOROUGH TESTING, INCLUDING A UNIT TEST!!</remarks>
		//        /// ------------------------------------------------------------------------------------
		//        private static ParaMatchType DoesTextMatch(IStJournalText text, ParagraphCollection paragraphs)
		//        {
		//            ParaMatchType bestType = ParaMatchType.None;
		//#if MATCHTEXTS
		//            ParaMatchType type = ParaMatchType.None;
		//            if (text.ParagraphsOS.Count >= bldrs.Count)
		//            {
		//                if (text.ParagraphsOS.Count == 0 && rgtss.Count == 0)
		//                    return ParaMatchType.Exact;
		//                int iPrevMatch = -1;
		//                for (int i = 0; i < text.ParagraphsOS.Count; ++i)
		//                {
		//                    int iMatch = FindMatchingParagraph(text.ParagraphsOS[i] as IStTxtPara, rgtss, out type);
		//                    switch (type)
		//                    {
		//                        case ParaMatchType.None:
		//                            return type;
		//                        case ParaMatchType.Exact:
		//                            if (iMatch == i)
		//                            {
		//                                if (bestType != ParaMatchType.Exact)
		//                                {
		//                                    if (bestType == ParaMatchType.None)
		//                                        bestType = type;
		//                                    // else it stays the same. Exact is subsumed by both Contains and IsContained
		//                                }
		//                            }
		//                            else if (iMatch > iPrevMatch)
		//                            {
		//                                if (bestType == ParaMatchType.Contains)
		//                                    return ParaMatchType.None;
		//                                else
		//                                    bestType = ParaMatchType.IsContained;
		//                            }
		//                            else
		//                            {
		//                                return ParaMatchType.None;
		//                            }
		//                            break;
		//                        case ParaMatchType.Contains:
		//                            if (iMatch == i)
		//                            {
		//                                if (bestType != ParaMatchType.IsContained)
		//                                    bestType = type;
		//                                else
		//                                    return ParaMatchType.None;
		//                            }
		//                            else
		//                            {
		//                                return ParaMatchType.None;
		//                            }
		//                            break;
		//                        case ParaMatchType.IsContained:
		//                            if (iMatch == i || iMatch > iPrevMatch)
		//                            {
		//                                if (bestType != ParaMatchType.Contains)
		//                                    bestType = type;
		//                                else
		//                                    return ParaMatchType.None;
		//                            }
		//                            else
		//                            {
		//                                return ParaMatchType.None;
		//                            }
		//                            break;
		//                    }
		//                    iPrevMatch = iMatch;
		//                }
		//            }
		//            else
		//            {

		//            }
		//#endif
		//            return bestType;
		//        }
		#endregion

		#region static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlNoteResponse"/> class based on the
		/// specified category.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static List<XmlNoteResponse> GetResponsesList(IScrScriptureNote ann, int wsDefault,
			ILgWritingSystemFactory lgwsf)
		{
			if (ann == null)
				return null;

			List<XmlNoteResponse> responses = new List<XmlNoteResponse>();
			foreach (IStJournalText txt in ann.ResponsesOS)
				responses.Add(new XmlNoteResponse(txt, wsDefault, lgwsf));

			return (responses.Count == 0 ? null : responses);
		}

		#endregion
	}
}
