using System;
using System.IO;
using NLog;

namespace ResourceCollection
{
    internal class ResourceLoader
    {
        private static ResourceLoader loader;
        private readonly string InitRootDir;
        private readonly ILogger logger;

        private ResourceLoader()
        {
            InitRootDir = Directory.GetCurrentDirectory();
            logger = LogManager.GetLogger("Resources");
        }

        public static ResourceLoader Get()
        {
            if (loader == null)
                loader = new ResourceLoader();
            return loader;
        }

        public byte[] LoadImageResoureFile(string Name)
        {
            byte[] Result = null;
            var FilePath = Path.Combine(ImageDir(), Name);
            try
            {
                Result = File.ReadAllBytes(FilePath);
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
            }
            return Result;
        }

        public string ImageDir()
        {
            return Path.Combine(RootDir(), "Images");
        }

        public string RootDir()
        {
            return InitRootDir;
        }
    }
}