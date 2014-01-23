// Copyright (c) 2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Provides the text for the given selection (or if no selection is specified, the entire
	/// rootbox (Select-All))
	/// </summary>
	public class SimpleRootSiteTextRangeProvider : ITextRangeProvider
	{

		private readonly SimpleRootSite m_site; // the control hosting this provider
		private readonly IVwRootBox m_rootb;
		private IVwSelection m_vwTextRange;

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleRootSiteTextRangeProvider"/> class.
		/// </summary>
		/// <param name="site">the root site.(by default provide entire text).</param>
		public SimpleRootSiteTextRangeProvider(SimpleRootSite site)
			: this(site, null)
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleRootSiteTextRangeProvider"/> class.
		/// </summary>
		/// <param name="textRange">The selection from which to extract the text</param>
		/// <param name="site">The site in which the textRange selection has been made.</param>
		public SimpleRootSiteTextRangeProvider(SimpleRootSite site, IVwSelection textRange)
		{
			m_site = site;
			m_rootb = site.RootBox;
			m_vwTextRange = textRange;
		}

		private IVwSelection TextRange
		{
			get
			{
				if (m_vwTextRange == null)
				{
					// simulate Control-A (Select All) but don't install the selection.
					if (m_vwTextRange == null)
					{
						IVwSelection selDocument = MakeTextSelectionEntireText(false);
						ValidateTextRange(selDocument);
						m_vwTextRange = selDocument;
					}
				}
				return m_vwTextRange;
			}

			//set
			//{
			//    ValidateTextRange(value);
			//    m_vwTextRange = value;
			//}
		}

		private static void ValidateTextRange(IVwSelection value)
		{
			if (value == null || !value.IsRange || value.SelType == VwSelType.kstPicture)
				throw new ArgumentException("TextRange must be set to valid range of a text.");
		}

		#region ITextRangeProvider Members

		/// <summary>
		/// Adds to the collection of highlighted text in a text container that supports multiple, disjoint selections.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">
		/// If text provider does not support multiple, disjoint selections (that is, <see cref="P:System.Windows.Automation.Provider.ITextProvider.SupportedTextSelection"/> must have a value of Multiple).
		/// </exception>
		public void AddToSelection()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a new <see cref="T:System.Windows.Automation.Provider.ITextRangeProvider"/> identical to the original <see cref="T:System.Windows.Automation.Provider.ITextRangeProvider"/> and inheriting all properties of the original.
		/// </summary>
		/// <returns>
		/// The new text range. A null reference (Nothing in Microsoft Visual Basic .NET) is never returned.
		/// </returns>
		public ITextRangeProvider Clone()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a value that indicates whether the span (the <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.Start"/> endpoint to the <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.End"/> endpoint) of a text range is the same as another text range.
		/// </summary>
		/// <param name="range">A text range to compare</param>
		/// <returns>
		/// true if the span of both text ranges is identical; otherwise false.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// If the range being compared does not come from the same text provider.
		/// </exception>
		public bool Compare(ITextRangeProvider range)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a value that specifies whether two text ranges have identical endpoints.
		/// </summary>
		/// <param name="endpoint">The <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.Start"/> or <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.End"/> endpoint of the caller.</param>
		/// <param name="targetRange">The target range for comparison.</param>
		/// <param name="targetEndpoint">The <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.Start"/> or <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.End"/> endpoint of the target.</param>
		/// <returns>
		/// Returns a negative value if the caller's endpoint occurs earlier in the text than the target endpoint.
		/// Returns zero if the caller's endpoint is at the same location as the target endpoint.
		/// Returns a positive value if the caller's endpoint occurs later in the text than the target endpoint.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// If <paramref name="targetRange"/> is from a different text provider.
		/// </exception>
		public int CompareEndpoints(System.Windows.Automation.Text.TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, System.Windows.Automation.Text.TextPatternRangeEndpoint targetEndpoint)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Expands the text range to the specified text unit.
		/// </summary>
		/// <param name="unit">The textual unit.</param>
		public void ExpandToEnclosingUnit(System.Windows.Automation.Text.TextUnit unit)
		{
			//SelectionHelper helper = SelectionHelper.Create(this);
			//if (helper != null && helper.Selection != null)
			//{
			//    helper.Selection.ExtendToStringBoundaries();
			//    //EditingHelper.SetKeyboardForSelection(helper.Selection);
			//}
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a text range subset that has the specified attribute value.
		/// </summary>
		/// <param name="attribute">The attribute to search for.</param>
		/// <param name="value">The attribute value to search for. This value must match the type specified for the attribute.</param>
		/// <param name="backward">true if the last occurring text range should be returned instead of the first; otherwise false.</param>
		/// <returns>
		/// A text range having a matching attribute and attribute value; otherwise null (Nothing in Microsoft Visual Basic .NET).
		/// </returns>
		public ITextRangeProvider FindAttribute(int attribute, object value, bool backward)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a text range subset that contains the specified text.
		/// </summary>
		/// <param name="text">The text string to search for.</param>
		/// <param name="backward">true if the last occurring text range should be returned instead of the first; otherwise false.</param>
		/// <param name="ignoreCase">true if case should be ignored; otherwise false.</param>
		/// <returns>
		/// A text range matching the specified text; otherwise null (Nothing in Microsoft Visual Basic .NET).
		/// </returns>
		public ITextRangeProvider FindText(string text, bool backward, bool ignoreCase)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Retrieves the value of the specified attribute across the text range.
		/// </summary>
		/// <param name="attribute">The text attribute.</param>
		/// <returns>
		/// Retrieves an object representing the value of the specified attribute. For example, GetAttributeValue(TextPattern.FontNameAttribute) would return a string that represents the font name of the text range while GetAttributeValue(TextPattern.IsItalicAttribute) would return a value of type <see cref="T:System.Boolean"/>.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// If the specified attribute is not valid.
		/// </exception>
		public object GetAttributeValue(int attribute)
		{
			if (attribute == TextPatternIdentifiers.TextFlowDirectionsAttribute.Id)
			{
				// todo: base the text flow off the initial ws of the text?
				//return FlowDirections.Default;
				return null;
			}
			if (attribute == TextPatternIdentifiers.IsReadOnlyAttribute.Id)
			{
				if (TextRange != null)
					return m_site.Invoke(() => TextRange.IsValid && !TextRange.IsEditable);
				return null;
			}
			return null;
		}

		/// <summary>
		/// Get the rectangles for each visible line of text on the screen.
		/// </summary>
		/// <returns></returns>
		public double[] GetBoundingRectangles()
		{
			return new double[0];
		}

		/// <summary>
		/// Retrieves a collection of all embedded objects that fall within the text range.
		/// </summary>
		/// <returns>
		/// A collection of child objects that fall within the range. Children that overlap with the text range but are not entirely enclosed by it will also be included in the collection.
		/// Returns an empty collection if there are no child objects.
		/// </returns>
		public IRawElementProviderSimple[] GetChildren()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns the innermost control that encloses the text range.
		/// </summary>
		/// <returns>
		/// The enclosing control, typically the text provider that supplies the text range. However, if the text provider supports child elements such as tables or hyperlinks, then the enclosing element could be a descendant of the text provider.
		/// </returns>
		public IRawElementProviderSimple GetEnclosingElement()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Retrieves the plain text of the range.
		/// </summary>
		/// <param name="maxLength">The maximum length of the string to return. Use -1 if no limit is required.</param>
		/// <returns>
		/// The plain text of the text range, possibly truncated at the specified <paramref name="maxLength"/>.
		/// </returns>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// If <paramref name="maxLength"/> is less than -1.
		/// </exception>
		public string GetText(int maxLength)
		{
			ITsString tss = null;
			if (!m_site.Invoke(() => TextRange.IsValid))
				return string.Empty;
			try
			{
				m_site.Invoke(() => TextRange.GetSelectionString(out tss, "; "));
			}
			catch (System.Runtime.InteropServices.COMException e)
			{
				// Writing system is required for every run in a TsString (except for newlines)
			}
			if (tss != null && tss.Text != null)
			{
				string text;
				if (maxLength == -1)
					text = tss.Text;
				else
					text = tss.Text.Substring(0, maxLength);
				return text.TrimEnd('\r', '\n');
			}
			return string.Empty;
		}

		/// <summary>
		/// Makes the text selection from the entire text (editable and noneditable).
		/// </summary>
		/// <param name="fInstall">if set to <c>true</c> install the selection.</param>
		/// <returns></returns>
		IVwSelection MakeTextSelectionEntireText(bool fInstall)
		{
			// simulate Control-A (Select All) but don't install the selection.
			return SelectAll(m_rootb, false, false);
		}

		/// <summary>
		/// Selects the contents of the whole rootbox.
		/// </summary>
		/// <param name="rootb"></param>
		/// <param name="fEditable">if set to <c>true</c> tries to start and end the selection in an editable field.</param>
		/// <param name="fInstall">if set to <c>true</c> installs the selection.</param>
		/// <returns></returns>
		internal static IVwSelection SelectAll(IVwRootBox rootb, bool fEditable, bool fInstall)
		{
			IVwSelection selDocument = null;
			IVwSelection selStart = rootb.MakeSimpleSel(true, fEditable, false, false);
			IVwSelection selEnd = rootb.MakeSimpleSel(false, fEditable, false, false);
			if (selStart != null && selEnd != null)
				selDocument = rootb.MakeRangeSelection(selStart, selEnd, fInstall);
			return selDocument;
		}

		/// <summary>
		/// Moves the text range the specified number of text units.
		/// </summary>
		/// <param name="unit">The text unit boundary.</param>
		/// <param name="count">The number of text units to move.
		/// A positive value moves the text range forward, a negative value moves the text range backward, and 0 has no effect.</param>
		/// <returns>
		/// The number of units actually moved. This can be less than the number requested if either of the new text range endpoints is greater than or less than the <see cref="P:System.Windows.Automation.Provider.ITextProvider.DocumentRange"/> endpoints.
		/// </returns>
		public int Move(System.Windows.Automation.Text.TextUnit unit, int count)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Moves one endpoint of a text range to the specified endpoint of a second text range.
		/// </summary>
		/// <param name="endpoint">The endpoint to move.</param>
		/// <param name="targetRange">Another range from the same text provider.</param>
		/// <param name="targetEndpoint">An endpoint on the other range.</param>
		public void MoveEndpointByRange(System.Windows.Automation.Text.TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, System.Windows.Automation.Text.TextPatternRangeEndpoint targetEndpoint)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Moves one endpoint of the text range the specified number of text units within the document range.
		/// </summary>
		/// <param name="endpoint">The endpoint to move.</param>
		/// <param name="unit">The textual unit for moving.</param>
		/// <param name="count">The number of units to move. A positive value moves the endpoint forward. A negative value moves backward. A value of 0 has no effect.</param>
		/// <returns>
		/// The number of units actually moved, which can be less than the number requested if moving the endpoint runs into the beginning or end of the document.
		/// </returns>
		public int MoveEndpointByUnit(System.Windows.Automation.Text.TextPatternRangeEndpoint endpoint, System.Windows.Automation.Text.TextUnit unit, int count)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes a highlighted section of text, corresponding to the caller's <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.Start"/> and <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.End"/> endpoints, from the collection of highlighted text in a text container that supports multiple, disjoint selections.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">
		/// If text provider does not support multiple, disjoint selections (for example, (see <c>System.Windows.Automation.TextPattern.SupportedTextSelection</c>) must have a value of Multiple).
		/// </exception>
		public void RemoveFromSelection()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Causes the text control to scroll vertically until the text range is visible in the viewport.
		/// </summary>
		/// <param name="alignToTop">true if the text control should be scrolled so the text range is flush with the top of the viewport; false if it should be flush with the bottom of the viewport.</param>
		public void ScrollIntoView(bool alignToTop)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Highlights text in the text control corresponding to the text range <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.Start"/> and <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.End"/> endpoints.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">
		/// Occurs when text selection is not supported by the text control.
		/// </exception>
		public void Select()
		{
			throw new NotImplementedException();
		}

		#endregion

	}
}
