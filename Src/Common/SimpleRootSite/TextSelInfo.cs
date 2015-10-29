// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// TextSelInfo is another variety of selection helper. It provides easy access to
	/// certain kinds of information in a selection.
	/// NOTE: some clients (e.g. EditingHelper.TextSelInfoBeforeEdit) depend upon
	/// the information cached in this object even after the underlying Selection
	/// has changed state.
	/// </summary>
	public class TextSelInfo
	{
		IVwSelection m_sel;
		ITsString m_tssA;
		ITsString m_tssE;
		bool m_fAssocPrev;
		int m_ichA;
		int m_ichE;
		int m_hvoObjA;
		int m_tagA;
		int m_hvoObjE;
		int m_tagE;
		int m_wsE;
		int m_wsA;
		bool m_fIsRange;
		VwSelType m_selType;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TextSelInfo"/> class.
		/// </summary>
		/// <param name="sel">The sel.</param>
		/// ------------------------------------------------------------------------------------
		public TextSelInfo(IVwSelection sel)
		{
			m_sel = sel;
			Init();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do remaining initialization once we (might) have a selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void Init()
		{
			if (m_sel == null)
				return;
			if (m_sel.SelType == VwSelType.kstPicture)
			{
				// TextSelInfo doesn't work; but we can get a good approximation of much of the
				// information like this.
				// Out variables for AllTextSelInfo.
				int ihvoRoot;
				int tagTextProp;
				int cpropPrevious;
				int ichAnchor;
				int ichEnd;
				int ws;
				bool fAssocPrev;
				int ihvoEnd;
				ITsTextProps ttpBogus;
				// Main array of information retrived from sel that made combo.
				SelLevInfo[] rgvsli;

				// Analysis can now be zero (e.g., displaying alterate case form for non-existent WfiWordform)
				// and I don't believe it's a problem for the code below (JohnT).
				//				if (sandbox.Analysis == 0)
				//				{
				//					// We aren't fully initialized yet, so don't do anything.
				//					return null;
				//				}
				try
				{
					int cvsli = m_sel.CLevels(false) - 1;
					rgvsli = SelLevInfo.AllTextSelInfo(m_sel, cvsli,
						out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
						out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);
				}
				catch (Exception)
				{
					// If anything goes wrong just give up.
					return;
				}
				m_ichA = ichAnchor;
				m_ichE = ichEnd;
				if (rgvsli.Length > 0)
					m_hvoObjA = m_hvoObjE = rgvsli[0].hvo;
				m_tagA = m_tagE = tagTextProp;
			}
			else
			{
				m_sel.TextSelInfo(true, out m_tssE, out m_ichE, out m_fAssocPrev, out m_hvoObjE, out m_tagE, out m_wsE);
				m_sel.TextSelInfo(false, out m_tssA, out m_ichA, out m_fAssocPrev, out m_hvoObjA, out m_tagA, out m_wsA);
			}
			m_fIsRange = m_sel.IsRange;
			m_selType = m_sel.SelType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a TextSelInfo from the current selection of the root box.
		/// </summary>
		/// <param name="rootbox"></param>
		/// ------------------------------------------------------------------------------------
		public TextSelInfo(IVwRootBox rootbox)
		{
			m_sel = rootbox.Selection;
			Init();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer true if there is a selection and it has a range.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsRange
		{
			get { return m_fIsRange; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The selection we're getting info about.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwSelection Selection
		{
			get { return m_sel; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The character index of the anchor. (0 if no selection)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int IchAnchor
		{
			get {return m_ichA; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The character index of the end-point. (0 if no selection)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int IchEnd
		{
			get {return m_ichE; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The string in which the selection occurs...if there's a difference,
		/// passing true gets the end-point string, false the anchor string.
		/// Both null if no selection.
		/// </summary>
		/// <param name="fEndPoint"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString SelectedProperty(bool fEndPoint)
		{
			if (fEndPoint)
				return m_tssE;
			else
				return m_tssA;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The string in which the (anchor of) the selection occurs.
		/// Null if no selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString TssAnchor
		{
			get { return m_tssA; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The text of the string in which the (anchor of) the selection occurs.
		/// Empty string if no selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AnchorText
		{
			get
			{
				if (m_tssA == null)
					return String.Empty;
				else
					return m_tssA.Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The length of the string in which the (anchor of) the selection occurs.
		/// Zero if no selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int AnchorLength
		{
			get
			{
				if (m_tssA == null)
					return 0;
				else
					return m_tssA.Length;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The id of the string property in which the selection occurs.
		/// Pass true for the property in which the end point occurs, false for anchor.
		/// Zero if no selection.
		/// </summary>
		/// <param name="fEndPoint"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int Tag(bool fEndPoint)
		{
			return fEndPoint ? m_tagE : m_tagA;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The id of the string property in which the (anchor of) the selection occurs.
		/// Zero if no selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int TagAnchor
		{
			get { return m_tagA; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The id of the object that has the text property (Tag(fEndPoint) in which the
		/// selection occurs.
		/// Zero if no selection.
		/// </summary>
		/// <param name="fEndPoint"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int Hvo(bool fEndPoint)
		{
			return fEndPoint ? m_hvoObjE : m_hvoObjA;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The id of the object that has the text property Tag in which the
		/// (anchor of the) selection occurs.
		/// Zero if no selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int HvoAnchor
		{
			get { return m_hvoObjA; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The writing system alternative of the selection.
		/// Note that this is not necessarily the writing system of the selected text,
		/// or even the character adjacent to the selected end point; it identifies
		/// an alternative in a multilingual alternative property, and is zero if there
		/// is no selection or if the selected property is not multilingual.
		/// </summary>
		/// <param name="fEndPoint"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int WsAlt(bool fEndPoint)
		{
			return fEndPoint ? m_wsE : m_wsA;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The anchor WsAlt.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int WsAltAnchor
		{
			get { return m_wsA; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is relevant only for insertion points. It indicates whether the selection
		/// associates with the previous or following character. (This can affect which character
		/// properties are used if the user types when an IP is at the boundary between
		/// character runs having different text properties, such as bold, italic, color, writing system,
		/// style, etc.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AssociatePrevChar
		{
			get { return m_fAssocPrev; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Level 0 returns the same object as Hvo(fEndPoint).
		/// Level 1 returns the object which contains the display of ContainingObject(0, fEndPoint).
		/// Level n returns the object which contains the display of ContainingObject(n-1, fEndPoint).
		/// Returns 0 if there was no selection or level is too large.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="fEndPoint"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int ContainingObject(int level, bool fEndPoint)
		{
			if (m_sel == null)
				return 0;
			if (level >= m_sel.CLevels(fEndPoint))
				return 0;
			int hvoObj, tag, ihvo, cpropPrevious;
			IVwPropertyStore vps;
			m_sel.PropInfo(fEndPoint, level, out hvoObj, out tag, out ihvo, out cpropPrevious, out vps);
			return hvoObj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// ContainingObject info about anchor.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int ContainingObject(int level)
		{
			return ContainingObject(level, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Level 0 returns the same as Tag(fEndPoint).
		/// Level 1 returns the id of the property of ContainingObject(1, fEndPoint) which contains ContainingObject(0, fEndPoint).
		/// Level 1 returns the id of the property of ContainingObject(n, fEndPoint) which contains ContainingObject(n-1, fEndPoint).
		/// Returns 0 if there was no selection or level is too large.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="fEndPoint"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int ContainingObjectTag(int level, bool fEndPoint)
		{
			if (m_sel == null)
				return 0;
			if (level >= m_sel.CLevels(fEndPoint))
				return 0;
			int hvoObj, tag, ihvo, cpropPrevious;
			IVwPropertyStore vps;
			m_sel.PropInfo(fEndPoint, level, out hvoObj, out tag, out ihvo, out cpropPrevious, out vps);
			return tag;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// ContainingObjectTag info about anchor.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int ContainingObjectTag(int level)
		{
			return ContainingObjectTag(level, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Level 0 returns zero.
		/// Level 1 returns the index of ContainingObject(0, fEndPoint)
		///		in property ContainingObjectTag(1, fEndPoint) of object ContainingObject(1, fEndPoint).
		/// Level n returns the index of ContainingObject(n-1, fEndPoint)
		///		in property ContainingObjectTag(n, fEndPoint) of object ContainingObject(n, fEndPoint).
		/// Returns 0 if there was no selection or level is too large (or the relevant property is atomic).
		/// </summary>
		/// <param name="level"></param>
		/// <param name="fEndPoint"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int ContainingObjectIndex(int level, bool fEndPoint)
		{
			if (m_sel == null)
				return -1;
			if (level >= m_sel.CLevels(fEndPoint))
				return -1;
			int hvoObj, tag, ihvo, cpropPrevious;
			IVwPropertyStore vps;
			m_sel.PropInfo(fEndPoint, level, out hvoObj, out tag, out ihvo, out cpropPrevious, out vps);
			return ihvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// ContainingObjectIndex info about anchor.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int ContainingObjectIndex(int level)
		{
			return ContainingObjectIndex(level, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The number of layers of containing objects in the selection
		/// (Levels - 1 is the largest meaningful argument to pass to any of the containing object methods).
		/// </summary>
		/// <param name="fEndPoint"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int Levels(bool fEndPoint)
		{
			if (m_sel == null)
				return 0;
			return m_sel.CLevels(fEndPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if there is a selection and it is a text one (selection type is kstText).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsText
		{
			get
			{
				if (m_sel == null)
					return false;
				return (m_selType == VwSelType.kstText);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if there is a selection and it is a picture one (selection type is kstPicture).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsPicture
		{
			get
			{
				if (m_sel == null)
					return false;
				return (m_selType == VwSelType.kstPicture);
			}
		}
	}
}
