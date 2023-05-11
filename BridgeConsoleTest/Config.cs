using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace BridgeConsole
{
    [DataContract(Name = "WindowInfo", Namespace = "")]
    internal class WindowInfo
    {
        [DataMember]
        public bool IsAutoFit;
        [DataMember]
        public int AutoDisplayIndex;
        [DataMember]
        public long Left;
        [DataMember]
        public long Top;
        [DataMember]
        public long Width;
        [DataMember]
        public long Heigth;
        [DataMember]
        public bool InvertXAxis;
        [DataMember]
        public bool InvertYAxis;
        [DataMember]
        public bool SwapAxis;
        [DataMember(IsRequired = false)]
        public bool IsEnabled;


        public void TransformRatioToWindowPosition(ref double x, ref double y)
        {
            x *= Width;
            y *= Heigth;
        }
    }
    [DataContract(Name = "EmbededWindowInfo", Namespace = "")]
    internal class EmbededWindowInfo : WindowInfo
    {
        [DataMember(IsRequired = false)]
        public string EmbededProgramPath;
        [DataMember(IsRequired = false)]
        public string WorkingDirectory;
        [DataMember(IsRequired = false)]
        public string Arguments;

        public static EmbededWindowInfo Default()
        {
            return new EmbededWindowInfo()
            {

            };
        }
    }


    [DataContract(Name = "Config", Namespace = "")]
    internal class Config
    {

        [DataMember]
        public WindowInfo WebBridge;


        [DataMember] public float NetworkStartupDelay = 0;
        [DataMember(IsRequired = false)]
        public uint wwwPort;


        protected Config()
        {
            WebBridge = new WindowInfo
            {
                IsAutoFit = false,
                Width = 800,
                Heigth = 600,
            };

        }
        public static Config LoadOrCreate(string configXml)
        {
            var result = Load(configXml);
            if (result == null)
            {
                result = new Config();
                result.Save(configXml);
            }
            else
            {
#if DEBUG
                result.Save(configXml);
#endif
            }
            return result;
        }



        public bool Save(string path)
        {
            try
            {
                var ser =
                    new DataContractSerializer(typeof(Config));
                var settings = new XmlWriterSettings()
                {
                    Indent = true,
                    IndentChars = "\t"
                };
                using (var writer = XmlWriter.Create(path, settings))
                {
                    ser.WriteObject(writer, this);
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public static Config Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;
                var ser = new DataContractSerializer(typeof(Config));
                using (var reader = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    return (Config)ser.ReadObject(reader);
                }

            }
            catch (Exception e)
            {

            }
            return null;
        }

        public int WwwhubPort() => (int)(wwwPort > 0 ? wwwPort : 8993);
    }


}