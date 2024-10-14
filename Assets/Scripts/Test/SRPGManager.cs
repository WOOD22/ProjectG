using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using TMPro; // TMP를 사용하기 위해 추가

/// <summary>
/// SRPG 게임의 전체 흐름을 관리합니다.
/// </summary>
public class SRPGManager : MonoBehaviour
{
    #region 싱글톤

    public static SRPGManager Instance;

    private void Awake()
    {
        // 싱글톤 인스턴스 설정
        Instance = this;
    }

    #endregion

    #region 열거형

    /// <summary>
    /// 턴의 상태를 나타냅니다.
    /// </summary>
    public enum TurnState { PlayerTurn, EnemyTurn }

    #endregion

    #region 필드

    [Header("턴 관리")]
    public TurnState currentTurn = TurnState.PlayerTurn;

    [Header("유닛")]
    public List<Unit> playerUnits = new List<Unit>();
    public List<Unit> enemyUnits = new List<Unit>();

    [Header("UI 요소")]
    public Button endTurnButton;
    public Button attackButton;

    [Header("Skill UI")]
    public ScrollRect skillScrollView;
    public Transform skillButtonContainer;
    public GameObject skillButtonPrefab;
    public Button toggleSkillWindowButton;

    private List<Toggle> skillToggles = new List<Toggle>();
    private bool isSkillScrollViewActive = false;

    [Header("타일맵")]
    public Tilemap highlightTilemap;
    public TileBase highlightTile;

    [Header("레이어")]
    public LayerMask wallLayer;

    private Unit selectedUnit;
    private Skill selectedSkill;
    private Grid grid;
    private AStar pathfinder;

    private Color greenHighlight = new Color(0, 1, 0, 0.5f);
    private Color orangeHighlight = new Color(1, 0.5f, 0, 0.5f);

    private Dictionary<Vector3Int, Color> highlightedTiles = new Dictionary<Vector3Int, Color>();

    private List<Vector3Int> currentPath;

    private bool isPathDisplayed = false;
    private bool isAttackMode = false;
    private bool isSkillMode = false;

    #endregion

    #region Unity 라이프사이클 메서드

    private void Start()
    {
        InitializeComponents();
        SetupTurn(currentTurn);
        SetupSkillScrollView();
    }

    /// <summary>
    /// 스킬 스크롤 뷰를 설정합니다.
    /// </summary>
    private void SetupSkillScrollView()
    {
        toggleSkillWindowButton.onClick.AddListener(ToggleSkillScrollView);
        skillScrollView.gameObject.SetActive(false);
    }

    /// <summary>
    /// 스킬 스크롤 뷰의 활성화 상태를 토글합니다.
    /// </summary>
    private void ToggleSkillScrollView()
    {
        isSkillScrollViewActive = !isSkillScrollViewActive;
        skillScrollView.gameObject.SetActive(isSkillScrollViewActive);

        if (isSkillScrollViewActive && selectedUnit != null)
        {
            UpdateSkillUI();
        }
    }

    /// <summary>
    /// 스킬 스크롤 뷰를 비활성화합니다.
    /// </summary>
    private void HideSkillScrollView()
    {
        if (isSkillScrollViewActive)
        {
            isSkillScrollViewActive = false;
            skillScrollView.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (currentTurn == TurnState.PlayerTurn)
        {
            HandlePlayerInput();
        }
    }

    #endregion

    #region 초기화

    /// <summary>
    /// 필요한 컴포넌트를 초기화합니다.
    /// </summary>
    private void InitializeComponents()
    {
        grid = FindObjectOfType<Grid>();
        pathfinder = GetComponent<AStar>();

        endTurnButton.onClick.AddListener(EndPlayerTurn);
        attackButton.onClick.AddListener(ToggleAttackMode);
    }

    #endregion

    #region 입력 처리

    /// <summary>
    /// 플레이어 입력을 처리합니다.
    /// </summary>
    private void HandlePlayerInput()
    {
        // 좌클릭 입력 및 UI 요소 위에서의 클릭이 아닐 경우 처리
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPosition = grid.WorldToCell(mousePosition);

            if (isSkillMode)
            {
                HandleSkillUse(cellPosition);
            }
            else if (isAttackMode)
            {
                HandleAttack(cellPosition);
            }
            else
            {
                HandleMovementInput(cellPosition);
            }
        }

        // 우클릭으로 현재 모드 취소
        if (Input.GetMouseButtonDown(1))
        {
            CancelCurrentMode();
        }
    }

    /// <summary>
    /// 현재 모드를 취소합니다.
    /// </summary>
    private void CancelCurrentMode()
    {
        isSkillMode = false;
        isAttackMode = false;
        selectedSkill = null;
        ClearHighlights();

        if (selectedUnit != null)
        {
            selectedUnit.Highlight(true);
        }

        Debug.Log("현재 모드가 취소되었습니다.");

        // 모드 취소 시 스킬창 비활성화
        HideSkillScrollView();
    }

    /// <summary>
    /// 이동 관련 입력을 처리합니다.
    /// </summary>
    /// <param name="cellPosition">클릭한 셀의 위치</param>
    private void HandleMovementInput(Vector3Int cellPosition)
    {
        Unit clickedUnit = playerUnits.Find(u => u.CurrentCell == cellPosition);

        if (clickedUnit != null)
        {
            SelectUnit(clickedUnit);
            // 새로운 유닛을 선택하면 현재 모드를 리셋합니다.
            CancelCurrentMode();
        }
        else if (selectedUnit != null && !isSkillMode && !isAttackMode)
        {
            if (!isPathDisplayed)
            {
                ShowPathToTile(cellPosition);
                isPathDisplayed = true;
            }
            else if (currentPath != null && currentPath.Contains(cellPosition))
            {
                MoveSelectedUnit(cellPosition);
                isPathDisplayed = false;
            }
            else
            {
                ShowPathToTile(cellPosition);
                isPathDisplayed = true;
            }
        }

        // 움직임 입력 후 스킬창 비활성화
        HideSkillScrollView();
    }

    #endregion

    #region 유닛 선택 및 이동

    /// <summary>
    /// 유닛을 선택합니다.
    /// </summary>
    /// <param name="unit">선택할 유닛</param>
    private void SelectUnit(Unit unit)
    {
        if (selectedUnit != unit)
        {
            if (selectedUnit != null)
            {
                selectedUnit.Highlight(false);
            }

            selectedUnit = unit;
            selectedUnit.Highlight(true);
            ClearHighlights();
            currentPath = null;
            isPathDisplayed = false;

            if (isSkillScrollViewActive)
            {
                UpdateSkillUI();
            }
        }
    }

    /// <summary>
    /// 스킬 UI를 업데이트합니다.
    /// </summary>
    private void UpdateSkillUI()
    {
        // 기존 스킬 버튼 제거
        foreach (var toggle in skillToggles)
        {
            Destroy(toggle.gameObject);
        }
        skillToggles.Clear();

        if (selectedUnit != null)
        {
            foreach (var skill in selectedUnit.skills)
            {
                GameObject buttonObj = Instantiate(skillButtonPrefab, skillButtonContainer);
                Toggle toggle = buttonObj.GetComponent<Toggle>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

                buttonText.text = skill.skillName;
                toggle.onValueChanged.AddListener((isOn) => OnSkillToggled(skill, isOn));

                skillToggles.Add(toggle);
            }
        }

        // 스크롤 뷰 컨텐츠 크기 조정
        LayoutRebuilder.ForceRebuildLayoutImmediate(skillButtonContainer as RectTransform);
    }

    /// <summary>
    /// 스킬 토글 상태가 변경될 때 호출됩니다.
    /// </summary>
    /// <param name="skill">선택된 스킬</param>
    /// <param name="isOn">토글 상태</param>
    private void OnSkillToggled(Skill skill, bool isOn)
    {
        if (isOn)
        {
            // 다른 모든 토글을 비활성화
            foreach (var toggle in skillToggles)
            {
                if (toggle.isOn && toggle.GetComponentInChildren<TextMeshProUGUI>().text != skill.skillName)
                {
                    toggle.isOn = false;
                }
            }

            SelectSkill(skill);
        }
        else
        {
            EndSkillMode();
        }
    }

    /// <summary>
    /// 목표 타일까지의 경로를 표시합니다.
    /// </summary>
    /// <param name="targetCell">목표 셀의 위치</param>
    private void ShowPathToTile(Vector3Int targetCell)
    {
        if (selectedUnit == null) return;

        HashSet<Vector3Int> obstacles = GetObstacles();
        currentPath = AStar.FindPath(selectedUnit.CurrentCell, targetCell, obstacles, IsWall);

        if (currentPath != null && currentPath.Count > 0)
        {
            ClearHighlights();
            HighlightPath(currentPath);
        }
    }

    /// <summary>
    /// 경로를 하이라이트합니다.
    /// </summary>
    /// <param name="path">경로 리스트</param>
    private void HighlightPath(List<Vector3Int> path)
    {
        int remainingAP = selectedUnit.currentAP;
        int remainingSP = selectedUnit.currentSP;
        bool resourceExceeded = false;

        for (int i = 1; i < path.Count; i++)
        {
            if (resourceExceeded)
            {
                HighlightTile(path[i], orangeHighlight);
            }
            else
            {
                remainingAP -= selectedUnit.baseAPMoveCost;
                remainingSP -= selectedUnit.baseSPMoveCost;
                if (remainingAP >= 0 && remainingSP >= 0)
                {
                    HighlightTile(path[i], greenHighlight);
                }
                else
                {
                    resourceExceeded = true;
                    i--;
                }
            }
        }
    }

    /// <summary>
    /// 선택된 유닛을 이동시킵니다.
    /// </summary>
    /// <param name="targetCell">목표 셀의 위치</param>
    private void MoveSelectedUnit(Vector3Int targetCell)
    {
        if (selectedUnit == null || currentPath == null) return;

        int maxMoves = selectedUnit.GetRemainingMovement();
        int targetIndex = currentPath.IndexOf(targetCell);
        if (targetIndex == -1) return;

        List<Vector3Int> movePath = currentPath.GetRange(0, Mathf.Min(maxMoves + 1, targetIndex + 1));

        StartCoroutine(selectedUnit.FollowPath(movePath));
        ClearHighlights();

        if (selectedUnit.currentAP == 0)
        {
            selectedUnit.Highlight(false);
            selectedUnit = null;
        }

        currentPath = null;
        isPathDisplayed = false;
    }

    #endregion

    #region 장애물 및 경로 찾기

    /// <summary>
    /// 장애물 목록을 가져옵니다.
    /// </summary>
    /// <returns>장애물의 위치를 포함하는 HashSet</returns>
    private HashSet<Vector3Int> GetObstacles()
    {
        HashSet<Vector3Int> obstacles = new HashSet<Vector3Int>();

        // 모든 유닛의 현재 위치를 장애물로 추가
        foreach (Unit unit in playerUnits.Concat(enemyUnits))
        {
            if (unit != selectedUnit)
                obstacles.Add(unit.CurrentCell);
        }

        // 벽 레이어에 있는 타일을 장애물로 추가
        BoundsInt bounds = highlightTilemap.cellBounds;
        for (int x = bounds.xMin; x <= bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y <= bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (IsWall(cell))
                {
                    obstacles.Add(cell);
                }
            }
        }

        return obstacles;
    }

    /// <summary>
    /// 해당 셀이 벽인지 확인합니다.
    /// </summary>
    /// <param name="cell">셀의 위치</param>
    /// <returns>벽이면 true, 아니면 false</returns>
    public bool IsWall(Vector3Int cell)
    {
        Vector3 worldPosition = grid.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0);
        return Physics2D.OverlapCircle(worldPosition, 0.1f, wallLayer) != null;
    }

    #endregion

    #region 하이라이팅

    /// <summary>
    /// 특정 타일을 하이라이트합니다.
    /// </summary>
    /// <param name="cell">타일의 위치</param>
    /// <param name="color">하이라이트 색상</param>
    private void HighlightTile(Vector3Int cell, Color color)
    {
        if (highlightTilemap == null)
        {
            Debug.LogError("HighlightTile: highlightTilemap is null");
            return;
        }

        if (highlightTile == null)
        {
            Debug.LogError("HighlightTile: highlightTile is null");
            return;
        }

        highlightTilemap.SetTile(cell, highlightTile);
        highlightTilemap.SetTileFlags(cell, TileFlags.None);
        highlightTilemap.SetColor(cell, color);
        highlightedTiles[cell] = color;

        Debug.Log($"Highlighted tile at {cell} with color {color}");
    }

    /// <summary>
    /// 특정 타일의 하이라이트를 제거합니다.
    /// </summary>
    /// <param name="cell">타일의 위치</param>
    public void RemoveHighlight(Vector3Int cell)
    {
        highlightTilemap.SetTile(cell, null);
        highlightedTiles.Remove(cell);
    }

    /// <summary>
    /// 모든 하이라이트를 제거합니다.
    /// </summary>
    private void ClearHighlights()
    {
        foreach (Vector3Int cell in highlightedTiles.Keys)
        {
            highlightTilemap.SetTile(cell, null);
        }
        highlightedTiles.Clear();
    }

    #endregion

    #region 턴 관리

    /// <summary>
    /// 플레이어의 턴을 종료합니다.
    /// </summary>
    private void EndPlayerTurn()
    {
        // 모든 플레이어 유닛의 스탯 회복 및 쿨다운 업데이트
        foreach (var unit in playerUnits)
        {
            unit.RecoverStatsOnTurnStart();
            unit.UpdateCooldowns();
            Debug.Log($"{unit.gameObject.name}의 스탯이 회복되었습니다. AP: {unit.currentAP}, SP: {unit.currentSP}");
        }

        // 턴 상태 변경 및 적 턴 처리 시작
        currentTurn = TurnState.EnemyTurn;
        SetupTurn(currentTurn);
        StartCoroutine(HandleEnemyTurn());

        // 턴 종료 시 스킬창 비활성화
        HideSkillScrollView();
    }

    /// <summary>
    /// 턴 종료 시 플레이어 유닛을 리셋합니다.
    /// </summary>
    private void ResetPlayerUnits()
    {
        if (selectedUnit != null)
        {
            selectedUnit.Highlight(false);
            selectedUnit = null;
        }

        ClearHighlights();
        currentPath = null;

        foreach (Unit unit in playerUnits)
        {
            unit.RecoverStatsOnTurnStart();
        }
    }

    /// <summary>
    /// 적의 턴 로직을 처리합니다.
    /// </summary>
    /// <returns>IEnumerator</returns>
    private System.Collections.IEnumerator HandleEnemyTurn()
    {
        foreach (Unit enemyUnit in enemyUnits)
        {
            yield return new WaitForSeconds(1f);
            // TODO: 여기에 적 AI 로직을 추가합니다.
        }

        // 턴 상태를 플레이어 턴으로 변경
        currentTurn = TurnState.PlayerTurn;
        SetupTurn(currentTurn);
    }

    /// <summary>
    /// 현재 상태에 따라 턴을 설정합니다.
    /// </summary>
    /// <param name="turn">현재 턴 상태</param>
    private void SetupTurn(TurnState turn)
    {
        if (turn == TurnState.PlayerTurn)
        {
            endTurnButton.interactable = true;
            attackButton.interactable = true;

            if (playerUnits.Count > 0)
            {
                SelectUnit(playerUnits[0]);
            }
        }
        else
        {
            endTurnButton.interactable = false;
            attackButton.interactable = false;
        }
    }

    #endregion

    #region 전투

    /// <summary>
    /// 공격 모드를 토글합니다.
    /// </summary>
    private void ToggleAttackMode()
    {
        if (selectedUnit == null || !selectedUnit.CanAttack()) return;

        isAttackMode = !isAttackMode;
        isSkillMode = false; // 스킬 모드 비활성화
        selectedSkill = null; // 선택된 스킬 초기화

        if (isAttackMode)
        {
            ShowAttackRange();
            HideSkillScrollView(); // 공격 모드 활성화 시 스킬창 비활성화
        }
        else
        {
            ClearHighlights();
        }
    }

    /// <summary>
    /// 공격 범위를 표시합니다.
    /// </summary>
    private void ShowAttackRange()
    {
        ClearHighlights();
        if (selectedUnit == null || selectedUnit.equippedWeapon == null) return;

        int range = selectedUnit.equippedWeapon.range;
        Vector3Int unitPos = selectedUnit.CurrentCell;

        HashSet<Vector3Int> rangeCells = GetCircularRange(unitPos, range);

        foreach (Vector3Int cell in rangeCells)
        {
            HighlightTile(cell, orangeHighlight);
        }
    }

    /// <summary>
    /// 원형에 가까운 범위 내의 셀을 반환합니다.
    /// </summary>
    /// <param name="center">중심점 셀</param>
    /// <param name="range">범위</param>
    /// <returns>범위 내의 셀 집합</returns>
    private HashSet<Vector3Int> GetCircularRange(Vector3Int center, int range)
    {
        HashSet<Vector3Int> cells = new HashSet<Vector3Int>();
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (dx * dx + dy * dy <= range * range)
                {
                    Vector3Int cell = center + new Vector3Int(dx, dy, 0);
                    if (IsValidAndVisibleCell(center, cell))
                    {
                        cells.Add(cell);
                    }
                }
            }
        }
        return cells;
    }

    /// <summary>
    /// 셀이 유효하고 중심점에서 보이는지 확인합니다.
    /// </summary>
    /// <param name="center">중심점 셀</param>
    /// <param name="target">대상 셀</param>
    /// <returns>유효하고 보이면 true, 아니면 false</returns>
    private bool IsValidAndVisibleCell(Vector3Int center, Vector3Int target)
    {
        if (IsWall(target)) return false;

        Vector3 centerWorld = grid.CellToWorld(center) + new Vector3(0.5f, 0.5f, 0);
        Vector3 targetWorld = grid.CellToWorld(target) + new Vector3(0.5f, 0.5f, 0);
        Vector2 direction = targetWorld - centerWorld;
        float distance = direction.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(centerWorld, direction, distance, wallLayer);
        return !hit;
    }

    /// <summary>
    /// 공격 입력을 처리합니다.
    /// </summary>
    /// <param name="targetCell">공격 대상 셀의 위치</param>
    private void HandleAttack(Vector3Int targetCell)
    {
        Unit targetUnit = enemyUnits.Find(u => u.CurrentCell == targetCell);
        if (targetUnit != null && selectedUnit.IsInRange(targetUnit))
        {
            PerformAttack(targetUnit);
            EndAttackMode();

            // 공격 후 스킬창 비활성화
            HideSkillScrollView();
        }
    }

    /// <summary>
    /// 대상 유닛에 공격을 수행합니다.
    /// </summary>
    /// <param name="targetUnit">공격 대상 유닛</param>
    private void PerformAttack(Unit targetUnit)
    {
        int distance = CalculateDistance(selectedUnit.CurrentCell, targetUnit.CurrentCell);
        int accuracyBonus = selectedUnit.equippedWeapon?.GetAccuracyBonus() ?? 0;
        int hitModifier = 10 - (distance - 1) + (selectedUnit.TEC - targetUnit.TEC) + accuracyBonus;

        int randomValue = Random.Range(0, 20);

        Debug.Log($"공격 주사위: {randomValue}, 명중 보정: {hitModifier}, 정확도 보너스: {accuracyBonus}");

        if (randomValue < hitModifier)
        {
            bool isCritical = (hitModifier > 0 && randomValue == 0);
            int damage = selectedUnit.CalculateAttackPower();
            if (isCritical)
            {
                damage *= 2;
                Debug.Log("치명타!");
            }
            targetUnit.TakeDamage(damage, selectedUnit.equippedWeapon.damageType);
            Debug.Log($"{selectedUnit.gameObject.name}이(가) {targetUnit.gameObject.name}에게 {damage}의 피해를 입혔습니다.");
        }
        else
        {
            Debug.Log($"{selectedUnit.gameObject.name}이(가) {targetUnit.gameObject.name}에게 공격을 빗나갔습니다.");
        }

        selectedUnit.currentAP -= 4;
        selectedUnit.currentSP -= 10;
    }

    /// <summary>
    /// 공격 모드를 종료합니다.
    /// </summary>
    private void EndAttackMode()
    {
        isAttackMode = false;
        ClearHighlights();

        if (selectedUnit.currentAP == 0)
        {
            selectedUnit.Highlight(false);
            selectedUnit = null;
        }
    }

    /// <summary>
    /// 두 셀 사이의 거리를 계산합니다.
    /// </summary>
    /// <param name="cell1">첫 번째 셀</param>
    /// <param name="cell2">두 번째 셀</param>
    /// <returns>맨해튼 거리</returns>
    private int CalculateDistance(Vector3Int cell1, Vector3Int cell2)
    {
        return Mathf.Abs(cell1.x - cell2.x) + Mathf.Abs(cell1.y - cell2.y);
    }

    #endregion

    #region 스킬

    /// <summary>
    /// 스킬을 선택합니다.
    /// </summary>
    /// <param name="skill">선택한 스킬</param>
    public void SelectSkill(Skill skill)
    {
        if (selectedUnit == null)
        {
            Debug.Log("스킬을 사용할 유닛이 선택되지 않았습니다.");
            return;
        }

        if (!selectedUnit.HasSkill(skill))
        {
            Debug.Log($"{selectedUnit.gameObject.name}는(은) 스킬 {skill.skillName}을(를) 가지고 있지 않습니다.");
            return;
        }

        if (selectedUnit.CanUseSkill(skill))
        {
            selectedSkill = skill;
            ShowSkillRange(skill);
            isSkillMode = true;
            isAttackMode = false; // 공격 모드 비활성화
            Debug.Log($"스킬 {skill.skillName}이(가) 선택되어 사용 준비가 완료되었습니다.");
        }
        else
        {
            Debug.Log($"선택한 스킬 {skill.skillName}을(를) 사용할 수 없습니다. 유닛: {selectedUnit.gameObject.name}");
        }
    }

    /// <summary>
    /// 선택한 스킬의 범위를 표시합니다.
    /// </summary>
    /// <param name="skill">선택한 스킬</param>
    /// <summary>
    /// 선택한 스킬의 범위를 표시합니다.
    /// </summary>
    /// <param name="skill">선택한 스킬</param>
    private void ShowSkillRange(Skill skill)
    {
        ClearHighlights();
        if (selectedUnit == null)
        {
            Debug.LogWarning("ShowSkillRange: selectedUnit is null");
            return;
        }

        Debug.Log($"ShowSkillRange: Skill: {skill.skillName}, TargetType: {skill.targetType}, Range: {skill.range}");

        Vector3Int unitPos = selectedUnit.CurrentCell;
        HashSet<Vector3Int> affectedCells = new HashSet<Vector3Int>();

        switch (skill.targetType)
        {
            case TargetType.Self:
                affectedCells.Add(unitPos);
                break;
            case TargetType.Ally:
            case TargetType.Enemy:
            case TargetType.All:
            case TargetType.AllAllies:
            case TargetType.AllEnemies:
                affectedCells = GetCircularRange(unitPos, skill.range);
                break;
        }

        Debug.Log($"Affected cells count: {affectedCells.Count}");

        foreach (Vector3Int cell in affectedCells)
        {
            HighlightTile(cell, GetHighlightColorForSkill(skill));
        }
    }

    /// <summary>
    /// 주어진 중심점으로부터 특정 범위 내의 모든 셀을 반환합니다.
    /// </summary>
    /// <param name="center">중심점 셀</param>
    /// <param name="range">범위</param>
    /// <returns>범위 내의 셀 집합</returns>
    private HashSet<Vector3Int> GetCellsInRange(Vector3Int center, int range)
    {
        HashSet<Vector3Int> cells = new HashSet<Vector3Int>();
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= range)
                {
                    cells.Add(center + new Vector3Int(dx, dy, 0));
                }
            }
        }
        return cells;
    }

    /// <summary>
    /// 스킬 유형에 따른 하이라이트 색상을 반환합니다.
    /// </summary>
    /// <param name="skill">스킬</param>
    /// <returns>하이라이트 색상</returns>
    private Color GetHighlightColorForSkill(Skill skill)
    {
        switch (skill.targetType)
        {
            case TargetType.Self:
                return new Color(0, 1, 0, 0.5f); // 초록색
            case TargetType.Ally:
            case TargetType.AllAllies:
                return new Color(0, 0, 1, 0.5f); // 파란색
            case TargetType.Enemy:
            case TargetType.AllEnemies:
                return new Color(1, 0, 0, 0.5f); // 빨간색
            case TargetType.All:
                return new Color(1, 1, 0, 0.5f); // 노란색
            default:
                return new Color(0.5f, 0.5f, 0.5f, 0.5f); // 회색
        }
    }

    /// <summary>
    /// 대상 셀에 스킬 사용을 처리합니다.
    /// </summary>
    /// <param name="targetCell">스킬 대상 셀의 위치</param>
    private void HandleSkillUse(Vector3Int targetCell)
    {
        if (selectedSkill == null || selectedUnit == null) return;

        switch (selectedSkill.targetType)
        {
            case TargetType.Self:
                if (targetCell == selectedUnit.CurrentCell)
                {
                    selectedUnit.UseSkill(selectedSkill, selectedUnit);
                    EndSkillMode();
                }
                break;

            case TargetType.Ally:
            case TargetType.Enemy:
                Unit targetUnit = GetUnitAtPosition(targetCell);
                if (targetUnit != null && IsValidTarget(targetUnit, selectedSkill.targetType) && IsWithinSkillRange(targetCell))
                {
                    selectedUnit.UseSkill(selectedSkill, targetUnit);
                    EndSkillMode();
                }
                else
                {
                    Debug.Log($"선택한 스킬의 유효한 대상이 아닙니다. 대상 셀: {targetCell}, 스킬 타입: {selectedSkill.targetType}");
                }
                break;

            case TargetType.AllAllies:
            case TargetType.AllEnemies:
            case TargetType.All:
                if (IsWithinSkillRange(targetCell))
                {
                    UseAreaSkill();
                    EndSkillMode();
                }
                else
                {
                    Debug.Log($"선택한 셀이 스킬 범위 밖입니다. 대상 셀: {targetCell}");
                }
                break;

            default:
                Debug.LogWarning($"처리되지 않은 TargetType: {selectedSkill.targetType}");
                break;
        }

        // 스킬 사용 후 스킬창 비활성화
        HideSkillScrollView();
    }

    /// <summary>
    /// 선택된 셀이 스킬 범위 내에 있는지 확인합니다.
    /// </summary>
    /// <param name="targetCell">확인할 셀의 위치</param>
    /// <returns>범위 내에 있으면 true, 아니면 false</returns>
    private bool IsWithinSkillRange(Vector3Int targetCell)
    {
        if (selectedUnit == null || selectedSkill == null) return false;

        int distance = CalculateDistance(selectedUnit.CurrentCell, targetCell);
        return distance <= selectedSkill.range;
    }

    /// <summary>
    /// 범위 스킬을 사용합니다.
    /// </summary>
    private void UseAreaSkill()
    {
        if (selectedSkill == null || selectedUnit == null) return;

        List<Unit> targets = GetTargetsInRange();
        Debug.Log($"UseAreaSkill: Found {targets.Count} targets for skill {selectedSkill.skillName}");

        if (targets.Count > 0 && selectedUnit.CanUseSkill(selectedSkill))
        {
            // 스킬 사용 전 자원 소모
            selectedUnit.currentAP -= selectedSkill.apCost;
            selectedUnit.currentSP -= selectedSkill.spCost;

            foreach (Unit target in targets)
            {
                selectedSkill.UseSkill(selectedUnit, target);
                Debug.Log($"Applied skill {selectedSkill.skillName} to target {target.gameObject.name}");
            }

            // 쿨다운 설정
            selectedUnit.SetSkillCooldown(selectedSkill);

            Debug.Log($"Skill use complete. Remaining AP: {selectedUnit.currentAP}, SP: {selectedUnit.currentSP}");
        }
        else
        {
            Debug.Log("Cannot use skill. Either no valid targets or insufficient resources.");
        }
    }


    /// <summary>
    /// 스킬 범위 내의 대상 유닛들을 가져옵니다.
    /// </summary>
    /// <returns>범위 내의 대상 유닛 리스트</returns>
    private List<Unit> GetTargetsInRange()
    {
        List<Unit> targets = new List<Unit>();
        List<Unit> potentialTargets;

        switch (selectedSkill.targetType)
        {
            case TargetType.AllAllies:
                potentialTargets = playerUnits;
                break;
            case TargetType.AllEnemies:
                potentialTargets = enemyUnits;
                break;
            case TargetType.All:
                potentialTargets = playerUnits.Concat(enemyUnits).ToList();
                break;
            default:
                Debug.LogWarning($"GetTargetsInRange called with unsupported TargetType: {selectedSkill.targetType}");
                return targets;
        }

        HashSet<Vector3Int> validCells = GetCircularRange(selectedUnit.CurrentCell, selectedSkill.range);

        foreach (Unit unit in potentialTargets)
        {
            if (validCells.Contains(unit.CurrentCell))
            {
                targets.Add(unit);
                Debug.Log($"Added target {unit.gameObject.name} at cell {unit.CurrentCell}");
            }
        }

        return targets;
    }

    /// <summary>
    /// 대상 유닛이 스킬에 유효한지 확인합니다.
    /// </summary>
    /// <param name="target">대상 유닛</param>
    /// <param name="targetType">스킬의 대상 유형</param>
    /// <returns>유효하면 true, 아니면 false</returns>
    private bool IsValidTarget(Unit target, TargetType targetType)
    {
        switch (targetType)
        {
            case TargetType.Self:
                return target == selectedUnit;

            case TargetType.Ally:
                return playerUnits.Contains(target);

            case TargetType.Enemy:
                return enemyUnits.Contains(target);

            case TargetType.AllAllies:
            case TargetType.AllEnemies:
            case TargetType.All:
                return true; // 이 타입들은 항상 유효합니다. 실제 적용은 스킬 사용 시 처리됩니다.

            default:
                Debug.LogWarning($"알 수 없는 TargetType: {targetType}");
                return false;
        }
    }

    /// <summary>
    /// 스킬 모드를 종료합니다.
    /// </summary>
    private void EndSkillMode()
    {
        isSkillMode = false;
        selectedSkill = null;
        ClearHighlights();
    }

    /// <summary>
    /// 턴 종료 시 유닛의 쿨다운을 업데이트합니다.
    /// </summary>
    private void EndTurn()
    {
        foreach (var unit in playerUnits.Concat(enemyUnits))
        {
            unit.UpdateCooldowns();
        }
        // TODO: 추가적인 턴 종료 로직을 구현합니다.
    }

    #endregion

    #region 유틸리티 메서드

    /// <summary>
    /// 특정 위치에 유닛이 있는지 확인합니다.
    /// </summary>
    /// <param name="position">확인할 위치</param>
    /// <returns>유닛이 있으면 true, 없으면 false</returns>
    public bool IsUnitAtPosition(Vector3Int position)
    {
        return playerUnits.Any(u => u.CurrentCell == position) || enemyUnits.Any(u => u.CurrentCell == position);
    }

    /// <summary>
    /// 특정 위치에 있는 유닛을 가져옵니다.
    /// </summary>
    /// <param name="position">유닛의 위치</param>
    /// <returns>해당 위치의 유닛, 없으면 null</returns>
    public Unit GetUnitAtPosition(Vector3Int position)
    {
        return playerUnits.FirstOrDefault(u => u.CurrentCell == position) ??
               enemyUnits.FirstOrDefault(u => u.CurrentCell == position);
    }

    #endregion

    public Unit GetNearestEnemy(Unit user)
    {
        Unit nearestEnemy = null;
        float minDistance = float.MaxValue;

        List<Unit> enemyList = user == playerUnits[0] ? enemyUnits : playerUnits;

        foreach (Unit enemy in enemyList)
        {
            float distance = Vector3.Distance(user.transform.position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }
}
