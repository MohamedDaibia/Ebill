using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EBill.Models
{
    // Mirrors Stock Management's own Settings model (StockManagement.Models.DbModels
    // .Settings) but for the reverse direction: one row per Ebill company, recording
    // the Stock Management company connected to it and the refresh token Stock
    // Management issued so Ebill can call Stock Management's API.
    //
    // Populated in two stages, same as the original connect flow it mirrors:
    //  1. StockCompanyId is set as soon as Stock Management's "Sync Ebill" redirect
    //     hands it over (see EbillConnectService.IssueTokenAndBuildRedirectUrlAsync).
    //  2. RefreshToken/IsSynced are set slightly later, once Stock Management's own
    //     server calls back here with a freshly issued token for it (see
    //     StockSyncController.RegisterRefreshToken) — Stock Management does this
    //     itself right after ConnectEbill succeeds on its side.
    public class Settings : IAuditable
    {
        public int Id { get; set; }

        [ValidateNever]
        public int CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        [ValidateNever]
        public Company? Company { get; set; }

        [MaxLength(100)]
        [ValidateNever]
        public string? StockCompanyId { get; set; }

        // Issued by Stock Management's Token endpoint - stored plaintext here, same
        // convention Stock Management uses for Ebill's own refresh token (see that
        // app's Settings.RefreshToken). Redeemed for a short-lived access token on
        // each call rather than used directly.
        [ValidateNever]
        public string? RefreshToken { get; set; }

        [ValidateNever]
        public bool IsSynced { get; set; } = false;

        // Document auto-numbering. Each is the NEXT number to hand out for that
        // document type - null means auto-numbering isn't configured yet for it, in
        // which case the document is saved with whatever the caller left it as
        // (currently that means the number field is simply left blank, since none of
        // Invoice/Quotation/PurchaseOrder creation collects one from the user).
        // Set by SettingsController.UpdateNumbering; consumed and incremented by
        // InvoiceController/QuotationController when a new document is created.
        [Range(1, int.MaxValue, ErrorMessage = "Invoice number must be a positive number.")]
        [Display(Name = "Start With Invoice Number")]
        public int? NextInvoiceNumber { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quotation number must be a positive number.")]
        [Display(Name = "Start With Quotation Number")]
        public int? NextQuotationNumber { get; set; }

        // Not consumed anywhere yet - there's no Purchase Order document type in the
        // app currently, so this is stored for when that feature exists.
        [Range(1, int.MaxValue, ErrorMessage = "Purchase order number must be a positive number.")]
        [Display(Name = "Start With Purchase Order Number")]
        public int? NextPurchaseOrderNumber { get; set; }

        // Rich-text (HTML) footer printed at the bottom of every invoice. Written by
        // the Invoice Footprint editor on the Settings page and always sanitized
        // server-side (see SettingsController.UpdateInvoiceFootprint) before it is
        // stored, so it is safe to render with Html.Raw.
        [ValidateNever]
        public string? InvoiceFootprint { get; set; }

        [ValidateNever]
        public DateTime CreatedDate { get; set; }

        [ValidateNever]
        public DateTime UpdatedDate { get; set; }
    }
}
