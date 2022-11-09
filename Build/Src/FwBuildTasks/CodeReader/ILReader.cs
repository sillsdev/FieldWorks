// Lifted from github.com/sillsdev/l10nsharp on 2016.11.07

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace FwBuildTasks
{
	/// ----------------------------------------------------------------------------------------
	internal class ILInstruction
	{
		public readonly OpCode opCode;
		public readonly object operand;

		/// ------------------------------------------------------------------------------------
		public ILInstruction(OpCode opCode, object operand)
		{
			this.opCode = opCode;
			this.operand = operand;
		}

		public override string ToString()
		{
			return $"{opCode}}}\t{{{operand}";
		}

		public override bool Equals(object other)
		{
			var otherInstr = other as ILInstruction;
			if (otherInstr == null)
				return false;
			return opCode == otherInstr.opCode && Equals(operand, otherInstr.operand);
		}

		public override int GetHashCode()
		{
			return opCode.GetHashCode() * operand.GetHashCode();
		}
	}

	/// ----------------------------------------------------------------------------------------
	internal class ILReader : IEnumerable<ILInstruction>
	{
		private readonly byte[] _byteArray;
		private int _position;

		private static readonly OpCode[] OneByteOpCodes = new OpCode[0x100];
		private static readonly OpCode[] TwoByteOpCodes = new OpCode[0x100];

		/// ------------------------------------------------------------------------------------
		static ILReader()
		{
			foreach (var fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				OpCode opCode = (OpCode)fi.GetValue(null);
				ushort value = (ushort)opCode.Value;

				if (value < 0x100)
					OneByteOpCodes[value] = opCode;
				else if ((value & 0xff00) == 0xfe00)
					TwoByteOpCodes[value & 0xff] = opCode;
			}
		}

		/// ------------------------------------------------------------------------------------
		public ILReader(MethodBase enclosingMethod)
		{
			var methodBody = enclosingMethod.GetMethodBody();
			_byteArray = ((methodBody == null) ? new byte[0] : methodBody.GetILAsByteArray()) ?? new byte[0];
			_position = 0;
		}

		/// ------------------------------------------------------------------------------------
		public IEnumerator<ILInstruction> GetEnumerator()
		{
			while (_position < _byteArray.Length)
				yield return Next();

			_position = 0;
		}

		/// ------------------------------------------------------------------------------------
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// ------------------------------------------------------------------------------------
		private ILInstruction Next()
		{
			//Int32 offset = _position;
			OpCode opCode;

			// read first 1 or 2 bytes as opCode
			var code = ReadByte();
			if (code != 0xFE)
				opCode = OneByteOpCodes[code];
			else
			{
				code = ReadByte();
				opCode = TwoByteOpCodes[code];
			}

			object operand;

			switch (opCode.OperandType)
			{
				case OperandType.InlineNone: operand = null; break;
				case OperandType.ShortInlineBrTarget: operand = ReadSByte(); break;
				case OperandType.InlineBrTarget: operand = ReadInt32(); break;
				case OperandType.ShortInlineI: operand = ReadByte(); break;
				case OperandType.InlineI: operand = ReadInt32(); break;
				case OperandType.InlineI8: operand = ReadInt64(); break;
				case OperandType.ShortInlineR: operand = ReadSingle(); break;
				case OperandType.InlineR: operand = ReadDouble(); break;
				case OperandType.ShortInlineVar: operand = ReadByte(); break;
				case OperandType.InlineVar: operand = ReadUInt16(); break;
				case OperandType.InlineString: operand = ReadInt32(); break;
				case OperandType.InlineSig: operand = ReadInt32(); break;
				case OperandType.InlineField: operand = ReadInt32(); break;
				case OperandType.InlineType: operand = ReadInt32(); break;
				case OperandType.InlineTok: operand = ReadInt32(); break;
				case OperandType.InlineMethod: operand = ReadInt32(); break;
				case OperandType.InlineSwitch:
					int cases = ReadInt32();
					int[] deltas = new int[cases];
					for (int i = 0; i < cases; i++)
						deltas[i] = ReadInt32();
					operand = deltas;
					break;
				default:
					throw new BadImageFormatException("unexpected OperandType " + opCode.OperandType);
			}

			return new ILInstruction(opCode, operand);
		}

		private byte ReadByte() { return _byteArray[_position++]; }
		private sbyte ReadSByte() { return (sbyte)ReadByte(); }

		private ushort ReadUInt16() { _position += 2; return BitConverter.ToUInt16(_byteArray, _position - 2); }
		//UInt32 ReadUInt32() { _position += 4; return BitConverter.ToUInt32(_byteArray, _position - 4); }
		//UInt64 ReadUInt64() { _position += 8; return BitConverter.ToUInt64(_byteArray, _position - 8); }

		private int ReadInt32() { _position += 4; return BitConverter.ToInt32(_byteArray, _position - 4); }
		private long ReadInt64() { _position += 8; return BitConverter.ToInt64(_byteArray, _position - 8); }

		private float ReadSingle() { _position += 4; return BitConverter.ToSingle(_byteArray, _position - 4); }
		private double ReadDouble() { _position += 8; return BitConverter.ToDouble(_byteArray, _position - 8); }
	}
}