using System.Collections;
using UnityEngine;

/// <summary>
/// บอส: สืบทอดจาก Monster.cs ทั้งหมด (ใช้ระบบ HP / Stun / Knockback / ไล่ผู้เล่น / Hit Flash / Item Drop เดิม)
/// เพิ่มระบบสกิล 3 ท่า สุ่มสลับกันตามน้ำหนัก (Weight) แต่ละท่ามี "สีเตือน" (Telegraph) ไม่เหมือนกัน
/// เพื่อให้ผู้เล่นแยกออกก่อนว่าบอสกำลังจะออกท่าไหน แล้วหลบให้ถูก
///
/// ท่า 1) Dash Charge   -> ใช้ระบบพุ่งชนเดิมของ Monster ทั้งชุด (สีเตือน/ความเร็ว/ระยะ ปรับที่ header "Dash Attack" ของ Monster เดิม)
/// ท่า 2) Projectile Barrage -> ยิงกระสุนกระจายเป็นมุมใส่ทิศผู้เล่น ณ ตอนยิง
/// ท่า 3) Ground Slam   -> พุ่งเข้าใกล้ผู้เล่นสั้นๆ แล้วฟาดพื้น ทำดาเมจ + ผลักกระเด็นรอบตัวเป็นวงกลม
///
/// ใช้สคริปต์นี้ตัวเดียวกันได้ทั้งบอสทั้ง 3 ตัว แค่ทำ Prefab แยกกัน
/// แล้วตั้งค่า(น้ำหนัก/ดาเมจ/สีเตือน/ความแรง ฯลฯ)ในแต่ละ Prefab ให้ไม่เหมือนกัน จะได้บอสที่เล่นต่างกัน
/// </summary>
public class BossController : Monster
{
    private enum BossSkill { Dash, ProjectileBarrage, GroundSlam }

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

    #region Area Indicator (โชว์ขนาดจริงตอน Dash/Slam)
    [Header("Area Indicator (โชว์ขนาดจริงตอน Dash/Slam)")]
    [Tooltip("ลาก Prefab ที่มี SpriteRenderer มาใส่ ถ้าไม่ใส่จะไม่สปอน")]
    [SerializeField] private GameObject areaIndicatorPrefab;

    [Tooltip("สีที่ย้อมทับ Sprite ตอนโชว์วงของท่า Dash")]
    [SerializeField] private Color dashIndicatorColor = new Color(1f, 0.5f, 0f, 0.5f);

    [Tooltip("สีที่ย้อมทับ Sprite ตอนโชว์วงของท่า Ground Slam")]
    [SerializeField] private Color slamIndicatorColor = new Color(1f, 0f, 0f, 0.5f);
    #endregion

    #region Line Indicator (เตือนทิศทางล่วงหน้าท่า Projectile Barrage)
    [Header("Line Indicator (เตือนทิศกระสุนล่วงหน้า)")]
    [Tooltip("ลาก Prefab ที่มี SpriteRenderer เป็นเส้นตรง/สี่เหลี่ยมยาว")]
    [SerializeField] private GameObject lineIndicatorPrefab;

    [Tooltip("ความหนาของเส้นเตือน (หน่วยเกม)")]
    [SerializeField] private float lineIndicatorWidth = 0.3f;
    #endregion

    #region 1) Dash Charge
    [Header("1) Dash Charge (ใช้ค่าจาก Header \"Dash Attack\" ของ Monster ด้านบนทั้งหมด)")]
    [SerializeField] private bool dashSkillEnabled = true;
    [Tooltip("น้ำหนักสุ่มของท่านี้ เทียบกับท่าอื่น")]
    [SerializeField] private float dashWeight = 1f;
    #endregion

    #region 2) Projectile Barrage
    [Header("2) Projectile Barrage")]
    [SerializeField] private bool projectileSkillEnabled = true;
    [SerializeField] private float projectileWeight = 1f;

    [Tooltip("Prefab กระสุนที่ใช้ยิง (ต้องมี Projectile.cs)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int projectileCount = 6;
    [Tooltip("มุมกระจายทั้งหมดของชุดกระสุน (องศา)")]
    [SerializeField] private float projectileArcAngle = 60f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private int projectileDamage = 8;
    [SerializeField] private float projectileRange = 10f;
    [Tooltip("แรงผลักผู้เล่นกระเด็นตอนโดนกระสุน (0 = ไม่ผลัก)")]
    [SerializeField] private float projectileKnockbackForce = 4f;

    [Tooltip("สีเตือนก่อนยิงกระสุน")]
    [SerializeField] private Color projectileTelegraphColor = Color.magenta;
    [SerializeField] private float projectileTelegraphDuration = 0.6f;
    [SerializeField] private float projectileBlinkInterval = 0.1f;
    #endregion

    #region 3) Ground Slam
    [Header("3) Ground Slam")]
    [SerializeField] private bool slamSkillEnabled = true;
    [SerializeField] private float slamWeight = 1f;

    [Tooltip("ความเร็วพุ่งเข้าหาผู้เล่นก่อนฟาดพื้น")]
    [SerializeField] private float slamChargeSpeed = 9f;
    [Tooltip("ระยะเวลาพุ่งเข้าหาก่อนฟาด (วินาที)")]
    [SerializeField] private float slamChargeDuration = 0.4f;

    [SerializeField] private float slamRadius = 2.5f;
    [SerializeField] private int slamDamage = 12;
    [SerializeField] private float slamKnockbackForce = 8f;

    [Tooltip("สีเตือนก่อนฟาดพื้น")]
    [SerializeField] private Color slamTelegraphColor = Color.red;
    [SerializeField] private float slamTelegraphDuration = 0.7f;
    [SerializeField] private float slamBlinkInterval = 0.1f;

    [Tooltip("(ไม่บังคับ) เอฟเฟกต์ตอนฟาดพื้น")]
    [SerializeField] private GameObject slamEffectPrefab;
    [SerializeField] private float slamEffectLifetime = 1f;
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

                // แสดงหลอดเลือดบอสบน Canvas กลางจอ
                UIBossHealthBar.Instance?.ShowBossBar(this, bossName);

                // สั่นหน้าจอแบบนุ่มนวลเฉพาะครั้งแรกที่พบ
                if (!hasTriggeredShake)
                {
                    hasTriggeredShake = true;
                    UIManager.Instance?.TriggerScreenShake(encounterShakeDuration, encounterShakeMagnitude);
                }
            }
        }
        else
        {
            // ออกนอกรัศมี ให้ซ่อนแถบเลือด (ไม่รีเซ็ตเลือด)
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

        if (isKnockedBack)
        {
            return;
        }

        if (isUsingSkill)
        {
            return;
        }

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
        base.TakeDamage(amount); // เรียกคำนวณดาเมจ, Hit Flash และตัวเลขดาเมจลอย

        // อัปเดตหลอดเลือดบอสบน Canvas ทันทีที่โดนโจมตี
        if (UIBossHealthBar.Instance != null)
        {
            UIBossHealthBar.Instance.OnBossTakeDamage(this);
        }
    }

    public override void Die()
    {
        if (isDead) return;

        if (exitPortal != null)
        {
            exitPortal.SetActive(true);
        }

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

        float totalWeight = 0f;
        if (dashAvailable) totalWeight += Mathf.Max(0f, dashWeight);
        if (projectileAvailable) totalWeight += Mathf.Max(0f, projectileWeight);
        if (slamAvailable) totalWeight += Mathf.Max(0f, slamWeight);

        if (totalWeight <= 0f) return null;

        float roll = Random.value * totalWeight;
        float cumulative = 0f;

        if (dashAvailable)
        {
            cumulative += Mathf.Max(0f, dashWeight);
            if (roll <= cumulative) return BossSkill.Dash;
        }
        if (projectileAvailable)
        {
            cumulative += Mathf.Max(0f, projectileWeight);
            if (roll <= cumulative) return BossSkill.ProjectileBarrage;
        }
        if (slamAvailable)
        {
            cumulative += Mathf.Max(0f, slamWeight);
            if (roll <= cumulative) return BossSkill.GroundSlam;
        }

        return null;
    }

    private IEnumerator UseSkillRoutine(BossSkill skill)
    {
        isUsingSkill = true;
        rb.linearVelocity = Vector2.zero;

        Debug.Log($"[Boss] {bossName} เตรียมออกท่า: {skill}");

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