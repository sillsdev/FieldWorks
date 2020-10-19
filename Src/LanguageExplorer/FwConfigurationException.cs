// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace LanguageExplorer
{
	/// <summary>
	/// Use this exception when it looks like the configuration XML itself has a problem.
	/// Thus, end-user should never see these exceptions.
	/// </summary>
	public class FwConfigurationException : ApplicationException
	{
		/// <summary />
		public FwConfigurationException(string message, XElement columnSpecificationElement, Exception exInner)
			: base(message + Environment.NewLine + columnSpecificationElement, exInner)
		{
		}

		/// <summary />
		public FwConfigurationException(string message, XmlNode node)
			: base(message + Environment.NewLine + node.OuterXml)
		{
		}

		/// <summary />
		public FwConfigurationException(string message, XElement columnSpecificationElement)
			: base(message + Environment.NewLine + columnSpecificationElement)
		{
		}

		/// <summary />
		public FwConfigurationException(string message, Exception exInner)
			: base(message, exInner)
		{
		}

		/// <summary />
		public FwConfigurationException(string message)
			: base(message)
		{
		}

		public void ShowDialog()
		{
			MessageBox.Show(Message, LanguageExplorerResources.XMLConfigurationError, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}
}