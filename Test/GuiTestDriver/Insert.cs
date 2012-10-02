using System;
using System.Threading;
using System.Xml;
using System.ComponentModel;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Insert.
	/// </summary>
	public class Insert : ActionBase
	{
		string m_text = null;
		int    m_pause = 0;

		public Insert()
		{
			m_text = null;
			m_tag  = "insert";
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
			foreach (XmlNode node in xn.ChildNodes)
			{
				switch (node.Name)
				{
				case "#text":
				case "#whitespace":
				case "#significant-whitespace": // a nameless text node
					Text = node.Value;
					break;
				default:
					m_log.fail(makeNameTag() + "Insert instruction must have something to insert.");
					break;
				}
			}
			m_log.isNotNull(Text, makeNameTag() + "Insert instruction must have some content.");
			m_log.isTrue(Text != "", makeNameTag() + "Insert instruction must have non-empty content.");
			return true;
		}

		public string Text
		{
			get { return m_text; }
			set { m_text = value; }
		}

		public int Pause
		{
			get { return m_pause; }
			set { m_pause = value; }
		}


		/// <summary>
		/// Sends SendKey codes and characters from a string.
		/// The codes have the pattern {+|^|%}*{.|/{*/}}
		/// The characters +^%[]{}() are escaped in {}'s.
		/// </summary>
		public override void Execute()
		{
			m_log.isNotNull(Application, makeNameTag() + "No application context for sending keystrokes");
			base.Execute();
			// Is the cursor at an editable text box or in an editable view field??
			Context con = (Context)Ancestor(typeof(Context));
			m_log.isNotNull(con, makeNameTag() + "Insert must occur in some context");
			if (m_text != null)
			{
				bool sendText = true;
				try { Application.Process.WaitForInputIdle(); }
				catch (Win32Exception e)
				{
					m_log.paragraph(makeNameTag() + " WaitForInputIdle: " + e.Message);
					sendText = false;
				}

				if (sendText)
				{
					m_text = Utilities.evalExpr(m_text);
					string slate = "";
					bool inBracket = false;
					foreach (char ch in m_text)
					{
						if ((ch.Equals('+') || ch.Equals('^') ||
							  ch.Equals('%') || ch.Equals('{')))
						{
							if (slate == "") slate = new String(ch, 1);
							else slate += ch;
							if (ch.Equals('{')) inBracket = true;
						}
						else if (slate.Length >= 1 && !inBracket)
						{ // previous ch's were + ^ and/or %
							// ch is not a special character
							// send the n character code
							if (m_pause > 0) Thread.Sleep(m_pause);
							if (ch.Equals('(') || ch.Equals(')') ||
								ch.Equals('[') || ch.Equals(']'))
							{ // Escape these meaningless brackets!
								m_log.paragraph("n character code: " + slate + "{" + ch + "}");
								Application.SendKeys(slate + "{" + ch + "}");
							}
							else
							{
								m_log.paragraph("n character code: " + slate + ch);
								Application.SendKeys(slate + ch);
							}
							slate = "";
						}
						else if (inBracket)
						{ // would be meaningless but legal to put () or [] in {}'s
							slate += ch;
							if (ch.Equals('}') && slate != "{}")
							{ // end of bracket code
								inBracket = false;
								if (m_pause > 0) Thread.Sleep(m_pause);
								m_log.paragraph("End of SendKeys code: " + slate);
								Application.SendKeys(slate);
								slate = "";
							}
						}
						else // slate == ""
						{ // send the individual character - it's not in a code
							if (m_pause > 0) Thread.Sleep(m_pause);
							if (ch.Equals('(') || ch.Equals(')') ||
								ch.Equals('[') || ch.Equals(']'))
							{ // Escape these meaningless brackets!
								m_log.paragraph("Character: {" + ch + "}");
								Application.SendKeys("{" + ch + "}");
							}
							else
							{
								m_log.paragraph("Character: " + ch);
								Application.SendKeys(new String(ch, 1));
							}
						}
					}
					if (slate != "") m_log.paragraph("Slate leftovers: " + slate);

					/* old code before fwr
					if (m_pause == 0) Application.SendKeys(m_text);
					else
					{ // send each character with the specified pause between them
						foreach (char ch in m_text)
						{
							Thread.Sleep(m_pause);
							Application.SendKeys(ch.ToString());
						}
					}
					*/
				} // if sendText
			} // m_text not null
			Finished = true; // tell do-once it's done
		}

		/// <summary>
		/// Gets the image of this instruction's data.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage (string name)
		{
			if (name == null) name = "text";
			switch (name)
			{
				case "text":
				{
					if (m_text != null) return m_text;
					return "(nothing to insert)";
				}
			case "pause": return m_pause.ToString();
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
			if (m_text != null) image += @" content=""" + Utilities.attrText(m_text) + @"""";
			if (m_pause != 0) image += @" pause=""" + m_pause.ToString() + @"""";
			return image;
		}
	}
}
