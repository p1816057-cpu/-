using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AudioConverter.Data
{
    /// <summary>
    /// 轻量 JSON 持久化，基于 .NET Framework 自带 DataContractJsonSerializer，
    /// 不引入第三方库，保证离线与 Win7 兼容。
    /// </summary>
    public static class JsonFile
    {
        public static T Load<T>(string path) where T : class
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    return serializer.ReadObject(stream) as T;
                }
            }
            catch
            {
                return null;
            }
        }

        public static void Save<T>(string path, T value)
        {
            if (string.IsNullOrEmpty(path) || value == null)
            {
                return;
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var serializer = new DataContractJsonSerializer(typeof(T));
            string json;
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, value);
                json = Encoding.UTF8.GetString(stream.ToArray());
            }

            File.WriteAllText(path, json, new UTF8Encoding(false));
        }
    }
}
