using FiveDegrees.Messages.ProcessManager;
using FiveDegrees.Messages.ProcessManager.v2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManager.BackgroundWorker.Extensions
{
    public class MessageDeserializer : ISerializer
    {
        readonly ISerializer _serializer;

        private static ConcurrentDictionary<string, Type> KnownTypes = new ConcurrentDictionary<string, Type>
        {
            ["FiveDegrees.Messages.ProcessManager.StartActivityMsg, FiveDegrees.Messages"] = typeof(StartActivityMsg),
            ["FiveDegrees.Messages.ProcessManager.v2.StartActivityMsgV2, FiveDegrees.Messages"] = typeof(StartActivityMsgV2),
            ["FiveDegrees.Messages.ProcessManager.UpdateActivityMsg, FiveDegrees.Messages"] = typeof(UpdateActivityMsg),
            ["FiveDegrees.Messages.ProcessManager.v2.UpdateActivityMsgV2, FiveDegrees.Messages"] = typeof(UpdateActivityMsgV2),
            ["FiveDegrees.Messages.ProcessManager.EndActivityMsg, FiveDegrees.Messages"] = typeof(EndActivityMsg),
            ["FiveDegrees.Messages.ProcessManager.v2.EndActivityMsgV2, FiveDegrees.Messages"] = typeof(EndActivityMsgV2),
            ["FiveDegrees.Messages.ProcessManager.ReportingProcessManagerMsg, FiveDegrees.Messages"] = typeof(ReportingProcessManagerMsg),
            ["FiveDegrees.Messages.ProcessManager.UpdateProcessStatusMsg, FiveDegrees.Messages"] = typeof(UpdateProcessStatusMsg),
            ["FiveDegrees.Messages.ProcessManager.v2.UpdateProcessStatusMsgV2, FiveDegrees.Messages"] = typeof(UpdateProcessStatusMsgV2),
            ["FiveDegrees.Messages.ProcessManager.InsertWorkflowRunMsg, FiveDegrees.Messages"] = typeof(InsertWorkflowRunMsg)
        };

        public MessageDeserializer(ISerializer serializer) => _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

        public async Task<Message> Deserialize(TransportMessage transportMessage)
        {
            var headers = transportMessage.Headers.Clone();
            var json = Encoding.UTF8.GetString(transportMessage.Body);
            var typeName = headers.GetValue(Headers.Type);

            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.None
            };

            if (!KnownTypes.TryGetValue(typeName, out var type))
            {
                var msgBody = JsonConvert.DeserializeObject<JObject>(json, serializerSettings);
                JsonExtensions.RemoveTypeMetadata(msgBody);
                var msg = new Message(headers, JTokenExtensions.ToCamelCase(msgBody));
                return msg;
            }

            var body = JsonConvert.DeserializeObject(json, type, serializerSettings);
            return new Message(headers, body);
        }

        public Task<TransportMessage> Serialize(Message message) => _serializer.Serialize(message);
    }

    public static class JTokenExtensions
    {
        // Recursively converts a JObject with PascalCase names to camelCase
        [Pure]
        public static JObject ToCamelCase(this JObject original)
        {
            var newObj = new JObject();
            foreach (var property in original.Properties())
            {
                var newPropertyName = property.Name.ToCamelCaseString();
                newObj[newPropertyName] = property.Value.ToCamelCaseJToken();
            }

            return newObj;
        }

        // Recursively converts a JToken with PascalCase names to camelCase
        [Pure]
        static JToken ToCamelCaseJToken(this JToken original)
        {
            switch (original.Type)
            {
                case JTokenType.Object:
                    return ((JObject)original).ToCamelCase();
                case JTokenType.Array:
                    return new JArray(((JArray)original).Select(x => x.ToCamelCaseJToken()));
                default:
                    return original.DeepClone();
            }
        }

        // Convert a string to camelCase
        [Pure]
        static string ToCamelCaseString(this string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                return char.ToLowerInvariant(str[0]) + str.Substring(1);
            }

            return str;
        }
    }


    public static class JsonExtensions
    {
        const string valuesName = "$values";
        const string typeName = "$type";

        public static JToken RemoveTypeMetadata(this JToken root)
        {
            if (root == null)
                throw new ArgumentNullException();
            var types = root.SelectTokens(".." + typeName).Select(v => (JProperty)v.Parent).ToList();
            foreach (var typeProperty in types)
            {
                var parent = (JObject)typeProperty.Parent;
                typeProperty.Remove();
                var valueProperty = parent.Property(valuesName);
                if (valueProperty != null && parent.Count == 1)
                {
                    // Bubble the $values collection up removing the synthetic container object.
                    var value = valueProperty.Value;
                    if (parent == root)
                    {
                        root = value;
                    }
                    // Remove the $values property, detach the value, then replace it in the parent's parent.
                    valueProperty.Remove();
                    valueProperty.Value = null;
                    if (parent.Parent != null)
                    {
                        parent.Replace(value);
                    }
                }
            }
            return root;
        }
    }
}
