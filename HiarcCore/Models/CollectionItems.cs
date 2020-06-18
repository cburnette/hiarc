using System;
using System.Collections.Generic;

namespace Hiarc.Core.Models 
{
    public class CollectionItems
    {
        public List<Collection> ChildCollections { get; set; }  
        public List<File> Files { get; set; }
              
        public Dictionary<string, object> ToDictionary()
        {
            var eventProps = new Dictionary<string,object>
            {
            };

            return eventProps;
        }
    }
}