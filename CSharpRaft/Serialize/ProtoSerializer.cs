using ProtoBuf;
using System.IO;

namespace CSharpRaft.Serialize
{
    public class ProtoSerializer : ISerializer
    {
        public void Serialize<T>(Stream destination, T instance)
        {
            Serializer.Serialize<T>(destination, instance);
        }

        public T Deserialize<T>(Stream source)
        {
            return Deserialize<T>(source);
        }
    }
}
