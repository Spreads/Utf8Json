using BenchmarkDotNet.Attributes;
using Spreads.Buffers;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Utf8Json;

namespace PerfBenchmark
{
    [Config(typeof(BenchmarkConfig))]
    public class SerializeBenchmark
    {
        public static TargetClass obj1;
        public static TargetClassContractless objContractless;

        private static readonly Encoding utf8 = Encoding.UTF8;
        private const int OperationsPerInvoke = 1000;

        static SerializeBenchmark()
        {
            Spreads.Settings.DoAdditionalCorrectnessChecks = false;
            var rand = new Random(34151513);
            obj1 = TargetClass.Create(rand);
            objContractless = new TargetClassContractless(obj1);

            // warmup JIT
            Spreads.Serialization.Utf8Json.JsonSerializer.Serialize(obj1);
            JsonSerializer.Serialize(obj1);
            MessagePack.MessagePackSerializer.Serialize(obj1);
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, obj1);
            }

            utf8.GetBytes(global::Jil.JSON.Serialize(obj1));
            utf8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(obj1));
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public byte[] SpreadsJsonSerializer()
        {
            for (int _ = 0; _ < OperationsPerInvoke; _++)
            {
                Spreads.Serialization.Utf8Json.JsonSerializer.Serialize(obj1);
            }
            return Spreads.Serialization.Utf8Json.JsonSerializer.Serialize(obj1);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public byte[] Utf8JsonSerializer()
        {
            for (int _ = 0; _ < OperationsPerInvoke; _++)
            {
                JsonSerializer.Serialize(obj1);
            }
            return JsonSerializer.Serialize(obj1);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public byte[] MessagePackCSharp()
        {
            for (int _ = 0; _ < OperationsPerInvoke; _++)
            {
                MessagePack.MessagePackSerializer.Serialize(obj1);
            }
            return MessagePack.MessagePackSerializer.Serialize(obj1);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public byte[] MessagePackCSharpContractless()
        {
            for (int _ = 0; _ < OperationsPerInvoke; _++)
            {
                MessagePack.MessagePackSerializer.Serialize(objContractless, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            }
            return MessagePack.MessagePackSerializer.Serialize(objContractless, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public void Protobufnet()
        {
            using (var ms = new MemoryStream())
            {
                for (int _ = 0; _ < OperationsPerInvoke; _++)
                {
                    ProtoBuf.Serializer.Serialize(ms, obj1);
                    ms.Position = 0;
                }
            }
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, obj1);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public byte[] Jil()
        {
            for (int _ = 0; _ < OperationsPerInvoke; _++)
            {
                utf8.GetBytes(global::Jil.JSON.Serialize(obj1));
            }
            return utf8.GetBytes(global::Jil.JSON.Serialize(obj1));
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public void JilTextWriter()
        {
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms, utf8))
            {
                for (int _ = 0; _ < OperationsPerInvoke; _++)
                {
                    global::Jil.JSON.Serialize(obj1, sw);
                    ms.Position = 0;
                }
            }
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms, utf8))
            {
                global::Jil.JSON.Serialize(obj1, sw);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public byte[] JsonNet()
        {
            for (int _ = 0; _ < OperationsPerInvoke; _++)
            {
                utf8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(obj1));
            }

            return utf8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(obj1));
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public unsafe class DeserializeBenchmark
    {
        private static DirectBuffer jsonDb;
        private static byte[] json = new SerializeBenchmark().Utf8JsonSerializer();
        private static byte[] proto;
        private static byte[] msgpack1 = new SerializeBenchmark().MessagePackCSharp();
        private static byte[] msgpack2 = new SerializeBenchmark().MessagePackCSharpContractless();
        private static Encoding utf8 = Encoding.UTF8;

        private const int OperationsPerInvoke = 1000;

        static DeserializeBenchmark()
        {
            Spreads.Settings.DoAdditionalCorrectnessChecks = false;
            var mem = ((Memory<byte>)json).Pin();
            jsonDb = new DirectBuffer(json.Length, (byte*)mem.Pointer);
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, SerializeBenchmark.obj1);
                proto = ms.ToArray();
            }

            // warm up JIT
            Spreads.Serialization.Utf8Json.JsonSerializer.Deserialize<TargetClass>(jsonDb);
            JsonSerializer.Deserialize<TargetClass>(json);
            MessagePack.MessagePackSerializer.Deserialize<TargetClass>(msgpack1);
            MessagePack.MessagePackSerializer.Deserialize<TargetClassContractless>(msgpack2, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Deserialize<TargetClass>(ms);
            }
            global::Jil.JSON.Deserialize<TargetClass>(utf8.GetString(json));
            Newtonsoft.Json.JsonConvert.DeserializeObject<TargetClass>(utf8.GetString(json));
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public TargetClass SpreadsJsonSerializer()
        {
            for (int _ = 0; _ < OperationsPerInvoke; _++)
            {
                Spreads.Serialization.Utf8Json.JsonSerializer.Deserialize<TargetClass>(jsonDb);
            }
            return Spreads.Serialization.Utf8Json.JsonSerializer.Deserialize<TargetClass>(jsonDb);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public TargetClass Utf8JsonSerializer()
        {
            for (int _ = 0; _ < OperationsPerInvoke; _++)
            {
                JsonSerializer.Deserialize<TargetClass>(json);
            }
            return JsonSerializer.Deserialize<TargetClass>(json);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public TargetClass MessagePackCSharp()
        {
            for (int _ = 0; _ < OperationsPerInvoke; _++)
            {
                MessagePack.MessagePackSerializer.Deserialize<TargetClass>(msgpack1);
            }
            return MessagePack.MessagePackSerializer.Deserialize<TargetClass>(msgpack1);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public TargetClassContractless MessagePackCSharpContractless()
        {
            for (int _ = 0; _ < OperationsPerInvoke; _++)
            {
                MessagePack.MessagePackSerializer.Deserialize<TargetClassContractless>(msgpack2, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            }
            return MessagePack.MessagePackSerializer.Deserialize<TargetClassContractless>(msgpack2, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public TargetClass Protobufnet()
        {
            using (var ms = new MemoryStream())
            {
                for (int _ = 0; _ < OperationsPerInvoke; _++)
                {
                    ProtoBuf.Serializer.Deserialize<TargetClass>(ms);
                    ms.Position = 0;
                }
            }

            using (var ms = new MemoryStream())
            {
                return ProtoBuf.Serializer.Deserialize<TargetClass>(ms);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public TargetClass Jil()
        {
            for (int _ = 0; _ < OperationsPerInvoke; _++)
            {
                global::Jil.JSON.Deserialize<TargetClass>(utf8.GetString(json));
            }
            return global::Jil.JSON.Deserialize<TargetClass>(utf8.GetString(json));
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public TargetClass JilTextReader()
        {
            using (var ms = new MemoryStream(json))
            using (var sr = new StreamReader(ms, utf8))
            {
                for (int _ = 0; _ < OperationsPerInvoke; _++)
                {
                    global::Jil.JSON.Deserialize<TargetClass>(sr);
                    ms.Position = 0;
                }
            }

            using (var ms = new MemoryStream(json))
            using (var sr = new StreamReader(ms, utf8))
            {
                return global::Jil.JSON.Deserialize<TargetClass>(sr);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public TargetClass JsonNet()
        {
            for (int _ = 0; _ < OperationsPerInvoke; _++)
            {
                Newtonsoft.Json.JsonConvert.DeserializeObject<TargetClass>(utf8.GetString(json));
            }
            return Newtonsoft.Json.JsonConvert.DeserializeObject<TargetClass>(utf8.GetString(json));
        }
    }
}