using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nckr.Demo.SimpleEventApi.Models
{
	public class EventFormSubmission : ITableEntity
	{
		public string PartitionKey { get; set; }
		public string RowKey { get; set; }
		public DateTimeOffset? Timestamp { get; set; }
		public ETag ETag { get; set; }
		public string FormId { get; set; }
		public string ResponseData { get; set; }
	}
}
