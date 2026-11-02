# Valor Chronicle 보스 구현 가이드

> 기준: 현재 저장소의 실제 구현. 신규 보스를 추가하거나 공통 보스 계약을 변경할 때 이 문서와 관련 테스트를 함께 갱신한다.

## 빠른 체크리스트

새 보스를 구현할 때는 다음 순서를 따른다.

- [ ] 안정적인 보스 ID, 행동 Skill ID, Effect ID, 시각 상태 ID를 확정한다.
- [ ] 보스 전용 `CombatConfigDefinition` 클래스와 Asset을 만든다.
- [ ] `BossDefinition`, 난이도 스탯, 행동 `SkillDefinition`, `BossPresentationDefinition` Asset을 만든다.
- [ ] 보스별 RuntimeState와 `IBossCombatActionProvider`를 구현한다.
- [ ] `IBossCombatRuntimeFactory`를 구현한다.
- [ ] 필요하면 `IBossIntentSource`, `IBossVisualStateSource`, 보드 명령 및 전용 Effect를 구현한다.
- [ ] `BossCombatRuntimeFactoryCatalog.CreateDefault()`에 Factory를 등록한다.
- [ ] `DefinitionDatabase.asset`에 모든 콘텐츠 Asset을 등록한다.
- [ ] Factory, 패턴, UI, 결과 및 저장 분리 테스트를 작성한다.
- [ ] 전체 EditMode/PlayMode 회귀 테스트를 실행한다.

## 1. 최신 보스 아키텍처

### 1.1 전투 생성 흐름

```text
BossDefinition
    ↓
BattleEncounterFactory
    ↓
BossCombatRuntimeFactoryCatalog
    ↓ BossId로 IBossCombatRuntimeFactory 선택 및 TryValidate
BattleEncounterDefinition
    ↓ Boss + Difficulty + RuntimeFactory
BattleSceneCombatComposition
    ↓ RuntimeFactory.Create(BossDefinition, BossBattleState, ActionIds)
BossCombatRuntime
    ├── IBossCombatActionProvider ──→ BattleFlowCombatBridge
    ├── IBossIntentSource ─────────→ Bootstrap → HUD / DebugPanel
    └── IBossVisualStateSource ────→ Bootstrap → HUD 보스 이미지
```

실제 프로덕션 진입 경로는 다음과 같다.

1. `BattleEntryRequest`가 `BossId`와 `DifficultyId`를 전달한다.
2. `BattleEncounterFactory.TryCreate()`가 DB에서 `BossDefinition`을 찾는다.
3. `BossCombatRuntimeFactoryCatalog`에서 같은 Boss ID의 Factory를 찾는다.
4. Factory의 `TryValidate()`로 Definition과 전용 Config를 검증한다.
5. 선택한 `BossDifficultyStats`와 Factory를 `BattleEncounterDefinition`에 넣는다.
6. `BattleSceneCombatComposition`이 선택 난이도의 HP/ATK로 `BossBattleState`를 만든다.
7. Composition이 Factory의 `Create()`를 전투당 한 번 호출한다.
8. Factory가 새 RuntimeState와 Provider를 포함하는 `BossCombatRuntime`을 반환한다.
9. ActionProvider는 `BattleFlowCombatBridge`에, Intent/Visual Source는 Bootstrap을 거쳐 UI에 연결된다.

### 1.2 주요 파일과 책임

| 파일 | 책임 | 신규 보스 추가 시 |
|---|---|---|
| `Assets/Game/Data/Definitions/BossDefinition.cs` | 보스 ID, 속성, 턴 제한, 행동 ID, 난이도 스탯, Config 참조 | 클래스 수정 없음 |
| `Assets/Game/Battle/Entry/BattleEncounterFactory.cs` | DB·난이도·등록 Factory 검증 | 수정 없음 |
| `Assets/Game/Battle/Entry/BattleEncounterDefinition.cs` | 검증된 Boss, Difficulty, RuntimeFactory 보관 | 수정 없음 |
| `Assets/Game/Battle/Combat/Integration/IBossCombatRuntimeFactory.cs` | 보스 Definition 검증과 Runtime 생성 계약 | 구현체 추가 |
| `Assets/Game/Battle/Combat/Integration/BossCombatRuntimeFactoryCatalog.cs` | Boss ID별 Factory 등록·조회 | `CreateDefault()`에 등록 1줄 추가 |
| `Assets/Game/Battle/Combat/Integration/BossCombatRuntime.cs` | Action/Intent/Visual Provider 묶음 | Factory에서 생성 |
| `Assets/Game/Battle/Flow/Presentation/BattleSceneCombatComposition.cs` | 파티, BossBattleState, 실행기, Runtime, 결과 저장 조립 | 수정 없음 |
| `Assets/Game/Battle/Flow/Presentation/BattleSceneCombatBootstrap.cs` | 요청 소비, Encounter/Composition 초기화, 공통 UI Source 노출 | 수정 없음 |
| `Assets/Game/Battle/Flow/Presentation/BattleHudController.cs` | 공통 Intent, Effect/Resource, 보스 이미지 표시 | 조건 충족 시 수정 없음 |
| `Assets/Game/Battle/Flow/Presentation/BattleFlowDebugPanel.cs` | 공통 SkillId와 행동까지 남은 턴 표시 | 수정 없음 |
| `Assets/Game/Battle/Combat/Integration/BattleFlowCombatBridge.cs` | 행동, 보드 명령, 완료 커밋, 턴 종료, 결과 연결 | 일반 보스는 수정 없음 |

### 1.3 전투별 독립성

`BossCombatRuntimeFactoryCatalog`는 Factory 등록 정보만 보유한다. `CreateDefault()`는 호출할 때마다 새 카탈로그를 반환하며 전역 변경 가능 상태를 공유하지 않는다.

Factory의 `Create()`는 매 전투마다 다음을 새로 만들어야 한다.

- 보스별 RuntimeState
- ActionProvider
- 선택적 IntentSource
- 선택적 VisualStateSource
- 전투 시작 Effect 및 보스 전용 Resource

ScriptableObject Definition/Config에는 패턴 인덱스, 현재 상태, pending 행동 같은 런타임 값을 저장하지 않는다.

## 2. 신규 보스 데이터 관리

### 2.1 ID 규칙

콘텐츠 ID는 소문자, 숫자, 단일 밑줄만 사용한다. 저장에 사용된 ID는 마이그레이션 없이 변경하지 않는다.

권장 예:

- 보스: `frost_warden`
- 행동: `boss_action_frost_warden_ice_spear`
- Effect: `effect_frost_warden_frozen_core`
- 시각 상태: `frost_warden_enraged`

표시 이름을 내부 ID로 사용하지 않는다.

### 2.2 BossDefinition

Unity 메뉴:

`Assets > Create > Valor Chronicle > Definitions > Boss`

권장 저장 위치:

`Assets/Data/Bosses/boss_<boss_id>.asset`

Inspector 필드:

| 필드 | 설정 |
|---|---|
| `id` | 안정적인 Boss ID |
| `displayNameKey` | 현지화 키 |
| `element` | 보스 방어 속성 |
| `turnLimit` | 전투 제한 턴 |
| `actionOrSkillIds` | 해당 보스의 BossAction Skill ID |
| `difficultyStats` | 난이도별 MaxHp/Attack |
| `combatConfig` | 보스 전용 Config Asset |

`actionOrSkillIds`는 데이터 검증과 UI 콘텐츠 참조용이다. 이 배열만 채운다고 행동 코드가 자동 생성되지는 않는다.

### 2.3 BossDifficultyStats

현재 `BattleEncounterFactory`가 인정하는 난이도 ID는 다음 다섯 개다.

- `difficulty_intro`
- `difficulty_normal`
- `difficulty_advanced`
- `difficulty_hard`
- `difficulty_challenge`

각 항목에는 양수 `MaxHp`와 유한한 0 이상 `Attack`을 설정한다. 보스가 제공하지 않는 난이도는 Encounter 생성 단계에서 안전하게 거부된다. 새 난이도 ID를 추가하는 일은 보스 추가와 별도 공통 시스템 변경이다.

### 2.4 보스 전용 Config

보스별 수치와 불변 규칙은 `CombatConfigDefinition` 파생 ScriptableObject에 둔다.

```csharp
[CreateAssetMenu(
    fileName = "FrostWardenCombatConfig",
    menuName = "Valor Chronicle/Combat Configs/Frost Warden")]
public sealed class FrostWardenCombatConfig : CombatConfigDefinition
{
    [SerializeField]
    private double iceSpearCoefficient;

    public double IceSpearCoefficient => iceSpearCoefficient;

    public override bool TryValidate(out string errorMessage)
    {
        if (double.IsNaN(iceSpearCoefficient)
            || double.IsInfinity(iceSpearCoefficient)
            || iceSpearCoefficient < 0d)
        {
            errorMessage =
                "IceSpearCoefficient must be finite and non-negative.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
```

권장 저장 위치:

`Assets/Data/CombatConfigs/<BossName>/`

Config는 런타임에 수정하지 않는다.

### 2.5 SkillDefinition

Unity 메뉴:

`Assets > Create > Valor Chronicle > Definitions > Skill`

보스 행동마다 다음을 설정한다.

- `id`: BossDefinition의 `actionOrSkillIds`와 Intent의 `SkillId`가 참조하는 값
- `displayNameKey`, `descriptionKey`: 현지화 키
- `icon`: HUD 행동 예고 Sprite
- `skillKind`: `BossAction`

권장 저장 위치:

`Assets/Data/Skills/<BossName>/`

HUD는 `IBossIntentSource`의 SkillId를 `DefinitionDatabase.TryGetSkill()`로 조회해 Icon을 표시한다.

새 현지화 키는 프로젝트의 Localization 데이터에도 등록한다. Definition의 키 문자열만 설정하면 실제 표시 문구가 자동 생성되는 것은 아니다.

### 2.6 BossPresentationDefinition

Unity 메뉴:

`Assets > Create > Valor Chronicle > Definitions > Boss Presentation`

권장 저장 위치:

`Assets/Data/BossPresentations/<BossName>/`

Inspector 필드:

- `bossId`: BossDefinition ID와 동일
- `defaultSprite`: 필수 기본 전투 Sprite
- `faceSprite`: 보스 선택 UI용 얼굴 Sprite
- `stateVisuals`: 선택적 VisualStateId/Sprite 목록

`IBossVisualStateSource.CurrentVisualStateId`가 `stateVisuals`의 ID와 정확히 일치해야 한다.

### 2.7 DefinitionDatabase 등록

`Assets/Data/Database/DefinitionDatabase.asset`에 다음 Asset을 등록한다.

- `bosses`: BossDefinition
- `skills`: 보스 행동 SkillDefinition
- `effects`: UI 메타데이터가 필요한 EffectDefinition
- `resources`: UI에 표시할 전용 ResourceDefinition
- `bossPresentations`: BossPresentationDefinition

`DefinitionDatabase.Initialize()`는 중복 ID와 잘못된 프레젠테이션 참조를 거부한다. `DataValidator`는 Config, 턴 제한, 난이도, BossAction Skill 참조, 기본 Sprite와 시각 상태 ID를 검사한다.

## 3. 보스 전용 코드 구현

### 3.1 필수 구현

#### RuntimeState

패턴 인덱스, 보스 고유 상태, pending 전환 등 전투마다 독립적인 값을 보관하는 plain C# 클래스다.

원칙:

- Config는 읽기 전용 참조로 보관한다.
- 예고 조회는 상태를 변경하지 않는다.
- 행동이 완전히 성공하기 전에는 다음 패턴을 커밋하지 않는다.
- Definition Asset에 현재 상태를 쓰지 않는다.

#### ActionProvider

최소 계약은 `IBossCombatActionProvider`다.

```csharp
public sealed class FrostWardenBossCombatActionProvider
    : IBossCombatActionProvider
{
    private readonly FrostWardenBattleRuntimeState runtimeState;

    public FrostWardenBossCombatActionProvider(
        FrostWardenBattleRuntimeState runtimeState)
    {
        this.runtimeState = runtimeState
            ?? throw new ArgumentNullException(nameof(runtimeState));
    }

    public IReadOnlyList<CombatAction> CreateRootActions(
        BossCombatActionContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        double coefficient =
            runtimeState.Config.IceSpearCoefficient;
        return new CombatAction[]
        {
            new BossDamageAction(
                context.ActionIds.Next(),
                new BossDamageContextBuildRequest(
                    context.Boss,
                    context.Party,
                    coefficient,
                    AttackTag.None,
                    BossDamageHitType.Direct))
        };
    }
}
```

위 코드는 아직 존재하지 않는 FrostWarden 도메인 타입을 사용한 최소 예시지만, 호출하는 공통 API와 생성자 인자는 현재 프로젝트 코드와 일치한다. `FrostWardenBattleRuntimeState`에는 읽기 전용 `Config` 프로퍼티가 있다는 전제다. 실제 기준은 `Assets/Game/Bosses/Kragmor/KragmorBossCombatActionProvider.cs`의 `AddDamageAction()`이다.

보드 명령이 필요하면 `IBossCombatActionPlanProvider`를 구현하고 `BossActionPlan`을 반환한다. 성공 후에만 패턴을 이동해야 하면 `IBossCombatActionCompletionHandler`도 구현한다.

#### RuntimeFactory

Factory는 보스 ID, Definition/Config 검증, 전투별 Runtime 생성을 담당한다.

```csharp
public sealed class FrostWardenBossCombatRuntimeFactory
    : IBossCombatRuntimeFactory
{
    public string BossId => FrostWardenRules.BossId;

    public bool TryValidate(
        BossDefinition definition,
        out string errorMessage)
    {
        if (definition == null || definition.Id != BossId)
        {
            errorMessage = "Frost Warden BossDefinition is invalid.";
            return false;
        }

        if (!(definition.CombatConfig
            is FrostWardenCombatConfig config))
        {
            errorMessage =
                "Frost Warden requires FrostWardenCombatConfig.";
            return false;
        }

        return config.TryValidate(out errorMessage);
    }

    public BossCombatRuntime Create(
        BossDefinition definition,
        BossBattleState boss,
        CombatActionIdSequence actionIds)
    {
        if (!TryValidate(definition, out string errorMessage))
        {
            throw new ArgumentException(
                errorMessage,
                nameof(definition));
        }

        var state = new FrostWardenBattleRuntimeState(
            (FrostWardenCombatConfig)definition.CombatConfig);
        var provider =
            new FrostWardenBossCombatActionProvider(state);

        return new BossCombatRuntime(provider);
    }
}
```

실제 Factory에서는 `boss`와 `actionIds`의 null 및 Boss ID/Element 일치도 검증한다. Provider가 `IBossIntentSource`도 구현하고 RuntimeState가 `IBossVisualStateSource`도 구현한다면 `new BossCombatRuntime(provider, provider, state)`처럼 함께 전달한다. 전투 시작 Effect나 Resource가 필요하면 이 `Create()` 안에서 한 번만 초기화한다.

### 3.2 선택적 구현

#### IBossIntentSource

행동 예고가 필요한 보스만 구현한다.

```csharp
public IReadOnlyList<BossIntentPreview> GetIntentForecast(int count)
{
    // 현재 패턴 상태를 읽기만 하고 변경하지 않는다.
    return new[]
    {
        new BossIntentPreview(
            turnsUntilAction: 1,
            skillId: FrostWardenRules.IceSpearSkillId)
    };
}
```

반환되는 `SkillId`는 DB에 등록된 `SkillDefinition`과 일치해야 한다. Source가 null이거나 빈 목록을 반환하면 HUD는 예고 슬롯을 숨긴다.

#### IBossVisualStateSource

보스 이미지 상태 전환이 필요한 경우 구현한다.

```csharp
public string CurrentVisualStateId => currentVisualStateId;
```

Source가 없으면 HUD는 기존 기본 보스 이미지를 사용한다.

`IBossVisualStateSource`는 보스 본체 이미지 상태를 선택한다. Buff/Debuff 상태 아이콘은 공통 `EffectInstance`와 DB의 Effect/Skill/Resource 메타데이터를 통해 별도로 표시된다.

#### 고유 Effect 및 기믹

기존 `EffectModifierType`으로 표현할 수 있으면 공통 `ApplyEffectAction`, `RemoveEffectAction`, `EffectCollection`을 사용한다. 패턴 카운터처럼 Modifier가 아닌 상태는 RuntimeState에 둔다.

보스 Effect 전환의 실제 기준 코드는 `KragmorBossCombatActionProvider.AddDefenseTransition()`이다.

```csharp
actions.Add(new RemoveEffectAction(
    actionIds.Next(),
    ActionOrigin.System,
    boss,
    currentEffect.RuntimeId));

long applyActionId = actionIds.Next();
actions.Add(new ApplyEffectAction(
    applyActionId,
    ActionOrigin.System,
    boss,
    KragmorDefenseEffectFactory.Create(
        nextState,
        applyActionId,
        runtimeState.Config)));
```

`CombatActionExecutor.ExecuteApplyEffect()`와 `ExecuteRemoveEffect()`가 대상 보스의 `BossBattleState.Effects`에 각각 `EffectCollection.ApplyEffect()`와 `RemoveEffect(runtimeId)`를 호출한다. 제거는 Effect ID가 아니라 해당 인스턴스의 양수 `RuntimeId`를 사용한다. 크라그모르는 새 Apply Action ID를 Effect의 Runtime ID와 CreationOrder로도 사용해 전투 범위에서 유일하게 만든다.

`EffectInstance.RemainingTurns`가 `null`이면 무기한이고, 유한 지속 시간은 양수로 시작한다. `BattleTurnEndProcessor.ProcessTurnEnd()`가 보스 행동의 CombatAction과 보드/후속 처리 이후 `boss.Effects.ProcessTurnEnd()`를 호출하면 1 감소하고, 0이 된 Effect는 즉시 컬렉션에서 제거된다. 이번 보스 행동 중 적용된 1턴 Effect도 같은 턴 종료 처리에서 0이 되므로, 다음 플레이어 턴까지 유지해야 한다면 필요한 지속 시간을 이 규칙에 맞춰 설정해야 한다.

같은 Effect ID 재적용은 `EffectStackPolicy`에 따라 처리된다.

| 정책 | 실제 처리 |
|---|---|
| `RefreshDuration` | 같은 Magnitude를 요구하고 지속 시간을 새 값으로 갱신 |
| `SeparateInstance` | 별도 Runtime ID/CreationOrder 인스턴스 추가 |
| `StackMagnitude` | Magnitude 합산 후 지속 시간 갱신 |
| `ReplaceWithStronger` | 더 강하면 교체, 같으면 더 긴 지속 시간 유지 |
| `ReplaceWithNewest` | 항상 새 인스턴스로 교체 |
| `Unique` | 기존 인스턴스를 그대로 유지 |
| `StackCount` | 동일 Magnitude를 요구하고 최대치까지 StackCount 증가 후 지속 시간 갱신 |

같은 Effect ID끼리는 Category, ModifierType, StackPolicy와 필터/그룹/최대 중첩 메타데이터가 같아야 한다. 전투 시작부터 필요한 Effect는 크라그모르처럼 Runtime Factory에서 `EffectCollection.ApplyEffect()`로 한 번 초기화할 수 있지만, 행동 중 변경은 CombatAction으로 표현해 실행 순서와 결과 추적을 보존한다.

새 보드 기믹이 기존 `BattleBoardMutationCommand`로 표현되지 않으면 보스 코드만으로 끝나지 않는다. 공통 Board 모델·Controller·View·테스트 확장이 필요하므로 별도 설계 작업으로 분리한다.

## 4. Factory Catalog 등록

새 Factory 구현 후 다음 파일만 수정한다.

`Assets/Game/Battle/Combat/Integration/BossCombatRuntimeFactoryCatalog.cs`

```csharp
public static BossCombatRuntimeFactoryCatalog CreateDefault()
{
    var catalog = new BossCombatRuntimeFactoryCatalog();
    catalog.Register(new KragmorBossCombatRuntimeFactory());
    catalog.Register(new FrostWardenBossCombatRuntimeFactory());
    return catalog;
}
```

등록 규칙:

- `Factory.BossId`는 유효한 콘텐츠 ID여야 한다.
- 같은 Boss ID를 두 번 등록하면 실패한다.
- Factory의 BossId와 BossDefinition.Id가 일치해야 한다.
- 등록되지 않은 보스는 `BattleEncounterFactory`에서 안전하게 거부된다.
- 카탈로그에 RuntimeState나 전투별 Provider 인스턴스를 저장하지 않는다.

Boss 선택 화면도 `BattleEncounterFactory.IsSupportedBoss()`를 사용하므로, 데이터 등록과 Factory 등록이 완료되면 별도 Boss ID 분기 없이 지원 여부를 판단한다.

## 5. 행동 패턴과 전투 연결

### 5.1 반복 패턴

고정 순환 패턴은 RuntimeState가 다음을 보관하도록 구현한다.

- 현재 패턴 인덱스
- 현재 보스 고유 상태
- pending 패턴/상태
- Config 참조

`GetIntentForecast(count)`는 현재 인덱스에서 모듈러 연산으로 미래 SkillId를 계산하되 상태를 변경하지 않는다.

### 5.2 CombatAction 재사용

가능하면 기존 공통 Action을 조합한다.

- 보스 피해: `BossDamageAction`
- Effect 적용/제거: `ApplyEffectAction`, `RemoveEffectAction`
- Resource 소비: 공통 Resource Action
- 여러 타격: 서로 다른 Action ID의 피해 Action 여러 개

각 Action ID는 Composition이 전달한 `CombatActionIdSequence`에서 발급한다.

#### 보스 공격 생성과 피해 계산

관련 구현 위치는 다음과 같다.

- Action/요청: `Assets/Game/Battle/Combat/Actions/BossDamageAction.cs`, `Assets/Game/Battle/Combat/Damage/BossDamageContextBuildRequest.cs`
- 실행: `CombatActionExecutor.ExecuteBossDamage()`
- 보정 수집/계산: `BossDamageContextFactory.Build()`, `BossDamageCalculator.Calculate()`
- 보호막/HP 적용: `PartyDamageApplier.Apply()`, `ShieldCollection.Absorb()`

공격 계수는 `BossDamageContextBuildRequest`의 `attackCoefficient`로 전달한다. 크라그모르는 `KragmorBattleRuntimeState.NextIntent.DamageCoefficient`를 사용하고, Heavy 행동이면 `AttackTag.Heavy`를 붙인다. 현재 최종 보호막 적용 전 피해는 다음 순서로 계산한 뒤 내림하여 `long`으로 만든다.

```text
finalAttack = Boss.Attack × (1 + 공격 증가 합 - 공격 감소 합)
dealtMultiplier = 1 + 주는 피해 증가 합 - 주는 피해 감소 합
partyTakenMultiplier = max(0.30,
    1 + 받는 피해 증가 합 - min(받는 피해 감소 합, 0.70))
finalDamageBeforeShield = floor(
    finalAttack × attackCoefficient
    × dealtMultiplier × partyTakenMultiplier)
```

이후 `PartyDamageApplier`가 보호막을 먼저 소모하고 남은 피해를 파티 공유 HP에 적용한다. 보호막은 남은 지속 시간이 짧은 순서, 같으면 CreationOrder가 빠른 순서로 소모된다.

`CombatActionIdSequence.Next()`는 전투 범위에서 1부터 증가하는 ID를 발급하며 오버플로를 검사한다. `BattleFlowCombatBridge.ValidateBossRootActions()`는 Provider 호출 전후 발급 수가 반환 Action 수와 같은지, Action이 발급 순서대로인지, 각 루트 Action의 `RootActionId == ActionId`이고 `SourceActionId`가 없는지 검사한다. 보스 공격에는 플레이어용 `DamageAction`이 아니라 `BossDamageAction`을 사용해야 한다.

다단 공격은 타격마다 `actionIds.Next()`로 별도의 `BossDamageAction`을 만든다. 각 타격이 개별적으로 피해 보정, 보호막 흡수와 HP 적용을 수행한다. `NextBossHitDamageReduction`은 Direct 타격 하나마다 가장 강한 인스턴스 하나를 선택하며, 파티가 이미 전투 불능이 아니었다면 그 타격 후 소비된다. 따라서 `AttackTag.MultiHit`만 붙인 단일 Action은 다단 처리로 분할되지 않는다.

### 5.3 BossActionPlan과 보드 명령

보드 변경이 없으면 `IBossCombatActionProvider.CreateRootActions()`만 구현해도 된다.

보드 변경이 있으면 `IBossCombatActionPlanProvider.CreatePlan()`에서 다음을 반환한다.

```csharp
return new BossActionPlan(
    actions,
    new BattleBoardMutationCommand(
        BattleBoardMutationKind.CreateRock,
        requestedCount,
        maximumCount));
```

현재 공통 구현이 직접 지원하는 보스 보드 명령은 Rock 생성이다. 새 명령 종류는 공통 계층 확장이 필요하다.

### 5.4 완료 커밋과 사망 중단

`BattleFlowCombatBridge`의 실제 처리 순서는 다음과 같다.

1. `CreatePlan()`으로 순서가 고정된 CombatAction 목록과 선택적 BoardCommand 생성
2. CombatAction을 목록 순서대로 하나씩 실행; Effect 적용/제거도 이 위치에서 즉시 반영
3. 파티 전투 불능 또는 보스 사망 검사
4. BoardCommand가 있으면 CombatAction이 모두 성공한 뒤 실행하고 완료 callback 대기
5. 패턴 커밋, 선택적 post-boss 캐릭터 처리, 턴 종료 Effect/Shield 감소
6. `BattleFlowCoordinator.CompleteBossAction()`으로 다음 Phase 진행

5번의 세부 순서는 현재 post-boss Provider 존재 여부에 따라 다르다. Provider가 있으면 패턴을 먼저 커밋하고 post-boss Action을 처리한 뒤 턴 종료를 수행한다. Provider가 없으면 턴 종료 후 패턴을 커밋한다. 보스 패턴 구현은 이 상대 순서에 의존하지 말고, `TryCommitCompletedAction()`에서는 pending 상태 확정만 수행해야 한다. 이 차이를 하나의 순서로 통일하는 기능은 현재 구현되어 있지 않다.

행동 도중 보스 사망으로 전투 결과가 확정되면 남은 행동·보드 명령·패턴 커밋을 진행하지 않는다. Provider는 계획 생성 시 실제 패턴 인덱스를 먼저 변경하지 않아야 한다.

조건부 패턴은 Intent와 실제 행동이 같은 RuntimeState의 커밋된 값과 같은 결정 함수를 읽도록 만든다. `GetIntentForecast()`는 상태를 바꾸지 않고, `CreatePlan()`은 현재 조건으로 선택한 패턴/분기를 pending 값으로 보관하며, 성공한 행동만 커밋한다. 실행 시 새 난수를 다시 뽑거나 예고와 다른 조건 함수를 사용하면 안 된다. 현재 `IBossIntentSource.GetIntentForecast(count)`에는 전투 Context 인자가 없으므로 파티 상태처럼 외부 조건에 따라 예고가 달라지는 보스는 Provider가 필요한 읽기 전용 상태를 보유하거나, 선택 결과를 RuntimeState에 미리 고정하는 별도 설계가 필요하다.

### 5.5 기준 구현: Kragmor

Kragmor는 현재 공통 경로가 실제 전용 규칙을 어떻게 조립하는지 보여 주는 기준 구현이다.

| 순서 | Skill ID | 주요 처리 |
|---:|---|---|
| 1 | `boss_action_kragmor_colossus_iron_fist` | 직접 피해, 필요하면 화산암 외피 복귀 |
| 2 | `boss_action_kragmor_rockshard_eruption` | 직접 피해와 Rock 생성 명령 |
| 3 | `boss_action_kragmor_core_compression` | 피해 없이 지핵 압축 Effect 전환 |
| 4 | `boss_action_kragmor_earth_collapse` | Heavy 피해와 노심 노출 Effect 전환 |

`KragmorBattleRuntimeState.GetIntentForecast()`는 이 순서를 읽기만 하며 `PatternIndex`를 변경하지 않는다. `KragmorBossCombatActionProvider`는 pending 패턴과 방어 상태를 보관하고 전체 행동 완료 뒤에만 커밋한다.

`KragmorBossCombatRuntimeFactory.Create()`는 전투마다 RuntimeState와 Provider를 새로 만들고 시작 상태인 화산암 외피 Effect를 정확히 한 번 초기화한다. Composition은 이 초기화를 중복 수행하지 않는다.

현재 Rock 기믹은 다음 공통 구성 요소를 사용한다.

- `BoardBlockType.Rock`: 원소가 없고 직접 Match되지 않는 블록
- `BattleBoardMutationKind.CreateRock`: 보스 Plan이 요청하는 보드 명령
- `BoardRockMutationResolver`: 생성 수와 최대 수를 제한하고 플레이 가능한 보드를 유지
- `BattleBoardController.TryExecuteMutation()`: 모델·View 반영 후 완료 callback 전달

Rock과 같은 기존 명령을 재사용하는 보스는 Bridge를 수정할 필요가 없다. 다른 규칙의 특수 블록이나 새 MutationKind는 공통 보드 시스템 확장 범위다.

## 6. UI 연결

### 6.1 행동 예고

`BossCombatRuntime.IntentSource`는 다음 경로로 전달된다.

```text
RuntimeFactory
  → BossCombatRuntime.IntentSource
  → BattleSceneCombatComposition.BossIntentSource
  → BattleSceneCombatBootstrap.BossIntentSource
  → BattleHudController / BattleFlowDebugPanel
```

HUD는 보스 ID나 구체 Provider 타입을 확인하지 않는다. 각 `BossIntentPreview`의 다음 값만 사용한다.

- `SkillId`
- `TurnsUntilAction`

다음 조건을 만족하면 신규 보스를 위해 HUD 코드를 수정할 필요가 없다.

- Factory가 공통 `IBossIntentSource`를 Runtime에 넣는다.
- Intent SkillId와 같은 `SkillDefinition`이 DB에 등록돼 있다.
- SkillDefinition에 Icon을 설정한다.
- 예고 조회가 런타임 상태를 변경하지 않는다.

IntentSource가 없거나 예고가 비어 있으면 슬롯은 숨겨진다. 없는 Skill이나 null Icon은 기존 HUD 기본 Sprite를 사용한다. 전투 종료 및 보스 변경 시 이전 Sprite와 남은 턴 텍스트를 초기화한다.

### 6.2 보스 시각 상태

`BossCombatRuntime.VisualStateSource`는 Bootstrap의 공통 `IBossVisualStateSource`로 노출된다.

HUD는 `BossPresentationDefinition`을 실제 전투 BossId로 조회한 뒤, `CurrentVisualStateId`에 맞는 Sprite를 선택한다. Source가 null이거나 상태 ID를 찾지 못하면 기본 Sprite를 사용한다.

크라그모르의 화산암 외피, 지핵 압축, 노심 노출도 이 공통 경로를 사용한다.

### 6.3 공통 상태 아이콘

Buff/Debuff와 보스 Resource 표시는 특정 보스 타입이 아니라 런타임 Effect/Resource ID와 DB 메타데이터로 연결한다. 새 보스 상태를 표시하려면 해당 EffectDefinition 또는 ResourceDefinition을 DB에 등록한다.

## 7. 결과 및 세이브

### 7.1 전달 경로

```text
BossDefinition.Id
  → BossBattleState.BossId
  → BattleFlowCombatBridge
  → BattleFinalizationInput
  → BattleFinalResult.BossId
  → BattleResultPersistenceCoordinator
  → BattleResultPersistenceService
  → BattleResultProfileUpdater
  → BossRecordSaveData
```

선택 난이도는 다음 경로를 따른다.

```text
BattleEntryRequest.DifficultyId
  → BossDifficultyStats.DifficultyId
  → BattleSceneCombatComposition.DifficultyId
  → BattleFlowCombatBridge
  → BattleFinalResult.DifficultyId
  → BossRecordSaveData.DifficultyId
```

`BattleResultProfileUpdater`는 `BossId + DifficultyId`가 모두 같은 레코드만 재사용한다. 따라서 다음 값은 보스와 난이도 조합별로 독립적이다.

- 도전/클리어 여부
- 최고 점수
- 최고 등급
- 최고 기록의 처치 턴과 잔여 턴
- 수령한 최초 등급 보상

기존 `BossRecordSaveData`가 이미 문자열 BossId와 DifficultyId를 저장하므로 일반적인 신규 보스 추가에 세이브 DTO 변경은 필요 없다.

단, 신규 BossDefinition과 난이도는 `DefinitionDatabase`에 등록돼야 저장 참조 검증을 통과한다. 기존에 저장에 사용한 Boss ID를 변경하려면 별도 마이그레이션이 필요하다.

### 7.2 점수, 등급과 보상 데이터

현재 밸런스 데이터는 보스별이 아니라 프로젝트 공통 단일 Asset인 `Assets/Data/BattleResults/BattleResultBalance.asset`에 있다. `DefinitionDatabase.battleResultBalance`가 이 `BattleResultBalanceDefinition`을 참조하고, `BattleSceneCombatBootstrap`이 `CreateBalance()` 결과를 Composition에 전달한다.

계산 경로는 다음과 같다.

```text
플레이어가 보스에게 실제 적용한 DamageAction 피해
  → BattleDamageScoreCollector / BattleDamageScoreAccumulator
  → BattleFlowCombatBridge.HandleResultReached()
  → BattleResultFinalizer.Create()
  → BattleScoreCalculator.Calculate()
  → BattleGradeRewardCalculator.Calculate()
  → BattleFinalResult
  → BattleResultProfileUpdater.Apply()
```

피해 점수에는 보스의 파티 공격인 `BossDamageAction`이 아니라 보스에게 실제 적용된 `DamageActionResult.AppliedDamage`만 Action ID당 한 번 더해진다. 승리 시 최종 점수는 피해 점수에 `floor(BossMaxHp × 잔여 턴 / TurnLimit × remainingTurnBonusPercent)`를 더하며, 패배와 턴 제한 종료에는 잔여 턴 보너스가 없다.

현재 Asset의 데이터 범위는 다음과 같다.

- 등급 기준: BossMaxHp 대비 최종 점수 비율 C 20%, B 40%, A 60%, S 80%, SS 100%, SSS 115%
- 최초 등급 보상: C~SSS별 공통 금액; 새 최고 등급까지 아직 받지 않은 하위 등급 보상도 함께 지급
- 반복 보상: `DifficultyId + 산출 등급`별 금액; BelowC도 포함
- 저장 단위: 최초 등급 보상 수령 여부와 기록은 `BossId + DifficultyId`별로 독립

따라서 기존 다섯 난이도 ID를 사용하는 신규 보스는 별도 점수·보상 Asset 등록이 필요 없다. 해당 보스 난이도의 MaxHp가 절대 등급 점수선을 결정한다. 반대로 보스별 등급 비율, 보스별 최초 보상, 보스별 반복 보상은 현재 데이터 모델에 없으며 필요하면 `BattleResultBalanceDefinition`, 계산 API, 검증과 저장 정책을 함께 확장해야 한다. 새로운 난이도 ID를 도입한다면 최소한 반복 보상 표와 공통 난이도 검증도 추가해야 한다.

## 8. 현재 Kragmor 전용 코드와 호환 API

### 8.1 유지해야 하는 전용 구현

`Assets/Game/Bosses/Kragmor/` 아래의 다음 코드는 크라그모르 규칙이므로 공통화 대상이 아니다.

- `KragmorCombatConfig`
- `KragmorBattleRuntimeState`
- `KragmorBossCombatActionProvider`
- `KragmorBossCombatRuntimeFactory`
- `KragmorDefenseEffectFactory`
- Rock을 포함한 4행동 패턴과 전용 Intent 모델

### 8.2 테스트 호환성을 위해 유지한 API

`BattleSceneCombatComposition`에는 다음 호환 표면이 남아 있다.

- Kragmor Config를 받는 기존 생성자
- `KragmorRuntimeState`
- `BossActionProvider`

이 API는 상세 Kragmor 회귀 테스트에서 사용된다. 실제 Bootstrap 프로덕션 경로는 검증된 `BattleEncounterDefinition`과 공통 Runtime Factory 생성자를 사용하므로 신규 보스 확장성을 막지 않는다.

`KragmorBossCombatActionProvider`의 `IKragmorBossIntentSource` 구현도 전용 테스트·진단 호환용이다. Bootstrap, HUD, DebugPanel은 이 인터페이스를 참조하지 않는다.

### 8.3 현재 제거 대상 하드코딩

현재 실제 전투 경로에는 신규 보스 추가를 막는 Kragmor ID 분기나 Config 캐스팅이 없다. 호환 API 제거는 기능 구현 선행 조건이 아니며, 관련 테스트를 공통 Runtime 접근 방식으로 정리하는 별도 유지보수 작업에서 수행할 수 있다.

## 9. 신규 보스 구현 체크리스트

| 순서 | 작업 항목 | 생성 또는 수정할 파일 | 주요 클래스/메서드 | 완료 확인 기준 |
|---:|---|---|---|---|
| 1 | ID와 규칙 확정 | 새 Rules 파일 | BossId, SkillId, EffectId, VisualStateId 상수 | ID가 유효하고 기존 ID와 중복되지 않음 |
| 2 | 전용 Config 구현 | `Assets/Game/Bosses/<Boss>/...CombatConfig.cs` | `CombatConfigDefinition.TryValidate()` | 비정상 수치가 거부됨 |
| 3 | Config Asset 생성 | `Assets/Data/CombatConfigs/<Boss>/` | Inspector | 모든 수치 입력 및 검증 통과 |
| 4 | BossDefinition 생성 | `Assets/Data/Bosses/boss_<id>.asset` | ID, Element, TurnLimit, DifficultyStats, Config | 지원할 난이도와 행동 ID가 올바름 |
| 5 | Skill Asset 생성 | `Assets/Data/Skills/<Boss>/` | `SkillDefinition`, SkillKind.BossAction | Intent SkillId로 DB 조회 및 Icon 확인 |
| 6 | Presentation 생성 | `Assets/Data/BossPresentations/<Boss>/` | `BossPresentationDefinition` | 기본/얼굴/상태 Sprite 조회 가능 |
| 7 | RuntimeState 구현 | 새 RuntimeState 파일 | 패턴·상태·예고 계산 | 전투별 독립, 예고 조회가 상태 불변 |
| 8 | ActionProvider 구현 | 새 Provider 파일 | `CreateRootActions()` 또는 `CreatePlan()` | 행동·보드 명령·커밋 순서 검증 |
| 9 | 선택적 Intent 구현 | Provider 또는 별도 Source | `IBossIntentSource.GetIntentForecast()` | HUD에서 Skill Icon과 남은 턴 표시 |
| 10 | 선택적 Visual 구현 | RuntimeState 또는 별도 Source | `IBossVisualStateSource.CurrentVisualStateId` | Presentation 상태 Sprite 표시 |
| 11 | RuntimeFactory 구현 | 새 Factory 파일 | `BossId`, `TryValidate()`, `Create()` | 전투마다 새 Runtime/Provider 생성 |
| 12 | Catalog 등록 | `BossCombatRuntimeFactoryCatalog.cs` | `CreateDefault().Register(...)` | Encounter 생성 및 선택 화면 지원 |
| 13 | DB 등록 | `DefinitionDatabase.asset` | bosses/skills/effects/resources/presentations | Initialize와 DataValidator 통과 |
| 14 | 점수/보상 확인 | 공통 `BattleResultBalance.asset` | 등급 비율, 최초 보상, 난이도별 반복 보상 | 기존 난이도 사용 시 별도 보스 등록이 불필요함을 확인; 새 난이도면 공통 표 확장 |
| 15 | 결과/저장 검증 | EditMode 통합 테스트 | `BattleFinalResult`, `BossRecordSaveData` | BossId/DifficultyId별 기록 분리 |
| 16 | 전체 회귀 검증 | EditMode/PlayMode | 기존 Kragmor 및 신규 보스 테스트 | 기존 전투 동작과 신규 전투 모두 통과 |

## 10. 테스트 가이드

### 10.1 신규 Factory/Runtime

- 등록된 Boss ID로 Factory가 선택되는가?
- 미등록 Boss가 안전하게 거부되는가?
- 잘못된 Config 타입과 값이 거부되는가?
- Factory `Create()`가 Composition당 한 번만 호출되는가?
- 두 전투가 RuntimeState, Provider, pending 상태를 공유하지 않는가?
- Factory가 반환한 ActionProvider가 Bridge에 연결되는가?

기준 테스트:

- `Assets/Game/Tests/EditMode/Battle/Flow/Presentation/BattleEncounterFactoryTests.cs`
- `Assets/Game/Tests/EditMode/Battle/Flow/Presentation/BattleSceneCombatRuntimeFactoryTests.cs`
- `Assets/Game/Tests/EditMode/Bosses/Kragmor/KragmorBossCombatRuntimeFactoryTests.cs`

### 10.2 패턴과 행동

- 첫 행동과 전체 반복 순서
- 예고 조회 전후 패턴 인덱스 불변
- 행동 성공 후 한 번만 커밋
- 행동 실패, 보드 실패, 보스 사망 시 미커밋
- 전용 Effect/Resource 초기화가 정확히 한 번
- 별도 전투 사이 상태 비공유

### 10.3 UI

- 새 SkillId의 Icon과 남은 턴 표시
- null/빈/부분 Intent 예고
- 누락된 SkillDefinition과 null Icon
- VisualStateSource null 및 알려지지 않은 상태 ID
- 전투 종료와 HUD 재활성화 후 이전 Sprite/텍스트 제거
- 보스별 기본/상태 Sprite 전환

기준 테스트:

- `BattleHudControllerTests`
- `BattleFlowDebugPanelTests`
- `BattleSceneCombatBootstrapTests`

### 10.4 결과와 저장

- 실제 선택 BossId와 DifficultyId가 최종 결과에 유지되는가?
- 같은 난이도의 서로 다른 BossId가 별도 레코드를 만드는가?
- 최고 점수·최고 등급·최초 보상 수령이 서로 섞이지 않는가?
- 저장 참조 검증이 새 Boss와 난이도를 찾는가?

기준 테스트:

- `BattleResultRuntimeIntegrationTests`
- `BattleResultProfileUpdaterTests`
- `BattleSceneCombatPlayModeTests`

### 10.5 Unity Editor 수동 검증

1. Project 창에서 보스 전용 Config Asset을 만들고 모든 계수와 기믹 수치를 입력한다.
2. `Assets > Create > Valor Chronicle > Definitions > Boss`로 BossDefinition을 만들고 ID, 현지화 키, Element, TurnLimit, Action/Skill ID, 지원 난이도별 MaxHp/Attack, CombatConfig를 설정한다.
3. 각 행동의 SkillDefinition과 BossPresentationDefinition을 만들고 Intent Skill ID, Icon, 기본/얼굴/상태 Sprite를 연결한다. 상태 아이콘이 필요하면 EffectDefinition/ResourceDefinition도 만든다.
4. `Assets/Data/Database/DefinitionDatabase.asset`의 bosses, skills, effects, resources, bossPresentations 배열에 새 Asset을 등록한다. 공통 `battleResultBalance` 참조는 기존 Asset을 유지한다.
5. Factory가 `BossCombatRuntimeFactoryCatalog.CreateDefault()`에 등록됐는지 확인한 뒤 Play Mode로 Main 화면을 연다. 보스 얼굴·이름·속성이 목록에 나타나고 지원하지 않는 난이도가 선택되지 않는지 확인한다.
6. 각 지원 난이도를 선택해 Battle Scene에 진입한다. BossDefinition의 MaxHp/Attack/TurnLimit가 적용되고 기본 보스 Sprite가 보이는지 확인한다.
7. 최소 한 패턴 주기를 진행하며 HUD Intent의 Icon·남은 턴과 실제 행동이 일치하는지, 공격 계수·다단 타격·보호막 소모·Effect 상태 아이콘/지속 시간·보드 변경·VisualState 전환이 설계 순서대로인지 확인한다.
8. 승리, 파티 전투 불능, 턴 제한 종료를 각각 확인한다. 보드 변경 실패처럼 강제 재현이 필요한 오류 경로는 자동 테스트로 검증하고 정상 수동 플레이에서 임의로 Asset을 훼손하지 않는다.
9. 결과 화면에서 피해 점수, 잔여 턴 보너스, 등급, 반복 보상과 신규 최초 등급 보상을 확인한다. 같은 등급을 다시 달성하면 최초 보상이 재지급되지 않는지 확인한다.
10. Main 화면으로 돌아가 Challenge 기록 표시를 확인하고, 게임을 재시작한 뒤 같은 BossId/DifficultyId의 최고 점수·등급·클리어·보상 수령 상태가 유지되는지 확인한다. 다른 보스와 난이도의 기록이 바뀌지 않았는지도 확인한다.

이 절차는 실제 Editor 검증 항목이며 이 문서 작성 과정에서 실행했다는 의미가 아니다.

## 11. 유지보수 규칙

다음 변경을 할 때 이 문서를 함께 갱신한다.

- `IBossCombatRuntimeFactory` 또는 `BossCombatRuntime` 계약 변경
- Factory 등록 방식 변경
- `IBossCombatActionProvider`, Plan, Completion 계약 변경
- `IBossIntentSource` 또는 `BossIntentPreview` 변경
- `IBossVisualStateSource` 또는 BossPresentationDefinition 변경
- 새로운 공통 보드 명령 추가
- 결과의 BossId/DifficultyId 전달 또는 저장 키 변경
- Composition의 Kragmor 호환 API 제거

공통 계약 변경 시 최소한 다음 테스트를 확인한다.

- `BattleEncounterFactoryTests`
- `BattleSceneCombatRuntimeFactoryTests`
- `KragmorBossCombatRuntimeFactoryTests`
- `BattleSceneCombatBootstrapTests`
- `BattleHudControllerTests`
- `BattleFlowDebugPanelTests`
- `BattleResultRuntimeIntegrationTests`
- `BattleResultProfileUpdaterTests`
- `BattleSceneCombatPlayModeTests`

## 12. 알려진 한계와 확장 판단

- 신규 실제 Boss Factory는 `CreateDefault()`에 명시적으로 등록해야 한다. 이는 의도된 조립 지점이다.
- 현재 보스 선택은 기존 다섯 난이도 ID를 전제로 한다.
- Rock 이외의 새 보드 기믹은 공통 Board 계층 확장이 필요할 수 있다.
- `BossActionPlan`은 선택적 보드 명령 하나를 가진다. 한 행동에 여러 종류의 보드 명령이 필요하면 별도 설계가 필요하다.
- Kragmor 호환 생성자와 진단 프로퍼티는 테스트 때문에 남아 있지만 프로덕션 신규 보스 경로에서는 사용되지 않는다.
- 실제 두 번째 보스 콘텐츠는 아직 구현되지 않았다. Fake Factory 테스트는 확장 경로를 검증할 뿐 실제 콘텐츠를 대체하지 않는다.
- 점수 등급, 최초 보상과 반복 보상은 단일 공통 Balance를 사용하며 보스별 Balance 선택은 구현되어 있지 않다.
- post-boss Provider 유무에 따라 패턴 커밋과 턴 종료의 상대 순서가 다르므로, 보스별 커밋 로직은 턴 종료 Effect 변화에 의존해서는 안 된다.

이 한계에 해당하지 않는 일반적인 신규 보스는 기존 Bootstrap, HUD, DebugPanel, BattleFlowCombatBridge, 결과 DTO 및 세이브 구조를 수정하지 않고 추가할 수 있다.
