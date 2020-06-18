using Xunit;
using Hiarc.Core.Models.Requests;
using System;
using System.Threading.Tasks;
using Hiarc.Core.Models;
using System.Collections.Generic;

namespace HiarcIntegrationTest.Tests
{
    public class DatabaseTests
    {
        //[Fact]
        [Fact(Skip = "Manual Only")]
        public void Init()
        {      
            var _hiarc = new HiarcClient();
            var success = _hiarc.InitDB().Result;
            Assert.True(success);

            // make sure you install APOC plugin before running tests
            // CALL dbms.procedures() YIELD name RETURN head(split(name,".")) as package, count(*), collect(name) as procedures;
        }
    }
}