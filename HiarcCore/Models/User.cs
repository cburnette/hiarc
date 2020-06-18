using System.Collections.Generic;

namespace Hiarc.Core.Models 
{
    public class User : Entity
    {
        public User()
        {
            this.Type = Entity.TYPE_USER;
        }
    }
}