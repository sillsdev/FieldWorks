// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Message.cs
// Responsibility: Testing
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// This is a container for mixed text, data references and sounds.
	/// </summary>
	public class Message
	{
		ArrayList m_Body = null;
		/// <summary>
		/// Creates a Message. AddText and AddDataRef to populate it.
		/// </summary>
		public Message()
		{
			m_Body = new ArrayList();
			Assert.IsNotNull(m_Body,"Message class failed to allocate an array to contain it.");
		}
		/// <summary>
		/// If there is something in the message to read, return true.
		/// </summary>
		/// <returns>true if there is something to read</returns>
		public bool HasContent() {return m_Body.Count > 0;}
		/// <summary>
		/// Adds text to a message.
		/// </summary>
		/// <param name="text">The text to add.</param>
		public void AddText(string text)
		{
			int index = m_Body.Add(text);
		}
		/// <summary>
		/// Adds a DataRef to the message using the parent if the id is null.
		/// </summary>
		/// <param name="id">Instruction name or id, can be null</param>
		/// <param name="name">Name of the data item, can be null</param>
		/// <param name="parent">Parent instruction of this DataRef</param>
		public void AddDataRef(string id, Instruction parent)
		{
			DataRef dref = new DataRef(id, parent);
			int index = m_Body.Add(dref);
		}
		/// <summary>
		/// Adds a sound or beep instruction to the message.
		/// It is played when the sound is read.
		/// </summary>
		/// <param name="ins">The sound or beep instruction.</param>
		public void AddSound(Instruction ins)
		{
			int index = m_Body.Add(ins);
		}
		/// <summary>
		/// Return a string image of the message content.
		/// </summary>
		/// <returns>A string image of the message content</returns>
		public string Read()
		{
			string text = null;
			IEnumerator segment = m_Body.GetEnumerator();
			while (segment.MoveNext())
			{
				if (segment.Current.GetType() == typeof(Beep)
					|| segment.Current.GetType() == typeof(Sound))
					((Instruction)segment.Current).Execute();
				else
					text += segment.Current.ToString();
			}
			return text;
		}
	}
	/// <summary>
	/// A reference to test script data stored in the TestState.
	/// It should only be used with messages.
	/// </summary>
	public class DataRef
	{
		static int sm_next_id = 1;
		TestState m_ts = null;
		string m_id = null;

		/// <summary>
		/// Constructor that uses a parent instruction instead if the id is null.
		/// </summary>
		/// <param name="id">Instruction name or id, can be null</param>
		/// <param name="parent">Parent instruction of this DataRef</param>
		public DataRef(string id, Instruction parent)
		{
			m_ts = TestState.getOnly();
			//Assert.IsNotNull(id,"Null id passed for constructing a DataRef");
			//Assert.IsFalse(id == "","Empty id passed for constructing a DataRef");
			Assert.IsNotNull(m_ts,"Null TestState passed for constructing a DataRef");
			Assert.IsNotNull(parent,"Non-null parent instruction expected for constructing a DataRef");
			// the id can be $id.data or $id or $.data or $ (parent default data)
			// these may occur with suffix ';' or space
			// replace all '$.' with '$genid.data'
			if (id == null || id.Equals("")) id = "$";
			m_id = id;
			int dol = m_id.IndexOf('$');
			if (dol >= 0)
			{ // there are data references
				string genId = "Internal#Data#Ref"+sm_next_id.ToString();
				string dgenId = "$"+genId;
				sm_next_id += 1;
				m_id = m_id.Replace("$.",dgenId+".");
				m_id = m_id.Replace("$;",dgenId+";");
				m_id = m_id.Replace("$ ",dgenId+" ");
				if (m_id == "$") m_id = dgenId;
				if (m_id[m_id.Length-1] == '$') // last char
					m_id = m_id.Substring(0,m_id.Length-1) + dgenId;
				// if dgenId was substituted, add the parent to the ts list
				if (-1 < m_id.IndexOf(dgenId))
					m_ts.AddNamedInstruction(genId,parent);
			}
			else Assert.IsNotNull(id,"A DataRef has no ref prefixed with '$'. Read: "+id);
		}
		/// <summary>
		/// Returns a string containing the value of the data referenced.
		/// </summary>
		/// <returns>A string dereferencing the data item.</returns>
		override public string ToString()
		{
			return Utilities.evalExpr(m_id);
		}
	}
}
