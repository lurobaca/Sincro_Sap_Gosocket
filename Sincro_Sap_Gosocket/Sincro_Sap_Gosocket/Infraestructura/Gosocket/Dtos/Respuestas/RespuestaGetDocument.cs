using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas
{
    public class RespuestaGetDocument
    {
        [JsonProperty("Documents")]
        public List<GetDocumentItem> Documents { get; set; } = new();

        [JsonProperty("ContinuationToken")]
        public string? ContinuationToken { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }
    }

    public class GetDocumentItem
    {
        [JsonProperty("GlobalDocumentId")]
        public string? GlobalDocumentId { get; set; }

        [JsonProperty("CountryDocumentId")]
        public string? CountryDocumentId { get; set; }

        [JsonProperty("ExternalId")]
        public string? ExternalId { get; set; }

        [JsonProperty("CountryId")]
        public string? CountryId { get; set; }

        [JsonProperty("Date")]
        public DateTime? Date { get; set; }

        [JsonProperty("DocumentTypeId")]
        public int? DocumentTypeId { get; set; }

        [JsonProperty("DocumentTypeName")]
        public string? DocumentTypeName { get; set; }

        [JsonProperty("NetAmount")]
        public decimal? NetAmount { get; set; }

        [JsonProperty("FreeAmount")]
        public decimal? FreeAmount { get; set; }

        [JsonProperty("TaxAmount")]
        public decimal? TaxAmount { get; set; }

        [JsonProperty("TotalAmount")]
        public decimal? TotalAmount { get; set; }

        [JsonProperty("CurrencyType")]
        public string? CurrencyType { get; set; }

        [JsonProperty("SeriesNumber")]
        public string? SeriesNumber { get; set; }

        [JsonProperty("Series")]
        public string? Series { get; set; }

        [JsonProperty("Number")]
        public int? Number { get; set; }

        [JsonProperty("NumberStr")]
        public string? NumberStr { get; set; }

        [JsonProperty("DocumentSenderCode")]
        public string? DocumentSenderCode { get; set; }

        [JsonProperty("DocumentSenderName")]
        public string? DocumentSenderName { get; set; }

        [JsonProperty("DocumentReceiverCode")]
        public string? DocumentReceiverCode { get; set; }

        [JsonProperty("DocumentReceiverName")]
        public string? DocumentReceiverName { get; set; }

        [JsonProperty("DocumentFinancialOwnerCode")]
        public string? DocumentFinancialOwnerCode { get; set; }

        [JsonProperty("DocumentFinancialOwnerName")]
        public string? DocumentFinancialOwnerName { get; set; }

        [JsonProperty("FinancialDate")]
        public DateTime? FinancialDate { get; set; }

        [JsonProperty("EstimatedPaymentDate")]
        public DateTime? EstimatedPaymentDate { get; set; }

        [JsonProperty("DocumentTimeStamp")]
        public DateTime? DocumentTimeStamp { get; set; }

        [JsonProperty("AuthorityTimeStamp")]
        public DateTime? AuthorityTimeStamp { get; set; }

        [JsonProperty("SyncPoint")]
        public string? SyncPoint { get; set; }

        [JsonProperty("DocumentTags")]
        public List<GetDocumentTag> DocumentTags { get; set; } = new();

        [JsonProperty("TwoCheck")]
        public object? TwoCheck { get; set; }

        [JsonProperty("Notes")]
        public List<GetDocumentNote> Notes { get; set; } = new();

        [JsonProperty("Offers")]
        public object? Offers { get; set; }

        [JsonProperty("Fields")]
        public List<GetDocumentField> Fields { get; set; } = new();

        [JsonProperty("Author")]
        public object? Author { get; set; }
    }

    public class GetDocumentTag
    {
        [JsonProperty("Code")]
        public string? Code { get; set; }

        [JsonProperty("TimeStamp")]
        public DateTime? TimeStamp { get; set; }

        [JsonProperty("Value")]
        public string? Value { get; set; }
    }

    public class GetDocumentNote
    {
        [JsonProperty("Mandatory")]
        public bool Mandatory { get; set; }

        [JsonProperty("Code")]
        public string? Code { get; set; }

        [JsonProperty("Note")]
        public string? Note { get; set; }

        [JsonProperty("TimeStamp")]
        public DateTime? TimeStamp { get; set; }

        [JsonProperty("Detail")]
        public string? Detail { get; set; }

        [JsonProperty("Source")]
        public string? Source { get; set; }
    }

    public class GetDocumentField
    {
        [JsonProperty("Code")]
        public string? Code { get; set; }

        [JsonProperty("Name")]
        public string? Name { get; set; }

        [JsonProperty("Value")]
        public string? Value { get; set; }

        [JsonProperty("ExternalId")]
        public string? ExternalId { get; set; }

        [JsonProperty("Source")]
        public string? Source { get; set; }

        [JsonProperty("IsValid")]
        public bool? IsValid { get; set; }

        [JsonProperty("EditingEnabled")]
        public bool? EditingEnabled { get; set; }

        [JsonProperty("Flow")]
        public string? Flow { get; set; }

        [JsonProperty("Timestamp")]
        public DateTime? Timestamp { get; set; }

        [JsonProperty("Category")]
        public string? Category { get; set; }
    }
}