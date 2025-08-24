namespace Hellscape.Domain {
  public static class InventoryLogic {
    public static InventoryState ApplyPickup(in InventoryState inv, in PickupData loot,
      out bool dropped, out PickupData droppedPickup)
    {
      dropped = false; droppedPickup = default;
      if (loot.type == WeaponType.None || loot.ammo <= 0) return inv;

      // Merge if same-type exists in slots 1..3
      for (int i=1;i<=3;i++){
        var s = inv.Get(i);
        if (s.type == loot.type){
          if (!s.IsInfinite) s.ammo += loot.ammo;
          var outInv = inv; outInv.Set(i, s);
          return outInv;
        }
      }
      // Place in free slot
      for (int i=1;i<=3;i++){
        if (inv.Get(i).IsEmpty){
          var outInv = inv; outInv.Set(i, new WeaponSlot(loot.type, loot.ammo));
          return outInv;
        }
      }
      // Full: replace target (active unless base; if base active, use slot1)
      int target = (inv.ActiveIndex == 0) ? 1 : inv.ActiveIndex;
      var toDrop = inv.Get(target);
      dropped = !toDrop.IsEmpty;
      if (dropped) droppedPickup = new PickupData(toDrop.type, toDrop.IsInfinite ? loot.ammo : toDrop.ammo);
      var outInv2 = inv; outInv2.Set(target, new WeaponSlot(loot.type, loot.ammo));
      return outInv2;
    }

    public static InventoryState SetActive(InventoryState inv, int index){
      if (index < 0) index = 0; if (index > 3) index = 3;
      inv.ActiveIndex = index;
      return inv;
    }

    public static ConsumeResult TryConsume(InventoryState inv){
      var s = inv.Get(inv.ActiveIndex);
      if (s.IsEmpty) return new ConsumeResult{ fired=false, next=inv };
      if (s.IsInfinite) return new ConsumeResult{ fired=true, next=inv };
      if (s.ammo <= 0) return new ConsumeResult{ fired=false, next=inv };
      s.ammo -= 1;
      inv.Set(inv.ActiveIndex, s);
      return new ConsumeResult{ fired=true, next=inv };
    }
  }
}
