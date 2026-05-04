using _2DREPOCODE.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace _2DREPOCODE.Handlers
{
    /// <summary>
    /// Handles the Hover Cart - a mobile physics container for transporting multiple items.
    /// Manages cart creation, item storage, stability, and collision damage.
    /// Responsibility: Axel
    /// </summary>
    public class CartHandler
    {
        // === Constants ===
        /// <summary>
        /// Default maximum capacity for carts.
        /// </summary>
        private const int DEFAULT_MAX_CAPACITY = 10;

        /// <summary>
        /// Default durability for carts.
        /// </summary>
        private const int DEFAULT_MAX_DURABILITY = 50;

        /// <summary>
        /// Minimum stability before cart tips over.
        /// </summary>
        private const float TIP_THRESHOLD = 0.2f;

        /// <summary>
        /// Collision velocity threshold for applying damage.
        /// </summary>
        private const float DAMAGE_VELOCITY_THRESHOLD = 5f;

        // === Cart Registry ===
        private Dictionary<int, Cart> activeCarts;
        private int nextCartId;

        /// <summary>
        /// Initializes the CartHandler.
        /// </summary>
        public CartHandler()
        {
            activeCarts = new Dictionary<int, Cart>();
            nextCartId = 1;
        }

        // === Cart Creation & Management ===

        /// <summary>
        /// Creates a new hover cart with default properties.
        /// </summary>
        /// <returns>The newly created cart.</returns>
        public Cart CreateCart()
        {
            Cart cart = new Cart(nextCartId, DEFAULT_MAX_CAPACITY, DEFAULT_MAX_DURABILITY);
            activeCarts.Add(nextCartId, cart);
            nextCartId++;

            Console.WriteLine($"Hover Cart #{cart.CartId} created! Capacity: {cart.MaxCapacity}, Durability: {cart.MaxDurability}");
            return cart;
        }

        /// <summary>
        /// Creates a cart with custom capacity and durability.
        /// </summary>
        public Cart CreateCustomCart(int maxCapacity, int maxDurability)
        {
            Cart cart = new Cart(nextCartId, maxCapacity, maxDurability);
            activeCarts.Add(nextCartId, cart);
            nextCartId++;

            Console.WriteLine($"Custom Cart #{cart.CartId} created! Capacity: {maxCapacity}, Durability: {maxDurability}");
            return cart;
        }

        /// <summary>
        /// Retrieves a cart by its ID.
        /// </summary>
        public Cart? GetCart(int cartId)
        {
            if (activeCarts.TryGetValue(cartId, out Cart? cart))
            {
                return cart;
            }
            return null;
        }

        /// <summary>
        /// Gets all active carts.
        /// </summary>
        public List<Cart> GetAllCarts()
        {
            return new List<Cart>(activeCarts.Values);
        }

        /// <summary>
        /// Removes a cart from the registry.
        /// </summary>
        public void RemoveCart(int cartId)
        {
            if (activeCarts.Remove(cartId))
            {
                Console.WriteLine($"Cart #{cartId} has been removed.");
            }
        }

        // === Item Management ===

        /// <summary>
        /// Adds an item to the cart.
        /// Returns true if successful, false if cart is full, tipped, or destroyed.
        /// </summary>
        public bool AddItemToCart(Cart cart, Item item)
        {
            if (cart.IsFull)
            {
                Console.WriteLine($"Cart #{cart.CartId} is full! ({cart.CurrentItemCount}/{cart.MaxCapacity})");
                return false;
            }

            if (cart.IsTipped)
            {
                Console.WriteLine($"Cart #{cart.CartId} is tipped over! Cannot add items.");
                return false;
            }

            if (cart.IsDestroyed)
            {
                Console.WriteLine($"Cart #{cart.CartId} is destroyed!");
                return false;
            }

            bool success = cart.AddItem(item);
            if (success)
            {
                item.PutInCart(cart.CartId);
                Console.WriteLine($"Added {item.Name} to Cart #{cart.CartId}. Total value: {cart.TotalValue}, Mass: {cart.TotalMass:F1}");
            }

            return success;
        }

        /// <summary>
        /// Removes a specific item from the cart.
        /// </summary>
        public bool RemoveItemFromCart(Cart cart, Item item)
        {
            bool success = cart.RemoveItem(item);
            if (success)
            {
                item.RemoveFromCart();
                Console.WriteLine($"Removed {item.Name} from Cart #{cart.CartId}.");
            }
            return success;
        }

        /// <summary>
        /// Unloads all items from the cart (for extraction).
        /// Returns the list of items.
        /// </summary>
        public List<Item> UnloadCart(Cart cart)
        {
            List<Item> items = cart.UnloadAll();

            // Mark all items as no longer in cart
            foreach (Item item in items)
            {
                item.RemoveFromCart();
            }

            Console.WriteLine($"Unloaded {items.Count} items from Cart #{cart.CartId}. Total value: {cart.TotalValue}");
            return items;
        }

        // === Physics & Collision ===

        /// <summary>
        /// Updates cart physics per frame.
        /// Handles stability calculation and tipping checks.
        /// </summary>
        public void UpdateCart(Cart cart, float deltaTime)
        {
            if (cart.IsDestroyed || cart.IsTipped)
            {
                return;
            }

            // Update stability based on movement
            cart.UpdateStability(deltaTime);

            // Check if cart tipped
            if (cart.IsTipped)
            {
                OnCartTipped(cart);
            }
        }

        /// <summary>
        /// Handles collision damage when cart hits something.
        /// Call this when a collision is detected in Unity's OnCollisionEnter2D.
        /// </summary>
        /// <param name="cart">The cart that collided.</param>
        /// <param name="impactVelocity">The relative velocity of the collision.</param>
        public void HandleCollision(Cart cart, float impactVelocity)
        {
            if (cart.IsDestroyed || cart.IsTipped)
            {
                return;
            }

            // Only apply damage if collision is strong enough
            if (impactVelocity < DAMAGE_VELOCITY_THRESHOLD)
            {
                return;
            }

            // Apply collision damage
            cart.TakeCollisionDamage(impactVelocity);

            Console.WriteLine($"Cart #{cart.CartId} collided! Velocity: {impactVelocity:F1}, Damage: {cart.DamageTaken}/{cart.MaxDurability}, Stability: {cart.Stability:F2}");

            // Check if cart is destroyed
            if (cart.IsDestroyed)
            {
                OnCartDestroyed(cart);
            }

            // Check if cart tipped
            if (cart.IsTipped)
            {
                OnCartTipped(cart);
            }

            // Also apply depreciation to items inside based on collision
            foreach (Item item in cart.ContainedItems)
            {
                item.ApplyCollisionDamage(impactVelocity * 0.5f); // Items take reduced damage when in cart
            }
        }

        /// <summary>
        /// Handles cart tipping event - items spill out!
        /// </summary>
        private void OnCartTipped(Cart cart)
        {
            Console.WriteLine($"⚠ Cart #{cart.CartId} has TIPPED OVER! Items spilled!");

            // In Unity, you would spawn the items on the ground around the cart
            // For now, we just clear the cart
            List<Item> spilledItems = cart.UnloadAll();

            foreach (Item item in spilledItems)
            {
                item.RemoveFromCart();
                // In Unity: Set item position near cart with random scatter
                // item.X = cart.X + random offset
                // item.Y = cart.Y + random offset
                Console.WriteLine($"  {item.Name} spilled! (Value: {item.CurrentValue})");
            }
        }

        /// <summary>
        /// Handles cart destruction event - everything is lost!
        /// </summary>
        private void OnCartDestroyed(Cart cart)
        {
            Console.WriteLine($"💥 Cart #{cart.CartId} has been DESTROYED!");

            // Destroy all items inside (or scatter them as debris)
            foreach (Item item in cart.ContainedItems)
            {
                Console.WriteLine($"  {item.Name} was destroyed in the wreckage!");
            }

            cart.UnloadAll();
        }

        // === Tethering Management ===

        /// <summary>
        /// Starts tethering a cart to a player's Grabber Tool.
        /// </summary>
        public bool TetherCart(Cart cart, Player player)
        {
            if (cart.IsDestroyed)
            {
                Console.WriteLine($"Cart #{cart.CartId} is destroyed and cannot be tethered!");
                return false;
            }

            if (cart.IsTipped)
            {
                Console.WriteLine($"Cart #{cart.CartId} is tipped over and cannot be tethered!");
                return false;
            }

            cart.StartTether(player.PlayerId);
            Console.WriteLine($"{player.Name} is now pulling Cart #{cart.CartId} (Effective Mass: {cart.GetEffectiveMass():F1})");
            return true;
        }

        /// <summary>
        /// Stops tethering a cart from a player.
        /// </summary>
        public void UntetherCart(Cart cart, Player player)
        {
            cart.StopTether(player.PlayerId);
            Console.WriteLine($"{player.Name} stopped pulling Cart #{cart.CartId}");
        }

        // === Cart Status ===

        /// <summary>
        /// Gets a formatted status string for a cart.
        /// </summary>
        public string GetCartStatus(Cart cart)
        {
            StringBuilder status = new StringBuilder();
            status.AppendLine($"=== Hover Cart #{cart.CartId} ===");
            status.AppendLine($"Position: ({cart.X:F1}, {cart.Y:F1})");
            status.AppendLine($"Items: {cart.CurrentItemCount}/{cart.MaxCapacity}");
            status.AppendLine($"Total Value: {cart.TotalValue} SURPLUS");
            status.AppendLine($"Total Mass: {cart.TotalMass:F1}");
            status.AppendLine($"Durability: {cart.DamageTaken}/{cart.MaxDurability}");
            status.AppendLine($"Stability: {cart.Stability:F2}");

            if (cart.IsBeingTethered)
            {
                status.AppendLine($"🔗 Being pulled by Player #{cart.TetheredByPlayerId} (+{cart.PlayersTetheringCount - 1} helpers)");
                status.AppendLine($"   Effective Mass: {cart.GetEffectiveMass():F1}");
            }

            if (cart.IsTipped)
                status.AppendLine("⚠ TIPPED OVER");
            if (cart.IsDestroyed)
                status.AppendLine("💥 DESTROYED");

            if (cart.ContainedItems.Count > 0)
            {
                status.AppendLine("\nContents:");
                foreach (Item item in cart.ContainedItems)
                {
                    status.AppendLine($"  - {item.Name} ({item.CurrentValue} SURPLUS, {item.Mass:F1} mass)");
                }
            }

            return status.ToString();
        }

        /// <summary>
        /// Prints cart status to console.
        /// </summary>
        public void PrintCartStatus(Cart cart)
        {
            Console.WriteLine(GetCartStatus(cart));
        }

        /// <summary>
        /// Calculates total value of all items in all carts.
        /// </summary>
        public int GetTotalCartValue()
        {
            int total = 0;
            foreach (Cart cart in activeCarts.Values)
            {
                total += cart.TotalValue;
            }
            return total;
        }

        /// <summary>
        /// Repairs a cart (restores durability).
        /// </summary>
        public void RepairCart(Cart cart, int repairAmount)
        {
            int previousDamage = cart.DamageTaken;
            cart.DamageTaken -= repairAmount;
            if (cart.DamageTaken < 0) cart.DamageTaken = 0;

            int actualRepair = previousDamage - cart.DamageTaken;
            Console.WriteLine($"Cart #{cart.CartId} repaired for {actualRepair} points! Durability: {cart.MaxDurability - cart.DamageTaken}/{cart.MaxDurability}");
        }

        /// <summary>
        /// Resets a tipped cart (if you have a special item/ability to do so).
        /// </summary>
        public void RightCart(Cart cart)
        {
            if (cart.IsTipped && !cart.IsDestroyed)
            {
                cart.IsTipped = false;
                cart.Stability = 0.5f; // Partially stable after righting
                Console.WriteLine($"Cart #{cart.CartId} has been righted!");
            }
        }

        /// <summary>
        /// Resets all carts for a new run.
        /// </summary>
        public void ResetAllCarts()
        {
            foreach (Cart cart in activeCarts.Values)
            {
                cart.UnloadAll();
                cart.DamageTaken = 0;
                cart.IsTipped = false;
                cart.Stability = 1.0f;
                cart.IsBeingTethered = false;
                cart.TetheredByPlayerId = -1;
                cart.PlayersTetheringCount = 0;
            }

            Console.WriteLine("All carts have been reset!");
        }

        /// <summary>
        /// Clears all carts (for returning to main menu).
        /// </summary>
        public void ClearAllCarts()
        {
            activeCarts.Clear();
            nextCartId = 1;
            Console.WriteLine("All carts cleared.");
        }
    }
}
