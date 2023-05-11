

namespace BridgeConsole
{
    internal class Pumps
    {
        private readonly ConsoleController _controller;
        private readonly PumpsState _dto;




        public Pumps(ConsoleController controller, PumpsState dto)
        {
            _controller = controller;
            _dto = dto;
        }


        public bool IsPump1Working() => !IsPump2Failed() && _dto.p1On;
        public bool IsPump2Working() => !IsPump2Failed() && _dto.p2On;
        public bool IsPortSparePumpWork() => !IsSparePumpFailed() && _dto.psOn;

        public bool IsPump1Failed() => _dto.p1Fail;
        public bool IsPump2Failed() => _dto.p2Fail;
        public bool IsSparePumpFailed() => _dto.psFail;







        public bool Process(string parameter)
        {
            switch (parameter)
            {
                case "p1On": break;
                case "p2On": break;
                case "psOn": break;
                case "s1On": break;
                case "s2On": break;
                case "ssOn": break;
                case "p1Off": break;
                case "p2Off": break;
                case "psOff": break;
                case "s1Off": break;
                case "s2Off": break;
                case "ssOff": break;

                case "PumpControlPort": break;
                case "PumpControlSync": break;
                case "PumpControlStbd": break;
                default:
                    return false;
            }
            return true;
        }
    }
}