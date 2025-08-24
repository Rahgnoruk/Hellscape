namespace Hellscape.Domain {
  public enum WeaponType : byte {
    None = 0,
    BasePistol = 1,  // slot0 only, infinite ammo
    Rifle = 10,
    SMG   = 11,
    Shotgun = 12
  }
}
