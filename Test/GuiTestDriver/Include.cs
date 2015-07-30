// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;

namespace GuiTestDriver
{
	class Include : Instruction
	{
		private string m_pathName = null;

		/// <summary>
		/// Constructs an include instruction that inserts script instructions
		/// into a script.
		/// </summary>
		public Include() : base()
		{
			m_tag = "include";
		}

		/// <summary>
		/// Called to finish construction when an instruction has been instantiated by
		/// a factory and had its properties set.
		/// This can check the integrity of the instruction or perform other initialization tasks.
		/// </summary>
		/// <param name="xn">XML node describing the instruction</param>
		/// <param name="con">Parent xml node instruction</param>
		/// <returns></returns>
		public override bool finishCreation(XmlNode xn, Context con)
		{  // finish factory construction
			Logger.getOnly().isNotNull(m_pathName, @"include must have a 'from' path.");
			Logger.getOnly().isTrue(m_pathName != "", @"include must have a 'from' path.");

			string pathname = TestState.getOnly().getScriptPath() + @"\" + m_pathName;
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true; // allows <insert> </insert>
			try { doc.Load(pathname); }
			catch (System.IO.FileNotFoundException ioe)
			{
				Logger.getOnly().fail(@"include '" + pathname + "' not found: " + ioe.Message);
			}
			catch (System.Xml.XmlException xme)
			{
				Logger.getOnly().fail(@"include '" + pathname + "' not loadable: " + xme.Message);
			}
			XmlNode include = doc["include"];
			Logger.getOnly().isNotNull(include, "Missing document element 'include'.");
			XmlElement conEl = con.Element;
			// clone insert and add it before so there's an insert before and after
			// after adding elements, delete the "after" one
			XmlDocumentFragment df = xn.OwnerDocument.CreateDocumentFragment();
			df.InnerXml = xn.OuterXml;
			conEl.InsertBefore(df, xn);
			foreach (XmlNode xnode in include.ChildNodes)
			{
				string image = xnode.OuterXml;
				if (image.StartsWith("<"))
				{
					XmlDocumentFragment dfrag = xn.OwnerDocument.CreateDocumentFragment();
					dfrag.InnerXml = xnode.OuterXml;
					conEl.InsertBefore(dfrag, xn);
				}
			}
			conEl.RemoveChild(xn);
			//Logger.getOnly().paragraph(Utilities.attrText(textImage));
			return true;
		}

		/// <summary>
		/// Gets and sets the name of the script instruction file to include.
		/// </summary>
		public string From
		{
			get { return m_pathName; }
			set { m_pathName = value; }
		}

		/// <summary>
		/// Executes the include instruction by reading the @from file and
		/// inserting its instructions into the current script.
		/// </summary>
		public override void Execute()
		{
			base.Execute();
			Finished = true; // tell do-once it's done
		}

		/// <summary>
		/// Gets the image of this instruction's data.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage(string name)
		{
			if (name == null) name = "from";
			switch (name)
			{
			case "from": return m_pathName;
			default: return base.GetDataImage(name);
			}
		}

		/// <summary>
		/// Echos an image of the instruction with its attributes
		/// and possibly more for diagnostic purposes.
		/// Over-riding methods should pre-pend this base result to their own.
		/// </summary>
		/// <returns>An image of this instruction.</returns>
		public override string image()
		{
			string image = base.image();
			if (m_pathName != null) image += @" from=""" + Utilities.attrText(m_pathName) + @"""";
			return image;
		}

	}
}
