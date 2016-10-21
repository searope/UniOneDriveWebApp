/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using System;
using Newtonsoft.Json;
using UniOneDriveWebApp.Models;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.OneDrive.Sdk;


namespace UniOneDriveWebApp.Controllers
{
    public class NotificationController : Controller
    {
        public ActionResult LoadView()
        {
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
                    var json = await inputStream.ReadToEndAsync();
                    var collection = JsonConvert.DeserializeObject<Models.OneDriveNotificationCollection>(json);
                    if (collection != null && collection.Notifications != null)
                    {
                        return collection.Notifications;
                    }
                }
            }
            catch
            {
            }
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
                OneDriveClient client;
                try
                {
                    #region Create OneDriveClient for current user
                    OneDriveUser user = OneDriveUser.UserForRequest(this.Request);
                    if (null == user)
                    {
                        return Redirect(Url.Action("Index", "Home"));
                    }
                    client = await SubscriptionController.GetOneDriveClientAsync(user);
                    #endregion
                }
                catch (Exception e)
                {
                    return Redirect(Url.Action("Index", "Home"));
                }

                foreach (var subscription in SubscriptionController.Subscriptions)
                {
                    var delta = await GetChanges(client, subscription.Value);
                }

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

        public async Task<ActionResult> GetDelta()
        {
            #region Create OneDriveClient for current user
            OneDriveUser user = OneDriveUser.UserForRequest(this.Request);
            if (null == user)
            {
                return Redirect(Url.Action("Index", "Home"));
            }
            OneDriveClient client = await SubscriptionController.GetOneDriveClientAsync(user);
            #endregion

            try
            {
                var delta = new List<OneDriveItemViewModel>();
                foreach (var subscription in SubscriptionController.Subscriptions)
                {
                    delta.AddRange(await GetChanges(client, subscription.Value));
                }
                return PartialView("_DeltaPartial", delta);
            }
            catch (Exception e)
            {
                ViewBag.Message = e.Message;
            }

            return new EmptyResult();
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

/*
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
*/
        }


        private async Task<List<OneDriveItemViewModel>> GetChanges(OneDriveClient client, OneDriveSubscription subscription)
        {
            var result = new List<OneDriveItemViewModel>(); // since it was a full scan, there is no delta yet.
            var delta = await client.Drives[subscription.DriveId].Root.Delta(subscription.DeltaToken).Request().GetAsync();
            if (subscription.DeltaToken == null)
            {
                subscription.Items = delta.ToDictionary(i => i.Id, i => i);
                //foreach (var item in delta)
                //{
                //    var itemDetails = await client.Drive.subscription.Items[item.Id].Request().GetAsync();
                //    subscription.Items[itemDetails.Id] = itemDetails;
                //}
            }
            else
            {
                foreach (var item in delta)
                {
                    if (item.Deleted != null) // item was deleted
                    {
                        Item itemDetails;
                        if (subscription.Items.TryGetValue(item.Id, out itemDetails))
                        {
                            subscription.Items.Remove(item.Id);
                            itemDetails.Deleted = item.Deleted;
                            result.Add(new OneDriveItemViewModel(subscription.DriveName, itemDetails, itemDetails));
                        }
                    }
                    else
                    {
                        Item storedItem;
                        var itemDetails = await client.Drives[subscription.DriveId].Items[item.Id].Request().GetAsync();
                        subscription.Items.TryGetValue(itemDetails.Id, out storedItem);
                        result.Add(new OneDriveItemViewModel(subscription.DriveName, itemDetails, storedItem));
                        subscription.Items[itemDetails.Id] = itemDetails;
                    }
                }
            }
            subscription.DeltaToken = delta.Token;
            return result;
        }

        private async Task ProcessChangesToUserFolder(OneDriveUser user, SignalR.NotificationService notificationService)
        {
            var client = await SubscriptionController.GetOneDriveClientAsync(user);

            List<string> filesChanged = new List<string>();

            var knownFiles = user.FileNameAndETag;
            var request = client.Drive.Root.Children.Request();
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