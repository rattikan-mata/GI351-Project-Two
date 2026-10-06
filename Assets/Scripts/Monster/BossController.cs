using System.Collections;
using UnityEngine;

public class BossController : Monster
{
    private enum BossSkill { Dash, ProjectileBarrage, GroundSlam, CrazyDash, TrackingShoot }

    #region Boss Info
    [Header("Boss Info")]
    [Tooltip("ชื่อบอส (ใช้โชว์บน UI และ Debug Log)")]
    [SerializeField] private string bossName = "Boss";

    [Header("Exit Portal")]
    [Tooltip("ประตูวาร์ปที่จะปรากฏขึ้นเมื่อบอสตัวนี้ตาย")]
    [SerializeField] private GameObject exitPortal;
    #endregion

    #region Boss Proximity & Intro Shake
    [Header("Boss UI & Encounter")]
    [Tooltip("รัศมีที่ผู้เล่นเข้าใกล้แล้วหลอดเลือดบอสกลางจอจะแสดง")]
    [SerializeField] private float bossUIRadius = 12f;

    [Tooltip("ระยะเวลาการสั่นของหน้าจอตอนเจอบอส (วินาที)")]
    [SerializeField] private float encounterShakeDuration = 3f;

    [Tooltip("ความแรงของการสั่นหน้าจอตอนเจอบอส (ค่ายิ่งน้อยยิ่งนุ่มนวล)")]
    [SerializeField] private float encounterShakeMagnitude = 0.08f;

    private bool hasTriggeredShake = false;
    private bool isPlayerInRadius = false;
    #endregion

    #region Skill Rotation
    [Header("Skill Rotation")]
    [Tooltip("ระยะที่บอสต้องเห็นผู้เล่นถึงจะเริ่มออกท่าได้ ถ้าไกลกว่านี้จะวิ่งไล่เข้ามาก่อนแบบมอนปกติ")]
    [SerializeField] private float skillRange = 8f;

    [Tooltip("หน่วงเวลาก่อนเริ่มสุ่มออกท่าถัดไป หลังจากท่าก่อนหน้าจบ (วินาที)")]
    [SerializeField] private float skillCooldown = 1.5f;

    private bool isUsingSkill = false;
    private float nextSkillReadyTime = 0f;
    #endregion

    #region Area Indicator & Line Indicator
    [Header("Area Indicator (โชว์ขนาดจริงตอน Dash/Slam)")]
    [SerializeField] private GameObject areaIndicatorPrefab;
    [SerializeField] private Color dashIndicatorColor = new Color(1f, 0.5f, 0f, 0.5f);
    [SerializeField] private Color slamIndicatorColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("Line Indicator (เตือนทิศกระสุนล่วงหน้า)")]
    [SerializeField] private GameObject lineIndicatorPrefab;
    [SerializeField] private float lineIndicatorWidth = 0.3f;
    #endregion

    #region 1) Dash Charge
    [Header("1) Dash Charge (ใช้ค่าจาก Header \"Dash Attack\" ของ Monster ด้านบนทั้งหมด)")]
    [SerializeField] private bool dashSkillEnabled = true;
    [SerializeField] private float dashWeight = 1f;
    #endregion

    #region 2) Projectile Barrage
    [Header("2) Projectile Barrage")]
    [SerializeField] private bool projectileSkillEnabled = true;
    [SerializeField] private float projectileWeight = 1f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int projectileCount = 6;
    [SerializeField] private float projectileArcAngle = 60f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private int projectileDamage = 8;
    [SerializeField] private float projectileRange = 10f;
    [SerializeField] private float projectileKnockbackForce = 4f;
    [SerializeField] private Color projectileTelegraphColor = Color.magenta;
    [SerializeField] private float projectileTelegraphDuration = 0.6f;
    [SerializeField] private float projectileBlinkInterval = 0.1f;
    #endregion

    #region 3) Ground Slam
    [Header("3) Ground Slam")]
    [SerializeField] private bool slamSkillEnabled = true;
    [SerializeField] private float slamWeight = 1f;
    [SerializeField] private float slamChargeSpeed = 9f;
    [SerializeField] private float slamChargeDuration = 0.4f;
    [SerializeField] private float slamRadius = 2.5f;
    [SerializeField] private int slamDamage = 12;
    [SerializeField] private float slamKnockbackForce = 8f;
    [SerializeField] private Color slamTelegraphColor = Color.red;
    [SerializeField] private float slamTelegraphDuration = 0.7f;
    [SerializeField] private float slamBlinkInterval = 0.1f;
    [SerializeField] private GameObject slamEffectPrefab;
    [SerializeField] private float slamEffectLifetime = 1f;
    #endregion

    #region 4) Crazy Dash (ปอบคลั่ง)
    [Header("4) Crazy Dash (วิ่งคลั่งชนกำแพง)")]
    [SerializeField] private bool crazyDashEnabled = false;
    [SerializeField] private float crazyDashWeight = 1f;
    [SerializeField] private float crazyDashSpeed = 15f;
    [SerializeField] private float crazyDashWarningTime = 0.5f;
    [SerializeField] private float crazyDashMaxTime = 3f;
    [SerializeField] private int crazyDashImpactDamage = 15;
    [SerializeField] private float crazyDashSelfStunDuration = 2f;
    #endregion

    #region 5) Tracking Shoot (ยิงตามเป้าทีละนัด)
    [Header("5) Tracking Shoot (ยิงตามเป้า)")]
    [SerializeField] private bool trackingShootEnabled = false;
    [SerializeField] private float trackingShootWeight = 1f;
    [SerializeField] private int trackingShootCount = 3;
    [SerializeField] private float trackingShootDelay = 0.4f;
    #endregion

    #region Unity Lifecycle & Detection
    protected override void Update()
    {
        base.Update();
        HandleBossEncounterRadius();
    }

    private void HandleBossEncounterRadius()
    {
        if (isDead || playerTransform == null)
        {
            if (isPlayerInRadius)
            {
                isPlayerInRadius = false;
                UIBossHealthBar.Instance?.HideBossBar(this);
            }
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance <= bossUIRadius)
        {
            if (!isPlayerInRadius)
            {
                isPlayerInRadius = true;
                UIBossHealthBar.Instance?.ShowBossBar(this, bossName);

                if (!hasTriggeredShake)
                {
                    hasTriggeredShake = true;
                    UIManager.Instance?.TriggerScreenShake(encounterShakeDuration, encounterShakeMagnitude);
                }
            }
        }
        else
        {
            if (isPlayerInRadius)
            {
                isPlayerInRadius = false;
                UIBossHealthBar.Instance?.HideBossBar(this);
            }
        }
    }

    protected override void FixedUpdate()
    {
        if (isDead || playerTransform == null) return;
        if (isKnockedBack) return;
        if (isUsingSkill) return;

        if (isStunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance <= skillRange && Time.time >= nextSkillReadyTime)
        {
            BossSkill? skill = ChooseSkill(distance);
            if (skill.HasValue)
            {
                StartCoroutine(UseSkillRoutine(skill.Value));
                return;
            }
        }

        ChasePlayerIfInRange();
    }
    #endregion

    #region Damage & Death
    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount); 
        if (UIBossHealthBar.Instance != null)
        {
            UIBossHealthBar.Instance.OnBossTakeDamage(this);
        }
    }

    public override void Die()
    {
        if (isDead) return;

        if (exitPortal != null) exitPortal.SetActive(true);

        if (bossName == "Tani")
        {
            StoryManager.Instance.isTaniDefeated = true;
            Debug.Log("ปลดล็อกวาร์ปคอกวัวแล้ว!");
        }

        UIBossHealthBar.Instance?.HideBossBar(this);
        base.Die();
    }
    #endregion

    #region Skill System
    private BossSkill? ChooseSkill(float distanceToPlayer)
    {
        bool dashAvailable = dashSkillEnabled && distanceToPlayer <= dashAttackRange;
        bool projectileAvailable = projectileSkillEnabled && projectilePrefab != null;
        bool slamAvailable = slamSkillEnabled;
        bool crazyDashAvailable = crazyDashEnabled;
        bool trackingShootAvailable = trackingShootEnabled && projectilePrefab != null;

        float totalWeight = 0f;
        if (dashAvailable) totalWeight += Mathf.Max(0f, dashWeight);
        if (projectileAvailable) totalWeight += Mathf.Max(0f, projectileWeight);
        if (slamAvailable) totalWeight += Mathf.Max(0f, slamWeight);
        if (crazyDashAvailable) totalWeight += Mathf.Max(0f, crazyDashWeight);
        if (trackingShootAvailable) totalWeight += Mathf.Max(0f, trackingShootWeight);

        if (totalWeight <= 0f) return null;

        float roll = Random.value * totalWeight;
        float cumulative = 0f;

        if (dashAvailable) { cumulative += Mathf.Max(0f, dashWeight); if (roll <= cumulative) return BossSkill.Dash; }
        if (projectileAvailable) { cumulative += Mathf.Max(0f, projectileWeight); if (roll <= cumulative) return BossSkill.ProjectileBarrage; }
        if (slamAvailable) { cumulative += Mathf.Max(0f, slamWeight); if (roll <= cumulative) return BossSkill.GroundSlam; }
        if (crazyDashAvailable) { cumulative += Mathf.Max(0f, crazyDashWeight); if (roll <= cumulative) return BossSkill.CrazyDash; }
        if (trackingShootAvailable) { cumulative += Mathf.Max(0f, trackingShootWeight); if (roll <= cumulative) return BossSkill.TrackingShoot; }

        return null;
    }

    private IEnumerator UseSkillRoutine(BossSkill skill)
    {
        isUsingSkill = true;
        rb.linearVelocity = Vector2.zero;

        switch (skill)
        {
            case BossSkill.Dash:
                SpawnAreaIndicator(transform.position, dashAttackRange, dashIndicatorColor, warningDuration + dashDuration + 0.3f);
                AudioManager.Instance?.PlaySFX("boss_dash");
                yield return StartCoroutine(DashAttackRoutine());
                break;
            case BossSkill.ProjectileBarrage:
                yield return StartCoroutine(ProjectileBarrageRoutine());
                break;
            case BossSkill.GroundSlam:
                yield return StartCoroutine(GroundSlamRoutine());
                break;
            case BossSkill.CrazyDash:
                yield return StartCoroutine(CrazyDashRoutine());
                break;
            case BossSkill.TrackingShoot:
                yield return StartCoroutine(TrackingShootRoutine());
                break;
        }

        isUsingSkill = false;
        nextSkillReadyTime = Time.time + skillCooldown;
    }

    private IEnumerator ProjectileBarrageRoutine()
    {
        if (isDead || isStunned || playerTransform == null) yield break;

        Vector2 baseDir = (Vector2)playerTransform.position - (Vector2)transform.position;
        baseDir = baseDir.sqrMagnitude > 0.0001f ? baseDir.normalized : Vector2.down;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        int count = Mathf.Max(1, projectileCount);
        float halfArc = projectileArcAngle * 0.5f;
        Vector2[] pelletDirs = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0.5f : (float)i / (count - 1);
            float angle = baseAngle - halfArc + (projectileArcAngle * t);
            pelletDirs[i] = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            SpawnLineIndicator(transform.position, pelletDirs[i], projectileRange, lineIndicatorWidth, projectileTelegraphColor, projectileTelegraphDuration);
        }

        yield return StartCoroutine(TelegraphFlash(projectileTelegraphColor, projectileTelegraphDuration, projectileBlinkInterval));
        if (isDead || isStunned) yield break;

        AudioManager.Instance?.PlaySFX("boss_shoot");
        for (int i = 0; i < count; i++)
        {
            GameObject projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            if (projObj.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.Init(pelletDirs[i], projectileSpeed, projectileDamage, projectileRange, projectileKnockbackForce);
            }
        }
    }

    private IEnumerator GroundSlamRoutine()
    {
        if (isDead || isStunned || playerTransform == null) yield break;

        Vector2 targetPos = playerTransform.position;
        float totalTelegraphTime = slamTelegraphDuration + slamChargeDuration;
        SpawnAreaIndicator(targetPos, slamRadius, slamIndicatorColor, totalTelegraphTime);

        yield return StartCoroutine(TelegraphFlash(slamTelegraphColor, slamTelegraphDuration, slamBlinkInterval));
        if (isDead || isStunned) yield break;

        float elapsed = 0f;
        while (elapsed < slamChargeDuration && !isDead && !isStunned)
        {
            Vector2 dir = (targetPos - rb.position).normalized;
            rb.linearVelocity = dir * slamChargeSpeed;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rb.linearVelocity = Vector2.zero;

        if (isDead || isStunned) yield break;

        AudioManager.Instance?.PlaySFX("boss_slam");
        if (slamEffectPrefab != null)
        {
            GameObject fx = Instantiate(slamEffectPrefab, targetPos, Quaternion.identity);
            Destroy(fx, slamEffectLifetime);
        }

        DrawDebugCircle(targetPos, slamRadius, Color.red, 0.75f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, slamRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject.CompareTag("Player") && hit.TryGetComponent<PlayerController>(out var player))
            {
                player.TakeDamage(slamDamage);
                Vector2 dir = ((Vector2)player.transform.position - targetPos).normalized;
                player.ApplyKnockback(dir, slamKnockbackForce);
                break;
            }
        }
    }

    private IEnumerator CrazyDashRoutine()
    {
        if (isDead || isStunned || playerTransform == null) yield break;

        yield return StartCoroutine(TelegraphFlash(Color.red, crazyDashWarningTime, 0.05f));
        if (isDead || isStunned) yield break;

        Vector2 dashDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        AudioManager.Instance?.PlaySFX("monster_dash"); 

        float elapsed = 0f;
        bool hitObstacle = false;

        while (elapsed < crazyDashMaxTime && !isDead && !isStunned)
        {
            rb.linearVelocity = dashDir * crazyDashSpeed;
            elapsed += Time.fixedDeltaTime;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, dashDir, 1f, LayerMask.GetMask("Default", "Wall", "Obstacle"));
            if (hit.collider != null && !hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
            {
                hitObstacle = true;
                break; 
            }
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;

        if (hitObstacle && !isDead)
        {
            AudioManager.Instance?.PlaySFX("boss_slam"); 
            DrawDebugCircle(transform.position, 2.5f, Color.red, 1f); 

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2.5f);
            foreach (var h in hits)
            {
                if (h.CompareTag("Player") && h.TryGetComponent<PlayerController>(out var player))
                {
                    player.TakeDamage(crazyDashImpactDamage);
                    player.ApplyKnockback((player.transform.position - transform.position).normalized, 8f);
                }
            }
            ApplyStun(crazyDashSelfStunDuration);
        }
    }

    private IEnumerator TrackingShootRoutine()
    {
        if (isDead || isStunned || playerTransform == null || projectilePrefab == null) yield break;

        for (int i = 0; i < trackingShootCount; i++)
        {
            if (isDead || isStunned) break;

            Vector2 aimDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            float warningTime = trackingShootDelay * 0.5f;
            SpawnLineIndicator(transform.position, aimDir, projectileRange, lineIndicatorWidth, projectileTelegraphColor, warningTime);
            yield return StartCoroutine(TelegraphFlash(projectileTelegraphColor, warningTime, 0.05f));

            if (isDead || isStunned) break;

            AudioManager.Instance?.PlaySFX("boss_shoot");
            GameObject projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            if (projObj.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.Init(aimDir, projectileSpeed, projectileDamage, projectileRange, projectileKnockbackForce);
            }
            yield return new WaitForSeconds(trackingShootDelay - warningTime);
        }
    }

    private IEnumerator TelegraphFlash(Color telegraphColor, float duration, float blinkInterval)
    {
        float elapsed = 0f;
        bool toggleColor = false;
        while (elapsed < duration)
        {
            if (!isStunned && !isFlashing && spriteRenderer != null)
            {
                spriteRenderer.color = toggleColor ? telegraphColor : originalColor;
            }
            toggleColor = !toggleColor;
            float waitTime = Mathf.Min(blinkInterval, duration - elapsed);
            yield return new WaitForSeconds(waitTime);
            elapsed += waitTime;
        }
        if (!isStunned && !isFlashing && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
    #endregion

    #region Indicators & Gizmos
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, bossUIRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, skillRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, dashAttackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }

    private void SpawnAreaIndicator(Vector3 position, float radius, Color tint, float duration)
    {
        if (areaIndicatorPrefab == null || radius <= 0f) return;
        GameObject obj = Instantiate(areaIndicatorPrefab, position, Quaternion.identity);
        if (obj.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = tint;
            float nativeWidth = (sr.sprite != null) ? sr.sprite.bounds.size.x : 1f;
            if (nativeWidth > 0.0001f)
            {
                float scale = (radius * 2f) / nativeWidth;
                obj.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
        Destroy(obj, duration);
    }

    private void SpawnLineIndicator(Vector3 origin, Vector2 direction, float length, float width, Color tint, float duration)
    {
        if (lineIndicatorPrefab == null || length <= 0f) return;
        if (direction.sqrMagnitude < 0.0001f) return;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 midPoint = origin + (Vector3)(direction.normalized * (length * 0.5f));
        GameObject obj = Instantiate(lineIndicatorPrefab, midPoint, Quaternion.Euler(0f, 0f, angle));
        if (obj.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = tint;
            Vector2 nativeSize = (sr.sprite != null) ? (Vector2)sr.sprite.bounds.size : Vector2.one;
            float scaleX = (nativeSize.x > 0.0001f) ? length / nativeSize.x : 1f;
            float scaleY = (nativeSize.y > 0.0001f) ? width / nativeSize.y : 1f;
            obj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        Destroy(obj, duration);
    }

    private void DrawDebugCircle(Vector2 center, float radius, Color color, float duration, int segments = 24)
    {
        if (radius <= 0f) return;
        Vector3 prevPoint = center + new Vector2(radius, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = (360f / segments) * i * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Debug.DrawLine(prevPoint, nextPoint, color, duration);
            prevPoint = nextPoint;
        }
    }
    #endregion
}