using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSystem.Domain.Events
{
    public class PaymentSucceededEvent
    {
        public Guid OrderId { get; set; }
        public DateTime PaidAtUtc { get; set; }
    }
}
