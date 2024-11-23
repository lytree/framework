using Framework.OSS.Models.Policy;
using System.Collections.Generic;

namespace Framework.OSS.Models
{
    public class PolicyInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<StatementItem> Statement { get; set; }
    }
}
