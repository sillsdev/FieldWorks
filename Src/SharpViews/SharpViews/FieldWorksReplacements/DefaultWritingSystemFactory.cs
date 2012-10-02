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
	class DefaultWritingSystemFactory : ILgWritingSystemFactory
	{
		public DefaultWritingSystemFactory()
		{
			m_engine.WritingSystemFactory = this;
		}
		private IRenderEngine m_engine = UniscribeEngineClass.Create();
		private ILgWritingSystem m_writingSystem = new DefaultWritingSystem();
		public ILgWritingSystem get_Engine(string bstrIcuLocale)
		{
			return m_writingSystem;
		}

		public ILgWritingSystem get_EngineOrNull(int ws)
		{
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

		public IRenderEngine get_RendererFromChrp(ref LgCharRenderProps _chrp)
		{
			throw new NotImplementedException();
		}

		public int UserWs
		{
			get { return 1; }
			set { throw new NotImplementedException(); }
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

		public bool RightToLeftScript
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

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
