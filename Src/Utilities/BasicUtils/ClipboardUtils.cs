// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ClipboardUtils.cs
// Responsibility: EberhardB
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Diagnostics;

namespace SIL.Utils
{
	#region IClipboard interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for clipboard methods. This helps in providing a system independent, reliable
	/// implementation for unit tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IClipboard
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether there is data on the Clipboard in the Text or UnicodeText format,
		/// depending on the operating system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool ContainsText();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves text data from the Clipboard in the Text or UnicodeText format, depending
		/// on the operating system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string GetText();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the data that is currently on the system Clipboard.
		/// </summary>
		/// <returns>An IDataObject that represents the data currently on the Clipboard, or
		/// <c>null</c> if there is no data on the Clipboard.</returns>
		/// ------------------------------------------------------------------------------------
		IDataObject GetDataObject();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds text data to the Clipboard in the Text or UnicodeText format, depending on the
		/// operating system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void SetText(string text);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds text data to the Clipboard in the format indicated by the specified
		/// TextDataFormat value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void SetText(string text, TextDataFormat format);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Places nonpersistent data on the system Clipboard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void SetDataObject(object data);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Places data on the system Clipboard and specifies whether the data should remain on
		/// the Clipboard after the application exits.
		/// </summary>
		/// <param name="data">The data to place on the Clipboard.</param>
		/// <param name="copy"><c>true</c> if you want data to remain on the Clipboard after
		/// this application exits; otherwise, <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		void SetDataObject(object data, bool copy);
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This basically just wraps the Clipboard class to allow replacing it with a test stub
	/// during unit tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class ClipboardUtils
	{
		private static IClipboard s_Clipboard = new ClipboardAdapter();

#if __MonoCS__ // inefficient work around for mono bug https://bugzilla.novell.com/show_bug.cgi?id=596402
			/// ----------------------------------------------------------------------------------------
			/// <summary>
			/// Please delete when mono bug https://bugzilla.novell.com/show_bug.cgi?id=596402 is fixed
			/// </summary>
			/// ----------------------------------------------------------------------------------------
			public static string ConvertLiternalUnicodeValues(string text)
			{
				Regex rgx = new Regex(@"\\u[0-9a-f]{4}");
				while(rgx.IsMatch(text))
				{
					var literalValue = rgx.Match(text);
					Debug.Assert(literalValue.Length == 6);

					var num = uint.Parse(literalValue.Value.Substring(2, 4), NumberStyles.AllowHexSpecifier);
					string nonLiteralValue = String.Format("{0}", (char)num);

					text = text.Replace(literalValue.Value, nonLiteralValue);
				}
			   return text;
			}
#endif
		#region ClipboardUtils Manager class
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Allows setting a different clipboard adapter (for testing purposes)
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public static class Manager
		{
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Sets the clipboard adapter.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public static void SetClipboardAdapter(IClipboard adapter)
			{
				s_Clipboard = adapter;
			}
		}
		#endregion

		#region ClipboardAdapter class
		private class ClipboardAdapter: IClipboard
		{

			#region IClipboard Members

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Indicates whether there is data on the Clipboard in the Text or UnicodeText format,
			/// depending on the operating system.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public bool ContainsText()
			{
				return Clipboard.ContainsText();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Retrieves text data from the Clipboard in the Text or UnicodeText format, depending
			/// on the operating system.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public string GetText()
			{
#if !__MonoCS__
				return Clipboard.GetText();
#else // work around for mono bug https://bugzilla.novell.com/show_bug.cgi?id=596402
				return ConvertLiternalUnicodeValues(Clipboard.GetText());
#endif
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Retrieves the data that is currently on the system Clipboard.
			/// </summary>
			/// <returns>
			/// An IDataObject that represents the data currently on the Clipboard, or
			/// <c>null</c> if there is no data on the Clipboard.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public IDataObject GetDataObject()
			{
				return Clipboard.GetDataObject();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Adds text data to the Clipboard in the Text or UnicodeText format, depending on the
			/// operating system.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void SetText(string text)
			{
				Clipboard.SetText(text);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Adds text data to the Clipboard in the format indicated by the specified
			/// TextDataFormat value.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void SetText(string text, TextDataFormat format)
			{
				Clipboard.SetText(text, format);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Places nonpersistent data on the system Clipboard.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void SetDataObject(object data)
			{
				Clipboard.SetDataObject(data);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Places data on the system Clipboard and specifies whether the data should remain on
			/// the Clipboard after the application exits.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void SetDataObject(object data, bool copy)
			{
				Clipboard.SetDataObject(data, copy);
			}

			#endregion
		}
		#endregion

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether there is data on the Clipboard in the Text or UnicodeText format,
		/// depending on the operating system.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public static bool ContainsText()
		{
			return s_Clipboard.ContainsText();
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves text data from the Clipboard in the Text or UnicodeText format, depending
		/// on the operating system.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public static string GetText()
		{
			return s_Clipboard.GetText();
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the data that is currently on the system Clipboard.
		/// </summary>
		/// <returns>
		/// An IDataObject that represents the data currently on the Clipboard, or
		/// <c>null</c> if there is no data on the Clipboard.
		/// </returns>
		/// --------------------------------------------------------------------------------
		public static IDataObject GetDataObject()
		{
			return s_Clipboard.GetDataObject();
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Adds text data to the Clipboard in the Text or UnicodeText format, depending on the
		/// operating system.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public static void SetText(string text)
		{
			s_Clipboard.SetText(text);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Adds text data to the Clipboard in the format indicated by the specified
		/// TextDataFormat value.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public static void SetText(string text, TextDataFormat format)
		{
			s_Clipboard.SetText(text, format);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Places nonpersistent data on the system Clipboard.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public static void SetDataObject(object data)
		{
			s_Clipboard.SetDataObject(data);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Places data on the system Clipboard and specifies whether the data should remain on
		/// the Clipboard after the application exits.
		/// </summary>
		/// <param name="data">The data to place on the Clipboard.</param>
		/// <param name="copy"><c>true</c> if you want data to remain on the Clipboard after
		/// this application exits; otherwise, <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		public static void SetDataObject(object data, bool copy)
		{
			s_Clipboard.SetDataObject(data, copy);
		}
	}
}
