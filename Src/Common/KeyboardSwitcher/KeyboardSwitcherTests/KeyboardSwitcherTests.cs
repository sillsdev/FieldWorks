#if __MonoCS__
using System;
using IBusDotNet;
using NUnit.Framework;
using SIL.FieldWorks.Views;
using SIL.Utils;

namespace KeyboardSwitcherTests
{
	[TestFixture]
	public class KeyboardSwitcherTests
	{
		[Test]
		public void GetEngineDesc_Version1Engine_ValidIBusEngineDesc()
		{
			using (var switcher = new KeyboardSwitcher())
			{
				var engineDescResult = new IBusEngineDesc
													{ longname = "longname",
														name = "name",
														description = "desc",
														language = "lang",
														license = "lic",
														author = "author",
														icon = "icon",
														layout = "layout" };

				var engineDesc = new IBusEngineDesc_v1
													{ longname = "longname",
														name = "name",
														description = "desc",
														language = "lang",
														license = "lic",
														author = "author",
														icon = "icon",
														layout = "layout" };

				var engine = new DummyIConvertible(engineDesc);
				Assert.AreEqual(engineDescResult, switcher.GetEngineDesc(engine));
			}
		}

		[Test]
		public void GetEngineDesc_Version2Engine_ValidIBusEngineDesc()
		{
			using (var switcher = new KeyboardSwitcher())
			{
				var engineDescResult = new IBusEngineDesc
													{ longname = "longname",
														name = "name",
														description = "desc",
														language = "lang",
														license = "lic",
														author = "author",
														icon = "icon",
														layout = "layout" };

				var engine = new DummyIConvertible(engineDescResult);
				Assert.AreEqual(engineDescResult, switcher.GetEngineDesc(engine));
			}
		}

		[Test]
		[ExpectedException(typeof(InvalidCastException))]
		public void GetEngineDesc_InvalidEngine_InvalidCastException()
		{
			using (var switcher = new KeyboardSwitcher())
			{
				var engine = new DummyIConvertible(new object());
				switcher.GetEngineDesc(engine);
			}
		}

		/// <summary>FWNX-442: Keyboard not turning off when it should</summary>
		[Test]
		public void IMEKeyboard_SetNoKeyboard_DisablesKeyboard()
		{
			using (var switcher = new KeyboardSwitcher())
			{
				// Create an input context
				using (var controller = new DummyInputBusController())
				{
				controller.Focus();
				var context = GlobalCachedInputContext.InputContext;

				// Set input method using the first available keyboard
				if (switcher.IMEKeyboardsCount <= 0)
				{
					Console.WriteLine("Warning: Test IMEKeyboard_SetNoKeyboard_DisablesKeyboard unable to run since no iBus keyboards available or no iBus running.");
					return; // Can't test without an iBus engine to use.
				}
				switcher.IMEKeyboard = switcher.GetKeyboardName(0);
				Assert.That(context.IsEnabled(), Is.True, "Keyboard input method should be enabled when set");

				// Turn off input method
				switcher.IMEKeyboard = null;
				Assert.That(context.IsEnabled(), Is.False, "Should have disabled keyboard input method");
			}
		}
	}
	}

	class DummyInputBusController: IDisposable
	{
		protected IBusConnection m_connection;
		protected InputContext m_inputContext;
		protected IBusDotNet.InputBusWrapper m_ibus;

		/// <summary/>
		public DummyInputBusController()
		{
			m_connection = IBusConnectionFactory.Create();

			if (m_connection == null)
				return;

			m_ibus = new IBusDotNet.InputBusWrapper(m_connection);
			m_inputContext = m_ibus.InputBus.CreateInputContext("UnitTest");
			m_inputContext.SetCapabilities(Capabilities.Focus | Capabilities.PreeditText);
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~DummyInputBusController()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_connection != null)
					m_connection.Dispose();
			}
			m_connection = null;
			IsDisposed = true;
		}
		#endregion

		/// <summary>Focus the input context</summary>
		public void Focus()
		{
			if (m_connection == null)
				return;

			if (m_inputContext != null)
				m_inputContext.FocusIn();

			SIL.FieldWorks.Views.GlobalCachedInputContext.InputContext = m_inputContext;
		}
	}

	class DummyIConvertible : IConvertible
	{
		object m_result;

		public DummyIConvertible(object result)
		{
			m_result = result;
		}

		#region IConvertible implementation
		public TypeCode GetTypeCode()
		{
			throw new NotImplementedException();
		}

		public bool ToBoolean(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public byte ToByte(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public char ToChar(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public DateTime ToDateTime(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public decimal ToDecimal(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public double ToDouble(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public short ToInt16(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public int ToInt32(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public long ToInt64(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public sbyte ToSByte(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public float ToSingle(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public string ToString(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public object ToType(Type conversionType, IFormatProvider provider)
		{
			return m_result;
		}

		public ushort ToUInt16(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public uint ToUInt32(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public ulong ToUInt64(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
#endif
