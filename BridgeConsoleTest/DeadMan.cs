namespace BridgeConsole
{
    internal class DeadMan
    {
        private readonly ConsoleController _parent;
        public bool IsAlarm;
        public bool IsOn;

        public DeadMan(ConsoleController parent)
        {
            _parent = parent;
        }

        public void ResetAlarm()
        {
            _parent.NotifyDataUpdated();
        }
    }
}