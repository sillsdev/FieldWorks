// --------------------------------------------------------------------------------------------
// Copyright (C) 2008 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: DB4oCustomSerialization.cs
// Responsibility: Randy Regnier
// Last reviewed: never
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using Db4objects.Db4o.Typehandlers;
using Db4objects.Db4o.Reflect;
using Db4objects.Db4o.Internal.Handlers;
using Db4objects.Db4o.Marshall;
using Db4objects.Db4o.Internal.Marshall;
using Db4objects.Db4o.Internal;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	///
	/// </summary>
	internal class CmObjectSurrogateTypeHandlerPredicate : ITypeHandlerPredicate
	{
		/// <summary>
		/// return true if a TypeHandler is to be used for a specific
		/// Type
		/// </summary>
		/// <param name="classReflector">
		/// the Type passed by db4o that is to
		/// be tested by this predicate.
		/// </param>
		/// <returns>
		/// true if the TypeHandler is to be used for a specific
		/// Type.
		/// </returns>
		public bool Match(IReflectClass classReflector)
		{
			var claxx = classReflector.Reflector().ForClass(typeof(CmObjectSurrogate));
			return claxx.Equals(classReflector);
		}
	}

	/// <summary>
	/// TODO (TomH): move this extenstion class MiscUtils (currently not, in order that db4o optimization changes to are restricted to FDO.dll, for easier testing)
	/// </summary>
	public static class StreamExtenstions
	{
		/// <summary>
		/// Read a 4 byte int from a Stream.
		/// </summary>
		public static int ReadInt(this Stream stream)
		{
			var bytes = new byte[4];
			stream.Read(bytes, 0, 4);

			return bytes[3] | (bytes[2] << 8) | (bytes[1] << 16) | (bytes[0] << 24);
		}

		/// <summary>
		/// Read a 8 byte long from a Stream.
		/// </summary>
		public static long ReadLong(this Stream stream)
		{
			var bytes = new byte[8];
			stream.Read(bytes, 0, 8);

			return bytes[7] | (bytes[6] << 8) | (bytes[5] << 16) | (bytes[4] << 24) | (bytes[3] << 32) | (bytes[2] << 40) | (bytes[1] << 48) | (bytes[0] << 56);
		}

		/// <summary>
		/// Read an array of bytes from a stream.
		/// </summary>
		public static byte[] ReadBytes(this Stream stream, int length)
		{
			var ret = new byte[length];
			stream.Read(ret, 0, length);
			return ret;
		}
	}

	/// <summary>
	/// Allow iternating over a stream of compressed CmObjectSurrogate's.
	/// Review (TomH): should this class be moved?
	/// </summary>
	internal class CmObjectSurrogateStreamDecompressor : IEnumerable<CmObjectSurrogate>, IEnumerator<CmObjectSurrogate>
	{
		private readonly FdoCache m_cache;
		private readonly IdentityMap m_identityMap;
		private readonly UnicodeEncoding m_unicodeEncoding = new UnicodeEncoding();

		private GZipStream m_decompressor;
		private CmObjectSurrogate m_current;

		// reference to the m_mapId so we can updated with the db4o id's.
		private Dictionary<ICmObjectId, long> m_mapId;

		public CmObjectSurrogateStreamDecompressor(byte[] compressedBytes, FdoCache cache, IdentityMap identitymap, Dictionary<ICmObjectId, long> mapid)
		{
			m_decompressor = new GZipStream(new MemoryStream(compressedBytes), CompressionMode.Decompress);
			m_cache = cache;
			m_identityMap = identitymap;
			m_mapId = mapid;
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~CmObjectSurrogateStreamDecompressor()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				m_decompressor.Dispose();
			}
			m_decompressor = null;
			IsDisposed = true;
		}
		#endregion

		public IEnumerator<CmObjectSurrogate> GetEnumerator()
		{
			return this;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool MoveNext()
		{
			CmObjectSurrogate obj = GetNextCmObjectSurrogate(m_decompressor);
			if (obj == null)
				return false;

			m_current = obj;
			return true;
		}

		/// <summary>
		/// returns null if there are no more CmObejtcSurroages on the stream.
		/// </summary>
		internal CmObjectSurrogate GetNextCmObjectSurrogate(GZipStream stream)
		{
			var temp = new byte[16];
			if (stream.Read(temp, 0, 16) <= 0)
				return null;
			var cmObjectId = CmObjectId.FromGuid(new Guid(temp), m_identityMap);
			string className = m_unicodeEncoding.GetString(stream.ReadBytes(stream.ReadInt()));
			byte[] xmldata = stream.ReadBytes(stream.ReadInt());

			long id = stream.ReadLong();

			var ret = new CmObjectSurrogate(m_cache, cmObjectId, className, xmldata);

			m_mapId.Add(ret.Id, id);

			return ret;
		}

		public void Reset()
		{
			throw new NotImplementedException();
		}

		public object Current
		{
			get { return m_current; }
		}

		CmObjectSurrogate IEnumerator<CmObjectSurrogate>.Current
		{
			get { return m_current; }
		}
	}

	/// <summary>
	/// Used to record all the raw data of the CmObjectSurroages during
	/// server mode activation in CmObjectSurrogateTypeHandler.
	/// Review (TomH): should this class be moved?
	/// </summary>
	[Serializable]
	internal class CachedProjectInformation : IDisposable
	{
		protected MemoryStream m_stream = new MemoryStream();
		protected GZipStream m_compressedCmObjectSurrogatesRawData;
		private bool m_disposed;

		public CachedProjectInformation()
		{
			m_stream = new MemoryStream();
			m_compressedCmObjectSurrogatesRawData = new GZipStream(m_stream, CompressionMode.Compress);
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~CachedProjectInformation()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				m_stream.Dispose();

				if (m_compressedCmObjectSurrogatesRawData != null)
					m_compressedCmObjectSurrogatesRawData.Dispose();
			}
			m_stream = null;
			m_compressedCmObjectSurrogatesRawData = null;
			IsDisposed = true;
		}
		#endregion

		public GZipStream Compressor
		{
			get { return m_compressedCmObjectSurrogatesRawData; }
		}

		public MemoryStream CompressedMemoryStream
		{
			get { return m_stream; }
		}
	}

	/// <summary>
	/// Maybe use StandardReferenceTypeHandler for superclass, if queries are needed.
	/// This class has two modes of operations, depending on how it is constructed.
	/// If constructed with a FdoCache and IdentityMap it operates in the normal client
	/// mode, activating CmObjectSurrogates.
	/// If Constructed with the default constructor it operates in a server mode which
	/// doesn't activate CmObjectSurrogates, rather uses a supplied CachedProjectInformation
	/// to write all its raw data.
	/// </summary>
	internal class CmObjectSurrogateTypeHandler : PlainObjectHandler
	{
		#region ServerOnly Handling Mode
		private readonly bool m_serverOnlyMode;

		// The inital offset in ByteArrayBuffer from context, in Activate method.
		internal const int ExpectedOffset = 24;

		internal CachedProjectInformation CachedProjectInformation { get; set; }

		internal void ActivateServer(IReferenceActivationContext context)
		{
			var context2 = (UnmarshallingContext)context;
			var buffer = (ByteArrayBuffer)context2.Buffer();

			if (buffer._offset != ExpectedOffset)
				throw new ApplicationException("unexpected offset value, has db4o version changed?");

			// Find total bytes.
			int storedOffset = buffer._offset;
			int total = 0;
			buffer.ReadBytes(16); total += 16;
			int stringLength = buffer.ReadInt(); total += sizeof(int);
			buffer.ReadBytes(stringLength); total += stringLength;
			int xmlLength = buffer.ReadInt(); total += sizeof(int);
			total += xmlLength;
			// don't need to do the last read.

			long id = context.ObjectContainer().Ext().GetID((CmObjectSurrogate) context.PersistentObject());

			CachedProjectInformation.Compressor.Write(buffer._buffer, storedOffset, total);

			// need to tranfer the id to the client in order to populate the id map (m_idMap).
			// quickest way to do this is to encode it in the compression data.

			// Can't use BitConverter because of endinenss differences, with the way db4o ByteArrayBuffer encodes int to bytes.
			CachedProjectInformation.Compressor.Write(GetBytes(id), 0, 8);
		}

		// TODO (TomH) : move this into some appropriate utils class.
		internal byte[] GetBytes(long val)
		{
			var temp = new byte[8];
			for (int i = 7; i >= 0; --i)
				temp[i] = (byte)(val >> ((7 - i) * 8));
			return temp;
		}


		#endregion

		private readonly FdoCache m_cache;
		private readonly IdentityMap m_identitymap;
		private readonly UnicodeEncoding m_unicodeEnc = new UnicodeEncoding();

		/// <summary>
		/// Constructor.
		/// </summary>
		internal CmObjectSurrogateTypeHandler(FdoCache cache, IdentityMap identitymap)
		{
			m_cache = cache;
			m_identitymap = identitymap;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		internal CmObjectSurrogateTypeHandler()
		{
			m_serverOnlyMode = true;
		}

		// ITypeHandler4 override
		public override void Write(IWriteContext context, object obj)
		{
			if (m_serverOnlyMode)
				throw new ApplicationException("Programming error. Should not be writing db4o objects in server mode.");

			var asSurrogate = (ICmObjectSurrogate) obj;
			// Write Guid.
			context.WriteBytes(asSurrogate.Guid.ToByteArray());
			// Write class name.
			var classBytes = m_unicodeEnc.GetBytes(asSurrogate.Classname);
			context.WriteInt(classBytes.Length);
			context.WriteBytes(classBytes);
			// Write the XML data.
			var dataBytes = asSurrogate.XMLBytes;
			context.WriteInt(dataBytes.Length);
			context.WriteBytes(dataBytes);
		}

		// IReferenceTypeHandler override
		public override void Activate(IReferenceActivationContext context)
		{
			if (m_serverOnlyMode)
			{
				ActivateServer(context);
				return;
			}

			if (m_cache == null) throw new NullReferenceException("m_cache");
			if (m_identitymap == null) throw new NullReferenceException("m_identitymap");

			var context2 = (UnmarshallingContext)context;
			var buffer = (ByteArrayBuffer)context2.Buffer();

			// Get Guid and CmObjectId.
			// Earlier code used a special CmObject.Create() method, I think because it is faster,
			// and on the assumption that we only activate objects at startup, so none of the
			// IDs have been seen before. However, with a background thread fluffing things, we
			// COULD have seen it, even during startup, if we've fluffed some object that references
			// this one. Also, this code is called in client-server mode when we Refresh an object
			// to see whether someone else changed it, and then the ID is very likely to exist already.
			// It MAY be possible to re-introduce the faster ID creation code just during startup
			// if we're not overlapping fluffing and reading.
			var reconId = CmObjectId.FromGuid(new Guid(buffer.ReadBytes(16)), m_identitymap);
			// Get class name.
			var className = m_unicodeEnc.GetString(buffer.ReadBytes(buffer.ReadInt()));
			// Get the data.
			var dataLength = buffer.ReadInt();
			// Reconstitute the surrogate.
			((CmObjectSurrogate)context.PersistentObject()).InitializeFromDataStore(
				m_cache,
				reconId,
				className,
				buffer.ReadBytes(dataLength));
		}
	}

	/// <summary>
	///
	/// </summary>
	internal class CustomFieldInfoTypeHandlerPredicate : ITypeHandlerPredicate
	{
		#region Implementation of ITypeHandlerPredicate

		/// <summary>
		/// return true if a TypeHandler is to be used for a specific
		/// Type
		/// </summary>
		/// <param name="classReflector">
		/// the Type passed by db4o that is to
		/// be tested by this predicate.
		/// </param>
		/// <returns>
		/// true if the TypeHandler is to be used for a specific
		/// Type.
		/// </returns>
		public bool Match(IReflectClass classReflector)
		{
			var claxx = classReflector.Reflector().ForClass(typeof(CustomFieldInfo));
			return claxx.Equals(classReflector);
		}

		#endregion
	}

	/// <summary>
	/// Maybe use StandardReferenceTypeHandler for superclass, if queries are needed.
	/// </summary>
	internal class CustomFieldInfoTypeHandler : PlainObjectHandler
	{

		// ITypeHandler4 override
		public override void Write(IWriteContext context, object obj)
		{
			// Write the data.
			var cfi = (CustomFieldInfo)obj;
			context.WriteInt(cfi.m_flid);
			context.WriteInt((int)cfi.m_fieldType);
			context.WriteInt(cfi.m_destinationClass);
			var cvtr = new UnicodeEncoding();
			var bytes = cvtr.GetBytes(cfi.m_classname);
			context.WriteInt(bytes.Length);
			context.WriteBytes(bytes);
			bytes = cvtr.GetBytes(cfi.m_fieldname);
			context.WriteInt(bytes.Length);
			context.WriteBytes(bytes);
			if (cfi.m_fieldname != cfi.Label)
			{
				// marker to distinguish this from any other optional info we may one day write,
				// and from cases where no label is written.
				context.WriteByte(1);
				bytes = cvtr.GetBytes(cfi.Label);
				context.WriteInt(bytes.Length);
				context.WriteBytes(bytes);
			}
			if (!String.IsNullOrEmpty(cfi.m_fieldHelp))
			{
				context.WriteByte(2);
				bytes = cvtr.GetBytes(cfi.m_fieldHelp);
				context.WriteInt(bytes.Length);
				context.WriteBytes(bytes);

			}
			if (cfi.m_fieldListRoot != Guid.Empty)
			{
				context.WriteByte(3);
				bytes = cfi.m_fieldListRoot.ToByteArray();
				context.WriteInt(bytes.Length);
				context.WriteBytes(bytes);
			}
			if (cfi.m_fieldWs != 0)
			{
				context.WriteByte(4);
				context.WriteInt(cfi.m_fieldWs);
			}
			// End marker
			context.WriteByte(0);
		}

		// IReferenceTypeHandler override
		public override void Activate(IReferenceActivationContext context)
		{
			var context2 = (UnmarshallingContext)context;
			var buffer = (ByteArrayBuffer)context2.Buffer();
			// Get the data.
			var cfi = (CustomFieldInfo)context.PersistentObject();
			cfi.m_flid = buffer.ReadInt();
			cfi.m_fieldType = (CellarPropertyType) buffer.ReadInt();
			cfi.m_destinationClass = buffer.ReadInt();
			var cvtr = new UnicodeEncoding();
			var len = buffer.ReadInt();
			cfi.m_classname = cvtr.GetString(buffer.ReadBytes(len));
			len = buffer.ReadInt();
			cfi.m_fieldname = cvtr.GetString(buffer.ReadBytes(len));
			// If we didn't write a marker byte (e.g., older data), this currently returns zero
			// (or maybe throws a fit, otherwise known as an exception).  For any current
			// database, we explicitly write a zero.
			byte tag;
			try
			{
				tag = context.ReadByte();
			}
			catch
			{
				tag = 0;
			}
			while (tag != 0) // label
			{
				switch(tag)
				{
					case 1:
						len = buffer.ReadInt();
						cfi.Label = cvtr.GetString(buffer.ReadBytes(len));
						break;
					case 2:
						len = buffer.ReadInt();
						cfi.m_fieldHelp = cvtr.GetString(buffer.ReadBytes(len));
						break;
					case 3:
						len = buffer.ReadInt();
						Debug.Assert(len == Guid.Empty.ToByteArray().Length);
						cfi.m_fieldListRoot = new Guid(buffer.ReadBytes(len));
						break;
					case 4:
						cfi.m_fieldWs = buffer.ReadInt();
						break;
				}
				tag = context.ReadByte();
			}
		}
	}

	/// <summary>
	/// A barebones way to store the model version number in DB4o.
	/// </summary>
	internal class ModelVersionNumber
	{
		internal int m_modelVersionNumber;
	}

	/// <summary>
	/// A singleton we write to the db4o backend to keep track of whether an object has been written
	/// since we read it.
	/// </summary>
	internal class WriteGeneration
	{
		internal int Generation;
	}
	/// <summary>
	///
	/// </summary>
	internal class ModelVersionNumberTypeHandlerPredicate : ITypeHandlerPredicate
	{
		/// <summary>
		/// return true if a TypeHandler is to be used for a specific
		/// Type
		/// </summary>
		/// <param name="classReflector">
		/// the Type passed by db4o that is to
		/// be tested by this predicate.
		/// </param>
		/// <returns>
		/// true if the TypeHandler is to be used for a specific
		/// Type.
		/// </returns>
		public bool Match(IReflectClass classReflector)
		{
			var claxx = classReflector.Reflector().ForClass(typeof(ModelVersionNumber));
			return claxx.Equals(classReflector);
		}
	}

	/// <summary>
	/// Maybe use StandardReferenceTypeHandler for superclass, if queries are needed.
	/// </summary>
	internal class ModelVersionNumberTypeHandler : PlainObjectHandler
	{
		// ITypeHandler4 override
		public override void Write(IWriteContext context, object obj)
		{
			// Write the data.
			context.WriteInt(((ModelVersionNumber)obj).m_modelVersionNumber);
		}

		// IReferenceTypeHandler override
		public override void Activate(IReferenceActivationContext context)
		{
			var context2 = (UnmarshallingContext)context;
			var buffer = (ByteArrayBuffer)context2.Buffer();
			// Get the data.
			((ModelVersionNumber) context.PersistentObject()).m_modelVersionNumber = buffer.ReadInt();
		}
	}
}
