using Spreads.Buffers;
using Spreads.Serialization.Utf8Json.Internal;
using Spreads.Serialization.Utf8Json.Resolvers;
using Spreads.Serialization.Utf8Json.Resolvers.Internal;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BufferPool = Spreads.Serialization.Utf8Json.Internal.BufferPool;

namespace Spreads.Serialization.Utf8Json
{
    /// <summary>
    /// High-Level API of Utf8Json.
    /// </summary>
    public static partial class JsonSerializer
    {
        private static readonly IJsonFormatterResolver defaultResolver = StandardResolver.Default;

        /// <summary>
        /// FormatterResolver that used resolver less overloads. If does not set it, used StandardResolver.Default.
        /// </summary>
        public static IJsonFormatterResolver DefaultResolver
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                //if (defaultResolver == null)
                //{
                //    defaultResolver = StandardResolver.Default;
                //}

                return defaultResolver;
            }
        }

        /// <summary>
        /// Is resolver decided?
        /// </summary>
        public static bool IsInitialized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return defaultResolver != null;
            }
        }

        // TODO we actually could set it before touching this class
        ///// <summary>
        ///// Set default resolver of Utf8Json APIs.
        ///// </summary>
        ///// <param name="resolver"></param>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void SetDefaultResolver(IJsonFormatterResolver resolver)
        //{
        //    defaultResolver = resolver;
        //}

        /// <summary>
        /// Serialize to binary with default resolver.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Serialize<T>(T obj)
        {
            return Serialize(obj, null);
        }

        /// <summary>
        /// Serialize to binary with specified resolver.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Serialize<T>(T value, IJsonFormatterResolver resolver)
        {
            var writer = new JsonWriter(MemoryPool.GetBuffer());
            Serialize(ref writer, value, resolver);
            return writer.ToUtf8ByteArray();
        }

#if SPREADS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RecyclableMemoryStream SerializeWithOffset<T>(T value, int offset)
        {
            // if we need to resize then all intermediate buffers are returned to the pool (in FastResize)
            // when RMS is disposed then the final buffer is also returned to the pool
            var bufferSize = 65535;
            if (offset >= bufferSize)
            {
                ThrowHelper.ThrowInvalidOperationException();
            }
            var resolver = StandardResolver.Default;
            var buffer = BufferPool<byte>.Rent(bufferSize);
            var writer = new JsonWriter(buffer, offset);
            var formatter = resolver.GetFormatterWithVerify<T>();
            formatter.Serialize(ref writer, value, resolver);
            if (writer.CurrentOffset > buffer.Length)
            {
                buffer = writer.GetBuffer().Array;
            }
            return RecyclableMemoryStream.Create(buffer.Length, buffer, writer.CurrentOffset, null, RecyclableMemoryStreamManager.Default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ArraySegment<byte> SerializeToRentedBuffer<T>(T value, int offset = 0)
        {
            const int bufferSize = 65535;
            if (offset >= bufferSize)
            {
                ThrowHelper.ThrowInvalidOperationException();
            }
            var resolver = DefaultResolver;

            var buffer = BufferPool<byte>.Rent(bufferSize);
            var writer = new JsonWriter(buffer, offset);
            var formatter = resolver.GetFormatterWithVerify<T>();
            formatter.Serialize(ref writer, value, resolver);
            // writer expands the buffer as needed using the same BufferPool<byte>
            if (writer.CurrentOffset > buffer.Length)
            {
                buffer = writer.GetBuffer().Array;
            }

            return new ArraySegment<byte>(buffer, offset, writer.CurrentOffset - offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RetainedMemory<byte> SerializeToRetainedMemory<T>(T value, int offset = 0)
        {
            var segment = SerializeToRentedBuffer(value, offset);
            var arrayMemory = ArrayMemory<byte>.Create(segment.Array, segment.Offset - offset, segment.Count + offset, externallyOwned: false, pin: false);
            return arrayMemory.Retain();
        }

#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize<T>(ref JsonWriter writer, T value)
        {
            Serialize<T>(ref writer, value, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize<T>(ref JsonWriter writer, T value, IJsonFormatterResolver resolver)
        {
            if (resolver == null)
            {
                if (DefaultStandardResolver.FormatterCache<T>.formatter != null)
                {
                    DefaultStandardResolver.FormatterCache<T>.formatter.Serialize(ref writer, value,
                        DefaultStandardResolver
                            .Instance); // StandardResolver.Default.GetFormatter<>().GetFormatterWithVerify<T>();
                    return;
                }

                resolver = DefaultResolver;
            }

            var formatter = resolver.GetFormatterWithVerify<T>();
            formatter.Serialize(ref writer, value, resolver);
        }

        /// <summary>
        /// Serialize to stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize<T>(Stream stream, T value)
        {
            Serialize(stream, value, defaultResolver);
        }

        /// <summary>
        /// Serialize to stream with specified resolver.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize<T>(Stream stream, T value, IJsonFormatterResolver resolver)
        {
            if (resolver == null) resolver = DefaultResolver;

            var buffer = SerializeUnsafe(value, resolver);
            stream.Write(buffer.Array, buffer.Offset, buffer.Count);
        }

#if NETSTANDARD

        /// <summary>
        /// Serialize to stream(write async).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Threading.Tasks.Task SerializeAsync<T>(Stream stream, T value)
        {
            return SerializeAsync<T>(stream, value, defaultResolver);
        }

        /// <summary>
        /// Serialize to stream(write async) with specified resolver.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async System.Threading.Tasks.Task SerializeAsync<T>(Stream stream, T value, IJsonFormatterResolver resolver)
        {
            if (resolver == null) resolver = DefaultResolver;

            var buf = BufferPool.Default.Rent();
            try
            {
                var writer = new JsonWriter(buf);
                var formatter = resolver.GetFormatterWithVerify<T>();
                formatter.Serialize(ref writer, value, resolver);
                var buffer = writer.GetBuffer();
                await stream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count).ConfigureAwait(false);
            }
            finally
            {
                BufferPool.Default.Return(buf);
            }
        }

#endif

        /// <summary>
        /// Serialize to binary. Get the raw memory pool byte[]. The result can not share across thread and can not hold, so use quickly.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArraySegment<byte> SerializeUnsafe<T>(T obj)
        {
            return SerializeUnsafe(obj, defaultResolver);
        }

        /// <summary>
        /// Serialize to binary with specified resolver. Get the raw memory pool byte[]. The result can not share across thread and can not hold, so use quickly.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArraySegment<byte> SerializeUnsafe<T>(T value, IJsonFormatterResolver resolver)
        {
            if (resolver == null) resolver = DefaultResolver;

            var writer = new JsonWriter(MemoryPool.GetBuffer());
            var formatter = resolver.GetFormatterWithVerify<T>();
            formatter.Serialize(ref writer, value, resolver);
            return writer.GetBuffer();
        }

        /// <summary>
        /// Serialize to JsonString.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToJsonString<T>(T value)
        {
            return ToJsonString(value, defaultResolver);
        }

        /// <summary>
        /// Serialize to JsonString with specified resolver.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToJsonString<T>(T value, IJsonFormatterResolver resolver)
        {
            if (resolver == null) resolver = DefaultResolver;

            var writer = new JsonWriter(MemoryPool.GetBuffer());
            var formatter = resolver.GetFormatterWithVerify<T>();
            formatter.Serialize(ref writer, value, resolver);
            return writer.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(string json)
        {
            return Deserialize<T>(json, defaultResolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(string json, IJsonFormatterResolver resolver)
        {
            return Deserialize<T>(StringEncoding.UTF8.GetBytes(json), resolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(DirectBuffer bytes)
        {
            return Deserialize<T>(bytes, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(DirectBuffer bytes, IJsonFormatterResolver resolver)
        {
            var reader = new JsonReader(bytes);
            if (resolver == null)
            {
                if (DefaultStandardResolver.FormatterCache<T>.formatter != null)
                {
                    return DefaultStandardResolver.FormatterCache<T>.formatter.Deserialize(ref reader,
                        DefaultStandardResolver
                            .Instance); // StandardResolver.Default.GetFormatter<>().GetFormatterWithVerify<T>();
                }
                resolver = DefaultResolver;
            }

            var formatter = resolver.GetFormatterWithVerify<T>();
            return formatter.Deserialize(ref reader, resolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(byte[] bytes)
        {
            return Deserialize<T>(bytes, defaultResolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(byte[] bytes, IJsonFormatterResolver resolver)
        {
            return Deserialize<T>(bytes, 0, resolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(byte[] bytes, int offset)
        {
            return Deserialize<T>(bytes, offset, defaultResolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(ReadOnlyMemory<byte> memory)
        {
            return Deserialize<T>(memory, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T Deserialize<T>(ReadOnlyMemory<byte> memory, IJsonFormatterResolver resolver)
        {
            if (resolver == null) resolver = DefaultResolver;
            var mh = memory.Pin();
            try
            {
                var db = new DirectBuffer(memory.Length, (byte*)mh.Pointer);
                var reader = new JsonReader(db);
                var formatter = resolver.GetFormatterWithVerify<T>();
                return formatter.Deserialize(ref reader, resolver);
            }
            finally
            {
                mh.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T Deserialize<T>(byte[] bytes, int offset, IJsonFormatterResolver resolver)
        {
            if (resolver == null) resolver = DefaultResolver;

            fixed (byte* ptr = &bytes[0])
            {
                var directBuffer = new DirectBuffer(bytes.Length, ptr);
                var reader = new JsonReader(directBuffer);
                var formatter = resolver.GetFormatterWithVerify<T>();
                return formatter.Deserialize(ref reader, resolver);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(ref JsonReader reader)
        {
            return Deserialize<T>(ref reader, defaultResolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(ref JsonReader reader, IJsonFormatterResolver resolver)
        {
            if (resolver == null) resolver = DefaultResolver;

            var formatter = resolver.GetFormatterWithVerify<T>();
            return formatter.Deserialize(ref reader, resolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(Stream stream)
        {
            return Deserialize<T>(stream, defaultResolver);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(Stream stream, IJsonFormatterResolver resolver)
        {
            if (resolver == null) resolver = DefaultResolver;

#pragma warning disable 618
            if (stream is RecyclableMemoryStream rms && rms.IsSingleChunk)
            {
                return Deserialize<T>(rms.SingleChunk, resolver);
            }
#pragma warning restore 618

#if NETSTANDARD && !NET45
            if (stream is MemoryStream ms)
            {
                if (ms.TryGetBuffer(out var buf))
                {
                    return Deserialize<T>(buf, resolver);
                }
            }
#endif

            if (stream.CanSeek)
            {
                var len = checked((int)stream.Length);
                var buf2 = BufferPool<byte>.Rent(len);
                try
                {
                    int numBytesToRead = len;
                    int numBytesRead = 0;
                    do
                    {
                        int n = stream.Read(buf2, numBytesRead, len);
                        numBytesRead += n;
                        numBytesToRead -= n;
                    } while (numBytesToRead > 0);

                    return Deserialize<T>(new ArraySegment<byte>(buf2, 0, len), resolver);
                }
                finally
                {
                    BufferPool<byte>.Return(buf2);
                }
            }

            {
                var buf3 = BufferPool<byte>.Rent(65536);
                var len = FillFromStream(stream, ref buf3);
                try
                {
                    return Deserialize<T>(new ArraySegment<byte>(buf3, 0, len), resolver);
                }
                finally
                {
                    BufferPool<byte>.Return(buf3);
                }
            }
        }

        public static System.Threading.Tasks.Task<T> DeserializeAsync<T>(Stream stream)
        {
            return DeserializeAsync<T>(stream, defaultResolver);
        }

        public static async System.Threading.Tasks.Task<T> DeserializeAsync<T>(Stream stream, IJsonFormatterResolver resolver)
        {
            throw new NotImplementedException("Async is fake in Utf8Json, it is done over sync methods w.r.t. serialization and requires a full buffer.");

            //if (resolver == null) resolver = DefaultResolver;

            //var buffer = BufferPool.Default.Rent();
            //var buf = buffer;
            //try
            //{
            //    int length = 0;
            //    int read;
            //    while ((read = await stream.ReadAsync(buf, length, buf.Length - length).ConfigureAwait(false)) > 0)
            //    {
            //        length += read;
            //        if (length == buf.Length)
            //        {
            //            BinaryUtil.FastResize(ref buf, length * 2);
            //        }
            //    }

            //    // when token is number, can not use from pool(can not find end line).
            //    var token = new JsonReader(buf).GetCurrentJsonToken();
            //    if (token == JsonToken.Number)
            //    {
            //        buf = BinaryUtil.FastCloneWithResize(buf, length);
            //    }

            //    return Deserialize<T>(buf, resolver);
            //}
            //finally
            //{
            //    BufferPool.Default.Return(buffer);
            //}
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static string PrettyPrint(byte[] json)
        //{
        //    return PrettyPrint(json, 0);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static string PrettyPrint(byte[] json, int offset)
        //{
        //    var reader = new JsonReader(json, offset);
        //    var writer = new JsonWriter(MemoryPool.GetBuffer());
        //    WritePrittyPrint(ref reader, ref writer, 0);
        //    return writer.ToString();
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static string PrettyPrint(string json)
        //{
        //    var reader = new JsonReader(Encoding.UTF8.GetBytes(json));
        //    var writer = new JsonWriter(MemoryPool.GetBuffer());
        //    WritePrittyPrint(ref reader, ref writer, 0);
        //    return writer.ToString();
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static byte[] PrettyPrintByteArray(byte[] json)
        //{
        //    return PrettyPrintByteArray(json, 0);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static byte[] PrettyPrintByteArray(byte[] json, int offset)
        //{
        //    var reader = new JsonReader(json, offset);
        //    var writer = new JsonWriter(MemoryPool.GetBuffer());
        //    WritePrittyPrint(ref reader, ref writer, 0);
        //    return writer.ToUtf8ByteArray();
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static byte[] PrettyPrintByteArray(string json)
        //{
        //    var reader = new JsonReader(Encoding.UTF8.GetBytes(json));
        //    var writer = new JsonWriter(MemoryPool.GetBuffer());
        //    WritePrittyPrint(ref reader, ref writer, 0);
        //    return writer.ToUtf8ByteArray();
        //}

        private static readonly byte[][] indent = Enumerable.Range(0, 100).Select(x => Encoding.UTF8.GetBytes(new string(' ', x * 2))).ToArray();
        private static readonly byte[] newLine = Encoding.UTF8.GetBytes(Environment.NewLine);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WritePrittyPrint(ref JsonReader reader, ref JsonWriter writer, int depth)
        {
            var token = reader.GetCurrentJsonToken();
            switch (token)
            {
                case JsonToken.BeginObject:
                    {
                        writer.WriteBeginObject();
                        writer.WriteRaw(newLine);
                        var c = 0;
                        while (reader.ReadIsInObject(ref c))
                        {
                            if (c != 1)
                            {
                                writer.WriteRaw((byte)',');
                                writer.WriteRaw(newLine);
                            }
                            writer.WriteRaw(indent[depth + 1]);
                            writer.WritePropertyName(reader.ReadPropertyName());
                            writer.WriteRaw((byte)' ');
                            WritePrittyPrint(ref reader, ref writer, depth + 1);
                        }
                        writer.WriteRaw(newLine);
                        writer.WriteRaw(indent[depth]);
                        writer.WriteEndObject();
                    }
                    break;

                case JsonToken.BeginArray:
                    {
                        writer.WriteBeginArray();
                        writer.WriteRaw(newLine);
                        var c = 0;
                        while (reader.ReadIsInArray(ref c))
                        {
                            if (c != 1)
                            {
                                writer.WriteRaw((byte)',');
                                writer.WriteRaw(newLine);
                            }
                            writer.WriteRaw(indent[depth + 1]);
                            WritePrittyPrint(ref reader, ref writer, depth + 1);
                        }
                        writer.WriteRaw(newLine);
                        writer.WriteRaw(indent[depth]);
                        writer.WriteEndArray();
                    }
                    break;

                case JsonToken.Number:
                    {
                        var v = reader.ReadDouble();
                        writer.WriteDouble(v);
                    }
                    break;

                case JsonToken.String:
                    {
                        var v = reader.ReadString();
                        writer.WriteString(v);
                    }
                    break;

                case JsonToken.True:
                case JsonToken.False:
                    {
                        var v = reader.ReadBoolean();
                        writer.WriteBoolean(v);
                    }
                    break;

                case JsonToken.Null:
                    {
                        reader.ReadIsNull();
                        writer.WriteNull();
                    }
                    break;

                default:
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FillFromStream(Stream input, ref byte[] buffer)
        {
            int length = 0;
            int read;
            while ((read = input.Read(buffer, length, buffer.Length - length)) > 0)
            {
                length += read;
                if (length == buffer.Length)
                {
                    BinaryUtil.FastResize(ref buffer, length * 2);
                }
            }

            return length;
        }

        private static class MemoryPool
        {
#if !SPREADS
            [ThreadStatic]
            private static byte[] buffer = null;
#endif

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static byte[] GetBuffer()
            {
#if !SPREADS
                if (buffer == null)
                {
                    buffer = new byte[65536];
                }
                return buffer;
#else
                return Buffers.BufferPool.StaticBuffer.Array;
#endif
            }
        }
    }
}