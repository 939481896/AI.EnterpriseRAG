using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.EnterpriseRAG.Application.Dtos
{
    public class ChatRequestDto
    {
        public string UserId { get; set; }=string.Empty;

        public string Question { get; set; } = string.Empty;
    }
}
