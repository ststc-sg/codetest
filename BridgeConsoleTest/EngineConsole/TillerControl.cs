

namespace BridgeConsole
{
    internal class TillerControl
    {
        private readonly ConsoleController _consoleController;
        private readonly TillerRoom _screenStateTillerRoom;

        public TillerControl(ConsoleController consoleController, TillerRoom screenStateTillerRoom)
        {
            _consoleController = consoleController;
            _screenStateTillerRoom = screenStateTillerRoom;
        }

        public bool OnToggle(string id, bool state)
        {
            switch (id)
            {
                case "primaryRudderPort":
                    _screenStateTillerRoom.PrimaryPumpMoveDirectionSettter = state ? ERidderMoveDirection.ERotatePort : ERidderMoveDirection.EIdle;

                    break;
                case "primaryRudderStbd":
                    _screenStateTillerRoom.PrimaryPumpMoveDirectionSettter = state ? ERidderMoveDirection.ERotateStbd : ERidderMoveDirection.EIdle;

                    break;
                case "secondaryRudderPort":
                    _screenStateTillerRoom.SecondaryPumpMoveDirectionSettter = state ? ERidderMoveDirection.ERotatePort : ERidderMoveDirection.EIdle;

                    break;
                case "secondaryRudderStbd":
                    _screenStateTillerRoom.SecondaryPumpMoveDirectionSettter = state ? ERidderMoveDirection.ERotateStbd : ERidderMoveDirection.EIdle;

                    break;
            }
            return false;
        }

        public bool OnButton(string parameter)
        {
            switch (parameter)
            {
                case "IsEmergencyControl":
                    break;
                case "emcyRudderPumpOn":
                    break;
                case "emcyRudderPumpOff":
                    break;
            }
            return false;
        }
    }
}