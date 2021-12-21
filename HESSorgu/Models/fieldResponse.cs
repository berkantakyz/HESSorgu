using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HESSorgu.Models
{
    public class fieldResponse
    {

        public string entityName { get; set; }
        public string errorKey { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public string status { get; set; }
        public string message { get; set; }

        //public string params { get; set; }
      
    }
}