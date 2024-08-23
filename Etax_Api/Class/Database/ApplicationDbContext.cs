using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etax_Api
{
    public class ApplicationDbContext : DbContext
    {
        private readonly string _connectionString;
        public ApplicationDbContext(IConfiguration _config)
        {
            _connectionString = _config["ConnectionStrings:Default"];
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_connectionString);
            }
        }

        public DbSet<Province> province { get; set; }
        public DbSet<Amphoe> amphoe { get; set; }
        public DbSet<District> district { get; set; }
        public DbSet<User> users { get; set; }
        public DbSet<Member> members { get; set; }
        public DbSet<Branch> branchs { get; set; }
        public DbSet<MemberUser> member_users { get; set; }
        public DbSet<MemberUserPermission> member_user_permission { get; set; }
        public DbSet<RawDataFile> rawdata_files { get; set; }
        public DbSet<EtaxFile> etax_files { get; set; }
        public DbSet<EtaxFileItem> etax_file_items { get; set; }
        public DbSet<SendEbxml> send_ebxml { get; set; }
        public DbSet<SendEmail> send_email { get; set; }
        public DbSet<SendSms> send_sms { get; set; }
        public DbSet<MemberPriceXml> member_price_xml { get; set; }
        public DbSet<MemberPricePdf> member_price_pdf { get; set; }
        public DbSet<MemberPriceEmail> member_price_email { get; set; }
        public DbSet<MemberPriceSms> member_price_sms { get; set; }
        public DbSet<MemberPriceEbxml> member_price_ebxml { get; set; }
        public DbSet<Contact> contact { get; set; }
        public DbSet<RequestEtax> request_etax { get; set; }
        public DbSet<ResponsEmail> response_email { get; set; }
        public DbSet<DocumentType> document_type { get; set; }
        public DbSet<MemberDocumentType> member_document_type { get; set; }
        public DbSet<MemberToken> member_token { get; set; }
        public DbSet<Cpa> cpa { get; set; }
        public DbSet<SendEmailAttachFile> send_email_attach_files { get; set; }
        public DbSet<MemberProblem> member_problem { get; set; }
        public DbSet<EtaxOutsourceFile> etax_outsource_files { get; set; }
        public DbSet<OtherReports> other_reports { get; set; }
        public DbSet<MemberUserBranch> member_user_branch { get; set; }


        public DbSet<ViewMemberUser> view_member_users { get; set; }
        public DbSet<ViewRawFile> view_rawdata_files { get; set; }
        public DbSet<ViewEtaxFile> view_etax_files { get; set; }
        public DbSet<ViewSendEmail> view_send_email { get; set; }
        public DbSet<ViewSendEmailList> view_send_email_list { get; set; }
        public DbSet<ViewSendSms> view_send_sms { get; set; }
        public DbSet<ViewSendEbxml> view_send_ebxml { get; set; }
        public DbSet<ViewMemberDocumentType> view_member_document_type { get; set; }
        public DbSet<ViewEtaxCountDay> view_etax_count_day { get; set; }
        public DbSet<ViewEtaxCountMonth> view_etax_count_month { get; set; }
        public DbSet<ViewEtaxPrice> view_etax_price { get; set; }
        public DbSet<ViewPaymentRawData> view_payment_rawdata { get; set; }
        public DbSet<ViewPayment> view_payment { get; set; }
        public DbSet<ViewTaxReport> view_tex_report { get; set; }
        public DbSet<ViewTaxCsvReport> view_tex_csv_report { get; set; }
        public DbSet<ViewTotalReport> view_total_report { get; set; }
        public DbSet<ViewMemberProblem> view_member_problem { get; set; }
        public DbSet<ViewTaxReportOutsource> view_tax_report_outsource { get; set; }


        public DbSet<LogMemberUser> log_member_users { get; set; }
        public DbSet<LogBranch> log_branchs { get; set; }
        public DbSet<LogRawFile> log_rawdata_files { get; set; }
        public DbSet<LogEtaxFile> log_etax_files { get; set; }
        public DbSet<LogSendEmail> log_send_email { get; set; }


        public DbSet<Setting> setting { get; set; }



        public DbSet<ViewEtaxFileNew> view_etax_files_new { get; set; }
        public DbSet<ViewSendEmailNew> view_send_email_new { get; set; }
    }
}
