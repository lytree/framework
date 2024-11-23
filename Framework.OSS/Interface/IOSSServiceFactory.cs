using Minio;

namespace Framework.OSS.Interface
{
    public interface IOSSServiceFactory
    {
        IOSSService Create();

        IOSSService Create(string name);
    }
}