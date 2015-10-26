// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.SharpViews.FieldWorksReplacements
{
	/// <summary>
	/// Default very simple implementation of this interface, provides only the methods needed
	/// by SharpViewsLight and UniscribeEngine.
	/// </summary>
	public class DefaultWritingSystemFactory : ILgWritingSystemFactory
	{
		internal DefaultWritingSystemFactory()
		{
			m_engine.WritingSystemFactory = this;
			((DefaultWritingSystem)m_rtlWritingSystem).RightToLeftScript = true;
		}
		private IRenderEngine m_engine = UniscribeEngineClass.Create();
		private ILgWritingSystem m_writingSystem = new DefaultWritingSystem();
		private ILgWritingSystem m_rtlWritingSystem = new DefaultWritingSystem();
		public ILgWritingSystem get_Engine(string bstrIcuLocale)
		{
			return m_writingSystem;
		}

		// Currently this is mainly for testing. It allows us to specify which writing systems
		// are RTL.
		internal HashSet<int> RtlWritingSystems = new HashSet<int>();

		public ILgWritingSystem get_EngineOrNull(int ws)
		{
			if (RtlWritingSystems.Contains(ws))
				return m_rtlWritingSystem;
			return m_writingSystem;
		}

		public int GetWsFromStr(string bstr)
		{
			throw new NotImplementedException();
		}

		public string GetStrFromWs(int wsId)
		{
			throw new NotImplementedException();
		}

		public int NumberOfWs
		{
			get { throw new NotImplementedException(); }
		}

		public void GetWritingSystems(ArrayPtr rgws, int cws)
		{
			throw new NotImplementedException();
		}

		private ILgCharacterPropertyEngine m_cpe = LgIcuCharPropEngineClass.Create();
		public ILgCharacterPropertyEngine get_CharPropEngine(int ws)
		{
			return m_cpe;
		}

		public IRenderEngine get_Renderer(int ws, IVwGraphics _vg)
		{
			return m_engine;
		}

		public IRenderEngine get_RendererFromChrp(IVwGraphics vg, ref LgCharRenderProps _chrp)
		{
			throw new NotImplementedException();
		}

		public int UserWs
		{
			get { return 1; }
			set { throw new NotImplementedException(); }
		}

		public IRenderEngine get_RendererFromChrp(ref LgCharRenderProps _chrp)
		{
			throw new NotImplementedException();
		}
	}

	class DefaultWritingSystem : ILgWritingSystem
	{
		public string Id
		{
			get { throw new NotImplementedException(); }
		}

		public int Handle
		{
			get { throw new NotImplementedException(); }
		}

		public string LanguageName
		{
			get { throw new NotImplementedException(); }
		}

		public string ISO3
		{
			get { throw new NotImplementedException(); }
		}

		public int LCID
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public string SpellCheckingId
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public bool RightToLeftScript { get; set; }


		public IRenderEngine get_Renderer(IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}

		public string DefaultFontFeatures
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public string DefaultFontName
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public ILgCharacterPropertyEngine CharPropEngine
		{
			get { throw new NotImplementedException(); }
		}

		public void InterpretChrp(ref LgCharRenderProps _chrp)
		{
		}

		public string Keyboard
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public int CurrentLCID
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}
	}
}
