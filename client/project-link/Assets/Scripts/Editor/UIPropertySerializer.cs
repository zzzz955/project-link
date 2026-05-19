using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.EditorTools
{
    public static class UIPropertySerializer
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ── Public API ────────────────────────────────────────────────────

        public static string Get(Component c, string key) => c switch
        {
            RectTransform rt              => GetRT(rt, key),
            Image img                     => GetImage(img, key),
            TextMeshProUGUI tmp           => GetTMP(tmp, key),
            CanvasGroup cg                => GetCG(cg, key),
            LayoutElement le              => GetLE(le, key),
            HorizontalLayoutGroup hlg     => GetLG(hlg, key),
            VerticalLayoutGroup vlg       => GetLG(vlg, key),
            ContentSizeFitter csf         => GetCSF(csf, key),
            Button btn                    => GetBtn(btn, key),
            _                             => null
        };

        public static bool Set(Component c, string key, string val) => c switch
        {
            RectTransform rt              => SetRT(rt, key, val),
            Image img                     => SetImage(img, key, val),
            TextMeshProUGUI tmp           => SetTMP(tmp, key, val),
            CanvasGroup cg                => SetCG(cg, key, val),
            LayoutElement le              => SetLE(le, key, val),
            HorizontalLayoutGroup hlg     => SetLG(hlg, key, val),
            VerticalLayoutGroup vlg       => SetLG(vlg, key, val),
            ContentSizeFitter csf         => SetCSF(csf, key, val),
            Button btn                    => SetBtn(btn, key, val),
            _                             => false
        };

        public static IEnumerable<(Component comp, string typeName, string[] keys)>
            TrackedComponents(GameObject go)
        {
            foreach (var c in go.GetComponents<Component>())
            {
                string[] keys = c switch
                {
                    RectTransform _ => new[] {
                        "anchoredPosition", "sizeDelta", "anchorMin", "anchorMax", "pivot" },
                    Image _ => new[] {
                        "color", "raycastTarget", "preserveAspect" },
                    TextMeshProUGUI _ => new[] {
                        "text", "fontSize", "color", "alignment", "fontStyle", "enableWordWrapping" },
                    CanvasGroup _ => new[] {
                        "alpha", "interactable", "blocksRaycasts" },
                    LayoutElement _ => new[] {
                        "minWidth", "minHeight", "preferredWidth", "preferredHeight",
                        "flexibleWidth", "flexibleHeight", "ignoreLayout" },
                    HorizontalLayoutGroup _ => new[] {
                        "spacing", "childAlignment", "paddingLeft", "paddingRight",
                        "paddingTop", "paddingBottom", "childForceExpandWidth", "childForceExpandHeight" },
                    VerticalLayoutGroup _ => new[] {
                        "spacing", "childAlignment", "paddingLeft", "paddingRight",
                        "paddingTop", "paddingBottom", "childForceExpandWidth", "childForceExpandHeight" },
                    ContentSizeFitter _ => new[] { "horizontalFit", "verticalFit" },
                    Button _ => new[] { "interactable" },
                    _ => null
                };
                if (keys != null) yield return (c, c.GetType().Name, keys);
            }
        }

        // ── RectTransform ─────────────────────────────────────────────────

        static string GetRT(RectTransform rt, string key) => key switch
        {
            "anchoredPosition" => Ser(rt.anchoredPosition),
            "sizeDelta"        => Ser(rt.sizeDelta),
            "anchorMin"        => Ser(rt.anchorMin),
            "anchorMax"        => Ser(rt.anchorMax),
            "pivot"            => Ser(rt.pivot),
            _                  => null
        };

        static bool SetRT(RectTransform rt, string key, string val)
        {
            if (!TryV2(val, out var v)) return false;
            switch (key)
            {
                case "anchoredPosition": rt.anchoredPosition = v; return true;
                case "sizeDelta":        rt.sizeDelta        = v; return true;
                case "anchorMin":        rt.anchorMin        = v; return true;
                case "anchorMax":        rt.anchorMax        = v; return true;
                case "pivot":            rt.pivot            = v; return true;
            }
            return false;
        }

        // ── Image ─────────────────────────────────────────────────────────

        static string GetImage(Image img, string key) => key switch
        {
            "color"          => Ser(img.color),
            "raycastTarget"  => img.raycastTarget.ToString(Inv),
            "preserveAspect" => img.preserveAspect.ToString(Inv),
            _                => null
        };

        static bool SetImage(Image img, string key, string val)
        {
            switch (key)
            {
                case "color":
                    if (!TryColor(val, out var c)) return false;
                    img.color = c; return true;
                case "raycastTarget":
                    if (!bool.TryParse(val, out var b1)) return false;
                    img.raycastTarget = b1; return true;
                case "preserveAspect":
                    if (!bool.TryParse(val, out var b2)) return false;
                    img.preserveAspect = b2; return true;
            }
            return false;
        }

        // ── TextMeshProUGUI ───────────────────────────────────────────────

        static string GetTMP(TextMeshProUGUI t, string key) => key switch
        {
            "text"               => t.text,
            "fontSize"           => t.fontSize.ToString("F2", Inv),
            "color"              => Ser(t.color),
            "alignment"          => ((int)t.alignment).ToString(Inv),
            "fontStyle"          => ((int)t.fontStyle).ToString(Inv),
            "enableWordWrapping" => t.enableWordWrapping.ToString(Inv),
            _                    => null
        };

        static bool SetTMP(TextMeshProUGUI t, string key, string val)
        {
            switch (key)
            {
                case "text": t.text = val; return true;
                case "fontSize":
                    if (!float.TryParse(val, NumberStyles.Float, Inv, out var f)) return false;
                    t.fontSize = f; return true;
                case "color":
                    if (!TryColor(val, out var c)) return false;
                    t.color = c; return true;
                case "alignment":
                    if (!int.TryParse(val, out var a)) return false;
                    t.alignment = (TextAlignmentOptions)a; return true;
                case "fontStyle":
                    if (!int.TryParse(val, out var fs)) return false;
                    t.fontStyle = (FontStyles)fs; return true;
                case "enableWordWrapping":
                    if (!bool.TryParse(val, out var b)) return false;
                    t.enableWordWrapping = b; return true;
            }
            return false;
        }

        // ── CanvasGroup ───────────────────────────────────────────────────

        static string GetCG(CanvasGroup cg, string key) => key switch
        {
            "alpha"          => cg.alpha.ToString("F4", Inv),
            "interactable"   => cg.interactable.ToString(Inv),
            "blocksRaycasts" => cg.blocksRaycasts.ToString(Inv),
            _                => null
        };

        static bool SetCG(CanvasGroup cg, string key, string val)
        {
            switch (key)
            {
                case "alpha":
                    if (!float.TryParse(val, NumberStyles.Float, Inv, out var f)) return false;
                    cg.alpha = f; return true;
                case "interactable":
                    if (!bool.TryParse(val, out var b1)) return false;
                    cg.interactable = b1; return true;
                case "blocksRaycasts":
                    if (!bool.TryParse(val, out var b2)) return false;
                    cg.blocksRaycasts = b2; return true;
            }
            return false;
        }

        // ── LayoutElement ─────────────────────────────────────────────────

        static string GetLE(LayoutElement le, string key) => key switch
        {
            "minWidth"        => le.minWidth.ToString("F2", Inv),
            "minHeight"       => le.minHeight.ToString("F2", Inv),
            "preferredWidth"  => le.preferredWidth.ToString("F2", Inv),
            "preferredHeight" => le.preferredHeight.ToString("F2", Inv),
            "flexibleWidth"   => le.flexibleWidth.ToString("F2", Inv),
            "flexibleHeight"  => le.flexibleHeight.ToString("F2", Inv),
            "ignoreLayout"    => le.ignoreLayout.ToString(Inv),
            _                 => null
        };

        static bool SetLE(LayoutElement le, string key, string val)
        {
            if (key == "ignoreLayout")
            {
                if (!bool.TryParse(val, out var b)) return false;
                le.ignoreLayout = b; return true;
            }
            if (!float.TryParse(val, NumberStyles.Float, Inv, out var f)) return false;
            switch (key)
            {
                case "minWidth":        le.minWidth        = f; return true;
                case "minHeight":       le.minHeight       = f; return true;
                case "preferredWidth":  le.preferredWidth  = f; return true;
                case "preferredHeight": le.preferredHeight = f; return true;
                case "flexibleWidth":   le.flexibleWidth   = f; return true;
                case "flexibleHeight":  le.flexibleHeight  = f; return true;
            }
            return false;
        }

        // ── HorizontalOrVerticalLayoutGroup ───────────────────────────────

        static string GetLG(HorizontalOrVerticalLayoutGroup lg, string key) => key switch
        {
            "spacing"                => lg.spacing.ToString("F2", Inv),
            "childAlignment"         => ((int)lg.childAlignment).ToString(Inv),
            "paddingLeft"            => lg.padding.left.ToString(Inv),
            "paddingRight"           => lg.padding.right.ToString(Inv),
            "paddingTop"             => lg.padding.top.ToString(Inv),
            "paddingBottom"          => lg.padding.bottom.ToString(Inv),
            "childForceExpandWidth"  => lg.childForceExpandWidth.ToString(Inv),
            "childForceExpandHeight" => lg.childForceExpandHeight.ToString(Inv),
            _                        => null
        };

        static bool SetLG(HorizontalOrVerticalLayoutGroup lg, string key, string val)
        {
            switch (key)
            {
                case "spacing":
                    if (!float.TryParse(val, NumberStyles.Float, Inv, out var f)) return false;
                    lg.spacing = f; return true;
                case "childAlignment":
                    if (!int.TryParse(val, out var a)) return false;
                    lg.childAlignment = (TextAnchor)a; return true;
                case "paddingLeft":
                    if (!int.TryParse(val, out var pl)) return false;
                    lg.padding.left = pl; return true;
                case "paddingRight":
                    if (!int.TryParse(val, out var pr)) return false;
                    lg.padding.right = pr; return true;
                case "paddingTop":
                    if (!int.TryParse(val, out var pt)) return false;
                    lg.padding.top = pt; return true;
                case "paddingBottom":
                    if (!int.TryParse(val, out var pb)) return false;
                    lg.padding.bottom = pb; return true;
                case "childForceExpandWidth":
                    if (!bool.TryParse(val, out var b1)) return false;
                    lg.childForceExpandWidth = b1; return true;
                case "childForceExpandHeight":
                    if (!bool.TryParse(val, out var b2)) return false;
                    lg.childForceExpandHeight = b2; return true;
            }
            return false;
        }

        // ── ContentSizeFitter ─────────────────────────────────────────────

        static string GetCSF(ContentSizeFitter csf, string key) => key switch
        {
            "horizontalFit" => ((int)csf.horizontalFit).ToString(Inv),
            "verticalFit"   => ((int)csf.verticalFit).ToString(Inv),
            _               => null
        };

        static bool SetCSF(ContentSizeFitter csf, string key, string val)
        {
            if (!int.TryParse(val, out var i)) return false;
            switch (key)
            {
                case "horizontalFit": csf.horizontalFit = (ContentSizeFitter.FitMode)i; return true;
                case "verticalFit":   csf.verticalFit   = (ContentSizeFitter.FitMode)i; return true;
            }
            return false;
        }

        // ── Button ────────────────────────────────────────────────────────

        static string GetBtn(Button btn, string key)
            => key == "interactable" ? btn.interactable.ToString(Inv) : null;

        static bool SetBtn(Button btn, string key, string val)
        {
            if (key != "interactable" || !bool.TryParse(val, out var b)) return false;
            btn.interactable = b; return true;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        public static string Ser(Vector2 v) =>
            $"({v.x.ToString("F4", Inv)},{v.y.ToString("F4", Inv)})";

        public static string Ser(Color c) => $"#{ColorUtility.ToHtmlStringRGBA(c)}";

        public static bool TryV2(string s, out Vector2 v)
        {
            v = Vector2.zero;
            s = s.Trim().Trim('(', ')');
            var p = s.Split(',');
            if (p.Length != 2) return false;
            if (!float.TryParse(p[0].Trim(), NumberStyles.Float, Inv, out var x) ||
                !float.TryParse(p[1].Trim(), NumberStyles.Float, Inv, out var y)) return false;
            v = new Vector2(x, y);
            return true;
        }

        public static bool TryColor(string s, out Color c) =>
            ColorUtility.TryParseHtmlString(s, out c);
    }
}
