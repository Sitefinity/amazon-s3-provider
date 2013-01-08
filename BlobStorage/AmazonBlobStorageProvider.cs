using System;
using System.Linq;
using Amazon.S3.Transfer;
using Telerik.Sitefinity.Modules.Libraries.BlobStorage;
using Telerik.Sitefinity.BlobStorage;
using Amazon.S3;
using Amazon.S3.Model;
using System.Configuration;
using System.Collections.Specialized;
using System.IO;

namespace Telerik.Sitefinity.Amazon.BlobStorage
{
    /// <summary>
    /// This provider implements the logic for persisting BLOB data on Amazon S3 Storage.
    /// </summary>
    public class AmazonBlobStorageProvider : CloudBlobStorageProvider
    {
        #region Overridden Methods

        /// <summary>
        /// Initializes access to the remote storage.
        /// </summary>
        /// <param name="config">The collection of parameters (each by its name and value) of the current provider's configuration settings.</param>
        protected override void InitializeStorage(NameValueCollection config)
        {
            this.accessKeyId = config[AccessKeyIdKey].Trim();
            if (String.IsNullOrEmpty(this.accessKeyId))
                throw new ConfigurationException("'{0}' is required.".Arrange(AccessKeyIdKey));

            this.secretKey = config[SecretKeyKey].Trim();
            if (String.IsNullOrEmpty(this.secretKey))
                throw new ConfigurationException("'{0}' is required.".Arrange(SecretKeyKey));

            this.bucketName = config[BucketNameKey].Trim();
            if (String.IsNullOrEmpty(this.bucketName))
                throw new ConfigurationException("'{0}' is required.".Arrange(BucketNameKey));

            this.transferUtility = new TransferUtility(accessKeyId, secretKey);
        }

        /// <summary>
        /// Resolves the content item's external URL on the remote blob storage.
        /// </summary>
        /// <param name="content">Descriptor of the item on the remote blob storage for which to retrieve the URL.</param>
        /// <returns>The resolved content item's external URL on the remote blob storage.</returns>
        public override string GetItemUrl(IBlobContentLocation content)
        {
            return string.Concat("http://", this.bucketName, ".s3.amazonaws.com/", content.FilePath);
        }

        /// <summary>
        /// Copies the source content to the specified destination on the remote blob storage.
        /// </summary>
        /// <param name="source">Descriptor of the source item on the remote blob storage.</param>
        /// <param name="destination">Descriptor of the destination item on the remote blob storage.</param>
        public override void Copy(IBlobContentLocation source, IBlobContentLocation destination)
        {
            var request = new CopyObjectRequest()
                .WithSourceBucket(this.bucketName).WithSourceKey(source.FilePath)
                .WithDestinationBucket(this.bucketName).WithDestinationKey(destination.FilePath);

            transferUtility.S3Client.CopyObject(request);
        }

        /// <summary>
        /// Sets the properties, like cacheControl, content type, etc.
        /// </summary>
        /// <param name="location">Descriptor of the item on the remote blob storage.</param>
        /// <param name="properties">The properties to set.</param>
        public override void SetProperties(IBlobContentLocation location, IBlobProperties properties)
        {
            //No properties to set by default
        }

        /// <summary>
        /// Gets the content type, cache control settings, etc. of a blob.
        /// </summary>
        /// <param name="location">Descriptor of the item on the remote blob storage.</param>
        /// <returns>The retrieved properties.</returns>
        public override IBlobProperties GetProperties(IBlobContentLocation location)
        {
            //No properties to get by default
            return null;
        }

        /// <summary>
        /// Uploads the specified content item to the remote blob storage.
        /// </summary>
        /// <param name="content">Descriptor of the item on the remote blob storage.</param>
        /// <param name="source">The source item's content stream.</param>
        /// <param name="bufferSize">Size of the upload buffer.</param>
        /// <returns>The length of the uploaded stream.</returns>
        public override long Upload(IBlobContent content, Stream source, int bufferSize)
        {
            var request = new TransferUtilityUploadRequest()
                .WithBucketName(this.bucketName)
                .WithKey(content.FilePath)
                .WithPartSize(bufferSize)
                .WithContentType(content.MimeType);

            //set the item's accessibility as public
            request.AddHeader("x-amz-acl", "public-read");

            //get it before the upload, because afterwards the stream is closed already
            long sourceLength = source.Length;
            request.InputStream = source;

            this.transferUtility.Upload(request);

            return sourceLength;
        }

        /// <summary>
        /// Gets the upload stream for a specific content.
        /// </summary>
        /// <param name="content">Descriptor of the item on the remote blob storage.</param>
        /// <returns>The upload stream for a specific content.</returns>
        public override Stream GetUploadStream(IBlobContent content)
        {
            throw new NotSupportedException("GetUploadStream() is not supported. Override Upload method.");
        }

        /// <summary>
        /// Gets the download stream for a specific content..
        /// </summary>
        /// <param name="content">Descriptor of the item on the remote blob storage.</param>
        /// <returns>The binary stream of the content item.</returns>
        public override Stream GetDownloadStream(IBlobContent content)
        {
            TransferUtilityOpenStreamRequest request = new TransferUtilityOpenStreamRequest()
               .WithBucketName(this.bucketName).WithKey(content.FilePath);
            var stream = this.transferUtility.OpenStream(request);
            return stream;
        }

        /// <summary>
        /// Deletes the blob item stored under the specified blob location
        /// </summary>
        /// <param name="location">Descriptor of the item on the remote blob storage.</param>
        public override void Delete(IBlobContentLocation location)
        {
            var request = new DeleteObjectRequest().WithBucketName(this.bucketName).WithKey(location.FilePath);
            transferUtility.S3Client.BeginDeleteObject(request, null, null);
        }

        /// <summary>
        /// Determines whether a blob item under the specified location exists.
        /// </summary>
        /// <param name="location">Descriptor of the item on the remote blob storage.</param>
        /// <returns>True if the item exists, otherwise - false</returns>
        public override bool BlobExists(IBlobContentLocation location)
        {
            var request = new GetObjectRequest().WithBucketName(this.bucketName).WithKey(location.FilePath);

            try
            {
                var response = transferUtility.S3Client.GetObject(request);
                return true;
            }
            catch (AmazonS3Exception err)
            { 
            }
            return false;
        }

        #endregion

        #region Properties

        public const string AccessKeyIdKey = "accessKeyId";
        public const string SecretKeyKey = "secretKey";
        public const string BucketNameKey = "bucketName";

        #endregion

        #region Fields

        private string accessKeyId = "";
        private string secretKey = "";
        private string bucketName = "";
        TransferUtility transferUtility;

        #endregion


    }
}
