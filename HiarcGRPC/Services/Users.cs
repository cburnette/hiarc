using System;
using System.Threading.Tasks;
using Grpc.Core;
using Hiarc.Core;
using Microsoft.Extensions.Logging;

namespace HiarcGRPC
{
    public partial class HiarcService2 : HiarcService.HiarcServiceBase
    {
        public override async Task<CreateUserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
        {
            try
            {
                if (request.User.Key == Admin.DEFAULT_ADMIN_NAME)
                {
                    _logger.LogError($"Attempted to create user with Admin name '{Admin.DEFAULT_ADMIN_NAME}'");
                    return new CreateUserResponse { Result = new Result { Success = false, Message = "Forbidden" }};
                }

                var newUserRequest = new Hiarc.Core.Models.Requests.CreateUserRequest() { Key = request.User.Key };
                var newUser = await _hiarcDatabase.CreateUser(newUserRequest);
                var uri = $"{_hiarcSettings.BaseUri}/users/{newUser.Key}";

                var newUser2 = new User() { Key = newUser.Key, Name = newUser.Name, Description = newUser.Description };
                return new CreateUserResponse { Result = new Result { Success = true }, User = newUser2 };
            }
            catch(Exception ex)
            {
                return new CreateUserResponse { Result = new Result { Success = false, Message = ex.Message }};
            }


            // try
            // {
            //     await _hiarcDatabase.InitDatabase(Admin.DEFAULT_ADMIN_NAME);
            //     return new InitDatabaseResponse { Result = new Result { Success = true, Message="Successfully initialized Hiarc database" }};
            // }
            // catch(Exception ex)
            // {
            //     return new InitDatabaseResponse { Result = new Result { Success = false, Message = ex.Message }};
            // } 
        }
    }
}