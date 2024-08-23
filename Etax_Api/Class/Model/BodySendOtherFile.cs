using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class BodySendOtherFile
    {
        public string name { get; set; }
        public string data { get; set; }
        public double size { get; set; }
    }
}
