using MaritimeERP.Core.Entities;
using ComponentEntity = MaritimeERP.Core.Entities.Component;

namespace MaritimeERP.Services.Interfaces
{
    public interface INavigationService
    {
        void NavigateToPage(string pageName);
        void NavigateToPageWithFilter(string pageName, ShipSystem? systemFilter = null);
        void NavigateToPageWithComponentFilter(string pageName, ComponentEntity? componentFilter = null);
    }
} 