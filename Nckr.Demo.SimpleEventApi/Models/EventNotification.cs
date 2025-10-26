using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nckr.Demo.SimpleEventApi.Models
{
	public class EventNotification
	{
		public string EventId { get; set; }
		public string Topic { get; set; }
		public string Content { get; set; }
	}
}
