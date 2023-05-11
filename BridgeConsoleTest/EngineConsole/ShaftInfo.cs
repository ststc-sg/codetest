
using System;

namespace BridgeConsole
{
    [global::ProtoBuf.ProtoContract()]
    public enum ENfuFu
    {
        ENone = 0,
        EFu = -1,
        ENfu = 1,
    }
    internal class ShaftInfo
    {
        private readonly ConsoleController _parent;
        private readonly uint _id;
        private bool isPitchAheadRequested;
        private bool isPitchAsternRequested;
        private bool IsPitchAhead;
        private bool IsPitchAstern;

        //clutch
        public bool IsClutchInRequested { get; private set; }
        public bool IsClutchOutRequested { get; private set; }
        public bool IsClutchIn { get; set; }

        public ShaftInfo(ConsoleController parent, uint Id)
        {
            _parent = parent;
            _id = Id;
        }

        public void OnReload()
        {
            IsEmergencyPitchControlRequested = false;
        }


        public int PitchPercent { get; private set; }
        public float Pitch { get; private set; }
        public bool IsEmergencyPitchControlRequested { get; private set; }
        public bool IsEmergencyPitchControlState { get; private set; }

        public float SpeedShaftRpm { get; private set; }
        public bool IsAdjustPitchToZeroAlarm { get; private set; }
        public ENfuFu NfuControlState { get; private set; } = ENfuFu.ENone;
        public bool IsReadyToClutch { get; }


        public void SwitchEmergencyPitchControl()
        {
            if (IsEmergencyPitchControlRequested || IsEmergencyPitchControlState)
                IsEmergencyPitchControlRequested = false;
            else
                IsEmergencyPitchControlRequested = true;

        }
        public void SetEmergencyPitchControl(bool b)
        {
            IsEmergencyPitchControlRequested = b;

        }



        public void PitchAhead()
        {
            this.isPitchAheadRequested = true;

        }

        public void PitchZero()
        {
            isPitchAheadRequested = isPitchAsternRequested = false;

        }

        public void PitchAstern()
        {
            this.isPitchAsternRequested = true;

        }



        public bool OnButton(string parameter)
        {
            switch (parameter)
            {
                case "emergencypitch":
                    SwitchEmergencyPitchControl();
                    return true;
                case "pitchlocal":
                    return true;
                case "pitchehead":
                case "pitchastern":
                    return true;    //mast be processed using OnToggle,so ignore simple press
            }

            return false;
        }
        public bool OnToggle(string parameter, bool state)
        {
            switch (parameter)
            {
                case "pitchehead":
                    if (state)
                        PitchAhead();
                    else
                        PitchZero();
                    return true;
                case "pitchastern":
                    if (state)
                        PitchAstern();
                    else
                        PitchZero();
                    return true;

                case "clutchin":
                    if (state)
                        ClutchInRequest();
                    else
                        ClutchInNotRequest();

                    return true;
                case "clutchout":
                    if (state)
                        ClutchOutRequest();
                    else
                        ClutchOutNotRequest();
                    return true;



            }

            return false;
        }

        public void WriteTo(Side state)
        {
            var islampBlink = DateTime.UtcNow.Second % 2 == 0;
            state.nfu = NfuControlState == ENfuFu.ENfu;
            state.fu = NfuControlState == ENfuFu.EFu;
            state.emergencypitch = IsEmergencyPitchControlState || IsEmergencyPitchControlRequested;
            state.pitchehead = IsPitchAhead || (isPitchAheadRequested && IsEmergencyPitchControlRequested);
            state.pitchastern = IsPitchAstern || (isPitchAsternRequested && IsEmergencyPitchControlRequested);
            state.clutchin = IsClutchIn || (IsClutchInRequested && islampBlink);
            state.clutchout = !IsClutchIn || (IsClutchOutRequested && islampBlink);
        }

        public void ClutchInRequest()
        {
            IsClutchInRequested = true;
            SendNotification();
        }



        public void ClutchInNotRequest()
        {
            IsClutchInRequested = false;
            SendNotification();

        }

        public void ClutchOutRequest()
        {
            IsClutchOutRequested = true;
            SendNotification();
        }


        public void ClutchOutNotRequest()
        {
            IsClutchOutRequested = false;
            SendNotification();
        }

        public void SendNotification()
        {

        }

    }
}