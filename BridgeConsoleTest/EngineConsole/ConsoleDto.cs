using System;

namespace BridgeConsole
{
    [Serializable]
    public class Side
    {
        public float meload;
        public float merpm;
        public float mepitch;
        public float meshaftrpm;
        public bool nfu = false;
        public bool fu;

        public bool bridge;
        public bool ecr;

        public bool emergencystop;

        public bool pitchlocal;
        public bool emergencypitch;

        public bool pitchehead;
        public bool pitchastern;

        public bool isConstant;
        public bool isCombinat;
        public bool clutchin;
        public bool clutchout;
    }
    [Serializable]
    public class PumpsState
    {
        public bool p1On;
        public bool p2On;
        public bool psOn;

        public bool s1On;
        public bool s2On;
        public bool ssOn;


        public bool p1Off => !p1On;
        public bool p2Off => !p2On;
        public bool psOff => !psOn;

        public bool s1Off => !s1On;
        public bool s2Off => !s2On;
        public bool ssOff => !ssOn;


        public bool p1Fail;
        public bool p2Fail;
        public bool psFail;

        public bool s1Fail;
        public bool s2Fail;
        public bool ssFail;


        public bool PumpControlPort;
        public bool PumpControlSync;
        public bool PumpControlStbd;
    }
    [global::ProtoBuf.ProtoContract()]
    public enum ERidderMoveDirection
    {
        EIdle = 0,
        ERotatePort = 1,
        ERotateStbd = 2,
    }
    [Serializable]
    public class TillerRoom
    {
        public bool IsEmergencyControl;
        public bool emcyRudderPumpOn;
        public bool emcyRudderPumpOff => !emcyRudderPumpOn;

        public ERidderMoveDirection PrimaryPumpMoveDirection;
        public ERidderMoveDirection SecondaryPumpMoveDirection;
        public bool primaryRudderPort => PrimaryPumpMoveDirection == ERidderMoveDirection.ERotatePort;
        public bool primaryRudderStbd => PrimaryPumpMoveDirection == ERidderMoveDirection.ERotateStbd;
        public bool secondaryRudderPort => SecondaryPumpMoveDirection == ERidderMoveDirection.ERotatePort;
        public bool secondaryRudderStbd => SecondaryPumpMoveDirection == ERidderMoveDirection.ERotateStbd;

        public ERidderMoveDirection PrimaryPumpMoveDirectionSettter;
        public ERidderMoveDirection SecondaryPumpMoveDirectionSettter;
    }

    [Serializable]
    public class EngineState
    {
        public float lengine; //prow or single engine percent
        public float rengine; //prow or single engine percent

        public bool pfull = true;
        public bool phalf = true;
        public bool pslow = true;
        public bool pdslow = true;
        public bool pstop = true;
        public bool prdslow = true;
        public bool prslow = true;
        public bool prhalf = true;
        public bool prfull = true;

        public bool psea = true;
        public bool pstandy = false;
        public bool pwe => !psea;

        public bool sfull = true;
        public bool shalf = true;
        public bool sslow = true;
        public bool sdslow = true;
        public bool sstop = true;
        public bool srdslow = true;
        public bool srslow = true;
        public bool srhalf = true;
        public bool srfull = true;

        public bool ssea = true;
        public bool sstandy = false;
        public bool swe => !ssea;


    }

    [Serializable]
    class ConsoleDto
    {
        private readonly SharedState _commonState;
        public string active; //currentpage
        public eThemeStyle ThemeStyle;
        public bool DG1On;
        public bool DG1Off;
        public bool DG2On;
        public bool DG2Off;
        public bool EDGOn;
        public bool EDGOff;
        public bool ShoreLinkOn;
        public bool ShoreLinkOff;
        public bool deadmanOn;
        public bool deadmanAlarm;
        public float log;
        public Side port = new Side();
        public Side stbd = new Side();
        public PumpsState pumps = new PumpsState();
        public EngineState engine = new EngineState();
        public TillerRoom tillerRoom = new TillerRoom();
        public bool ProwThrusterLeft;
        public bool ProwThrusterRight;
        public bool SternThrusterLeft;
        public bool SternThrusterRight;
        public int ProwThruster => (int)_commonState.ProwThruster;
        public int SternThruster => (int)_commonState.SternThruster;
        public float ProwThrusterText => (float)Math.Round(_commonState.ProwThruster, 1);
        public float SternThrusterText => (float)Math.Round(_commonState.SternThruster, 1);
        public ConsoleDto(SharedState commonState)
        {
            _commonState = commonState;
            active = "engine.html";
        }
    }
}