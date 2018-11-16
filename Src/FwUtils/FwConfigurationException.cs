// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Use this exception when it looks like the configuration XML itself has a problem.
	/// Thus, end-user should never see these exceptions.
	/// </summary>
	public class FwConfigurationException : ApplicationException
	{
		/// <summary />
		public FwConfigurationException(string message, XmlNode node, Exception exInner)
			: base(message + Environment.NewLine + node.OuterXml, exInner)
		{
		}

		/// <summary />
		public FwConfigurationException(string message, XElement node, Exception exInner)
			: base(message + Environment.NewLine + node, exInner)
		{
		}

		/// <summary />
		public FwConfigurationException(string message, XmlNode node)
			: base(message + Environment.NewLine + node.OuterXml)
		{
		}

		/// <summary />
		public FwConfigurationException(string message, XElement node)
			: base(message + Environment.NewLine + node)
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
			MessageBox.Show(Message, FwUtilsStrings.XMLConfigurationError, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}
}