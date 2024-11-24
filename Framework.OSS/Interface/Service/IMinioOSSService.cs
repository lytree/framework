using Framework.OSS.Models;
using Framework.OSS.Models.Policy;
namespace Framework.OSS.Interface.Service
{
    public interface IMinioOSSService : IOSSService
    {
        Task<bool> RemoveIncompleteUploadAsync(string bucketName, string objectName);

        Task<List<ItemUploadInfo>> ListIncompleteUploads(string bucketName);

        Task<PolicyInfo> GetPolicyAsync(string bucketName);

        Task<bool> SetPolicyAsync(string bucketName, List<StatementItem> statements);

        Task<bool> RemovePolicyAsync(string bucketName);

        Task<bool> PolicyExistsAsync(string bucketName, StatementItem statement);
    }
}
