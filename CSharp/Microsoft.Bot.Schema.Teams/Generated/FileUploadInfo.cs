// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Bot.Schema.Teams
{
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Upload information for the file.
    /// </summary>
    public partial class FileUploadInfo
    {
        /// <summary>
        /// Initializes a new instance of the FileUploadInfo class.
        /// </summary>
        public FileUploadInfo()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the FileUploadInfo class.
        /// </summary>
        /// <param name="name">File name.</param>
        /// <param name="uploadUrl">URL to an upload session for the file
        /// contents.</param>
        /// <param name="contentUrl">URL to the file.</param>
        /// <param name="uniqueId">Identifier that uniquely identifies the
        /// file.</param>
        /// <param name="fileType">File type.</param>
        public FileUploadInfo(string name = default(string), string uploadUrl = default(string), string contentUrl = default(string), string uniqueId = default(string), string fileType = default(string))
        {
            Name = name;
            UploadUrl = uploadUrl;
            ContentUrl = contentUrl;
            UniqueId = uniqueId;
            FileType = fileType;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets file name.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets URL to an upload session for the file contents.
        /// </summary>
        [JsonProperty(PropertyName = "uploadUrl")]
        public string UploadUrl { get; set; }

        /// <summary>
        /// Gets or sets URL to the file.
        /// </summary>
        [JsonProperty(PropertyName = "contentUrl")]
        public string ContentUrl { get; set; }

        /// <summary>
        /// Gets or sets identifier that uniquely identifies the file.
        /// </summary>
        [JsonProperty(PropertyName = "uniqueId")]
        public string UniqueId { get; set; }

        /// <summary>
        /// Gets or sets file type.
        /// </summary>
        [JsonProperty(PropertyName = "fileType")]
        public string FileType { get; set; }

    }
}
