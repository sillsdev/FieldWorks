// Copyright (c) 2024 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SIL.Extensions;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.Utils;
using SIL.WritingSystems;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	public class GrammarExportDialog : ExportDialog
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public GrammarExportDialog(Mediator mediator, PropertyTable propertyTable)
			: base(mediator, propertyTable)
		{
			m_helpTopic = "khtpExportLexicon";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the relative path to the export configuration files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string ConfigurationFilePath
		{
			get { return String.Format("Language Explorer{0}Export Templates{0}Grammar", Path.DirectorySeparatorChar); }
		}

	}
}
