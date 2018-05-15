namespace CSharpRaft.Serialize
{
    public class SerializerFactory
    {
        private static ISerializer defaultSerializer = new JsonSerializer();

        static ISerializer serializer;

        public static ISerializer GetSerilizer()
        {
            if (serializer == null)
            {
                return defaultSerializer;
            }

            return serializer;
        }

        public static void SetSerilizer(ISerializer ser)
        {
            serializer = ser;
        }
    }
}
