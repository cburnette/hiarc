using System.Threading.Tasks;
using Hiarc.Core.Events.Models;

namespace Hiarc.Core.Events
{
    public interface IEventService
    {
        Task SendEvent(IHiarcEvent theEvent);
    }
}