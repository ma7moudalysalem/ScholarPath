using ScholarPath.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScholarPath.Domain.Entities
{
    public class ExpertiseTag : BaseEntity

    {
        public string Name { get; set; } = string.Empty;
        public virtual ICollection<UpgradeRequest> UpgradeRequests { get; set; } = new List<UpgradeRequest>();
       
    }
    }
    

