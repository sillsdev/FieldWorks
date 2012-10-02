using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

class Rc2glade
{
	GladeInterface layout;
	Match m_match;

	static void Main(string[] args)
	{
		Rc2glade r = new Rc2glade(args);
	}

	public Rc2glade(string[] args)
	{
		double hScale = args.Length < 1 ? 2.0 : Convert.ToDouble(args[0]);
		double vScale = args.Length < 2 ? 2.0 : Convert.ToDouble(args[1]);

		layout = new GladeInterface(hScale, vScale);

		DirectoryInfo dir = new DirectoryInfo("DialogResourceFiles");
		if (dir != null)
		{
			//Console.WriteLine("Directory Name: {0}", dir.Name);
			FileInfo[] filesInDir = dir.GetFiles();
			foreach (FileInfo file in filesInDir)
			{
				Console.WriteLine("** File Name: {0} **", file.Name);
				ProcessFile(file);
			}
		}

		layout.DialogsToArray();

		XmlSerializer serializer = new XmlSerializer(typeof(GladeInterface));

		serializer.UnknownAttribute +=
			new XmlAttributeEventHandler(HandleUnknownAttribute);
		serializer.UnknownElement +=
			new XmlElementEventHandler(HandleUnknownElement);
		serializer.UnknownNode += new XmlNodeEventHandler(HandleUnknownNode);

		using (FileStream oStream = File.OpenWrite("GladeInterface.glade"))
		{
			serializer.Serialize(oStream, layout);
		}
	}

	public void ProcessFile(FileInfo iFile)
	{
		//FileInfo iFile = new FileInfo(args[0]);
		StreamReader iStream = iFile.OpenText();
		string text = iStream.ReadLine();

		while (text != null)
		{
//			more code goes here
			if (ComplexControlFound(text, ref m_match))
			{
				//Console.WriteLine("A complex control has been processed");
				layout.AddWidget(m_match);
			}
			else if (LabelledSimpleControlFound(text, ref m_match))
			{
				//Console.WriteLine("A labelled simple control has been processed");
				layout.AddWidget(m_match);
			}
			else if (UnlabelledSimpleControlFound(text, ref m_match))
			{
				//Console.WriteLine("An unlabelled simple control has been processed");
				layout.AddWidget(m_match);
			}
			else if (DialogFound(text, ref m_match))
			{
				layout.AddWidget(m_match);
			}
			else if (DialogTitleFound(text, ref m_match))
			{
				//Console.WriteLine("**** Dialog title has been found ****");
				layout.SetDialogTitle(m_match.Groups["title"].ToString());
			}
			else
			{
				Console.WriteLine("**** Ignored line: {0} ****", text);
			}
			text = iStream.ReadLine();

		}

		iStream.Close();

		layout.CloseContainer();
	}

	private bool ComplexControlFound(string text, ref Match m_match)
	{
		// find a complex control (e.g. CONTROL ... "Button")
		Regex theReg = new Regex(@"^(\s{4}|\t)CONTROL" +
			@"\s+(""(?<label>(.*))""|(?<label>(.*)))" +
			@",\s*(?<name>(.+))" +
			@",\s*""(?<type>(.*))""" +
			@",\s*(?<options>(.+))" +
			@",\s*(?<left>(\d+))" +
			@",\s*(?<top>(\d+))" +
			@",\s*(?<width>(\d+))" +
			@",\s*(?<height>(\d+))" +
			@"(,\s*(?<more_options>(.+)))?"
			);

		// get the collection of matches
		MatchCollection theMatches = theReg.Matches(text);

		if (theMatches.Count == 0)
			return false;

		// iterate through the collection
		foreach (Match theMatch in theMatches)
		{
			/*Console.WriteLine("\ntheMatch.Length: {0}", theMatch.Length);*/

			if (theMatch.Length != 0)
			{
				Console.WriteLine("theMatch: {0}", theMatch.ToString());
				m_match = theMatch;
				/*Console.WriteLine("label: {0}", theMatch.Groups["label"]);
				Console.WriteLine("name: {0}", theMatch.Groups["name"]);
				Console.WriteLine("type: {0}", theMatch.Groups["type"]);
				Console.WriteLine("options: {0}", theMatch.Groups["options"]);
				Console.WriteLine("left: {0}", theMatch.Groups["left"]);
				Console.WriteLine("top: {0}", theMatch.Groups["top"]);
				Console.WriteLine("width: {0}", theMatch.Groups["width"]);
				Console.WriteLine("height: {0}", theMatch.Groups["height"]);
				Console.WriteLine("more_options: {0}", theMatch.Groups["more_options"]);*/
			}
		}

		return true;
	}

	private bool LabelledSimpleControlFound(string text, ref Match m_match)
	{
		// find a labelled simple control (e.g. LTEXT)
		Regex theReg = new Regex(@"^(\s{4}|\t)(?<type>(\S+))" +
			//@"\s+""(?<label>(.*))""" +
			@"\s+(""(?<label>(.*))""|(?<label>(.*)))" +
			@",\s*(?<name>(.+))" +
			@",\s*(?<left>(\d+))" +
			@",\s*(?<top>(\d+))" +
			@",\s*(?<width>(\d+))" +
			@",\s*(?<height>(\d+))" +
			@"(,\s*(?<options>(.+)))?"
			);

		// get the collection of matches
		MatchCollection theMatches = theReg.Matches(text);

		if (theMatches.Count == 0)
			return false;

		// iterate through the collection
		foreach (Match theMatch in theMatches)
		{
			/*Console.WriteLine("\ntheMatch.Length: {0}", theMatch.Length);*/

			if (theMatch.Length != 0)
			{
				Console.WriteLine("theMatch: {0}", theMatch.ToString());
				m_match = theMatch;
				/*Console.WriteLine("type: {0}", theMatch.Groups["type"]);
				Console.WriteLine("label: {0}", theMatch.Groups["label"]);
				Console.WriteLine("name: {0}", theMatch.Groups["name"]);
				Console.WriteLine("left: {0}", theMatch.Groups["left"]);
				Console.WriteLine("top: {0}", theMatch.Groups["top"]);
				Console.WriteLine("width: {0}", theMatch.Groups["width"]);
				Console.WriteLine("height: {0}", theMatch.Groups["height"]);*/
			}
		}

		return true;
	}

	private bool UnlabelledSimpleControlFound(string text, ref Match m_match)
	{
		// find an unlabelled simple control (e.g. EDITTEXT)
		Regex theReg = new Regex(@"^(\s{4}|\t)(?<type>(\S+))" +
			@"\s+(?<name>(.+))" +
			@",\s*(?<left>(\d+))" +
			@",\s*(?<top>(\d+))" +
			@",\s*(?<width>(\d+))" +
			@",\s*(?<height>(\d+))" +
			@"(,\s*(?<options>(.+)))?"
			);

		// get the collection of matches
		MatchCollection theMatches = theReg.Matches(text);

		if (theMatches.Count == 0)
			return false;

		// iterate through the collection
		foreach (Match theMatch in theMatches)
		{
			/*Console.WriteLine("\ntheMatch.Length: {0}", theMatch.Length);*/

			if (theMatch.Length != 0)
			{
				Console.WriteLine("theMatch: {0}", theMatch.ToString());
				m_match = theMatch;
				/*Console.WriteLine("type: {0}", theMatch.Groups["type"]);
				Console.WriteLine("name: {0}", theMatch.Groups["name"]);
				Console.WriteLine("left: {0}", theMatch.Groups["left"]);
				Console.WriteLine("top: {0}", theMatch.Groups["top"]);
				Console.WriteLine("width: {0}", theMatch.Groups["width"]);
				Console.WriteLine("height: {0}", theMatch.Groups["height"]);
				Console.WriteLine("options: {0}", theMatch.Groups["options"]);*/
			}
		}

		return true;
	}

	private bool DialogFound(string text, ref Match m_match)
	{
		// find a form (e.g. DIALOG)
		Regex theReg = new Regex(@"^(?<name>(\S+))" +
			@"\s+(?<type>(DIALOG\s+\S+|DIALOGEX))" +
			@"\s+(?<left>(\d+))" +
			@",\s*(?<top>(\d+))" +
			@",\s*(?<width>(\d+))" +
			@",\s*(?<height>(\d+))"
			);

		// get the collection of matches
		MatchCollection theMatches = theReg.Matches(text);

		if (theMatches.Count == 0)
			return false;

//		string name = "";

		// iterate through the collection
		foreach (Match theMatch in theMatches)
		{
			/*Console.WriteLine("\ntheMatch.Length: {0}", theMatch.Length);*/

			if (theMatch.Length != 0)
			{
				Console.WriteLine("theMatch: {0}", theMatch.ToString());
				m_match = theMatch;
				/*Console.WriteLine("name: {0}", theMatch.Groups["name"]);
//				name = theMatch.Groups["name"].ToString();
				Console.WriteLine("left: {0}", theMatch.Groups["left"]);
				Console.WriteLine("top: {0}", theMatch.Groups["top"]);
				Console.WriteLine("width: {0}", theMatch.Groups["width"]);
				Console.WriteLine("height: {0}", theMatch.Groups["height"]);*/
			}
		}

		return true;
	}

	private bool DialogTitleFound(string text, ref Match m_match)
	{
		// find a dialog's title
		Regex theReg = new Regex(@"^CAPTION" +
			@"\s+""(?<title>(.*))""$"
			);

		// get the collection of matches
		MatchCollection theMatches = theReg.Matches(text);

		if (theMatches.Count == 0)
			return false;

		// iterate through the collection
		foreach (Match theMatch in theMatches)
		{
			/*Console.WriteLine("\ntheMatch.Length: {0}", theMatch.Length);*/

			if (theMatch.Length != 0)
			{
				Console.WriteLine("theMatch: {0}", theMatch.ToString());
				m_match = theMatch;
				/*Console.WriteLine("title: {0}", theMatch.Groups["title"]);*/
			}
		}

		return true;
	}

	private static void HandleUnknownNode(object sender, XmlNodeEventArgs e)
	{
		string value = e.Text;
		Property property = (Property)e.ObjectBeingDeserialized;
		Console.WriteLine("Unknown Node: " + e.LineNumber + ", " + value + ", "
			+ property._name);
	}

	private static void HandleUnknownAttribute(object sender, XmlAttributeEventArgs e)
	{
		XmlAttribute attribute = e.Attr;
		string name = attribute.LocalName;
		string value = attribute.Value;
		object obj = e.ObjectBeingDeserialized;
		Type t = obj.GetType();
		Console.WriteLine("Unknown Attribute: " + e.LineNumber + ", " + name + ", "
			+ value + ", " + t.Name);
	}

	private static void HandleUnknownElement(object sender, XmlElementEventArgs e)
	{
		Console.WriteLine("Unknown Element");
	}
}
