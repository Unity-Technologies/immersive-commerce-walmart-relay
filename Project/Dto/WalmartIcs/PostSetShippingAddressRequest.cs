using System.Text.Json.Serialization;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class PostSetShippingAddressRequest
{
    [JsonPropertyName("contractId")]
    public string ContractId { get; init; } = string.Empty;
    
    [JsonPropertyName("addressId")]
    public string AddressId { get; init; } = string.Empty;
}