using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace GE
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct IdStruct
    {
        public short Process;  // 14bit
        public uint Time;    // 30bit
        public uint Value;   // 20bit

        public long ToLong()
        {
            ulong result = 0;
            result |= (ushort)this.Process;
            result <<= 30;
            result |= this.Time;
            result <<= 20;
            result |= this.Value;
            return (long)result;
        }

        public IdStruct(uint time, short process, uint value)
        {
            this.Process = process;
            this.Time = time;
            this.Value = value;
        }

        public IdStruct(long id)
        {
            ulong result = (ulong)id;
            this.Value = (uint)(result & IdGenerator.Mask20bit);
            result >>= 20;
            this.Time = (uint)result & IdGenerator.Mask30bit;
            result >>= 30;
            this.Process = (short)(result & IdGenerator.Mask14bit);
        }

        public override string ToString()
        {
            return $"process: {this.Process}, time: {this.Time}, value: {this.Value}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct InstanceIdStruct
    {
        public uint Time;  // 32bit
        public uint Value; // 32bit

        public long ToLong()
        {
            ulong result = 0;
            result |= this.Time;
            result <<= 32;
            result |= this.Value;
            return (long)result;
        }

        public InstanceIdStruct(uint time, uint value)
        {
            this.Time = time;
            this.Value = value;
        }

        public InstanceIdStruct(long id)
        {
            ulong result = (ulong)id;
            this.Value = (uint)(result & uint.MaxValue);
            result >>= 32;
            this.Time = (uint)(result & uint.MaxValue);
        }

        public override string ToString()
        {
            return $"time: {this.Time}, value: {this.Value}";
        }
    }

    internal class IdGenerator : Singleton<IdGenerator>, ISingletonAwake
    {
        public const int MaxZone = 1024;

        public const int Mask14bit = 0x3fff;
        public const int Mask30bit = 0x3fffffff;
        public const int Mask20bit = 0xfffff;

        private long _epoch2022;

        private int _value;
        private int _instanceIdValue;

        public void Awake()
        {
            long epoch1970tick = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000;
            this._epoch2022 = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000 - epoch1970tick;
        }

        private uint TimeSince2022()
        {
            uint a = (uint)((TimeInfo.Instance.FrameTime - this._epoch2022) / 1000);
            return a;
        }

        public long GenerateId()
        {
            uint time = TimeSince2022();
            int v = 0;
            // 这里必须加锁
            lock (this)
            {
                if (++this._value > Mask20bit - 1)
                {
                    this._value = 0;
                }
                v = this._value;
            }

            IdStruct idStruct = new(time, (short)0, (uint)v);
            return idStruct.ToLong();
        }

        public long GenerateInstanceId()
        {
            uint time = this.TimeSince2022();
            uint v = (uint)Interlocked.Add(ref this._instanceIdValue, 1);
            InstanceIdStruct instanceIdStruct = new(time, v);
            return instanceIdStruct.ToLong();
        }
    }
}
