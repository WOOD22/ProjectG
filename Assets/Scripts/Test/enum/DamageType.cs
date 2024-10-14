public enum DamageType
{
    // 물리 타입
    Piercing,   // 관통
    Slashing,   // 절단
    Blunt,      // 충격

    // 속성 타입
    Wood,       // 목
    Fire,       // 화
    Earth,      // 토
    Metal,      // 금
    Water       // 수
}

public enum DamageReaction
{
    Weak,       // 약점 (2배 피해 입음)
    Resistant,  // 저항 (1/2배 피해 입음)
    Null,       // 무효 (피해 입지 않음)
    Absorb      // 흡수 (피해량 만큼 체력 회복)
}