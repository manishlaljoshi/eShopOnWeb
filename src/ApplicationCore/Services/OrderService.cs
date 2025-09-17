using System.Linq;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

/// <summary>
/// Service for handling business logic related to orders.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;

    /// <summary>
    /// Constructor for OrderService.
    /// </summary>
    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
    }

    /// <summary>
    /// Creates an Order from a Basket.
    /// </summary>
    /// <param name="basketId">The ID of the Basket.</param>
    /// <param name="shippingAddress">The shipping address for the order.</param>
    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        // Retrieve the Basket with items from the database.
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

        // Check if the Basket exists and contains any items.
        Guard.Against.Null(basket, nameof(basket));
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        // Retrieve all CatalogItems referenced by the Basket items.
        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        // Map each BasketItem to an OrderItem.
        var items = basket.Items.Select(basketItem =>
        {
            // Retrieve the CatalogItem from the list of retrieved CatalogItems.
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);

            // Create an OrderItem with the necessary properties.
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);

            // Return the created OrderItem.
            return orderItem;
        }).ToList();

        // Create a new Order with the necessary properties.
        var order = new Order(basket.BuyerId, shippingAddress, items);

        // Add the Order to the repository and save changes.
        await _orderRepository.AddAsync(order);
    }
}