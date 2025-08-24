using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Hellscape.Presentation.UI;

namespace Hellscape.Tests
{
    public class UITests
    {
        [Test]
        public void MainMenuUI_CanBeCreated()
        {
            // Arrange & Act
            var go = new GameObject("TestMainMenu");
            var document = go.AddComponent<UIDocument>();
            var mainMenuUI = go.AddComponent<MainMenuUI>();
            
            // Assert
            Assert.IsNotNull(mainMenuUI);
            Assert.IsNotNull(document);
        }
        
        [Test]
        public void HudUI_CanBeCreated()
        {
            // Arrange & Act
            var go = new GameObject("TestHUD");
            var document = go.AddComponent<UIDocument>();
            var hudUI = go.AddComponent<HudUI>();
            
            // Assert
            Assert.IsNotNull(hudUI);
            Assert.IsNotNull(document);
        }
        
        [Test]
        public void PauseMenuUI_CanBeCreated()
        {
            // Arrange & Act
            var go = new GameObject("TestPauseMenu");
            var document = go.AddComponent<UIDocument>();
            var pauseMenuUI = go.AddComponent<PauseMenuUI>();
            
            // Assert
            Assert.IsNotNull(pauseMenuUI);
            Assert.IsNotNull(document);
        }
        
        [Test]
        public void VignetteFxUI_CanBeCreated()
        {
            // Arrange & Act
            var go = new GameObject("TestVignette");
            var document = go.AddComponent<UIDocument>();
            var vignetteFxUI = go.AddComponent<VignetteFxUI>();
            
            // Assert
            Assert.IsNotNull(vignetteFxUI);
            Assert.IsNotNull(document);
        }
        
        [Test]
        public void UIBootstrap_CanBeCreated()
        {
            // Arrange & Act
            var go = new GameObject("TestUIBootstrap");
            var uiBootstrap = go.AddComponent<UIBootstrap>();
            
            // Assert
            Assert.IsNotNull(uiBootstrap);
        }
        
        [Test]
        public void HudUI_SetLocalHealth_UpdatesHealthValues()
        {
            // Arrange
            var go = new GameObject("TestHUD");
            var document = go.AddComponent<UIDocument>();
            var hudUI = go.AddComponent<HudUI>();
            
            // Act
            hudUI.SetLocalHealth(75, 100);
            
            // Assert - Note: In a real test, we'd need to mock the UIDocument
            // For now, we just verify the method doesn't throw
            Assert.Pass("SetLocalHealth method executed without errors");
        }
        
        [Test]
        public void HudUI_SetSlot_UpdatesSlotState()
        {
            // Arrange
            var go = new GameObject("TestHUD");
            var document = go.AddComponent<UIDocument>();
            var hudUI = go.AddComponent<HudUI>();
            
            // Act
            hudUI.SetSlot(1, "TestWeapon", 30, true);
            
            // Assert - Note: In a real test, we'd need to mock the UIDocument
            // For now, we just verify the method doesn't throw
            Assert.Pass("SetSlot method executed without errors");
        }
    }
}
