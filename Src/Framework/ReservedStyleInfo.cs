// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Holds info about certain reserved styles. This info overrides any conflicting info
	/// in the external XML stylesheet
	/// </summary>
	public class ReservedStyleInfo
	{
		/// <summary>Factory guid</summary>
		public Guid guid;
		/// <summary />
		public bool created = false;
		/// <summary />
		public ContextValues context;
		/// <summary />
		public StructureValues structure;
		/// <summary />
		public FunctionValues function;
		/// <summary />
		public StyleType styleType;
		/// <summary />
		public string nextStyle;
		/// <summary />
		public string basedOn;

		/// <summary>
		/// General-purpose constructor
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="structure">Structure</param>
		/// <param name="function">Function</param>
		/// <param name="styleType">Paragraph or character</param>
		/// <param name="nextStyle">Name of "Next" style, or null if this is info about a
		/// character style</param>
		/// <param name="basedOn">Name of base style, or null if this is info about a
		/// character style </param>
		/// <param name="guid">The universal identifier for this style </param>
		public ReservedStyleInfo(ContextValues context, StructureValues structure, FunctionValues function, StyleType styleType, string nextStyle, string basedOn, string guid)
		{
			this.guid = new Guid(guid);
			this.context = context;
			this.structure = structure;
			this.function = function;
			this.styleType = styleType;
			this.nextStyle = nextStyle;
			this.basedOn = basedOn;
		}

		/// <summary>
		/// Constructor for character style info
		/// </summary>
		public ReservedStyleInfo(ContextValues context, StructureValues structure, FunctionValues function, string guid)
			: this(context, structure, function, StyleType.kstCharacter, null, null, guid)
		{
		}
	}
}