using System.Text.Json;
using System.Text.Json.Serialization;

namespace Abaddax.Utilities.Text.Json
{
    public sealed class SerializableObject
    {
        private readonly string? _initalJsonType;
        private readonly JsonElement? _initalJsonValue;
        private bool _valueDeserialized;
        private object? _value;

        [JsonPropertyName("$type")]
        public string JsonType
        {
            get
            {
                if (!string.IsNullOrEmpty(_initalJsonType) && _value == null)
                    return _initalJsonType;
                return _value?.GetType()?.AssemblyQualifiedName ?? string.Empty;
            }
            init
            {
                _initalJsonType = value;
            }
        }
        [JsonPropertyName("$value")]
        public JsonElement JsonValue
        {
            get
            {
                if (_initalJsonValue.HasValue && _value == null)
                    return _initalJsonValue.Value;
                return JsonSerializer.SerializeToElement(_value);
            }
            init
            {
                _initalJsonValue = value;
            }
        }

        [JsonIgnore]
        public object? Value
        {
            get
            {
                if (_value != null || _valueDeserialized)
                    return _value;
                if (string.IsNullOrEmpty(JsonType))
                    return null;
                var type = Type.GetType(JsonType) ?? throw new InvalidOperationException($"Type not found: {JsonType}");
                _value = JsonValue.Deserialize(type);
                _valueDeserialized = true;
                return _value;
            }
            set
            {
                _value = value;
            }
        }

    }
}
