using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.EnterpriseRAG.Core.Exceptions
{
    public class BusinessException:Exception
    {
        public int Code { get; set; }

        public BusinessException(int code,string message):base(message)
        {  Code = code; }
        public BusinessException(string message):base(message) {
            Code = 500;
        }
    }
}
