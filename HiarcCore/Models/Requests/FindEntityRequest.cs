using System;
using System.Collections.Generic;

namespace Hiarc.Core.Models.Requests
{
    public class FindEntityRequest
    {
        public List<Dictionary<string, object>> Query { get; set; }
    }
}