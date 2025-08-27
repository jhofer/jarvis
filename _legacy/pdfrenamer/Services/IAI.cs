namespace PDFRenamerIsolated.Services
{
    public interface IAI
    {
        public Task<AIResult> ExtractTitleAsync(AIRequest request);
    }
}