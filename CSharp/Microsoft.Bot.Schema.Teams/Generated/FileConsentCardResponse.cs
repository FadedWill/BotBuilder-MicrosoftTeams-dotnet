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
    /// File consent card response invoke activity payload.
    /// </summary>
    public partial class FileConsentCardResponse
    {
        /// <summary>
        /// Initializes a new instance of the FileConsentCardResponse class.
        /// </summary>
        public FileConsentCardResponse()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the FileConsentCardResponse class.
        /// </summary>
        /// <param name="action">User action on the file consent card. Possible
        /// values include: 'accept', 'decline'</param>
        /// <param name="context">Context sent with the file consent
        /// card.</param>
        /// <param name="uploadInfo">Context sent back to the Bot if user
        /// declined. This is free flow schema and is sent back in Value field
        /// of Activity.</param>
        public FileConsentCardResponse(string action = default(string), object context = default(object), FileUploadInfo uploadInfo = default(FileUploadInfo))
        {
            Action = action;
            Context = context;
            UploadInfo = uploadInfo;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets user action on the file consent card. Possible values
        /// include: 'accept', 'decline'
        /// </summary>
        [JsonProperty(PropertyName = "action")]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets context sent with the file consent card.
        /// </summary>
        [JsonProperty(PropertyName = "context")]
        public object Context { get; set; }

        /// <summary>
        /// Gets or sets context sent back to the Bot if user declined. This is
        /// free flow schema and is sent back in Value field of Activity.
        /// </summary>
        [JsonProperty(PropertyName = "uploadInfo")]
        public FileUploadInfo UploadInfo { get; set; }

    }
}
