// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron
{
    /// <summary>
    /// GameObject.CreatePrimitive gives the built-in default material, which URP players draw magenta.
    /// Every runtime primitive goes through here so it starts with a URP Lit material instead.
    /// </summary>
    public static class PrimitiveFactory
    {
        private static Material lit;

        public static GameObject Create(PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            var r = go.GetComponent<Renderer>();
            var m = Lit;
            if (r != null && m != null) r.sharedMaterial = m;
            return go;
        }

        public static Material Lit
        {
            get
            {
                if (lit != null) return lit;
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit");
                if (shader == null) return null;
                lit = new Material(shader) { name = "Runtime_Lit" };
                if (lit.HasProperty("_Smoothness")) lit.SetFloat("_Smoothness", 0.15f);
                return lit;
            }
        }
    }
}
