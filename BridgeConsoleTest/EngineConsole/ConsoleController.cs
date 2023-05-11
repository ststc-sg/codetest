using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;


namespace BridgeConsole
{

    internal class ConsoleController : IDisposable
    {
        public readonly SharedState _commonState;

        private readonly ConsoleDto _screenState;
        public readonly DeadMan DeadMan;
        public readonly EngineInfo ProwEngine;
        public readonly Pumps Pumps;
        public readonly EngineInfo StbdEngine;

        private FlagStorageTemplate<SFailureEnum> _failures;
        public Timer _timer;
        public ShaftInfo Shaft1;
        public ShaftInfo Shaft2;
        public TillerControl tillerControl;

        public EEngineControlSource CurrentControlSource { get; set; } = EEngineControlSource.ENoEnfineControlInfo;
        public bool IsEcrControlRequested = false;
        public bool IsBridgeControlRequested = false;

        public bool IsEcrWantToControlFromBridge;
        public bool IsEcrWantToControlFromEcr;
        private int _nfuControlRequest;


        public ConsoleController(SharedState commonState)
        {
            _screenState = new ConsoleDto(commonState);
            DeadMan = new DeadMan(this);
            ProwEngine = new EngineInfo(this, 1);
            StbdEngine = new EngineInfo(this, 2);

            Shaft1 = new ShaftInfo(this, 1);
            Shaft2 = new ShaftInfo(this, 2);
            Pumps = new Pumps(this, _screenState.pumps);
            tillerControl = new TillerControl(this, _screenState.tillerRoom);

            _commonState = commonState;
            _timer = new Timer(OnTimer, this, 0, 500);
        }

        private void OnReload()
        {
            Shaft1?.OnReload();
            Shaft2?.OnReload();
        }

        public double HDG { get; private set; }
        public double MHDG { get; private set; }

        public float BackLight => 128;


        public float ProwThrusterSetter
        {
            get => _commonState.ProwThrusterSetter;
            set
            {
                var modvalue = Math.Min(100, Math.Max(-100, value));
                _commonState.ProwThrusterSetter = modvalue;

                NotifyDataUpdated();
                _commonState.ProwThrusterSetter = modvalue;
            }
        }

        public float SternThrusterSetter
        {
            get => _commonState.SternThrusterSetter;
            set
            {
                var modvalue = Math.Min(100, Math.Max(-100, value));


                NotifyDataUpdated();
                _commonState.SternThrusterSetter = modvalue;
            }
        }



        /// <summary>
        /// Don't know how this workes
        /// </summary>
        public int NfuControlRequest
        {
            get => _nfuControlRequest;
            set => _nfuControlRequest = value;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }













        public void NotifyDataUpdated()
        {

        }

        public float Log()
        {
            return _screenState.log;
        }




        public void EmergencyStopPress()
        {
            ProwEngine.ForceEmergencyStop();
            StbdEngine.ForceEmergencyStop();
        }

        public void EmergencyStopRelease()
        {
            ProwEngine.ReleaseEmergencyStop();
            StbdEngine.ReleaseEmergencyStop();
        }


        public byte[] HandleHttpRequest(string collection, IDictionary<string, string> evt)
        {
            var command = string.Empty;
            if (evt?.TryGetValue("command", out command) == true)
            {
                evt.TryGetValue("parameter", out var parameter);
                evt.TryGetValue("outputTag", out var outputTag);
                HandleCommand(command, parameter, outputTag);
            }

            LastActionsBeforeSerialize();
            var result = JsonConvert.SerializeObject(_screenState);
            return Encoding.UTF8.GetBytes(result);
        }


        private void LastActionsBeforeSerialize()
        {
            var timetooBlinkLamp = DateTime.UtcNow.Second % 2 == 0;
            var eng = _screenState.engine;
            eng.lengine = ProwEngine.EngineSetPosition;
            eng.rengine = StbdEngine.EngineSetPosition;

            eng.pfull = ManualBlink(ProwEngine.TelegraphSignalMatch(ETelegraphPosition.AheadFull), timetooBlinkLamp);
            eng.phalf = ManualBlink(ProwEngine.TelegraphSignalMatch(ETelegraphPosition.AheadHalf), timetooBlinkLamp);
            eng.pslow = ManualBlink(ProwEngine.TelegraphSignalMatch(ETelegraphPosition.AheadSlow), timetooBlinkLamp);
            eng.pdslow = ManualBlink(ProwEngine.TelegraphSignalMatch(ETelegraphPosition.AheadDeadSlow),
                timetooBlinkLamp);
            eng.pstop = ManualBlink(ProwEngine.TelegraphSignalMatch(ETelegraphPosition.Stop), timetooBlinkLamp);
            eng.prdslow = ManualBlink(ProwEngine.TelegraphSignalMatch(ETelegraphPosition.AsternDeadSlow),
                timetooBlinkLamp);
            eng.prslow = ManualBlink(ProwEngine.TelegraphSignalMatch(ETelegraphPosition.AsternSlow), timetooBlinkLamp);
            eng.prhalf = ManualBlink(ProwEngine.TelegraphSignalMatch(ETelegraphPosition.AsternHalf), timetooBlinkLamp);
            eng.prfull = ManualBlink(ProwEngine.TelegraphSignalMatch(ETelegraphPosition.AsternFull), timetooBlinkLamp);

            eng.sfull = ManualBlink(StbdEngine.TelegraphSignalMatch(ETelegraphPosition.AheadFull), timetooBlinkLamp);
            eng.shalf = ManualBlink(StbdEngine.TelegraphSignalMatch(ETelegraphPosition.AheadHalf), timetooBlinkLamp);
            eng.sslow = ManualBlink(StbdEngine.TelegraphSignalMatch(ETelegraphPosition.AheadSlow), timetooBlinkLamp);
            eng.sdslow = ManualBlink(StbdEngine.TelegraphSignalMatch(ETelegraphPosition.AheadDeadSlow),
                timetooBlinkLamp);
            eng.sstop = ManualBlink(StbdEngine.TelegraphSignalMatch(ETelegraphPosition.Stop), timetooBlinkLamp);
            eng.srdslow = ManualBlink(StbdEngine.TelegraphSignalMatch(ETelegraphPosition.AsternDeadSlow),
                timetooBlinkLamp);
            eng.srslow = ManualBlink(StbdEngine.TelegraphSignalMatch(ETelegraphPosition.AsternSlow), timetooBlinkLamp);
            eng.srhalf = ManualBlink(StbdEngine.TelegraphSignalMatch(ETelegraphPosition.AsternHalf), timetooBlinkLamp);
            eng.srfull = ManualBlink(StbdEngine.TelegraphSignalMatch(ETelegraphPosition.AsternFull), timetooBlinkLamp);

            eng.psea = true;
            eng.pstandy = false;

            eng.ssea = true;
            eng.sstandy = false;

            ProwEngine.WriteTo(_screenState.port);
            StbdEngine.WriteTo(_screenState.stbd);
            Shaft1.WriteTo(_screenState.port);
            Shaft2.WriteTo(_screenState.stbd);

            _screenState.port.bridge = BridgeLampState(timetooBlinkLamp);
            _screenState.port.ecr = EcrLampState(timetooBlinkLamp);
            _screenState.port.pitchlocal = _screenState.stbd.pitchlocal = CurrentControlSource == EEngineControlSource.ELocalControl;
        }

        bool BridgeLampState(bool timetooBlinkLamp)
        {
            if (CurrentControlSource == EEngineControlSource.EBridgeControl)
                return true;
            if (IsBridgeControlRequested || IsEcrWantToControlFromBridge)
                return timetooBlinkLamp;
            return false;
        }
        bool EcrLampState(bool timetooBlinkLamp)
        {
            if (CurrentControlSource != EEngineControlSource.EBridgeControl)
                return true;
            if (IsEcrControlRequested || IsEcrWantToControlFromEcr)
                return timetooBlinkLamp;
            return false;
        }

        private bool ManualBlink(int telegraphSignalMatch, bool timetooBlinkLamp)
        {
            switch (telegraphSignalMatch)
            {
                case 2:
                    return true;
                case 1:
                    return timetooBlinkLamp;
                default:
                    return false;
            }
        }

        private void HandleCommand(string command, string parameter, string outputTag)
        {
            switch (command)
            {
                case "navigate":
                    _screenState.active = parameter;
                    break;
                case "daynightswitch":
                    _screenState.ThemeStyle = _screenState.ThemeStyle == eThemeStyle.LIGHT
                        ? eThemeStyle.DARK
                        : eThemeStyle.LIGHT;
                    break;
                case "buttonclick":
                    OnButton(parameter, outputTag);
                    break;
                case "pressToggle":
                    OnToggle(parameter, true, outputTag);
                    break;
                case "releaseToggle":
                    OnToggle(parameter, false, outputTag);
                    break;
                case "lengine":
                    ProwEngine.EngineSetPosition = float.Parse(parameter, CultureInfo.InvariantCulture);
                    break;
                case "rengine":
                    StbdEngine.EngineSetPosition = float.Parse(parameter, CultureInfo.InvariantCulture);
                    break;
            }
        }

        private bool BothSideActions(string id, bool state)
        {
            switch (id)
            {
                case "ecr":
                    if (state)
                        PushEcrControl();
                    else
                        PopEcrControl();
                    return true;
                case "bridge":
                    if (state)
                        PushBridgeControl();
                    else
                        PopBridgeControl();
                    return true;
            }

            return false;

        }
        private void OnToggle(string id, bool state, string outputTag)
        {
            if (outputTag == "port" && (BothSideActions(id, state) || Shaft1.OnToggle(id, state) || ProwEngine.OnToggle(id, state)))
                return;
            if (outputTag == "stbd" && (BothSideActions(id, state) || Shaft2.OnToggle(id, state) || StbdEngine.OnToggle(id, state)))
                return;
            if (outputTag == "tillerRoom" && tillerControl.OnToggle(id, state))
                return;
            switch (id)
            {
                case "ProwThrusterLeft":
                    _screenState.ProwThrusterLeft = state;
                    break;
                case "ProwThrusterRight":
                    _screenState.ProwThrusterRight = state;
                    break;
                case "SternThrusterLeft":
                    _screenState.SternThrusterLeft = state;
                    break;
                case "SternThrusterRight":
                    _screenState.SternThrusterRight = state;
                    break;
            }
        }

        private void OnButton(string parameter, string outputTag)
        {
            if (Pumps.Process(parameter))
                return;
            if (TelegraphButtons(parameter))
                return;
            if (outputTag == "tillerRoom" && tillerControl.OnButton(parameter))
                return;
            if (outputTag == "port" && (ProwEngine.OnButton(parameter) || Shaft1.OnButton(parameter)))
                return;
            if (outputTag == "stbd" && (StbdEngine.OnButton(parameter) || Shaft2.OnButton(parameter)))
                return;



            switch (parameter)
            {
                case "DG1On":

                    break;
                case "DG1Off":

                    break;
                case "DG2On":

                    break;
                case "DG2Off":

                    break;
                case "EDGOn":

                    break;
                case "EDGOff":

                    break;
                case "ShoreLinkOn":

                    break;
                case "ShoreLinkOff":

                    break;
            }
        }

        private bool TelegraphButtons(string parameter)
        {
            switch (parameter)
            {
                case "pfull":
                    ProwEngine.SendTelegraphCommand(ETelegraphPosition.AheadFull);
                    return true;
                case "phalf":
                    ProwEngine.SendTelegraphCommand(ETelegraphPosition.AheadHalf);
                    return true;
                case "pslow":
                    ProwEngine.SendTelegraphCommand(ETelegraphPosition.AheadSlow);
                    return true;
                case "pdslow":
                    ProwEngine.SendTelegraphCommand(ETelegraphPosition.AheadDeadSlow);
                    return true;
                case "pstop":
                    ProwEngine.SendTelegraphCommand(ETelegraphPosition.Stop);
                    return true;
                case "prdslow":
                    ProwEngine.SendTelegraphCommand(ETelegraphPosition.AsternDeadSlow);
                    return true;
                case "prslow":
                    ProwEngine.SendTelegraphCommand(ETelegraphPosition.AsternSlow);
                    return true;
                case "prhalf":
                    ProwEngine.SendTelegraphCommand(ETelegraphPosition.AsternHalf);
                    return true;
                case "prfull":
                    ProwEngine.SendTelegraphCommand(ETelegraphPosition.AsternFull);
                    return true;

                case "sfull":
                    StbdEngine.SendTelegraphCommand(ETelegraphPosition.AheadFull);
                    return true;
                case "shalf":
                    StbdEngine.SendTelegraphCommand(ETelegraphPosition.AheadHalf);
                    return true;
                case "sslow":
                    StbdEngine.SendTelegraphCommand(ETelegraphPosition.AheadSlow);
                    return true;
                case "sdslow":
                    StbdEngine.SendTelegraphCommand(ETelegraphPosition.AheadDeadSlow);
                    return true;
                case "sstop":
                    StbdEngine.SendTelegraphCommand(ETelegraphPosition.Stop);
                    return true;
                case "srdslow":
                    StbdEngine.SendTelegraphCommand(ETelegraphPosition.AsternDeadSlow);
                    return true;
                case "srslow":
                    StbdEngine.SendTelegraphCommand(ETelegraphPosition.AsternSlow);
                    return true;
                case "srhalf":
                    StbdEngine.SendTelegraphCommand(ETelegraphPosition.AsternHalf);
                    return true;
                case "srfull":
                    StbdEngine.SendTelegraphCommand(ETelegraphPosition.AsternFull);
                    return true;
            }

            return false;
        }

        private void OnTimer(object state)
        {
            //for test
            ProwEngine.TestIncrement();
            StbdEngine.TestIncrement();

            var increment = 5;
            if (_screenState.ProwThrusterLeft)
                ProwThrusterSetter -= increment;
            else if (_screenState.ProwThrusterRight)
                ProwThrusterSetter += increment;

            if (_screenState.SternThrusterLeft)
                SternThrusterSetter -= increment;
            else if (_screenState.SternThrusterRight)
                SternThrusterSetter += increment;
        }

        public void PushEcrControl()
        {
            IsEcrControlRequested = true;

        }

        public void PopEcrControl()
        {
            IsEcrControlRequested = false;

        }

        public void PushBridgeControl()
        {
            IsBridgeControlRequested = true;

        }

        public void PopBridgeControl()
        {
            IsBridgeControlRequested = false;

        }
    }
}