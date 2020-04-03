// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// TextSelInfo is another variety of selection helper. It provides easy access to
	/// certain kinds of information in a selection.
	/// NOTE: some clients (e.g. EditingHelper.TextSelInfoBeforeEdit) depend upon
	/// the information cached in this object even after the underlying Selection
	/// has changed state.
	/// </summary>
	internal sealed class TextSelInfo
	{
		private ITsString m_tssA;
		private ITsString m_tssE;
		private int m_ichA;
		private int m_ichE;
		private int m_hvoObjA;
		private int m_tagA;
		private int m_hvoObjE;
		private int m_tagE;
		private int m_wsE;
		private int m_wsA;
		private VwSelType m_selType;

		/// <summary />
		/// Initialize
		internal TextSelInfo(IVwSelection sel)
		{
			Selection = sel;
			Init();
		}

		/// <summary>
		/// Do remaining initialization once we (might) have a selection.
		/// </summary>
		private void Init()
		{
			if (Selection == null)
			{
				return;
			}
			if (Selection.SelType == VwSelType.kstPicture)
			{
				// TextSelInfo doesn't work; but we can get a good approximation of much of the
				// information like this.
				// Out variables for AllTextSelInfo.
				int tagTextProp;
				int ichAnchor;
				int ichEnd;
				// Main array of information retrieved from sel that made combo.
				SelLevInfo[] rgvsli;
				try
				{
					var cvsli = Selection.CLevels(false) - 1;
					rgvsli = SelLevInfo.AllTextSelInfo(Selection, cvsli, out _, out tagTextProp, out _, out ichAnchor,
						out ichEnd, out _, out _, out _, out _);
				}
				catch (Exception)
				{
					// If anything goes wrong just give up.
					return;
				}
				m_ichA = ichAnchor;
				m_ichE = ichEnd;
				if (rgvsli.Length > 0)
				{
					m_hvoObjA = m_hvoObjE = rgvsli[0].hvo;
				}
				m_tagA = m_tagE = tagTextProp;
			}
			else
			{
				Selection.TextSelInfo(true, out m_tssE, out m_ichE, out _, out m_hvoObjE, out m_tagE, out m_wsE);
				Selection.TextSelInfo(false, out m_tssA, out m_ichA, out _, out m_hvoObjA, out m_tagA, out m_wsA);
			}
			IsRange = Selection.IsRange;
			m_selType = Selection.SelType;
		}

		/// <summary>
		/// Construct a TextSelInfo from the current selection of the root box.
		/// </summary>
		internal TextSelInfo(IVwRootBox rootbox)
		{
			Selection = rootbox.Selection;
			Init();
		}

		/// <summary>
		/// Answer true if there is a selection and it has a range.
		/// </summary>
		internal bool IsRange { get; private set; }

		/// <summary>
		/// The selection we're getting info about.
		/// </summary>
		internal IVwSelection Selection { get; }

		/// <summary>
		/// The character index of the anchor. (0 if no selection)
		/// </summary>
		internal int IchAnchor => m_ichA;

		/// <summary>
		/// The character index of the end-point. (0 if no selection)
		/// </summary>
		internal int IchEnd => m_ichE;

		/// <summary>
		/// The string in which the selection occurs...if there's a difference,
		/// passing true gets the end-point string, false the anchor string.
		/// Both null if no selection.
		/// </summary>
		internal ITsString SelectedProperty(bool fEndPoint)
		{
			return fEndPoint ? m_tssE : m_tssA;
		}

		/// <summary>
		/// The string in which the (anchor of) the selection occurs.
		/// Null if no selection.
		/// </summary>
		internal ITsString TssAnchor => m_tssA;

		/// <summary>
		/// The text of the string in which the (anchor of) the selection occurs.
		/// Empty string if no selection.
		/// </summary>
		internal string AnchorText => m_tssA == null ? string.Empty : m_tssA.Text;

		/// <summary>
		/// The length of the string in which the (anchor of) the selection occurs.
		/// Zero if no selection.
		/// </summary>
		internal int AnchorLength => m_tssA?.Length ?? 0;

		/// <summary>
		/// The id of the string property in which the selection occurs.
		/// Pass true for the property in which the end point occurs, false for anchor.
		/// Zero if no selection.
		/// </summary>
		internal int Tag(bool fEndPoint)
		{
			return fEndPoint ? m_tagE : m_tagA;
		}

		/// <summary>
		/// The id of the string property in which the (anchor of) the selection occurs.
		/// Zero if no selection.
		/// </summary>
		internal int TagAnchor => m_tagA;

		/// <summary>
		/// The id of the object that has the text property (Tag(fEndPoint) in which the
		/// selection occurs.
		/// Zero if no selection.
		/// </summary>
		internal int Hvo(bool fEndPoint)
		{
			return fEndPoint ? m_hvoObjE : m_hvoObjA;
		}

		/// <summary>
		/// The id of the object that has the text property Tag in which the
		/// (anchor of the) selection occurs.
		/// Zero if no selection.
		/// </summary>
		internal int HvoAnchor => m_hvoObjA;

		/// <summary>
		/// The writing system alternative of the selection.
		/// Note that this is not necessarily the writing system of the selected text,
		/// or even the character adjacent to the selected end point; it identifies
		/// an alternative in a multilingual alternative property, and is zero if there
		/// is no selection or if the selected property is not multilingual.
		/// </summary>
		internal int WsAlt(bool fEndPoint)
		{
			return fEndPoint ? m_wsE : m_wsA;
		}

		/// <summary>
		/// The anchor WsAlt.
		/// </summary>
		internal int WsAltAnchor => m_wsA;

		/// <summary>
		/// Level 0 returns the same object as Hvo(fEndPoint).
		/// Level 1 returns the object which contains the display of ContainingObject(0, fEndPoint).
		/// Level n returns the object which contains the display of ContainingObject(n-1, fEndPoint).
		/// Returns 0 if there was no selection or level is too large.
		/// </summary>
		internal int ContainingObject(int level, bool fEndPoint)
		{
			if (Selection == null || level >= Selection.CLevels(fEndPoint))
			{
				return 0;
			}
			Selection.PropInfo(fEndPoint, level, out var hvoObj, out _, out _, out _, out _);
			return hvoObj;
		}

		/// <summary>
		/// ContainingObject info about anchor.
		/// </summary>
		internal int ContainingObject(int level)
		{
			return ContainingObject(level, false);
		}

		/// <summary>
		/// Level 0 returns the same as Tag(fEndPoint).
		/// Level 1 returns the id of the property of ContainingObject(1, fEndPoint) which contains ContainingObject(0, fEndPoint).
		/// Level 1 returns the id of the property of ContainingObject(n, fEndPoint) which contains ContainingObject(n-1, fEndPoint).
		/// Returns 0 if there was no selection or level is too large.
		/// </summary>
		internal int ContainingObjectTag(int level, bool fEndPoint)
		{
			if (Selection == null || level >= Selection.CLevels(fEndPoint))
			{
				return 0;
			}
			Selection.PropInfo(fEndPoint, level, out _, out var tag, out _, out _, out _);
			return tag;
		}

		/// <summary>
		/// ContainingObjectTag info about anchor.
		/// </summary>
		internal int ContainingObjectTag(int level)
		{
			return ContainingObjectTag(level, false);
		}

		/// <summary>
		/// Level 0 returns zero.
		/// Level 1 returns the index of ContainingObject(0, fEndPoint)
		///		in property ContainingObjectTag(1, fEndPoint) of object ContainingObject(1, fEndPoint).
		/// Level n returns the index of ContainingObject(n-1, fEndPoint)
		///		in property ContainingObjectTag(n, fEndPoint) of object ContainingObject(n, fEndPoint).
		/// Returns 0 if there was no selection or level is too large (or the relevant property is atomic).
		/// </summary>
		internal int ContainingObjectIndex(int level, bool fEndPoint)
		{
			if (Selection == null || level >= Selection.CLevels(fEndPoint))
			{
				return -1;
			}
			Selection.PropInfo(fEndPoint, level, out _, out _, out var ihvo, out _, out _);
			return ihvo;
		}

		/// <summary>
		/// ContainingObjectIndex info about anchor.
		/// </summary>
		internal int ContainingObjectIndex(int level)
		{
			return ContainingObjectIndex(level, false);
		}

		/// <summary>
		/// The number of layers of containing objects in the selection
		/// (Levels - 1 is the largest meaningful argument to pass to any of the containing object methods).
		/// </summary>
		internal int Levels(bool fEndPoint)
		{
			return Selection?.CLevels(fEndPoint) ?? 0;
		}

		/// <summary>
		/// Return true if there is a selection and it is a text one (selection type is kstText).
		/// </summary>
		internal bool IsText => Selection != null && m_selType == VwSelType.kstText;

		/// <summary>
		/// Return true if there is a selection and it is a picture one (selection type is kstPicture).
		/// </summary>
		internal bool IsPicture => Selection != null && m_selType == VwSelType.kstPicture;
	}
}