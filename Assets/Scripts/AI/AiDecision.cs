using System.Collections.Generic;
using UnityEngine;

public enum UnitType {None, Swordsman, Archer} // for future use, if we will empliment different types of units

public struct AttackDecision
{
    public int TargetPlayerId;
    public UnitType UnitType;
    public bool CanAttack => TargetPlayerId != -1 && UnitType != UnitType.None;

    public AttackDecision(int targetPlayerId, UnitType unitType)
    {
        TargetPlayerId = targetPlayerId;
        UnitType = unitType;
    }

    public static AttackDecision Wait()
    {
        return new AttackDecision(-1, UnitType.None);
    }
}
