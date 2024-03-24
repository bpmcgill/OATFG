using YamlDotNet.Serialization;

namespace OATFG.Core.Utilities
{
    public static class YamlToJson
    {
        public static string ConvertYamlToJsonFromYamlString(string yaml)
        {
            var deserializer = new Deserializer();
            var yamlObject = deserializer.Deserialize(new StringReader(yaml));
            var serializer = new SerializerBuilder()
                .JsonCompatible()
                .Build();
            return serializer.Serialize(yamlObject);
        }

        public static string ConvertYamlToJsonFromYamlFile(string yamlFilePath)
        {
            try
            {
                string yaml = File.ReadAllText(yamlFilePath);

                var deserializer = new Deserializer();
                var yamlObject = deserializer.Deserialize(new StreamReader(yamlFilePath));
                var serializer = new SerializerBuilder()
                    .JsonCompatible()
                    .Build();
                return serializer.Serialize(yamlObject);
            }
            catch (Exception)
            {
                //TODO: Add logging & Exception handling
                throw;
            }
        }
    }
}
