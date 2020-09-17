using Xunit;
using Hiarc.Core.Models.Requests;
using System;
using Hiarc.Core.Models;

namespace HiarcIntegrationTest.Tests
{
    public class UseCaseTests : HiarcTestBase
    {
        //[Fact]
        [Fact(Skip = "Manual Only")]
        public async void LotsOfUsers()
        {
            var rand = new Random();
            var numUsers = 20000000;

            for (var i=0; i < numUsers; i++)
            {
                var u = await _hiarc.CreateUser(logToConsole: false);

                var c = await _hiarc.CreateCollection();
                await _hiarc.AddUserToCollection(c.Key, u.Key, AccessLevel.READ_WRITE);

                var numFiles = rand.Next(2,10);
                for (var j=0; j < numFiles; j++)
                {
                    var f = await _hiarc.AttachToExistingFile(AWS_EAST_STORAGE, "does_not_matter");
                    await _hiarc.AddFileToCollection(c.Key, f.Key);
                }

                if (i % 1000 == 0)
                {
                    Console.WriteLine(i);
                }
            }
        }

    }
}
