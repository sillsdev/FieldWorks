// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Paragraphs;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// This abstract class provides a default implementation of IParagraphOperations.
	/// Minimally the methods that read and write the property that contains the text must be implemented.
	/// Enhance JohnT: may want to break out a subclass so that MakeListItem is abstract and the baseclass
	/// does not require a zero-argument constructor for T.
	/// </summary>
	public abstract class BaseParagraphOperations<T> : ISeqParagraphOperations<T>, IParaOpsList<T>
	{
		/// <summary>
		/// The list of objects which paragraph operations modifies.
		/// </summary>
		public IList<T> List { get; set; }

		public SequenceHookup<T> Hookup { get; set; }

		/// <summary>
		/// Insert a new paragraph, after the user types Enter, where the previous selection
		/// is an insertion point at the end of the paragraph.
		/// </summary>
		public virtual bool InsertFollowingParagraph(InsertionPoint ip, out Action makeSelection)
		{
			int index = ItemIndex(ip) + 1;
			MakeListItem(index, false);
			makeSelection = () => SelectionBuilder.In(Hookup)[index].Offset(0).Install();
			return true;
		}

		public virtual bool InsertFollowingParagraph(RangeSelection range, out Action makeSelection)
		{
			int index = ItemIndex(range.Start) + 1;
			DeleteItems(range.Start.StringPosition, range.End.StringPosition);
			MakeListItem(index, false);
			makeSelection = () => SelectionBuilder.In(Hookup)[index].Offset(0).Install();
			return true;
		}

		/// <summary>
		/// Determine the index of the item containing the IP
		/// </summary>
		internal int ItemIndex(InsertionPoint ip)
		{
			return Hookup.IndexOfChild(ip);
		}

		/// <summary>
		/// This method must be provided to make new things in the list, unless all callers are overridden.
		/// It makes a new item and inserts it at the specified index.
		/// </summary>
		public abstract T MakeListItem(int index, bool ipAtStartOfPara);

		public bool SplitParagraph(InsertionPoint ip, out Action makeSelection)
		{
			int index = ItemIndex(ip);
			MakeListItem(index + 1, false);
			var newItem = List[index + 1];
			MoveMaterialAfterIpToStart(ip, "", "", newItem);
			makeSelection = () => SelectionBuilder.In(Hookup)[index + 1].Offset(0).Install();
			return true;
		}

		private bool SplitParagraph(MultiLineInsertData input, out Action makeSelection)
		{
			int index = ItemIndex(input.Selection as InsertionPoint);
			MakeListItem(index + 1, false);
			var newItem = List[index + 1];
			var sel = input.Selection as InsertionPoint;

			if (input.TsStrAppendToFirstPara == null || input.TsStrPrependToLastPara == null)
				MoveMaterialAfterIpToStart(sel, input.StringAppendToFirstPara, input.StringPrependToLastPara, newItem);
			else
				MoveMaterialAfterIpToStart(sel, input.TsStrAppendToFirstPara, input.TsStrPrependToLastPara, newItem);
			if (input.ParaStyles != null)
			{
				if (input.ParaStyles.First() != null)
					ApplyParagraphStyle(index, 1, input.ParaStyles.First().Name);
				if (input.ParaStyles.Last() != null)
					ApplyParagraphStyle(index + 1, 1, input.ParaStyles.Last().Name);
			}

			makeSelection = () => SelectionBuilder.In(Hookup)[index].Offset(0).Install();
			return true;
		}

		private static string FindNextStyleStart(string input, bool outIndexStart, out int index)
		{
			string styleName = "";
			index = 0;

			while (index < input.Length)
			{
				int paraIndexStart = input.IndexOf("\\s", index);
				//int otherParaIndexStart = input.IndexOf("\\pard");
				//paraIndexStart = Math.Min(paraIndexStart != -1 ? paraIndexStart : otherParaIndexStart,
				//						  otherParaIndexStart != -1 ? otherParaIndexStart : paraIndexStart);
				int paraIndexEnd = paraIndexStart;
				int paraNumber = -1;
				while (paraIndexStart != -1)
				{
					paraIndexEnd = input.IndexOf("\\", paraIndexStart + 2);
					if (input.Substring(paraIndexStart, 2) == "\\s" && int.TryParse(input.Substring(paraIndexStart + 2, paraIndexEnd - paraIndexStart - 2), out paraNumber))
						break;
					//if (input.Substring(paraIndexStart, 5) == "\\pard")
					//{
					//    paraNumber = 0;
					//    break;
					//}
					paraIndexStart = input.IndexOf("\\s", paraIndexEnd);
					//otherParaIndexStart = input.IndexOf("\\pard", paraIndexEnd);
					//paraIndexStart = Math.Min(paraIndexStart != -1 ? paraIndexStart : otherParaIndexStart,
					//						  otherParaIndexStart != -1 ? otherParaIndexStart : paraIndexStart);
					paraNumber = -1;
				}
				int charIndexStart = input.IndexOf("cs", index);
				int charIndexEnd = paraIndexStart;
				int charNumber = -1;
				while (charIndexStart != -1)
				{
					charIndexEnd = input.IndexOf("\\", charIndexStart + 2);
					if ((input.Substring(charIndexStart - 3, 3) == "\\*\\" || input.Substring(charIndexStart - 8, 8) == @"\ltrch\f") && int.TryParse(input.Substring(charIndexStart + 2, charIndexEnd - charIndexStart - 2), out charNumber))
						break;
					charIndexStart = input.IndexOf("cs", charIndexEnd);
					charNumber = -1;
				}
				if (paraNumber != -1 && (paraIndexStart < charIndexStart || charNumber == -1))
				{
					if (outIndexStart)
						index = paraIndexStart;
					else
						index = paraIndexEnd + 1;
					styleName = input.Substring(paraIndexStart, paraIndexEnd - paraIndexStart);
					break;
				}
				if (charNumber != -1 && (charIndexStart < paraIndexStart || paraNumber == -1))
				{
					if (outIndexStart)
						index = charIndexStart;
					else
						index = charIndexEnd + 1;
					styleName = input.Substring(charIndexStart, charIndexEnd - charIndexStart);
					break;
				}
				if (paraIndexStart == -1 && charIndexStart == -1)
					return null;
				index = Math.Max(paraIndexEnd, charIndexEnd);
			}
			return styleName;
		}

		private IStyle FindNextRtfStyle(string input, Dictionary<int, Color> colorTable, bool inStylesheet, out int index)
		{
			string styleName = FindNextStyleStart(input, false, out index);
			if (styleName == null || index > input.IndexOf("}}"))
				return null;
			var styleProps = new List<string>();
			int stopIndex = inStylesheet ? input.IndexOf(';', index) : input.IndexOf(' ', input.IndexOf(' ') + 1);

			while (true)
			{
				int nextSlash = input.IndexOf('\\', index);
				int propIndex = Math.Min(nextSlash != -1 ? nextSlash : stopIndex, stopIndex);
				if (index >= input.Length || propIndex < 0)
					return null;
				if (stopIndex < index)
					break;
				string prop = input.Substring(index, propIndex - index);
				prop.Replace("\\", "");
				styleProps.Add(prop);
				index = propIndex + 1;
			}

			index = stopIndex + 1;

			if (styleProps.Last().StartsWith("additive "))
			{
				string possibleName = styleProps.Last().Substring(9, styleProps.Last().Length - 9);
				IStyle possibleStyle = Hookup.ContainingBox.Style.Stylesheet.Style(possibleName);
				if (possibleStyle != null && possibleStyle.Name != null)
					return possibleStyle;
			}

			return CreateStyle(styleProps, styleName, colorTable);
		}

		public virtual IStyle CreateStyle(List<string> styleProps, string name, Dictionary<int, Color> colorTable)
		{
			return null;
		}

		private static Dictionary<int, Color> GetColorTable(string input)
		{
			var colorTable = new Dictionary<int, Color>();
			int index = input.IndexOf("\\") + 1;
			while (index < input.IndexOf("}"))
			{
				int red = int.Parse(input.Substring(index + 3, input.IndexOf("\\", index) - index - 3));
				index = input.IndexOf("\\", index) + 1;
				int green = int.Parse(input.Substring(index + 5, input.IndexOf("\\", index) - index - 5));
				index = input.IndexOf("\\", index) + 1;
				int blue = int.Parse(input.Substring(index + 4, input.IndexOf(";", index) - index - 4));
				index = input.IndexOf("\\", index) + 1;
				colorTable.Add(colorTable.Count + 1, Color.FromArgb(red, green, blue));
			}
			return colorTable;
		}

		public bool InsertRTF(InsertionPoint insertionPoint, string rtfToInsert, out Action makeSelection)
		{
			makeSelection = () => DoNothing();
			int styleIndex;
			int index = rtfToInsert.IndexOf("{\\colortbl") + 10;
			Dictionary<int, Color> colorTable = GetColorTable(rtfToInsert.Substring(index));
			index = rtfToInsert.IndexOf("{\\stylesheet{") + 13;
			var textToInsert = new List<ITsString>();
			// The actual text (with formatting information stripped out) that we will insert into our view
			IStylesheet stylesheet = insertionPoint.RootBox.Style.Stylesheet;
			IStyle nextStyle;
			var tempStylesheet = new Dictionary<string, IStyle>();
			// Indicates the paragraph style that should be applied to each paragraph of the inserted text.
			var paraStylePlacements = new List<IStyle>();
			ITsStrBldr bldr = TsStrFactoryClass.Create().MakeString("", 1).GetBldr();

			while (true)
			{
				nextStyle = FindNextRtfStyle(rtfToInsert.Substring(index), colorTable, true, out styleIndex);
				if (nextStyle == null)
					break;
				if (stylesheet.Style(nextStyle.Name) == null)
					AddStyle(nextStyle, stylesheet);
				int styleIndexStart;
				FindNextStyleStart(rtfToInsert.Substring(index), true, out styleIndexStart);
				if (!tempStylesheet.ContainsKey(rtfToInsert.Substring(index + styleIndexStart, 6)))
					tempStylesheet.Add(rtfToInsert.Substring(index + styleIndexStart, 6), nextStyle);
				index += styleIndex;
			}
			index++;
			while (true)
			{
				string nextStyleName = FindNextStyleStart(rtfToInsert.Substring(index), false, out styleIndex);
				if (nextStyleName == null)
					break;
				nextStyle = tempStylesheet.FirstOrDefault(style => style.Key.StartsWith(nextStyleName)).Value;
				if (nextStyle == null)
				{
					index += styleIndex - 6;
					nextStyle = FindNextRtfStyle(rtfToInsert.Substring(index), colorTable, false, out styleIndex);
				}
				if (stylesheet.Style(nextStyle.Name) == null)
					AddStyle(nextStyle, stylesheet);
				if (nextStyle.IsParagraphStyle)
				{
					if (rtfToInsert.IndexOf("}}", index) < styleIndex)
						break;
					index += styleIndex;
					int substringIndex = rtfToInsert.IndexOf(" {", index);
					if (substringIndex != -1 && (rtfToInsert.Substring(substringIndex).StartsWith(" {\\*\\cs") || rtfToInsert.Substring(substringIndex).StartsWith(" {\\rtlch")))
					{
						nextStyleName = FindNextStyleStart(rtfToInsert.Substring(index) + nextStyle.Name.Length, false, out styleIndex);
						paraStylePlacements.Add(nextStyle);
						if (nextStyleName == null)
							break;
						nextStyle = tempStylesheet.FirstOrDefault(style => style.Key.StartsWith(nextStyleName)).Value;
						if (nextStyle == null)
						{
							index += styleIndex - 6;
							nextStyle = FindNextRtfStyle(rtfToInsert.Substring(index), colorTable, false, out styleIndex);
						}
						if (stylesheet.Style(nextStyle.Name) == null)
							AddStyle(nextStyle, stylesheet);
					}
					else
					{
						bldr.ReplaceTsString((bldr.Text ?? string.Empty).Length, (bldr.Text ?? string.Empty).Length,
											 TsStrFactoryClass.Create().MakeString(
												rtfToInsert.Substring(rtfToInsert.IndexOf(" ", index) + 1,
																	  rtfToInsert.IndexOf("}", index) - rtfToInsert.IndexOf(" ", index) - 1)
													.Replace(@"\par", ""), 1));
						paraStylePlacements.Add(nextStyle);
						if (rtfToInsert.Substring(rtfToInsert.IndexOf("}", index) - 4, 4) == @"\par")
						{
							textToInsert.Add(bldr.GetString());
							bldr = TsStrFactoryClass.Create().MakeString("", 1).GetBldr();
						}
						continue;
					}
				}
				else if (((textToInsert.Count == 0 && bldr.Text == null) || rtfToInsert.Substring(index - 15, 4) == @"\par"))
				{
					paraStylePlacements.Add(null);
				}
				if (rtfToInsert.IndexOf("}}", index) < styleIndex)
					break;
				index += styleIndex;
				int oldLength = (bldr.Text ?? string.Empty).Length;

				bldr.ReplaceTsString(oldLength, oldLength,
									 TsStrFactoryClass.Create().MakeString(
										rtfToInsert.Substring(rtfToInsert.IndexOf(" ", index) + 1,
															  rtfToInsert.IndexOf("}", index) - rtfToInsert.IndexOf(" ", index) - 1)
											.Replace(@"\par", ""), 1));
				bldr.SetStrPropValue(oldLength, (bldr.Text ?? string.Empty).Length, (int)FwTextPropType.ktptNamedStyle, nextStyle.Name);

				if (rtfToInsert.Substring(rtfToInsert.IndexOf("}", index) - 4, 4) == @"\par")
				{
					textToInsert.Add(bldr.GetString());
					bldr = TsStrFactoryClass.Create().MakeString("", 1).GetBldr();
				}
			}
			int ipPosition = insertionPoint.StringPosition;
			if (textToInsert.Count == 1)
			{
				insertionPoint.Hookup.InsertText(insertionPoint, textToInsert[0]);
				insertionPoint.ApplyStyle(paraStylePlacements.FirstOrDefault().Name);
			}
			else if (textToInsert.Count > 1)
			{
				if (!InsertLines(new MultiLineInsertData(insertionPoint, textToInsert, paraStylePlacements), out makeSelection))
					return false;
			}
			else
			{
				return false;
			}
			return true;
		}

		public virtual void AddStyle(IStyle style, IStylesheet stylesheet)
		{
		}

		public bool InsertLines(MultiLineInsertData input, out Action makeSelection)
		{
			var selection = input.Selection as InsertionPoint;
			int index = ItemIndex(selection) + 1;
			if (input.InsertedTsStrLines != null)
			{
				var styles = new IStyle[input.InsertedTsStrLines.Count + 1];
				if (input.ParaStyles != null)
					styles = input.ParaStyles.ToArray();
				SplitParagraph(input, out makeSelection);

				for (int i = 0; i < input.InsertedTsStrLines.Count; i++)
				{
					ITsString str = input.InsertedTsStrLines[i];
					IStyle style = styles[i + 1];
					var item = MakeListItem(index, false);
					SetString(item, str);
					if (style != null)
						ApplyParagraphStyle(index, 1, style.Name);
					index++;
				}
			}
			else
			{
				var styles = new IStyle[input.InsertedStringLines.Count + 1];
				if (input.ParaStyles != null)
					styles = input.ParaStyles.ToArray();
				SplitParagraph(input, out makeSelection);

				for (int i = 0; i < input.InsertedStringLines.Count; i++)
				{
					string str = input.InsertedStringLines[i];
					IStyle style = styles[i + 1];
					var item = MakeListItem(index, false);
					SetString(item, str);
					if (style != null)
						ApplyParagraphStyle(index, 1, style.Name);
					index++;
				}
			}
			return true;
		}

		/// <summary>
		/// Move whatever comes after the given IP to the start of the destination object.
		/// </summary>
		public virtual void MoveMaterialAfterIpToStart(InsertionPoint ip, string append, string prepend, T destination)
		{
			var stringHookup = ip.Hookup as StringHookup;
			if (stringHookup != null)
			{
				stringHookup.InsertText(ip, append);
				ip.StringPosition += append.Length;
				var run = stringHookup.ParaBox.Source.ClientRuns[stringHookup.ClientRunIndex] as StringClientRun;
				if (run != null) // should always be true, I think.
				{
					string oldValue = run.Contents;
					if (ip.StringPosition > oldValue.Length)
						ip.StringPosition = oldValue.Length;
					string move = prepend + oldValue.Substring(ip.StringPosition);
					string keep = oldValue.Substring(0, ip.StringPosition);
					stringHookup.Writer(keep);
					InsertAtStartOfNewObject(ip, move, destination);
				}
				return;
			}
			var tssHookup = ip.Hookup as TssHookup;
			if (tssHookup != null)
			{
				tssHookup.InsertText(ip, append);
				ip.StringPosition += append.Length;
				var run = tssHookup.ParaBox.Source.ClientRuns[tssHookup.ClientRunIndex] as TssClientRun;
				if (run != null) // should always be true, I think
				{
					var oldValue = run.Tss;
					var move = oldValue.GetSubstring(ip.StringPosition, oldValue.Length);
					var bldr = move.GetBldr();
					bldr.Replace(0, 0, prepend, null);
					move = bldr.GetString();
					var keep = oldValue.GetSubstring(0, ip.StringPosition);
					tssHookup.Writer(keep);
					InsertAtStartOfNewObject(ip, move, destination);
				}
			}
		}

		/// <summary>
		/// Move whatever comes after the given IP to the start of the destination object.
		/// </summary>
		public virtual void MoveMaterialAfterIpToStart(InsertionPoint ip, ITsString append, ITsString prepend, T destination)
		{
			var stringHookup = ip.Hookup as StringHookup;
			if (stringHookup != null)
			{
				stringHookup.InsertText(ip, append.Text);
				ip.StringPosition += append.Length;
				var run = stringHookup.ParaBox.Source.ClientRuns[stringHookup.ClientRunIndex] as StringClientRun;
				if (run != null) // should always be true, I think.
				{
					string oldValue = run.Contents;
					if (ip.StringPosition > oldValue.Length)
						ip.StringPosition = oldValue.Length;
					string move = prepend + oldValue.Substring(ip.StringPosition);
					string keep = oldValue.Substring(0, ip.StringPosition);
					stringHookup.Writer(keep);
					InsertAtStartOfNewObject(ip, move, destination);
				}
				return;
			}
			var tssHookup = ip.Hookup as TssHookup;
			if (tssHookup != null)
			{
				tssHookup.InsertText(ip, append);
				ip.StringPosition += append.Length;
				var run = tssHookup.ParaBox.Source.ClientRuns[tssHookup.ClientRunIndex] as TssClientRun;
				if (run != null) // should always be true, I think
				{
					var oldValue = run.Tss;
					var move = oldValue.GetSubstring(ip.StringPosition, oldValue.Length);
					var bldr = move.GetBldr();
					bldr.ReplaceTsString(0, 0, prepend);
					move = bldr.GetString();
					var keep = oldValue.GetSubstring(0, ip.StringPosition);
					tssHookup.Writer(keep);
					InsertAtStartOfNewObject(ip, move, destination);
				}
			}
		}

		/// <summary>
		/// Insert the indicated text at the start of the new object. The IP indicates where it came from,
		/// which may be significant for subclasses where the display of T is complex. This is used for
		/// moving the tail end of a split string, when inserting a line break into a paragraph. The destination
		/// may be assumed newly created and empty.
		/// </summary>
		public virtual void InsertAtStartOfNewObject(InsertionPoint source, string move, T destination)
		{
			SetString(destination, move);
		}

		/// <summary>
		/// Insert the indicated text at the start of the new object. The IP indicates where it came from,
		/// which may be significant for subclasses where the display of T is complex. This is used for
		/// moving the tail end of a split string, when inserting a line break into a paragraph. The destination
		/// may be assumed newly created and empty.
		/// </summary>
		public virtual void InsertAtStartOfNewObject(InsertionPoint source, ITsString move, T destination)
		{
			SetString(destination, move);
		}

		/// <summary>
		/// This is the simplest method to override. It sets the paragraph content property to the given string.
		/// If you do not override this you need to override all its senders (or else the TsString version,
		/// if your main content is TsStrings). This method would be abstract, except that some clients may
		/// prefer to override callers, and some need to override the TsString overload instead.
		/// </summary>
		public virtual void SetString(T destination, string val)
		{
			throw new NotImplementedException("subclass must override callers of SetString or implement it");
		}

		/// <summary>
		/// This is the simplest method to override for TsString props.
		/// It sets the paragraph content property to the given TsString.
		/// If you do not override this you need to override all its senders (or else the string version,
		/// if your main content is strings). This method would be abstract, except that some clients may
		/// prefer to override callers, and some need to override the string overload instead.
		/// </summary>
		public virtual void SetString(T destination, ITsString val)
		{
			throw new NotImplementedException("subclass must override callers of SetString or implement it");
		}

		public virtual bool InsertPrecedingParagraph(InsertionPoint ip, out Action makeSelection)
		{
			int index = ItemIndex(ip);
			MakeListItem(index, true);
			// The selection is still at the start of the original paragraph. No makeSelection action is needed.
			makeSelection = () => DoNothing();
			return true;
		}

		static void DoNothing()
		{ }

		/// <summary>
		/// Replace the text in the range, presumed to extend across more than one paragraph,
		/// with the supplied text.
		/// This one serves also for backspace at start of paragraph and delete at end, since
		/// these can readily be transformed into deleting a range from the end of one para
		/// to the start of the next. The given range is replace by a simple string.
		/// Enhance JohnT: it would probably make sense to break this up, more like SplitParagraph,
		/// into further virtual methods that subclasses could override. But I haven't figured out how yet.
		/// </summary>
		public bool InsertString(RangeSelection range, string insert, out Action makeSelection)
		{
			makeSelection = () => DoNothing(); // a default.
			var firstIp = range.Start;
			var lastIp = range.End;
			int firstItemIndex = ItemIndex(firstIp);
			int lastItemIndex = ItemIndex(lastIp);
			if (firstItemIndex == lastItemIndex)
				throw new ArgumentException("Should not need to use ParagraphOps.InsertString for range in same item");
			int startPosition = firstIp.StringPosition;
			if (!MergeTextAfterSecondIntoFirst(firstIp, lastIp, insert))
				return false;
			DeleteItems(firstItemIndex + 1, lastItemIndex);
			makeSelection = () => SelectionBuilder.In(Hookup)[firstItemIndex].Offset(startPosition + insert.Length).Install();
			return true;
		}

		public virtual void ApplyParagraphStyle(int index, int numBoxes, string style)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Delete the specified range of items from the list.
		/// Enhance JohnT: it would generally be much preferable to delete all of them and then do one change notification.
		/// </summary>
		public virtual void DeleteItems(int firstIndex, int lastIndex)
		{
			for (int i = firstIndex; i <= lastIndex; i++)
				List.RemoveAt(firstIndex); // not at i, they keep moving up
		}

		/// <summary>
		/// Change the property containing firstIp to contain its current text before firstIp followed by
		/// the inserted text followed by the text after lastIp in that IP's property.
		/// Return true if successful. Typically used as part of
		/// deleting the range in between the two IPs and inserting the specified text (if any).
		/// </summary>
		public virtual bool MergeTextAfterSecondIntoFirst(InsertionPoint firstIp, InsertionPoint lastIp, string insert)
		{
			var firstStringHookup = firstIp.Hookup as StringHookup;
			var lastStringHookup = lastIp.Hookup as StringHookup;
			if (firstStringHookup != null && lastStringHookup != null)
			{
				var firstRun = firstStringHookup.ParaBox.Source.ClientRuns[firstStringHookup.ClientRunIndex] as StringClientRun;
				var lastRun = lastStringHookup.ParaBox.Source.ClientRuns[lastStringHookup.ClientRunIndex] as StringClientRun;
				if (firstRun == null || lastRun == null)
					return false; //
				string tailend = lastRun.Contents.Substring(lastIp.StringPosition);
				string leadIn = firstRun.Contents.Substring(0, firstIp.StringPosition);
				string newContents = leadIn + insert + tailend; // what if they're different writing systems??
				firstStringHookup.Writer(newContents);
				return true;
			}
			var firstTssHookup = firstIp.Hookup as TssHookup;
			var lastTssHookup = lastIp.Hookup as TssHookup;
			if (firstTssHookup != null && lastTssHookup != null)
			{
				var firstRun = firstTssHookup.ParaBox.Source.ClientRuns[firstTssHookup.ClientRunIndex] as TssClientRun;
				var lastRun = lastTssHookup.ParaBox.Source.ClientRuns[lastTssHookup.ClientRunIndex] as TssClientRun;
				if (firstRun == null || lastRun == null)
					return false; //
				var tailend = lastRun.Tss.GetSubstring(lastIp.StringPosition, lastRun.Tss.Length);
				var leadIn = firstRun.Tss.GetSubstring(0, firstIp.StringPosition);
				var bldr = leadIn.GetBldr();
				// Enhance JohnT: get props from start of selected text, if any.
				bldr.Replace(bldr.Length, bldr.Length, insert, null);
				bldr.ReplaceTsString(bldr.Length, bldr.Length, tailend);
				var newContents = bldr.GetString();
				firstTssHookup.Writer(newContents);
				return true;
			}
			return false;
		}
	}

	public abstract class ParagraphOperations<T> : BaseParagraphOperations<T> where T : new()
	{
		/// <summary>
		/// This should often be overridden, because (except for special lists like FdoVectors or
		/// MonitoredList) inserting an item like this will not generate any event.
		/// </summary>
		public override T MakeListItem(int index, bool ipAtStartOfPara)
		{
			var result = new T();
			List.Insert(index, result);
			return result;
		}
	}
}
