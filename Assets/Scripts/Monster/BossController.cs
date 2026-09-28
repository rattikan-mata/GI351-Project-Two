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
/// ใช้สคริปต์นี้ตัวเดียวกันได้ทั้งบอส "ตานี" และ "ปลอบ" แค่ทำ 2 Prefab แยกกัน
/// แล้วตั้งค่า(น้ำหนัก/ดาเมจ/สีเตือน/ความแรง ฯลฯ)ในแต่ละ Prefab ให้ไม่เหมือนกัน จะได้บอส 2 ตัวที่เล่นต่างกัน
/// </summary>
public class BossController : Monster
{
    private enum BossSkill { Dash, ProjectileBarrage, GroundSlam }

    #region Boss Info
    [Header("Boss Info")]
    [Tooltip("ชื่อบอส (ใช้โชว์ใน Debug Log เฉยๆ ตอนนี้ ต่อยอดทำ UI ชื่อบอส/Health Bar ทีหลังได้)")]
    [SerializeField] private string bossName = "Boss";
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

    #region Area Indicator (สปอนวัตถุวงกลมโชว์ขนาดจริงตอน Dash/Slam เปลี่ยน Sprite เองได้)
    [Header("Area Indicator (โชว์ขนาดจริงตอน Dash/Slam)")]
    [Tooltip("ลาก Prefab ที่มี SpriteRenderer (เช่น Sprite วงกลมโปร่งใส) มาใส่ จะสปอนขึ้นมาแล้วปรับขนาดให้พอดีรัศมีจริงอัตโนมัติ ไม่ว่า Sprite จะเป็นรูปอะไร/ขนาดกี่พิกเซลก็ตาม ถ้าไม่ใส่จะไม่สปอน (ยังเห็นเส้น Gizmo ปกติ)")]
    [SerializeField] private GameObject areaIndicatorPrefab;

    [Tooltip("สีที่ย้อมทับ Sprite ตอนโชว์วงของท่า Dash (ใส่ Alpha ต่ำๆ จะได้โปร่งแสง)")]
    [SerializeField] private Color dashIndicatorColor = new Color(1f, 0.5f, 0f, 0.5f);

    [Tooltip("สีที่ย้อมทับ Sprite ตอนโชว์วงของท่า Ground Slam")]
    [SerializeField] private Color slamIndicatorColor = new Color(1f, 0f, 0f, 0.5f);
    #endregion

    #region Line Indicator (เตือนทิศทางล่วงหน้าท่า Projectile Barrage)
    [Header("Line Indicator (เตือนทิศกระสุนล่วงหน้า)")]
    [Tooltip("ลาก Prefab ที่มี SpriteRenderer เป็นเส้นตรง/สี่เหลี่ยมยาว (Pivot กลาง ค่า Default ตอน import) ใช้โชว์ทิศทางกระสุนแต่ละนัดล่วงหน้าก่อนยิงจริง ถ้าไม่ใส่จะไม่โชว์ (ยังยิงกระสุนได้ปกติ)")]
    [SerializeField] private GameObject lineIndicatorPrefab;

    [Tooltip("ความหนาของเส้นเตือน (หน่วยเกม)")]
    [SerializeField] private float lineIndicatorWidth = 0.3f;
    #endregion

    #region 1) Dash Charge
    [Header("1) Dash Charge (ใช้ค่าจาก Header \"Dash Attack\" ของ Monster ด้านบนทั้งหมด)")]
    [SerializeField] private bool dashSkillEnabled = true;
    [Tooltip("น้ำหนักสุ่มของท่านี้ เทียบกับท่าอื่น (ท่านี้จะถูกเลือกได้ก็ต่อเมื่อผู้เล่นอยู่ในระยะ Dash Attack Range ด้วย)")]
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
    [Tooltip("ระยะเวลาพุ่งเข้าหาก่อนฟาด (วินาที) ยิ่งนานยิ่งไล่ผู้เล่นได้ไกลก่อนฟาด")]
    [SerializeField] private float slamChargeDuration = 0.4f;

    [SerializeField] private float slamRadius = 2.5f;
    [SerializeField] private int slamDamage = 12;
    [SerializeField] private float slamKnockbackForce = 8f;
    [Tooltip("เวลาสตันผู้เล่นหลังโดนฟาด (0 = ไม่สตัน) — ใช้ร่วมกับ PlayerController.TakeDamage/ApplyKnockback ปกติ")]
    [SerializeField] private float slamStunDuration = 0f;

    [Tooltip("สีเตือนก่อนฟาดพื้น")]
    [SerializeField] private Color slamTelegraphColor = Color.red;
    [SerializeField] private float slamTelegraphDuration = 0.7f;
    [SerializeField] private float slamBlinkInterval = 0.1f;

    [Tooltip("(ไม่บังคับ) เอฟเฟกต์ตอนฟาดพื้น")]
    [SerializeField] private GameObject slamEffectPrefab;
    [SerializeField] private float slamEffectLifetime = 1f;
    #endregion

    // ทับ FixedUpdate ของ Monster ทั้งหมด: แทนที่ "ไล่แล้วพุ่งชนอย่างเดียว" ด้วยระบบเลือกสกิล 3 ท่า
    protected override void FixedUpdate()
    {
        if (isDead || playerTransform == null) return;

        if (isKnockedBack)
        {
            return; // ให้ KnockbackRoutine คุมความเร็วเอง เหมือน Monster เดิม
        }

        if (isUsingSkill)
        {
            return; // กำลังเตือน/ออกท่าอยู่ -> ให้ coroutine ของท่านั้นคุมความเร็วเอง
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

        // ยังไกลเกินจะออกท่า หรือคูลดาวน์ยังไม่หมด -> ไล่ตามแบบมอนปกติ (ใช้ของ Monster เดิม)
        ChasePlayerIfInRange();
    }

    // สุ่มเลือก 1 ท่าจากท่าที่ "เปิดใช้งาน" และ "อยู่ในเงื่อนไข" ตอนนี้ ตามน้ำหนัก (Weighted Random)
    // ท่า Dash ต้องผู้เล่นอยู่ในระยะ Dash Attack Range (ค่าจาก Monster เดิม) ด้วย ไม่งั้นพุ่งไม่ถึงตัว
    private BossSkill? ChooseSkill(float distanceToPlayer)
    {
        bool dashAvailable = dashSkillEnabled && distanceToPlayer <= dashAttackRange;
        bool projectileAvailable = projectileSkillEnabled && projectilePrefab != null;
        bool slamAvailable = slamSkillEnabled;

        float totalWeight = 0f;
        if (dashAvailable) totalWeight += Mathf.Max(0f, dashWeight);
        if (projectileAvailable) totalWeight += Mathf.Max(0f, projectileWeight);
        if (slamAvailable) totalWeight += Mathf.Max(0f, slamWeight);

        if (totalWeight <= 0f) return null; // ไม่มีท่าไหนใช้ได้ตอนนี้เลย (เช่น ปิดหมด/ยังไม่ได้ใส่ Prefab กระสุน)

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

        // Debug: ดูว่าบอสตัวไหน (ชื่อ) เลือกออกท่าอะไร ช่วยจับกรณีน้ำหนัก/เงื่อนไขตั้งผิดจนออกท่าเดิมซ้ำๆ
        Debug.Log($"[Boss] {bossName} เตรียมออกท่า: {skill}");

        switch (skill)
        {
            case BossSkill.Dash:
                // โชว์วงกลมขนาด Dash Attack Range (ระยะที่บอสจะพุ่งชน) ให้เห็นตลอดช่วงเตือน+พุ่ง+พักฟื้นคร่าวๆ
                SpawnAreaIndicator(transform.position, dashAttackRange, dashIndicatorColor, warningDuration + dashDuration + 0.3f);

                // ใช้ระบบพุ่งชนเดิมของ Monster ทั้งชุด (เตือนสีกระพริบ + พุ่ง + พักฟื้น) ไม่ต้องเขียนใหม่
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

    // ท่า 2: ล็อกทิศยิงทุกนัดไว้ก่อน -> โชว์เส้นเตือนล่วงหน้าตามทิศจริง (ระหว่างกระพริบสีเตือน) -> ยิงจริงตามทิศที่ล็อกไว้
    // (เหมือน Dungeon Quest: เห็นแนวเส้นก่อนโดนจริง ไม่ใช่โดนแบบไม่ทันตั้งตัว)
    private IEnumerator ProjectileBarrageRoutine()
    {
        if (isDead || isStunned || playerTransform == null) yield break;

        Vector2 baseDir = (Vector2)playerTransform.position - (Vector2)transform.position;
        baseDir = baseDir.sqrMagnitude > 0.0001f ? baseDir.normalized : Vector2.down;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        int count = Mathf.Max(1, projectileCount);
        float halfArc = projectileArcAngle * 0.5f;

        // ล็อกทิศทุกนัดไว้ตั้งแต่ตอนนี้ (ก่อนกระพริบเตือน) แล้วโชว์เส้นเตือนตามทิศที่ล็อกไว้เลย
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

        for (int i = 0; i < count; i++)
        {
            GameObject projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            if (projObj.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.Init(pelletDirs[i], projectileSpeed, projectileDamage, projectileRange, projectileKnockbackForce);
            }
        }
    }

    // ท่า 3: ล็อกจุดที่จะฟาดไว้ก่อน (ตำแหน่งผู้เล่นตอนเริ่มท่า) -> โชว์วงเตือนที่จุดนั้นทันทีค้างไว้ตลอดช่วงเตือน+พุ่งเข้า
    // -> พุ่งเข้าใกล้จุดที่ล็อกไว้ -> ฟาดตรงจุดเดิมเป๊ะ (ไม่ใช่ตำแหน่งผู้เล่น ณ ตอนนั้น) ผู้เล่นเลยหลบได้ด้วยการออกนอกวงก่อนมันฟาด
    // (เหมือน Dungeon Quest: เห็นวงแดงตั้งแต่ก่อนพุ่งเข้ามา ไม่ใช่โผล่มาตอนฟาดแล้ว)
    private IEnumerator GroundSlamRoutine()
    {
        if (isDead || isStunned || playerTransform == null) yield break;

        Vector2 targetPos = playerTransform.position;

        // โชว์วงเตือนที่จุดเป้าหมายทันที ค้างไว้ตลอดช่วงเตือน+พุ่งเข้า (เห็นล่วงหน้าเต็มๆ)
        float totalTelegraphTime = slamTelegraphDuration + slamChargeDuration;
        SpawnAreaIndicator(targetPos, slamRadius, slamIndicatorColor, totalTelegraphTime);

        yield return StartCoroutine(TelegraphFlash(slamTelegraphColor, slamTelegraphDuration, slamBlinkInterval));

        if (isDead || isStunned) yield break;

        // พุ่งเข้าหาจุดที่ล็อกไว้ (ไม่ใช่ตำแหน่งผู้เล่น ณ ตอนนี้ เพื่อให้ตรงกับวงเตือนที่โชว์ไว้พอดี)
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

        if (slamEffectPrefab != null)
        {
            GameObject fx = Instantiate(slamEffectPrefab, targetPos, Quaternion.identity);
            Destroy(fx, slamEffectLifetime);
        }

        // Debug: เห็นวงกลม AoE จริงตอนฟาดพื้นใน Scene View (โชว์ 0.75 วิ) เอาไว้เช็คว่าตรงกับวงเตือนที่โชว์ไว้ก่อนหน้าไหม
        DrawDebugCircle(targetPos, slamRadius, Color.red, 0.75f);

        // เช็คดาเมจที่ "จุดที่ล็อกไว้" (ไม่ใช่ตำแหน่งบอสตอนนี้) ผู้เล่นที่ขยับออกนอกวงทันเวลาจะไม่โดน
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, slamRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject.CompareTag("Player") && hit.TryGetComponent<PlayerController>(out var player))
            {
                player.TakeDamage(slamDamage);

                Vector2 dir = ((Vector2)player.transform.position - targetPos).normalized;
                player.ApplyKnockback(dir, slamKnockbackForce);

                break; // มีผู้เล่นแค่คนเดียวในเกม กันกรณีผู้เล่นมีหลาย Collider โดนดาเมจซ้อน
            }
        }
    }

    // กระพริบสีเตือนก่อนออกท่า (ใช้ร่วมกันได้ทุกท่า) เคารพระบบ Hit Flash/Stun เดิมของ Monster
    // เช่น ถ้าบอสโดนตีหรือโดนสตันระหว่างเตือน จะไม่แย่งสีกัน (เหมือนที่ DashAttackRoutine เดิมทำ)
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

    #region Debug Gizmos (เช็ค Hitbox แต่ละท่า)
    // วาดตลอดเวลาไม่ต้องคลิกเลือกบอสก่อน (เดิมเป็น OnDrawGizmosSelected เปลี่ยนเป็น OnDrawGizmos เพื่อให้เห็นสดๆ ตอนกด Play เลย)
    // สีส้ม = ระยะพุ่งชน (Dash Attack Range, ค่าจาก Monster เดิม), ฟ้า = ระยะเริ่มออกท่า (Skill Range)
    // แดง = รัศมี Ground Slam (วาดรอบตำแหน่งบอสตอนนี้ ของจริงจะขยับเข้าหาผู้เล่นก่อนฟาด)
    // ม่วง = มุมกระจาย Projectile Barrage (ตอน Play จะชี้ไปทางผู้เล่นจริง ตอน Edit จะชี้ลงเป็นค่าเริ่มต้น)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, skillRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, dashAttackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, slamRadius);

        Gizmos.color = Color.magenta;
        Vector2 previewDir = (Application.isPlaying && playerTransform != null)
            ? ((Vector2)playerTransform.position - (Vector2)transform.position).normalized
            : Vector2.down;
        DrawArcGizmo(transform.position, previewDir, projectileArcAngle, projectileRange);
    }

    private void DrawArcGizmo(Vector3 origin, Vector2 dir, float arcAngle, float range)
    {
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float halfArc = arcAngle * 0.5f;

        Vector3 leftEdge = origin + Quaternion.Euler(0f, 0f, baseAngle - halfArc) * Vector3.right * range;
        Vector3 rightEdge = origin + Quaternion.Euler(0f, 0f, baseAngle + halfArc) * Vector3.right * range;

        Gizmos.DrawLine(origin, leftEdge);
        Gizmos.DrawLine(origin, rightEdge);

        int segments = 12;
        Vector3 prevPoint = leftEdge;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = baseAngle - halfArc + (arcAngle * t);
            Vector3 point = origin + Quaternion.Euler(0f, 0f, angle) * Vector3.right * range;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }

    // สร้างวัตถุวงกลมขนาดเท่ารัศมีจริงมาโชว์ชั่วคราว ปรับสเกลอัตโนมัติตามขนาด Sprite ต้นฉบับ (bounds.size.x ตอน scale 1)
    // เพราะงั้นเปลี่ยน Sprite ใน Prefab เป็นรูปอะไรก็ได้ ขนาดจะยังตรงกับรัศมีจริงเสมอ ไม่ต้องคำนวณสเกลเอง
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
                float scale = (radius * 2f) / nativeWidth; // รัศมี x 2 = เส้นผ่านศูนย์กลางที่ต้องการ
                obj.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
        else
        {
            Debug.LogWarning("[Boss] Area Indicator Prefab ไม่มี SpriteRenderer -> ปรับขนาดอัตโนมัติไม่ได้ ยังสปอนได้แต่ขนาดจะเป็นค่าดีฟอลต์ของ Prefab", obj);
        }

        Destroy(obj, duration);
    }

    // สร้างเส้นเตือนล่วงหน้า (ยาว = ระยะยิงจริง) ชี้ตามทิศที่จะยิงจริง ปรับสเกลอัตโนมัติตามขนาด Sprite ต้นฉบับเหมือน SpawnAreaIndicator
    // ต้องใช้ Sprite Pivot กลาง (ค่า Default ตอน import Unity) ถึงจะยืดออกจากจุดกำเนิดไปตามทิศได้พอดี
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
        else
        {
            Debug.LogWarning("[Boss] Line Indicator Prefab ไม่มี SpriteRenderer -> ปรับขนาดอัตโนมัติไม่ได้", obj);
        }

        Destroy(obj, duration);
    }

    // วาดวงกลมด้วย Debug.DrawLine ให้เห็นตอน Play จริง (ต่างจาก Gizmos ที่เห็นเฉพาะตอนเลือกออบเจกต์)
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