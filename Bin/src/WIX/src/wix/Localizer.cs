//-------------------------------------------------------------------------------------------------
// <copyright file="Localizer.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Parses localization files and localizes database values.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Globalization;
	using System.IO;
	using System.Xml;
	using System.Xml.XPath;

	/// <summary>
	/// Parses localization files and localizes database values.
	/// </summary>
	public class Localizer
	{
		private int codepage;
		private StringDictionary stringDictionary;

		/// <summary>
		/// Instantiate a new Localizer.
		/// </summary>
		public Localizer()
		{
			this.codepage = -1;
			this.stringDictionary = new StringDictionary();
		}

		/// <summary>
		/// Event for messages.
		/// </summary>
		public event MessageEventHandler Message;

		/// <summary>
		/// Gets the codepage.
		/// </summary>
		/// <value>The codepage.</value>
		public int Codepage
		{
			get { return this.codepage; }
		}

		/// <summary>
		/// Get a localized data value.
		/// </summary>
		/// <param name="str">The un-localized data value.</param>
		/// <returns>The localized data value.</returns>
		public string GetLocalizedValue(string str)
		{
			if (null != str)
			{
				int dollar = 0;

				// for all the localizable columns replace any $(loc.XXX) markers
				// with the appropriate string localization
				while (dollar < str.Length && 0 <= (dollar = str.IndexOf('$', dollar)))
				{
					string strVar = str.Substring(dollar + 1);
					if (strVar.StartsWith("(loc."))
					{
						int closeParen = strVar.IndexOf(')', 5);
						if (-1 == closeParen)
						{
							this.OnMessage(WixErrors.LocalizationStringUnclosed(str));
							return str;
						}

						strVar = strVar.Substring(5, closeParen - 5);
						string locString = this.stringDictionary[strVar];
						if (null == locString)
						{
							this.OnMessage(WixErrors.LocalizationStringUnknown(strVar));
							return str;
						}

						str = String.Concat(str.Substring(0, dollar), locString, str.Substring(dollar + 1 + closeParen + 1));
						dollar = dollar + locString.Length;
					}
					else
					{
						dollar++; // move past the dollar and search again
					}
				}
			}

			return str;
		}

		/// <summary>
		/// Load localized strings from a wixloc file.
		/// </summary>
		/// <param name="path">Path to the wixloc file.</param>
		public void LoadFromFile(string path)
		{
			SourceLineNumberCollection sourceLineNumbers = SourceLineNumberCollection.FromFileName(path);

			try
			{
				XPathDocument doc = new XPathDocument(path);
				XPathNavigator nav = doc.CreateNavigator();

				nav.MoveToRoot();
				if (nav.MoveToFirstChild())
				{
					// move to the first element (skipping comments and stuff)
					while (XPathNodeType.Element != nav.NodeType)
					{
						nav.MoveToNext();
					}

					if ("WixLocalization" != nav.LocalName)
					{
						this.OnMessage(WixErrors.InvalidDocumentElement(sourceLineNumbers, nav.Name, "localization", "WixLocalization"));
					}

					if (nav.MoveToAttribute("Codepage", String.Empty))
					{
						try
						{
							int newCodepage = Convert.ToInt32(nav.Value, CultureInfo.InvariantCulture.NumberFormat);

							// if the codepage has not been set
							if (-1 == this.codepage)
							{
								this.codepage = newCodepage;
							}
							else if (newCodepage != this.codepage) // fail if codepage has already been specified but is different
							{
								this.OnMessage(WixErrors.DuplicateLocalizedCodepage(sourceLineNumbers, nav.Value));
							}
						}
						catch (FormatException)
						{
							this.OnMessage(WixErrors.IllegalIntegerValue(null, "WixLocalization", nav.Name, nav.Value));
						}
						catch (OverflowException)
						{
							this.OnMessage(WixErrors.IllegalIntegerValue(null, "WixLocalization", nav.Name, nav.Value));
						}

						nav.MoveToParent();
					}

					if (nav.MoveToFirstChild())
					{
						do
						{
							if (XPathNodeType.Element == nav.NodeType)
							{
								string localizationId = null;
								string localizationValue = String.Empty;

								if ("String" != nav.LocalName)
								{
									continue;
								}

								if (nav.MoveToAttribute("Id", String.Empty))
								{
									localizationId = nav.Value;
									nav.MoveToParent();
								}

								if (null == localizationId)
								{
									this.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, "String", "Id"));
								}

								if (nav.MoveToFirstChild())
								{
									// find the text() of the element
									do
									{
										if (XPathNodeType.Text == nav.NodeType)
										{
											localizationValue = nav.Value;
											break; // found the text() of the element and bail
										}
									}
									while (nav.MoveToNext());

									nav.MoveToParent(); // back up
								}

								if (!this.stringDictionary.ContainsKey(localizationId))
								{
									this.stringDictionary.Add(localizationId, localizationValue);
								}
								else // duplicate localization identifier
								{
									this.OnMessage(WixErrors.DuplicateLocalizationIdentifier(sourceLineNumbers, localizationId));
								}
							}
						}
						while (nav.MoveToNext());
					}
				}
			}
			catch (DirectoryNotFoundException)
			{
				this.OnMessage(WixErrors.FileNotFound(path));
			}
			catch (FileNotFoundException)
			{
				this.OnMessage(WixErrors.FileNotFound(path));
			}
			catch (XmlException e)
			{
				this.OnMessage(WixErrors.InvalidXml(sourceLineNumbers, "localization", e.Message));
			}
		}

		/// <summary>
		/// Sends a message to the message delegate if there is one.
		/// </summary>
		/// <param name="mea">Message event arguments.</param>
		private void OnMessage(MessageEventArgs mea)
		{
			if (null != this.Message)
			{
				this.Message(this, mea);
			}
		}
	}
}