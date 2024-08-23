using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etax_Api
{
    public class ReturnRawFile
    {
        public string name { get; set; }
        public List<ReturnFile> listFileXml { get; set; }
        public List<ReturnFile> listFilePdf { get; set; }

    }
}
