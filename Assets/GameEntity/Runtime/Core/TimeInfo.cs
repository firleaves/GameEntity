using System;

namespace GE
{
    internal class TimeInfo: Singleton<TimeInfo>, ISingletonAwake
    {
        private int _timeZone;
        
        public int TimeZone
        {
            get
            {
                return this._timeZone;
            }
            set
            {
                this._timeZone = value;
                _dt = _dt1970.AddHours(TimeZone);
            }
        }
        
        private DateTime _dt1970;
        private DateTime _dt;
        
        // ping消息会设置该值，原子操作
        public long ServerMinusClientTime { private get; set; }

        public long FrameTime { get; private set; }
        
        public void Awake()
        {
            this._dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            this._dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            this.FrameTime = this.ClientNow();
        }

        public void Update()
        {
            // 赋值long型是原子操作，线程安全
            this.FrameTime = this.ClientNow();
        }
        
        /// <summary> 
        /// 根据时间戳获取时间 
        /// </summary>  
        public DateTime ToDateTime(long timeStamp)
        {
            return _dt.AddTicks(timeStamp * 10000);
        }
        
        // 线程安全
        public long ClientNow()
        {
            return (DateTime.UtcNow.Ticks - this._dt1970.Ticks) / 10000;
        }
        
        public long ServerNow()
        {
            return ClientNow() + this.ServerMinusClientTime;
        }
        
        public long ClientFrameTime()
        {
            return this.FrameTime;
        }
        
        public long ServerFrameTime()
        {
            return this.FrameTime + this.ServerMinusClientTime;
        }
        
        public long Transition(DateTime d)
        {
            return (d.Ticks - _dt.Ticks) / 10000;
        }
    }
}