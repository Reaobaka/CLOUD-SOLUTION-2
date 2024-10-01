using Azure;
using Azure.Data.Tables;
using System;

namespace PART2.Models
{
    public class CustomerProfiles : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customers";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string HomeAddress { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}


