namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class OrderAmountTotals
{
    public decimal SubTotal { get; init; }
    public decimal ShippingTotal { get; init; }
    public decimal TaxTotal { get; init; }
    public decimal FeesTotal { get; init; }
    public decimal GrandTotal { get; init; }
}