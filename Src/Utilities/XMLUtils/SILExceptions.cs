// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SILExceptions.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
//	The idea is to eventually display a nice dialog box when the configuration author makes a mistake
//	or there is a problem with the installation.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Xml;

namespace SIL.Utils
{
	/// <summary>
	/// Use this exception when it looks like the configuration XML itself has a problem.
	/// Thus, end-user should never see these exceptions.
	/// </summary>
	public class ConfigurationException : ApplicationException
	{
		XmlNode m_node=null;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigurationException"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ConfigurationException(string message, XmlNode node) :base(message + "\r\n"+node.OuterXml)
		{
			m_node = node;
		}

		public ConfigurationException(string message) :base(message)
		{

		}

		public void ShowDialog()
		{
			System.Windows.Forms.MessageBox.Show(this.Message, XmlUtilsStrings.XMLConfigurationError, System.  Windows.Forms.MessageBoxButtons.OK,System.  Windows.  Forms.  MessageBoxIcon.Exclamation);
		}
	}

	/// <summary>
	/// Use this exception when the format of the configuration XML
	/// may be fine, but there is a run-time linking problem with an assembly or class that was specified.
	/// </summary>
	public class RuntimeConfigurationException : ApplicationException
	{
		public RuntimeConfigurationException(string message) :base(message)
		{

		}

		/// <summary>
		/// Use this one if you are inside of a catch block where you have access to the original exception
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public RuntimeConfigurationException(string message, Exception innerException) :base(message, innerException)
		{

		}
	}
}
