using System.Threading.Tasks;
using Hiarc.Core.Events.Models;
using Hiarc.Core.Models;

namespace Hiarc.Core.Events
{
    public interface IEventServiceProvider
    {
        Task SendEvent(IHiarcEvent theEvent);
        Task SendUserCreatedEvent(User user);
        Task SendUserUpdatedEvent(User user);
        Task SendGroupCreatedEvent(Group group);
        Task SendCollectionCreatedEvent(Collection collection);
        Task SendFileCreatedEvent(File file);
        Task SendAddedUserToGroupEvent(User user, Group group);
        Task SendAddedChildToCollectionEvent(Collection child, Collection parent);
        Task SendAddedGroupToCollectionEvent(Group group, Collection collection);
        Task SendAddedFileToCollectionEvent(File file, Collection collection);
        Task SendNewVersionOfFileCreatedEvent(File file, FileVersion fileVersion);
    }
}