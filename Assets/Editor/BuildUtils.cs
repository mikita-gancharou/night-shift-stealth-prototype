using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Stealth.EditorTools
{
    /// <summary>Layer indices used across the generated content.</summary>
    public static class StealthLayers
    {
        public const int Environment = 6;
        public const int Player = 7;
        public const int Enemy = 8;
        public const int Interactable = 9;
        public const int Throwable = 10;
        public const int VisionCone = 11;
        public const int Zone = 12;

        public static int Mask(params int[] layers)
        {
            int mask = 0;
            foreach (int layer in layers) mask |= 1 << layer;
            return mask;
        }
    }

    /// <summary>Folder layout of the generated assets.</summary>
    public static class StealthPaths
    {
        public const string Art = "Assets/Art";
        public const string Materials = "Assets/Art/Materials";
        public const string Sprites = "Assets/Art/Sprites";
        public const string Shaders = "Assets/Art/Shaders";
        public const string Audio = "Assets/Audio";
        public const string Prefabs = "Assets/Prefabs";
        public const string Scenes = "Assets/Scenes";
        public const string Settings = "Assets/Settings";

        public const string LevelScene = "Assets/Scenes/Level01_Depot.unity";
        public const string MenuScene = "Assets/Scenes/MainMenu.unity";
        public const string NavMeshAsset = "Assets/Scenes/Level01_Depot_NavMesh.asset";
    }

    /// <summary>Small helpers shared by the content builders: primitives, triggers, UI widgets.</summary>
    public static class BuildUtils
    {
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>Creates a box (cube primitive) with an optional collider.</summary>
        public static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material material,
            int layer = StealthLayers.Environment, bool collider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.layer = layer;

            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (material != null) go.GetComponent<MeshRenderer>().sharedMaterial = material;

            return go;
        }

        public static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 center,
            Vector3 size, Material material, int layer = StealthLayers.Environment, bool collider = true)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.layer = layer;

            Collider existing = go.GetComponent<Collider>();
            if (!collider && existing != null) Object.DestroyImmediate(existing);
            if (material != null) go.GetComponent<MeshRenderer>().sharedMaterial = material;

            return go;
        }

        /// <summary>Creates an empty object carrying a box trigger, used for every zone in the level.</summary>
        public static GameObject Trigger(string name, Transform parent, Vector3 center, Vector3 size, int layer = StealthLayers.Zone)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.layer = layer;

            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;

            return go;
        }

        public static GameObject Empty(string name, Transform parent, Vector3 localPosition = default,
            Quaternion localRotation = default)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation == default ? Quaternion.identity : localRotation;
            return go;
        }

        public static void SetLayerRecursive(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform) SetLayerRecursive(child.gameObject, layer);
        }

        // ----- UI helpers -------------------------------------------------------------------------

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, string text, float size,
            TextAlignmentOptions alignment, Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = Rect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        public static Image Sprite(string name, Transform parent, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = Rect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Button Button(string name, Transform parent, string text, Sprite background, Vector2 anchoredPosition,
            Vector2 size, Color backgroundColor, Color textColor, float fontSize = 26f)
        {
            RectTransform rect = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = background;
            image.type = Image.Type.Sliced;
            image.color = backgroundColor;

            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            button.targetGraphic = image;

            TextMeshProUGUI label = Label(name + "Label", rect, text, fontSize, TextAlignmentOptions.Center, textColor,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.fontStyle = FontStyles.Bold;

            rect.gameObject.AddComponent<Stealth.UI.ButtonSfx>();
            return button;
        }

        /// <summary>Assigns a private serialized field by name - how the builders wire up components.</summary>
        public static void SetField(Object target, string field, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning($"[Stealth] Field '{field}' not found on {target.GetType().Name}");
                return;
            }

            switch (value)
            {
                case null:
                    property.objectReferenceValue = null;
                    break;
                case Object unityObject:
                    property.objectReferenceValue = unityObject;
                    break;
                case int intValue when property.propertyType == SerializedPropertyType.LayerMask:
                    property.intValue = intValue;
                    break;
                case int intValue:
                    property.intValue = intValue;
                    break;
                case float floatValue:
                    property.floatValue = floatValue;
                    break;
                case bool boolValue:
                    property.boolValue = boolValue;
                    break;
                case string stringValue:
                    property.stringValue = stringValue;
                    break;
                case Color colorValue:
                    property.colorValue = colorValue;
                    break;
                case Vector3 vectorValue:
                    property.vector3Value = vectorValue;
                    break;
                default:
                    Debug.LogWarning($"[Stealth] Unsupported field type for '{field}'");
                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFields(Object target, params (string field, object value)[] values)
        {
            foreach ((string field, object value) in values) SetField(target, field, value);
        }

        /// <summary>Assigns a serialized array of object references (renderers, lights, ...).</summary>
        public static void SetArray(Object target, string field, params Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);

            if (property == null || !property.isArray)
            {
                Debug.LogWarning($"[Stealth] Array field '{field}' not found on {target.GetType().Name}");
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
