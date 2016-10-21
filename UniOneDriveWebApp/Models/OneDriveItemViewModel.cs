using System;
using Microsoft.OneDrive.Sdk;
using UniOneDriveWebApp.Helpers;

namespace UniOneDriveWebApp.Models
{
    public class OneDriveItemViewModel
    {
        public string DriveName;

        public OneDriveItemViewModel(string driveName, Item item, Item savedItem = null)
        {
            DriveName = driveName;
            Id = item.Id;
            Name = item.Name;
            Url = item.WebUrl;
            Size = item.Size;
            CreatedBy = item.CreatedBy?.User?.DisplayName;
            CreatedDateTime = item.CreatedDateTime;
            LastModifiedBy = item.CreatedBy?.User?.DisplayName;
            LastModifiedDateTime = item.LastModifiedDateTime;

            if (item.Folder != null)
            {
                ItemType = ItemType.Folder;
            }
            else if (item.File != null)
            {
                ItemType = ItemType.File;
            }
            else
            {
                ItemType = ItemType.Unknown;
            }

            if (item.Deleted != null)
            {
                ChangeType = ChangeType.Deleted;
            }
            else if (savedItem == null)
            {
                ChangeType = ChangeType.Created;
            }
            else if (item.ETag == savedItem.ETag)
            {
                ChangeType = ChangeType.NotChanged;
            }
            else if (item.ETag != savedItem.ETag)
            {
                if (item.Name != savedItem.Name && item.File?.Hashes.QuickXorHash() == savedItem.File?.Hashes.QuickXorHash())
                {
                    ChangeType = ChangeType.Renamed;
                }
                else
                {
                    ChangeType = ChangeType.Modified;
                }
            }
            else
            {
                ChangeType = ChangeType.NotAvailable;
            }
        }
        public string Id { get; set; }
        public ItemType ItemType { get; set; }
        public ChangeType ChangeType { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public long? Size { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset? CreatedDateTime { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTimeOffset? LastModifiedDateTime { get; set; }
    }

    public enum ItemType
    {
        Unknown,
        Folder,
        File
    }

    public enum ChangeType
    {
        NotAvailable,
        Created,
        Modified,
        Deleted,
        Renamed,
        NotChanged
    }
}