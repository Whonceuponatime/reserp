using System;
using MaritimeERP.Core.Interfaces;

namespace MaritimeERP.Desktop.Services
{
    public class DataChangeNotificationService : IDataChangeNotificationService
    {
        public event EventHandler<MaritimeERP.Core.Interfaces.DataChangeEventArgs>? DataChanged;

        public void NotifyDataChanged(string dataType, string operation, object? data = null)
        {
            DataChanged?.Invoke(this, new MaritimeERP.Core.Interfaces.DataChangeEventArgs
            {
                DataType = dataType,
                Operation = operation,
                Data = data,
                Timestamp = DateTime.UtcNow
            });
        }
    }
} 