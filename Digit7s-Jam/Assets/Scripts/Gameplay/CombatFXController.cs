using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Digit7s.Characters;
using Digit7s.UI;

namespace Digit7s.Gameplay
{
    /// <summary>
    /// Manages high-impact visual & audio combat effects for player attacks, projectiles,
    /// enemy damage impacts, hit flashes, and enemy counter-attacks using Unity ParticleSystems.
    /// </summary>
    public class CombatFXController : MonoBehaviour
    {
        private static CombatFXController instance;

        public static CombatFXController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = UnityEngine.Object.FindFirstObjectByType<CombatFXController>();
                    if (instance == null)
                    {
                        var go = new GameObject("[CombatFXController]");
                        instance = go.AddComponent<CombatFXController>();
                    }
                }
                return instance;
            }
        }

        [Header("Particle Materials")]
        [SerializeField] private Material blueStarMat;
        [SerializeField] private Material yellowStarMat;
        [SerializeField] private Material redStarMat;
        [SerializeField] private Material circleGlowMat;
        [SerializeField] private Material lightMat;

        [Header("Particle Sprites")]
        [SerializeField] private Sprite glowSprite;
        [SerializeField] private Sprite circleGlowSprite;
        [SerializeField] private Sprite rotateLineSprite;
        [SerializeField] private Sprite starYellowSprite;
        [SerializeField] private Sprite starRedSprite;
        [SerializeField] private Sprite starBlueSprite;

        [Header("Character Bullet Sprites")]
        [SerializeField] private Sprite blasterBulletSprite;
        [SerializeField] private Sprite brawlerBulletSprite;
        [SerializeField] private Sprite guardianBulletSprite;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            LoadAssetsIfNeeded();
        }

        private void LoadAssetsIfNeeded()
        {
            // Load Particle Materials
            if (blueStarMat == null) blueStarMat = Resources.Load<Material>("Particles/fx_Star_l_clear_blue");
            if (yellowStarMat == null) yellowStarMat = Resources.Load<Material>("Particles/fx_Star_l_clear_yellow");
            if (redStarMat == null) redStarMat = Resources.Load<Material>("Particles/fx_Star_l_clear_red");
            if (circleGlowMat == null) circleGlowMat = Resources.Load<Material>("Particles/fx_circle_glow01");
            if (lightMat == null) lightMat = Resources.Load<Material>("Particles/fx_light01");

            // Load Sprites
            if (glowSprite == null) glowSprite = Resources.Load<Sprite>("Particles/fx_glow");
            if (circleGlowSprite == null) circleGlowSprite = Resources.Load<Sprite>("Particles/fx_circle_glow");
            if (rotateLineSprite == null) rotateLineSprite = Resources.Load<Sprite>("Particles/fx_rotate_line");
            if (starYellowSprite == null) starYellowSprite = Resources.Load<Sprite>("Particles/fx_star_yellow");
            if (starRedSprite == null) starRedSprite = Resources.Load<Sprite>("Particles/fx_star_red");
            if (starBlueSprite == null) starBlueSprite = Resources.Load<Sprite>("Particles/fx_star_blue");

            // Load Bullet Sprites for each Character
            if (blasterBulletSprite == null) blasterBulletSprite = Resources.Load<Sprite>("Bullets/Bullet_CatBlaster") ?? Resources.Load<Sprite>("Bullets/Cat Blaster");
            if (brawlerBulletSprite == null) brawlerBulletSprite = Resources.Load<Sprite>("Bullets/Bullet_CatBrawler") ?? Resources.Load<Sprite>("Bullets/Cat Brawler");
            if (guardianBulletSprite == null) guardianBulletSprite = Resources.Load<Sprite>("Bullets/Bullet_CatGuardian") ?? Resources.Load<Sprite>("Bullets/Cat Guardian");

#if UNITY_EDITOR
            if (blueStarMat == null) blueStarMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Particles/Materials/fx_Star_l_clear_blue.mat");
            if (yellowStarMat == null) yellowStarMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Particles/Materials/fx_Star_l_clear_yellow.mat");
            if (redStarMat == null) redStarMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Particles/Materials/fx_Star_l_clear_red.mat");
            if (circleGlowMat == null) circleGlowMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Particles/Materials/fx_circle_glow01.mat");
            if (lightMat == null) lightMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Particles/Materials/fx_light01.mat");

            if (glowSprite == null) glowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Particles/Texture/fx_glow.png");
            if (circleGlowSprite == null) circleGlowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Particles/Texture/fx_circle_glow.png");
            if (rotateLineSprite == null) rotateLineSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Particles/Texture/fx_rotate_line.png");
            if (starYellowSprite == null) starYellowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Particles/Texture/fx_star_yellow.png");
            if (starRedSprite == null) starRedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Particles/Texture/fx_star_red.png");
            if (starBlueSprite == null) starBlueSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Particles/Texture/fx_star_blue.png");

            if (blasterBulletSprite == null) blasterBulletSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Bullets/Bullet_CatBlaster.png");
            if (brawlerBulletSprite == null) brawlerBulletSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Bullets/Bullet_CatBrawler.png");
            if (guardianBulletSprite == null) guardianBulletSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Bullets/Bullet_CatGuardian.png");
#endif
        }

        /// <summary>
        /// Fires a weapon projectile with ParticleSystem muzzle burst, sparkling particle trail, and impact burst.
        /// </summary>
        public void PlayPlayerAttack(Vector3 fromPos, Vector3 toPos, CatClassType catType, bool isCrit, Action onImpact)
        {
            StartCoroutine(PlayerAttackRoutine(fromPos, toPos, catType, isCrit, onImpact));
        }

        private IEnumerator PlayerAttackRoutine(Vector3 fromPos, Vector3 toPos, CatClassType catType, bool isCrit, Action onImpact)
        {
            LoadAssetsIfNeeded();

            // 1. Play weapon sound
            UISoundManager.PlayPlayerAttack();

            // Determine thematic energy color
            Color mainColor = new Color(0.25f, 0.85f, 1f, 1f); // Blaster: Cyan/Blue
            Material particleMat = blueStarMat;

            if (catType == CatClassType.Brawler)
            {
                mainColor = new Color(1f, 0.55f, 0.15f, 1f); // Brawler: Fiery Orange
                particleMat = yellowStarMat;
            }
            else if (catType == CatClassType.Guardian)
            {
                mainColor = new Color(1f, 0.85f, 0.25f, 1f); // Guardian: Holy Gold
                particleMat = yellowStarMat;
            }

            if (isCrit)
            {
                mainColor = new Color(1f, 0.25f, 0.35f, 1f); // Critical: Fierce Crimson
                particleMat = redStarMat;
            }

            // 2. Muzzle Flash Particle Burst at player weapon
            SpawnMuzzleParticles(fromPos, particleMat, mainColor);

            // 3. Projectile Bolt flying to enemy with ParticleSystem Trail
            var projObj = new GameObject("PlayerAttackBolt");
            projObj.transform.position = fromPos;

            // Orient the horizontal bullet capsule to face the flight trajectory
            Vector3 flightDir = (toPos - fromPos).normalized;
            float flightAngle = Mathf.Atan2(flightDir.y, flightDir.x) * Mathf.Rad2Deg;
            projObj.transform.rotation = Quaternion.Euler(0f, 0f, flightAngle);

            // Select bullet sprite based on character class
            Sprite chosenBulletSprite = blasterBulletSprite;
            if (catType == CatClassType.Brawler)
            {
                chosenBulletSprite = brawlerBulletSprite ?? blasterBulletSprite;
            }
            else if (catType == CatClassType.Guardian)
            {
                chosenBulletSprite = guardianBulletSprite ?? blasterBulletSprite;
            }

            var sr = projObj.AddComponent<SpriteRenderer>();
            sr.sprite = chosenBulletSprite;
            sr.color = isCrit ? new Color(1.2f, 1.2f, 1.2f, 1f) : Color.white;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 45;

            float duration = 0.14f;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * (isCrit ? 1.2f : 0.95f);
            projObj.transform.localScale = startScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeIn = t * t; // Fast acceleration curve
                projObj.transform.position = Vector3.Lerp(fromPos, toPos, easeIn);

                yield return null;
            }

            Destroy(projObj);

            // 4. Trigger Particle Impact on target
            PlayEnemyHit(toPos, isCrit, catType);
            onImpact?.Invoke();
        }

        /// <summary>
        /// Spawns a clean, pretty ParticleSystem burst at weapon muzzle.
        /// </summary>
        private void SpawnMuzzleParticles(Vector3 pos, Material mat, Color color)
        {
            var psObj = new GameObject("MuzzleParticleBurst");
            psObj.transform.position = pos;
            var ps = psObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var psr = psObj.GetComponent<ParticleSystemRenderer>();
            psr.material = mat ?? blueStarMat;
            psr.sortingLayerName = "Default";
            psr.sortingOrder = 48;

            var main = ps.main;
            main.duration = 0.20f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startColor = color;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 0.5f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            ps.Play();
        }

        /// <summary>
        /// Plays impact ParticleSystem explosion, radial sparks, and hit audio on enemy.
        /// </summary>
        public void PlayEnemyHit(Vector3 hitPos, bool isCrit = false, CatClassType catType = CatClassType.Blaster)
        {
            LoadAssetsIfNeeded();

            // 1. Play hit sound
            UISoundManager.PlayEnemyHit(isCrit);

            // 2. Determine spark material & count
            Material sparkMat = yellowStarMat;
            Color sparkColor = new Color(1f, 0.9f, 0.35f, 1f); // Gold

            if (isCrit)
            {
                sparkMat = redStarMat;
                sparkColor = new Color(1f, 0.25f, 0.3f, 1f); // Fierce Red
            }
            else if (catType == CatClassType.Blaster)
            {
                sparkMat = blueStarMat;
                sparkColor = new Color(0.35f, 0.9f, 1f, 1f); // Cyan
            }

            // 3. Spawn ParticleSystem radial burst
            var hitPsObj = new GameObject("HitParticleBurst");
            hitPsObj.transform.position = hitPos;
            var ps = hitPsObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var psr = hitPsObj.GetComponent<ParticleSystemRenderer>();
            psr.material = sparkMat ?? yellowStarMat;
            psr.sortingLayerName = "Default";
            psr.sortingOrder = 50;

            var main = ps.main;
            main.duration = 0.25f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.20f, 0.32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, isCrit ? 7.5f : 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.40f, isCrit ? 0.85f : 0.65f);
            main.startColor = sparkColor;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var em = ps.emission;
            em.rateOverTime = 0;
            int count = isCrit ? 16 : 10;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(sparkColor, 0f), new GradientColorKey(Color.white, 0.4f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            ps.Play();
        }

        /// <summary>
        /// Plays enemy counter-attack with claw slash ParticleSystem and player reaction.
        /// </summary>
        public void PlayEnemyCounterAttack(Vector3 enemyPos, Vector3 playerPos, bool isBlocked, Action onImpact)
        {
            StartCoroutine(EnemyAttackRoutine(enemyPos, playerPos, isBlocked, onImpact));
        }

        private IEnumerator EnemyAttackRoutine(Vector3 enemyPos, Vector3 playerPos, bool isBlocked, Action onImpact)
        {
            LoadAssetsIfNeeded();

            UISoundManager.PlayEnemyAttack();

            yield return new WaitForSeconds(0.12f);

            // Spawn Enemy Attack Slash ParticleSystem (clean particle burst without oversized glow/slash sprite)
            SpawnEnemyAttackParticles(playerPos, isBlocked);

            onImpact?.Invoke();
        }

        /// <summary>
        /// Spawns simple, pretty dark-crimson embers ParticleSystem slashing across the player.
        /// </summary>
        private void SpawnEnemyAttackParticles(Vector3 pos, bool isBlocked)
        {
            var psObj = new GameObject("EnemyAttackParticles");
            psObj.transform.position = pos;
            var ps = psObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var psr = psObj.GetComponent<ParticleSystemRenderer>();
            psr.material = isBlocked ? (blueStarMat ?? yellowStarMat) : (redStarMat ?? yellowStarMat);
            psr.sortingLayerName = "Default";
            psr.sortingOrder = 50;

            Color slashColor = isBlocked ? new Color(0.3f, 0.85f, 1f, 1f) : new Color(1f, 0.18f, 0.22f, 1f);

            var main = ps.main;
            main.duration = 0.22f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.26f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.0f, 6.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startColor = slashColor;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(slashColor, 0f), new GradientColorKey(Color.white, 0.4f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            ps.Play();
        }

        private readonly Dictionary<GameObject, Coroutine> activeHitReactions = new Dictionary<GameObject, Coroutine>();
        private readonly Dictionary<GameObject, Vector3> baseTargetScales = new Dictionary<GameObject, Vector3>();

        /// <summary>
        /// Visceral hit feedback: flashes Spine skeleton color momentarily and triggers punch squash/stretch.
        /// Fully protected against overlapping hits and guaranteed to restore back to default white color.
        /// </summary>
        public void TriggerHitReaction(GameObject target, bool isEnemy)
        {
            if (target == null || !target.activeInHierarchy) return;

            if (activeHitReactions.TryGetValue(target, out Coroutine running) && running != null)
            {
                StopCoroutine(running);
                activeHitReactions.Remove(target);
            }

            if (!baseTargetScales.ContainsKey(target))
            {
                baseTargetScales[target] = target.transform.localScale;
            }

            activeHitReactions[target] = StartCoroutine(HitReactionRoutine(target, isEnemy));
        }

        private IEnumerator HitReactionRoutine(GameObject target, bool isEnemy)
        {
            if (target == null) yield break;

            Vector3 origScale = baseTargetScales.ContainsKey(target) ? baseTargetScales[target] : target.transform.localScale;
            var sa = target.GetComponent<SkeletonAnimation>();
            var mr = target.GetComponent<MeshRenderer>();
            var sr = target.GetComponent<SpriteRenderer>();

            try
            {
                // 1. Flash vibrant red hit tint
                if (sa != null && sa.Skeleton != null)
                {
                    sa.Skeleton.SetColor(new Color(2.0f, 0.35f, 0.35f, 1.0f));
                }
                if (sr != null)
                {
                    sr.color = new Color(1.0f, 0.35f, 0.35f, 1.0f);
                }

                Vector3 punchScale = new Vector3(origScale.x * 1.15f, origScale.y * 0.85f, origScale.z);

                // 2. Punch squash while red (brief 0.06s flash)
                float squashDur = 0.06f;
                float t = 0f;
                while (t < squashDur && target != null)
                {
                    t += Time.deltaTime;
                    target.transform.localScale = Vector3.Lerp(origScale, punchScale, t / squashDur);
                    yield return null;
                }

                // 3. IMMEDIATELY restore default color back to clean White (never stays red)
                if (target != null)
                {
                    if (sa != null && sa.Skeleton != null)
                    {
                        sa.Skeleton.SetColor(Color.white);
                    }
                    if (mr != null && mr.material != null)
                    {
                        mr.material.color = Color.white;
                    }
                    if (sr != null)
                    {
                        sr.color = Color.white;
                    }
                }

                // 4. Rebound back to normal scale (0.08s)
                float reboundDur = 0.08f;
                t = 0f;
                while (t < reboundDur && target != null)
                {
                    t += Time.deltaTime;
                    target.transform.localScale = Vector3.Lerp(punchScale, origScale, t / reboundDur);
                    yield return null;
                }
            }
            finally
            {
                // Always guaranteed to restore scale and default white color
                if (target != null)
                {
                    target.transform.localScale = origScale;

                    if (sa != null && sa.Skeleton != null)
                    {
                        sa.Skeleton.SetColor(Color.white);
                    }
                    if (mr != null && mr.material != null)
                    {
                        mr.material.color = Color.white;
                    }
                    if (sr != null)
                    {
                        sr.color = Color.white;
                    }

                    baseTargetScales.Remove(target);
                    activeHitReactions.Remove(target);
                }
            }
        }
    }
}
