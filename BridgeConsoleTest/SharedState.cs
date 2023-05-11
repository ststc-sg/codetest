using System.Threading;

namespace BridgeConsole
{
    internal class SharedState
    {
        private readonly Mutex _lockGuard = new Mutex();
        private float _prowThruster;
        private float _prowThrusterSetter;
        private float _sternThruster;
        private float _sternThrusterSetter;

        public bool IsBridgeControl;

        public float ProwThruster
        {
            get
            {
                lock (_lockGuard)
                {
                    return _prowThruster;
                }
            }
            private set
            {
                lock (_lockGuard)
                {
                    _prowThruster = value;
                }
            }
        }

        public float SternThruster
        {
            get
            {
                lock (_lockGuard)
                {
                    return _sternThruster;
                }
            }
            private set
            {
                lock (_lockGuard)
                {
                    _sternThruster = value;
                }
            }
        }

        public float ProwThrusterSetter
        {
            get
            {
                lock (_lockGuard)
                {
                    return _prowThrusterSetter;
                }
            }
            set
            {
                lock (_lockGuard)
                {
                    _prowThrusterSetter = value;
                }
            }
        }

        public float SternThrusterSetter
        {
            get
            {
                lock (_lockGuard)
                {
                    return _sternThrusterSetter;
                }
            }
            set
            {
                lock (_lockGuard)
                {
                    _sternThrusterSetter = value;
                }
            }
        }
    }
}