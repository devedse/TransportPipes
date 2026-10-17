using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesDatabase.Helpers
{
    /// <summary>
    /// This serializer adds a class name to the serialized strings to ensure it can be deserialized to the right class
    /// </summary>
    public class TypedJsvStringSerializer : IStringSerializer
    {
        private const string SplitTextInDb = "<TypeSplit>";
        private readonly JsvStringSerializer jsv = new JsvStringSerializer();

        public object DeserializeFromString(string serializedText, Type type)
        {
            if (type.IsEnum)
            {
                var retval = jsv.DeserializeFromString(serializedText, type);
                return retval;
            }
            else
            {
                var splitted = serializedText.Split(new string[] { SplitTextInDb }, StringSplitOptions.RemoveEmptyEntries);

                if (splitted.Length != 2)
                {
                    return jsv.DeserializeFromString(serializedText, type);
                }
                else
                {
                    var realType = (Type)jsv.DeserializeFromString(splitted[0], typeof(Type));
                    var retval = jsv.DeserializeFromString(splitted[1], realType);
                    return retval;
                }
            }
        }

        public To DeserializeFromString<To>(string serializedText)
        {
            if (typeof(To).IsEnum)
            {
                return jsv.DeserializeFromString<To>(serializedText);
            }
            else
            {
                var splitted = serializedText.Split(new string[] { SplitTextInDb }, StringSplitOptions.RemoveEmptyEntries);

                var realType = (Type)jsv.DeserializeFromString(splitted[0], typeof(Type));
                var deserialized = jsv.DeserializeFromString(splitted[1], realType);

                var retval = (To)deserialized;
                return retval;
            }
        }

        public string SerializeToString<TFrom>(TFrom from)
        {
            var serializedItem = jsv.SerializeToString(from);

            //If it is not an enum also serialize the type with it
            if (from.GetType().IsEnum)
            {
                return serializedItem;
            }
            else
            {
                var serializedType = jsv.SerializeToString(from.GetType()).Replace("\"", "");
                return serializedType + SplitTextInDb + serializedItem;
            }
        }
    }
}
