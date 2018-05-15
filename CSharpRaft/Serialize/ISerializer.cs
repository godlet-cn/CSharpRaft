using System.IO;

namespace CSharpRaft.Serialize
{
    /// <summary>
    /// ISerializer is a interface to serialize object
    /// </summary>
    public interface ISerializer
    {
        void Serialize<T>(Stream destination, T instance);

        T Deserialize<T>(Stream source);
    }
}
