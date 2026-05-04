using _2DREPOCODE.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace _2DREPOCODE.Models
{
    /// <summary>
    /// Represents the Hover Cart - a mobile physics container for transporting multiple items.
    /// In Unity, this will have a Rigidbody2D and can be tethered/dragged by players.
    /// </summary>
    public class Cart
    {
        // === Identity ===
        /// <summary>
        /// Unique identifier for this cart.
        /// </summary>
        public int CartId { get; set; }

        // === Position & Physics ===
        /// <summary>
        /// Current X coordinate on the grid.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Current Y coordinate on the grid.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Velocity X component (for physics simulation).
        /// </summary>
        public float VelocityX { get; set; }

        /// <summary>
        /// Velocity Y component (for physics simulation).
        /// </summary>
        public float VelocityY { get; set; }

        /// <summary>
        /// Base mass of the empty cart (in physics units).
        /// Total mass increases with items added.
        /// </summary>
        public float BaseMass { get; set; }

        /// <summary>
        /// Current total mass (base + all contained items).
        /// Affects how hard it is to push/pull the cart.
        /// </summary>
        public float TotalMass { get; set; }

        // === Capacity ===
        /// <summary>
        /// Maximum number of items the cart can hold.
        /// </summary>
        public int MaxCapacity { get; set; }

        /// <summary>
        /// Current number of items in the cart.
        /// </summary>
        public int CurrentItemCount { get; set; }

        /// <summary>
        /// List of items currently stored in the cart.
        /// </summary>
        public List<Item> ContainedItems { get; set; }

        /// <summary>
        /// Returns true if the cart is full.
        /// </summary>
        public bool IsFull => CurrentItemCount >= MaxCapacity;

        /// <summary>
        /// Returns true if the cart is empty.
        /// </summary>
        public bool IsEmpty => CurrentItemCount == 0;

        // === Tethering ===
        /// <summary>
        /// Whether the cart is currently being tethered/pulled by a player.
        /// </summary>
        public bool IsBeingTethered { get; set; }

        /// <summary>
        /// The player ID currently tethering this cart (or -1 if none).
        /// </summary>
        public int TetheredByPlayerId { get; set; }

        /// <summary>
        /// Number of players currently pulling this cart simultaneously.
        /// Multiple players reduce the effective mass for easier movement.
        /// </summary>
        public int PlayersTetheringCount { get; set; }

        // === State ===
        /// <summary>
        /// Whether the cart has tipped over (unrecoverable, items spill out).
        /// </summary>
        public bool IsTipped { get; set; }

        /// <summary>
        /// Stability factor (0.0 = completely unstable, 1.0 = stable).
        /// Decreases with high velocity, sharp turns, or uneven item distribution.
        /// If it drops too low, the cart tips over.
        /// </summary>
        public float Stability { get; set; }

        /// <summary>
        /// Damage taken from collisions. If too high, cart breaks.
        /// </summary>
        public int DamageTaken { get; set; }

        /// <summary>
        /// Maximum damage before cart breaks.
        /// </summary>
        public int MaxDurability { get; set; }

        /// <summary>
        /// Returns true if cart is destroyed.
        /// </summary>
        public bool IsDestroyed => DamageTaken >= MaxDurability;

        // === Value Tracking ===
        /// <summary>
        /// Total monetary value of all items in the cart.
        /// </summary>
        public int TotalValue { get; set; }

        // === Constructor ===
        /// <summary>
        /// Creates a new empty hover cart with default properties.
        /// </summary>
        public Cart()
        {
            CartId = 0;
            X = 0;
            Y = 0;
            VelocityX = 0;
            VelocityY = 0;

            BaseMass = 10f; // Base cart weight
            TotalMass = BaseMass;

            MaxCapacity = 10; // Can hold up to 10 items
            CurrentItemCount = 0;
            ContainedItems = new List<Item>();

            IsBeingTethered = false;
            TetheredByPlayerId = -1;
            PlayersTetheringCount = 0;

            IsTipped = false;
            Stability = 1.0f; // Fully stable initially
            DamageTaken = 0;
            MaxDurability = 50; // Can take 50 damage before breaking

            TotalValue = 0;
        }

        /// <summary>
        /// Creates a cart with custom capacity and durability.
        /// </summary>
        public Cart(int cartId, int maxCapacity, int maxDurability)
        {
            CartId = cartId;
            X = 0;
            Y = 0;
            VelocityX = 0;
            VelocityY = 0;

            BaseMass = 10f;
            TotalMass = BaseMass;

            MaxCapacity = maxCapacity;
            CurrentItemCount = 0;
            ContainedItems = new List<Item>();

            IsBeingTethered = false;
            TetheredByPlayerId = -1;
            PlayersTetheringCount = 0;

            IsTipped = false;
            Stability = 1.0f;
            DamageTaken = 0;
            MaxDurability = maxDurability;

            TotalValue = 0;
        }

        // === Item Management ===

        /// <summary>
        /// Adds an item to the cart if there's space.
        /// Updates total mass and value.
        /// Returns true if successful.
        /// </summary>
        public bool AddItem(Item item)
        {
            if (IsFull || IsTipped || IsDestroyed)
            {
                return false;
            }

            ContainedItems.Add(item);
            CurrentItemCount++;
            TotalMass += item.Mass;
            TotalValue += item.CurrentValue;

            return true;
        }

        /// <summary>
        /// Removes a specific item from the cart.
        /// Updates total mass and value.
        /// Returns true if item was found and removed.
        /// </summary>
        public bool RemoveItem(Item item)
        {
            if (ContainedItems.Remove(item))
            {
                CurrentItemCount--;
                TotalMass -= item.Mass;
                TotalValue -= item.CurrentValue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes and returns all items from the cart (for extraction).
        /// </summary>
        public List<Item> UnloadAll()
        {
            List<Item> items = new List<Item>(ContainedItems);
            ContainedItems.Clear();
            CurrentItemCount = 0;
            TotalMass = BaseMass;
            TotalValue = 0;
            return items;
        }

        // === Physics & Collision ===

        /// <summary>
        /// Applies damage from a collision based on impact velocity.
        /// High-velocity collisions damage the cart and may tip it over.
        /// </summary>
        public void TakeCollisionDamage(float impactVelocity)
        {
            // Damage calculation based on impact force
            int damage = (int)(impactVelocity * 2f);
            DamageTaken += damage;

            // High-speed impacts reduce stability
            if (impactVelocity > 5f)
            {
                Stability -= 0.2f;
                if (Stability < 0.3f)
                {
                    TipOver();
                }
            }
        }

        /// <summary>
        /// Tips the cart over, spilling all contents.
        /// This is catastrophic - all items are lost!
        /// </summary>
        public void TipOver()
        {
            if (!IsTipped)
            {
                IsTipped = true;
                Stability = 0f;
                // Items spill out and can be individually recovered
                // (Implementation note: scatter items on the ground in Unity)
            }
        }

        /// <summary>
        /// Updates stability based on current velocity and load distribution.
        /// Called per frame in Unity.
        /// </summary>
        public void UpdateStability(float deltaTime)
        {
            // Calculate speed
            float speed = (float)Math.Sqrt(VelocityX * VelocityX + VelocityY * VelocityY);

            // High speeds reduce stability
            if (speed > 10f)
            {
                Stability -= 0.1f * deltaTime;
            }
            else if (speed < 2f)
            {
                // Slow movement allows stability to recover slightly
                Stability += 0.05f * deltaTime;
            }

            // Heavy loads reduce stability
            if (TotalMass > 50f)
            {
                Stability -= 0.05f * deltaTime;
            }

            // Clamp stability between 0 and 1
            Stability = Math.Clamp(Stability, 0f, 1f);

            // Check for tipping
            if (Stability < 0.2f && !IsTipped)
            {
                TipOver();
            }
        }

        // === Tethering ===

        /// <summary>
        /// Starts tethering by a player.
        /// </summary>
        public void StartTether(int playerId)
        {
            if (!IsBeingTethered)
            {
                IsBeingTethered = true;
                TetheredByPlayerId = playerId;
                PlayersTetheringCount = 1;
            }
            else
            {
                // Additional player helping to pull
                PlayersTetheringCount++;
            }
        }

        /// <summary>
        /// Stops tethering by a player.
        /// </summary>
        public void StopTether(int playerId)
        {
            PlayersTetheringCount--;
            if (PlayersTetheringCount <= 0)
            {
                IsBeingTethered = false;
                TetheredByPlayerId = -1;
                PlayersTetheringCount = 0;
            }
        }

        /// <summary>
        /// Calculates effective mass considering multiple players pulling.
        /// More players = feels lighter to pull.
        /// </summary>
        public float GetEffectiveMass()
        {
            if (PlayersTetheringCount <= 0) return TotalMass;
            return TotalMass / PlayersTetheringCount;
        }

        /// <summary>
        /// Returns a string representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"Cart({CartId}) Items:{CurrentItemCount}/{MaxCapacity} Value:{TotalValue} Mass:{TotalMass:F1} Stability:{Stability:F2}";
        }
    }
}
