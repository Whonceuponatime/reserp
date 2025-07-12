using System;

namespace MaritimeERP.Desktop.Services
{
    public interface IDataChangeNotificationService
    {
        event EventHandler<DataChangeEventArgs>? DataChanged;
        void NotifyDataChanged(string dataType, string operation, object? data = null);
    }

    public class DataChangeNotificationService : IDataChangeNotificationService
    {
        public event EventHandler<DataChangeEventArgs>? DataChanged;

        public void NotifyDataChanged(string dataType, string operation, object? data = null)
        {
            DataChanged?.Invoke(this, new DataChangeEventArgs
            {
                DataType = dataType,
                Operation = operation,
                Data = data,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public class DataChangeEventArgs : EventArgs
    {
        public string DataType { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public object? Data { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 