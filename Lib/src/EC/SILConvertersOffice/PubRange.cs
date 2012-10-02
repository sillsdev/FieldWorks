using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;               // for COMException
using Pub = Microsoft.Office.Interop.Publisher;

namespace SILConvertersOffice
{
	internal class PubStory
	{
		protected Pub.Story m_aStoryBasedOn;
		protected int m_nOffset;

		public PubStory(Pub.Story basedOnStory)
		{
			m_aStoryBasedOn = basedOnStory;
			m_nOffset = 0;  // not sure if needed
		}

		public PubParagraphs Paragraphs
		{
			get
			{
				PubParagraphs aParagraphRanges = new PubParagraphs();

				int i = 1, nLastStart = -1;
				while(true)
				{
					Pub.TextRange aPubRange = m_aStoryBasedOn.TextRange.Paragraphs(i++, 1);

					if (nLastStart != aPubRange.Start)
					{
						if( aPubRange.Text.Length > 1 )
							aParagraphRanges.Add(new PubRange(aPubRange));
					}
					else
						break;

					nLastStart = aPubRange.Start;
				}

				return aParagraphRanges;
			}
		}
	}

	internal class PubParagraphs : List<PubRange>
	{
	}

	internal class PubRange : OfficeRange
	{
		public Pub.TextRange RangeBasedOn
		{
			get { return (Pub.TextRange)base.m_aRangeBasedOn; }
		}

		public PubRange(PubRange basedOnRange)
			: this(basedOnRange.RangeBasedOn)
		{
		}

		public PubRange(Pub.TextRange basedOnRange)
			: base(basedOnRange)
		{
			m_nOffset = basedOnRange.Start;
			m_nNumPages = RangeBasedOn.Application.ActiveDocument.Pages.Count;
		}

		public override int Start
		{
			get { return RangeBasedOn.Start; }
			set { RangeBasedOn.Start = value; }
		}

		public override int End
		{
			get { return RangeBasedOn.End; }
			set { RangeBasedOn.End = value; }
		}

		public override string Text
		{
			get { return RangeBasedOn.Text; }
			set { RangeBasedOn.Text = value; }
		}

		public void ReplaceText(string strNewText)
		{
			Pub.FindReplace aFindReplace = RangeBasedOn.Find;
			aFindReplace.ReplaceScope = Microsoft.Office.Interop.Publisher.PbReplaceScope.pbReplaceScopeOne;
			aFindReplace.FindText = RangeBasedOn.Text;
			aFindReplace.ReplaceWithText = strNewText;
			aFindReplace.Execute();
		}

		public PubParagraphs Paragraphs
		{
			get
			{
				PubParagraphs aParagraphRanges = new PubParagraphs();

				int i = 1, nLastStart = -1;
				while (true)
				{
					Pub.TextRange aPubRange = RangeBasedOn.Paragraphs(i++, 1);

					if ((nLastStart != aPubRange.Start) && (aPubRange.Start < RangeBasedOn.End))
					{
						if (aPubRange.Text.Length > 1)
							aParagraphRanges.Add(new PubRange(aPubRange));
					}
					else
						break;

					nLastStart = aPubRange.Start;
				}

				return aParagraphRanges;
			}
		}

		public override string FontName
		{
			get { return RangeBasedOn.MajorityFont.Name; }
			set { RangeBasedOn.Font.Name = value; }
		}

		protected int m_nStoryID = -1;
		public int StoryID
		{
			get
			{
				System.Diagnostics.Debug.Assert(m_nStoryID != -1);  // otherwise it means it was never initialized
				return m_nStoryID;
			}
			set { m_nStoryID = value; }
		}

		protected int m_nNumPages, m_nCurrPage = 0;

		public override void Select()
		{
			bool bOnceThru = false;
			do
			{
				try
				{
					RangeBasedOn.Select();
					return;
				}
				catch (System.Runtime.InteropServices.ExternalException ex)
				{
					if ((ex.ErrorCode == -2147221473) && (ex.Message == "You can only select objects that are on the current page."))
					{
						if (++m_nCurrPage == m_nNumPages)
						{
							bOnceThru = true;
							m_nCurrPage = 1;
						}

						RangeBasedOn.Application.ActiveDocument.ActiveView.ActivePage = RangeBasedOn.Application.ActiveDocument.Pages[m_nCurrPage];
					}
					else
						break;
				}
				catch (Exception)
				{
					// this occurs when the word is beyond the edge of a text frame...
					// just ignore it (we can't see it anyway and this isn't that crucial)
					return;
				}

			} while (!bOnceThru);
		}
	}
}
