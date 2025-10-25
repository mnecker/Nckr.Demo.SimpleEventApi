using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nckr.Demo.SimpleEventApi.Models
{
	public class EventForm : ITableEntity
	{
		public string PartitionKey { get; set; }
		public string RowKey { get; set; }
		public DateTimeOffset? Timestamp { get; set; }
		public ETag ETag { get; set; }
		public string EventId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Format { get; set; }
	}
}
