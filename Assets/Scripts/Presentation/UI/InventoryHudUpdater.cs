using Hellscape.Domain;
using Hellscape.Net;
using UnityEngine;
using Unity.Netcode;
namespace Hellscape.Presentation.UI
{
    public class InventoryHudUpdater : MonoBehaviour
    {
        [SerializeField] private HudUI hudUI;
        private NetPlayer _localPlayer;
        
        private void Start()
        {
            if (hudUI == null)
            {
                hudUI = FindObjectOfType<HudUI>();
            }
        }
        
        private void Update()
        {
            // Find local player if not found
            if (_localPlayer == null)
            {
                _localPlayer = FindLocalPlayer();
                if (_localPlayer != null)
                {
                    // Subscribe to inventory changes
                    _localPlayer.netSlots.OnListChanged += OnInventoryChanged;
                    _localPlayer.activeIndex.OnValueChanged += OnActiveSlotChanged;
                }
            }
            
            // Update HUD if we have a local player
            if (_localPlayer != null)
            {
                UpdateInventoryDisplay();
            }
        }
        
        private NetPlayer FindLocalPlayer()
        {
            var players = FindObjectsByType<NetPlayer>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.IsOwner)
                {
                    return player;
                }
            }
            return null;
        }
        
        private void OnInventoryChanged(NetworkListEvent<NetWeaponSlot> changeEvent)
        {
            UpdateInventoryDisplay();
        }
        
        private void OnActiveSlotChanged(int previousValue, int newValue)
        {
            UpdateInventoryDisplay();
        }
        
        private void UpdateInventoryDisplay()
        {
            if (_localPlayer == null || hudUI == null) return;
            
            var activeIndex = _localPlayer.activeIndex.Value;
            
            for (int i = 0; i < _localPlayer.netSlots.Count && i < 4; i++)
            {
                var netSlot = _localPlayer.netSlots[i];
                var weaponSlot = netSlot.ToDomain();
                
                string weaponName = GetWeaponName(weaponSlot.type);
                string ammoText = GetAmmoText(weaponSlot.ammo);
                bool isActive = (i == activeIndex);
                
                hudUI.SetSlot(i, $"{weaponName}\n{ammoText}", weaponSlot.ammo, isActive);
            }
        }
        
        private string GetWeaponName(WeaponType type)
        {
            return type switch
            {
                WeaponType.None => "Empty",
                WeaponType.BasePistol => "Base",
                WeaponType.Rifle => "Rifle",
                WeaponType.SMG => "SMG",
                WeaponType.Shotgun => "Shotgun",
                _ => "Unknown"
            };
        }
        
        private string GetAmmoText(int ammo)
        {
            if (ammo < 0) return "∞";
            return ammo.ToString();
        }
        
        private void OnDestroy()
        {
            if (_localPlayer != null)
            {
                _localPlayer.netSlots.OnListChanged -= OnInventoryChanged;
                _localPlayer.activeIndex.OnValueChanged -= OnActiveSlotChanged;
            }
        }
    }
}
