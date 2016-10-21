/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using System;
using System.Collections.Generic;
using Microsoft.OneDrive.Sdk;
using Newtonsoft.Json;

namespace UniOneDriveWebApp.Models
{
    public class OneDriveSubscription
    {
        // The string that MS Graph should send with each notification. Maximum length is 255 characters. 
        // To verify that the notification is from MS Graph, compare the value received with the notification to the value you sent with the subscription request.
        [JsonProperty("clientState", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ClientState { get; set; }

        // The URL of the endpoint that receives the subscription response and notifications. Requires https.
        [JsonProperty("notificationUrl", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string NotificationUrl { get; set; }

        // The resource to monitor for changes.
        [JsonProperty("resource", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Resource { get; set; }

        // The date and time when the webhooks subscription expires.
        // The time is in UTC, and can be up to three days from the time of subscription creation.
        [JsonProperty("expirationDateTime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset SubscriptionExpirationDateTime { get; set; }

        // The unique identifier for the webhooks subscription.
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SubscriptionId { get; set; }

        // OneDrive Personal requires scenarios to be passed currently. This requirement will be removed in the future
        [JsonProperty("scenarios", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] Scenarios { get; set; }

        // Drive name
        [JsonIgnore]
        public string DriveId { get; set; }
        // Drive name
        [JsonIgnore]
        public string DriveName { get; set; }

        [JsonIgnore]
        public string DeltaToken { get; set; }

        [JsonIgnore]
        public Dictionary<string, Item> Items { get; set; }

        public OneDriveSubscription()
        {
            ClientState = Guid.NewGuid().ToString("N");
            Scenarios = new[] { "Webhook" };
            Items = new Dictionary<string, Item>();
        }
    }

    public class SubscriptionViewModel
    {
        public SubscriptionViewModel()
        {
            Subscriptions = new Dictionary<string, OneDriveSubscription>();
        }

        public SubscriptionViewModel(Dictionary<string, OneDriveSubscription> subscriptions)
        {
            Subscriptions = subscriptions;
        }

        public Dictionary<string, OneDriveSubscription> Subscriptions { get; set; }
    }
}