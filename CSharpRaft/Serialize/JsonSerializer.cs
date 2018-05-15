using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace CSharpRaft.Serialize
{
    class JsonSerializer : ISerializer
    {
        public void Serialize<T>(Stream destination, T instance)
        {
            string ser = JsonConvert.SerializeObject(instance);
            byte[] data = UTF8Encoding.UTF8.GetBytes(ser);
            destination.Write(data,0, data.Length);
        }

        public T Deserialize<T>(Stream source)
        {
            byte[] data = new byte[source.Length];
            source.Read(data,0, data.Length);

            string ser = UTF8Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(ser);
        }
    }
}
