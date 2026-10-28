using UnityEngine;
using UnityEditor;
using TalkingKitty.Cat;

namespace TalkingKitty.EditorTools
{
    /// <summary>
    /// Builds a cute low-poly toddler (backward cap + overalls) from primitives.
    /// Used by TalkingKittySceneBuilder; also available via menu for iteration.
    /// </summary>
    public static class Kid3DCharacterBuilder
    {
        [MenuItem("TalkingKitty/Rebuild 3D Kid Prefab Preview In Scene")]
        public static void RebuildSelectedOrCreate()
        {
            var existing = GameObject.Find("Kid3D");
            if (existing != null) Object.DestroyImmediate(existing);
            Build(null);
            Debug.Log("[Kid3DCharacterBuilder] Created Kid3D in the open scene (not saved).");
        }

        public static Kid3DPresenter Build(Transform parent)
        {
            var root = new GameObject("Kid3D");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, -1.2f, 0f);
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * 0.95f;

            var skin = Mat(new Color(1f, 0.86f, 0.75f));
            var hair = Mat(new Color(0.22f, 0.16f, 0.12f));
            var shirt = Mat(new Color(0.96f, 0.96f, 0.96f));
            var denim = Mat(new Color(0.28f, 0.30f, 0.34f));
            var cap = Mat(new Color(0.90f, 0.86f, 0.78f));
            var eyeWhite = Mat(Color.white);
            var eyeDark = Mat(new Color(0.12f, 0.10f, 0.10f));
            var mouth = Mat(new Color(0.75f, 0.35f, 0.40f));
            var cheek = Mat(new Color(1f, 0.70f, 0.68f));

            // Body
            var torso = Prim(root.transform, "Torso", PrimitiveType.Capsule, shirt,
                new Vector3(0f, 0.95f, 0f), new Vector3(0.55f, 0.45f, 0.35f));
            Prim(root.transform, "Overalls", PrimitiveType.Capsule, denim,
                new Vector3(0f, 0.55f, 0f), new Vector3(0.62f, 0.42f, 0.38f));
            Prim(root.transform, "StrapL", PrimitiveType.Cube, denim,
                new Vector3(-0.18f, 1.15f, 0.05f), new Vector3(0.10f, 0.55f, 0.06f));
            Prim(root.transform, "StrapR", PrimitiveType.Cube, denim,
                new Vector3(0.18f, 1.15f, 0.05f), new Vector3(0.10f, 0.55f, 0.06f));

            // Legs / arms
            Prim(root.transform, "LegL", PrimitiveType.Capsule, denim,
                new Vector3(-0.16f, 0.05f, 0f), new Vector3(0.16f, 0.28f, 0.16f));
            Prim(root.transform, "LegR", PrimitiveType.Capsule, denim,
                new Vector3(0.16f, 0.05f, 0f), new Vector3(0.16f, 0.28f, 0.16f));
            Prim(root.transform, "ArmL", PrimitiveType.Capsule, skin,
                new Vector3(-0.48f, 0.95f, 0f), new Vector3(0.12f, 0.28f, 0.12f));
            Prim(root.transform, "ArmR", PrimitiveType.Capsule, skin,
                new Vector3(0.48f, 0.95f, 0f), new Vector3(0.12f, 0.28f, 0.12f));

            // Head
            var head = Prim(root.transform, "Head", PrimitiveType.Sphere, skin,
                new Vector3(0f, 1.72f, 0f), new Vector3(0.78f, 0.72f, 0.72f));
            Prim(head.transform, "CheekL", PrimitiveType.Sphere, cheek,
                new Vector3(-0.28f, -0.05f, 0.28f), new Vector3(0.22f, 0.16f, 0.14f));
            Prim(head.transform, "CheekR", PrimitiveType.Sphere, cheek,
                new Vector3(0.28f, -0.05f, 0.28f), new Vector3(0.22f, 0.16f, 0.14f));
            Prim(head.transform, "Hair", PrimitiveType.Sphere, hair,
                new Vector3(0f, 0.12f, -0.05f), new Vector3(0.82f, 0.45f, 0.78f));

            // Eyes
            var eyeL = Prim(head.transform, "EyeL", PrimitiveType.Sphere, eyeWhite,
                new Vector3(-0.18f, 0.06f, 0.32f), new Vector3(0.16f, 0.18f, 0.10f));
            Prim(eyeL.transform, "PupilL", PrimitiveType.Sphere, eyeDark,
                new Vector3(0f, 0f, 0.45f), new Vector3(0.45f, 0.5f, 0.45f));
            var eyeR = Prim(head.transform, "EyeR", PrimitiveType.Sphere, eyeWhite,
                new Vector3(0.18f, 0.06f, 0.32f), new Vector3(0.16f, 0.18f, 0.10f));
            Prim(eyeR.transform, "PupilR", PrimitiveType.Sphere, eyeDark,
                new Vector3(0f, 0f, 0.45f), new Vector3(0.45f, 0.5f, 0.45f));

            // Backward cap
            var capGo = Prim(head.transform, "Cap", PrimitiveType.Sphere, cap,
                new Vector3(0f, 0.28f, -0.05f), new Vector3(0.92f, 0.42f, 0.92f));
            Prim(capGo.transform, "CapStrap", PrimitiveType.Cube, cap,
                new Vector3(0f, -0.15f, 0.55f), new Vector3(0.55f, 0.08f, 0.08f));
            Prim(capGo.transform, "CapRed", PrimitiveType.Cube, Mat(new Color(0.85f, 0.2f, 0.2f)),
                new Vector3(0.22f, -0.15f, 0.58f), new Vector3(0.08f, 0.06f, 0.06f));

            // Jaw / mouth (pivots for talking)
            var jaw = new GameObject("Jaw");
            jaw.transform.SetParent(head.transform, false);
            jaw.transform.localPosition = new Vector3(0f, -0.18f, 0.22f);
            Prim(jaw.transform, "Mouth", PrimitiveType.Sphere, mouth,
                new Vector3(0f, -0.02f, 0.08f), new Vector3(0.28f, 0.10f, 0.16f));
            Prim(jaw.transform, "Teeth", PrimitiveType.Cube, Mat(new Color(0.95f, 0.95f, 0.92f)),
                new Vector3(0f, 0.02f, 0.10f), new Vector3(0.16f, 0.04f, 0.06f));

            var presenter = root.AddComponent<Kid3DPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("root").objectReferenceValue = root.transform;
            so.FindProperty("jaw").objectReferenceValue = jaw.transform;
            // Tint skin/cheeks lightly on listen/speak so the kid "reacts".
            var cheekL = head.transform.Find("CheekL")?.GetComponent<Renderer>();
            var cheekR = head.transform.Find("CheekR")?.GetComponent<Renderer>();
            var tintProp = so.FindProperty("tintRenderers");
            tintProp.arraySize = 2;
            tintProp.GetArrayElementAtIndex(0).objectReferenceValue = cheekL;
            tintProp.GetArrayElementAtIndex(1).objectReferenceValue = cheekR;
            so.ApplyModifiedProperties();

            // Hide unused warning
            _ = torso;
            return presenter;
        }

        static GameObject Prim(Transform parent, string name, PrimitiveType type, Material mat,
            Vector3 localPos, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
            return go;
        }

        static Material Mat(Color c)
        {
            const string dir = "Assets/Art/Materials";
            if (!AssetDatabase.IsValidFolder("Assets/Art"))
                AssetDatabase.CreateFolder("Assets", "Art");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/Art", "Materials");

            // Stable-ish filename from color so rebuilds reuse the same mats.
            var hex = ColorUtility.ToHtmlStringRGB(c);
            var path = $"{dir}/Kid_{hex}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Sprites/Default");
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.45f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
