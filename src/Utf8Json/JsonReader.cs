using System;
using System.Numerics;
using System.Text;
using Spreads.Buffers;
using Spreads.Serialization.Utf8Json.Internal;

#if NETSTANDARD

using System.Runtime.CompilerServices;

#endif

namespace Spreads.Serialization.Utf8Json
{
    // JSON RFC: https://www.ietf.org/rfc/rfc4627.txt

    public unsafe struct JsonReader
    {
        private static readonly OffHeapBuffer<byte> nullTokenSegment = CreateNullSegment();

        // private static readonly ArraySegment<byte> nullTokenSegment = new ArraySegment<byte>(new byte[] { 110, 117, 108, 108 }, 0, 4);
        private static readonly byte[] bom = Encoding.UTF8.GetPreamble();

        private static OffHeapBuffer<byte> CreateNullSegment()
        {
            var seg = new OffHeapBuffer<byte>(4);
            var db = seg.DirectBuffer;
            db[0] = 110;
            db[1] = 117;
            db[2] = 108;
            db[3] = 108;
            return seg;
        }

        private readonly DirectBuffer bytes;
        private readonly int _length;
        private int offset;

#if !SPREADS
        [Obsolete("For tests only. It pins the array and never releases it.")]
        public static JsonReader FromArray(byte[] bytes, int offset = 0)
        {
            var h = (bytes).AsMemory().Pin();
            var db = new DirectBuffer(bytes.Length, (byte*)h.Pointer);
            return new JsonReader(db.Slice(offset));
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonReader(DirectBuffer bytes)
        {
            this.bytes = bytes;
            this._length = bytes.Length;
            this.offset = 0;

            // skip bom
            if (bytes.Length >= 3)
            {
                if (bytes[offset] == bom[0] && bytes[offset + 1] == bom[1] && bytes[offset + 2] == bom[2])
                {
                    this.offset = offset += 3;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingException(string expected)
        {
            var actual = ((char)bytes[offset]).ToString();
            var pos = offset;

            try
            {
                var token = GetCurrentJsonToken();
                switch (token)
                {
                    case JsonToken.Number:
                        var ns = ReadNumberSegment();
                        actual = StringEncoding.UTF8.GetString(ns.Data, ns.Length);
                        break;

                    case JsonToken.String:
                        actual = "\"" + ReadString() + "\"";
                        break;

                    case JsonToken.True:
                        actual = "true";
                        break;

                    case JsonToken.False:
                        actual = "false";
                        break;

                    case JsonToken.Null:
                        actual = "null";
                        break;

                    default:
                        break;
                }
            }
            catch { }

            throw new JsonParsingException("expected:'" + expected + "', actual:'" + actual + "', at offset:" + pos, bytes, pos, offset, actual);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionMessage(string message)
        {
            var actual = ((char)bytes[offset]).ToString();
            var pos = offset;

            throw new JsonParsingException(message, bytes, pos, pos, actual);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionNotInRange()
        {
            var pos = offset;

            throw new JsonParsingException("Reached end of JSON", bytes, pos, pos, null);
        }

        public bool IsInRange
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => offset < _length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdvanceOffset(int offset)
        {
            this.offset += offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DirectBuffer GetBufferUnsafe()
        {
            return bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCurrentOffsetUnsafe()
        {
            return offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonToken GetCurrentJsonToken()
        {
            SkipWhiteSpace();
            if (offset < bytes.Length)
            {
                var c = bytes[offset];
                switch (c)
                {
                    case (byte)'{': return JsonToken.BeginObject;
                    case (byte)'}': return JsonToken.EndObject;
                    case (byte)'[': return JsonToken.BeginArray;
                    case (byte)']': return JsonToken.EndArray;
                    case (byte)'t': return JsonToken.True;
                    case (byte)'f': return JsonToken.False;
                    case (byte)'n': return JsonToken.Null;
                    case (byte)',': return JsonToken.ValueSeparator;
                    case (byte)':': return JsonToken.NameSeparator;
                    case (byte)'-': return JsonToken.Number;
                    case (byte)'0': return JsonToken.Number;
                    case (byte)'1': return JsonToken.Number;
                    case (byte)'2': return JsonToken.Number;
                    case (byte)'3': return JsonToken.Number;
                    case (byte)'4': return JsonToken.Number;
                    case (byte)'5': return JsonToken.Number;
                    case (byte)'6': return JsonToken.Number;
                    case (byte)'7': return JsonToken.Number;
                    case (byte)'8': return JsonToken.Number;
                    case (byte)'9': return JsonToken.Number;
                    case (byte)'\"': return JsonToken.String;
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15:
                    case 16:
                    case 17:
                    case 18:
                    case 19:
                    case 20:
                    case 21:
                    case 22:
                    case 23:
                    case 24:
                    case 25:
                    case 26:
                    case 27:
                    case 28:
                    case 29:
                    case 30:
                    case 31:
                    case 32:
                    case 33:
                    case 35:
                    case 36:
                    case 37:
                    case 38:
                    case 39:
                    case 40:
                    case 41:
                    case 42:
                    case 43:
                    case 46:
                    case 47:
                    case 59:
                    case 60:
                    case 61:
                    case 62:
                    case 63:
                    case 64:
                    case 65:
                    case 66:
                    case 67:
                    case 68:
                    case 69:
                    case 70:
                    case 71:
                    case 72:
                    case 73:
                    case 74:
                    case 75:
                    case 76:
                    case 77:
                    case 78:
                    case 79:
                    case 80:
                    case 81:
                    case 82:
                    case 83:
                    case 84:
                    case 85:
                    case 86:
                    case 87:
                    case 88:
                    case 89:
                    case 90:
                    case 92:
                    case 94:
                    case 95:
                    case 96:
                    case 97:
                    case 98:
                    case 99:
                    case 100:
                    case 101:
                    case 103:
                    case 104:
                    case 105:
                    case 106:
                    case 107:
                    case 108:
                    case 109:
                    case 111:
                    case 112:
                    case 113:
                    case 114:
                    case 115:
                    case 117:
                    case 118:
                    case 119:
                    case 120:
                    case 121:
                    case 122:
                    default:
                        return JsonToken.None;
                }
            }
            else
            {
                return JsonToken.None;
            }
        }

        private static readonly Vector<byte> WSCandidates = InitWsCandidates();

        private static Vector<byte> InitWsCandidates()
        {
            var bytes = new byte[Vector<byte>.Count];
            bytes[0] = 0x20;
            bytes[1] = 0x09;
            bytes[2] = 0x0A;
            bytes[3] = 0x0D;
            bytes[4] = (byte)'/';
            return new Vector<byte>(bytes);
        }

        /// <summary>
        /// Returns true if still in range after skipping ws
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SkipWhiteSpace()
        {
            if (!IsInRange)
            {
                return false;
            }

            if (Vector.IsHardwareAccelerated)
            {
                // Json written with this lib should not have WS, this is the fast path
                var vec = new Vector<byte>(bytes.Data[offset]);
                if (!Vector.EqualsAny(vec, WSCandidates))
                {
                    return true;
                }
            }

            return SkipWhiteSpaceSlow();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool SkipWhiteSpaceSlow()
        {
            var len = _length;

            for (int i = offset; i < _length; i++)
            {
                var bi = bytes.Data[i];
                if (bi == 0x20 || bi == 0x09 || bi == 0x0A || bi == 0x0D)
                {
                    continue;
                }

                if (bi == (byte)'/')
                {
                    i = ReadComment(bytes, i);
                    continue;
                }

                offset = i;
                return true; // end
            }

            offset = len;
            return false;
        }

        private static readonly Vector<byte> NullCandidates = InitNullCandidates();

        private static Vector<byte> InitNullCandidates()
        {
            Span<byte> sp = default;
            var bytes = new byte[Vector<byte>.Count];
            bytes[0] = 0x20;
            bytes[1] = 0x09;
            bytes[2] = 0x0A;
            bytes[3] = 0x0D;
            bytes[4] = (byte)'/';
            bytes[5] = (byte)'n';

            return new Vector<byte>(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadIsNull()
        {
            Vector<byte> vec = new Vector<byte>(*(bytes.Data + offset));
            if (Vector.IsHardwareAccelerated && !Vector.EqualsAny(vec, NullCandidates))
            {
                return false;
            }

            return ReadIsNullSlow();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool ReadIsNullSlow()
        {
            if (SkipWhiteSpace() && bytes[offset] == 'n')
            {
                if (bytes[offset + 1] != 'u') goto ERROR;
                if (bytes[offset + 2] != 'l') goto ERROR;
                if (bytes[offset + 3] != 'l') goto ERROR;
                offset += 4;
                return true;
            }
            else
            {
                return false;
            }

        ERROR:
            CreateParsingExceptionNull();
            return default;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionNull()
        {
            CreateParsingException("null");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadIsBeginArray()
        {
            if (SkipWhiteSpace() && bytes[offset] == '[')
            {
                offset += 1;
                return true;
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadIsBeginArrayWithVerify()
        {
            if (!ReadIsBeginArray())
            {
                CreateParsingException("[");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadIsEndArray()
        {
            if (SkipWhiteSpace() && bytes[offset] == ']')
            {
                offset += 1;
                return true;
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadIsEndArrayWithVerify()
        {
            if (!ReadIsEndArray())
            {
                CreateParsingException("]");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadIsEndArrayWithSkipValueSeparator(ref int count)
        {
            if (SkipWhiteSpace() && bytes[offset] == ']')
            {
                offset += 1;
                return true;
            }
            else
            {
                if (count++ != 0)
                {
                    ReadIsValueSeparatorWithVerify();
                }
                return false;
            }
        }

        /// <summary>
        /// Convinient pattern of ReadIsBeginArrayWithVerify + while(!ReadIsEndArrayWithSkipValueSeparator)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadIsInArray(ref int count)
        {
            if (count == 0)
            {
                ReadIsBeginArrayWithVerify();
                if (ReadIsEndArray())
                {
                    return false;
                }
            }
            else
            {
                if (ReadIsEndArray())
                {
                    return false;
                }
                else
                {
                    ReadIsValueSeparatorWithVerify();
                }
            }

            count++;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadIsBeginObject()
        {
            if (SkipWhiteSpace() && bytes.Data[offset] == '{')
            {
                offset += 1;
                return true;
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadIsBeginObjectWithVerify()
        {
            if (!ReadIsBeginObject())
            {
                CreateParsingExceptionBeginObject();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionBeginObject()
        {
            CreateParsingException("{");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadIsEndObject()
        {
            if (bytes.Data[offset] == '}')
            {
                offset += 1;
                return true;
            }

            return ReadIsEndObjectSlow();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool ReadIsEndObjectSlow()
        {
            if (SkipWhiteSpace() && bytes.Data[offset] == '}')
            {
                offset += 1;
                return true;
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadIsEndObjectWithVerify()
        {
            if (!ReadIsEndObject())
            {
                CreateParsingExceptionEndObject();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionEndObject()
        {
            CreateParsingException("}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadIsEndObjectWithSkipValueSeparator(ref int count)
        {
            var bo = bytes.Data[offset];
            var isEndObjFast = bo == '}';
            if (isEndObjFast || (bo == ',' && count++ != 0))
            {
                offset += 1;
                return isEndObjFast;
            }

            if (SkipWhiteSpace() && bytes.Data[offset] == '}')
            {
                offset += 1;
                return true;
            }
            else
            {
                if (count++ != 0)
                {
                    ReadIsValueSeparatorWithVerify();
                }

                return false;
            }
        }

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //private bool ReadIsEndObjectWithSkipValueSeparatorSlow(ref int count)
        //{
        //    SkipWhiteSpace();
        //    if (IsInRange && bytes[offset] == '}')
        //    {
        //        offset += 1;
        //        return true;
        //    }
        //    else
        //    {
        //        if (count++ != 0)
        //        {
        //            ReadIsValueSeparatorWithVerify();
        //        }

        //        return false;
        //    }
        //}

        /// <summary>
        /// Convinient pattern of ReadIsBeginObjectWithVerify + while(!ReadIsEndObjectWithSkipValueSeparator)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadIsInObject(ref int count)
        {
            if (count == 0)
            {
                ReadIsBeginObjectWithVerify();
                if (ReadIsEndObject())
                {
                    return false;
                }
            }
            else
            {
                if (ReadIsEndObject())
                {
                    return false;
                }
                else
                {
                    ReadIsValueSeparatorWithVerify();
                }
            }

            count++;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadIsValueSeparator()
        {
            if (bytes.Data[offset] == ',')
            {
                offset += 1;
                return true;
            }

            return ReadIsValueSeparatorSlow();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool ReadIsValueSeparatorSlow()
        {
            if (SkipWhiteSpace() && bytes.Data[offset] == ',')
            {
                offset += 1;
                return true;
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadIsValueSeparatorWithVerify()
        {
            if (!ReadIsValueSeparator())
            {
                CreateParsingExceptionValueSeparatorWithVerify();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionValueSeparatorWithVerify()
        {
            CreateParsingException(",");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadIsNameSeparator()
        {
            if (bytes.Data[offset] == ':')
            {
                offset += 1;
                return true;
            }

            return ReadIsNameSeparatorSlow();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool ReadIsNameSeparatorSlow()
        {
            if (SkipWhiteSpace() && bytes.Data[offset] == ':')
            {
                offset += 1;
                return true;
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadIsNameSeparatorWithVerify()
        {
            if (!ReadIsNameSeparator())
            {
                CreateParsingExceptionNameSeparatorWithVerify();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionNameSeparatorWithVerify()
        {
            CreateParsingException(":");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DirectBuffer ReadStringSegmentCore() // , out int resultOffset, out int resultLength)
        {
            // TODO (Spreads) test without ref var

            // SkipWhiteSpace is already called from IsNull
            ref OffHeapBuffer<byte> builder = ref StringBuilderCache.Buffer;
            var builderOffset = 0;
            ref OffHeapBuffer<char> codePointStringBuffer = ref StringBuilderCache.CodePointStringBuffer;
            var codePointStringOffet = 0;

            if (bytes.Data[offset] != '\"')
            {
                CreateParsingExceptionStringBeginToken();
            }
            offset++;

            var from = offset;

            // eliminate array-bound check
            for (int i = offset; i < _length; i++)
            {
                byte escapeCharacter = 0;
                switch (bytes.Data[i])
                {
                    case (byte)'\\': // escape character
                        switch ((char)bytes.Data[i + 1])
                        {
                            case '"':
                            case '\\':
                            case '/':
                                escapeCharacter = bytes.Data[i + 1];
                                goto COPY;
                            case 'b':
                                escapeCharacter = (byte)'\b';
                                goto COPY;
                            case 'f':
                                escapeCharacter = (byte)'\f';
                                goto COPY;
                            case 'n':
                                escapeCharacter = (byte)'\n';
                                goto COPY;
                            case 'r':
                                escapeCharacter = (byte)'\r';
                                goto COPY;
                            case 't':
                                escapeCharacter = (byte)'\t';
                                goto COPY;
                            case 'u':
                                // if (codePointStringBuffer == null) codePointStringBuffer = StringBuilderCache.GetCodePointStringBufferPtr();

                                if (codePointStringOffet == 0)
                                {
                                    // if (builder == null) builder = StringBuilderCache.GetBufferPtr();

                                    var copyCount = i - from;
                                    builder.EnsureCapacity(builderOffset + copyCount + 1);
                                    // BinaryUtil.EnsureCapacity(ref builder, builderOffset, copyCount + 1); // require + 1
                                    if (copyCount > 0)
                                    {
                                        Unsafe.CopyBlockUnaligned((byte*)builder._pointer + builderOffset, bytes.Data + from, (uint)copyCount);
                                    }
                                    builderOffset += copyCount;
                                }

                                if (StringBuilderCache.CodePointStringBuffer.Length == codePointStringOffet)
                                {
                                    codePointStringBuffer.EnsureCapacity(StringBuilderCache.CodePointStringBuffer.Length * 2);
                                    // Array.Resize(ref codePointStringBuffer, codePointStringBuffer.Length * 2);
                                }

                                var a = (char)bytes.Data[i + 2];
                                var b = (char)bytes.Data[i + 3];
                                var c = (char)bytes.Data[i + 4];
                                var d = (char)bytes.Data[i + 5];
                                var codepoint = GetCodePoint(a, b, c, d);
                                codePointStringBuffer[codePointStringOffet++] = (char)codepoint;
                                i += 5;
                                offset += 6;
                                from = offset;
                                continue;
                            default:
                                CreateParsingExceptionMessageBadJsonEscape();
                                // resultBytes = default;
                                //resultOffset = 0;
                                //resultLength = 0;
                                return default;
                        }
                    case (byte)'"': // endtoken
                        offset++;
                        goto END;
                    default: // string
                        if (codePointStringOffet != 0)
                        {
                            builder.EnsureCapacity(builderOffset + StringEncoding.UTF8.GetMaxByteCount(codePointStringOffet));

                            //if (builder == null) builder = StringBuilderCache.GetBuffer();
                            //BinaryUtil.EnsureCapacity(ref builder, builderOffset, StringEncoding.UTF8.GetMaxByteCount(codePointStringOffet));

                            builderOffset += StringEncoding.UTF8.GetBytes((char*)codePointStringBuffer._pointer, codePointStringOffet, (byte*)builder._pointer + builderOffset, builder.Length - builderOffset);
                            //builderOffset += StringEncoding.UTF8.GetBytes(codePointStringBuffer, 0, codePointStringOffet, builder, builderOffset);
                            codePointStringOffet = 0;
                        }
                        offset++;
                        continue;
                }

            COPY:
                {
                    // if (builder == null) builder = StringBuilderCache.GetBuffer();
                    if (codePointStringOffet != 0)
                    {
                        builder.EnsureCapacity(builderOffset + StringEncoding.UTF8.GetMaxByteCount(codePointStringOffet));
                        // BinaryUtil.EnsureCapacity(ref builder, builderOffset, StringEncoding.UTF8.GetMaxByteCount(codePointStringOffet));

                        builderOffset += StringEncoding.UTF8.GetBytes((char*)codePointStringBuffer._pointer, codePointStringOffet, (byte*)builder._pointer + builderOffset, builder.Length - builderOffset);
                        // builderOffset += StringEncoding.UTF8.GetBytes(codePointStringBuffer, 0, codePointStringOffet, builder, builderOffset);

                        codePointStringOffet = 0;
                    }

                    var copyCount = i - from;
                    builder.EnsureCapacity(builderOffset + copyCount + 1); // require + 1!
                    // BinaryUtil.EnsureCapacity(ref builder, builderOffset, copyCount + 1);

                    if (copyCount > 0) { Unsafe.CopyBlockUnaligned((byte*)builder._pointer + builderOffset, bytes.Data + from, (uint)copyCount); }
                    builderOffset += copyCount;
                    builder[builderOffset++] = escapeCharacter;
                    i += 1;
                    offset += 2;
                    from = offset;
                }
            }

            //resultBytes = DirectBuffer.Invalid;
            //resultOffset = 0;
            //resultLength = 0;
            CreateParsingExceptionStringEndToken();
            return default;

        END:
            if (builderOffset == 0 && codePointStringOffet == 0) // no escape
            {
                return bytes.Slice(from, offset - 1 - from);
                //resultOffset = from;
                //resultLength = offset - 1 - from; // skip last quote
            }
            else
            {
                // if (builder == null) builder = StringBuilderCache.GetBuffer();
                if (codePointStringOffet != 0)
                {
                    builder.EnsureCapacity(builderOffset + StringEncoding.UTF8.GetMaxByteCount(codePointStringOffet));
                    // BinaryUtil.EnsureCapacity(ref builder, builderOffset, StringEncoding.UTF8.GetMaxByteCount(codePointStringOffet));

                    builderOffset += StringEncoding.UTF8.GetBytes((char*)codePointStringBuffer._pointer, codePointStringOffet, (byte*)builder._pointer + builderOffset, builder.Length - builderOffset);
                    // builderOffset += StringEncoding.UTF8.GetBytes(codePointStringBuffer, 0, codePointStringOffet, builder, builderOffset);
                    codePointStringOffet = 0;
                }

                var copyCount = offset - from - 1;
                builder.EnsureCapacity(builderOffset + copyCount);
                // BinaryUtil.EnsureCapacity(ref builder, builderOffset, copyCount);
                if (copyCount > 0) { Unsafe.CopyBlockUnaligned((byte*)builder._pointer + builderOffset, bytes.Data + from, (uint)copyCount); }
                builderOffset += copyCount;

                return builder.DirectBuffer.Slice(0, builderOffset);
                //resultOffset = 0;
                //resultLength = builderOffset;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionMessageBadJsonEscape()
        {
            CreateParsingExceptionMessage("Bad JSON escape.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionStringBeginToken()
        {
            CreateParsingException("String Begin Token");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionStringEndToken()
        {
            CreateParsingException("String End Token");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetCodePoint(char a, char b, char c, char d)
        {
            return (((((ToNumber(a) * 16) + ToNumber(b)) * 16) + ToNumber(c)) * 16) + ToNumber(d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ToNumber(char x)
        {
            if ('0' <= x && x <= '9')
            {
                return x - '0';
            }
            else if ('a' <= x && x <= 'f')
            {
                return x - 'a' + 10;
            }
            else if ('A' <= x && x <= 'F')
            {
                return x - 'A' + 10;
            }

            JsonParsingExceptionInvalidChar(x);
            return default;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void JsonParsingExceptionInvalidChar(char x)
        {
            throw new JsonParsingException("Invalid Character" + x);
        }

        public DirectBuffer ReadStringSegmentUnsafe()
        {
            if (ReadIsNull()) return nullTokenSegment.DirectBuffer;

            return ReadStringSegmentCore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString()
        {
            if (ReadIsNull()) return null;

            return Encoding.UTF8.GetString(ReadStringSegmentCore());
        }

        /// <summary>ReadString + ReadIsNameSeparatorWithVerify</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadPropertyName()
        {
            var key = ReadString();
            ReadIsNameSeparatorWithVerify();
            return key;
        }

        /// <summary>Get raw string-span(do not unescape)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DirectBuffer ReadStringSegmentRaw()
        {
            // TODO Global constants
            const byte quote = (byte)'\"';
            const byte bs = (byte)'\\';

            if (*(bytes.Data + offset) != quote)
            {
                if (ReadIsNull())
                {
                    return nullTokenSegment.DirectBuffer;
                }

                // SkipWhiteSpace is already called from ReadIsNull

                if (*(bytes.Data + offset) != quote)
                {
                    CreateParsingExceptionNotQuote();
                }
            }

            offset++;

            var from = offset;

#if USE_SYSTEM_TEXT_JSON // - this is actually slower for this case, need review. Maybe could be useful for string values.
            // https://tools.ietf.org/html/rfc8259
            // Does the span contain '"', '\',  or any control characters (i.e. 0 to 31)
            // IndexOfAny(34, 92, < 32)
            // Borrowed from System.Text.Json.JsonReaderHelper:
            var idx = offset + Algorithms.VectorSearch.IndexOfOrLessThan(
                            ref bytes.Data[offset],
                            (byte)'\"',
                            (byte)'\\',
                            lessThan: 32, // Space ' '
                            _length - offset);
            if (bytes.Data[idx] == '\"')
            {
                offset = idx + 1;
                goto OK;
            }
#else
            var idx = offset;
#endif
            // ReadStringSegmentRaw is used to read property names. They are usually
            // human-readable and very often more than 3 symbols, so we access
            // memory once per 4/8 bytes and most often there is no quote. If there
            // is one we already have it's index and go to per-byte processing for there.

            if (IntPtr.Size == 8)
            {
                for (; idx + 8 < _length; idx += 8)
                {
                    var bi = Unsafe.ReadUnaligned<EightBytes>(bytes.Data + idx);
                    var qi = bi.QuoteIndex(out var unescaped);

                    if (qi >= 0)
                    {
                        if (unescaped)
                        {
                            offset = idx + qi + 1;
                            goto OK;
                        }

                        idx += qi;
                        break;
                    }
                }
            }
            else
            {
                for (; idx + 4 < _length; idx += 4)
                {
                    var bi = Unsafe.ReadUnaligned<FourBytes>(bytes.Data + idx);
                    var qi = bi.QuoteIndex(out var unescaped);

                    if (qi >= 0)
                    {
                        if (unescaped)
                        {
                            offset = idx + qi + 1;
                            goto OK;
                        }

                        idx += qi;
                        break;
                    }
                }
            }

            for (var i = idx; i < _length; i++)
            {
                var bi = bytes.Data[i];

                if (bi == quote)
                {
                    // is escape?
                    if (bytes.Data[i - 1] != bs || bytes.Data[i - 2] == bs)
                    {
                        offset = i + 1;
                        goto OK;
                    }
                }
            }

            CreateParsingExceptionMessageNotFoundEndString();

        OK:
            var key = bytes.Slice(@from, offset - @from - 1);

            return key;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionNotQuote()
        {
            CreateParsingException("\"");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionMessageNotFoundEndString()
        {
            CreateParsingExceptionMessage("not found end string.");
        }

        /// <summary>Get raw string-span(do not unescape) + ReadIsNameSeparatorWithVerify</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DirectBuffer ReadPropertyNameSegmentRaw()
        {
            var key = ReadStringSegmentRaw();
            ReadIsNameSeparatorWithVerify();
            return key;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
        {
            SkipWhiteSpace();
            if (bytes.Data[offset] == 't')
            {
                if (bytes.Data[offset + 1] != 'r') goto ERROR_TRUE;
                if (bytes.Data[offset + 2] != 'u') goto ERROR_TRUE;
                if (bytes.Data[offset + 3] != 'e') goto ERROR_TRUE;
                offset += 4;
                return true;
            }
            else if (bytes.Data[offset] == 'f')
            {
                if (bytes.Data[offset + 1] != 'a') goto ERROR_FALSE;
                if (bytes.Data[offset + 2] != 'l') goto ERROR_FALSE;
                if (bytes.Data[offset + 3] != 's') goto ERROR_FALSE;
                if (bytes.Data[offset + 4] != 'e') goto ERROR_FALSE;
                offset += 5;
                return false;
            }
            else
            {
                CreateParsingException("true | false");
            }

        ERROR_TRUE:
            CreateParsingException("true");
        ERROR_FALSE:
            CreateParsingException("false");
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsWordBreak(byte c)
        {
            if (c == (byte)' ' || c == (byte)'{' || c == (byte)'}' || c == (byte)'[' || c == (byte)']' ||
                c == (byte)',' || c == (byte)':' || c == (byte)'\"')
                return true;
            else
                return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadNext()
        {
            var token = GetCurrentJsonToken();
            ReadNextCore(token);
        }

#if NETSTANDARD

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private
#endif
        void ReadNextCore(JsonToken token)
        {
            switch (token)
            {
                case JsonToken.BeginObject:
                case JsonToken.BeginArray:
                case JsonToken.ValueSeparator:
                case JsonToken.NameSeparator:
                case JsonToken.EndObject:
                case JsonToken.EndArray:
                    offset += 1;
                    break;

                case JsonToken.String:
                    {
                        offset += 1; // position is "\"";
                        for (int i = offset; i < _length; i++)
                        {
                            if (bytes.Data[i] == (char)'\"')
                            {
                                // is escape?
                                if (bytes.Data[i - 1] == (char)'\\')
                                {
                                    continue;
                                }
                                else
                                {
                                    offset = i + 1;
                                    return; // end
                                }
                            }
                        }

                        CreateParsingExceptionMessageNotFoundEnd();
                        break;
                    }
                case JsonToken.Number:
                    {
                        for (int i = offset; i < _length; i++)
                        {
                            if (IsWordBreak(bytes.Data[i]))
                            {
                                offset = i;
                                return;
                            }
                        }

                        offset = _length;
                        break;
                    }
                case JsonToken.True:
                case JsonToken.Null:
                    offset += 4;
                    break;

                case JsonToken.False:
                    offset += 5;
                    break;

                default:
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionMessageNotFoundEnd()
        {
            CreateParsingExceptionMessage("not found end string.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadNextBlock()
        {
            var stack = 0;

        AGAIN:
            var token = GetCurrentJsonToken();
            switch (token)
            {
                case JsonToken.BeginObject:
                case JsonToken.BeginArray:
                    offset++;
                    stack++;
                    goto AGAIN;
                case JsonToken.EndObject:
                case JsonToken.EndArray:
                    offset++;
                    stack--;
                    if (stack != 0)
                    {
                        goto AGAIN;
                    }
                    break;

                case JsonToken.True:
                case JsonToken.False:
                case JsonToken.Null:
                case JsonToken.String:
                case JsonToken.Number:
                case JsonToken.NameSeparator:
                case JsonToken.ValueSeparator:
                    do
                    {
                        ReadNextCore(token);
                        token = GetCurrentJsonToken();
                    } while (stack != 0 && !((int)token < 5)); // !(None, Begin/EndObject, Begin/EndArray)

                    if (stack != 0)
                    {
                        goto AGAIN;
                    }
                    break;

                case JsonToken.None:
                default:
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DirectBuffer ReadNextBlockSegment()
        {
            var startOffset = offset;
            ReadNextBlock();
            return bytes.Slice(startOffset, offset - startOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte()
        {
            return checked((sbyte)ReadInt64());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            return checked((short)ReadInt64());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            return checked((int)ReadInt64());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            if (SkipWhiteSpace())
            {
                int readCount;
                var v = NumberConverter.ReadInt64(bytes, offset, out readCount);
                if (readCount == 0)
                {
                    CreateParsingExceptionNumberToken();
                }

                offset += readCount;
                return v;
            }
            CreateParsingExceptionNotInRange();
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            return checked((byte)ReadUInt64());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            return checked((ushort)ReadUInt64());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            return checked((uint)ReadUInt64());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            if (SkipWhiteSpace())
            {
                int readCount;
                var v = NumberConverter.ReadUInt64(bytes, offset, out readCount);
                if (readCount == 0)
                {
                    CreateParsingExceptionNumberToken();
                }
                offset += readCount;
                return v;
            }

            CreateParsingExceptionNotInRange();
            return default;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CreateParsingExceptionNumberToken()
        {
            CreateParsingException("Number Token");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Single ReadSingle()
        {
            SkipWhiteSpace();
            int readCount;
            var v = Utf8Json.Internal.DoubleConversion.StringToDoubleConverter.ToSingle(bytes, offset, out readCount);
            if (readCount == 0)
            {
                CreateParsingExceptionNumberToken();
            }
            offset += readCount;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double ReadDouble()
        {
            SkipWhiteSpace();
            int readCount;
            var v = Utf8Json.Internal.DoubleConversion.StringToDoubleConverter.ToDouble(bytes, offset, out readCount);
            if (readCount == 0)
            {
                CreateParsingExceptionNumberToken();
            }
            offset += readCount;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DirectBuffer ReadNumberSegment()
        {
            SkipWhiteSpace();
            var initialOffset = offset;
            for (int i = offset; i < _length; i++)
            {
                if (!NumberConverter.IsNumberRepresentation(bytes[i]))
                {
                    offset = i;
                    goto END;
                }
            }
            offset = _length;

        END:
            return bytes.Slice(initialOffset, offset - initialOffset);
        }

        // return last offset.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ReadComment(DirectBuffer bytes, int offset)
        {
            // current token is '/'
            if (bytes[offset + 1] == '/')
            {
                // single line
                offset += 2;
                for (int i = offset; i < bytes.Length; i++)
                {
                    if (bytes[i] == '\r' || bytes[i] == '\n')
                    {
                        return i;
                    }
                }

                ReadCommentJsonParsingExceptionSingle();
            }
            else if (bytes[offset + 1] == '*')
            {
                offset += 2; // '/' + '*';
                for (int i = offset; i < bytes.Length; i++)
                {
                    if (bytes[i] == '*' && bytes[i + 1] == '/')
                    {
                        return i + 1;
                    }
                }
                ReadCommentJsonParsingExceptionMulti();
            }

            return offset;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ReadCommentJsonParsingExceptionSingle()
        {
            throw new JsonParsingException("Can not find end token of single line comment(\r or \n).");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ReadCommentJsonParsingExceptionMulti()
        {
            throw new JsonParsingException("Can not find end token of multi line comment(*/).");
        }

        internal static class StringBuilderCache
        {
            [ThreadStatic]
            private static OffHeapBuffer<byte> _buffer;

            [ThreadStatic]
            public static OffHeapBuffer<char> _codePointStringBuffer;

            public static ref OffHeapBuffer<byte> Buffer
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _buffer;
            }

            public static ref OffHeapBuffer<char> CodePointStringBuffer
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _codePointStringBuffer;
            }
        }
    }

    public class JsonParsingException : Exception
    {
        private DirectBuffer underyingBytes;
        private int limit;
        public int Offset { get; private set; }
        public string ActualChar { get; set; }

        public JsonParsingException(string message)
            : base(message)
        {
        }

        public JsonParsingException(string message, DirectBuffer underlyingBytes, int offset, int limit, string actualChar)
            : base(message)
        {
            this.underyingBytes = underlyingBytes;
            this.Offset = offset;
            this.ActualChar = actualChar;
            this.limit = limit;
        }

        /// <summary>
        /// Underlying bytes is may be a pooling buffer, be careful to use it. If lost reference or can not handled byte[], return null.
        /// </summary>
        public DirectBuffer GetUnderlyingByteArrayUnsafe()
        {
            return underyingBytes;
        }

        /// <summary>
        /// Underlying bytes is may be a pooling buffer, be careful to use it. If lost reference or can not handled byte[], return null.
        /// </summary>
        public unsafe string GetUnderlyingStringUnsafe()
        {
            var bytes = underyingBytes;
            if (bytes.IsValid)
            {
                return StringEncoding.UTF8.GetString(bytes.Data, limit) + "...";
            }
            return null;
        }
    }
}
