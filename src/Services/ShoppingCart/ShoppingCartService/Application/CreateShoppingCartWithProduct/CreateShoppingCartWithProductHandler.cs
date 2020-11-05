using System;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using MediatR;
using N8T.Infrastructure.App.Dtos;
using N8T.Infrastructure.Auth;
using ShoppingCartService.Domain.Gateway;
using ShoppingCartService.Domain.Service;
using ShoppingCartService.Infrastructure.Extensions;

namespace ShoppingCartService.Application.CreateShoppingCartWithProduct
{
    public class CreateShoppingCartWithProductHandler : IRequestHandler<CreateShoppingCartWithProductQuery, CartDto>
    {
        private readonly DaprClient _daprClient;
        private readonly IProductCatalogService _productCatalogService;
        private readonly IPromoGateway _promoGateway;
        private readonly IShippingGateway _shippingGateway;
        private readonly ISecurityContextAccessor _securityContextAccessor;

        public CreateShoppingCartWithProductHandler(DaprClient daprClient,
            IProductCatalogService productCatalogService,
            IPromoGateway promoGateway,
            IShippingGateway shippingGateway,
            ISecurityContextAccessor securityContextAccessor)
        {
            _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
            _productCatalogService = productCatalogService ?? throw new ArgumentNullException(nameof(productCatalogService));
            _promoGateway = promoGateway ?? throw new ArgumentNullException(nameof(promoGateway));
            _shippingGateway = shippingGateway ?? throw new ArgumentNullException(nameof(shippingGateway));
            _securityContextAccessor = securityContextAccessor ?? throw new ArgumentNullException(nameof(securityContextAccessor));
        }

        public async Task<CartDto> Handle(CreateShoppingCartWithProductQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _securityContextAccessor.UserId;
            var cart = new CartDto {UserId = currentUserId};

            await cart.InsertItemToCartAsync(request.Quantity, request.ProductId, _productCatalogService);
            await cart.CalculateCartAsync(_productCatalogService, _shippingGateway, _promoGateway);

            await _daprClient.SaveStateAsync("statestore", $"shopping-cart-{currentUserId}", cart,
                cancellationToken: cancellationToken);

            return cart;
        }
    }
}