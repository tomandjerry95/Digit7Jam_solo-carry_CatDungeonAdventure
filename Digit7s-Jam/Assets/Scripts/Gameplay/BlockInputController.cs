using System.Collections.Generic;
using UnityEngine;

namespace Digit7s.Gameplay
{
    /// <summary>
    /// Handles single-target click and touch release to attack enemies, use ladders, break props, or destroy blocks.
    /// Does NOT drag/draw cut. Only triggers on release over the target.
    /// </summary>
    public class BlockInputController : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("Layer mask for blocks to detect. Set to Everything (default) or a specific Block layer.")]
        [SerializeField] private LayerMask blockLayer = ~0;

        [Tooltip("Small touch tolerance radius around the release position.")]
        [SerializeField] private float tapRadius = 0.2f;

        public static bool IsModalBlockingInput { get; set; } = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticInputBlock()
        {
            IsModalBlockingInput = false;
        }

        private Camera mainCamera;

        // Tracks targets where each finger/pointer started pressing down
        private Dictionary<int, DestructibleBlock> pressedBlocksByFinger = new Dictionary<int, DestructibleBlock>();
        private Dictionary<int, EnemyInstance> pressedEnemiesByFinger = new Dictionary<int, EnemyInstance>();
        private Dictionary<int, LevelLadder> pressedLaddersByFinger = new Dictionary<int, LevelLadder>();
        private Dictionary<int, WoodenBreakableProp> pressedPropsByFinger = new Dictionary<int, WoodenBreakableProp>();
        private Dictionary<int, ItemPickupProp> pressedItemsByFinger = new Dictionary<int, ItemPickupProp>();

        private DestructibleBlock mousePressedBlock;
        private EnemyInstance mousePressedEnemy;
        private LevelLadder mousePressedLadder;
        private WoodenBreakableProp mousePressedProp;
        private ItemPickupProp mousePressedItem;

        private void Awake()
        {
            mainCamera = Camera.main;
            Input.simulateMouseWithTouches = false; // We handle touches directly for precise touch-release
        }

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (IsModalBlockingInput)
            {
                ResetMouseTargets();
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            // 1. Mobile Touch Input
            if (Input.touchCount > 0)
            {
                HandleTouchInput();
            }
            // 2. Mouse Input (Desktop / Editor)
            else
            {
                HandleMouseInput();
            }
        }

        private void HandleMouseInput()
        {
            // Click Down: record target enemy, ladder, prop, or block
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 worldPos = GetWorldPosition(Input.mousePosition);
                mousePressedEnemy = GetEnemyAtPosition(worldPos);
                if (mousePressedEnemy == null)
                {
                    mousePressedLadder = GetLadderAtPosition(worldPos);
                    if (mousePressedLadder == null)
                    {
                        mousePressedItem = GetItemAtPosition(worldPos);
                        if (mousePressedItem == null)
                        {
                            mousePressedProp = GetPropAtPosition(worldPos);
                            if (mousePressedProp == null)
                            {
                                var block = GetBlockAtPosition(worldPos);
                                if (block != null)
                                {
                                    if (block.IsClickable())
                                    {
                                        mousePressedBlock = block;
                                    }
                                    else
                                    {
                                        if (EnemyInstance.IsBlockLocked(block.transform.position))
                                        {
                                            CatPowerUpController.TriggerNotification("Defeat nearby enemy first!", new Color(1f, 0.4f, 0.4f));
                                        }
                                        else if (!DestructibleBlock.IsAdjacentToClearedSpace(block.transform.position))
                                        {
                                            CatPowerUpController.TriggerNotification("Break adjacent blocks first!", new Color(0.7f, 0.7f, 0.7f));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // Release (Mouse Up): trigger attack, ladder, prop, or break block
            else if (Input.GetMouseButtonUp(0))
            {
                Vector2 releasePos = GetWorldPosition(Input.mousePosition);

                // 1. Check Enemy first (must have pressed down on this specific attackable enemy)
                EnemyInstance releasedEnemy = GetEnemyAtPosition(releasePos);
                if (releasedEnemy != null && mousePressedEnemy != null && releasedEnemy == mousePressedEnemy)
                {
                    releasedEnemy.HandlePlayerAttack();
                    ResetMouseTargets();
                    return;
                }

                // 2. Check Ladder (must have pressed down on this specific interactable ladder)
                LevelLadder releasedLadder = GetLadderAtPosition(releasePos);
                if (releasedLadder != null && mousePressedLadder != null && releasedLadder == mousePressedLadder)
                {
                    releasedLadder.OnLadderClicked();
                    ResetMouseTargets();
                    return;
                }

                // 2b. Check Item Pickup (must have pressed down on this specific interactable item)
                ItemPickupProp releasedItem = GetItemAtPosition(releasePos);
                if (releasedItem != null && mousePressedItem != null && releasedItem == mousePressedItem)
                {
                    releasedItem.Collect();
                    ResetMouseTargets();
                    return;
                }

                // 3. Check Wooden Prop (must have pressed down on this specific interactable prop)
                WoodenBreakableProp releasedProp = GetPropAtPosition(releasePos);
                if (releasedProp != null && mousePressedProp != null && releasedProp == mousePressedProp)
                {
                    releasedProp.BreakProp();
                    ResetMouseTargets();
                    return;
                }

                // 4. Check Block (must have pressed down on this specific block)
                DestructibleBlock releasedBlock = GetBlockAtPosition(releasePos);
                if (releasedBlock != null && mousePressedBlock != null && releasedBlock == mousePressedBlock)
                {
                    if (releasedBlock.IsClickable())
                    {
                        releasedBlock.BreakBlock();
                    }
                }

                ResetMouseTargets();
            }
        }

        private void ResetMouseTargets()
        {
            mousePressedEnemy = null;
            mousePressedLadder = null;
            mousePressedItem = null;
            mousePressedProp = null;
            mousePressedBlock = null;
        }

        private void HandleTouchInput()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector2 worldPos = GetWorldPosition(touch.position);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        var enemy = GetEnemyAtPosition(worldPos);
                        if (enemy != null)
                        {
                            pressedEnemiesByFinger[touch.fingerId] = enemy;
                        }
                        else
                        {
                            var ladder = GetLadderAtPosition(worldPos);
                            if (ladder != null)
                            {
                                pressedLaddersByFinger[touch.fingerId] = ladder;
                            }
                            else
                            {
                                var item = GetItemAtPosition(worldPos);
                                if (item != null)
                                {
                                    pressedItemsByFinger[touch.fingerId] = item;
                                }
                                else
                                {
                                    var prop = GetPropAtPosition(worldPos);
                                    if (prop != null)
                                    {
                                        pressedPropsByFinger[touch.fingerId] = prop;
                                    }
                                    else
                                    {
                                        var block = GetBlockAtPosition(worldPos);
                                        if (block != null)
                                        {
                                            if (block.IsClickable())
                                            {
                                                pressedBlocksByFinger[touch.fingerId] = block;
                                            }
                                            else
                                            {
                                                if (EnemyInstance.IsBlockLocked(block.transform.position))
                                                {
                                                    CatPowerUpController.TriggerNotification("Defeat nearby enemy first!", new Color(1f, 0.4f, 0.4f));
                                                }
                                                else if (!DestructibleBlock.IsAdjacentToClearedSpace(block.transform.position))
                                                {
                                                    CatPowerUpController.TriggerNotification("Break adjacent blocks first!", new Color(0.7f, 0.7f, 0.7f));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        break;

                    case TouchPhase.Ended:
                        // 1. Enemy (must have started touch on this specific attackable enemy)
                        EnemyInstance releasedEnemy = GetEnemyAtPosition(worldPos);
                        pressedEnemiesByFinger.TryGetValue(touch.fingerId, out EnemyInstance initialEnemy);
                        if (releasedEnemy != null && initialEnemy != null && releasedEnemy == initialEnemy)
                        {
                            releasedEnemy.HandlePlayerAttack();
                            ClearTouchTargets(touch.fingerId);
                            break;
                        }

                        // 2. Ladder (must have started touch on this specific interactable ladder)
                        LevelLadder releasedLadder = GetLadderAtPosition(worldPos);
                        pressedLaddersByFinger.TryGetValue(touch.fingerId, out LevelLadder initialLadder);
                        if (releasedLadder != null && initialLadder != null && releasedLadder == initialLadder)
                        {
                            releasedLadder.OnLadderClicked();
                            ClearTouchTargets(touch.fingerId);
                            break;
                        }

                        // 2b. Item Pickup (must have started touch on this specific interactable item)
                        ItemPickupProp releasedItem = GetItemAtPosition(worldPos);
                        pressedItemsByFinger.TryGetValue(touch.fingerId, out ItemPickupProp initialItem);
                        if (releasedItem != null && initialItem != null && releasedItem == initialItem)
                        {
                            releasedItem.Collect();
                            ClearTouchTargets(touch.fingerId);
                            break;
                        }

                        // 3. Wooden Prop (must have started touch on this specific interactable prop)
                        WoodenBreakableProp releasedProp = GetPropAtPosition(worldPos);
                        pressedPropsByFinger.TryGetValue(touch.fingerId, out WoodenBreakableProp initialProp);
                        if (releasedProp != null && initialProp != null && releasedProp == initialProp)
                        {
                            releasedProp.BreakProp();
                            ClearTouchTargets(touch.fingerId);
                            break;
                        }

                        // 4. Block (must have started touch on this specific block)
                        DestructibleBlock releasedBlock = GetBlockAtPosition(worldPos);
                        pressedBlocksByFinger.TryGetValue(touch.fingerId, out DestructibleBlock initialBlock);

                        if (releasedBlock != null && initialBlock != null && releasedBlock == initialBlock)
                        {
                            if (releasedBlock.IsClickable())
                            {
                                releasedBlock.BreakBlock();
                            }
                        }

                        ClearTouchTargets(touch.fingerId);
                        break;

                    case TouchPhase.Canceled:
                        ClearTouchTargets(touch.fingerId);
                        break;
                }
            }
        }

        private void ClearTouchTargets(int fingerId)
        {
            pressedEnemiesByFinger.Remove(fingerId);
            pressedLaddersByFinger.Remove(fingerId);
            pressedItemsByFinger.Remove(fingerId);
            pressedPropsByFinger.Remove(fingerId);
            pressedBlocksByFinger.Remove(fingerId);
        }

        private EnemyInstance GetEnemyAtPosition(Vector2 worldPos)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
            {
                var enemy = hit.GetComponent<EnemyInstance>() ?? hit.GetComponentInParent<EnemyInstance>();
                if (enemy != null && enemy.IsAttackable && !enemy.IsDead) return enemy;
            }

            if (tapRadius > 0f)
            {
                Collider2D circleHit = Physics2D.OverlapCircle(worldPos, tapRadius);
                if (circleHit != null)
                {
                    var enemy = circleHit.GetComponent<EnemyInstance>() ?? circleHit.GetComponentInParent<EnemyInstance>();
                    if (enemy != null && enemy.IsAttackable && !enemy.IsDead) return enemy;
                }
            }

            return null;
        }

        private LevelLadder GetLadderAtPosition(Vector2 worldPos)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
            {
                var ladder = hit.GetComponent<LevelLadder>() ?? hit.GetComponentInParent<LevelLadder>();
                if (ladder != null && ladder.IsInteractable && !ladder.IsTransitioning) return ladder;
            }

            if (tapRadius > 0f)
            {
                Collider2D circleHit = Physics2D.OverlapCircle(worldPos, tapRadius);
                if (circleHit != null)
                {
                    var ladder = circleHit.GetComponent<LevelLadder>() ?? circleHit.GetComponentInParent<LevelLadder>();
                    if (ladder != null && ladder.IsInteractable && !ladder.IsTransitioning) return ladder;
                }
            }

            return null;
        }

        private ItemPickupProp GetItemAtPosition(Vector2 worldPos)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
            {
                var item = hit.GetComponent<ItemPickupProp>() ?? hit.GetComponentInParent<ItemPickupProp>();
                if (item != null && item.IsInteractable && !item.IsCollected) return item;
            }

            if (tapRadius > 0f)
            {
                Collider2D circleHit = Physics2D.OverlapCircle(worldPos, tapRadius);
                if (circleHit != null)
                {
                    var item = circleHit.GetComponent<ItemPickupProp>() ?? circleHit.GetComponentInParent<ItemPickupProp>();
                    if (item != null && item.IsInteractable && !item.IsCollected) return item;
                }
            }

            return null;
        }

        private WoodenBreakableProp GetPropAtPosition(Vector2 worldPos)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
            {
                var prop = hit.GetComponent<WoodenBreakableProp>() ?? hit.GetComponentInParent<WoodenBreakableProp>();
                if (prop != null && prop.IsInteractable && !prop.IsBroken) return prop;
            }

            if (tapRadius > 0f)
            {
                Collider2D circleHit = Physics2D.OverlapCircle(worldPos, tapRadius);
                if (circleHit != null)
                {
                    var prop = circleHit.GetComponent<WoodenBreakableProp>() ?? circleHit.GetComponentInParent<WoodenBreakableProp>();
                    if (prop != null && prop.IsInteractable && !prop.IsBroken) return prop;
                }
            }

            return null;
        }

        private DestructibleBlock GetBlockAtPosition(Vector2 worldPos)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos, blockLayer);
            if (hit != null)
            {
                var block = hit.GetComponent<DestructibleBlock>() ?? hit.GetComponentInParent<DestructibleBlock>();
                if (block != null && !block.IsDestroyed) return block;
            }

            if (tapRadius > 0f)
            {
                Collider2D circleHit = Physics2D.OverlapCircle(worldPos, tapRadius, blockLayer);
                if (circleHit != null)
                {
                    var block = circleHit.GetComponent<DestructibleBlock>() ?? circleHit.GetComponentInParent<DestructibleBlock>();
                    if (block != null && !block.IsDestroyed) return block;
                }
            }

            return null;
        }

        private Vector2 GetWorldPosition(Vector2 screenPos)
        {
            if (mainCamera == null) return screenPos;
            Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));
            return new Vector2(world.x, world.y);
        }
    }
}
