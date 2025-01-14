using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etax_Api
{
    public class DtResult<T>
    {
        [JsonProperty("draw")]
        public int Draw { get; set; }

        [JsonProperty("recordsTotal")]
        public int RecordsTotal { get; set; }

        [JsonProperty("recordsFiltered")]
        public int RecordsFiltered { get; set; }
        [JsonProperty("data")]
        public IEnumerable<T> Data { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public string Error { get; set; }

        public string PartialView { get; set; }
    }
    public class BodyDtParameters
    {
        public int Draw { get; set; }
        public DtColumn[] Columns { get; set; }
        public DtOrder[] Order { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DtSearch Search { get; set; }
        public int id { get; set; }
        public string docType { get; set; }
        public string fileGroup { get; set; }
        public string docRdType { get; set; }
        public string statusType1 { get; set; }
        public string statusType2 { get; set; }
        public List<TaxType> taxType { get; set; }
        public List<ProcessType> processType { get; set; }
        public string dateType { get; set; }
        public DateTime dateStart { get; set; }
        public DateTime dateEnd { get; set; }
        public string searchText { get; set; }

        public bool view_self_only { get; set; }
        public bool view_branch_only { get; set; }
        public List<PermissionBranch> branchs { get; set; }
    }

    public class BodyAdminDtParameters
    {
        public int Draw { get; set; }
        public DtColumn[] Columns { get; set; }
        public DtOrder[] Order { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DtSearch Search { get; set; }
        public int id { get; set; }
        public string docType { get; set; }
        public List<FileGroup> fileGroup { get; set; }
        public string docRdType { get; set; }
        public string statusType1 { get; set; }
        public string statusType2 { get; set; }
        public List<TaxType> taxType { get; set; }
        public List<ProcessType> processType { get; set; }
        public string dateType { get; set; }
        public DateTime dateStart { get; set; }
        public DateTime dateEnd { get; set; }
        public string searchText { get; set; }

        public bool view_self_only { get; set; }
        public bool view_branch_only { get; set; }
        public List<PermissionBranch> branchs { get; set; }
    }

    public class DtColumn
    {
        public string Data { get; set; }
        public string Name { get; set; }
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DtSearch Search { get; set; }
    }

    public class DtOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; }
    }

    public class DtSearch
    {
        public string Value { get; set; }
        public bool Regex { get; set; }
    }

    public class TaxType
    {
        public string id { get; set; }
        public string text { get; set; }
    }

    public class ProcessType
    {
        public string id { get; set; }
        public string text { get; set; }
    }

    public class PermissionBranch
    {
        public int id { get; set; }
        public int member_id { get; set; }
        public int member_user_id { get; set; }
        public int branch_id { get; set; }
    }

    public class FileGroup
    {
        public string id { get; set; }
        public string text { get; set; }
    }
}
