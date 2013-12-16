// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CheckBase.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Xml;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for CheckBase.
	/// </summary>
	public abstract class CheckBase : Instruction
	{
		protected Message m_message;
		protected bool    m_Result;
		protected string  m_onPass    = null;
		protected string  m_onFail    = null;

		public CheckBase(): base()
		{
			m_message = null;
			m_tag     = "check";
			m_Result  = false;
		}

		public Message Message
		{
			get {return m_message;}
			set {m_message = value;}
		}

		public bool Result
		{
			get {return m_Result;}
			set {m_Result = value;}
		}

		public string OnFail
		{
			get {return m_onFail;}
			set {m_onFail = value;}
		}

		public string OnPass
		{
			get {return m_onPass;}
			set {m_onPass = value;}
		}

		/// <summary>
		/// Creates and parses a message contained in some instructions.
		/// </summary>
		/// <param name="ins"></param>
		/// <param name="body"></param>
		protected void InterpretMessage(XmlNodeList body)
		{
			if (body != null)
			{
				Message message = new Message();

				foreach (XmlNode node in body)
				{
					switch (node.Name)
					{
					case "#text": // a nameless text node
						message.AddText(node.Value);
						break;
					case "data":
						message.AddDataRef(XmlFiler.getAttribute(node, "of"), this);
						break;
					case "beep":
						message.AddSound(new Beep());
						break;
					case "sound":
						string frequency = XmlFiler.getAttribute(node, "frequency");
						string duration = XmlFiler.getAttribute(node, "duration");
						m_log.isTrue(frequency != null && duration != null, makeNameTag() + "Sound instruction must have a frequency and duration.");
						Sound sound = null;
						try { sound = new Sound(Convert.ToUInt32(frequency), Convert.ToUInt32(duration)); }
						catch { m_log.fail(makeNameTag() + "Sound instruction must have a frequency and duration in milliseconds."); }
						message.AddSound(sound);
						break;
					}
				}
				Message = message;
			}
		}

		/// <summary>
		/// Gets the image of this instruction's data.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage (string name)
		{
			if (name == null) name = "result";
			switch (name)
			{
				case "result":	return m_Result.ToString();
				case "on-fail":	return m_onFail;
				case "on-pass":	return m_onPass;
				default:		return base.GetDataImage(name);
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
			if (m_onFail != null) image += @" on-fail="""+m_onFail+@"""";
			if (m_onPass != null) image += @" on-pass="""+m_onPass+@"""";
			return image;
		}

		/// <summary>
		/// Returns attributes showing results of the instruction for the Logger.
		/// </summary>
		/// <returns>Result attributes.</returns>
		public override string resultImage()
		{
			string image = base.resultImage();
			image += @" result="""+m_Result+@"""";
			if (m_message != null) image += @" message="""+m_message.Read()+@"""";
			return image;
		}
	}
}
