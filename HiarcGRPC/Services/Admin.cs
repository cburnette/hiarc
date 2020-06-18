using System;
using System.Threading.Tasks;
using Grpc.Core;
using Hiarc.Core;

namespace HiarcGRPC
{
    public partial class HiarcService2 : HiarcService.HiarcServiceBase
    {
        public override async Task<InitDatabaseResponse> InitDatabase(InitDatabaseRequest request, ServerCallContext context)
        {
            try
            {
                await _hiarcDatabase.InitDatabase(Admin.DEFAULT_ADMIN_NAME);
                return new InitDatabaseResponse { Result = new Result { Success = true, Message="Successfully initialized Hiarc database" }};
            }
            catch(Exception ex)
            {
                return new InitDatabaseResponse { Result = new Result { Success = false, Message = ex.Message }};
            } 
        }
    }
}