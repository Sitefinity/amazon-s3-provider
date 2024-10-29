using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Telerik.Microsoft.Practices.Unity;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Amazon.BlobStorage;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.Modules.Libraries;
using Telerik.Sitefinity.Modules.Libraries.Configuration;

namespace Telerik.Sitefinity.Amazon
{
    /// <summary>
    /// Registers Amazon blob storage provider.
    /// </summary>
    public static class Initializer
    {
        /// <summary>
        /// Attach to ObjectFactory_RegisteringIoCTypes event.
        /// </summary>
        public static void Initialize()
        {
            ObjectFactory.RegisteringIoCTypes += ObjectFactory_RegisteringIoCTypes;
        }

        private static void ObjectFactory_RegisteringIoCTypes(object sender, EventArgs e)
        {
            ObjectFactory.Container.RegisterType(typeof(IBlobStorageConfiguration), typeof(AmazonBlobStorageConfiguration), typeof(AmazonBlobStorageConfiguration).Name);
        }

        /// <summary>
        /// Register Amazon blob strorage providers and types
        /// </summary>
        class AmazonBlobStorageConfiguration : IBlobStorageConfiguration
        {
            public IEnumerable<BlobStorageTypeConfigElement> GetProviderTypeConfigElements(ConfigElement parent)
            {
                var providerTypes = new List<BlobStorageTypeConfigElement>();

                providerTypes.Add(new BlobStorageTypeConfigElement(parent)
                {
                    Name = "Amazon",
                    ProviderType = typeof(AmazonBlobStorageProvider),
                    Title = "Amazon S3",
                    Parameters = new NameValueCollection()
                    {
                        { AmazonBlobStorageProvider.AccessKeyIdKey, string.Empty },
                        { AmazonBlobStorageProvider.SecretKeyKey, "#sf_Secret" },
                        { AmazonBlobStorageProvider.BucketNameKey, string.Empty },
                        { AmazonBlobStorageProvider.RegionEndpointKey, string.Empty },
                        { AmazonBlobStorageProvider.UrlSchemeKey, string.Empty }
                   }
                });

                return providerTypes;
            }

            public IEnumerable<DataProviderSettings> GetProviderConfigElements(ConfigElement parent)
            {
                return new List<DataProviderSettings>();
            }
        }
    }
}
