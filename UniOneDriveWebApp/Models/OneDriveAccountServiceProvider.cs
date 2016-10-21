/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace UniOneDriveWebApp.Models
{
    class OneDriveAccountServiceProvider : IAuthenticationProvider
    {
        private readonly OneDriveUser _user;
        private readonly string _resource;

        public OneDriveAccountServiceProvider(OneDriveUser user)
        {
            _user = user;

            Uri baseUrl;
            if (!Uri.TryCreate(user.OneDriveBaseUrl, UriKind.Absolute, out baseUrl))
            {
                throw new InvalidOperationException("Unable to parse base URL: " + user.OneDriveBaseUrl);
            }

            _resource = string.Concat(baseUrl.Scheme, "://", baseUrl.Host);
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await _user.GetAccessTokenAsync(_resource));
        }
    }
}
