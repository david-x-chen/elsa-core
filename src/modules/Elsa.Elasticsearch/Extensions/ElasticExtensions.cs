using Elasticsearch.Net;
using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Nest;

namespace Elsa.Elasticsearch.Extensions;

public static class ElasticExtensions
{
    public static ConnectionSettings ConfigureAuthentication(this ConnectionSettings settings, ElasticsearchOptions options)
    {
        if (!string.IsNullOrEmpty(options.ApiKey))
        {
            settings.ApiKeyAuthentication(new ApiKeyAuthenticationCredentials(options.ApiKey));
        }
        else if (!string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password))
        {
            settings.BasicAuthentication(options.Username, options.Password);
        }

        return settings;
    }
    
    public static ConnectionSettings ConfigureMapping(this ConnectionSettings settings, IDictionary<string,string> aliasConfig)
    {
        foreach (var config in Utils.GetElasticConfigurationTypes())
        {
            var configInstance = (IElasticConfiguration)Activator.CreateInstance(config)!;
            configInstance.Apply(settings, aliasConfig);
        }

        return settings;
    }
    
    public static void ConfigureIndicesAndAliases(this ElasticClient client, IDictionary<string,string> aliasConfig)
    {
        foreach (var type in Utils.GetElasticDocumentTypes())
        {
            var aliasName = aliasConfig[type.Name];
            var indexName = Utils.GenerateIndexName(aliasName);
            
            var indexExists = client.Indices.Exists(indexName).Exists;

            if (indexExists) continue;
            
            var response = client.Indices.Create(indexName, s => s
                .Aliases(a => a.Alias(aliasName)));
                
            if (response.IsValid) continue;
            throw response.OriginalException;
                
        }
    }
}