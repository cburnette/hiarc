namespace Hiarc.Core.Storage
{
    public interface IStorageServiceProvider
    {
        IStorageService Service(string name = null);
    }
}