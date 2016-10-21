/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.OneDrive.Sdk;
using UniOneDriveWebApp.Models;
using System.Threading;
using BaseRequest = Microsoft.Graph.BaseRequest;

namespace UniOneDriveWebApp.Controllers
{
    public class SubscriptionController : Controller
    {
        public static Dictionary<string, OneDriveSubscription> Subscriptions = new Dictionary<string, OneDriveSubscription>();

        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> Drives()
        {
            #region Create OneDriveClient for current user
            OneDriveUser user = OneDriveUser.UserForRequest(this.Request);
            if (null == user)
            {
                return Redirect(Url.Action("Index", "Home"));
            }
            var client = await GetOneDriveClientAsync(user);
            #endregion

            try
            {
                var drives = await client.Drives.Request().GetAsync();
                /*
                                var drives = await client.Drives.Request().GetAsync();
                                //var drives = await client.Drives.Request().GetAsync();
                                foreach (var drive in drives)
                                {
                                    var details = await client.Drives[drive.Id].Request().GetAsync();
                                }
                */
            }
            catch (Exception e)
            {
                //throw;
            }

            return View("Index");
        }

        // Create webhook subscription
        public async Task<ActionResult> CreateSubscription()
        {
            Subscriptions.Clear();
            var viewModel = new SubscriptionViewModel(Subscriptions);
            ViewBag.Message = "";

            #region Create OneDriveClient for current user
            OneDriveUser user = OneDriveUser.UserForRequest(this.Request);
            if (null == user)
            {
                return Redirect(Url.Action("Index", "Home"));
            }
            OneDriveClient client = await GetOneDriveClientAsync(user);
            #endregion

            IOneDriveDrivesCollectionPage drives;
            try
            {
                // Ensure the app folder is created first
                drives = await client.Drives.Request().GetAsync();
                //var delta = await client.Drive.Root.Delta(null).Request().GetAsync();
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
                return View("Error");
            }

            foreach (var drive in drives)
            {
                var driveName = drive.AdditionalData?.FirstOrDefault(ad => ad.Key == "name").Value?.ToString();
                if (driveName == null || Settings.BuildInDocLibs.Contains(driveName))
                { // skipping build-in sharpoint doclibs
                    continue;
                }

                // Create a subscription on the drive
                var subscription = new OneDriveSubscription
                {
                    NotificationUrl = ConfigurationManager.AppSettings["ida:NotificationUrl"],
                    SubscriptionExpirationDateTime = DateTime.UtcNow.AddHours(1)
                };

                // Because the OneDrive SDK does not support OneDrive subscriptions natively yet, 
                // we use BaseRequest to generate a request the SDK can understand. You could also use HttpClient
                var request = new BaseRequest(client.BaseUrl + "/drive/root/subscriptions", client)
                {
                    Method = "POST",
                    ContentType = "application/json"
                };

                try
                {
                    var subscriptionResponse = await request.SendAsync<OneDriveSubscription>(subscription, CancellationToken.None);
                    if (null != subscriptionResponse)
                    {
                        // Store the subscription ID so we can keep track of which subscriptions are tied to which users
                        user.SubscriptionId = subscriptionResponse.SubscriptionId;
                        subscriptionResponse.ClientState = subscription.ClientState;
                        subscriptionResponse.DriveId = drive.Id;
                        subscriptionResponse.DriveName = driveName;
                        Subscriptions.Add(subscriptionResponse.SubscriptionId, subscriptionResponse);
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Message = ex.Message + Environment.NewLine;
                }
            }
            if (Subscriptions.Count > 0)
            {
                return View("Subscription", viewModel);
            }

            if (ViewBag.Message == "")
            {
                ViewBag.Message = "No drives were found";
            }

            return View("Error");
        }

        /// <summary>
        /// Delete the user's active subscription and then redirect to logout
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> DeleteSubscription()
        {
            OneDriveUser user = OneDriveUser.UserForRequest(this.Request);
            if (null == user)
            {
                return Redirect(Url.Action("Index", "Home"));
            }

            if (!string.IsNullOrEmpty(user.SubscriptionId))
            {
                var client = await GetOneDriveClientAsync(user);

                // Because the OneDrive SDK does not support OneDrive subscriptions natively yet, 
                // we use BaseRequest to generate a request the SDK can understand
                var request = new BaseRequest(client.BaseUrl + "/drive/documents/subscriptions/" + user.SubscriptionId, client) { Method = "DELETE" };

                try
                {
                    var response = await request.SendRequestAsync(null, CancellationToken.None);
                    if (!response.IsSuccessStatusCode)
                    {
                        ViewBag.Message = response.ReasonPhrase;
                        return View("Error");
                    }
                    else
                    {
                        user.SubscriptionId = null;
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Message = ex.Message;
                    return View("Error");
                }
            }
            return RedirectToAction("SignOut", "Account");
        }

        #region SDK helper methods

        /// <summary>
        /// Create a new instance of the OneDriveClient for the signed in user.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        internal static async Task<OneDriveClient> GetOneDriveClientAsync(OneDriveUser user)
        {
            //if (string.IsNullOrEmpty(user.OneDriveBaseUrl))
            //{
                // Resolve the API URL for this user
                user.OneDriveBaseUrl = "https://microsoft.sharepoint.com/teams/WDGUNITEST/_api/v2.0"; ;
            //}

            var client = new Microsoft.OneDrive.Sdk.OneDriveClient(new OneDriveAccountServiceProvider(user));
            client.BaseUrl = "https://microsoft.sharepoint.com/teams/WDGUNITEST/_api/v2.0";

            return client;
        }

        #endregion
    }
}