/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */
 
 using Owin;
using Microsoft.Owin;
[assembly: OwinStartup(typeof(UniOneDriveWebApp.Startup))]
namespace UniOneDriveWebApp
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Any connection or hub wire up and configuration should go here
            app.MapSignalR();
        }
    }
}