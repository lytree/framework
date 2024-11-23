
using Framework.OSS.Interface;
using Framework.OSS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Framework.OSS.Interface.Service
{
    public interface IQiniuOSSService : IOSSService
    {
        /// <summary>
        /// 获取储存桶信息
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        Task<Bucket> GetBucketInfoAsync(string bucketName);

        /// <summary>
        /// 获取储存桶绑定域名
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        Task<List<string>> GetBucketDomainNameAsync(string bucketName);
    }
}
