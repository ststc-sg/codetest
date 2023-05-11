using ProtoBuf;
using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
namespace BridgeConsole
{
    //special class to store common data
    public enum eThemeStyle
    {
        LIGHT,
        DARK
    }

    public enum SFailureEnum
    {
        ME1 = 0, // main engine 1
        ME2, // main engine 2
        RAS, // remote automatic control
        Steer, // steering
        DopLog,
        Log,
        Gyro,
        GPS,
        VPPHydraulicFail,
        SarpBroken,
        SensorsSystematicError,
        TugControl,
        RadarAntenna,
        AISBroken,
        EchoSounderBroken,
        FireAlarm,
        RadarPowerFailure,
        Glonass,
        AllPowerDown,
        IncreasedAntennaNoise,
        RudderFatal,
        AbandonShipAlarm,

        PortRudderPump1Alarm,
        PortRudderPump2Alarm,
        PortSparePumpsAlarm,

        StbdRudderPump1Alarm,
        StbdRudderPump2Alarm,
        StbdSparePumpsAlarm,

        SingleCompartmentFlooding,

        SonarBroken,
        TrawlSonarBroken,
        MultibeamSonarBroken
    }

    public static class HackEnumConvert<TEnumType> where TEnumType : struct, Enum
    {
        private static readonly Func<TEnumType, int> Wrapper;

        public static int ToInt(TEnumType enu)
        {
            return Wrapper(enu);
        }

        static HackEnumConvert()
        {
            var p = Expression.Parameter(typeof(TEnumType), null);
            var c = Expression.ConvertChecked(p, typeof(int));
            Wrapper = Expression.Lambda<Func<TEnumType, int>>(c, p).Compile();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [ProtoBuf.ProtoContract]
    public struct FlagStorageTemplate<T> where T : struct, Enum
    {
        [ProtoMember(1, IsRequired = false)] public UInt32 Dummy;

        public bool this[T i]
        {
            get
            {
                var pos = HackEnumConvert<T>.ToInt(i);
                return ((Dummy & (1 << pos)) != 0);
            }
            set
            {
                var pos = HackEnumConvert<T>.ToInt(i);
                if (value)
                    Dummy |= (UInt32)(1 << pos);
                else
                    Dummy &= (UInt32)~(1 << pos);
            }
        }

        public void Switch(T i) => this[i] = !this[i];
    }

    enum EEngineControlSource
    {
        ENoEnfineControlInfo = 0,
        ELocalControl = 1,
        EECRControl = 2,
        EBridgeControl = 3,
    }
}