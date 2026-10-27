// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.EditorTools
{
    /// <summary>Ahmed's two real shops on the Riyadh street: the electronics store and the grocery.</summary>
    public static partial class AronLevelBuilder
    {
        private static void BuildRealShops(Transform world)
        {
            var shops = Group("RealShops", world).transform;
            DeveloperStore(shops, new Vector3(6.6f, 0f, 16f));
            GroceryStore(shops, new Vector3(-6.6f, 0f, 70f));
        }

        private static void DeveloperStore(Transform parent, Vector3 pos)
        {
            var root = ShopRoot("Shop_DeveloperAhmed", parent, pos);
            var front = Facade(root, pos.x);

            var shell = Mat("DevStoreShell", new Color(0.16f, 0.17f, 0.2f));
            var trim = Mat("DevStoreTrim", new Color(0.75f, 0.77f, 0.8f));
            var glass = Mat("DevStoreGlass", new Color(0.12f, 0.3f, 0.42f));
            glass.SetFloat("_Smoothness", 0.95f);
            var screen = Glow("DevStoreScreen", new Color(0.25f, 0.7f, 1f), 1.2f);
            var door = Glow("DevStoreDoor", new Color(0.2f, 0.9f, 1f), 1.5f);

            Block("Body", front, new Vector3(0f, 1.75f, 0.2f), new Vector3(3.8f, 3.5f, 2.2f), shell);
            Block("Roof", front, new Vector3(0f, 3.55f, 0.1f), new Vector3(4f, 0.12f, 2.5f), trim, PrimitiveType.Cube, false);
            Block("Step", front, new Vector3(0f, 0.06f, -1.15f), new Vector3(1.6f, 0.12f, 0.5f), trim, PrimitiveType.Cube, false);
            foreach (float x in new[] { -1.3f, 1.3f })
            {
                Block("Window", front, new Vector3(x, 1.4f, -0.92f), new Vector3(1.05f, 1.9f, 0.06f), glass, PrimitiveType.Cube, false);
                Block("Screen", front, new Vector3(x, 1.5f, -0.88f), new Vector3(0.7f, 0.45f, 0.04f), screen, PrimitiveType.Cube, false);
                Block("Laptop", front, new Vector3(x, 0.75f, -0.85f), new Vector3(0.5f, 0.04f, 0.32f), trim, PrimitiveType.Cube, false);
            }
            Block("DoorFrame", front, new Vector3(0f, 1.15f, -0.93f), new Vector3(1.15f, 2.3f, 0.06f), trim, PrimitiveType.Cube, false);
            var glowDoor = Block("Door", front, new Vector3(0f, 1.1f, -0.97f), new Vector3(0.95f, 2.15f, 0.04f), door, PrimitiveType.Cube, false);

            Block("SignBoard", front, new Vector3(0f, 2.85f, -0.98f), new Vector3(3.6f, 0.75f, 0.1f), Mat("DevStoreSign", new Color(0.05f, 0.06f, 0.08f)), PrimitiveType.Cube, false);
            Block("SignLine", front, new Vector3(0f, 2.5f, -1.04f), new Vector3(3.4f, 0.04f, 0.02f), door, PrimitiveType.Cube, false);
            ArabicSign(front, new Vector3(0.25f, 2.86f, -1.05f), "متجر المبرمج أحمد", 4.2f, new Vector2(3f, 0.6f), new Color(0.6f, 0.95f, 1f));
            WorldText(front, "Logo", new Vector3(-1.45f, 2.86f, -1.05f), 4f, new Vector2(0.7f, 0.6f), new Color(0.3f, 0.9f, 1f)).text = "</>";

            var bubble = Bubble(root, front.TransformPoint(new Vector3(0f, 4.3f, -1.2f)) - root.position);
            Wire(root, global::Aron.Store.ShopKind.Developer, glowDoor, new Color(0.2f, 0.9f, 1f), bubble, pos.x);
        }

        private static void GroceryStore(Transform parent, Vector3 pos)
        {
            var root = ShopRoot("Shop_AhmedGrocery", parent, pos);
            var front = Facade(root, pos.x);

            var white = Mat("GroceryWhite", new Color(0.95f, 0.95f, 0.92f));
            var green = Mat("GroceryGreen", new Color(0.05f, 0.45f, 0.25f));
            var wood = Mat("GroceryCrate", new Color(0.62f, 0.45f, 0.25f));
            var fridgeGlass = Mat("GroceryFridgeGlass", new Color(0.55f, 0.75f, 0.85f));
            var door = Glow("GroceryDoor", new Color(1f, 0.85f, 0.45f), 1.2f);

            Block("Body", front, new Vector3(0f, 1.6f, 0.2f), new Vector3(3.8f, 3.2f, 2.2f), white);
            Block("Base", front, new Vector3(0f, 0.2f, -0.92f), new Vector3(3.8f, 0.4f, 0.06f), green, PrimitiveType.Cube, false);
            Block("DoorFrame", front, new Vector3(0.05f, 1.1f, -0.93f), new Vector3(1.1f, 2.2f, 0.06f), green, PrimitiveType.Cube, false);
            var glowDoor = Block("Door", front, new Vector3(0.05f, 1.05f, -0.97f), new Vector3(0.9f, 2.05f, 0.04f), door, PrimitiveType.Cube, false);

            // Drinks fridge by the door, the way every بقالة has one.
            Block("Fridge", front, new Vector3(-1.2f, 0.95f, -1.15f), new Vector3(0.9f, 1.9f, 0.6f), white, PrimitiveType.Cube, false);
            Block("FridgeGlass", front, new Vector3(-1.2f, 1.05f, -1.46f), new Vector3(0.75f, 1.5f, 0.03f), fridgeGlass, PrimitiveType.Cube, false);
            Color[] cans = { new Color(0.85f, 0.1f, 0.1f), new Color(0.1f, 0.4f, 0.85f), new Color(0.95f, 0.75f, 0.1f) };
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    Block("Can", front, new Vector3(-1.43f + c * 0.23f, 0.55f + r * 0.45f, -1.38f), new Vector3(0.12f, 0.2f, 0.12f),
                          Mat("GroceryCan" + ((r + c) % 3), cans[(r + c) % 3]), PrimitiveType.Cylinder, false);

            // Crates of fruit and veg on the pavement, beside the door.
            Color[] produce = { new Color(0.9f, 0.45f, 0.05f), new Color(0.85f, 0.12f, 0.1f), new Color(0.4f, 0.7f, 0.15f) };
            for (int i = 0; i < produce.Length; i++)
            {
                var at = new Vector3(1.05f + (i % 2) * 0.58f, 0.2f + (i / 2) * 0.42f, -1.4f - (i / 2) * 0.05f);
                Block("ProduceCrate", front, at, new Vector3(0.55f, 0.4f, 0.45f), wood, PrimitiveType.Cube, false);
                for (int k = 0; k < 4; k++)
                    Block("Fruit", front, at + new Vector3((k % 2 - 0.5f) * 0.24f, 0.25f, (k / 2 - 0.5f) * 0.2f),
                          Vector3.one * 0.16f, Mat("Produce" + i, produce[i]), PrimitiveType.Sphere, false);
            }

            Awning(front, ShopKind.Grocery);
            Block("SignBoard", front, new Vector3(0f, 3.05f, -0.98f), new Vector3(3.8f, 0.8f, 0.1f), green, PrimitiveType.Cube, false);
            Block("SignEdge", front, new Vector3(0f, 2.62f, -1.02f), new Vector3(3.8f, 0.05f, 0.04f), white, PrimitiveType.Cube, false);
            ArabicSign(front, new Vector3(0f, 3.06f, -1.06f), "متجر أحمد للمواد الغذائية", 3.4f, new Vector2(3.6f, 0.65f), Color.white);

            var bubble = Bubble(root, front.TransformPoint(new Vector3(0.05f, 4.2f, -1.2f)) - root.position);
            Wire(root, global::Aron.Store.ShopKind.Grocery, glowDoor, new Color(1f, 0.85f, 0.45f), bubble, pos.x);
        }

        private static Transform ShopRoot(string name, Transform parent, Vector3 pos) => Group(name, parent, pos).transform;

        /// <summary>The building turned towards the road and the camera, like the other street shops.</summary>
        private static Transform Facade(Transform root, float x)
        {
            var front = Group("Front", root).transform;
            front.localRotation = Quaternion.Euler(0f, x < 0f ? -55f : 55f, 0f);
            return front;
        }

        private static Material Glow(string name, Color color, float strength)
        {
            var m = Mat(name, color);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * strength);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        }

        private static Transform Bubble(Transform root, Vector3 localPos)
        {
            var bubble = Group("Bubble", root, localPos).transform;
            Block("Back", bubble, new Vector3(0f, 0f, 0.03f), new Vector3(2.2f, 0.6f, 0.04f), Mat("BubbleWhite", Color.white), PrimitiveType.Cube, false);
            Block("Tail", bubble, new Vector3(0f, -0.36f, 0.03f), new Vector3(0.2f, 0.2f, 0.04f), Mat("BubbleWhite", Color.white), PrimitiveType.Cube, false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            ArabicSign(bubble, Vector3.zero, "اضغط للدخول", 3f, new Vector2(2.1f, 0.5f), new Color(0.1f, 0.1f, 0.12f));
            return bubble;
        }

        /// <summary>ShopFront on the root, with its walk-up zone on the road right in front of the shop.</summary>
        private static void Wire(Transform root, global::Aron.Store.ShopKind kind, GameObject door, Color glow, Transform bubble, float x)
        {
            float side = Mathf.Sign(x);
            float roadX = side * (HalfWidth - 0.7f);
            Trigger(root.gameObject, new Vector3(1.4f, 3f, 3f), new Vector3(roadX - x, 1.5f, 0f));
            var shop = root.gameObject.AddComponent<global::Aron.Store.ShopFront>();
            Set(shop, "kind", kind);
            Set(shop, "doorGlow", door.GetComponent<Renderer>());
            Set(shop, "glowColor", glow);
            Set(shop, "bubble", bubble);
        }
    }
}
