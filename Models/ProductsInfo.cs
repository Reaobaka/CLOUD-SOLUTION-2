using Azure;

namespace PART2.Models
{
    public class ProductsInfo
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ETag ETag { get; set; }

        
        public string BlobName { get; set; }

        
        public string ImageUrl { get; set; }
    }
}