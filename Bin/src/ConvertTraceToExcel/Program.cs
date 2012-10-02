using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

namespace ConvertTraceToExcel
{
	class Program
	{
		static void Main(string[] args)
		{
			string fileName = args[0];
			XmlDocument doc = new XmlDocument();
			string outputFile = Path.ChangeExtension(fileName, ".txt");
			TextWriter writer = new StreamWriter(outputFile);
			writer.WriteLine("Error\tReads\tRowCounts\tQuery\tDuration\tWrites\t");
			doc.Load(fileName);
			foreach(XmlNode node in doc.GetElementsByTagName("Event"))
			{
				if (node.Attributes["name"] == null || node.Attributes["name"].Value != "SQL:BatchCompleted")
					continue;
				foreach (XmlNode child in node.ChildNodes)
				{
					if (child.Name != "Column")
						continue;
					switch (child.Attributes["name"].Value)
					{
						case "Duration":
						case "Reads":
						case "RowCounts":
						case "Error":
						case "TextData":
						case "Writes":
							writer.Write(child.InnerText);
							writer.Write('\t');
							break;
					}
				}
				writer.WriteLine();
			}
			writer.Close();
		}
	}
}
