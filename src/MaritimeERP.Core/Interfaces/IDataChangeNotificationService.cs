using System;

namespace MaritimeERP.Core.Interfaces
{
    public interface IDataChangeNotificationService
    {
        event EventHandler<DataChangeEventArgs>? DataChanged;
        void NotifyDataChanged(string dataType, string operation, object? data = null);
    }

    public class DataChangeEventArgs : EventArgs
    {
        public string DataType { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public object? Data { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 