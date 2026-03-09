using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.EnterpriseRAG.Infrastructure.Services.VectorStores
{
    public class QdrantVectorStore : IVectorStore
    {
        public Task BatchDeleteByDocumentIdsAsync(List<Guid> documentIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task ClearAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteExpiredAsync(DateTime expireTime, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> InitAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task InsertAsync(DocumentChunk chunk, float[] vector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<DocumentChunk>> SearchAsync(float[] queryVector, int topK = 3, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
