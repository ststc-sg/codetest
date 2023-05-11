using System;

namespace BridgeConsole
{
    enum ETelegraphPosition
    {
        Stop = 0,
        AheadFull = 5,
        AheadHalf = 4,
        AheadSlow = 3,
        AheadDeadSlow = 2,
        ForwardStop = 1,
        BackwardStop = -1,
        AsternDeadSlow = -2,
        AsternSlow = -3,
        AsternHalf = -4,
        AsternFull = -5,
    }
    enum EControlType
    {
        ENoInfo = 0,
        EConstant = 1,
        ECombinat = 2,
    }
    internal class EngineInfo
    {
        private readonly ConsoleController _parent;
        private readonly uint _id;
        public float LoadPercent { get; private set; }
        public float RpmPercent { get; private set; }
        public float Rpm { get; private set; }
        public uint AirPressurePercent { get; private set; }
        private float _engineSetPosition;
        private ETelegraphPosition TelegraphState;
        private ETelegraphPosition TelegraphCommand;

        public void TestIncrement()
        {
            if(LoadPercent>=100)
                LoadPercent=0;
            else
                LoadPercent=MathF.Min(100, LoadPercent+1);
        }
        public float EngineSetPosition
        {
            get => _engineSetPosition;
            set
            {
                if (Math.Abs(_engineSetPosition - value) > 1)
                {
                    _engineSetPosition = value;
                    SendEngineCommandUpdate();
                }
            }
        }

        public bool IsReducePowerAlarm { get; set; }
        public bool IsShutDownAlarm { get; set; }
        public bool IsSlowDownAlarm { get; set; }

        /// <summary>
        /// Flag from hardware stop button
        /// </summary>
        bool ForceEmergencyStopFlag;

        //Is software emergency stop button pressed
        bool IsEmergencyStopRequested
        {
            get => _isEmergencyStopRequested || ForceEmergencyStopFlag;
            set => _isEmergencyStopRequested = value;
        }

        public bool IsEmergencyStopHappen { get; private set; }

        private bool _isEmergencyStopRequested;

        public EControlType ControlTypeRequest { get; private set; }
        public EControlType ControlTypeState { get; private set; }


        public EngineInfo(ConsoleController parent, uint Id)
        {
            _parent = parent;
            _id = Id;
        }






        public void SetControlMode(EControlType controlType)
        {
            ControlTypeRequest = controlType;
            SendEngineCommandUpdate();
        }
        public void SendTelegraphCommand(ETelegraphPosition command)
        {
            TelegraphCommand = command;
            SendEngineCommandUpdate();
        }

        public int TelegraphSignalMatch(ETelegraphPosition checkstate)
        {
            if (TelegraphCommand == checkstate)
                return TelegraphState == checkstate ? 2 : 1;
            return 0;
        }
        public bool OnButton(string parameter)
        {
            switch (parameter)
            {
                case "isConstant":
                case "isCombinat":
                    return true;
                case "emergencystop":
                    EmergencyStopSwitch();
                    return true;
            }

            return false;
        }
        public bool OnToggle(string id, bool state)
        {
            switch (id)
            {
                case "isConstant":
                    SetControlMode(state ? EControlType.EConstant : EControlType.ENoInfo);
                    return true;
                case "isCombinat":
                    SetControlMode(state ? EControlType.ECombinat : EControlType.ENoInfo);
                    return true;
            }
            return false;
        }

        public void EmergencyStopSwitch()
        {
            if (IsEmergencyStopRequested || IsEmergencyStopHappen || ForceEmergencyStopFlag) //if fired somehow try reset
                IsEmergencyStopRequested = false;
            else
            {
                IsEmergencyStopRequested = true;
            }
        }


        public void WriteTo(Side state)
        {
            var islampBlink = DateTime.UtcNow.Second % 2 == 0;
            state.bridge = _parent._commonState.IsBridgeControl;
            state.isConstant = ControlTypeState == EControlType.EConstant || (ControlTypeRequest == EControlType.EConstant && islampBlink);
            state.isCombinat = ControlTypeState == EControlType.ECombinat || (ControlTypeRequest == EControlType.ECombinat && islampBlink);
            state.meload=LoadPercent;
            state.emergencystop = IsEmergencyStopHappen || IsEmergencyStopRequested;
        }



        public void ForceEmergencyStop()
        {
            IsEmergencyStopRequested = ForceEmergencyStopFlag = true;
            SendEngineCommandUpdate();
        }

        public void ReleaseEmergencyStop()
        {
            IsEmergencyStopRequested = ForceEmergencyStopFlag = false;
            SendEngineCommandUpdate();
        }



        public void EmergencyStopPush()
        {
            IsEmergencyStopRequested = true;
            SendEngineCommandUpdate();
        }

        public void OnEmergencyStopPop()
        {
            IsEmergencyStopRequested = false;
            SendEngineCommandUpdate();
        }

        void SendEngineCommandUpdate()
        {

        }
    }
}