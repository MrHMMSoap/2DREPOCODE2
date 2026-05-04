using _2DREPOCODE.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace _2DREPOCODE.Models
{
    /// <summary>
    /// Represents a loot item that can be collected and extracted.
    /// Items have physics properties and their value depreciates with collisions.
    /// NOTE: This is primarily Michael's responsibility (ItemHandler).
    /// </summary>
    public class Item
    {
        // === Identity ===
        /// <summary>
        /// Unique identifier for this item instance.
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// Display name of the item (e.g., "Ancient Vase", "Radioactive Canister").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type classification of the item (affects physics and behavior).
        /// </summary>
        public ItemType Type { get; set; }

        // === Position & Physics ===
        /// <summary>
        /// Current X coordinate.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Current Y coordinate.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Mass of the item (affects how hard it is to drag).
        /// Heavy items require multiple players to move efficiently.
        /// </summary>
        public float Mass { get; set; }

        /// <summary>
        /// Velocity X component (for physics simulation).
        /// </summary>
        public float VelocityX { get; set; }

        /// <summary>
        /// Velocity Y component (for physics simulation).
        /// </summary>
        public float VelocityY { get; set; }

        // === Value System ===
        /// <summary>
        /// Original monetary value when first spawned.
        /// </summary>
        public int BaseValue { get; set; }

        /// <summary>
        /// Current monetary value (decreases with damage/collisions).
        /// This is what the player earns when extracting.
        /// </summary>
        public int CurrentValue { get; set; }

        /// <summary>
        /// Value depreciation rate per collision (as a percentage, 0.0-1.0).
        /// Example: 0.1 = loses 10% value per collision.
        /// </summary>
        public float DepreciationRate { get; set; }

        // === Durability ===
        /// <summary>
        /// Whether this item is breakable (Fragile type items).
        /// </summary>
        public bool IsBreakable { get; set; }

        /// <summary>
        /// Current durability/health of the item.
        /// </summary>
        public int CurrentDurability { get; set; }

        /// <summary>
        /// Maximum durability.
        /// </summary>
        public int MaxDurability { get; set; }

        /// <summary>
        /// Returns true if the item is destroyed/shattered.
        /// </summary>
        public bool IsDestroyed => CurrentDurability <= 0;

        // === State ===
        /// <summary>
        /// Whether this item is currently being tethered by a player.
        /// </summary>
        public bool IsBeingHeld { get; set; }

        /// <summary>
        /// ID of the player holding this item (-1 if none).
        /// </summary>
        public int HeldByPlayerId { get; set; }

        /// <summary>
        /// Whether this item is inside a cart.
        /// </summary>
        public bool IsInCart { get; set; }

        /// <summary>
        /// ID of the cart containing this item (-1 if not in a cart).
        /// </summary>
        public int ContainedInCartId { get; set; }

        /// <summary>
        /// Whether this item has been extracted (deposited at extraction point).
        /// </summary>
        public bool IsExtracted { get; set; }

        // === Special Properties (for specific item types) ===
        /// <summary>
        /// For Hazardous items - damage per second when held.
        /// </summary>
        public int HazardDamagePerSecond { get; set; }

        /// <summary>
        /// For Artifact items - special effect identifier (handled by ItemHandler).
        /// </summary>
        public string? SpecialEffect { get; set; }

        // === Sprite/Rendering ===
        /// <summary>
        /// Sprite index for rendering in Unity.
        /// </summary>
        public int SpriteIndex { get; set; }

        // === Constructor ===
        /// <summary>
        /// Creates a standard item with default values.
        /// </summary>
        public Item()
        {
            ItemId = 0;
            Name = "Unknown Item";
            Type = ItemType.Standard;

            X = 0;
            Y = 0;
            Mass = 5f;
            VelocityX = 0;
            VelocityY = 0;

            BaseValue = 100;
            CurrentValue = BaseValue;
            DepreciationRate = 0.05f; // 5% per collision

            IsBreakable = false;
            CurrentDurability = 100;
            MaxDurability = 100;

            IsBeingHeld = false;
            HeldByPlayerId = -1;
            IsInCart = false;
            ContainedInCartId = -1;
            IsExtracted = false;

            HazardDamagePerSecond = 0;
            SpecialEffect = null;
            SpriteIndex = 0;
        }

        /// <summary>
        /// Creates an item with specific properties.
        /// </summary>
        public Item(int itemId, string name, ItemType type, int baseValue, float mass)
        {
            ItemId = itemId;
            Name = name;
            Type = type;

            X = 0;
            Y = 0;
            Mass = mass;
            VelocityX = 0;
            VelocityY = 0;

            BaseValue = baseValue;
            CurrentValue = BaseValue;

            // Set properties based on item type
            switch (type)
            {
                case ItemType.Standard:
                    DepreciationRate = 0.05f;
                    IsBreakable = false;
                    MaxDurability = 100;
                    break;

                case ItemType.Fragile:
                    DepreciationRate = 0.15f; // High depreciation
                    IsBreakable = true;
                    MaxDurability = 20; // Breaks easily
                    break;

                case ItemType.Heavy:
                    DepreciationRate = 0.02f; // Low depreciation (sturdy)
                    IsBreakable = false;
                    MaxDurability = 200;
                    break;

                case ItemType.Lightweight:
                    DepreciationRate = 0.08f;
                    IsBreakable = false;
                    MaxDurability = 50;
                    break;

                case ItemType.Hazardous:
                    DepreciationRate = 0.05f;
                    IsBreakable = false;
                    MaxDurability = 100;
                    HazardDamagePerSecond = 5; // Damages holder
                    break;

                case ItemType.Artifact:
                    DepreciationRate = 0.01f; // Very low (precious)
                    IsBreakable = false;
                    MaxDurability = 150;
                    break;
            }

            CurrentDurability = MaxDurability;

            IsBeingHeld = false;
            HeldByPlayerId = -1;
            IsInCart = false;
            ContainedInCartId = -1;
            IsExtracted = false;

            SpecialEffect = null;
            SpriteIndex = (int)type;
        }

        // === Helper Methods ===

        /// <summary>
        /// Applies depreciation from a collision based on impact velocity.
        /// Returns true if item was destroyed.
        /// </summary>
        public bool ApplyCollisionDamage(float impactVelocity)
        {
            // Calculate damage based on velocity
            int damage = (int)(impactVelocity * Mass / 2f);
            CurrentDurability -= damage;

            // Apply value depreciation
            int valueLoss = (int)(CurrentValue * DepreciationRate);
            CurrentValue -= valueLoss;
            if (CurrentValue < 0) CurrentValue = 0;

            // Fragile items break on high-velocity impacts
            if (IsBreakable && impactVelocity > 10f)
            {
                CurrentDurability = 0; // Instant shatter
                CurrentValue = 0; // Worthless when broken
                return true;
            }

            return IsDestroyed;
        }

        /// <summary>
        /// Marks item as being held by a player.
        /// </summary>
        public void PickUp(int playerId)
        {
            IsBeingHeld = true;
            HeldByPlayerId = playerId;
            IsInCart = false;
            ContainedInCartId = -1;
        }

        /// <summary>
        /// Releases item from player's grasp.
        /// </summary>
        public void Drop()
        {
            IsBeingHeld = false;
            HeldByPlayerId = -1;
        }

        /// <summary>
        /// Puts item into a cart.
        /// </summary>
        public void PutInCart(int cartId)
        {
            IsBeingHeld = false;
            HeldByPlayerId = -1;
            IsInCart = true;
            ContainedInCartId = cartId;
        }

        /// <summary>
        /// Removes item from cart.
        /// </summary>
        public void RemoveFromCart()
        {
            IsInCart = false;
            ContainedInCartId = -1;
        }

        /// <summary>
        /// Marks item as extracted (successfully deposited).
        /// </summary>
        public void Extract()
        {
            IsExtracted = true;
            IsBeingHeld = false;
            IsInCart = false;
        }

        /// <summary>
        /// Returns a string representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"Item({ItemId}) {Name} Type:{Type} Value:{CurrentValue}/{BaseValue} Mass:{Mass:F1}";
        }
    }
}
