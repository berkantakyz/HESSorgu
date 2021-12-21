using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HESSorgu.Models
{
    public class checkEmployee
    {
        public DateTime expiration_date { get; set; }

        public string current_health_status { get; set; }

        public string masked_firstname { get; set; }

        public string masked_identity_number { get; set; } 

        public string masked_lastname { get; set; }
    
    }
}