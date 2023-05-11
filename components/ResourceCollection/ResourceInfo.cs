using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using MimeTypeMap;
using System.Threading.Tasks;

namespace ResourceCollection
{
    public interface IResourceInfo
    {
        byte [] Resource { get; }
        bool IsStatic { get; }
        string Mime { get; }
    }
    public class ResourceInfo : IResourceInfo
    {
        public bool IsStatic { get; internal set; }
        public string Mime { get; }
        public byte[] Resource{ get; }

        public ResourceInfo(byte[] resource, string mime)
        {
            Resource = resource;
            Mime = mime;
        }

        public static string MimeByExtension(string extension)
        {
            return MimeTypeMap.List.MimeTypeMap.GetMimeType(extension).FirstOrDefault();
        }
        public static string MimeByName(string name)
        {
            var extension = Path.GetExtension(name);
            var mime = MimeByExtension(extension);
            return mime;
        }

        
    }

    public interface IResourceSource
    {
        Task<byte []> TryLoad(string filepath);
        event FileSystemEventHandler OnResourceChanged ;
    }
}