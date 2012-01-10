// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2011' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: QuestionSections.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SILUBS.PhraseTranslationHelper
{
	#region class QuestionSections
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	[XmlRoot("ComprehensionCheckingQuestions", Namespace = "", IsNullable = false)]
	public class QuestionSections
	{
		[XmlElement("Section", typeof(Section), Form = XmlSchemaForm.Unqualified)]
		public Section[] Items { get; set; }
	}

	#endregion

	#region class Section
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	public class Section
	{
		[XmlAttribute("heading")]
		public string Heading { get; set; }

		[XmlAttribute("scrref")]
		public string ScriptureReference { get; set; }

		[XmlAttribute("startref")]
		public int StartRef { get; set; }

		[XmlAttribute("endref")]
		public int EndRef { get; set; }

		[XmlArray(Form = XmlSchemaForm.Unqualified), XmlArrayItem("Category", typeof(Category), IsNullable = false)]
		public Category[] Categories { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	public class Category
	{
		[XmlAttribute("type")]
		public string Type { get; set; }

		[XmlArray(Form = XmlSchemaForm.Unqualified), XmlArrayItem("Question", typeof(Question), IsNullable = false)]
		public Question[] Questions { get; set; }
	}
	#endregion
}
