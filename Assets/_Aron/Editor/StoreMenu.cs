// (c) Ahmed Alamrani. All rights reserved.
using Aron.Save;
using Aron.Store;
using UnityEditor;
using UnityEngine;

namespace Aron.EditorTools
{
    /// <summary>Store switches for testing in the editor.</summary>
    public static class StoreMenu
    {
        private const string FakePath = "Aron/Store/Use Fake Store";

        [MenuItem(FakePath, priority = 60)]
        private static void ToggleFake()
        {
            bool on = !EditorPrefs.GetBool(StoreService.FakeStorePref, true);
            EditorPrefs.SetBool(StoreService.FakeStorePref, on);
            Debug.Log(on ? "[Aron] Store purchases go to the fake store." : "[Aron] Store purchases go to Unity IAP (needs the package and a store).");
        }

        [MenuItem(FakePath, true)]
        private static bool ToggleFakeCheck()
        {
            Menu.SetChecked(FakePath, EditorPrefs.GetBool(StoreService.FakeStorePref, true));
            return true;
        }

        [MenuItem("Aron/Store/Give 5000 Coins", priority = 61)]
        private static void GiveCoins()
        {
            SaveSystem.Data.coins += 5000;
            SaveSystem.Save();
            Debug.Log("[Aron] Coins: " + SaveSystem.Data.coins);
        }

        [MenuItem("Aron/Store/Forget Fake Purchases", priority = 62)]
        private static void ForgetFake()
        {
            PlayerPrefs.DeleteKey(FakePurchaseBackend.OwnedPref);
            Debug.Log("[Aron] Fake store forgot its purchases. Items already in the save stay.");
        }
    }
}
