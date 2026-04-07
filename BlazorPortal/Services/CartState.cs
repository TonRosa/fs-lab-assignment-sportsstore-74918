namespace BlazorPortal.Services;

public class CartState
{
    public List<CartItem> Items { get; private set; } = new();
    public event Action? OnChange;

    public void AddItem(ProductDto product, int quantity = 1)
    {
        var existing = Items.FirstOrDefault(i => i.Product.Id == product.Id);
        if (existing != null)
            existing.Quantity += quantity;
        else
            Items.Add(new CartItem { Product = product, Quantity = quantity });
        OnChange?.Invoke();
    }

    public void RemoveItem(long productId)
    {
        Items.RemoveAll(i => i.Product.Id == productId);
        OnChange?.Invoke();
    }

    public void Clear()
    {
        Items.Clear();
        OnChange?.Invoke();
    }

    public decimal Total => Items.Sum(i => i.Product.Price * i.Quantity);
    public int Count => Items.Sum(i => i.Quantity);
}