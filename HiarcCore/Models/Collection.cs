using System;

namespace Hiarc.Core.Models 
{
    public class Collection : Entity
    {
        public Collection()
        {
            this.Type = Entity.TYPE_COLLECTION;
        }
    }
}