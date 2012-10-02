using System;
using System.Threading;

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

		public override void Execute()
		{
			base.Execute();
			// Is the cursor at an editable text box or in an editable view field??
			Context con = (Context)Ancestor(typeof(Context));
			isNotNull(con,"Insert must occur in some context");
			if (m_text != null)
			{
				m_text = Utilities.evalExpr(m_text);
				if (m_pause == 0) Application.SendKeys(m_text);
				else
				{ // send each character with the specified pause between them
					foreach (char ch in m_text)
					{
						Thread.Sleep(m_pause);
						Application.SendKeys(ch.ToString());
					}
				}
			}
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
