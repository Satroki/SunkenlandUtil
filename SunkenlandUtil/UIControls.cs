using Fusion.StatsInternal;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UnityGameUI
{
    // UI 控件
    internal class UIControls : MonoBehaviour
    {
        #region[声明]
        private const float kWidth = 160f;
        private const float kThickHeight = 30f;
        private const float kThinHeight = 20f;
        private static Vector2 s_ThickElementSize = new Vector2(160f, 30f);
        private static Vector2 s_ThinElementSize = new Vector2(160f, 20f);
        private static Vector2 s_ImageElementSize = new Vector2(100f, 100f);
        private static Color s_DefaultSelectableColor = new Color(1f, 1f, 1f, 1f);
        private static Color s_PanelColor = new Color(1f, 1f, 1f, 0.392f);
        private static Color s_TextColor = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);

        public struct Resources
        {
            public Sprite standard;
            public Sprite background;
            public Sprite inputField;
            public Sprite knob;
            public Sprite checkmark;
            public Sprite dropdown;
            public Sprite mask;
        }
        #endregion

        public UIControls()
        {

        }

        #region[元素]

        // 创建根元素
        private static GameObject CreateUIElementRoot(string name, Vector2 size)
        {
            GameObject gameObject = new GameObject(name);
            RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = size;
            return gameObject;
        }

        // 创建UI对象
        private static GameObject CreateUIObject(string name, GameObject parent)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.AddComponent<RectTransform>();
            UIControls.SetParentAndAlign(gameObject, parent);
            return gameObject;
        }

        // 设置默认文本
        private static void SetDefaultTextValues(Text lbl)
        {
            lbl.color = UIControls.s_TextColor;
            //lbl.AssignDefaultFont();
            //lbl.FontTextureChanged();
            lbl.font = (Font)UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
            // 设置字体为宋体
            //lbl.font = (Font)UnityEngine.Resources.GetBuiltinResource<Font>("simkai.ttf");
        }

        // 设置默认颜色过度值
        private static void SetDefaultColorTransitionValues(Selectable slider)
        {
            ColorBlock colors = slider.colors;
            colors.highlightedColor = new Color(0.882f, 0.882f, 0.882f);
            colors.pressedColor = new Color(0.698f, 0.698f, 0.698f);
            colors.disabledColor = new Color(0.521f, 0.521f, 0.521f);
        }

        // 设置父级对其
        private static void SetParentAndAlign(GameObject child, GameObject parent)
        {
            if (!(parent == null))
            {
                child.transform.SetParent(parent.transform, false);
                UIControls.SetLayerRecursively(child, parent.layer);
            }
        }

        // 递归设置层
        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            Transform transform = go.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                UIControls.SetLayerRecursively(transform.GetChild(i).gameObject, layer);
            }
        }

        // 创建面板
        public static GameObject CreatePanel(UIControls.Resources resources)
        {
            GameObject gameObject = UIControls.CreateUIElementRoot("Panel", UIControls.s_ThickElementSize);
            RectTransform component = gameObject.GetComponent<RectTransform>();
            component.anchorMin = Vector2.zero;
            component.anchorMax = Vector2.one;
            component.anchoredPosition = Vector2.zero;
            component.sizeDelta = Vector2.zero;
            Image image = gameObject.AddComponent<Image>();
            image.sprite = resources.background;
            image.type = Image.Type.Sliced;
            image.color = UIControls.s_PanelColor;
            return gameObject;
        }

        // 创建文本
        public static GameObject CreateText(UIControls.Resources resources)
        {
            GameObject gameObject = UIControls.CreateUIElementRoot("Text", UIControls.s_ThickElementSize);
            Text text = gameObject.AddComponent<Text>();
            text.text = "New Text";
            UIControls.SetDefaultTextValues(text);
            return gameObject;
        }

        #endregion

        #region[创建组件]

        // 将 16进制格式颜色转换为Color32
        public static Color32 HTMLString2Color(string htmlcolorstring)
        {
            #region[DevNote]
            // Unity ref: https://docs.unity3d.com/ScriptReference/ColorUtility.TryParseHtmlString.html
            // Note: Color strings can also set alpha: "#7AB900" vs. w/alpha "#7AB90003" 
            //ColorUtility.TryParseHtmlString(htmlcolorstring, out color); // Unity's Method, This may have been stripped
            #endregion

            Color32 color = htmlcolorstring.HexToColor();

            return color;
        }

        public static Texture2D createDefaultTexture(string htmlcolorstring)
        {
            Color32 color = HTMLString2Color(htmlcolorstring);

            // Make a new sprite from a texture
            Texture2D SpriteTexture = new Texture2D(1, 1);
            SpriteTexture.SetPixel(0, 0, color);
            SpriteTexture.Apply();

            return SpriteTexture;
        }

        // 通过文件创建 Texture2D贴图格式
        public static Texture2D createTextureFromFile(string FilePath)
        {
            // Load a PNG or JPG file from disk to a Texture2D
            // 将 PNG 或 JPG 文件从磁盘加载到 Texture2D
            Texture2D Tex2D;
            byte[] FileData;

            if (File.Exists(FilePath))
            {
                FileData = File.ReadAllBytes(FilePath);
                Tex2D = new Texture2D(265, 198);
                Tex2D.LoadRawTextureData(FileData);
                //Tex2D.LoadImage(FileData, false);  // This is Broke. Unhollower/Texture2D doesn't like it...
                Tex2D.Apply();
                return Tex2D;
            }
            return null;
        }

        // 通过纹理贴图创建元素
        public static Sprite createSpriteFrmTexture(Texture2D SpriteTexture)
        {
            // Create a new Sprite from Texture
            Sprite NewSprite = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), 100.0f, 0, SpriteMeshType.Tight);

            return NewSprite;
        }


        // 创建画布
        public static GameObject createUICanvas()
        {
            // Debug.Log("创建画布");

            // Create a new Canvas Object with required components
            GameObject CanvasGO = new GameObject("CanvasGO");
            Object.DontDestroyOnLoad(CanvasGO);

            // 传入 Canvas 类型
            Canvas canvas = CanvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;

            CanvasScaler cs = CanvasGO.AddComponent<CanvasScaler>();
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            cs.referencePixelsPerUnit = 100f;
            cs.referenceResolution = new Vector2(1024f, 788f);

            GraphicRaycaster gr = CanvasGO.AddComponent<GraphicRaycaster>();
            ;
            return CanvasGO;
        }

        // 创建UI面板
        public static GameObject createUIPanel(GameObject canvas, string height, string width, float x, float y, Sprite BgSprite = null)
        {
            UIControls.Resources uiResources = new UIControls.Resources();

            uiResources.background = BgSprite;

            //log.LogMessage("   Creating UI Panel");
            // Debug.Log("创建UI面板");
            GameObject uiPanel = UIControls.CreatePanel(uiResources);
            uiPanel.transform.SetParent(canvas.transform, false);

            RectTransform rectTransform = uiPanel.GetComponent<RectTransform>();

            float size;
            size = Single.Parse(height); // 它们在 Unhollower 中没有浮动支持，这样可以避免错误
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            size = Single.Parse(width);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            rectTransform.SetPositionAndRotation(new Vector3(x, y), default);
            // 您也可以使用 rectTransform.sizeDelta = new Vector2(width, height);

            return uiPanel;
        }

        // 创建文本
        public static GameObject createUIText(GameObject parent, Sprite BgSprite, string textColor = null)
        {
            UIControls.Resources uiResources = new UIControls.Resources();
            uiResources.background = BgSprite;

            // Debug.Log("创建文本");
            GameObject uiText = UIControls.CreateText(uiResources);
            uiText.transform.SetParent(parent.transform, false);

            //uiText.transform.GetChild(0).GetComponent<Text>().font = (Font)Resources.GetBuiltinResource<Font>("Arial.ttf"); // 设置字体
            if (textColor != null) uiText.GetComponent<Text>().color = HTMLString2Color(textColor);
            return uiText;
        }

        #endregion
    }
}
