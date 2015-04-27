// ---------------------------------------------------------------------------------------------
// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
		[XmlAttribute("overview")]
		public bool IsOverview { get; set; }

		[XmlAttribute("type")]
		public string Type { get; set; }

		[XmlArray(Form = XmlSchemaForm.Unqualified), XmlArrayItem("Question", typeof(Question), IsNullable = false)]
		public Question[] Questions { get; set; }
	}
	#endregion
}
