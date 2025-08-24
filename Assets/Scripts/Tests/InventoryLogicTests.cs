using NUnit.Framework;
using Hellscape.Domain;

namespace Hellscape.Tests.EditMode
{
    [TestFixture]
    public class InventoryLogicTests
    {
        [Test]
        public void NewWithBase_CreatesCorrectInitialState()
        {
            var inv = InventoryState.NewWithBase();
            
            Assert.AreEqual(WeaponType.BasePistol, inv.s0.type);
            Assert.AreEqual(-1, inv.s0.ammo);
            Assert.AreEqual(WeaponType.None, inv.s1.type);
            Assert.AreEqual(WeaponType.None, inv.s2.type);
            Assert.AreEqual(WeaponType.None, inv.s3.type);
            Assert.AreEqual(0, inv.ActiveIndex);
        }

        [Test]
        public void ApplyPickup_SameType_MergesAmmo()
        {
            var inv = InventoryState.NewWithBase();
            inv.s1 = new WeaponSlot(WeaponType.Rifle, 10);
            
            var loot = new PickupData(WeaponType.Rifle, 5);
            var result = InventoryLogic.ApplyPickup(inv, loot, out bool dropped, out var droppedPickup);
            
            Assert.IsFalse(dropped);
            Assert.AreEqual(15, result.s1.ammo);
            Assert.AreEqual(WeaponType.Rifle, result.s1.type);
        }

        [Test]
        public void ApplyPickup_EmptySlot_TakesPickup()
        {
            var inv = InventoryState.NewWithBase();
            var loot = new PickupData(WeaponType.SMG, 20);
            
            var result = InventoryLogic.ApplyPickup(inv, loot, out bool dropped, out var droppedPickup);
            
            Assert.IsFalse(dropped);
            Assert.AreEqual(WeaponType.SMG, result.s1.type);
            Assert.AreEqual(20, result.s1.ammo);
        }

        [Test]
        public void ApplyPickup_FullInventory_ActiveBase_ReplacesSlot1()
        {
            var inv = InventoryState.NewWithBase();
            inv.s1 = new WeaponSlot(WeaponType.Rifle, 10);
            inv.s2 = new WeaponSlot(WeaponType.SMG, 15);
            inv.s3 = new WeaponSlot(WeaponType.Shotgun, 8);
            inv.ActiveIndex = 0; // Base pistol active
            
            var loot = new PickupData(WeaponType.Rifle, 5);
            var result = InventoryLogic.ApplyPickup(inv, loot, out bool dropped, out var droppedPickup);
            
            Assert.IsTrue(dropped);
            Assert.AreEqual(WeaponType.Rifle, droppedPickup.type);
            Assert.AreEqual(10, droppedPickup.ammo);
            Assert.AreEqual(WeaponType.Rifle, result.s1.type);
            Assert.AreEqual(5, result.s1.ammo);
        }

        [Test]
        public void ApplyPickup_FullInventory_ActiveNonBase_ReplacesActive()
        {
            var inv = InventoryState.NewWithBase();
            inv.s1 = new WeaponSlot(WeaponType.Rifle, 10);
            inv.s2 = new WeaponSlot(WeaponType.SMG, 15);
            inv.s3 = new WeaponSlot(WeaponType.Shotgun, 8);
            inv.ActiveIndex = 2; // SMG active
            
            var loot = new PickupData(WeaponType.Rifle, 5);
            var result = InventoryLogic.ApplyPickup(inv, loot, out bool dropped, out var droppedPickup);
            
            Assert.IsTrue(dropped);
            Assert.AreEqual(WeaponType.SMG, droppedPickup.type);
            Assert.AreEqual(15, droppedPickup.ammo);
            Assert.AreEqual(WeaponType.Rifle, result.s2.type);
            Assert.AreEqual(5, result.s2.ammo);
        }

        [Test]
        public void ApplyPickup_BasePistol_NeverDropped()
        {
            var inv = InventoryState.NewWithBase();
            inv.s1 = new WeaponSlot(WeaponType.Rifle, 10);
            inv.s2 = new WeaponSlot(WeaponType.SMG, 15);
            inv.s3 = new WeaponSlot(WeaponType.Shotgun, 8);
            inv.ActiveIndex = 0; // Base pistol active
            
            var loot = new PickupData(WeaponType.Shotgun, 5);
            var result = InventoryLogic.ApplyPickup(inv, loot, out bool dropped, out var droppedPickup);
            
            Assert.IsTrue(dropped);
            Assert.AreEqual(WeaponType.Rifle, droppedPickup.type); // Slot1 dropped, not base
            Assert.AreEqual(WeaponType.BasePistol, result.s0.type); // Base still there
        }

        [Test]
        public void SetActive_ValidIndex_SetsActive()
        {
            var inv = InventoryState.NewWithBase();
            inv.s1 = new WeaponSlot(WeaponType.Rifle, 10);
            
            var result = InventoryLogic.SetActive(inv, 1);
            
            Assert.AreEqual(1, result.ActiveIndex);
        }

        [Test]
        public void SetActive_OutOfRange_ClampsToValid()
        {
            var inv = InventoryState.NewWithBase();
            
            var result1 = InventoryLogic.SetActive(inv, -1);
            var result2 = InventoryLogic.SetActive(inv, 5);
            
            Assert.AreEqual(0, result1.ActiveIndex);
            Assert.AreEqual(3, result2.ActiveIndex);
        }

        [Test]
        public void TryConsume_InfiniteAmmo_FiresWithoutConsuming()
        {
            var inv = InventoryState.NewWithBase(); // Base pistol has infinite ammo
            
            var result = InventoryLogic.TryConsume(inv);
            
            Assert.IsTrue(result.fired);
            Assert.AreEqual(-1, result.next.s0.ammo); // Still infinite
        }

        [Test]
        public void TryConsume_FiniteAmmo_FiresAndConsumes()
        {
            var inv = InventoryState.NewWithBase();
            inv.s1 = new WeaponSlot(WeaponType.Rifle, 10);
            inv.ActiveIndex = 1;
            
            var result = InventoryLogic.TryConsume(inv);
            
            Assert.IsTrue(result.fired);
            Assert.AreEqual(9, result.next.s1.ammo);
        }

        [Test]
        public void TryConsume_EmptySlot_DoesNotFire()
        {
            var inv = InventoryState.NewWithBase();
            inv.ActiveIndex = 1; // Empty slot
            
            var result = InventoryLogic.TryConsume(inv);
            
            Assert.IsFalse(result.fired);
        }

        [Test]
        public void TryConsume_ZeroAmmo_DoesNotFire()
        {
            var inv = InventoryState.NewWithBase();
            inv.s1 = new WeaponSlot(WeaponType.Rifle, 0);
            inv.ActiveIndex = 1;
            
            var result = InventoryLogic.TryConsume(inv);
            
            Assert.IsFalse(result.fired);
            Assert.AreEqual(0, result.next.s1.ammo);
        }
    }
}
