namespace Microsoft.eShopWeb.Web.ViewModels;

public class OrderDetailViewModel : OrderViewModel
{
    public List<OrderItemViewModel> OrderItems { get; set; } = new();

    // Add new properties here:
    public decimal TaxAmount { get; set; }
    public decimal TotalWithTax { get; set; }
}