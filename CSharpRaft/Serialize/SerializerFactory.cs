namespace CSharpRaft.Serialize
{
    /// <summary>
    /// SerializerFactory sets or get a object serializer
    /// </summary>
    public class SerializerFactory
    {
        private static ISerializer defaultSerializer = new JsonSerializer();

        static ISerializer serializer;

        /// <summary>
        /// Gets object Serilizer
        /// </summary>
        /// <returns></returns>
        public static ISerializer GetSerilizer()
        {
            if (serializer == null)
            {
                return defaultSerializer;
            }

            return serializer;
        }

        /// <summary>
        /// Sets object Serilizer
        /// </summary>
        /// <param name="ser"></param>
        public static void SetSerilizer(ISerializer ser)
        {
            serializer = ser;
        }
    }
}
