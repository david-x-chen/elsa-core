using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Serialization.Converters;

/// <summary>
/// (De)serializes objects of type <see cref="IActivity"/>.
/// </summary>
public class ActivityJsonConverter : JsonConverter<IActivity>
{
    private readonly IActivityRegistry _activityRegistry;
    private readonly IServiceProvider _serviceProvider;

    public ActivityJsonConverter(IActivityRegistry activityRegistry, IServiceProvider serviceProvider)
    {
        _activityRegistry = activityRegistry;
        _serviceProvider = serviceProvider;
    }

    public override IActivity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        if (!doc.RootElement.TryGetProperty("typeName", out var activityTypeNameElement))
            throw new JsonException("Failed to extract activity type property");

        var activityTypeName = activityTypeNameElement.GetString()!;
        var activityDescriptor = _activityRegistry.Find(activityTypeName);

        if (activityDescriptor == null)
        {
            var activityId = doc.RootElement.TryGetProperty("Id", out var activityIdElement) ? activityIdElement.GetString() : default;
            
            return new NotFoundActivity(activityTypeName)
            {
                Id = activityId ?? Guid.NewGuid().ToString("N"),
            };
        }

        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Add(new InputJsonConverterFactory(_serviceProvider));
        newOptions.Converters.Add(new OutputJsonConverterFactory(_serviceProvider));

        var context = new ActivityConstructorContext(doc.RootElement, newOptions);
        var activity = activityDescriptor.Constructor(context);

        return activity;
    }

    public override void Write(Utf8JsonWriter writer, IActivity value, JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Add(new InputJsonConverterFactory(_serviceProvider));
        newOptions.Converters.Add(new OutputJsonConverterFactory(_serviceProvider));
        JsonSerializer.Serialize(writer, value, value.GetType(), newOptions);
    }
}