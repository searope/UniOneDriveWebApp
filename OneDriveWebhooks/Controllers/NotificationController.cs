/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using Newtonsoft.Json;
using OneDriveWebhookTranslator.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;


namespace OneDriveWebhookTranslator.Controllers
{
    public class NotificationController : Controller
    {
        public ActionResult LoadView(string subscriptionId)
        {
            ViewBag.SubscriptionId = subscriptionId;
            return View("Notification");
        }

        /// <summary>
        /// Parse JSON from webhook message
        /// </summary>
        /// <returns></returns>
        private async Task<Models.OneDriveWebhookNotification[]> ParseIncomingNotificationAsync()
        {
            try
            {
                using (var inputStream = new System.IO.StreamReader(Request.InputStream))
                {
                    var collection = JsonConvert.DeserializeObject<Models.OneDriveNotificationCollection>(await inputStream.ReadToEndAsync());
                    if (collection != null && collection.Notifications != null)
                    {
                        return collection.Notifications;
                    }
                }
            }
            catch { }
            return null;
        }

        public async Task<ActionResult> Listen()
        {
            #region Validation new subscriptions
            // Respond to validation requests from the service by sending the token
            // back to the service. This response is required for each subscription.
            const string ValidationTokenKey = "validationToken";
            if (Request.QueryString[ValidationTokenKey] != null)
            {
                string token = Request.QueryString[ValidationTokenKey];
                return Content(token, "text/plain");
            }
            #endregion

            var notifications = await ParseIncomingNotificationAsync();
            if (null != notifications && notifications.Any())
            {
                await ProcessNotificationsAsync(notifications);
            }

            // Return a 200 so the service doesn't resend the notification.
            return new HttpStatusCodeResult(200);
        }

        /// <summary>
        /// Enumerate the changes detected by this notification
        /// </summary>
        /// <param name="notifications"></param>
        /// <returns></returns>
        private async Task ProcessNotificationsAsync(Models.OneDriveWebhookNotification[] notifications)
        {
            SignalR.NotificationService service = new SignalR.NotificationService();
            service.SendNotificationToClient(notifications.ToList());

            // In a production service, you should store notifications into a queue and process them on a WebJob or
            // other background service runner
            foreach (var notification in notifications)
            {
                var user = OneDriveUserManager.LookupUserForSubscriptionId(notification.SubscriptionId);
                if (null != user)
                {
                    await ProcessChangesToUserFolder(user, service);
                }
            }
        }

        private async Task ProcessChangesToUserFolder(OneDriveUser user, SignalR.NotificationService notificationService)
        {
            var client = await SubscriptionController.GetOneDriveClientAsync(user);

            List<string> filesChanged = new List<string>();

            var knownFiles = user.FileNameAndETag;
            var request = client.Drive.Special["approot"].Children.Request();
            while (request != null)
            {
                var items = await request.GetAsync();
                // Pull out the changes we're interested in

                foreach (var item in items)
                {
                    string etag;
                    if (knownFiles.TryGetValue(item.Name, out etag))
                    {
                        if (etag == item.ETag)
                            continue;
                    }
                    knownFiles[item.Name] = item.ETag;
                    filesChanged.Add(item.Name);
                }
                request = items.NextPageRequest;
            }

            notificationService.SendFileChangeNotification(filesChanged);
        }



    }
}