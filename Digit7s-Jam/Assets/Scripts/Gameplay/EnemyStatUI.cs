using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Digit7s.Gameplay
{
    public class EnemyStatUI : MonoBehaviour
    {
        private Canvas worldCanvas;
        private RectTransform canvasRT;
        private Text hpText;
        private Text dmgText;
        private GameObject containerObj;

        private int maxHealth = 1;
        private int currentHealth = 1;
        private int damage = 1;
        private Font runtimeFont;

        public void Initialize(int currentHp, int maxHp, int dmg)
        {
            currentHealth = currentHp;
            maxHealth = Mathf.Max(1, maxHp);
            damage = dmg;

            runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            BuildUIHierarchy();
            UpdateStats(currentHealth, maxHealth, damage);
            AlignPosition();
        }

        public void AlignPosition()
        {
            var mr = GetComponentInParent<MeshRenderer>();
            if (mr != null)
            {
                // Align world space UI right below the enemy's feet with comfortable offset for 2x scale
                float localCenterX = mr.bounds.center.x - transform.parent.position.x;
                float localBottomY = mr.bounds.min.y - transform.parent.position.y;
                transform.localPosition = new Vector3(localCenterX, localBottomY - 0.38f, -0.05f);
            }
            else
            {
                transform.localPosition = new Vector3(0f, 1.15f, -0.05f);
            }
        }

        private void BuildUIHierarchy()
        {
            if (containerObj != null && hpText != null && dmgText != null)
            {
                return;
            }

            // Clear existing if any
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var childObj = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(childObj);
                else
                    DestroyImmediate(childObj);
            }

            // 1. World Space Canvas
            worldCanvas = gameObject.GetComponent<Canvas>();
            if (worldCanvas == null)
            {
                worldCanvas = gameObject.AddComponent<Canvas>();
            }
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingLayerName = "Default";
            worldCanvas.sortingOrder = 30;

            canvasRT = GetComponent<RectTransform>();
            // Scale doubled (0.0075f -> 0.015f) for x2 visual size
            canvasRT.sizeDelta = new Vector2(160f, 44f);
            canvasRT.localScale = new Vector3(0.015f, 0.015f, 1f);

            // 2. Container Background Card
            containerObj = new GameObject("StatCard");
            containerObj.transform.SetParent(transform, false);
            var cardRT = containerObj.AddComponent<RectTransform>();
            cardRT.anchorMin = Vector2.zero;
            cardRT.anchorMax = Vector2.one;
            cardRT.offsetMin = Vector2.zero;
            cardRT.offsetMax = Vector2.zero;

            var cardBg = containerObj.AddComponent<Image>();
            cardBg.color = new Color(0.06f, 0.08f, 0.12f, 0.90f);
            cardBg.raycastTarget = false;

            // Border Outline
            var outline = containerObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.65f, 0.2f, 0.7f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // 3. Stats Row (HP and DMG) with Icon_Heart and Icon_Sword
            var statsRowObj = new GameObject("StatsRow");
            statsRowObj.transform.SetParent(containerObj.transform, false);
            var statsRowRT = statsRowObj.AddComponent<RectTransform>();
            statsRowRT.anchorMin = Vector2.zero;
            statsRowRT.anchorMax = Vector2.one;
            statsRowRT.offsetMin = new Vector2(8f, 0f);
            statsRowRT.offsetMax = new Vector2(-8f, 0f);

            var heartSprite = LoadIconSprite("Icons/Icon_Heart", "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Heart.png");
            var swordSprite = LoadIconSprite("Icons/Icon_Sword", "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Sword.png");

            // Left: HP Section (Icon_Heart + HP Value)
            var hpGroup = new GameObject("HpGroup");
            hpGroup.transform.SetParent(statsRowObj.transform, false);
            var hpGroupRT = hpGroup.AddComponent<RectTransform>();
            hpGroupRT.anchorMin = new Vector2(0f, 0f);
            hpGroupRT.anchorMax = new Vector2(0.5f, 1f);
            hpGroupRT.offsetMin = Vector2.zero;
            hpGroupRT.offsetMax = Vector2.zero;

            var hpIconObj = new GameObject("HpIcon");
            hpIconObj.transform.SetParent(hpGroup.transform, false);
            var hpIconRT = hpIconObj.AddComponent<RectTransform>();
            hpIconRT.anchorMin = new Vector2(0f, 0.5f);
            hpIconRT.anchorMax = new Vector2(0f, 0.5f);
            hpIconRT.pivot = new Vector2(0f, 0.5f);
            hpIconRT.anchoredPosition = new Vector2(0f, 0f);
            hpIconRT.sizeDelta = new Vector2(24f, 24f);

            var hpIcon = hpIconObj.AddComponent<Image>();
            hpIcon.sprite = heartSprite;
            hpIcon.preserveAspect = true;
            hpIcon.raycastTarget = false;

            var hpTextObj = new GameObject("HpText");
            hpTextObj.transform.SetParent(hpGroup.transform, false);
            var hpTextRT = hpTextObj.AddComponent<RectTransform>();
            hpTextRT.anchorMin = new Vector2(0f, 0f);
            hpTextRT.anchorMax = new Vector2(1f, 1f);
            hpTextRT.offsetMin = new Vector2(28f, 0f);
            hpTextRT.offsetMax = Vector2.zero;

            hpText = hpTextObj.AddComponent<Text>();
            hpText.font = runtimeFont;
            hpText.fontSize = 22;
            hpText.fontStyle = FontStyle.Bold;
            hpText.alignment = TextAnchor.MiddleLeft;
            hpText.color = Color.white;
            hpText.raycastTarget = false;
            var hpShadow = hpTextObj.AddComponent<Shadow>();
            hpShadow.effectColor = Color.black;
            hpShadow.effectDistance = new Vector2(1.2f, -1.2f);

            // Right: DMG Section (Icon_Sword + DMG Value)
            var dmgGroup = new GameObject("DmgGroup");
            dmgGroup.transform.SetParent(statsRowObj.transform, false);
            var dmgGroupRT = dmgGroup.AddComponent<RectTransform>();
            dmgGroupRT.anchorMin = new Vector2(0.5f, 0f);
            dmgGroupRT.anchorMax = new Vector2(1f, 1f);
            dmgGroupRT.offsetMin = Vector2.zero;
            dmgGroupRT.offsetMax = Vector2.zero;

            var dmgIconObj = new GameObject("DmgIcon");
            dmgIconObj.transform.SetParent(dmgGroup.transform, false);
            var dmgIconRT = dmgIconObj.AddComponent<RectTransform>();
            dmgIconRT.anchorMin = new Vector2(0f, 0.5f);
            dmgIconRT.anchorMax = new Vector2(0f, 0.5f);
            dmgIconRT.pivot = new Vector2(0f, 0.5f);
            dmgIconRT.anchoredPosition = new Vector2(4f, 0f);
            dmgIconRT.sizeDelta = new Vector2(24f, 24f);

            var dmgIcon = dmgIconObj.AddComponent<Image>();
            dmgIcon.sprite = swordSprite;
            dmgIcon.preserveAspect = true;
            dmgIcon.raycastTarget = false;

            var dmgTextObj = new GameObject("DmgText");
            dmgTextObj.transform.SetParent(dmgGroup.transform, false);
            var dmgTextRT = dmgTextObj.AddComponent<RectTransform>();
            dmgTextRT.anchorMin = new Vector2(0f, 0f);
            dmgTextRT.anchorMax = new Vector2(1f, 1f);
            dmgTextRT.offsetMin = new Vector2(32f, 0f);
            dmgTextRT.offsetMax = Vector2.zero;

            dmgText = dmgTextObj.AddComponent<Text>();
            dmgText.font = runtimeFont;
            dmgText.fontSize = 22;
            dmgText.fontStyle = FontStyle.Bold;
            dmgText.alignment = TextAnchor.MiddleLeft;
            dmgText.color = new Color(1f, 0.85f, 0.3f, 1f); // Vibrant Gold
            dmgText.raycastTarget = false;
            var dmgShadow = dmgTextObj.AddComponent<Shadow>();
            dmgShadow.effectColor = Color.black;
            dmgShadow.effectDistance = new Vector2(1.2f, -1.2f);
        }

        private static Sprite LoadIconSprite(string resourceName, string fallbackPath)
        {
            var sp = Resources.Load<Sprite>(resourceName);
            if (sp != null) return sp;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(fallbackPath))
            {
                sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(fallbackPath);
            }
#endif
            return sp;
        }

        public void UpdateStats(int curHp, int maxHp, int dmg)
        {
            currentHealth = Mathf.Max(0, curHp);
            maxHealth = Mathf.Max(1, maxHp);
            damage = dmg;

            if (hpText != null)
            {
                hpText.text = $"{currentHealth}";
            }

            if (dmgText != null)
            {
                dmgText.text = $"{damage}";
            }
        }

        public void ShowDamagePopup(int amount)
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(DamagePopupRoutine(amount));
        }

        public void ShowStatusPopup(string message, Color color)
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(StatusPopupRoutine(message, color));
        }

        public void SetStunned(int remainingClicks)
        {
            if (dmgText != null)
            {
                dmgText.text = $"<color=#FFD700><b>STUN({remainingClicks})</b></color>";
            }
        }

        public void ClearStun(int curHp, int maxHp, int dmg)
        {
            UpdateStats(curHp, maxHp, dmg);
        }

        private IEnumerator StatusPopupRoutine(string message, Color color)
        {
            var popupObj = new GameObject("StatusPopup");
            popupObj.transform.SetParent(transform, false);

            var rt = popupObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160f, 40f);
            rt.anchoredPosition = new Vector2(0f, 55f);

            var txt = popupObj.AddComponent<Text>();
            txt.font = runtimeFont;
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = message;
            txt.color = color;
            txt.raycastTarget = false;

            var outline = popupObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2f, -2f);

            float duration = 0.8f;
            float elapsed = 0f;
            Vector2 startPos = rt.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, 40f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                txt.color = new Color(color.r, color.g, color.b, 1f - t);
                yield return null;
            }

            Destroy(popupObj);
        }

        private IEnumerator DamagePopupRoutine(int amount)
        {
            var popupObj = new GameObject("DamagePopup");
            popupObj.transform.SetParent(transform, false);

            var rt = popupObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120f, 40f);
            rt.anchoredPosition = new Vector2(0f, 40f);

            var txt = popupObj.AddComponent<Text>();
            txt.font = runtimeFont;
            txt.fontSize = 28;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = $"-{amount}";
            txt.color = new Color(1f, 0.25f, 0.25f, 1f);
            txt.raycastTarget = false;

            var outline = popupObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2f, -2f);

            float duration = 0.6f;
            float elapsed = 0f;
            Vector2 startPos = rt.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, 35f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                txt.color = new Color(1f, 0.25f, 0.25f, 1f - t);
                yield return null;
            }

            Destroy(popupObj);
        }

        public void SetVisible(bool visible)
        {
            if (containerObj != null)
            {
                containerObj.SetActive(visible);
            }
        }

        public void SetAllyTheme()
        {
            if (containerObj != null)
            {
                var cardBg = containerObj.GetComponent<Image>();
                if (cardBg != null)
                {
                    cardBg.color = new Color(0.04f, 0.14f, 0.10f, 0.92f);
                }

                var outline = containerObj.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = new Color(0.25f, 0.95f, 0.55f, 0.90f);
                }
            }

            if (hpText != null)
            {
                hpText.color = new Color(0.45f, 1f, 0.65f, 1f);
            }
        }
    }
}


