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
