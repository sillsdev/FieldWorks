/*
 *    WpDoc.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using System;
using System.Collections;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SIL.FieldWorks.WorldPad
{
	[Serializable]
	[XmlRoot(ElementName="WpDoc")]
	public class WpDoc
	{
		[XmlAnyAttribute]
		public XmlAttribute[] attributes;

		[XmlElement(ElementName="Languages")]
		public Languages languages;
		[XmlElement(ElementName="Styles")]
		public Styles styles;
		[XmlElement(ElementName="Body")]
		public Body body;
		[XmlElement(ElementName="PageSetup")]
		public PageSetup pagesetup;

		public static WpDoc Deserialize(string fileName)
		{
			XmlValidatingReader reader = null;
			WpDoc result = null;
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(WpDoc));
				serializer.UnknownAttribute +=
					new XmlAttributeEventHandler(HandleUnknownAttribute);
				serializer.UnknownElement +=
					new XmlElementEventHandler(HandleUnknownElement);
				serializer.UnknownNode += new XmlNodeEventHandler(HandleUnknownNode);
				XmlTextReader textReader = new XmlTextReader(fileName);
				//textReader.WhitespaceHandling = WhitespaceHandling.None;
				textReader.WhitespaceHandling = WhitespaceHandling.Significant;
				reader = new XmlValidatingReader(textReader);
				reader.ValidationType = ValidationType.DTD;
				reader.ValidationEventHandler +=
					new ValidationEventHandler(HandleValidationError);
				result = (WpDoc)serializer.Deserialize(reader);
			}
			finally
			{
				if (reader != null)
					reader.Close();
			}

			return result;
		}

		private static void HandleValidationError(object sender, ValidationEventArgs e)
		{
			Console.WriteLine(e.Message);
		}

		private static void HandleUnknownNode(object sender, XmlNodeEventArgs e)
		{
			string nodeValue = e.Text;
			object obj = e.ObjectBeingDeserialized;
			Type t = obj.GetType();
			Console.WriteLine("Unknown Node: value: {0}, parent: {1}",
				 nodeValue, t.Name);
		}

		private static void HandleUnknownAttribute(object sender, XmlAttributeEventArgs e)
		{
			XmlAttribute attribute = e.Attr;
			string name = attribute.LocalName;
			string attrValue = attribute.Value;
			object obj = e.ObjectBeingDeserialized;
			Type t = obj.GetType();
			Console.WriteLine("Unknown attribute: {0} (value: {1}, parent: {2})",
				name, attrValue, t.Name);
		}

		private static void HandleUnknownElement(object sender, XmlElementEventArgs e)
		{
			XmlElement element = e.Element;
			string name = element.LocalName;
			object obj = e.ObjectBeingDeserialized;
			Type t = obj.GetType();
			Console.WriteLine("Unknown element: {0} (parent: {1})", name, t.Name);
		}

		public WpDoc()
		{
			//Console.WriteLine("Deserializer called WpDoc constructor");

			StStyle.style = null;
			StStyle.styles.Clear();

			StTxtPara.paragraphs.Clear();

			LgWritingSystem.writingSystems.Clear();
		}

		public ArrayList GetParagraphs()
		{
			//return new ArrayList(StTxtPara.paragraphs);
			return StTxtPara.paragraphs;
		}

		public Hashtable GetStyles()
		{
			//return new ArrayList(StStyle.styles);
			return StStyle.styles;
		}

		public ArrayList GetWritingSystems()
		{
			return LgWritingSystem.writingSystems;
		}
	}

	[Serializable]
	public class Languages
	{
		[XmlElement(ElementName="LgWritingSystem")]
		public LgWritingSystem[] lgWritingSystems;

		public Languages()
		{
			//Console.WriteLine("Deserializer called Languages constructor");
		}
	}

	[Serializable]
	public class LgWritingSystem
	{
		public static ArrayList writingSystems = new ArrayList();

		[XmlAttribute(AttributeName="id")]
		public string id;
		[XmlAnyAttribute]
		public XmlAttribute[] attributes;

		[XmlElement(ElementName="Name24")]
		public Name24 name24;
		[XmlAnyElement]
		public XmlElement[] elements;

		public LgWritingSystem()
		{
			//Console.WriteLine("Deserializer called LgWritingSystem constructor");

			LgWritingSystem.writingSystems.Add(this);
		}
	}

	[Serializable]
	public class Name24
	{
		[XmlElement(ElementName="AUni")]
		public AUni[] aUni;

		public Name24()
		{
			//Console.WriteLine("Deserializer called Name24 constructor");
		}
	}

	[Serializable]
	public class AUni
	{
		[XmlAnyAttribute]
		public XmlAttribute[] attributes;

		[XmlText]
		public string text;

		public AUni()
		{
			//Console.WriteLine("Deserializer called AUni constructor");
		}
	}

	[Serializable]
	public class Styles
	{
		[XmlElement(ElementName="StStyle")]
		public StStyle[] stStyles;

		/*[XmlIgnore]
		private ArrayList stStylesList = new ArrayList();

		public void AddStStyle(string _name, string _value)
		{
			StStyle stStyle = new StStyle();
			stStyle._name = _name;
			stStyle._value = _value;
			stStylesList.Add(stStyle);
		}

		public void StStylesToArray()
		{
			stStyles = (StStyle[])stStylesList.ToArray(typeof(StStyle));
		}*/

		public Styles()
		{
			//Console.WriteLine("Deserializer called Styles constructor");
		}
	}

	[Serializable]
	public class StStyle
	{
		//public static ArrayList styles = new ArrayList();
		public static StStyle style;

		public static Hashtable styles = new Hashtable();

		[XmlElement(ElementName="Name17")]
		public Name17 name17;
		[XmlElement(ElementName="Rules17")]
		public Rules17 rules17;
		[XmlAnyElement]
		public XmlElement[] elements;

		public StStyle()
		{
			//Console.WriteLine("Deserializer called StStyle constructor");

			//styles.Add(this);
			style = this;
		}
	}

	[Serializable]
	public class Name17
	{
		[XmlElement(ElementName="Uni")]
		public Uni uni;

		public Name17()
		{
			//Console.WriteLine("Deserializer called Name17 constructor");
		}
	}

	[Serializable]
	public class Uni
	{
		[XmlText]
		public string text;

		public Uni()
		{
			//Console.WriteLine("Deserializer called Uni constructor");
		}
	}

	[Serializable]
	public class Rules17
	{
		[XmlElement(ElementName="Prop")]
		public Prop prop;
		[XmlAnyElement]
		public XmlElement[] elements;

		public Rules17()
		{
			//Console.WriteLine("Deserializer called Rules17 constructor");

			StStyle parent = StStyle.style;
			StStyle.styles.Add(parent.name17.uni.text, this);
		}
	}

	[Serializable]
	public class Prop
	{
		/*[XmlAttribute(AttributeName="bold")]
		public string bold;
		[XmlAttribute(AttributeName="italic")]
		public string italic;*/
		[XmlAnyAttribute]
		public XmlAttribute[] attributes;

		[XmlAnyElement]
		public XmlElement[] elements;

		public Prop()
		{
			//Console.WriteLine("Deserializer called Prop constructor");
		}
	}

	[Serializable]
	public class Body
	{
		[XmlAttribute]
		public string docRightToLeft;

		[XmlElement(ElementName="StTxtPara")]
		public StTxtPara[] stTxtParas;
		[XmlAnyElement]
		public XmlElement[] elements;

		public Body()
		{
			//Console.WriteLine("Deserializer called Body constructor");
		}
	}

	[Serializable]
	public class StTxtPara
	{
		public static ArrayList paragraphs = new ArrayList();

		[XmlIgnore]
		public ArrayList runs;

		[XmlElement(ElementName="StyleRules15")]
		public StyleRules15 styleRules15;
		[XmlElement(ElementName="Contents16")]
		public Contents16 contents16;
		[XmlAnyElement]
		public XmlElement[] elements;

		public StTxtPara()
		{
			//Console.WriteLine("Deserializer called StTxtPara constructor");

			paragraphs.Add(this);
			runs = new ArrayList();
		}

		public StProp GetParagraphStyle()
		{
			return styleRules15.stProp;
		}

		/*public string GetParagraphStyle()
		{
			string style = null;

			if (styleRules15.stProp.attributes != null)
			{
				foreach (XmlAttribute attribute in styleRules15.stProp.attributes)
				{
					if (attribute.LocalName == "namedStyle")
						style = attribute.Value;
				}
			}

			return style;
		}*/

		public ArrayList GetRuns()
		{
			//return new ArrayList(runs);
			return runs;
		}
	}

	[Serializable]
	public class StyleRules15
	{
		[XmlElement(ElementName="Prop")]
		public StProp stProp;

		public StyleRules15()
		{
			//Console.WriteLine("Deserializer called StyleRules15 constructor");
		}
	}

	[Serializable]
	public class StProp
	{
		[XmlAnyAttribute]
		public XmlAttribute[] attributes;

		[XmlAnyElement]
		public XmlElement[] elements;

		public StProp()
		{
			//Console.WriteLine("Deserializer called StProp constructor");
		}
	}

	[Serializable]
	public class Contents16
	{
		[XmlElement(ElementName="Str")]
		public Str str;

		public Contents16()
		{
			//Console.WriteLine("Deserializer called Contents16 constructor");
		}
	}

	[Serializable]
	public class Str
	{
		[XmlElement(ElementName="Run")]
		public Run[] runs;

		public Str()
		{
			//Console.WriteLine("Deserializer called Str constructor");
		}
	}

	[Serializable]
	public class Run
	{
		/*[XmlAttribute(AttributeName="bold")]
		public string bold;
		[XmlAttribute(AttributeName="fontsize")]
		public string fontsize;
		[XmlAttribute(AttributeName="forecolor")]
		public string forecolor;*/
		[XmlAnyAttribute]
		public XmlAttribute[] attributes;

		[XmlText]
		public string text;

		public Run()
		{
			//Console.WriteLine("Deserializer called Run constructor");

			StTxtPara parent =
				(StTxtPara)StTxtPara.paragraphs[StTxtPara.paragraphs.Count - 1];
			parent.runs.Add(this);
		}

		public Run(string _text)
		{
			text = _text;
		}
	}

	[Serializable]
	public class PageSetup
	{
		[XmlAnyElement]
		public XmlElement[] elements;
	}
}
