namespace OmniSerializer
{
    /// <summary>
    /// Helper class for <see cref="Serializer"/>
    /// </summary>
    public static class SerializerTestHelper
    {
        /// <summary>
        /// Empty the TypeData cache
        /// </summary>
        public static void ClearTypeDataMap()
        {
            Serializer serializer = new Serializer();
            serializer.ClearTypeDataMap();
        }
    }
}
