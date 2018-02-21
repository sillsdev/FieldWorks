// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// Encapsulates the accessors that controls need for setting up text boxes
	/// with stylesheets, so that such controls can use WritingSystemAndStylesheetHelper
	/// to setup this information.
	/// </summary>
	public interface IWritingSystemAndStylesheet
	{
		/// <summary></summary>
		System.Drawing.Font Font { get; set; }
		/// <summary></summary>
		IVwStylesheet StyleSheet { get; set; }
		/// <summary></summary>
		int WritingSystemCode { get; set; }
		/// <summary></summary>
		ILgWritingSystemFactory WritingSystemFactory { get; set; }
	}
}
