using Core.Domain.Purchases;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Domain
{
    [Table("VendorContact")]
    public class VendorContact : BaseEntity
    {
        //public int Id { get; set; }
        public int ContactId { get; set; }

        public int VendorId { get; set; }

        [ForeignKey("VendorId")]
        public virtual Vendor Vendor { get; set; }

        [ForeignKey("ContactId")]
        public virtual Contact Contact { get; set; }
    }
}
