using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Unit 클래스: 게임 내 유닛의 속성과 행동을 관리합니다.
/// </summary>
public class Unit : MonoBehaviour
{
    #region Fields

    [Header("Basic Stats")]
    public int maxHP = 100;
    public int currentHP;
    public int stress = 0;
    public int maxStress = 100;
    public int maxAP = 10;
    public int currentAP;
    public int maxSP = 100;
    public int currentSP;

    [Header("Core Stats")]
    public int PHY = 5; // 물리력
    public int TEC = 5; // 기술력
    public int INT = 5; // 지능
    public int WIL = 5; // 의지력
    public int CHA = 5; // 매력
    public int LUK = 5; // 운

    [Header("Weapon Settings")]
    public Weapon equippedWeapon;

    [Header("Inventory")]
    public Item[] itemSlots = new Item[4];

    [Header("Skills")]
    public List<Skill> skills = new List<Skill>();
    private Dictionary<Skill, int> skillCooldowns = new Dictionary<Skill, int>();

    [Header("Movement Costs")]
    public int baseAPMoveCost = 2;
    public int baseSPMoveCost = 2;

    [Header("Damage Reactions")]
    [SerializeField]
    private List<DamageReactionEntry> damageReactionsList = new List<DamageReactionEntry>();

    private Dictionary<DamageType, DamageReaction?> damageReactions = new Dictionary<DamageType, DamageReaction?>();

    public Vector3Int CurrentCell { get; private set; }

    private Grid grid;
    private SpriteRenderer spriteRenderer;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        InitializeDamageReactions();
    }

    private void Start()
    {
        InitializeComponents();
        ResetStats();
        EquipInitialWeapon();
    }

    private void OnValidate()
    {
        InitializeDamageReactions();
    }

    #endregion

    #region Initialization Methods

    /// <summary>
    /// 데미지 반응을 초기화합니다.
    /// </summary>
    private void InitializeDamageReactions()
    {
        damageReactions.Clear();

        foreach (var entry in damageReactionsList)
        {
            damageReactions[entry.damageType] = entry.reaction;
        }

        // 누락된 DamageType이 있다면 null로 초기화 (일반 데미지를 의미)
        foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
        {
            if (!damageReactions.ContainsKey(damageType))
            {
                damageReactions[damageType] = null;
            }
        }
    }

    /// <summary>
    /// 컴포넌트를 초기화하고 현재 셀을 설정합니다.
    /// </summary>
    private void InitializeComponents()
    {
        grid = FindObjectOfType<Grid>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        CurrentCell = grid.WorldToCell(transform.position);
    }

    /// <summary>
    /// 초기 무기를 장착합니다.
    /// </summary>
    private void EquipInitialWeapon()
    {
        if (equippedWeapon != null)
        {
            EquipWeapon(equippedWeapon);
        }
    }

    #endregion

    #region Stats Management

    /// <summary>
    /// 유닛의 스탯을 초기화합니다.
    /// </summary>
    public void ResetStats()
    {
        currentHP = maxHP;
        currentAP = maxAP;
        currentSP = maxSP;
    }

    /// <summary>
    /// 특정 스탯의 값을 반환합니다.
    /// </summary>
    public int GetStatValue(string statName)
    {
        switch (statName.ToUpper())
        {
            case "PHY": return PHY;
            case "TEC": return TEC;
            case "INT": return INT;
            case "WIL": return WIL;
            case "CHA": return CHA;
            case "LUK": return LUK;
            default:
                Debug.LogWarning($"Unknown stat: {statName}");
                return 0;
        }
    }

    /// <summary>
    /// 턴 시작 시 스탯을 회복합니다.
    /// </summary>
    public void RecoverStatsOnTurnStart()
    {
        currentAP = maxAP;
        currentSP = Mathf.Min(maxSP, currentSP + 10);
    }

    #endregion

    #region Weapon Management

    /// <summary>
    /// 무기를 장착합니다.
    /// </summary>
    public bool EquipWeapon(Weapon weapon)
    {
        if (weapon.CanEquip(this))
        {
            equippedWeapon = weapon;
            Debug.Log($"{gameObject.name} equipped {weapon.weaponName}");
            return true;
        }
        Debug.Log($"{gameObject.name} cannot equip {weapon.weaponName} due to insufficient stats");
        return false;
    }

    /// <summary>
    /// 현재 장착된 무기를 해제합니다.
    /// </summary>
    public void UnequipWeapon()
    {
        if (equippedWeapon != null)
        {
            Debug.Log($"{gameObject.name} unequipped {equippedWeapon.weaponName}");
            equippedWeapon = null;
        }
    }

    #endregion

    #region Combat Methods

    /// <summary>
    /// 유닛의 공격력을 계산합니다.
    /// </summary>
    public int CalculateAttackPower()
    {
        int basePower = PHY; // 기본 공격력은 PHY 능력치
        if (equippedWeapon != null)
        {
            basePower += equippedWeapon.attackPower;
        }
        return basePower;
    }

    /// <summary>
    /// 유닛이 공격할 수 있는 상태인지 확인합니다.
    /// </summary>
    public bool CanAttack()
    {
        return currentAP >= 4 && currentSP >= 10 && equippedWeapon != null;
    }

    /// <summary>
    /// 대상 유닛을 공격합니다.
    /// </summary>
    public void Attack(Unit target)
    {
        if (!CanAttack()) return;

        int damage = CalculateAttackPower();
        DamageType damageType = equippedWeapon != null ? equippedWeapon.damageType : DamageType.Blunt;

        target.TakeDamage(damage, damageType);

        currentAP -= 4;
        currentSP -= 10;

        int actualDamage = target.CalculateActualDamage(damage, damageType);
        LogAttackResult(target, damageType, actualDamage);
    }

    /// <summary>
    /// 공격 결과를 로그에 기록합니다.
    /// </summary>
    private void LogAttackResult(Unit target, DamageType damageType, int actualDamage)
    {
        string damageResult = GetDamageResultString(target, damageType, actualDamage);
        Debug.Log($"{gameObject.name} attacked {target.gameObject.name} with {damageType} type. Target {damageResult}.");
    }

    /// <summary>
    /// 데미지 결과 문자열을 생성합니다.
    /// </summary>
    private string GetDamageResultString(Unit target, DamageType damageType, int actualDamage)
    {
        if (target.damageReactions.TryGetValue(damageType, out var reaction))
        {
            switch (reaction)
            {
                case DamageReaction.Absorb: return $"healed for {actualDamage}";
                case DamageReaction.Null: return "took no damage";
                default: return $"took {actualDamage} damage ({reaction})";
            }
        }
        return $"took {actualDamage} damage (normal)";
    }

    /// <summary>
    /// 대상 유닛이 공격 범위 내에 있는지 확인합니다.
    /// </summary>
    public bool IsInRange(Unit target)
    {
        if (equippedWeapon == null) return false;

        Vector3Int distance = target.CurrentCell - CurrentCell;
        return Mathf.Abs(distance.x) + Mathf.Abs(distance.y) <= equippedWeapon.range;
    }

    /// <summary>
    /// 데미지를 받습니다.
    /// </summary>
    public void TakeDamage(int amount, DamageType damageType)
    {
        int actualDamage = CalculateActualDamage(amount, damageType);

        if (damageReactions.TryGetValue(damageType, out var reaction) && reaction == DamageReaction.Absorb)
        {
            Heal(actualDamage);
        }
        else if (!damageReactions.TryGetValue(damageType, out reaction) || reaction != DamageReaction.Null)
        {
            currentHP -= actualDamage;
            AddStress(actualDamage / 2);
            if (currentHP <= 0)
            {
                Die();
            }
        }
    }

    /// <summary>
    /// 실제 받는 데미지를 계산합니다.
    /// </summary>
    public int CalculateActualDamage(int baseDamage, DamageType damageType)
    {
        if (!damageReactions.TryGetValue(damageType, out var reaction) || reaction == null)
        {
            return baseDamage; // 일반 데미지
        }

        switch (reaction.Value)
        {
            case DamageReaction.Weak: return baseDamage * 2;
            case DamageReaction.Resistant: return baseDamage / 2;
            case DamageReaction.Null:
            case DamageReaction.Absorb: return 0;
            default: return baseDamage;
        }
    }

    /// <summary>
    /// 체력을 회복합니다.
    /// </summary>
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
    }

    /// <summary>
    /// 스트레스를 추가합니다.
    /// </summary>
    public void AddStress(int amount)
    {
        stress += amount;
        if (stress >= maxStress)
        {
            MentalBreakdown();
        }
    }

    /// <summary>
    /// 유닛이 사망했을 때 호출됩니다.
    /// </summary>
    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        // 여기에 사망 처리 로직 추가
    }

    /// <summary>
    /// 정신적 붕괴가 일어났을 때 호출됩니다.
    /// </summary>
    private void MentalBreakdown()
    {
        Debug.Log($"{gameObject.name} has suffered a mental breakdown.");
        // 여기에 정신 붕괴 처리 로직 추가
    }

    #endregion

    #region Movement and Pathfinding

    /// <summary>
    /// 주어진 경로를 따라 이동합니다.
    /// </summary>
    public IEnumerator FollowPath(List<Vector3Int> path)
    {
        for (int i = 1; i < path.Count; i++)
        {
            if (currentAP < baseAPMoveCost || currentSP < baseSPMoveCost)
                break;

            Vector3 targetPosition = grid.CellToWorld(path[i]) + new Vector3(0.5f, 0.5f, 0);
            yield return MoveToPosition(targetPosition);

            UpdatePositionAndResources(path[i]);
        }
    }

    /// <summary>
    /// 목표 위치로 이동합니다.
    /// </summary>
    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, 5f * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>
    /// 위치와 자원을 업데이트합니다.
    /// </summary>
    private void UpdatePositionAndResources(Vector3Int newCell)
    {
        CurrentCell = newCell;
        //SRPGManager.Instance.RemoveHighlight(newCell);
        currentAP -= baseAPMoveCost;
        currentSP -= baseSPMoveCost;
    }

    /// <summary>
    /// 남은 이동력을 계산합니다.
    /// </summary>
    public int GetRemainingMovement()
    {
        int maxMovesAP = currentAP / baseAPMoveCost;
        int maxMovesSP = currentSP / baseSPMoveCost;
        return Mathf.Min(maxMovesAP, maxMovesSP);
    }

    #endregion

    #region Visual Effects

    /// <summary>
    /// 유닛을 하이라이트합니다.
    /// </summary>
    public void Highlight(bool active)
    {
        spriteRenderer.color = active ? Color.yellow : Color.white;
    }

    #endregion

    #region Damage Reaction Management

    [Serializable]
    public class DamageReactionEntry
    {
        public DamageType damageType;
        public DamageReaction reaction;  // nullable 제거
    }

    /// <summary>
    /// 특정 데미지 타입에 대한 반응을 설정합니다.
    /// </summary>
    public void SetDamageReaction(DamageType damageType, DamageReaction reaction)
    {
        damageReactions[damageType] = reaction;

        // 리스트도 업데이트
        var entry = damageReactionsList.Find(e => e.damageType == damageType);
        if (entry != null)
        {
            entry.reaction = reaction;
        }
        else
        {
            damageReactionsList.Add(new DamageReactionEntry { damageType = damageType, reaction = reaction });
        }
    }

    #endregion

    public bool CanUseSkill(Skill skill)
    {
        bool hasEnoughAP = currentAP >= skill.apCost;
        bool hasEnoughSP = currentSP >= skill.spCost;
        bool isNotOnCooldown = !skillCooldowns.ContainsKey(skill) || skillCooldowns[skill] == 0;

        if (!hasEnoughAP)
            Debug.Log($"Not enough AP to use {skill.skillName}. Current: {currentAP}, Required: {skill.apCost}");
        if (!hasEnoughSP)
            Debug.Log($"Not enough SP to use {skill.skillName}. Current: {currentSP}, Required: {skill.spCost}");
        if (!isNotOnCooldown)
            Debug.Log($"{skill.skillName} is on cooldown. Remaining: {skillCooldowns[skill]}");

        return hasEnoughAP && hasEnoughSP && isNotOnCooldown;
    }

    public void UseSkill(Skill skill, Unit target)
    {
        if (!CanUseSkill(skill))
        {
            Debug.Log($"Cannot use skill {skill.skillName}.");
            return;
        }

        skill.UseSkill(this, target);

        currentAP -= skill.apCost;
        currentSP -= skill.spCost;

        SetSkillCooldown(skill);  // 새로 추가된 메서드 사용

        Debug.Log($"{gameObject.name} used {skill.skillName} on {target.gameObject.name}. Remaining AP: {currentAP}, SP: {currentSP}");
    }

    public void UpdateCooldowns()
    {
        var keys = new List<Skill>(skillCooldowns.Keys);
        foreach (var skill in keys)
        {
            if (skillCooldowns[skill] > 0)
            {
                skillCooldowns[skill]--;
            }
        }
    }

    public bool HasSkill(Skill skill)
    {
        return skills.Contains(skill);
    }

    public bool AddItem(Item item)
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] == null)
            {
                itemSlots[i] = item;
                Debug.Log($"{gameObject.name} added {item.itemName} to inventory");
                return true;
            }
        }
        Debug.Log($"{gameObject.name}'s inventory is full");
        return false;
    }

    public void UseItem(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < itemSlots.Length && itemSlots[slotIndex] != null)
        {
            Item item = itemSlots[slotIndex];
            item.Use(this);
            if (item is ConsumableItem)
            {
                itemSlots[slotIndex] = null;
                Debug.Log($"{gameObject.name} consumed {item.itemName}");
            }
        }
        else
        {
            Debug.Log($"No item in slot {slotIndex}");
        }
    }

    public void RestoreSP(int amount)
    {
        currentSP = Mathf.Min(currentSP + amount, maxSP);
        Debug.Log($"{gameObject.name} restored {amount} SP. Current SP: {currentSP}");
    }

    /// <summary>
    /// 스킬의 쿨다운을 설정합니다.
    /// </summary>
    /// <param name="skill">쿨다운을 설정할 스킬</param>
    public void SetSkillCooldown(Skill skill)
    {
        if (skill == null)
        {
            Debug.LogError("Attempted to set cooldown for a null skill.");
            return;
        }

        if (!skills.Contains(skill))
        {
            Debug.LogWarning($"Attempted to set cooldown for skill {skill.skillName} which is not in the unit's skill list.");
            return;
        }

        if (skillCooldowns.ContainsKey(skill))
        {
            skillCooldowns[skill] = skill.cooldown;
        }
        else
        {
            skillCooldowns.Add(skill, skill.cooldown);
        }

        Debug.Log($"Set cooldown for skill {skill.skillName} to {skill.cooldown} turns.");
    }
}