using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This exception is thrown by Loaders during the load process. It is used
	/// to indicate loading and configuration parsing errors.
	/// </summary>
	public class LoadException : Exception
	{
		/// <summary>
		/// The specific error type of morph exception. This is useful for displaying
		/// user-friendly error messages.
		/// </summary>
		public enum LoadErrorType
		{
			/// <summary>
			/// An object is not defined.
			/// </summary>
			UNDEFINED_OBJECT,
			/// <summary>
			/// A error occurred while parsing the configuration.
			/// </summary>
			PARSE_ERROR,
			/// <summary>
			/// Too many feature values were defined.
			/// </summary>
			TOO_MANY_FEATURE_VALUES,
			/// <summary>
			/// A character definition table could not translate a phonetic shape used in a rule.
			/// </summary>
			INVALID_RULE_SHAPE,
			/// <summary>
			/// A character definition table could not translate a phonetic shape used in a lexical entry.
			/// </summary>
			INVALID_ENTRY_SHAPE,
			/// <summary>
			/// A phonological rule has an unknown subrule type.
			/// </summary>
			INVALID_SUBRULE_TYPE,
			/// <summary>
			/// The configuration is in the incorrect format.
			/// </summary>
			INVALID_FORMAT,
			/// <summary>
			/// There is no current morpher selected in the loader.
			/// </summary>
			NO_CURRENT_MORPHER
		}

		LoadErrorType m_errorType;
		Loader m_loader;

		public LoadException(LoadErrorType errorType, Loader loader)
		{
			m_errorType = errorType;
			m_loader = loader;
		}

		public LoadException(LoadErrorType errorType, Loader loader, string message)
			: base(message)
		{
			m_errorType = errorType;
			m_loader = loader;
		}

		public LoadException(LoadErrorType errorType, Loader loader, string message, Exception inner)
			: base(message, inner)
		{
			m_errorType = errorType;
			m_loader = loader;
		}

		public LoadErrorType ErrorType
		{
			get
			{
				return m_errorType;
			}
		}

		public Loader Loader
		{
			get
			{
				return m_loader;
			}
		}
	}

	/// <summary>
	/// This class is the abstract class that all Loader classes inherit from.
	/// Loaders are responsible for parsing the input configuration file, loading
	/// Morphers, and running commands.
	/// </summary>
	public abstract class Loader
	{
		protected Morpher m_curMorpher = null;
		protected HCObjectSet<Morpher> m_morphers;
		protected bool m_quitOnError = true;
		protected IOutput m_output = null;
		protected bool m_isLoaded = false;
		protected bool m_traceInputs = true;

		public Loader()
		{
			m_morphers = new HCObjectSet<Morpher>();
		}

		/// <summary>
		/// Gets the default output encoding.
		/// </summary>
		/// <value>The default output encoding.</value>
		public abstract Encoding DefaultOutputEncoding
		{
			get;
		}

		/// <summary>
		/// Gets all loaded morphers.
		/// </summary>
		/// <value>All morphers.</value>
		public IEnumerable<Morpher> Morphers
		{
			get
			{
				return m_morphers;
			}
		}

		/// <summary>
		/// Gets the current morpher.
		/// </summary>
		/// <value>The current morpher.</value>
		public Morpher CurrentMorpher
		{
			get
			{
				return m_curMorpher;
			}
		}

		/// <summary>
		/// Gets or sets the output.
		/// </summary>
		/// <value>The output.</value>
		public IOutput Output
		{
			get
			{
				return m_output;
			}

			set
			{
				m_output = value;
			}
		}

		public bool IsLoaded
		{
			get
			{
				return m_isLoaded;
			}
		}

		public bool QuitOnError
		{
			get
			{
				return m_quitOnError;
			}

			set
			{
				m_quitOnError = value;
			}
		}

		/// <summary>
		/// Gets the morpher associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The morpher.</returns>
		public Morpher GetMorpher(string id)
		{
			Morpher morpher;
			if (m_morphers.TryGetValue(id, out morpher))
				return morpher;
			return null;
		}

		/// <summary>
		/// Loads the specified config file and runs all commands.
		/// </summary>
		/// <param name="configFile">The config file.</param>
		public abstract void Load(string configFile);

		public abstract void Load();

		public virtual void Reset()
		{
			m_curMorpher = null;
			m_morphers.Clear();
			m_isLoaded = false;
		}

		protected void MorphAndLookupWord(string word, bool prettyPrint)
		{
			if (m_output != null)
				m_output.MorphAndLookupWord(m_curMorpher, word, prettyPrint, m_traceInputs);
		}

		protected LoadException CreateUndefinedObjectException(string message, string id)
		{
			LoadException le = new LoadException(LoadException.LoadErrorType.UNDEFINED_OBJECT, this, message);
			le.Data["id"] = id;
			return le;
		}
	}

}
